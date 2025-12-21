// Assets/Scripts/GamePlay/InteractableObject.cs
using UnityEngine;

/// <summary>
/// 可交互物体组件
/// 支持三种交互类型：拾取物品、放大查看、触发事件
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("基本信息")]
    [Tooltip("物体的唯一标识符（用于存档）")]
    public string objectID;

    [Tooltip("物体的显示名称")]
    public string displayName;

    [Header("交互设置")]
    [Tooltip("选择物体的交互类型")]
    public InteractionType interactionType = InteractionType.Pickup;

    [Header("拾取物品设置 (Pickup)")]
    [Tooltip("分配给这个物体的物品数据")]
    public ItemData item;

    [Tooltip("是否可以被拾取")]
    public bool isPickupable = true;

    [Header("放大视图设置 (ZoomView)")]
    [Tooltip("选择进入的放大视图")]
    public GameManager.ViewState associatedZoomView;

    [Header("音效设置（可选）")]
    [Tooltip("拾取物品时播放的音效")]
    public string pickupSoundName = "Audio/SFX/item_pickup";

    [Tooltip("进入放大视图时播放的音效")]
    public string zoomSoundName = "Audio/SFX/zoom_in";

    [Tooltip("触发事件时播放的音效")]
    public string triggerSoundName = "Audio/SFX/trigger";

    [Header("触发事件设置 (Trigger)")]
    [Tooltip("触发后是否禁用此物体")]
    public bool disableAfterTrigger = false;

    /// <summary>
    /// 交互类型枚举
    /// </summary>
    public enum InteractionType
    {
        Pickup,      // 拾取物品
        ZoomView,    // 放大查看
        Trigger      // 触发事件
    }

    /// <summary>
    /// 执行交互 - 由 InteractionSystem 调用
    /// </summary>
    public void Interact()
    {
        Debug.Log($"[InteractableObject] 与物体 '{displayName}' (ID: {objectID}) 进行交互，类型: {interactionType}");

        switch (interactionType)
        {
            case InteractionType.Pickup:
                HandlePickup();
                break;

            case InteractionType.ZoomView:
                HandleZoomView();
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
    /// 处理拾取物品逻辑
    /// </summary>
    private void HandlePickup()
    {
        if (!isPickupable)
        {
            Debug.Log($"[InteractableObject] 物体 '{displayName}' 无法被拾取（isPickupable = false）");
            return;
        }

        if (item == null)
        {
            Debug.LogError($"[InteractableObject] 物体 '{displayName}' 没有分配 ItemData！请在 Inspector 中设置。");
            return;
        }

        // 调用背包系统添加物品
        bool added = InventorySystem.Instance.AddItem(item);

        if (added)
        {
            Debug.Log($"[InteractableObject] 成功拾取物品: {item.displayName}");

            // 播放拾取音效
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSoundName))
            {
                AudioManager.Instance.PlaySFX(pickupSoundName);
            }

            // 禁用物体（表示已拾取）
            gameObject.SetActive(false);

            // ⭐ 实时保存游戏进度
            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.OnItemPickedUp(objectID);
            }
        }
        else
        {
            Debug.LogWarning($"[InteractableObject] 无法拾取物品 '{item.displayName}'，背包可能已满！");
        }
    }

    /// <summary>
    /// 处理放大视图逻辑
    /// </summary>
    private void HandleZoomView()
    {
        string viewStateName = associatedZoomView.ToString();

        if (!viewStateName.Contains("Zoom"))
        {
            Debug.LogError($"[InteractableObject] 物体 '{displayName}' 的 Associated Zoom View 设置错误！" +
                          $"当前值: {associatedZoomView}，请选择包含 'Zoom' 的视图状态。");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[InteractableObject] GameManager 不存在，无法切换视图！");
            return;
        }

        Debug.Log($"[InteractableObject] 进入放大视图: {associatedZoomView}");
        GameManager.Instance.EnterZoomView(associatedZoomView);

        // 播放放大音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(zoomSoundName))
        {
            AudioManager.Instance.PlaySFX(zoomSoundName);
        }
    }

    /// <summary>
    /// 处理触发事件逻辑
    /// </summary>
    private void HandleTrigger()
    {
        Debug.Log($"[InteractableObject] 触发了事件: {displayName} (ID: {objectID})");

        // 播放触发音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(triggerSoundName))
        {
            AudioManager.Instance.PlaySFX(triggerSoundName);
        }

        // 调用自定义事件处理
        OnTriggered();

        // 如果设置了触发后禁用，则禁用物体
        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);

            // 实时保存
            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.SaveGame();
            }
        }
    }

    /// <summary>
    /// 触发事件的虚方法，子类可以重写实现特定逻辑
    /// 例如：开门、触发机关、播放对话等
    /// </summary>
    protected virtual void OnTriggered()
    {
        // 默认实现为空
        // 子类可以重写此方法来实现特定的触发逻辑
    }

    // ============ 编辑器辅助 ============

    /// <summary>
    /// 编辑器中在 Scene 视图绘制物体信息
    /// </summary>
    private void OnDrawGizmosSelected()
    {
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

        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    /// <summary>
    /// 编辑器验证设置
    /// </summary>
    private void OnValidate()
    {
        // 自动生成 objectID（如果为空）
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"{gameObject.name}_{GetInstanceID()}";
        }

        // 验证 Pickup 类型必须有 ItemData
        if (interactionType == InteractionType.Pickup && item == null)
        {
            Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为 Pickup，但没有分配 ItemData！", this);
        }

        // 验证 ZoomView 类型的视图状态
        if (interactionType == InteractionType.ZoomView)
        {
            string viewStateName = associatedZoomView.ToString();
            if (!viewStateName.Contains("Zoom"))
            {
                Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为 ZoomView，但 Associated Zoom View 设置可能不正确（当前: {associatedZoomView}）", this);
            }
        }
    }
}