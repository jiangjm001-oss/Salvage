// Assets/Scripts/GamePlay/PickupAndSwapController.cs
using UnityEngine;

/// <summary>
/// 拾取并切换控制器
/// 点击时：拾取物品 + 自己消失 + 下一个物体出现
/// 
/// 使用场景：咖啡罐B（点击拾取咖啡豆，切换到咖啡罐C）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PickupAndSwapController : MonoBehaviour
{
    [Header("基本信息")]
    [Tooltip("唯一标识符（用于存档）")]
    public string objectID;

    [Tooltip("显示名称")]
    public string displayName;

    [Header("拾取设置")]
    [Tooltip("点击时拾取的物品")]
    public ItemData pickupItem;

    [Tooltip("拾取音效")]
    public string pickupSoundName = "Audio/SFX/item_pickup";

    [Header("切换设置")]
    [Tooltip("切换到的下一个物体")]
    public GameObject nextObject;

    [Header("状态（自动管理）")]
    [HideInInspector]
    public bool hasBeenUsed = false;

    private void OnMouseDown()
    {
        // 检查是否点击在UI上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Interact();
    }

    public void Interact()
    {
        if (hasBeenUsed) return;

        Debug.Log($"[PickupAndSwapController] 与 '{displayName}' 交互");

        // 1. 拾取物品
        if (pickupItem != null && InventorySystem.Instance != null)
        {
            bool added = InventorySystem.Instance.AddItem(pickupItem);
            if (added)
            {
                Debug.Log($"[PickupAndSwapController] 拾取: {pickupItem.displayName}");

                // 播放音效
                if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSoundName))
                {
                    AudioManager.Instance.PlaySFX(pickupSoundName);
                }
            }
            else
            {
                Debug.Log("[PickupAndSwapController] 背包已满，无法拾取");
                return; // 背包满了就不切换
            }
        }

        // 2. 显示下一个物体
        if (nextObject != null)
        {
            nextObject.SetActive(true);
            Debug.Log($"[PickupAndSwapController] 显示: {nextObject.name}");
        }

        // 3. 标记并隐藏自己
        hasBeenUsed = true;
        gameObject.SetActive(false);

        // 4. 保存
        SaveLoadSystem.Instance?.SaveGame();
    }

    /// <summary>
    /// 存档恢复
    /// </summary>
    public void RestoreState(bool used)
    {
        hasBeenUsed = used;
        if (hasBeenUsed)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"{gameObject.name}_{GetInstanceID()}";
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制到下一个物体的连线
        if (nextObject != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.7f); // 橙色
            Gizmos.DrawLine(transform.position, nextObject.transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
    }
}