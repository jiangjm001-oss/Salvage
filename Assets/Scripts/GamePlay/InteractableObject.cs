// Assets/Scripts/GamePlay/InteractableObject.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 可交互物体组件
/// 支持八种交互类型：拾取、放大、触发、需要物品、物品合成、状态切换、物体切换、容器
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

    // ============ 拾取物品设置 (Pickup) ============
    [Header("拾取物品设置 (Pickup)")]
    [Tooltip("分配给这个物体的物品数据")]
    public ItemData item;

    [Tooltip("是否可以被拾取")]
    public bool isPickupable = true;

    [Tooltip("是否已被拾取（用于存档和容器判断）")]
    [HideInInspector]
    public bool hasBeenPickedUp = false;

    // ============ 放大视图设置 (ZoomView) ============
    [Header("放大视图设置 (ZoomView)")]
    [Tooltip("选择进入的放大视图")]
    public GameManager.ViewState associatedZoomView;

    // ============ 音效设置 ============
    [Header("音效设置（可选）")]
    [Tooltip("拾取物品时播放的音效")]
    public string pickupSoundName = "Audio/SFX/item_pickup";

    [Tooltip("进入放大视图时播放的音效")]
    public string zoomSoundName = "Audio/SFX/zoom_in";

    [Tooltip("触发事件时播放的音效")]
    public string triggerSoundName = "Audio/SFX/trigger";

    // ============ 触发事件设置 (Trigger) ============
    [Header("触发事件设置 (Trigger)")]
    [Tooltip("触发后是否禁用此物体")]
    public bool disableAfterTrigger = false;

    // ============ 条件触发设置 (RequireItem) ============
    [Header("条件触发设置 (RequireItem)")]
    [Tooltip("需要使用的物品（直接拖入 ItemData）")]
    public ItemData requiredItem;

    [Tooltip("交互成功后是否消耗该物品")]
    public bool consumeItemOnUse = true;

    [Tooltip("未选中任何物品时的提示")]
    public string noItemHint = "需要用什么东西...";

    [Tooltip("选中了错误物品时的提示")]
    public string wrongItemHint = "这个东西在这里没有用...";

    [Tooltip("成功使用物品后触发的事件")]
    public UnityEvent OnItemUsedSuccess;

    [Tooltip("成功使用后播放的音效")]
    public string itemUsedSoundName = "Audio/SFX/item_used";

    // ============ 物品合成设置 (ItemCombine) ============
    [Header("物品合成设置 (ItemCombine)")]
    [Tooltip("合成需要的物品（直接拖入 ItemData）")]
    public ItemData combineRequiredItem;

    [Tooltip("合成产出的物品（放入背包）")]
    public ItemData combineResultItem;

    [Tooltip("合成后是否消耗手中的物品")]
    public bool consumeCombineItem = true;

    [Tooltip("合成后是否禁用此场景物体（如水被用完）")]
    public bool disableAfterCombine = false;

    [Tooltip("合成成功后触发的事件")]
    public UnityEvent OnCombineSuccess;

    [Tooltip("合成成功播放的音效")]
    public string combineSoundName = "Audio/SFX/combine";

    // ============ 状态切换设置 (StateSwitch) ============
    [Header("状态切换设置 (StateSwitch)")]
    [Tooltip("状态切换需要的物品（直接拖入 ItemData）")]
    public ItemData switchRequiredItem;

    [Tooltip("切换后显示的精灵图（图片B）")]
    public Sprite switchedSprite;

    [Tooltip("切换后是否消耗物品")]
    public bool consumeSwitchItem = true;

    [Tooltip("是否已切换状态（用于存档）")]
    [HideInInspector]
    public bool hasStateSwitch = false;

    [Tooltip("状态切换成功后触发的事件")]
    public UnityEvent OnStateSwitchSuccess;

    [Tooltip("状态切换播放的音效")]
    public string stateSwitchSoundName = "Audio/SFX/state_switch";

    // ============ 物体切换设置 (ObjectSwap) ============
    [Header("物体切换设置 (ObjectSwap)")]
    [Tooltip("切换到的目标物体（如：柜门关 → 柜门开）")]
    public GameObject swapTargetObject;

    [Tooltip("首次切换需要的物品（留空则无条件切换）")]
    public ItemData swapRequiredItem;

    [Tooltip("是否消耗切换物品")]
    public bool consumeSwapItem = true;

    [Tooltip("是否已经解锁（首次使用物品后变为true）")]
    [HideInInspector]
    public bool isSwapUnlocked = false;

    [Tooltip("切换成功后触发的事件")]
    public UnityEvent OnSwapSuccess;

    [Tooltip("物体切换播放的音效")]
    public string swapSoundName = "Audio/SFX/swap";

    // ============ ⭐ 容器设置 (Container) ============
    [Header("容器设置 (Container)")]
    [Tooltip("关闭状态的精灵图")]
    public Sprite containerClosedSprite;

    [Tooltip("打开状态的精灵图")]
    public Sprite containerOpenedSprite;

    [Tooltip("容器内的物品（可多个）")]
    public GameObject[] containedObjects;

    [Tooltip("首次打开需要的物品（留空则无条件打开）")]
    public ItemData containerRequiredItem;

    [Tooltip("是否消耗开启物品")]
    public bool consumeContainerItem = true;

    [Tooltip("是否已解锁（用过物品后无需再用）")]
    [HideInInspector]
    public bool isContainerUnlocked = false;

    [Tooltip("当前是否打开")]
    [HideInInspector]
    public bool isContainerOpen = false;

    [Tooltip("打开容器的音效")]
    public string containerOpenSound = "Audio/SFX/container_open";

    [Tooltip("关闭容器的音效")]
    public string containerCloseSound = "Audio/SFX/container_close";

    // 缓存
    private Sprite originalSprite;
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// 交互类型枚举
    /// </summary>
    public enum InteractionType
    {
        Pickup,      // 拾取物品
        ZoomView,    // 放大查看
        Trigger,     // 触发事件
        RequireItem, // 需要特定物品才能交互
        ItemCombine, // 物品合成
        StateSwitch, // 状态切换（单向）
        ObjectSwap,  // 物体切换（双向，两个物体）
        Container    // ⭐ 容器（单物体，开关状态 + 内部物品）
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
    }

    /// <summary>
    /// 执行交互
    /// </summary>
    public void Interact()
    {
        Debug.Log($"[InteractableObject] 与 '{displayName}' (ID: {objectID}) 交互，类型: {interactionType}");

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
            case InteractionType.RequireItem:
                HandleRequireItem();
                break;
            case InteractionType.ItemCombine:
                HandleItemCombine();
                break;
            case InteractionType.StateSwitch:
                HandleStateSwitch();
                break;
            case InteractionType.ObjectSwap:
                HandleObjectSwap();
                break;
            case InteractionType.Container:
                HandleContainer();
                break;
        }
    }

    // ============ Pickup ============
    private void HandlePickup()
    {
        if (!isPickupable) return;
        if (item == null)
        {
            Debug.LogError($"[InteractableObject] '{displayName}' 没有分配 ItemData！");
            return;
        }

        bool added = InventorySystem.Instance.AddItem(item);
        if (added)
        {
            Debug.Log($"[InteractableObject] 拾取: {item.displayName}");

            if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSoundName))
            {
                AudioManager.Instance.PlaySFX(pickupSoundName);
            }

            // ⭐ 标记为已拾取（供容器判断）
            hasBeenPickedUp = true;

            gameObject.SetActive(false);

            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.OnItemPickedUp(objectID);
            }
        }
    }

    // ============ ZoomView ============
    private void HandleZoomView()
    {
        string viewStateName = associatedZoomView.ToString();
        bool isValidZoomView = viewStateName.IndexOf("zoom", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (!isValidZoomView)
        {
            Debug.LogError($"[InteractableObject] '{displayName}' 的 ZoomView 设置错误！");
            return;
        }

        if (GameManager.Instance == null) return;

        GameManager.Instance.EnterZoomView(associatedZoomView);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(zoomSoundName))
        {
            AudioManager.Instance.PlaySFX(zoomSoundName);
        }
    }

    // ============ Trigger ============
    private void HandleTrigger()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(triggerSoundName))
        {
            AudioManager.Instance.PlaySFX(triggerSoundName);
        }

        OnTriggered();

        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);
            SaveLoadSystem.Instance?.SaveGame();
        }
    }

    // ============ RequireItem ============
    private void HandleRequireItem()
    {
        if (UIManager.Instance == null || requiredItem == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return;
        if (selectedItem.itemID != requiredItem.itemID) return;

        Debug.Log($"[InteractableObject] ✓ 使用 '{selectedItem.displayName}' 于 '{displayName}'");

        if (consumeItemOnUse)
            UIManager.Instance.ConsumeSelectedItem();
        else
            UIManager.Instance.DeselectItem();

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(itemUsedSoundName))
        {
            AudioManager.Instance.PlaySFX(itemUsedSoundName);
        }

        OnItemUsedSuccess?.Invoke();

        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);
        }

        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ ItemCombine ============
    private void HandleItemCombine()
    {
        if (UIManager.Instance == null || combineRequiredItem == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return;
        if (selectedItem.itemID != combineRequiredItem.itemID) return;
        if (combineResultItem == null) return;

        Debug.Log($"[InteractableObject] ✓ 合成: {selectedItem.displayName} + {displayName} = {combineResultItem.displayName}");

        if (consumeCombineItem)
            UIManager.Instance.ConsumeSelectedItem();
        else
            UIManager.Instance.DeselectItem();

        InventorySystem.Instance.AddItem(combineResultItem);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(combineSoundName))
        {
            AudioManager.Instance.PlaySFX(combineSoundName);
        }

        OnCombineSuccess?.Invoke();

        if (disableAfterCombine)
        {
            gameObject.SetActive(false);
        }

        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ StateSwitch ============
    private void HandleStateSwitch()
    {
        if (hasStateSwitch)
        {
            OnStateSwitchSuccess?.Invoke();
            return;
        }

        if (UIManager.Instance == null || switchRequiredItem == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return;
        if (selectedItem.itemID != switchRequiredItem.itemID) return;
        if (switchedSprite == null) return;

        Debug.Log($"[InteractableObject] ✓ 状态切换: '{displayName}'");

        if (consumeSwitchItem)
            UIManager.Instance.ConsumeSelectedItem();
        else
            UIManager.Instance.DeselectItem();

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = switchedSprite;
        }

        hasStateSwitch = true;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(stateSwitchSoundName))
        {
            AudioManager.Instance.PlaySFX(stateSwitchSoundName);
        }

        OnStateSwitchSuccess?.Invoke();
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ ObjectSwap ============
    private void HandleObjectSwap()
    {
        if (swapTargetObject == null) return;

        // 已解锁或不需要物品 → 直接切换
        if (isSwapUnlocked || swapRequiredItem == null)
        {
            PerformSwap();
            return;
        }

        // 需要物品
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return;
        if (selectedItem.itemID != swapRequiredItem.itemID) return;

        Debug.Log($"[InteractableObject] ✓ 解锁 '{displayName}'");

        if (consumeSwapItem)
            UIManager.Instance.ConsumeSelectedItem();
        else
            UIManager.Instance.DeselectItem();

        isSwapUnlocked = true;

        // 同步解锁目标
        InteractableObject target = swapTargetObject.GetComponent<InteractableObject>();
        if (target != null)
        {
            target.isSwapUnlocked = true;
        }

        PerformSwap();
    }

    private void PerformSwap()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(swapSoundName))
        {
            AudioManager.Instance.PlaySFX(swapSoundName);
        }

        gameObject.SetActive(false);
        swapTargetObject.SetActive(true);

        OnSwapSuccess?.Invoke();
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ ⭐ Container ============
    private void HandleContainer()
    {
        // 检查是否需要物品才能首次打开
        if (!isContainerOpen && !isContainerUnlocked && containerRequiredItem != null)
        {
            if (UIManager.Instance == null) return;

            ItemData selectedItem = UIManager.Instance.GetSelectedItem();
            if (selectedItem == null) return;
            if (selectedItem.itemID != containerRequiredItem.itemID) return;

            Debug.Log($"[InteractableObject] ✓ 解锁容器 '{displayName}'");

            if (consumeContainerItem)
                UIManager.Instance.ConsumeSelectedItem();
            else
                UIManager.Instance.DeselectItem();

            isContainerUnlocked = true;
        }

        // 需要物品但未解锁，且当前关闭 → 无法打开
        if (!isContainerOpen && !isContainerUnlocked && containerRequiredItem != null)
        {
            return;
        }

        // 切换开关状态
        isContainerOpen = !isContainerOpen;

        if (isContainerOpen)
        {
            OpenContainer();
        }
        else
        {
            CloseContainer();
        }

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void OpenContainer()
    {
        Debug.Log($"[InteractableObject] 打开容器: '{displayName}'");

        // 切换精灵图
        if (spriteRenderer != null && containerOpenedSprite != null)
        {
            spriteRenderer.sprite = containerOpenedSprite;
        }

        // 显示内部物品（只显示未被拾取的）
        if (containedObjects != null)
        {
            foreach (var obj in containedObjects)
            {
                if (obj == null) continue;

                // ⭐ 检查是否已被拾取
                InteractableObject interactable = obj.GetComponent<InteractableObject>();
                if (interactable != null && interactable.hasBeenPickedUp)
                {
                    // 已拾取，不显示
                    continue;
                }

                obj.SetActive(true);
            }
        }

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(containerOpenSound))
        {
            AudioManager.Instance.PlaySFX(containerOpenSound);
        }
    }

    private void CloseContainer()
    {
        Debug.Log($"[InteractableObject] 关闭容器: '{displayName}'");

        // 切换精灵图
        if (spriteRenderer != null && containerClosedSprite != null)
        {
            spriteRenderer.sprite = containerClosedSprite;
        }

        // 隐藏内部物品
        if (containedObjects != null)
        {
            foreach (var obj in containedObjects)
            {
                if (obj == null) continue;
                obj.SetActive(false);
            }
        }

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(containerCloseSound))
        {
            AudioManager.Instance.PlaySFX(containerCloseSound);
        }
    }

    // ============ 状态恢复 ============

    public void RestoreStateSwitch(bool switched)
    {
        hasStateSwitch = switched;
        if (switched && spriteRenderer != null && switchedSprite != null)
        {
            spriteRenderer.sprite = switchedSprite;
        }
    }

    public void RestoreSwapUnlocked(bool unlocked)
    {
        isSwapUnlocked = unlocked;
    }

    /// <summary>
    /// 恢复容器状态（供存档系统调用）
    /// </summary>
    public void RestoreContainerState(bool unlocked, bool open)
    {
        isContainerUnlocked = unlocked;
        isContainerOpen = open;

        if (spriteRenderer != null)
        {
            if (open && containerOpenedSprite != null)
            {
                spriteRenderer.sprite = containerOpenedSprite;
            }
            else if (!open && containerClosedSprite != null)
            {
                spriteRenderer.sprite = containerClosedSprite;
            }
        }

        // 恢复内部物品显示状态
        if (containedObjects != null)
        {
            foreach (var obj in containedObjects)
            {
                if (obj == null) continue;

                if (open)
                {
                    // 打开状态：只显示未被拾取的
                    InteractableObject interactable = obj.GetComponent<InteractableObject>();
                    if (interactable != null && interactable.hasBeenPickedUp)
                    {
                        obj.SetActive(false);
                    }
                    else
                    {
                        obj.SetActive(true);
                    }
                }
                else
                {
                    // 关闭状态：全部隐藏
                    obj.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 标记为已拾取（供存档系统调用）
    /// </summary>
    public void MarkAsPickedUp()
    {
        hasBeenPickedUp = true;
    }

    protected virtual void OnTriggered() { }

    // ============ 编辑器辅助 ============

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
            case InteractionType.RequireItem:
                Gizmos.color = Color.magenta;
                break;
            case InteractionType.ItemCombine:
                Gizmos.color = Color.cyan;
                break;
            case InteractionType.StateSwitch:
                Gizmos.color = new Color(1f, 0.5f, 0f);
                break;
            case InteractionType.ObjectSwap:
                Gizmos.color = new Color(0.5f, 0f, 1f);
                break;
            case InteractionType.Container:
                Gizmos.color = new Color(0f, 1f, 0.5f); // 青绿色
                break;
        }

        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // ObjectSwap 连线
        if (interactionType == InteractionType.ObjectSwap && swapTargetObject != null)
        {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.5f);
            Gizmos.DrawLine(transform.position, swapTargetObject.transform.position);
        }

        // Container 连线
        if (interactionType == InteractionType.Container && containedObjects != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
            foreach (var obj in containedObjects)
            {
                if (obj != null)
                {
                    Gizmos.DrawLine(transform.position, obj.transform.position);
                }
            }
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"{gameObject.name}_{GetInstanceID()}";
        }
    }
}