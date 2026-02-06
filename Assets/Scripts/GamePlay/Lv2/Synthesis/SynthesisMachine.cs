// Assets/Scripts/GamePlay/Lv2/Synthesis/SynthesisMachine.cs
// 合成机器控制器 - 优化版
// 简化状态管理,移除视觉效果,使用Object中的Sprite

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 合成机器控制器
/// 
/// 使用流程:
/// 1. 点击机器 → 进入放大视图,盖子打开状态
/// 2. 选中装满的陶罐,点击机器 → 放入陶罐(陶罐显示在画面中)
/// 3. 点击机器 → 切换为关闭盖子
/// 4. 点击按钮 → 启动机器,播放震动音效
/// 5. 2秒后盖子自动打开 → 播放开盖音效,停止震动音效
/// 6. 展示水晶碎片,空陶罐在拾取位置自动刷新
/// 7. 点击水晶碎片拾取 → 四次对应四个不同水晶碎片
/// 
/// 陶罐使用四次,顺序不固定
/// </summary>
public class SynthesisMachine : MonoBehaviour
{
    // ============ 状态枚举 ============
    public enum MachineState
    {
        Idle,                    // 空闲 - 盖子关闭
        LidOpen_Empty,           // 盖子打开 - 等待放入陶罐
        LidOpen_PotInserted,     // 盖子打开 - 已放入陶罐
        LidClosed_Ready,         // 盖子关闭 - 已放入陶罐,等待启动
        Processing,              // 合成中
        Complete                 // 完成 - 展示结果
    }

    // ============ 基本配置 ============
    [Header("基本信息")]
    [Tooltip("物体唯一ID")]
    public string objectID = "synthesis_machine";

    [Tooltip("显示名称")]
    public string displayName = "合成机器";

    // ============ 组件引用 ============
    [Header("机器状态物体")]
    [Tooltip("机器关闭状态物体(盖子关闭的完整机器)")]
    public GameObject machineClosedObject;

    [Tooltip("机器打开状态物体(盖子打开的完整机器)")]
    public GameObject machineOpenObject;

    [Header("按钮物体")]
    [Tooltip("按钮未按下状态物体")]
    public GameObject buttonNormalObject;

    [Tooltip("按钮按下状态物体")]
    public GameObject buttonPressedObject;

    [Header("展示物体")]
    [Tooltip("陶罐显示位置的GameObject(包含SpriteRenderer)")]
    public GameObject potDisplayObject;

    [Tooltip("水晶碎片显示位置的GameObject(包含SpriteRenderer)")]
    public GameObject shardDisplayObject;

    // ============ Sprite Renderer 引用 ============
    [Header("Sprite Renderer")]
    [Tooltip("陶罐展示的SpriteRenderer")]
    public SpriteRenderer potSpriteRenderer;

    [Tooltip("水晶碎片展示的SpriteRenderer")]
    public SpriteRenderer shardSpriteRenderer;

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
    [Tooltip("合成所需时间(秒)")]
    public float synthesisTime = 2f;

    // ============ 提示文本 ============
    [Header("提示文本")]
    public string needFilledPotHint = "需要选中装满的陶罐";
    public string needCloseLidHint = "请先关闭盖子";
    public string needPotHint = "需要先放入陶罐";
    public string machineRunningHint = "机器正在运行...";
    public string collectItemsFirstHint = "请先取出物品";

    // ============ 音效 ============
    [Header("音效设置")]
    public string lidOpenSFX = "Audio/SFX/lid_open";
    public string lidCloseSFX = "Audio/SFX/lid_close";
    public string potInsertSFX = "Audio/SFX/pot_insert";
    public string buttonPressSFX = "Audio/SFX/button_press";
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
    [Header("调试信息(只读)")]
    [SerializeField] private MachineState currentState = MachineState.Idle;
    [SerializeField] private PotRecipe currentRecipe = null;
    [SerializeField] private bool potCollected = false;
    [SerializeField] private bool shardCollected = false;
    [SerializeField] private int totalShardsCollected = 0;

    // 私有变量
    private Coroutine processingCoroutine;
    private AudioSource processingAudioSource;
    private bool isProcessing = false;

    // ============ 初始化 ============
    void Start()
    {
        InitializeComponents();
        UpdateVisuals();
    }

    private void InitializeComponents()
    {
        // 确保Sprite Renderer引用
        if (potDisplayObject != null && potSpriteRenderer == null)
        {
            potSpriteRenderer = potDisplayObject.GetComponent<SpriteRenderer>();
        }

        if (shardDisplayObject != null && shardSpriteRenderer == null)
        {
            shardSpriteRenderer = shardDisplayObject.GetComponent<SpriteRenderer>();
        }

        // 初始化显示状态
        if (potDisplayObject != null) potDisplayObject.SetActive(false);
        if (shardDisplayObject != null) shardDisplayObject.SetActive(false);

        Debug.Log($"[SynthesisMachine] 初始化完成,当前状态: {currentState}");
    }

    // ============ 视觉更新 ============
    private void UpdateVisuals()
    {
        UpdateMachineVisual();
        UpdateButtonVisual();
    }

    private void UpdateMachineVisual()
    {
        bool isLidOpen = (currentState == MachineState.LidOpen_Empty ||
                         currentState == MachineState.LidOpen_PotInserted ||
                         currentState == MachineState.Complete);

        if (machineClosedObject != null)
            machineClosedObject.SetActive(!isLidOpen);

        if (machineOpenObject != null)
            machineOpenObject.SetActive(isLidOpen);

        Debug.Log($"[SynthesisMachine] 更新机器视觉: 盖子{(isLidOpen ? "打开" : "关闭")}");
    }

    private void UpdateButtonVisual()
    {
        bool isButtonPressed = (currentState == MachineState.Processing);

        if (buttonNormalObject != null)
            buttonNormalObject.SetActive(!isButtonPressed);

        if (buttonPressedObject != null)
            buttonPressedObject.SetActive(isButtonPressed);
    }

    // ============ 陶罐和碎片显示 ============
    private void ShowPot(bool show, Sprite sprite = null)
    {
        if (potDisplayObject == null) return;

        potDisplayObject.SetActive(show);

        if (show && sprite != null && potSpriteRenderer != null)
        {
            potSpriteRenderer.sprite = sprite;
        }

        Debug.Log($"[SynthesisMachine] {(show ? "显示" : "隐藏")}陶罐");
    }

    private void ShowShard(bool show, Sprite sprite = null)
    {
        if (shardDisplayObject == null) return;

        shardDisplayObject.SetActive(show);

        if (show && sprite != null && shardSpriteRenderer != null)
        {
            shardSpriteRenderer.sprite = sprite;
        }

        Debug.Log($"[SynthesisMachine] {(show ? "显示" : "隐藏")}水晶碎片");
    }

    // ============ 音效播放 ============
    private void PlaySFX(string sfxPath)
    {
        if (string.IsNullOrEmpty(sfxPath)) return;
        AudioManager.Instance?.PlaySFX(sfxPath);
    }

    private void StartProcessingSound()
    {
        if (string.IsNullOrEmpty(processingLoopSFX)) return;

        if (processingAudioSource == null)
        {
            processingAudioSource = gameObject.AddComponent<AudioSource>();
            processingAudioSource.loop = true;
        }

        AudioClip clip = Resources.Load<AudioClip>(processingLoopSFX);
        if (clip != null)
        {
            processingAudioSource.clip = clip;
            processingAudioSource.Play();
            Debug.Log("[SynthesisMachine] 开始播放震动音效循环");
        }
    }

    private void StopProcessingSound()
    {
        if (processingAudioSource != null && processingAudioSource.isPlaying)
        {
            processingAudioSource.Stop();
            Debug.Log("[SynthesisMachine] 停止播放震动音效");
        }
    }

    // ============ 提示信息 ============
    private void ShowHint(string message)
    {
        Debug.Log($"[SynthesisMachine] 提示: {message}");
        // TODO: 如果项目有统一的提示系统,可以在这里调用
        // HintSystem.Instance?.ShowHint(message);
    }

    // ============ 状态检查 ============
    private bool HasSelectedFilledPot()
    {
        if (UIManager.Instance == null || filledPotItem == null) return false;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        return selectedItem != null && selectedItem.itemID == filledPotItem.itemID;
    }

    // ============ 交互处理 ============

    /// <summary>
    /// 点击机器主体
    /// </summary>
    public void OnMachineClicked()
    {
        Debug.Log($"[SynthesisMachine] 机器被点击,当前状态: {currentState}");

        switch (currentState)
        {
            case MachineState.Idle:
                // 空闲状态,检查是否选中陶罐
                if (HasSelectedFilledPot())
                {
                    // 选中了陶罐,打开盖子并自动放入
                    OpenLid();
                    InsertPot();
                }
                else
                {
                    // 没有选中陶罐,只打开盖子
                    OpenLid();
                }
                break;

            case MachineState.LidOpen_Empty:
                // 盖子打开,等待放入陶罐
                if (HasSelectedFilledPot())
                {
                    InsertPot();
                }
                else
                {
                    // 没有陶罐,关闭盖子
                    CloseLid();
                }
                break;

            case MachineState.LidOpen_PotInserted:
                // 已放入陶罐,关闭盖子
                CloseLid();
                break;

            case MachineState.LidClosed_Ready:
                // 已准备好,打开盖子(可以取出陶罐)
                OpenLid();
                break;

            case MachineState.Processing:
                ShowHint(machineRunningHint);
                break;

            case MachineState.Complete:
                ShowHint(collectItemsFirstHint);
                break;
        }
    }

    /// <summary>
    /// 点击按钮
    /// </summary>
    public void OnButtonClicked()
    {
        Debug.Log($"[SynthesisMachine] 按钮被点击,当前状态: {currentState}");

        switch (currentState)
        {
            case MachineState.LidClosed_Ready:
                // 盖子关闭且陶罐已放入,启动合成
                StartSynthesis();
                break;

            case MachineState.LidOpen_PotInserted:
                ShowHint(needCloseLidHint);
                PlaySFX(errorSFX);
                break;

            case MachineState.Idle:
            case MachineState.LidOpen_Empty:
                ShowHint(needPotHint);
                PlaySFX(errorSFX);
                break;

            case MachineState.Processing:
                ShowHint(machineRunningHint);
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// 点击展示的陶罐
    /// </summary>
    public void OnPotClicked()
    {
        if (currentState == MachineState.Complete && !potCollected)
        {
            CollectPot();
        }
    }

    /// <summary>
    /// 点击展示的水晶碎片
    /// </summary>
    public void OnShardClicked()
    {
        if (currentState == MachineState.Complete && !shardCollected)
        {
            CollectShard();
        }
    }

    // ============ 核心逻辑 ============

    private void OpenLid()
    {
        Debug.Log("[SynthesisMachine] 打开盖子");

        // 更新状态
        if (currentState == MachineState.Idle)
        {
            currentState = MachineState.LidOpen_Empty;
        }
        else if (currentState == MachineState.LidClosed_Ready)
        {
            currentState = MachineState.LidOpen_PotInserted;
        }

        UpdateMachineVisual();
        PlaySFX(lidOpenSFX);
        OnLidOpened?.Invoke();

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void CloseLid()
    {
        Debug.Log("[SynthesisMachine] 关闭盖子");

        // 更新状态
        if (currentState == MachineState.LidOpen_Empty)
        {
            currentState = MachineState.Idle;
        }
        else if (currentState == MachineState.LidOpen_PotInserted)
        {
            currentState = MachineState.LidClosed_Ready;
        }

        UpdateMachineVisual();
        PlaySFX(lidCloseSFX);
        OnLidClosed?.Invoke();

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void InsertPot()
    {
        Debug.Log("[SynthesisMachine] 放入陶罐");

        // 消耗背包中的陶罐
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }

        // 获取陶罐的配方信息
        if (potController != null)
        {
            currentRecipe = potController.MatchedRecipe;
        }

        // 显示陶罐(使用装满的陶罐sprite)
        Sprite potSprite = null;
        if (currentRecipe != null && currentRecipe.filledPotSprite != null)
        {
            potSprite = currentRecipe.filledPotSprite;
        }
        else if (filledPotItem != null)
        {
            potSprite = filledPotItem.icon;
        }

        ShowPot(true, potSprite);

        // 更新状态
        currentState = MachineState.LidOpen_PotInserted;

        PlaySFX(potInsertSFX);
        OnPotInserted?.Invoke();

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void StartSynthesis()
    {
        Debug.Log("[SynthesisMachine] 开始合成");

        // 更新状态
        currentState = MachineState.Processing;
        isProcessing = true;

        UpdateButtonVisual();
        PlaySFX(buttonPressSFX);
        OnSynthesisStarted?.Invoke();

        // 启动合成协程
        if (processingCoroutine != null)
        {
            StopCoroutine(processingCoroutine);
        }
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
        Debug.Log("[SynthesisMachine] 合成完成!");

        // 更新状态
        currentState = MachineState.Complete;
        isProcessing = false;
        potCollected = false;
        shardCollected = false;

        // 打开盖子并播放开盖音效
        UpdateMachineVisual();
        PlaySFX(lidOpenSFX);

        // 播放完成音效
        PlaySFX(completeSFX);

        // 标记配方完成
        if (potController != null)
        {
            potController.MarkRecipeCompleted();
        }

        // 显示结果
        ShowResults();

        UpdateButtonVisual();
        OnSynthesisCompleted?.Invoke();

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void ShowResults()
    {
        // 将陶罐图片更换为空陶罐
        Sprite emptyPotSprite = emptyPotItem != null ? emptyPotItem.icon : null;
        ShowPot(true, emptyPotSprite);

        // 显示水晶碎片
        if (currentRecipe != null && currentRecipe.resultShard != null)
        {
            Sprite shardSprite = currentRecipe.shardDisplaySprite != null
                ? currentRecipe.shardDisplaySprite
                : currentRecipe.resultShard.icon;

            ShowShard(true, shardSprite);
        }

        Debug.Log("[SynthesisMachine] 展示结果: 空陶罐和水晶碎片");
    }

    private void CollectPot()
    {
        Debug.Log("[SynthesisMachine] 拾取空陶罐");

        if (emptyPotItem == null)
        {
            Debug.LogError("[SynthesisMachine] emptyPotItem 未设置!");
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
        ShowPot(false);

        // 在陶罐原位置刷新空陶罐(如果陶罐控制器存在)
        if (potController != null)
        {
            potController.ClearContents();
        }

        PlaySFX(pickupSFX);
        potCollected = true;
        OnPotCollected?.Invoke();

        CheckAllCollected();
        SaveLoadSystem.Instance?.SaveGame();
    }

    private void CollectShard()
    {
        Debug.Log("[SynthesisMachine] 拾取水晶碎片");

        if (currentRecipe == null || currentRecipe.resultShard == null)
        {
            Debug.LogError("[SynthesisMachine] 当前配方或结果碎片为空!");
            return;
        }

        // 添加到背包
        bool added = InventorySystem.Instance.AddItem(currentRecipe.resultShard);
        if (!added)
        {
            ShowHint("背包已满");
            return;
        }

        // 隐藏展示的水晶碎片
        ShowShard(false);

        PlaySFX(pickupSFX);
        shardCollected = true;
        totalShardsCollected++;
        OnShardCollected?.Invoke();

        CheckAllCollected();
        SaveLoadSystem.Instance?.SaveGame();
    }

    private void CheckAllCollected()
    {
        if (potCollected && shardCollected)
        {
            Debug.Log("[SynthesisMachine] 所有物品已收集,重置机器");

            // 重置状态
            currentState = MachineState.Idle;
            currentRecipe = null;

            UpdateVisuals();

            // 检查是否完成所有配方
            if (potController != null && potController.AreAllRecipesCompleted())
            {
                Debug.Log($"[SynthesisMachine] 所有配方已完成! 总共收集了 {totalShardsCollected} 个水晶碎片");
                OnAllShardsCollected?.Invoke();
            }

            SaveLoadSystem.Instance?.SaveGame();
        }
    }

    // ============ 保存/加载 ============
    public MachineData GetSaveData()
    {
        return new MachineData
        {
            objectID = this.objectID,
            currentState = this.currentState.ToString(),
            potCollected = this.potCollected,
            shardCollected = this.shardCollected,
            totalShardsCollected = this.totalShardsCollected,
            currentRecipeID = this.currentRecipe != null ? this.currentRecipe.recipeID : ""
        };
    }

    public void LoadFromData(MachineData data)
    {
        if (data == null) return;

        // 恢复状态
        if (System.Enum.TryParse(data.currentState, out MachineState state))
        {
            currentState = state;
        }

        potCollected = data.potCollected;
        shardCollected = data.shardCollected;
        totalShardsCollected = data.totalShardsCollected;

        // 恢复配方
        if (!string.IsNullOrEmpty(data.currentRecipeID) && potController != null)
        {
            foreach (var recipe in potController.availableRecipes)
            {
                if (recipe.recipeID == data.currentRecipeID)
                {
                    currentRecipe = recipe;
                    break;
                }
            }
        }

        // 更新视觉
        UpdateVisuals();

        // 恢复陶罐和碎片显示
        if (currentState == MachineState.Complete)
        {
            ShowResults();

            if (potCollected)
            {
                ShowPot(false);
            }

            if (shardCollected)
            {
                ShowShard(false);
            }
        }
        else if (currentState == MachineState.LidOpen_PotInserted ||
                 currentState == MachineState.LidClosed_Ready ||
                 currentState == MachineState.Processing)
        {
            // 恢复陶罐显示
            Sprite potSprite = null;
            if (currentRecipe != null && currentRecipe.filledPotSprite != null)
            {
                potSprite = currentRecipe.filledPotSprite;
            }
            else if (filledPotItem != null)
            {
                potSprite = filledPotItem.icon;
            }
            ShowPot(true, potSprite);
        }

        Debug.Log($"[SynthesisMachine] 加载数据完成,状态: {currentState}");
    }
}

// ============ 保存数据结构 ============
[System.Serializable]
public class MachineData
{
    public string objectID;
    public string currentState;
    public bool potCollected;
    public bool shardCollected;
    public int totalShardsCollected;
    public string currentRecipeID;
}