// Assets/Scripts/GamePlay/OilLampBController.cs
using UnityEngine;

/// <summary>
/// 煤油灯B控制器
/// 当煤油灯B被激活且玩家手持BlankNote时，点击会触发特殊效果：
/// - BlankNote从背包消失
/// - titleNote出现（可拾取）
/// 
/// 使用方法：挂在煤油灯B的GameObject上
/// </summary>
public class OilLampBController : MonoBehaviour
{
    [Header("物品设置")]
    [Tooltip("需要的物品（BlankNote的ItemData）")]
    public ItemData requiredItem;  // BlankNote

    [Tooltip("是否消耗物品")]
    public bool consumeItem = true;

    [Header("结果设置")]
    [Tooltip("触发后显示的物体（titleNote）")]
    public GameObject resultObject;  // titleNote

    [Header("音效（可选）")]
    public string burnSoundName = "";

    [Header("状态")]
    [Tooltip("是否已触发过（用于存档）")]
    public bool hasTriggered = false;

    private InteractableObject interactable;

    private void Awake()
    {
        interactable = GetComponent<InteractableObject>();
    }

    private void OnEnable()
    {
        // 煤油灯B激活时，检查是否已触发
        if (hasTriggered && resultObject != null)
        {
            // 已触发过，确保titleNote可见（如果还没被拾取）
            var titleInteractable = resultObject.GetComponent<InteractableObject>();
            if (titleInteractable != null && !titleInteractable.hasBeenPickedUp)
            {
                resultObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 在InteractableObject.Interact()之前检查特殊交互
    /// 需要通过事件或修改InteractableObject调用
    /// </summary>
    public bool TrySpecialInteraction()
    {
        // 已触发过，跳过
        if (hasTriggered) return false;

        // 检查是否有选中物品
        if (UIManager.Instance == null || requiredItem == null) return false;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return false;

        // 检查是否是正确的物品
        if (selectedItem.itemID != requiredItem.itemID) return false;

        // 执行特殊交互
        PerformSpecialInteraction();
        return true;
    }

    private void PerformSpecialInteraction()
    {
        Debug.Log($"[OilLampBController] 特殊交互触发：{requiredItem.displayName} → 燃烧");

        // 1. 消耗物品（BlankNote消失）
        if (consumeItem)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        // 2. 显示结果物体（titleNote出现）
        if (resultObject != null)
        {
            resultObject.SetActive(true);
        }

        // 3. 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(burnSoundName))
        {
            AudioManager.Instance.PlaySFX(burnSoundName);
        }

        // 4. 标记已触发
        hasTriggered = true;

        // 5. 保存
        SaveLoadSystem.Instance?.SaveGame();
    }

    /// <summary>
    /// 存档恢复
    /// </summary>
    public void RestoreState(bool triggered)
    {
        hasTriggered = triggered;

        if (hasTriggered && resultObject != null)
        {
            var titleInteractable = resultObject.GetComponent<InteractableObject>();
            if (titleInteractable != null && !titleInteractable.hasBeenPickedUp)
            {
                resultObject.SetActive(true);
            }
        }
    }
}