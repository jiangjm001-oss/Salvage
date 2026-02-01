// Assets/Scripts/GamePlay/Experimenter/ExperimenterPuzzleController.cs
// 实验者放大镜谜题主控制器
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 实验者放大镜谜题控制器
/// 管理整个谜题流程：放大镜使用 → 拖动定位 → 肋骨显示 → 收集粉末
/// </summary>
public class ExperimenterPuzzleController : MonoBehaviour
{
    #region ========== 谜题状态 ==========

    /// <summary>
    /// 谜题状态枚举
    /// </summary>
    public enum PuzzleState
    {
        WaitingForMagnifier,    // 等待使用放大镜
        MagnifierPlaced,        // 放大镜已放置，可拖动
        RibsRevealed,           // 肋骨已显示，等待试管
        Completed               // 谜题完成
    }

    [Header("当前状态")]
    [SerializeField] private PuzzleState currentState = PuzzleState.WaitingForMagnifier;
    public PuzzleState CurrentState => currentState;

    #endregion

    #region ========== 物品配置 ==========

    [Header("所需物品")]
    [Tooltip("放大镜物品数据")]
    public ItemData magnifierItem;

    [Tooltip("试管物品数据")]
    public ItemData testTubeItem;

    [Tooltip("肋骨粉末物品数据（收集后获得）")]
    public ItemData ribPowderItem;

    [Header("物品消耗设置")]
    [Tooltip("使用放大镜后是否从背包移除")]
    public bool consumeMagnifier = false;

    [Tooltip("使用试管后是否从背包移除")]
    public bool consumeTestTube = true;

    #endregion

    #region ========== 场景物体引用 ==========

    [Header("场景物体")]
    [Tooltip("身体可点击区域（用于放置放大镜）")]
    public GameObject bodyClickArea;

    [Tooltip("放大镜物体（场景中的，初始隐藏）")]
    public GameObject magnifierObject;

    [Tooltip("放大镜目标区域（拖动到此处触发）")]
    public GameObject magnifierTargetZone;

    [Tooltip("肋骨物体（初始隐藏，放大后显示）")]
    public GameObject ribsObject;

    [Tooltip("肋骨可点击区域（用于收集粉末）")]
    public GameObject ribsClickArea;

    [Tooltip("放大后的放大镜外观（可选，用于放大效果）")]
    public GameObject magnifierEnlargedObject;

    #endregion

    #region ========== 音效配置 ==========

    [Header("音效")]
    [Tooltip("放置放大镜音效")]
    public string placeMagnifierSound = "Audio/SFX/item_place";

    [Tooltip("发现肋骨音效")]
    public string revealRibsSound = "Audio/SFX/discover";

    [Tooltip("收集粉末音效")]
    public string collectPowderSound = "Audio/SFX/item_pickup";

    #endregion

    #region ========== 提示文本 ==========

    [Header("提示文本")]
    [Tooltip("没有选中物品时的提示")]
    public string noItemHint = "需要用什么东西查看...";

    [Tooltip("选中错误物品时的提示")]
    public string wrongItemHint = "这个东西在这里没有用...";

    [Tooltip("需要试管时的提示")]
    public string needTestTubeHint = "需要用什么来收集这些粉末...";

    #endregion

    #region ========== 事件 ==========

    [Header("事件")]
    public UnityEvent OnMagnifierPlaced;
    public UnityEvent OnRibsRevealed;
    public UnityEvent OnPowderCollected;
    public UnityEvent OnPuzzleCompleted;

    #endregion

    #region ========== 组件缓存 ==========

    private DraggableMagnifier draggableMagnifier;
    private MagnifierTargetZone targetZone;

    #endregion

    #region ========== Unity 生命周期 ==========

    private void Awake()
    {
        // 获取组件引用
        if (magnifierObject != null)
        {
            draggableMagnifier = magnifierObject.GetComponent<DraggableMagnifier>();
        }

        if (magnifierTargetZone != null)
        {
            targetZone = magnifierTargetZone.GetComponent<MagnifierTargetZone>();
        }
    }

    private void Start()
    {
        InitializePuzzle();
    }

    private void OnEnable()
    {
        // 订阅事件
        if (draggableMagnifier != null)
        {
            draggableMagnifier.OnReachedTarget.AddListener(OnMagnifierReachedTarget);
        }
    }

    private void OnDisable()
    {
        // 取消订阅
        if (draggableMagnifier != null)
        {
            draggableMagnifier.OnReachedTarget.RemoveListener(OnMagnifierReachedTarget);
        }
    }

    #endregion

    #region ========== 初始化 ==========

    /// <summary>
    /// 初始化谜题状态
    /// </summary>
    private void InitializePuzzle()
    {
        // 根据当前状态设置物体可见性
        switch (currentState)
        {
            case PuzzleState.WaitingForMagnifier:
                SetupWaitingForMagnifier();
                break;

            case PuzzleState.MagnifierPlaced:
                SetupMagnifierPlaced();
                break;

            case PuzzleState.RibsRevealed:
                SetupRibsRevealed();
                break;

            case PuzzleState.Completed:
                SetupCompleted();
                break;
        }

        Debug.Log($"[ExperimenterPuzzle] 初始化完成，当前状态: {currentState}");
    }

    private void SetupWaitingForMagnifier()
    {
        // 隐藏放大镜和肋骨
        if (magnifierObject != null) magnifierObject.SetActive(false);
        if (magnifierEnlargedObject != null) magnifierEnlargedObject.SetActive(false);
        if (ribsObject != null) ribsObject.SetActive(false);
        if (ribsClickArea != null) ribsClickArea.SetActive(false);
        if (magnifierTargetZone != null) magnifierTargetZone.SetActive(false);

        // 显示身体点击区域
        if (bodyClickArea != null) bodyClickArea.SetActive(true);
    }

    private void SetupMagnifierPlaced()
    {
        // 显示可拖动的放大镜
        if (magnifierObject != null) magnifierObject.SetActive(true);
        if (magnifierTargetZone != null) magnifierTargetZone.SetActive(true);

        // 隐藏肋骨
        if (ribsObject != null) ribsObject.SetActive(false);
        if (ribsClickArea != null) ribsClickArea.SetActive(false);
        if (magnifierEnlargedObject != null) magnifierEnlargedObject.SetActive(false);
    }

    private void SetupRibsRevealed()
    {
        // 显示放大的放大镜和肋骨
        if (magnifierObject != null) magnifierObject.SetActive(false);
        if (magnifierEnlargedObject != null) magnifierEnlargedObject.SetActive(true);
        if (ribsObject != null) ribsObject.SetActive(true);
        if (ribsClickArea != null) ribsClickArea.SetActive(true);

        // 隐藏目标区域
        if (magnifierTargetZone != null) magnifierTargetZone.SetActive(false);
    }

    private void SetupCompleted()
    {
        // 隐藏所有谜题相关物体
        if (magnifierObject != null) magnifierObject.SetActive(false);
        if (magnifierEnlargedObject != null) magnifierEnlargedObject.SetActive(false);
        if (ribsObject != null) ribsObject.SetActive(false);
        if (ribsClickArea != null) ribsClickArea.SetActive(false);
        if (magnifierTargetZone != null) magnifierTargetZone.SetActive(false);
    }

    #endregion

    #region ========== 交互处理 ==========

    /// <summary>
    /// 处理身体点击（由身体的 InteractableObject 调用）
    /// </summary>
    public void OnBodyClicked()
    {
        Debug.Log($"[ExperimenterPuzzle] 身体被点击，当前状态: {currentState}");

        if (currentState != PuzzleState.WaitingForMagnifier)
        {
            Debug.Log("[ExperimenterPuzzle] 当前状态不需要处理身体点击");
            return;
        }

        // 检查是否选中了放大镜
        if (!TryUseMagnifier())
        {
            // 显示提示
            ShowHint();
        }
    }

    /// <summary>
    /// 尝试使用放大镜
    /// </summary>
    private bool TryUseMagnifier()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[ExperimenterPuzzle] UIManager.Instance 为空！");
            return false;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        // 没有选中物品
        if (selectedItem == null)
        {
            Debug.Log("[ExperimenterPuzzle] 没有选中任何物品");
            return false;
        }

        // 检查是否是放大镜
        if (magnifierItem == null)
        {
            Debug.LogError("[ExperimenterPuzzle] 未配置 magnifierItem！");
            return false;
        }

        if (selectedItem.itemID != magnifierItem.itemID)
        {
            Debug.Log($"[ExperimenterPuzzle] 选中的不是放大镜: {selectedItem.displayName}");
            return false;
        }

        // 使用放大镜
        PlaceMagnifier();
        return true;
    }

    /// <summary>
    /// 放置放大镜
    /// </summary>
    private void PlaceMagnifier()
    {
        Debug.Log("[ExperimenterPuzzle] ✓ 放置放大镜");

        // 消耗或取消选中放大镜
        if (consumeMagnifier)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        // 播放音效
        PlaySound(placeMagnifierSound);

        // 更新状态
        currentState = PuzzleState.MagnifierPlaced;
        SetupMagnifierPlaced();

        // 启用拖动
        if (draggableMagnifier != null)
        {
            draggableMagnifier.EnableDragging();
        }

        OnMagnifierPlaced?.Invoke();
        SaveProgress();
    }

    /// <summary>
    /// 放大镜到达目标位置的回调
    /// </summary>
    private void OnMagnifierReachedTarget()
    {
        if (currentState != PuzzleState.MagnifierPlaced)
        {
            return;
        }

        Debug.Log("[ExperimenterPuzzle] ✓ 放大镜到达目标位置，显示肋骨");

        // 播放发现音效
        PlaySound(revealRibsSound);

        // 更新状态
        currentState = PuzzleState.RibsRevealed;
        SetupRibsRevealed();

        OnRibsRevealed?.Invoke();
        SaveProgress();
    }

    /// <summary>
    /// 处理肋骨点击（由肋骨的 InteractableObject 调用）
    /// </summary>
    public void OnRibsClicked()
    {
        Debug.Log($"[ExperimenterPuzzle] 肋骨被点击，当前状态: {currentState}");

        if (currentState != PuzzleState.RibsRevealed)
        {
            Debug.Log("[ExperimenterPuzzle] 当前状态不需要处理肋骨点击");
            return;
        }

        // 检查是否选中了试管
        if (!TryUseTestTube())
        {
            // 显示试管提示
            ShowTestTubeHint();
        }
    }

    /// <summary>
    /// 尝试使用试管收集粉末
    /// </summary>
    private bool TryUseTestTube()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[ExperimenterPuzzle] UIManager.Instance 为空！");
            return false;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        // 没有选中物品
        if (selectedItem == null)
        {
            Debug.Log("[ExperimenterPuzzle] 没有选中任何物品");
            return false;
        }

        // 检查是否是试管
        if (testTubeItem == null)
        {
            Debug.LogError("[ExperimenterPuzzle] 未配置 testTubeItem！");
            return false;
        }

        if (selectedItem.itemID != testTubeItem.itemID)
        {
            Debug.Log($"[ExperimenterPuzzle] 选中的不是试管: {selectedItem.displayName}");
            return false;
        }

        // 收集粉末
        CollectPowder();
        return true;
    }

    /// <summary>
    /// 收集肋骨粉末
    /// </summary>
    private void CollectPowder()
    {
        Debug.Log("[ExperimenterPuzzle] ✓ 收集肋骨粉末");

        // 消耗或取消选中试管
        if (consumeTestTube)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        // 添加肋骨粉末到背包
        if (ribPowderItem != null && InventorySystem.Instance != null)
        {
            InventorySystem.Instance.AddItem(ribPowderItem);
            Debug.Log($"[ExperimenterPuzzle] 获得: {ribPowderItem.displayName}");
        }

        // 播放收集音效
        PlaySound(collectPowderSound);

        // 更新状态
        currentState = PuzzleState.Completed;
        SetupCompleted();

        OnPowderCollected?.Invoke();
        OnPuzzleCompleted?.Invoke();
        SaveProgress();
    }

    #endregion

    #region ========== 提示系统 ==========

    private void ShowHint()
    {
        ItemData selectedItem = UIManager.Instance?.GetSelectedItem();

        string hint = selectedItem == null ? noItemHint : wrongItemHint;
        Debug.Log($"[ExperimenterPuzzle] 提示: {hint}");

        // TODO: 如果有 HintSystem，可以调用显示提示
        // HintSystem.Instance?.ShowHint(hint);
    }

    private void ShowTestTubeHint()
    {
        Debug.Log($"[ExperimenterPuzzle] 提示: {needTestTubeHint}");

        // TODO: 如果有 HintSystem，可以调用显示提示
        // HintSystem.Instance?.ShowHint(needTestTubeHint);
    }

    #endregion

    #region ========== 辅助方法 ==========

    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    private void SaveProgress()
    {
        SaveLoadSystem.Instance?.SaveGame();
    }

    #endregion

    #region ========== 存档恢复 ==========

    /// <summary>
    /// 设置谜题状态（用于存档恢复）
    /// </summary>
    public void SetPuzzleState(PuzzleState state)
    {
        currentState = state;
        InitializePuzzle();
        Debug.Log($"[ExperimenterPuzzle] 状态已恢复: {state}");
    }

    /// <summary>
    /// 获取当前状态（用于存档）
    /// </summary>
    public int GetStateForSave()
    {
        return (int)currentState;
    }

    /// <summary>
    /// 从存档恢复状态
    /// </summary>
    public void RestoreFromSave(int stateValue)
    {
        if (System.Enum.IsDefined(typeof(PuzzleState), stateValue))
        {
            SetPuzzleState((PuzzleState)stateValue);
        }
    }

    #endregion
}