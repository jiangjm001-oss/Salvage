// Assets/Scripts/GamePlay/Synthesis/SynthesisMachine.cs
// 合成机器控制器 - 管理陶罐合成过程
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 合成机器控制器
/// 负责管理机器的状态、接收陶罐、执行合成、展示结果
/// 
/// 使用流程：
/// 1. 玩家点击机器盖子 → 打开盖子
/// 2. 玩家选中装满的陶罐，点击机器内部 → 放入陶罐
/// 3. 玩家点击盖子 → 关闭盖子
/// 4. 玩家点击按钮 → 启动合成
/// 5. 2秒后盖子自动打开 → 展示空陶罐和水晶碎片
/// 6. 玩家点击陶罐 → 拾取空陶罐
/// 7. 玩家点击水晶碎片 → 拾取碎片
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
    [Tooltip("机器盖子物体")]
    public GameObject lidObject;

    [Tooltip("机器按钮物体")]
    public GameObject buttonObject;

    [Tooltip("机器内部区域（用于放置陶罐）")]
    public GameObject machineInterior;

    [Tooltip("陶罐显示位置")]
    public Transform potDisplayPosition;

    [Tooltip("水晶碎片显示位置")]
    public Transform shardDisplayPosition;

    [Header("精灵渲染器")]
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

    // ============ 视觉反馈 ============
    [Header("视觉反馈")]
    [Tooltip("按钮高亮颜色")]
    public Color buttonHighlightColor = new Color(1f, 1f, 0.8f, 1f);

    [Tooltip("可拾取物品高亮颜色")]
    public Color pickupHighlightColor = new Color(1f, 1f, 0.7f, 1f);

    [Tooltip("合成中的闪烁颜色")]
    public Color processingFlashColor = new Color(0.5f, 1f, 0.5f, 1f);

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

    // ============ 音效 ============
    [Header("音效设置")]
    public string lidOpenSFX = "Audio/SFX/lid_open";
    public string lidCloseSFX = "Audio/SFX/lid_close";
    public string potInsertSFX = "Audio/SFX/pot_insert";
    public string buttonPressSFX = "Audio/SFX/button_press";
    public string processingSFX = "Audio/SFX/machine_running";
    public string completeSFX = "Audio/SFX/synthesis_complete";
    public string pickupSFX = "Audio/SFX/item_pickup";

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
    private bool isAnimating = false;

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
        // 合成中的视觉效果
        if (currentState == MachineState.Processing && buttonRenderer != null)
        {
            float flash = (Mathf.Sin(Time.time * processingFlashSpeed) + 1f) / 2f;
            buttonRenderer.color = Color.Lerp(Color.white, processingFlashColor, flash);
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
    }

    // ============ 交互入口 ============

    /// <summary>
    /// 盖子被点击
    /// </summary>
    public void OnLidClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 盖子被点击，当前状态: {currentState}");

        switch (currentState)
        {
            case MachineState.Idle_LidClosed:
                OpenLid();
                currentState = MachineState.Idle_LidOpen;
                break;

            case MachineState.Idle_LidOpen:
                CloseLid();
                currentState = MachineState.Idle_LidClosed;
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
                // 合成完成后不允许关闭盖子，直到收集完物品
                ShowHint("请先取出物品");
                break;
        }

        SaveLoadSystem.Instance?.SaveGame();
    }

    /// <summary>
    /// 机器内部被点击（用于放入陶罐）
    /// </summary>
    public void OnInteriorClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 机器内部被点击，当前状态: {currentState}");

        switch (currentState)
        {
            case MachineState.Idle_LidOpen:
                TryInsertPot();
                break;

            case MachineState.Complete_LidOpen:
            case MachineState.ResultCollecting:
                // 点击内部区域不做任何事，玩家需要点击具体的陶罐或碎片
                break;

            default:
                if (currentState == MachineState.Idle_LidClosed ||
                    currentState == MachineState.PotInserted_LidClosed)
                {
                    ShowHint(lidNotOpenHint);
                }
                break;
        }
    }

    /// <summary>
    /// 按钮被点击
    /// </summary>
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
                break;

            case MachineState.PotInserted_LidOpen:
                ShowHint("请先关闭盖子");
                break;

            case MachineState.Processing:
                ShowHint(machineRunningHint);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 展示的陶罐被点击（用于拾取）
    /// </summary>
    public void OnDisplayPotClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 展示的陶罐被点击，状态: {currentState}, potCollected: {potCollected}");

        if ((currentState == MachineState.Complete_LidOpen || currentState == MachineState.ResultCollecting)
            && !potCollected)
        {
            CollectPot();
        }
    }

    /// <summary>
    /// 展示的水晶碎片被点击（用于拾取）
    /// </summary>
    public void OnDisplayShardClicked()
    {
        if (isAnimating) return;

        Debug.Log($"[SynthesisMachine] 展示的水晶碎片被点击，状态: {currentState}, shardCollected: {shardCollected}");

        if ((currentState == MachineState.Complete_LidOpen || currentState == MachineState.ResultCollecting)
            && !shardCollected)
        {
            CollectShard();
        }
    }

    // ============ 核心逻辑 ============

    private void OpenLid()
    {
        Debug.Log("[SynthesisMachine] 打开盖子");

        UpdateLidVisual(true);
        PlaySFX(lidOpenSFX);

        // 显示机器内部
        if (machineInterior != null)
        {
            machineInterior.SetActive(true);
        }

        OnLidOpened?.Invoke();
    }

    private void CloseLid()
    {
        Debug.Log("[SynthesisMachine] 关闭盖子");

        UpdateLidVisual(false);
        PlaySFX(lidCloseSFX);

        // 隐藏机器内部（如果没有陶罐）
        if (machineInterior != null && currentState == MachineState.Idle_LidOpen)
        {
            machineInterior.SetActive(false);
        }

        OnLidClosed?.Invoke();
    }

    private void TryInsertPot()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[SynthesisMachine] UIManager.Instance 为空！");
            return;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        if (selectedItem == null)
        {
            ShowHint(noItemHint);
            return;
        }

        // 检查是否是装满的陶罐
        if (filledPotItem == null || selectedItem.itemID != filledPotItem.itemID)
        {
            ShowHint(wrongItemHint);
            return;
        }

        InsertPot();
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

        // 播放音效
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

        // 播放处理音效
        PlaySFX(processingSFX);

        // 等待合成时间
        yield return new WaitForSeconds(synthesisTime);

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

        // 打开盖子
        UpdateLidVisual(true);

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

        // 显示空陶罐
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
            yield return StartCoroutine(ScaleAnimation(displayPotRenderer?.transform));
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
            // 弹性缓动
            t = 1f - Mathf.Pow(1f - t, 3f);
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

    // ============ 视觉更新 ============

    private void UpdateLidVisual(bool isOpen)
    {
        if (lidRenderer != null)
        {
            lidRenderer.sprite = isOpen ? lidOpenSprite : lidClosedSprite;
        }

        // 也可以通过激活/禁用不同的物体来实现
        // if (lidOpenObject != null) lidOpenObject.SetActive(isOpen);
        // if (lidClosedObject != null) lidClosedObject.SetActive(!isOpen);
    }

    private void UpdateButtonVisual()
    {
        if (buttonRenderer == null) return;

        switch (currentState)
        {
            case MachineState.PotInserted_LidClosed:
                // 可以启动
                buttonRenderer.sprite = buttonNormalSprite;
                buttonRenderer.color = buttonHighlightColor;
                break;

            case MachineState.Processing:
                // 运行中
                buttonRenderer.sprite = buttonActiveSprite ?? buttonPressedSprite;
                // 颜色在 Update 中闪烁
                break;

            default:
                // 禁用或正常状态
                buttonRenderer.sprite = buttonDisabledSprite ?? buttonNormalSprite;
                buttonRenderer.color = Color.white;
                break;
        }
    }

    private void ShowDisplayPot(bool show, Sprite sprite)
    {
        if (displayPotRenderer != null)
        {
            displayPotRenderer.enabled = show;
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

        // 不恢复，因为状态会变为 Processing
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
                if (!shardCollected && currentRecipe != null)
                {
                    Sprite shardSprite = currentRecipe.shardDisplaySprite ?? currentRecipe.resultShard?.icon;
                    ShowDisplayShard(true, shardSprite);
                }
                break;

            default:
                ShowDisplayPot(false, null);
                ShowDisplayShard(false, null);
                break;
        }

        if (machineInterior != null)
        {
            machineInterior.SetActive(isLidOpen);
        }
    }
}