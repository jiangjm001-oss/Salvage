// Assets/Scripts/GamePlay/FaucetController.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 水龙头控制器 - 处理烧杯接水交互
/// 
/// 交互流程：
/// 1. 玩家从背包选中空烧杯
/// 2. 点击水龙头
/// 3. 消耗空烧杯，显示水流动画 + 有水的烧杯（场景物体）
/// 4. 玩家点击有水的烧杯拾取到背包
/// 5. 水流消失，有水烧杯物体隐藏
/// 
/// 使用方法：
/// 1. 将此脚本挂载到水龙头物体上
/// 2. 在 Inspector 中配置所有引用
/// 3. 确保水龙头有 Collider2D 用于点击检测
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FaucetController : MonoBehaviour
{
    public enum FaucetState
    {
        Idle,           // 等待玩家使用空烧杯
        Filling,        // 正在接水（水流显示中）
        Completed       // 已完成（有水烧杯被拾取）
    }

    [Header("基本信息")]
    [Tooltip("用于存档的唯一标识符")]
    public string objectID = "faucet_001";

    [Tooltip("显示名称")]
    public string displayName = "水龙头";

    [Header("当前状态（只读）")]
    [SerializeField]
    private FaucetState currentState = FaucetState.Idle;

    [Header("物品设置")]
    [Tooltip("需要使用的物品（空烧杯）")]
    public ItemData requiredItem;

    [Tooltip("接水后获得的物品（有水的烧杯）- 用于拾取")]
    public ItemData resultItem;

    [Tooltip("使用空烧杯后是否从背包移除")]
    public bool consumeRequiredItem = true;

    [Header("场景物体引用")]
    [Tooltip("水流效果物体（初始隐藏）")]
    public GameObject waterFlowObject;

    [Tooltip("有水的烧杯物体（初始隐藏，点击后拾取并隐藏）")]
    public GameObject filledBeakerObject;

    [Header("提示信息")]
    [Tooltip("未选中物品时的提示")]
    public string noItemHint = "需要用什么来接水...";

    [Tooltip("选中错误物品时的提示")]
    public string wrongItemHint = "这个东西接不了水";

    [Tooltip("已经在接水时的提示")]
    public string alreadyFillingHint = "水龙头正在出水";

    [Header("音效设置")]
    [Tooltip("开始接水的音效")]
    public string fillStartSound = "";

    [Tooltip("水流持续音效（循环）")]
    public string waterFlowSound = "";

    [Tooltip("拾取有水烧杯的音效")]
    public string pickupSound = "Audio/SFX/item_pickup";

    [Header("事件")]
    [Tooltip("开始接水时触发")]
    public UnityEvent OnFillStart;

    [Tooltip("拾取有水烧杯时触发")]
    public UnityEvent OnBeakerCollected;

    [Tooltip("整个流程完成时触发")]
    public UnityEvent OnInteractionComplete;

    // 私有变量
    private AudioSource loopingWaterSound;

    private void Start()
    {
        // 确保初始状态正确
        if (currentState == FaucetState.Idle)
        {
            HideWaterEffects();
        }
        else if (currentState == FaucetState.Filling)
        {
            // 如果是读档恢复到 Filling 状态，显示水流
            ShowWaterEffects();
        }
        else if (currentState == FaucetState.Completed)
        {
            HideWaterEffects();
        }

        // 为有水烧杯添加点击监听（如果存在）
        SetupFilledBeakerClickHandler();
    }

    /// <summary>
    /// 为有水烧杯设置点击处理
    /// </summary>
    private void SetupFilledBeakerClickHandler()
    {
        if (filledBeakerObject == null) return;

        // 确保有 Collider2D
        Collider2D beakerCollider = filledBeakerObject.GetComponent<Collider2D>();
        if (beakerCollider == null)
        {
            beakerCollider = filledBeakerObject.AddComponent<BoxCollider2D>();
            Debug.Log("[FaucetController] 为有水烧杯添加了 BoxCollider2D");
        }

        // 添加点击处理组件（如果还没有）
        FilledBeakerClickHandler clickHandler = filledBeakerObject.GetComponent<FilledBeakerClickHandler>();
        if (clickHandler == null)
        {
            clickHandler = filledBeakerObject.AddComponent<FilledBeakerClickHandler>();
        }
        clickHandler.Initialize(this);
    }

    /// <summary>
    /// 点击水龙头时调用
    /// </summary>
    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleFaucetClick();
    }

    /// <summary>
    /// 处理水龙头点击
    /// </summary>
    public void HandleFaucetClick()
    {
        Debug.Log($"[FaucetController] 点击水龙头，当前状态: {currentState}");

        switch (currentState)
        {
            case FaucetState.Idle:
                TryStartFilling();
                break;

            case FaucetState.Filling:
                // 提示玩家去点击有水的烧杯
                ShowHint(alreadyFillingHint);
                break;

            case FaucetState.Completed:
                // 已完成，不做任何事
                Debug.Log("[FaucetController] 交互已完成");
                break;
        }
    }

    /// <summary>
    /// 尝试开始接水
    /// </summary>
    private void TryStartFilling()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[FaucetController] UIManager 未找到！");
            return;
        }

        // 检查是否选中了物品
        if (!UIManager.Instance.HasSelectedItem())
        {
            ShowHint(noItemHint);
            return;
        }

        // 获取选中的物品
        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            ShowHint(noItemHint);
            return;
        }

        // 检查是否是正确的物品
        if (requiredItem == null)
        {
            Debug.LogError("[FaucetController] 未设置 requiredItem！");
            return;
        }

        if (selectedItem.itemID != requiredItem.itemID)
        {
            ShowHint(wrongItemHint);
            return;
        }

        // 开始接水流程
        StartFilling();
    }

    /// <summary>
    /// 开始接水
    /// </summary>
    private void StartFilling()
    {
        Debug.Log("[FaucetController] 开始接水");

        // 消耗或取消选中空烧杯
        if (consumeRequiredItem)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        // 切换状态
        currentState = FaucetState.Filling;

        // 显示水流效果
        ShowWaterEffects();

        // 播放音效
        PlaySound(fillStartSound);
        StartLoopingWaterSound();

        // 触发事件
        OnFillStart?.Invoke();

        // 保存进度
        SaveProgress();
    }

    /// <summary>
    /// 显示水流效果和有水烧杯
    /// </summary>
    private void ShowWaterEffects()
    {
        if (waterFlowObject != null)
        {
            waterFlowObject.SetActive(true);
            Debug.Log("[FaucetController] 显示水流");
        }

        if (filledBeakerObject != null)
        {
            filledBeakerObject.SetActive(true);
            Debug.Log("[FaucetController] 显示有水烧杯");
        }
    }

    /// <summary>
    /// 隐藏水流效果和有水烧杯
    /// </summary>
    private void HideWaterEffects()
    {
        if (waterFlowObject != null)
        {
            waterFlowObject.SetActive(false);
        }

        if (filledBeakerObject != null)
        {
            filledBeakerObject.SetActive(false);
        }
    }

    /// <summary>
    /// 点击有水烧杯时调用（由 FilledBeakerClickHandler 调用）
    /// </summary>
    public void OnFilledBeakerClicked()
    {
        if (currentState != FaucetState.Filling)
        {
            Debug.Log("[FaucetController] 当前状态不是 Filling，忽略点击");
            return;
        }

        Debug.Log("[FaucetController] 点击有水烧杯，尝试拾取");

        // 检查是否设置了结果物品
        if (resultItem == null)
        {
            Debug.LogError("[FaucetController] 未设置 resultItem！");
            return;
        }

        // 检查背包系统
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[FaucetController] InventorySystem 未找到！");
            return;
        }

        // 添加有水烧杯到背包
        bool added = InventorySystem.Instance.AddItem(resultItem);
        if (!added)
        {
            Debug.LogWarning("[FaucetController] 背包已满，无法拾取");
            ShowHint("背包已满");
            return;
        }

        Debug.Log($"[FaucetController] 拾取成功: {resultItem.displayName}");

        // 完成交互
        CompleteInteraction();
    }

    /// <summary>
    /// 完成整个交互流程
    /// </summary>
    private void CompleteInteraction()
    {
        Debug.Log("[FaucetController] 交互完成");

        // 切换状态
        currentState = FaucetState.Completed;

        // 停止水流音效
        StopLoopingWaterSound();

        // 播放拾取音效
        PlaySound(pickupSound);

        // 隐藏水流和有水烧杯
        HideWaterEffects();

        // 触发事件
        OnBeakerCollected?.Invoke();
        OnInteractionComplete?.Invoke();

        // 保存进度
        SaveProgress();
    }

    // ============ 音效相关 ============

    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    private void StartLoopingWaterSound()
    {
        if (string.IsNullOrEmpty(waterFlowSound)) return;
        if (AudioManager.Instance == null) return;

        // 创建循环音效（简化实现，实际可能需要 AudioManager 支持）
        // 这里假设 waterFlowSound 会自然循环或足够长
        AudioManager.Instance.PlaySFX(waterFlowSound);
    }

    private void StopLoopingWaterSound()
    {
        // 如果使用了循环音效，在这里停止
        // 具体实现取决于 AudioManager 的功能
    }

    // ============ 提示信息 ============

    private void ShowHint(string message)
    {
        Debug.Log($"[FaucetController] 提示: {message}");

        // 如果有 UI 提示系统，可以在这里调用
        // 例如：UIManager.Instance?.ShowHint(message);
    }

    // ============ 存档相关 ============

    private void SaveProgress()
    {
        SaveLoadSystem.Instance?.SaveGame();
    }

    /// <summary>
    /// 获取当前状态（用于存档）
    /// </summary>
    public int GetStateForSave()
    {
        return (int)currentState;
    }

    /// <summary>
    /// 恢复状态（用于读档）
    /// </summary>
    public void RestoreState(int stateIndex)
    {
        currentState = (FaucetState)stateIndex;

        switch (currentState)
        {
            case FaucetState.Idle:
                HideWaterEffects();
                break;

            case FaucetState.Filling:
                ShowWaterEffects();
                StartLoopingWaterSound();
                break;

            case FaucetState.Completed:
                HideWaterEffects();
                break;
        }

        Debug.Log($"[FaucetController] 状态已恢复: {currentState}");
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"faucet_{GetInstanceID()}";
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 在编辑器中显示关联物体的连线
        Gizmos.color = Color.cyan;

        if (waterFlowObject != null)
        {
            Gizmos.DrawLine(transform.position, waterFlowObject.transform.position);
            Gizmos.DrawWireSphere(waterFlowObject.transform.position, 0.2f);
        }

        if (filledBeakerObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, filledBeakerObject.transform.position);
            Gizmos.DrawWireSphere(filledBeakerObject.transform.position, 0.2f);
        }
    }
}

/// <summary>
/// 有水烧杯点击处理器 - 自动添加到有水烧杯物体上
/// </summary>
public class FilledBeakerClickHandler : MonoBehaviour
{
    private FaucetController faucetController;

    /// <summary>
    /// 初始化，关联到水龙头控制器
    /// </summary>
    public void Initialize(FaucetController controller)
    {
        faucetController = controller;
    }

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (faucetController != null)
        {
            faucetController.OnFilledBeakerClicked();
        }
        else
        {
            Debug.LogWarning("[FilledBeakerClickHandler] FaucetController 未设置！");
        }
    }
}