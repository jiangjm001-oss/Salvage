// Assets/Scripts/GamePlay/AlcoholLampExperiment.cs
// 酒精灯加热实验控制器
// 流程：火柴点燃酒精灯 → 试管放上火焰 → 加入粉末 → 加热变熟 → 拾取成品
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class AlcoholLampExperiment : MonoBehaviour
{
    // ============ 单例 ============
    public static AlcoholLampExperiment Instance { get; private set; }

    // ============ 实验阶段枚举 ============
    public enum Stage
    {
        Initial,            // 初始状态：酒精灯未点燃
        LampLit,            // 酒精灯已点燃，火焰出现
        TestTubePlaced,     // 试管已放置在火焰上
        PowderAdded,        // 粉末已加入试管
        PowderCooked,       // 粉末已烧熟，可拾取
        Complete            // 实验完成，粉末已拾取
    }

    [Header("当前状态")]
    [SerializeField] private Stage currentStage = Stage.Initial;

    [Header("物品配置")]
    [Tooltip("火柴物品数据")]
    public ItemData matchItem;

    [Tooltip("试管物品数据")]
    public ItemData testTubeItem;

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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        UpdateVisibility();
    }

    /// <summary>
    /// 根据当前阶段更新场景物体的显示状态
    /// </summary>
    private void UpdateVisibility()
    {
        if (flame != null)
            flame.SetActive(currentStage >= Stage.LampLit && currentStage < Stage.Complete);

        if (testTubeOnFlame != null)
            testTubeOnFlame.SetActive(currentStage >= Stage.TestTubePlaced && currentStage < Stage.Complete);

        if (rawPowderSprite != null)
            rawPowderSprite.SetActive(currentStage == Stage.PowderAdded);

        if (cookedPowderSprite != null)
            cookedPowderSprite.SetActive(currentStage == Stage.PowderCooked);

        if (pickupableCookedPowder != null)
            pickupableCookedPowder.SetActive(currentStage == Stage.PowderCooked);
    }

    // ============ 交互入口方法 ============

    /// <summary>
    /// 点击酒精灯
    /// </summary>
    public void ClickAlcoholLamp()
    {
        Debug.Log($"[AlcoholLampExperiment] 点击酒精灯，当前阶段: {currentStage}");

        if (currentStage != Stage.Initial)
        {
            Debug.Log("[AlcoholLampExperiment] 酒精灯已点燃或实验已完成");
            return;
        }

        if (!TryUseItem(matchItem, wrongItemForLampHint))
            return;

        // 点燃酒精灯
        Debug.Log("[AlcoholLampExperiment] ★ 点燃酒精灯！");
        currentStage = Stage.LampLit;
        PlaySound(lightFireSound);
        UpdateVisibility();
        OnLampLit?.Invoke();
        SaveGame();
    }

    /// <summary>
    /// 点击火焰
    /// </summary>
    public void ClickFlame()
    {
        Debug.Log($"[AlcoholLampExperiment] 点击火焰，当前阶段: {currentStage}");

        if (currentStage != Stage.LampLit)
        {
            Debug.Log("[AlcoholLampExperiment] 当前阶段不能放置试管");
            return;
        }

        if (!TryUseItem(testTubeItem, wrongItemForFlameHint))
            return;

        // 放置试管
        Debug.Log("[AlcoholLampExperiment] ★ 放置试管到火焰上！");
        currentStage = Stage.TestTubePlaced;
        PlaySound(placeTubeSound);
        UpdateVisibility();
        OnTestTubePlaced?.Invoke();
        SaveGame();
    }

    /// <summary>
    /// 点击试管（直接开始加热，无需选中物品）
    /// </summary>
    public void ClickTestTube()
    {
        Debug.Log($"[AlcoholLampExperiment] 点击试管，当前阶段: {currentStage}");

        // 正在加热中
        if (isCooking)
        {
            ShowHint(waitingHint);
            return;
        }

        // 只有试管放置后才能点击加热
        if (currentStage != Stage.TestTubePlaced)
        {
            Debug.Log("[AlcoholLampExperiment] 当前阶段不能加热");
            return;
        }

        // 直接开始加热（不需要选中物品）
        Debug.Log("[AlcoholLampExperiment] ★ 开始加热试管！");
        currentStage = Stage.PowderAdded;
        PlaySound(addPowderSound);
        UpdateVisibility();
        OnPowderAdded?.Invoke();

        if (cookingCoroutine != null)
            StopCoroutine(cookingCoroutine);
        cookingCoroutine = StartCoroutine(CookingProcess());
        SaveGame();
    }

    /// <summary>
    /// 点击熟粉末拾取
    /// </summary>
    public void ClickCookedPowder()
    {
        Debug.Log($"[AlcoholLampExperiment] 点击熟粉末，当前阶段: {currentStage}");

        if (currentStage != Stage.PowderCooked)
        {
            Debug.Log("[AlcoholLampExperiment] 粉末还没烧熟或已被拾取");
            return;
        }

        // 拾取熟粉末
        Debug.Log("[AlcoholLampExperiment] ★ 拾取烧熟的粉末！");

        if (cookedPowderItem != null && InventorySystem.Instance != null)
        {
            bool added = InventorySystem.Instance.AddItem(cookedPowderItem);
            if (!added)
            {
                Debug.LogWarning("[AlcoholLampExperiment] 背包已满！");
                ShowHint("背包已满！");
                return;
            }
            Debug.Log($"[AlcoholLampExperiment] 获得物品: {cookedPowderItem.displayName}");
        }

        currentStage = Stage.Complete;
        PlaySound(pickupSound);
        UpdateVisibility();
        OnExperimentComplete?.Invoke();
        SaveGame();
    }

    // ============ 加热协程 ============

    private IEnumerator CookingProcess()
    {
        isCooking = true;
        Debug.Log($"[AlcoholLampExperiment] 开始加热，等待 {cookingTime} 秒...");

        yield return new WaitForSeconds(cookingTime);

        isCooking = false;
        Debug.Log("[AlcoholLampExperiment] ★ 加热完成，粉末已烧熟！");

        currentStage = Stage.PowderCooked;
        PlaySound(cookingCompleteSound);
        UpdateVisibility();
        OnPowderCooked?.Invoke();
        SaveGame();
    }

    // ============ 辅助方法 ============

    private bool TryUseItem(ItemData requiredItem, string wrongItemHint)
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[AlcoholLampExperiment] UIManager.Instance 不存在！");
            return false;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        if (selectedItem == null)
        {
            Debug.Log("[AlcoholLampExperiment] 没有选中任何物品");
            ShowHint(noItemHint);
            return false;
        }

        if (requiredItem == null || selectedItem.itemID != requiredItem.itemID)
        {
            Debug.Log($"[AlcoholLampExperiment] 选中的物品 '{selectedItem.displayName}' 不匹配");
            ShowHint(wrongItemHint);
            return false;
        }

        Debug.Log($"[AlcoholLampExperiment] 使用物品: {selectedItem.displayName}");
        UIManager.Instance.ConsumeSelectedItem();
        return true;
    }

    private void ShowHint(string hint)
    {
        Debug.Log($"[AlcoholLampExperiment] 提示: {hint}");
    }

    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
            AudioManager.Instance.PlaySFX(soundName);
    }

    private void SaveGame()
    {
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ 存档相关 ============

    public int GetCurrentStageIndex()
    {
        return (int)currentStage;
    }

    public void SetCurrentStage(int stageIndex)
    {
        currentStage = (Stage)stageIndex;

        if (currentStage == Stage.PowderAdded)
            StartCoroutine(CookingProcess());
        else
            UpdateVisibility();

        Debug.Log($"[AlcoholLampExperiment] 从存档恢复阶段: {currentStage}");
    }

    public bool IsCooking()
    {
        return isCooking;
    }

    // ============ 编辑器调试 ============

#if UNITY_EDITOR
    [ContextMenu("调试 - 重置实验")]
    private void DebugReset()
    {
        if (cookingCoroutine != null)
            StopCoroutine(cookingCoroutine);
        isCooking = false;
        currentStage = Stage.Initial;
        UpdateVisibility();
        Debug.Log("[AlcoholLampExperiment] 实验已重置");
    }

    [ContextMenu("调试 - 跳到下一阶段")]
    private void DebugNextStage()
    {
        if (currentStage < Stage.Complete)
        {
            currentStage++;
            UpdateVisibility();
            Debug.Log($"[AlcoholLampExperiment] 跳转到阶段: {currentStage}");
        }
    }
#endif
}