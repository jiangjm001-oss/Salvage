// Assets/Scripts/GamePlay/Synthesis/SynthesisMachine.cs
// 合成机器控制器 - 管理陶罐合成过程
// 优化版：简化交互流程，添加平滑颜色过渡，持续震动音效
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 合成机器控制器
/// 负责管理机器的状态、接收陶罐、执行合成、展示结果
/// 
/// 简化后的使用流程：
/// 1. 玩家点击机器 → 打开盖子
/// 2. 玩家选中装满的陶罐，点击机器 → 放入陶罐（陶罐显示在机器中）
/// 3. 玩家点击盖子 → 关闭盖子
/// 4. 玩家点击按钮 → 启动合成（播放震动音效）
/// 5. 2秒后盖子自动打开（停止震动音效，播放开盖音效）→ 展示水晶碎片和空陶罐
/// 6. 玩家点击水晶碎片 → 拾取碎片
/// 7. 玩家点击陶罐 → 拾取空陶罐
/// </summary>
public class SynthesisMachine : MonoBehaviour
{
    // ============ 状态枚举 ============
    public enum MachineState
    {
        Idle_LidClosed,       // 空闲 - 盖子关闭
        Idle_LidOpen,         // 空闲 - 盖子打开，等待放入陶罐
        PotInserted_LidOpen,  // 已放入陶罐 - 盖子打开
        PotInserted_LidClosed,// 已放入陶罐 - 盖子关闭，等待启动
        Processing,           // 合成中 - 盖子关闭，正在运行
        Complete_LidOpen,     // 合成完成 - 盖子打开，展示结果
        ResultCollecting      // 正在收集结果
    }

    // ============ 基本配置 ============
    [Header("基本信息")]
    [Tooltip("物体唯一ID")]
    public string objectID = "synthesis_machine";

    [Tooltip("显示名称")]
    public string displayName = "合成机器";

    // ============ 组件引用 ============
    [Header("组件引用")]
    [Tooltip("机器主体（用于点击打开盖子和放入陶罐）")]
    public GameObject machineBody;

    [Tooltip("机器盖子物体")]
    public GameObject lidObject;

    [Tooltip("机器按钮物体")]
    public GameObject buttonObject;

    [Tooltip("陶罐显示位置")]
    public Transform potDisplayPosition;

    [Tooltip("水晶碎片显示位置")]
    public Transform shardDisplayPosition;

    [Header("精灵渲染器")]
    [Tooltip("机器主体的 SpriteRenderer")]
    public SpriteRenderer machineRenderer;

    [Tooltip("盖子的 SpriteRenderer")]
    public SpriteRenderer lidRenderer;

    [Tooltip("按钮的 SpriteRenderer")]
    public SpriteRenderer buttonRenderer;

    [Tooltip("机器内展示的陶罐 SpriteRenderer")]
    public SpriteRenderer displayPotRenderer;

    [Tooltip("机器内展示的水晶碎片 SpriteRenderer")]
    public SpriteRenderer displayShardRenderer;

    // ============ 精灵图配置 ============
    [Header("盖子精灵图")]
    [Tooltip("盖子关闭的精灵")]
    public Sprite lidClosedSprite;

    [Tooltip("盖子打开的精灵")]
    public Sprite lidOpenSprite;

    [Header("按钮精灵图")]
    [Tooltip("按钮正常状态精灵")]
    public Sprite buttonNormalSprite;

    [Tooltip("按钮按下状态精灵")]
    public Sprite buttonPressedSprite;

    [Tooltip("按钮激活/运行中精灵")]
    public Sprite buttonActiveSprite;

    [Tooltip("按钮禁用状态精灵")]
    public Sprite buttonDisabledSprite;

    [Header("陶罐精灵图")]
    [Tooltip("装满的陶罐精灵")]
    public Sprite potFilledSprite;

    [Tooltip("空陶罐精灵")]
    public Sprite potEmptySprite;

    // ============ 物品配置 ============
    [Header("物品配置")]
    [Tooltip("装满的陶罐物品数据")]
    public ItemData filledPotItem;

    [Tooltip("空陶罐物品数据")]
    public ItemData emptyPotItem;

    [Tooltip("场景中的陶罐控制器引用")]
    public PotController potController;

    // ============ 合成配置 ============
    [Header("合成配置")]
    [Tooltip("合成所需时间（秒）")]
    public float synthesisTime = 2f;

    // ============ 视觉反馈 - 颜色 ============
    [Header("视觉反馈 - 颜色")]
    [Tooltip("正常颜色")]
    public Color normalColor = Color.white;

    [Tooltip("鼠标悬停颜色")]
    public Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

    [Tooltip("可交互提示颜色（如按钮可用时）")]
    public Color interactableColor = new Color(0.8f, 1f, 0.8f, 1f);

    [Tooltip("合成中的闪烁颜色")]
    public Color processingColor = new Color(0.6f, 1f, 0.6f, 1f);

    [Tooltip("可拾取物品高亮颜色")]
    public Color pickupHighlightColor = new Color(1f, 1f, 0.7f, 1f);

    [Header("视觉反馈 - 过渡设置")]
    [Tooltip("颜色过渡时间（秒）")]
    public float colorTransitionDuration = 0.15f;

    [Tooltip("合成中闪烁速度")]
    public float processingFlashSpeed = 5f;

    [Tooltip("结果出现时的缩放动画")]
    public bool enableResultScaleAnimation = true;

    [Tooltip("缩放动画持续时间")]
    public float scaleAnimationDuration = 0.3f;

    // ============ 提示文本 ============
    [Header("提示文本")]
    public string noItemHint = "需要选中装满的陶罐";
    public string wrongItemHint = "需要放入装满的陶罐";
    public string lidNotOpenHint = "需要先打开盖子";
    public string noPotHint = "需要先放入陶罐";
    public string machineRunningHint = "机器正在运行...";
    public string closeLidFirstHint = "请先关闭盖子";
    public string collectItemsFirstHint = "请先取出物品";

    // ============ 音效 ============
    [Header("音效设置")]
    public string lidOpenSFX = "Audio/SFX/lid_open";
    public string lidCloseSFX = "Audio/SFX/lid_close";
    public string potInsertSFX = "Audio/SFX/pot_insert";
    public string buttonPressSFX = "Audio/SFX/button_press";
    [Tooltip("合成中的震动/运行音效（循环播放）")]
    public string processingLoopSFX = "Audio/SFX/machine_vibrate";
    public string completeSFX = "Audio/SFX/synthesis_complete";
    public string pickupSFX = "Audio/SFX/item_pickup";
    public string errorSFX = "Audio/SFX/error";

    // ============ 事件 ============
    [Header("事件")]
    public UnityEvent OnLidOpened;
    public UnityEvent OnLidClosed;
    public UnityEvent OnPotInserted;
    public UnityEvent OnSynthesisStarted;
    public UnityEvent OnSynthesisCompleted;
    public UnityEvent OnPotCollected;
    public UnityEvent OnShardCollected;
    public UnityEvent OnAllShardsCollected;

    // ============ 运行时数据 ============
    [Header("调试信息（只读）")]
    [SerializeField] private MachineState currentState = MachineState.Idle_LidClosed;
    [SerializeField] private PotRecipe currentRecipe = null;
    [SerializeField] private bool potCollected = false;
    [SerializeField] private bool shardCollected = false;
    [SerializeField] private int totalShardsCollected = 0;

    // 缓存
    private Coroutine processingCoroutine;
    private Coroutine machineColorCoroutine;
    private Coroutine lidColorCoroutine;
    private Coroutine buttonColorCoroutine;
    private Coroutine potColorCoroutine;
    private Coroutine shardColorCoroutine;

    private bool isAnimating = false;
    private bool isMachineHovering = false;
    private bool isLidHovering = false;
    private bool isButtonHovering = false;
    private bool isPotHovering = false;
    private bool isShardHovering = false;

    // 震动音效相关
    private AudioSource processingAudioSource;

    // ============ 属性 ============
    public MachineState CurrentState => currentState;
    public int TotalShardsCollected => totalShardsCollected;

    // ============ Unity 生命周期 ============
    private void Start()
    {
        InitializeVisuals();
    }

    private void Update()
    {
        // 合成中的按钮闪烁效果
        if (currentState == MachineState.Processing && buttonRenderer != null)
        {
            float flash = (Mathf.Sin(Time.time * processingFlashSpeed) + 1f) / 2f;
            buttonRenderer.color = Color.Lerp(normalColor, processingColor, flash);
        }
    }

    // ============ 初始化 ============
    private void InitializeVisuals()
    {
        UpdateLidVisual(false);
        UpdateButtonVisual();

        // 隐藏展示区域的物品
        if (displayPotRenderer != null) displayPotRenderer.enabled = false;
        if (displayShardRenderer != null) displayShardRenderer.enabled = false;

        // 设置初始颜色
        SetRendererColor(machineRenderer, normalColor);
        SetRendererColor(lidRenderer, normalColor);
        SetRendererColor(buttonRenderer, normalColor);
    }

    // ============ 颜色过渡系统 ============

    /// <summary>
    /// 平滑过渡渲染器颜色
    /// </summary>
    private Coroutine TransitionColor(SpriteRenderer renderer, Color targetColor, ref Coroutine existingCoroutine)
    {
        if (renderer == null) return null;

        if (existingCoroutine != null)
        {
            StopCoroutine(existingCoroutine);
        }

        existingCoroutine = StartCoroutine(ColorTransitionCoroutine(renderer, targetColor));
        return existingCoroutine;
    }

    private IEnumerator ColorTransitionCoroutine(SpriteRenderer renderer, Color targetColor)
    {
        if (renderer == null) yield break;

        Color startColor = renderer.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = SmoothStep(elapsed / colorTransitionDuration);
            renderer.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        renderer.color = targetColor;
    }

    private void SetRendererColor(SpriteRenderer renderer, Color color)
    {
        if (renderer != null)
        {
            renderer.color = color;
        }
    }

    /// <summary>
    /// 平滑步进函数
    /// </summary>
    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    // ============ 鼠标事件处理 ============

    // --- 机器主体 ---
    public void OnMachineMouseEnter()
    {
        isMachineHovering = true;
        if (CanInteractWithMachine())
        {
            TransitionColor(machineRenderer, hoverColor, ref machineColorCoroutine);
        }
    }

    public void OnMachineMouseExit()
    {
        isMachineHovering = false;
        TransitionColor(machineRenderer, normalColor, ref machineColorCoroutine);
    }

    public void OnMachineClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 机器被点击，当前状态: {currentState}");

        switch (currentState)
        {
            case MachineState.Idle_LidClosed:
                // ⭐ 如果选中了陶罐，打开盖子后自动放入
                if (HasSelectedFilledPot())
                {
                    OpenLid();
                    currentState = MachineState.Idle_LidOpen;
                    // 立即放入陶罐
                    InsertPot();
                }
                else
                {
                    // 没有选中陶罐，只打开盖子
                    OpenLid();
                    currentState = MachineState.Idle_LidOpen;
                }
                break;

            case MachineState.Idle_LidOpen:
                // ⭐ 优先检查是否要放入陶罐
                if (HasSelectedFilledPot())
                {
                    InsertPot();
                }
                else
                {
                    // 没有选中陶罐，关闭盖子
                    CloseLid();
                    currentState = MachineState.Idle_LidClosed;
                }
                break;

            case MachineState.PotInserted_LidOpen:
                // 已有陶罐，提示关闭盖子
                ShowHint(closeLidFirstHint);
                break;

            case MachineState.Processing:
                ShowHint(machineRunningHint);
                break;

            case MachineState.Complete_LidOpen:
            case MachineState.ResultCollecting:
                ShowHint(collectItemsFirstHint);
                break;

            default:
                break;
        }

        SaveLoadSystem.Instance?.SaveGame();
    }

    // --- 盖子 ---
    public void OnLidMouseEnter()
    {
        isLidHovering = true;
        if (CanInteractWithLid())
        {
            TransitionColor(lidRenderer, hoverColor, ref lidColorCoroutine);
        }
    }

    public void OnLidMouseExit()
    {
        isLidHovering = false;
        TransitionColor(lidRenderer, normalColor, ref lidColorCoroutine);
    }

    public void OnLidClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 盖子被点击，当前状态: {currentState}");

        switch (currentState)
        {
            case MachineState.Idle_LidClosed:
                // ⭐ 如果选中了陶罐，打开盖子后自动放入
                if (HasSelectedFilledPot())
                {
                    OpenLid();
                    currentState = MachineState.Idle_LidOpen;
                    InsertPot();
                }
                else
                {
                    OpenLid();
                    currentState = MachineState.Idle_LidOpen;
                }
                break;

            case MachineState.Idle_LidOpen:
                // ⭐ 优先检查是否要放入陶罐
                if (HasSelectedFilledPot())
                {
                    InsertPot();
                }
                else
                {
                    CloseLid();
                    currentState = MachineState.Idle_LidClosed;
                }
                break;

            case MachineState.PotInserted_LidOpen:
                CloseLid();
                currentState = MachineState.PotInserted_LidClosed;
                UpdateButtonVisual();
                break;

            case MachineState.PotInserted_LidClosed:
                OpenLid();
                currentState = MachineState.PotInserted_LidOpen;
                UpdateButtonVisual();
                break;

            case MachineState.Processing:
                ShowHint(machineRunningHint);
                break;

            case MachineState.Complete_LidOpen:
            case MachineState.ResultCollecting:
                ShowHint(collectItemsFirstHint);
                break;
        }

        SaveLoadSystem.Instance?.SaveGame();
    }

    // --- 按钮 ---
    public void OnButtonMouseEnter()
    {
        isButtonHovering = true;
        if (currentState == MachineState.PotInserted_LidClosed)
        {
            TransitionColor(buttonRenderer, hoverColor, ref buttonColorCoroutine);
        }
    }

    public void OnButtonMouseExit()
    {
        isButtonHovering = false;
        if (currentState == MachineState.PotInserted_LidClosed)
        {
            TransitionColor(buttonRenderer, interactableColor, ref buttonColorCoroutine);
        }
        else if (currentState != MachineState.Processing)
        {
            TransitionColor(buttonRenderer, normalColor, ref buttonColorCoroutine);
        }
    }

    public void OnButtonClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 按钮被点击，当前状态: {currentState}");

        switch (currentState)
        {
            case MachineState.PotInserted_LidClosed:
                StartSynthesis();
                break;

            case MachineState.Idle_LidClosed:
            case MachineState.Idle_LidOpen:
                ShowHint(noPotHint);
                PlaySFX(errorSFX);
                break;

            case MachineState.PotInserted_LidOpen:
                ShowHint(closeLidFirstHint);
                PlaySFX(errorSFX);
                break;

            case MachineState.Processing:
                ShowHint(machineRunningHint);
                break;

            default:
                break;
        }
    }

    // --- 展示的陶罐 ---
    public void OnDisplayPotMouseEnter()
    {
        isPotHovering = true;
        if (CanCollectPot())
        {
            TransitionColor(displayPotRenderer, pickupHighlightColor, ref potColorCoroutine);
        }
    }

    public void OnDisplayPotMouseExit()
    {
        isPotHovering = false;
        TransitionColor(displayPotRenderer, normalColor, ref potColorCoroutine);
    }

    public void OnDisplayPotClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 展示的陶罐被点击");

        if (CanCollectPot())
        {
            CollectPot();
        }
    }

    // --- 展示的水晶碎片 ---
    public void OnDisplayShardMouseEnter()
    {
        isShardHovering = true;
        if (CanCollectShard())
        {
            TransitionColor(displayShardRenderer, pickupHighlightColor, ref shardColorCoroutine);
        }
    }

    public void OnDisplayShardMouseExit()
    {
        isShardHovering = false;
        TransitionColor(displayShardRenderer, normalColor, ref shardColorCoroutine);
    }

    public void OnDisplayShardClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 展示的水晶碎片被点击");

        if (CanCollectShard())
        {
            CollectShard();
        }
    }

    // ============ 状态检查 ============

    private bool CanInteractWithMachine()
    {
        return currentState == MachineState.Idle_LidClosed ||
               currentState == MachineState.Idle_LidOpen;
    }

    private bool CanInteractWithLid()
    {
        return currentState != MachineState.Processing &&
               currentState != MachineState.Complete_LidOpen &&
               currentState != MachineState.ResultCollecting;
    }

    private bool CanCollectPot()
    {
        return (currentState == MachineState.Complete_LidOpen ||
                currentState == MachineState.ResultCollecting) && !potCollected;
    }

    private bool CanCollectShard()
    {
        return (currentState == MachineState.Complete_LidOpen ||
                currentState == MachineState.ResultCollecting) && !shardCollected;
    }

    /// <summary>
    /// 检查是否选中了装满的陶罐
    /// </summary>
    private bool HasSelectedFilledPot()
    {
        if (UIManager.Instance == null) return false;
        if (filledPotItem == null) return false;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        return selectedItem != null && selectedItem.itemID == filledPotItem.itemID;
    }

    // ============ 核心逻辑 ============

    private void OpenLid()
    {
        Debug.Log("[SynthesisMachine] 打开盖子");

        UpdateLidVisual(true);
        PlaySFX(lidOpenSFX);

        OnLidOpened?.Invoke();
    }

    private void CloseLid()
    {
        Debug.Log("[SynthesisMachine] 关闭盖子");

        UpdateLidVisual(false);
        PlaySFX(lidCloseSFX);

        OnLidClosed?.Invoke();
    }

    private void InsertPot()
    {
        Debug.Log("[SynthesisMachine] 放入陶罐");

        // 消耗背包中的陶罐
        UIManager.Instance.ConsumeSelectedItem();

        // 获取陶罐的配方信息
        if (potController != null)
        {
            currentRecipe = potController.MatchedRecipe;
        }

        // 显示陶罐
        ShowDisplayPot(true, potFilledSprite);

        // 更新状态
        currentState = MachineState.PotInserted_LidOpen;

        // 播放音效
        PlaySFX(potInsertSFX);

        // 触发事件
        OnPotInserted?.Invoke();

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void StartSynthesis()
    {
        Debug.Log("[SynthesisMachine] 开始合成");

        // 按钮按下效果
        StartCoroutine(ButtonPressAnimation());

        // 播放按钮音效
        PlaySFX(buttonPressSFX);

        // 更新状态
        currentState = MachineState.Processing;

        // 更新按钮视觉
        UpdateButtonVisual();

        // 触发事件
        OnSynthesisStarted?.Invoke();

        // 启动合成协程
        processingCoroutine = StartCoroutine(SynthesisProcess());
    }

    private IEnumerator SynthesisProcess()
    {
        Debug.Log($"[SynthesisMachine] 合成进行中... ({synthesisTime}秒)");

        // 开始播放循环震动音效
        StartProcessingSound();

        // 等待合成时间
        yield return new WaitForSeconds(synthesisTime);

        // 停止震动音效
        StopProcessingSound();

        // 合成完成
        CompleteSynthesis();
    }

    private void CompleteSynthesis()
    {
        Debug.Log("[SynthesisMachine] 合成完成！");

        // 播放完成音效
        PlaySFX(completeSFX);

        // 更新状态
        currentState = MachineState.Complete_LidOpen;
        potCollected = false;
        shardCollected = false;

        // 打开盖子并播放开盖音效
        UpdateLidVisual(true);
        PlaySFX(lidOpenSFX);

        // 标记配方完成
        if (potController != null)
        {
            potController.MarkRecipeCompleted();
        }

        // 显示结果
        StartCoroutine(ShowResultsAnimation());

        // 更新按钮视觉
        UpdateButtonVisual();

        // 触发事件
        OnSynthesisCompleted?.Invoke();

        SaveLoadSystem.Instance?.SaveGame();
    }

    private IEnumerator ShowResultsAnimation()
    {
        isAnimating = true;

        // 先将陶罐图片更换为空陶罐
        ShowDisplayPot(true, potEmptySprite);

        // 显示水晶碎片
        if (currentRecipe != null && currentRecipe.resultShard != null)
        {
            Sprite shardSprite = currentRecipe.shardDisplaySprite;
            if (shardSprite == null)
            {
                shardSprite = currentRecipe.resultShard.icon;
            }
            ShowDisplayShard(true, shardSprite);
        }

        // 缩放动画
        if (enableResultScaleAnimation)
        {
            // 同时播放两个缩放动画
            StartCoroutine(ScaleAnimation(displayPotRenderer?.transform));
            yield return StartCoroutine(ScaleAnimation(displayShardRenderer?.transform));
        }

        isAnimating = false;
    }

    private IEnumerator ScaleAnimation(Transform target)
    {
        if (target == null) yield break;

        Vector3 originalScale = target.localScale;
        target.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < scaleAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / scaleAnimationDuration;
            // 弹性缓动 (EaseOutBack)
            t = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            t = Mathf.Clamp01(t);
            target.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    private void CollectPot()
    {
        Debug.Log("[SynthesisMachine] 拾取空陶罐");

        if (emptyPotItem == null)
        {
            Debug.LogError("[SynthesisMachine] emptyPotItem 未设置！");
            return;
        }

        // 添加到背包
        bool added = InventorySystem.Instance.AddItem(emptyPotItem);
        if (!added)
        {
            ShowHint("背包已满");
            return;
        }

        // 隐藏展示的陶罐
        ShowDisplayPot(false, null);

        // 播放音效
        PlaySFX(pickupSFX);

        // 标记已拾取
        potCollected = true;

        // 触发事件
        OnPotCollected?.Invoke();

        // 检查是否全部收集完成
        CheckAllCollected();

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void CollectShard()
    {
        Debug.Log("[SynthesisMachine] 拾取水晶碎片");

        if (currentRecipe == null || currentRecipe.resultShard == null)
        {
            Debug.LogError("[SynthesisMachine] 当前配方或结果碎片为空！");
            return;
        }

        // 添加到背包
        bool added = InventorySystem.Instance.AddItem(currentRecipe.resultShard);
        if (!added)
        {
            ShowHint("背包已满");
            return;
        }

        // 隐藏展示的碎片
        ShowDisplayShard(false, null);

        // 播放音效
        PlaySFX(pickupSFX);

        // 标记已拾取
        shardCollected = true;
        totalShardsCollected++;

        Debug.Log($"[SynthesisMachine] 已收集水晶碎片数量: {totalShardsCollected}");

        // 触发事件
        OnShardCollected?.Invoke();

        // 检查是否收集了所有碎片
        if (potController != null && potController.AreAllRecipesCompleted())
        {
            Debug.Log("[SynthesisMachine] ★ 所有水晶碎片已收集！");
            OnAllShardsCollected?.Invoke();
        }

        // 检查是否全部收集完成
        CheckAllCollected();

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void CheckAllCollected()
    {
        if (potCollected && shardCollected)
        {
            Debug.Log("[SynthesisMachine] 所有物品已收集，重置机器");

            // 重置状态
            currentState = MachineState.Idle_LidOpen;
            currentRecipe = null;

            // 更新按钮
            UpdateButtonVisual();
        }
        else
        {
            currentState = MachineState.ResultCollecting;
        }
    }

    // ============ 音效系统 ============

    private void StartProcessingSound()
    {
        if (string.IsNullOrEmpty(processingLoopSFX)) return;

        // 如果 AudioManager 支持循环播放
        if (AudioManager.Instance != null)
        {
            // 创建临时音源用于循环播放
            if (processingAudioSource == null)
            {
                processingAudioSource = gameObject.AddComponent<AudioSource>();
                processingAudioSource.loop = true;
            }

            // 尝试从 Resources 加载音频
            AudioClip clip = Resources.Load<AudioClip>(processingLoopSFX);
            if (clip != null)
            {
                processingAudioSource.clip = clip;
                processingAudioSource.Play();
                Debug.Log("[SynthesisMachine] 开始播放震动音效");
            }
            else
            {
                // 如果找不到，尝试用 AudioManager 播放一次
                AudioManager.Instance.PlaySFX(processingLoopSFX);
            }
        }
    }

    private void StopProcessingSound()
    {
        if (processingAudioSource != null && processingAudioSource.isPlaying)
        {
            processingAudioSource.Stop();
            Debug.Log("[SynthesisMachine] 停止震动音效");
        }
    }

    // ============ 视觉更新 ============

    private void UpdateLidVisual(bool isOpen)
    {
        if (lidRenderer != null)
        {
            lidRenderer.sprite = isOpen ? lidOpenSprite : lidClosedSprite;
        }
    }

    private void UpdateButtonVisual()
    {
        if (buttonRenderer == null) return;

        switch (currentState)
        {
            case MachineState.PotInserted_LidClosed:
                // 可以启动 - 显示为可交互状态
                buttonRenderer.sprite = buttonNormalSprite;
                TransitionColor(buttonRenderer, interactableColor, ref buttonColorCoroutine);
                break;

            case MachineState.Processing:
                // 运行中
                buttonRenderer.sprite = buttonActiveSprite ?? buttonPressedSprite;
                // 颜色在 Update 中闪烁
                break;

            default:
                // 禁用或正常状态
                buttonRenderer.sprite = buttonDisabledSprite ?? buttonNormalSprite;
                TransitionColor(buttonRenderer, normalColor, ref buttonColorCoroutine);
                break;
        }
    }

    private void ShowDisplayPot(bool show, Sprite sprite)
    {
        if (displayPotRenderer != null)
        {
            displayPotRenderer.enabled = show;
            displayPotRenderer.color = normalColor;
            if (sprite != null)
            {
                displayPotRenderer.sprite = sprite;
            }
        }
    }

    private void ShowDisplayShard(bool show, Sprite sprite)
    {
        if (displayShardRenderer != null)
        {
            displayShardRenderer.enabled = show;
            displayShardRenderer.color = normalColor;
            if (sprite != null)
            {
                displayShardRenderer.sprite = sprite;
            }
        }
    }

    private IEnumerator ButtonPressAnimation()
    {
        if (buttonRenderer == null) yield break;

        Sprite originalSprite = buttonRenderer.sprite;
        buttonRenderer.sprite = buttonPressedSprite ?? originalSprite;

        yield return new WaitForSeconds(0.1f);

        // 状态会变为 Processing，所以不需要恢复
    }

    // ============ 辅助方法 ============

    private void PlaySFX(string sfxName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxName))
        {
            AudioManager.Instance.PlaySFX(sfxName);
        }
    }

    private void ShowHint(string hint)
    {
        Debug.Log($"[提示] {hint}");
        // 如果有提示系统：
        // HintSystem.Instance?.ShowHint(hint);
    }

    private void OnDestroy()
    {
        // 清理音源
        StopProcessingSound();
        if (processingAudioSource != null)
        {
            Destroy(processingAudioSource);
        }
    }

    // ============ 存档/读档 ============

    [System.Serializable]
    public class MachineSaveData
    {
        public string objectID;
        public int currentState;
        public string currentRecipeID;
        public bool potCollected;
        public bool shardCollected;
        public int totalShardsCollected;
    }

    public MachineSaveData GetSaveData()
    {
        return new MachineSaveData
        {
            objectID = this.objectID,
            currentState = (int)this.currentState,
            currentRecipeID = currentRecipe?.recipeID ?? "",
            potCollected = this.potCollected,
            shardCollected = this.shardCollected,
            totalShardsCollected = this.totalShardsCollected
        };
    }

    public void LoadSaveData(MachineSaveData data, PotRecipe[] availableRecipes)
    {
        if (data == null) return;

        this.currentState = (MachineState)data.currentState;
        this.potCollected = data.potCollected;
        this.shardCollected = data.shardCollected;
        this.totalShardsCollected = data.totalShardsCollected;

        // 恢复配方
        this.currentRecipe = null;
        if (!string.IsNullOrEmpty(data.currentRecipeID) && availableRecipes != null)
        {
            foreach (var recipe in availableRecipes)
            {
                if (recipe != null && recipe.recipeID == data.currentRecipeID)
                {
                    this.currentRecipe = recipe;
                    break;
                }
            }
        }

        // 恢复视觉状态
        RestoreVisualState();

        Debug.Log($"[SynthesisMachine] 加载存档: state={currentState}, shards={totalShardsCollected}");
    }

    private void RestoreVisualState()
    {
        bool isLidOpen = currentState == MachineState.Idle_LidOpen ||
                         currentState == MachineState.PotInserted_LidOpen ||
                         currentState == MachineState.Complete_LidOpen ||
                         currentState == MachineState.ResultCollecting;

        UpdateLidVisual(isLidOpen);
        UpdateButtonVisual();

        // 重置所有颜色
        SetRendererColor(machineRenderer, normalColor);
        SetRendererColor(lidRenderer, normalColor);

        // 恢复展示物品
        switch (currentState)
        {
            case MachineState.PotInserted_LidOpen:
            case MachineState.PotInserted_LidClosed:
                ShowDisplayPot(true, potFilledSprite);
                ShowDisplayShard(false, null);
                break;

            case MachineState.Complete_LidOpen:
            case MachineState.ResultCollecting:
                if (!potCollected)
                {
                    ShowDisplayPot(true, potEmptySprite);
                }
                else
                {
                    ShowDisplayPot(false, null);
                }

                if (!shardCollected && currentRecipe != null)
                {
                    Sprite shardSprite = currentRecipe.shardDisplaySprite ?? currentRecipe.resultShard?.icon;
                    ShowDisplayShard(true, shardSprite);
                }
                else
                {
                    ShowDisplayShard(false, null);
                }
                break;

            default:
                ShowDisplayPot(false, null);
                ShowDisplayShard(false, null);
                break;
        }
    }
}