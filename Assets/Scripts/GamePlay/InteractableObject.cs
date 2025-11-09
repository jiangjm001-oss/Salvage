// Assets/_Scripts/Gameplay/InteractableObject.cs
using UnityEngine;

/// <summary>
/// 可交互物体组件
/// 支持三种交互类型：拾取物品、放大查看、触发事件
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("物体基本信息")]
    [Tooltip("物体的唯一标识符")]
    public string objectID;

    [Tooltip("物体的显示名称")]
    public string displayName;

    [Header("交互类型设置")]
    [Tooltip("选择此物体的交互类型")]
    public InteractionType interactionType = InteractionType.Pickup;

    [Header("拾取物品设置 (Pickup)")]
    [Tooltip("此物体代表的物品数据（仅当交互类型为Pickup时使用）")]
    public ItemData item;

    [Tooltip("是否可以被拾取（仅当交互类型为Pickup时使用）")]
    public bool isPickupable = true;

    [Header("放大视图设置 (ZoomView)")]
    [Tooltip("选择这个物体关联的放大视图（仅当交互类型为ZoomView时使用）")]
    public GameManager.ViewState associatedZoomView;

    [Header("音效设置（可选）")]
    [Tooltip("拾取物品时播放的音效名称")]
    public string pickupSoundName = "item_pickup";

    [Tooltip("进入放大视图时播放的音效名称")]
    public string zoomSoundName = "zoom_in";

    [Tooltip("触发事件时播放的音效名称")]
    public string triggerSoundName = "trigger";

    [Header("触发事件设置 (Trigger)")]
    [Tooltip("触发后是否禁用此物体")]
    public bool disableAfterTrigger = false;

    /// <summary>
    /// 交互类型枚举
    /// </summary>
    public enum InteractionType
    {
        Pickup,      // 拾取物品到背包
        ZoomView,    // 放大查看
        Trigger      // 触发事件/机关
    }

    /// <summary>
    /// 主交互方法 - 由InteractionSystem调用
    /// </summary>
    public void Interact()
    {
        Debug.Log($"[InteractableObject] 玩家与物体 '{displayName}' (ID: {objectID}) 进行了交互。交互类型: {interactionType}");

        // 根据交互类型分发到不同的处理方法
        switch (interactionType)
        {
            case InteractionType.Pickup:
                HandlePickup();
                break;

            case InteractionType.ZoomView:
                // 应该调用 GameManager 的方法
                GameManager.Instance.EnterZoomView(associatedZoomView);
                break;

            case InteractionType.Trigger:
                HandleTrigger();
                break;

            default:
                Debug.LogWarning($"[InteractableObject] 未知的交互类型: {interactionType}");
                break;
        }
    }

    /// <summary>
    /// 处理拾取物品交互
    /// </summary>
    private void HandlePickup()
    {
        if (!isPickupable)
        {
            Debug.Log($"[InteractableObject] 物体 '{displayName}' 无法被拾取（isPickupable = false）。");
            return;
        }

        if (item == null)
        {
            Debug.LogError($"[InteractableObject] 物体 '{displayName}' 没有分配ItemData！请在Inspector中设置。");
            return;
        }

        // 调用背包系统添加物品
        InventorySystem.Instance.AddItem(item);

        Debug.Log($"[InteractableObject] 成功拾取物品: {item.displayName}");

        // 播放拾取音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSoundName))
        {
            AudioManager.Instance.PlaySFX(pickupSoundName);
        }

        // 从场景中移除这个物体
        gameObject.SetActive(false);
        // 或者使用 Destroy(gameObject); 如果不需要再次激活
    }

    /// <summary>
    /// 处理放大视图交互
    /// </summary>
    private void HandleZoomView()
    {
        // 验证是否是有效的放大视图枚举值
        string viewStateName = associatedZoomView.ToString();

        if (!viewStateName.Contains("Zoom"))
        {
            Debug.LogError($"[InteractableObject] 物体 '{displayName}' 的Associated Zoom View设置错误！" +
                          $"当前值: {associatedZoomView}，必须选择包含'Zoom'的视图状态。");
            return;
        }

        // 检查GameManager是否存在
        if (GameManager.Instance == null)
        {
            Debug.LogError("[InteractableObject] GameManager不存在！无法切换视图。");
            return;
        }

        // 切换到放大视图
        Debug.Log($"[InteractableObject] 进入放大视图: {associatedZoomView}");
        GameManager.Instance.EnterZoomView(associatedZoomView);

        // 播放放大音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(zoomSoundName))
        {
            AudioManager.Instance.PlaySFX(zoomSoundName);
        }
    }

    /// <summary>
    /// 处理触发事件交互
    /// </summary>
    private void HandleTrigger()
    {
        Debug.Log($"[InteractableObject] 触发了物体: {displayName} (ID: {objectID})");

        // 播放触发音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(triggerSoundName))
        {
            AudioManager.Instance.PlaySFX(triggerSoundName);
        }

        // 触发自定义事件（可以在子类中重写或通过UnityEvent扩展）
        OnTriggered();

        // 如果设置了触发后禁用，则禁用此物体
        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 触发事件的虚方法，可在子类中重写以实现特定逻辑
    /// 例如：打开门、激活机关、触发谜题等
    /// </summary>
    protected virtual void OnTriggered()
    {
        // 基类默认实现为空
        // 子类可以重写这个方法来实现特定的触发逻辑

        // 示例：如果有PuzzleManager，可以通知它
        // if (PuzzleManager.Instance != null)
        // {
        //     PuzzleManager.Instance.OnObjectTriggered(objectID);
        // }
    }

    /// <summary>
    /// 编辑器辅助：在Scene视图中显示物体信息
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 根据交互类型显示不同颜色的Gizmo
        switch (interactionType)
        {
            case InteractionType.Pickup:
                Gizmos.color = Color.green;
                break;
            case InteractionType.ZoomView:
                Gizmos.color = Color.blue;
                break;
            case InteractionType.Trigger:
                Gizmos.color = Color.yellow;
                break;
        }

        // 在物体位置绘制一个小球
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    /// <summary>
    /// 编辑器辅助：验证设置
    /// </summary>
    private void OnValidate()
    {
        // 在编辑器中实时验证设置
        if (interactionType == InteractionType.Pickup && item == null)
        {
            Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为Pickup，但没有设置ItemData！", this);
        }

        if (interactionType == InteractionType.ZoomView)
        {
            string viewStateName = associatedZoomView.ToString();
            if (!viewStateName.Contains("Zoom"))
            {
                Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为ZoomView，但Associated Zoom View设置可能不正确（当前: {associatedZoomView}）！", this);
            }
        }
    }
}