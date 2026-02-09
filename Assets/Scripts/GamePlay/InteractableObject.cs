// Assets/Scripts/GamePlay/InteractableObject.cs
// 增强版 - 支持跨视图物品状态同步
// v1.1 - 修复：处理 GameObject 已经 inactive 的情况
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 可交互物体组件
/// 支持八种交互类型：拾取、放大、触发、需要物品、物品合成、状态切换、物体切换、容器
/// ★ 新增：跨视图状态同步功能
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("基本信息")]
    [Tooltip("物体的唯一标识符（用于存档和跨视图同步）")]
    public string objectID;

    [Tooltip("物体的显示名称")]
    public string displayName;

    [Header("交互设置")]
    [Tooltip("选择物体的交互类型")]
    public InteractionType interactionType = InteractionType.Pickup;

    // ============ ★ 跨视图同步设置 ============
    [Header("跨视图同步设置")]
    [Tooltip("是否监听其他同ID物品的状态变化（用于同一物品在不同视图中的同步）")]
    public bool syncWithSameID = true;

    [Tooltip("消失时是否播放动画")]
    public bool useDisappearAnimation = true;

    [Tooltip("消失动画持续时间")]
    [Range(0.1f, 1f)]
    public float disappearDuration = 0.25f;

    // ============ 拾取物品设置 (Pickup) ============
    [Header("拾取物品设置 (Pickup)")]
    [Tooltip("分配给这个物体的物品数据")]
    public ItemData item;

    [Tooltip("是否可以被拾取")]
    public bool isPickupable = true;

    [Tooltip("是否已被拾取（用于存档和容器判断）")]
    [HideInInspector]
    public bool hasBeenPickedUp = false;

    // ============ 放大视图设置 (ZoomView) - 简化版 ============
    [Header("放大视图设置 (ZoomView)")]
    [Tooltip("【推荐】直接拖入放大视图 GameObject")]
    public GameObject zoomViewTarget;

    [Tooltip("【旧版兼容】使用枚举选择（如果上面已拖入则忽略此项）")]
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

    public UnityEvent OnTrigger;

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

    // ============ 容器设置 (Container) ============
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

    // ============ 私有变量 ============
    private Sprite originalSprite;
    private SpriteRenderer spriteRenderer;
    private bool isDisappearing = false;
    private bool hasSyncedHidden = false;  // ★ 新增：标记是否已通过同步被隐藏
    private Vector3 originalScale;
    private Color originalColor;

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
        Container    // 容器（单物体，开关状态 + 内部物品）
    }

    // ============ 生命周期 ============

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSprite = spriteRenderer.sprite;
            originalColor = spriteRenderer.color;
        }
        originalScale = transform.localScale;
    }

    private void Start()
    {
        // ★ 注册到状态管理器并订阅事件
        if (!string.IsNullOrEmpty(objectID) && syncWithSameID)
        {
            // 注册初始状态
            if (WorldObjectStateManager.Instance != null)
            {
                WorldObjectStateManager.Instance.RegisterObject(objectID, gameObject.activeSelf);
            }

            // 订阅状态改变事件
            WorldObjectStateManager.OnObjectStateChanged += OnObjectStateChanged;

            // 检查初始状态（处理存档读取后的情况）
            CheckInitialState();
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (syncWithSameID)
        {
            WorldObjectStateManager.OnObjectStateChanged -= OnObjectStateChanged;
        }
    }

    /// <summary>
    /// ★ 关键：当物体被激活时检查是否应该隐藏
    /// 这处理了物体在 inactive 时收到拾取事件的情况
    /// </summary>
    private void OnEnable()
    {
        // 如果已被标记为同步隐藏，立即隐藏自己
        if (hasSyncedHidden || hasBeenPickedUp)
        {
            // 检查是否确实需要隐藏（可能是 Pickup 类型被拾取了）
            if (interactionType == InteractionType.Pickup)
            {
                Debug.Log($"[InteractableObject] '{displayName}' OnEnable 检测到已被拾取，隐藏自己");
                gameObject.SetActive(false);
                return;
            }
        }

        // 额外检查：从 WorldObjectStateManager 获取最新状态
        if (syncWithSameID && !string.IsNullOrEmpty(objectID) && WorldObjectStateManager.Instance != null)
        {
            if (WorldObjectStateManager.Instance.IsObjectPickedUp(objectID))
            {
                Debug.Log($"[InteractableObject] '{displayName}' OnEnable 从状态管理器检测到已被拾取，隐藏自己");
                hasBeenPickedUp = true;
                hasSyncedHidden = true;
                gameObject.SetActive(false);
            }
        }
    }

    // ============ ★ 状态同步 ============

    /// <summary>
    /// 检查初始状态（用于处理存档读取）
    /// </summary>
    private void CheckInitialState()
    {
        if (string.IsNullOrEmpty(objectID)) return;
        if (WorldObjectStateManager.Instance == null) return;

        // 如果状态管理器记录该物品已被拾取，标记自己
        if (WorldObjectStateManager.Instance.IsObjectPickedUp(objectID))
        {
            hasBeenPickedUp = true;
            hasSyncedHidden = true;

            // 如果当前是激活状态，隐藏自己
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }

            Debug.Log($"[InteractableObject] '{displayName}' 检测到已被拾取，标记为隐藏");
        }
    }

    /// <summary>
    /// 响应物品状态改变事件
    /// </summary>
    private void OnObjectStateChanged(string changedObjectID, bool isActive)
    {
        // 只响应相同ID的状态变化
        if (changedObjectID != objectID) return;

        // ★ 如果已经被处理过，直接返回
        if (hasSyncedHidden && !isActive) return;
        if (isDisappearing) return;

        Debug.Log($"[InteractableObject] '{displayName}' 收到同步事件: {changedObjectID} → {(isActive ? "激活" : "隐藏")}");

        if (!isActive)
        {
            // 物品被拾取/隐藏，同步隐藏自己
            hasBeenPickedUp = true;
            hasSyncedHidden = true;
            StartDisappear();
        }
        else
        {
            // 物品被激活（特殊情况）
            hasSyncedHidden = false;
            gameObject.SetActive(true);
            ResetAppearance();
        }
    }

    /// <summary>
    /// 开始消失动画
    /// </summary>
    private void StartDisappear()
    {
        if (isDisappearing) return;

        // ★ 关键修复：检查 GameObject 是否处于激活状态
        // 如果已经是 inactive，直接标记并返回，不尝试启动协程
        if (!gameObject.activeInHierarchy)
        {
            Debug.Log($"[InteractableObject] '{displayName}' 已经是 inactive 状态，直接标记为已隐藏");
            hasBeenPickedUp = true;
            hasSyncedHidden = true;
            return;
        }

        isDisappearing = true;

        if (useDisappearAnimation && spriteRenderer != null)
        {
            StartCoroutine(DisappearAnimation());
        }
        else
        {
            gameObject.SetActive(false);
            isDisappearing = false;
        }
    }

    /// <summary>
    /// 消失动画协程
    /// </summary>
    private System.Collections.IEnumerator DisappearAnimation()
    {
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / disappearDuration);

            // 使用缓入曲线
            float easeT = t * t;

            // 淡出 + 缩小
            if (spriteRenderer != null)
            {
                Color c = startColor;
                c.a = startColor.a * (1f - easeT);
                spriteRenderer.color = c;
            }
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, easeT);

            yield return null;
        }

        gameObject.SetActive(false);
        ResetAppearance();
        isDisappearing = false;
    }

    /// <summary>
    /// 重置外观
    /// </summary>
    private void ResetAppearance()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        transform.localScale = originalScale;
    }

    // ============ 交互入口 ============

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

            hasBeenPickedUp = true;
            hasSyncedHidden = true;

            // ★ 通知状态管理器（会同步到其他同ID物品）
            if (!string.IsNullOrEmpty(objectID) && WorldObjectStateManager.Instance != null)
            {
                WorldObjectStateManager.Instance.MarkAsPickedUp(objectID);
            }

            // 隐藏自己
            gameObject.SetActive(false);

            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.OnItemPickedUp(objectID);
            }
        }
    }

    // ============ ZoomView - 简化版 ============
    private void HandleZoomView()
    {
        if (GameManager.Instance == null) return;

        // ⭐【新方式】优先使用直接引用的 GameObject
        if (zoomViewTarget != null)
        {
            GameManager.Instance.EnterZoomViewDirect(zoomViewTarget);

            if (AudioManager.Instance != null && !string.IsNullOrEmpty(zoomSoundName))
            {
                AudioManager.Instance.PlaySFX(zoomSoundName);
            }

            Debug.Log($"[InteractableObject] 进入放大视图: {zoomViewTarget.name}");
            return;
        }

        // 【旧方式】使用枚举（兼容已有配置）
        string viewStateName = associatedZoomView.ToString();
        bool isValidZoomView = viewStateName.IndexOf("zoom", System.StringComparison.OrdinalIgnoreCase) >= 0;

        if (!isValidZoomView)
        {
            Debug.LogError($"[InteractableObject] '{displayName}' 的 ZoomView 设置错误！请拖入 zoomViewTarget 或选择正确的枚举");
            return;
        }

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

        OnTrigger?.Invoke();
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

        // ⭐ 新增：检查是否有特殊控制器需要优先处理
        var specialController = GetComponent<OilLampBController>();
        if (specialController != null && specialController.TrySpecialInteraction())
        {
            // 特殊交互已处理，不执行切换
            return;
        }

        if (isSwapUnlocked || swapRequiredItem == null)
        {
            PerformSwap();
            return;
        }

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

    // ============ Container ============
    private void HandleContainer()
    {
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

        if (!isContainerOpen && !isContainerUnlocked && containerRequiredItem != null)
        {
            return;
        }

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

        if (spriteRenderer != null && containerOpenedSprite != null)
        {
            spriteRenderer.sprite = containerOpenedSprite;
        }

        if (containedObjects != null)
        {
            foreach (var obj in containedObjects)
            {
                if (obj == null) continue;

                InteractableObject interactable = obj.GetComponent<InteractableObject>();
                if (interactable != null && interactable.hasBeenPickedUp)
                {
                    continue;
                }

                obj.SetActive(true);
            }
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(containerOpenSound))
        {
            AudioManager.Instance.PlaySFX(containerOpenSound);
        }
    }

    private void CloseContainer()
    {
        Debug.Log($"[InteractableObject] 关闭容器: '{displayName}'");

        if (spriteRenderer != null && containerClosedSprite != null)
        {
            spriteRenderer.sprite = containerClosedSprite;
        }

        if (containedObjects != null)
        {
            foreach (var obj in containedObjects)
            {
                if (obj == null) continue;
                obj.SetActive(false);
            }
        }

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

        if (containedObjects != null)
        {
            foreach (var obj in containedObjects)
            {
                if (obj == null) continue;

                if (open)
                {
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
                    obj.SetActive(false);
                }
            }
        }
    }

    public void MarkAsPickedUp()
    {
        hasBeenPickedUp = true;
        hasSyncedHidden = true;
    }

    protected virtual void OnTriggered() { }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"{gameObject.name}_{GetInstanceID()}";
        }
    }
}