// Assets/Scripts/GamePlay/InteractableObject.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 可交互物体组件
/// 支持六种交互类型：拾取物品、放大查看、触发事件、需要物品、物品合成、状态切换
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

    // ============ ⭐ 物品合成设置 (ItemCombine) ============
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

    // ============ ⭐ 状态切换设置 (StateSwitch) ============
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

    // 缓存原始精灵图（用于存档恢复）
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
        ItemCombine, // ⭐ 物品合成：选中物品A + 点击场景物品B = 获得物品C
        StateSwitch  // ⭐ 状态切换：使用物品改变物体外观/状态
    }

    private void Awake()
    {
        // 缓存 SpriteRenderer 和原始精灵图
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
        }
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

            case InteractionType.RequireItem:
                HandleRequireItem();
                break;

            case InteractionType.ItemCombine:
                HandleItemCombine();
                break;

            case InteractionType.StateSwitch:
                HandleStateSwitch();
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

        bool added = InventorySystem.Instance.AddItem(item);

        if (added)
        {
            Debug.Log($"[InteractableObject] 成功拾取物品: {item.displayName}");

            if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSoundName))
            {
                AudioManager.Instance.PlaySFX(pickupSoundName);
            }

            gameObject.SetActive(false);

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
        bool isValidZoomView = viewStateName.IndexOf("zoom", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (!isValidZoomView)
        {
            Debug.LogError($"[InteractableObject] 物体 '{displayName}' 的 Associated Zoom View 设置错误！" +
                          $"当前值: {associatedZoomView}，请选择包含 'zoom' 的视图状态。");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("[InteractableObject] GameManager 不存在，无法切换视图！");
            return;
        }

        Debug.Log($"[InteractableObject] 进入放大视图: {associatedZoomView}");
        GameManager.Instance.EnterZoomView(associatedZoomView);

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

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(triggerSoundName))
        {
            AudioManager.Instance.PlaySFX(triggerSoundName);
        }

        OnTriggered();

        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);

            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.SaveGame();
            }
        }
    }

    /// <summary>
    /// 处理需要物品的交互逻辑
    /// </summary>
    private void HandleRequireItem()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[InteractableObject] UIManager 不存在，无法检查选中物品！");
            return;
        }

        // ⭐ 检查是否配置了需要的物品
        if (requiredItem == null)
        {
            Debug.LogError($"[InteractableObject] '{displayName}' 没有配置 requiredItem！");
            return;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        if (selectedItem == null)
        {
            Debug.Log($"[InteractableObject] 点击了 '{displayName}'，但没有选中物品");
            return;
        }

        // ⭐ 使用 ItemData 的 itemID 进行比较
        if (selectedItem.itemID != requiredItem.itemID)
        {
            Debug.Log($"[InteractableObject] 物品 '{selectedItem.displayName}' 不能用于 '{displayName}'（需要: {requiredItem.displayName}）");
            return;
        }

        Debug.Log($"[InteractableObject] ✓ 成功使用 '{selectedItem.displayName}' 于 '{displayName}'");

        if (consumeItemOnUse)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(itemUsedSoundName))
        {
            AudioManager.Instance.PlaySFX(itemUsedSoundName);
        }

        OnItemUsedSuccess?.Invoke();

        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);
        }

        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    // ============ ⭐ 物品合成逻辑 ============
    /// <summary>
    /// 处理物品合成逻辑
    /// 例如：毛巾 + 水 = 浸湿的毛巾
    /// </summary>
    private void HandleItemCombine()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[InteractableObject] UIManager 不存在，无法检查选中物品！");
            return;
        }

        // ⭐ 检查是否配置了合成需要的物品
        if (combineRequiredItem == null)
        {
            Debug.LogError($"[InteractableObject] '{displayName}' 没有配置 combineRequiredItem！");
            return;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        // 情况1：没有选中任何物品
        if (selectedItem == null)
        {
            Debug.Log($"[InteractableObject] 点击了 '{displayName}'，但没有选中物品进行合成");
            return;
        }

        // 情况2：选中的物品不匹配（使用 ItemData 的 itemID 比较）
        if (selectedItem.itemID != combineRequiredItem.itemID)
        {
            Debug.Log($"[InteractableObject] 物品 '{selectedItem.displayName}' 无法与 '{displayName}' 合成（需要: {combineRequiredItem.displayName}）");
            return;
        }

        // 情况3：检查合成产物是否配置
        if (combineResultItem == null)
        {
            Debug.LogError($"[InteractableObject] '{displayName}' 没有配置合成产物 combineResultItem！");
            return;
        }

        // 合成成功！
        Debug.Log($"[InteractableObject] ✓ 合成成功: {selectedItem.displayName} + {displayName} = {combineResultItem.displayName}");

        // 1. 消耗手中的物品（如果需要）
        if (consumeCombineItem)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        // 2. 将合成产物添加到背包
        bool added = InventorySystem.Instance.AddItem(combineResultItem);
        if (!added)
        {
            Debug.LogWarning($"[InteractableObject] 无法添加合成产物 '{combineResultItem.displayName}'，背包可能已满！");
        }

        // 3. 播放合成音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(combineSoundName))
        {
            AudioManager.Instance.PlaySFX(combineSoundName);
        }

        // 4. 触发合成成功事件
        OnCombineSuccess?.Invoke();

        // 5. 如果设置了合成后禁用（如水被用完）
        if (disableAfterCombine)
        {
            gameObject.SetActive(false);
        }

        // 6. 保存进度
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    // ============ ⭐ 状态切换逻辑 ============
    /// <summary>
    /// 处理状态切换逻辑
    /// 例如：浸湿毛巾 + 脏镜子 = 干净镜子（图片从A切换到B）
    /// </summary>
    private void HandleStateSwitch()
    {
        // 如果已经切换过状态，可以选择不再响应或执行其他逻辑
        if (hasStateSwitch)
        {
            Debug.Log($"[InteractableObject] '{displayName}' 已经切换过状态");
            OnStateSwitchSuccess?.Invoke();
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("[InteractableObject] UIManager 不存在，无法检查选中物品！");
            return;
        }

        // ⭐ 检查是否配置了状态切换需要的物品
        if (switchRequiredItem == null)
        {
            Debug.LogError($"[InteractableObject] '{displayName}' 没有配置 switchRequiredItem！");
            return;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        // 情况1：没有选中任何物品
        if (selectedItem == null)
        {
            Debug.Log($"[InteractableObject] 点击了 '{displayName}'，但没有选中物品");
            return;
        }

        // 情况2：选中的物品不匹配（使用 ItemData 的 itemID 比较）
        if (selectedItem.itemID != switchRequiredItem.itemID)
        {
            Debug.Log($"[InteractableObject] 物品 '{selectedItem.displayName}' 无法用于 '{displayName}'（需要: {switchRequiredItem.displayName}）");
            return;
        }

        // 情况3：检查切换精灵图是否配置
        if (switchedSprite == null)
        {
            Debug.LogError($"[InteractableObject] '{displayName}' 没有配置切换后的精灵图 switchedSprite！");
            return;
        }

        // 切换成功！
        Debug.Log($"[InteractableObject] ✓ 状态切换成功: 使用 '{selectedItem.displayName}' 于 '{displayName}'");

        // 1. 消耗物品（如果需要）
        if (consumeSwitchItem)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        // 2. 切换精灵图
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = switchedSprite;
        }

        // 3. 标记状态已切换
        hasStateSwitch = true;

        // 4. 播放状态切换音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(stateSwitchSoundName))
        {
            AudioManager.Instance.PlaySFX(stateSwitchSoundName);
        }

        // 5. 触发状态切换成功事件
        OnStateSwitchSuccess?.Invoke();

        // 6. 保存进度
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    // ============ 状态恢复方法（供存档系统调用） ============

    /// <summary>
    /// 恢复状态切换（用于读取存档）
    /// </summary>
    public void RestoreStateSwitch(bool switched)
    {
        hasStateSwitch = switched;
        if (switched && spriteRenderer != null && switchedSprite != null)
        {
            spriteRenderer.sprite = switchedSprite;
        }
    }

    /// <summary>
    /// 触发事件的虚方法，子类可以重写实现特定逻辑
    /// </summary>
    protected virtual void OnTriggered()
    {
        // 默认实现为空
    }

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
        }

        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

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
            bool isValidZoomView = viewStateName.IndexOf("zoom", System.StringComparison.OrdinalIgnoreCase) >= 0;
            bool isWallView = viewStateName.StartsWith("Wall_");

            if (!isValidZoomView || isWallView)
            {
                Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为 ZoomView，但 Associated Zoom View 设置可能不正确", this);
            }
        }

        // ⭐ 验证 RequireItem 类型必须有 requiredItem
        if (interactionType == InteractionType.RequireItem && requiredItem == null)
        {
            Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为 RequireItem，但没有设置 Required Item！", this);
        }

        // ⭐ 验证 ItemCombine 类型
        if (interactionType == InteractionType.ItemCombine)
        {
            if (combineRequiredItem == null)
            {
                Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为 ItemCombine，但没有设置 Combine Required Item！", this);
            }
            if (combineResultItem == null)
            {
                Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为 ItemCombine，但没有设置 Combine Result Item！", this);
            }
        }

        // ⭐ 验证 StateSwitch 类型
        if (interactionType == InteractionType.StateSwitch)
        {
            if (switchRequiredItem == null)
            {
                Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为 StateSwitch，但没有设置 Switch Required Item！", this);
            }
            if (switchedSprite == null)
            {
                Debug.LogWarning($"[InteractableObject] 物体 '{gameObject.name}' 的交互类型为 StateSwitch，但没有设置 Switched Sprite！", this);
            }
        }
    }
}