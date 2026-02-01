// Assets/Scripts/GamePlay/Lv2/AlcoholLampExperiment.cs
// 酒精灯实验控制器 - 管理酒精灯加热试管的多步骤实验流程
// 流程：火柴点燃酒精灯 → 试管放上火焰 → 加入粉末 → 加热变熟 → 拾取成品
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class AlcoholLampExperiment : MonoBehaviour
{
    // ============ 单例 ============
    public static AlcoholLampExperiment Instance { get; private set; }

    // ============ 实验阶段枚举 ============
    public enum ExperimentStage
    {
        Initial,            // 初始状态：酒精灯未点燃
        LampLit,            // 酒精灯已点燃，火焰出现
        TestTubePlaced,     // 试管已放置在火焰上
        PowderAdded,        // 粉末已加入试管
        PowderCooked,       // 粉末已烧熟，可拾取
        Complete            // 实验完成，粉末已拾取
    }

    [Header("当前状态")]
    [SerializeField] private ExperimentStage currentStage = ExperimentStage.Initial;

    [Header("物品配置")]
    [Tooltip("火柴物品数据")]
    public ItemData matchItem;

    [Tooltip("试管物品数据")]
    public ItemData testTubeItem;

    [Tooltip("肋骨粉末物品数据")]
    public ItemData ribPowderItem;

    [Tooltip("烧熟的粉末物品数据（最终产物）")]
    public ItemData cookedPowderItem;

    [Header("场景物体引用")]
    [Tooltip("酒精灯物体")]
    public GameObject alcoholLamp;

    [Tooltip("火焰物体（初始隐藏）")]
    public GameObject flame;

    [Tooltip("放置在火焰上的试管物体（初始隐藏）")]
    public GameObject testTubeOnFlame;

    [Tooltip("试管中的生粉末精灵（初始隐藏）")]
    public GameObject rawPowderSprite;

    [Tooltip("试管中的熟粉末精灵（初始隐藏）")]
    public GameObject cookedPowderSprite;

    [Tooltip("可拾取的熟粉末物体（初始隐藏）")]
    public GameObject pickupableCookedPowder;

    [Header("时间设置")]
    [Tooltip("粉末加热变熟的时间（秒）")]
    public float cookingTime = 2f;

    [Header("提示文本")]
    public string noItemHint = "需要用什么东西...";
    public string wrongItemForLampHint = "这个点不着酒精灯...";
    public string wrongItemForFlameHint = "这个东西不能放在火上加热...";
    public string wrongItemForTubeHint = "这个东西不需要加热...";
    public string waitingHint = "正在加热中，请稍等...";

    [Header("音效")]
    public string lightFireSound = "Audio/SFX/fire_light";
    public string placeTubeSound = "Audio/SFX/glass_place";
    public string addPowderSound = "Audio/SFX/powder_pour";
    public string cookingCompleteSound = "Audio/SFX/cooking_done";
    public string pickupSound = "Audio/SFX/item_pickup";

    [Header("事件")]
    public UnityEvent OnLampLit;
    public UnityEvent OnTestTubePlaced;
    public UnityEvent OnPowderAdded;
    public UnityEvent OnPowderCooked;
    public UnityEvent OnExperimentComplete;

    // 内部状态
    private bool isCooking = false;
    private Coroutine cookingCoroutine;

    // ============ 生命周期 ============

    private void Awake()
    {
        // 单例设置
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 初始化场景物体状态
        InitializeSceneObjects();
    }

    /// <summary>
    /// 初始化场景物体的显示状态
    /// </summary>
    private void InitializeSceneObjects()
    {
        // 根据当前阶段设置物体显示状态
        UpdateSceneObjectsVisibility();
    }

    /// <summary>
    /// 根据当前阶段更新场景物体的显示状态
    /// </summary>
    private void UpdateSceneObjectsVisibility()
    {
        // 火焰：LampLit 及之后阶段显示
        if (flame != null)
        {
            flame.SetActive(currentStage >= ExperimentStage.LampLit && currentStage < ExperimentStage.Complete);
        }

        // 试管：TestTubePlaced 及之后阶段显示
        if (testTubeOnFlame != null)
        {
            testTubeOnFlame.SetActive(currentStage >= ExperimentStage.TestTubePlaced && currentStage < ExperimentStage.Complete);
        }

        // 生粉末：PowderAdded 阶段显示
        if (rawPowderSprite != null)
        {
            rawPowderSprite.SetActive(currentStage == ExperimentStage.PowderAdded);
        }

        // 熟粉末：PowderCooked 阶段显示
        if (cookedPowderSprite != null)
        {
            cookedPowderSprite.SetActive(currentStage == ExperimentStage.PowderCooked);
        }

        // 可拾取的熟粉末：PowderCooked 阶段显示
        if (pickupableCookedPowder != null)
        {
            pickupableCookedPowder.SetActive(currentStage == ExperimentStage.PowderCooked);
        }
    }

    // ============ 交互入口方法 ============

    /// <summary>
    /// 点击酒精灯时调用
    /// </summary>
    public void OnAlcoholLampClicked()
    {
        Debug.Log($"[AlcoholLampExperiment] 点击酒精灯，当前阶段: {currentStage}");

        if (currentStage != ExperimentStage.Initial)
        {
            Debug.Log("[AlcoholLampExperiment] 酒精灯已经点燃或实验已完成");
            return;
        }

        // 检查是否选中了火柴
        if (!TryUseItem(matchItem, wrongItemForLampHint))
        {
            return;
        }

        // 点燃酒精灯
        LightAlcoholLamp();
    }

    /// <summary>
    /// 点击火焰时调用
    /// </summary>
    public void OnFlameClicked()
    {
        Debug.Log($"[AlcoholLampExperiment] 点击火焰，当前阶段: {currentStage}");

        if (currentStage != ExperimentStage.LampLit)
        {
            Debug.Log("[AlcoholLampExperiment] 当前阶段不能放置试管");
            return;
        }

        // 检查是否选中了试管
        if (!TryUseItem(testTubeItem, wrongItemForFlameHint))
        {
            return;
        }

        // 放置试管
        PlaceTestTube();
    }

    /// <summary>
    /// 点击试管时调用
    /// </summary>
    public void OnTestTubeClicked()
    {
        Debug.Log($"[AlcoholLampExperiment] 点击试管，当前阶段: {currentStage}");

        // 如果正在加热中，显示等待提示
        if (isCooking)
        {
            ShowHint(waitingHint);
            return;
        }

        if (currentStage != ExperimentStage.TestTubePlaced)
        {
            Debug.Log("[AlcoholLampExperiment] 当前阶段不能添加粉末");
            return;
        }

        // 检查是否选中了肋骨粉末
        if (!TryUseItem(ribPowderItem, wrongItemForTubeHint))
        {
            return;
        }

        // 添加粉末并开始加热
        AddPowderAndStartCooking();
    }

    /// <summary>
    /// 点击熟粉末拾取
    /// </summary>
    public void OnCookedPowderClicked()
    {
        Debug.Log($"[AlcoholLampExperiment] 点击熟粉末，当前阶段: {currentStage}");

        if (currentStage != ExperimentStage.PowderCooked)
        {
            Debug.Log("[AlcoholLampExperiment] 粉末还没烧熟或已被拾取");
            return;
        }

        // 拾取熟粉末
        PickupCookedPowder();
    }

    // ============ 实验阶段处理 ============

    /// <summary>
    /// 点燃酒精灯
    /// </summary>
    private void LightAlcoholLamp()
    {
        Debug.Log("[AlcoholLampExperiment] ★ 点燃酒精灯！");

        currentStage = ExperimentStage.LampLit;

        // 播放音效
        PlaySound(lightFireSound);

        // 更新显示
        UpdateSceneObjectsVisibility();

        // 触发事件
        OnLampLit?.Invoke();

        // 保存游戏
        SaveGame();
    }

    /// <summary>
    /// 放置试管到火焰上
    /// </summary>
    private void PlaceTestTube()
    {
        Debug.Log("[AlcoholLampExperiment] ★ 放置试管到火焰上！");

        currentStage = ExperimentStage.TestTubePlaced;

        // 播放音效
        PlaySound(placeTubeSound);

        // 更新显示
        UpdateSceneObjectsVisibility();

        // 触发事件
        OnTestTubePlaced?.Invoke();

        // 保存游戏
        SaveGame();
    }

    /// <summary>
    /// 添加粉末并开始加热
    /// </summary>
    private void AddPowderAndStartCooking()
    {
        Debug.Log("[AlcoholLampExperiment] ★ 添加粉末，开始加热！");

        currentStage = ExperimentStage.PowderAdded;

        // 播放音效
        PlaySound(addPowderSound);

        // 更新显示（显示生粉末）
        UpdateSceneObjectsVisibility();

        // 触发事件
        OnPowderAdded?.Invoke();

        // 开始加热协程
        if (cookingCoroutine != null)
        {
            StopCoroutine(cookingCoroutine);
        }
        cookingCoroutine = StartCoroutine(CookingProcess());

        // 保存游戏
        SaveGame();
    }

    /// <summary>
    /// 加热过程协程
    /// </summary>
    private IEnumerator CookingProcess()
    {
        isCooking = true;
        Debug.Log($"[AlcoholLampExperiment] 开始加热，等待 {cookingTime} 秒...");

        // 等待加热时间
        yield return new WaitForSeconds(cookingTime);

        // 加热完成
        isCooking = false;
        Debug.Log("[AlcoholLampExperiment] ★ 加热完成，粉末已烧熟！");

        currentStage = ExperimentStage.PowderCooked;

        // 播放完成音效
        PlaySound(cookingCompleteSound);

        // 更新显示（切换为熟粉末）
        UpdateSceneObjectsVisibility();

        // 触发事件
        OnPowderCooked?.Invoke();

        // 保存游戏
        SaveGame();
    }

    /// <summary>
    /// 拾取烧熟的粉末
    /// </summary>
    private void PickupCookedPowder()
    {
        Debug.Log("[AlcoholLampExperiment] ★ 拾取烧熟的粉末！");

        // 添加到背包
        if (cookedPowderItem != null && InventorySystem.Instance != null)
        {
            bool added = InventorySystem.Instance.AddItem(cookedPowderItem);
            if (added)
            {
                Debug.Log($"[AlcoholLampExperiment] 获得物品: {cookedPowderItem.displayName}");
            }
            else
            {
                Debug.LogWarning("[AlcoholLampExperiment] 背包已满，无法添加物品！");
                ShowHint("背包已满！");
                return;
            }
        }

        currentStage = ExperimentStage.Complete;

        // 播放音效
        PlaySound(pickupSound);

        // 更新显示（隐藏所有实验物体）
        UpdateSceneObjectsVisibility();

        // 触发事件
        OnExperimentComplete?.Invoke();

        // 保存游戏
        SaveGame();
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 尝试使用指定物品
    /// </summary>
    private bool TryUseItem(ItemData requiredItem, string wrongItemHint)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[AlcoholLampExperiment] UIManager.Instance 不存在！");
            return false;
        }

        // 获取当前选中的物品
        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        // 没有选中任何物品
        if (selectedItem == null)
        {
            Debug.Log("[AlcoholLampExperiment] 没有选中任何物品");
            ShowHint(noItemHint);
            return false;
        }

        // 检查是否是正确的物品
        if (requiredItem == null || selectedItem.itemID != requiredItem.itemID)
        {
            Debug.Log($"[AlcoholLampExperiment] 选中的物品 '{selectedItem.displayName}' 不匹配");
            ShowHint(wrongItemHint);
            return false;
        }

        // 消耗物品
        Debug.Log($"[AlcoholLampExperiment] 使用物品: {selectedItem.displayName}");
        UIManager.Instance.ConsumeSelectedItem();

        return true;
    }

    /// <summary>
    /// 显示提示信息
    /// </summary>
    private void ShowHint(string hint)
    {
        Debug.Log($"[AlcoholLampExperiment] 提示: {hint}");
        // 如果有提示系统，在这里调用
        // HintSystem.Instance?.ShowHint(hint);
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    /// <summary>
    /// 保存游戏
    /// </summary>
    private void SaveGame()
    {
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ 存档相关 ============

    /// <summary>
    /// 获取当前阶段（用于存档）
    /// </summary>
    public int GetCurrentStageIndex()
    {
        return (int)currentStage;
    }

    /// <summary>
    /// 设置当前阶段（用于读档）
    /// </summary>
    public void SetCurrentStage(int stageIndex)
    {
        currentStage = (ExperimentStage)stageIndex;

        if (currentStage == ExperimentStage.PowderAdded)
        {
            StartCoroutine(CookingProcess());
        }
        else
        {
            UpdateSceneObjectsVisibility();
        }

        Debug.Log($"[AlcoholLampExperiment] 从存档恢复阶段: {currentStage}");
    }

    /// <summary>
    /// 获取是否正在加热
    /// </summary>
    public bool IsCooking()
    {
        return isCooking;
    }

    // ============ 编辑器调试 ============

#if UNITY_EDITOR
    [ContextMenu("调试 - 重置实验")]
    private void DebugResetExperiment()
    {
        if (cookingCoroutine != null)
        {
            StopCoroutine(cookingCoroutine);
        }
        isCooking = false;
        currentStage = ExperimentStage.Initial;
        UpdateSceneObjectsVisibility();
        Debug.Log("[AlcoholLampExperiment] 实验已重置");
    }

    [ContextMenu("调试 - 跳到下一阶段")]
    private void DebugNextStage()
    {
        if (currentStage < ExperimentStage.Complete)
        {
            currentStage++;
            UpdateSceneObjectsVisibility();
            Debug.Log($"[AlcoholLampExperiment] 跳转到阶段: {currentStage}");
        }
    }
#endif
}