// Assets/Scripts/GamePlay/MirrorController.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 镜子控制器 - 处理镜子的动态显示逻辑
/// 功能：
/// 1. 根据镜子状态显示不同图片（脏镜子A、干净镜子B、特殊状态C等）
/// 2. 干净状态下，动态显示当前选中的物品
/// 3. 特定物品触发特殊状态切换
/// </summary>
public class MirrorController : MonoBehaviour
{
    // ============ 镜子状态 ============
    public enum MirrorState
    {
        Dirty,      // 脏镜子（初始状态，图片A）
        Clean,      // 干净镜子（显示主角，图片B）
        Special     // 特殊状态（如显示戒指效果，图片C）
    }

    [Header("镜子状态")]
    [Tooltip("当前镜子状态")]
    public MirrorState currentState = MirrorState.Dirty;

    [Header("镜子图片配置")]
    [Tooltip("脏镜子图片（状态A）")]
    public Sprite dirtySprite;

    [Tooltip("干净镜子图片（显示主角，状态B）")]
    public Sprite cleanSprite;

    [Tooltip("特殊状态图片（状态C）")]
    public Sprite specialSprite;

    [Header("动态物品显示")]
    [Tooltip("是否启用动态物品显示（仅在 Clean 状态下生效）")]
    public bool enableDynamicItemDisplay = true;

    [Tooltip("物品显示位置（子物体，用于显示选中物品的图标）")]
    public Transform itemDisplayPosition;

    [Tooltip("物品显示的缩放比例")]
    public float itemDisplayScale = 1f;

    [Header("特殊物品触发")]
    [Tooltip("触发特殊状态的物品列表")]
    public List<SpecialItemTrigger> specialItemTriggers = new List<SpecialItemTrigger>();

    [Header("事件")]
    [Tooltip("镜子状态改变时触发")]
    public UnityEvent<MirrorState> OnMirrorStateChanged;

    [Tooltip("显示特殊状态时触发")]
    public UnityEvent OnSpecialStateTriggered;

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private SpriteRenderer itemDisplayRenderer;
    private GameObject itemDisplayObject;
    private string lastDisplayedItemID = "";

    /// <summary>
    /// 特殊物品触发配置
    /// </summary>
    [System.Serializable]
    public class SpecialItemTrigger
    {
        [Tooltip("触发特殊状态的物品（直接拖入 ItemData）")]
        public ItemData triggerItem;  // ⭐ 改为 ItemData 引用

        [Tooltip("该物品触发的特殊精灵图")]
        public Sprite triggerSprite;

        [Tooltip("触发时播放的音效")]
        public string triggerSound;

        [Tooltip("触发后是否永久切换状态（否则只在选中时显示）")]
        public bool permanentSwitch = false;
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"[MirrorController] '{gameObject.name}' 没有 SpriteRenderer 组件！");
        }

        // 创建物品显示子物体
        SetupItemDisplay();
    }

    private void Start()
    {
        // 初始化镜子状态
        UpdateMirrorVisual();

        // 订阅背包变化事件
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged.AddListener(OnInventoryChanged);
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged.RemoveListener(OnInventoryChanged);
        }
    }

    private void Update()
    {
        // 只在干净状态下检测选中物品变化
        if (currentState == MirrorState.Clean && enableDynamicItemDisplay)
        {
            CheckSelectedItemChange();
        }
    }

    /// <summary>
    /// 设置物品显示子物体
    /// </summary>
    private void SetupItemDisplay()
    {
        // 如果没有指定显示位置，创建一个默认的
        if (itemDisplayPosition == null)
        {
            itemDisplayObject = new GameObject("ItemDisplay");
            itemDisplayObject.transform.SetParent(transform);
            itemDisplayObject.transform.localPosition = Vector3.zero;
            itemDisplayPosition = itemDisplayObject.transform;
        }
        else
        {
            itemDisplayObject = itemDisplayPosition.gameObject;
        }

        // 确保有 SpriteRenderer
        itemDisplayRenderer = itemDisplayObject.GetComponent<SpriteRenderer>();
        if (itemDisplayRenderer == null)
        {
            itemDisplayRenderer = itemDisplayObject.AddComponent<SpriteRenderer>();
        }

        // 设置排序层级（在镜子上方）
        itemDisplayRenderer.sortingOrder = spriteRenderer != null ? spriteRenderer.sortingOrder + 1 : 1;

        // 初始隐藏
        itemDisplayRenderer.enabled = false;
    }

    /// <summary>
    /// 检测选中物品变化
    /// </summary>
    private void CheckSelectedItemChange()
    {
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        string currentItemID = selectedItem != null ? selectedItem.itemID : "";

        // 如果选中物品没有变化，跳过
        if (currentItemID == lastDisplayedItemID) return;

        lastDisplayedItemID = currentItemID;

        // 检查是否是特殊物品
        if (selectedItem != null)
        {
            SpecialItemTrigger trigger = FindSpecialTrigger(selectedItem);
            if (trigger != null)
            {
                HandleSpecialItemSelected(trigger, selectedItem);
                return;
            }
        }

        // 普通物品显示
        UpdateItemDisplay(selectedItem);
    }

    /// <summary>
    /// 查找特殊物品触发器（通过 ItemData 比较）
    /// </summary>
    private SpecialItemTrigger FindSpecialTrigger(ItemData item)
    {
        if (item == null) return null;

        foreach (var trigger in specialItemTriggers)
        {
            // ⭐ 使用 ItemData 的 itemID 进行比较
            if (trigger.triggerItem != null && trigger.triggerItem.itemID == item.itemID)
            {
                return trigger;
            }
        }
        return null;
    }

    /// <summary>
    /// 处理特殊物品选中
    /// </summary>
    private void HandleSpecialItemSelected(SpecialItemTrigger trigger, ItemData item)
    {
        Debug.Log($"[MirrorController] 特殊物品选中: {item.displayName}");

        // 播放特殊音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(trigger.triggerSound))
        {
            AudioManager.Instance.PlaySFX(trigger.triggerSound);
        }

        // 如果是永久切换
        if (trigger.permanentSwitch)
        {
            SetMirrorState(MirrorState.Special);
            if (trigger.triggerSprite != null)
            {
                spriteRenderer.sprite = trigger.triggerSprite;
            }

            // 隐藏物品显示
            if (itemDisplayRenderer != null)
            {
                itemDisplayRenderer.enabled = false;
            }

            OnSpecialStateTriggered?.Invoke();

            // 保存进度
            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.SaveGame();
            }
        }
        else
        {
            // 临时显示特殊精灵图
            if (trigger.triggerSprite != null)
            {
                spriteRenderer.sprite = trigger.triggerSprite;
            }

            // 隐藏物品显示
            if (itemDisplayRenderer != null)
            {
                itemDisplayRenderer.enabled = false;
            }
        }
    }

    /// <summary>
    /// 更新物品显示
    /// </summary>
    private void UpdateItemDisplay(ItemData item)
    {
        if (itemDisplayRenderer == null) return;

        // 如果不在干净状态，不显示物品
        if (currentState != MirrorState.Clean)
        {
            itemDisplayRenderer.enabled = false;
            return;
        }

        // 恢复干净镜子的精灵图（如果之前显示的是特殊状态）
        if (spriteRenderer.sprite != cleanSprite && currentState == MirrorState.Clean)
        {
            spriteRenderer.sprite = cleanSprite;
        }

        if (item != null && item.icon != null)
        {
            itemDisplayRenderer.sprite = item.icon;
            itemDisplayRenderer.enabled = true;
            itemDisplayRenderer.transform.localScale = Vector3.one * itemDisplayScale;

            Debug.Log($"[MirrorController] 镜子显示物品: {item.displayName}");
        }
        else
        {
            itemDisplayRenderer.enabled = false;
            Debug.Log("[MirrorController] 镜子不显示物品（未选中或无图标）");
        }
    }

    /// <summary>
    /// 背包变化回调
    /// </summary>
    private void OnInventoryChanged()
    {
        // 背包变化时重新检查显示
        if (currentState == MirrorState.Clean)
        {
            lastDisplayedItemID = ""; // 强制刷新
            CheckSelectedItemChange();
        }
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 设置镜子状态
    /// </summary>
    public void SetMirrorState(MirrorState newState)
    {
        if (currentState == newState) return;

        MirrorState oldState = currentState;
        currentState = newState;

        Debug.Log($"[MirrorController] 镜子状态: {oldState} → {newState}");

        UpdateMirrorVisual();
        OnMirrorStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// 更新镜子视觉效果
    /// </summary>
    private void UpdateMirrorVisual()
    {
        if (spriteRenderer == null) return;

        switch (currentState)
        {
            case MirrorState.Dirty:
                spriteRenderer.sprite = dirtySprite;
                if (itemDisplayRenderer != null)
                {
                    itemDisplayRenderer.enabled = false;
                }
                break;

            case MirrorState.Clean:
                spriteRenderer.sprite = cleanSprite;
                // 检查是否需要显示选中物品
                if (enableDynamicItemDisplay)
                {
                    lastDisplayedItemID = ""; // 强制刷新
                    CheckSelectedItemChange();
                }
                break;

            case MirrorState.Special:
                spriteRenderer.sprite = specialSprite;
                if (itemDisplayRenderer != null)
                {
                    itemDisplayRenderer.enabled = false;
                }
                break;
        }
    }

    /// <summary>
    /// 清洁镜子（由外部调用，如 InteractableObject 的 OnStateSwitchSuccess 事件）
    /// </summary>
    public void CleanMirror()
    {
        SetMirrorState(MirrorState.Clean);
    }

    /// <summary>
    /// 重置镜子到脏状态
    /// </summary>
    public void ResetMirror()
    {
        SetMirrorState(MirrorState.Dirty);
        lastDisplayedItemID = "";
    }

    // ============ 存档相关 ============

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
    public void RestoreState(int state)
    {
        currentState = (MirrorState)state;
        UpdateMirrorVisual();
    }
}