// Assets/Scripts/Player/InventorySystem.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 背包系统 - 管理玩家收集的物品
/// </summary>
public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("背包设置")]
    [SerializeField] private int inventorySize = 12;

    [Header("事件")]
    public UnityEvent OnInventoryChanged = new UnityEvent();

    // 背包槽位列表
    private List<InventorySlot> slots = new List<InventorySlot>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[InventorySystem] Duplicate detected! Destroying this component.");
            Destroy(this);
            return;
        }
        Instance = this;
        Debug.Log("[InventorySystem] Instance initialized.");

        // 初始化槽位
        InitializeSlots();
    }

    /// <summary>
    /// 初始化空槽位
    /// </summary>
    private void InitializeSlots()
    {
        slots.Clear();
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new InventorySlot());
        }
        Debug.Log($"[InventorySystem] Initialized {inventorySize} slots.");
    }

    // ============ 物品操作 ============

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    public bool AddItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogError("[InventorySystem] Cannot add null item!");
            return false;
        }

        // 查找第一个空槽位
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].item = item;
                Debug.Log($"[InventorySystem] Added item '{item.displayName}' to slot {i}");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        Debug.LogWarning("[InventorySystem] Inventory is full!");
        return false;
    }

    /// <summary>
    /// 添加物品到指定槽位
    /// </summary>
    public bool AddItemToSlot(ItemData item, int slotIndex)
    {
        if (item == null)
        {
            Debug.LogError("[InventorySystem] Cannot add null item!");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= slots.Count)
        {
            Debug.LogError($"[InventorySystem] Invalid slot index: {slotIndex}");
            return false;
        }

        slots[slotIndex].item = item;
        Debug.Log($"[InventorySystem] Added item '{item.displayName}' to slot {slotIndex}");
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 从背包移除物品
    /// </summary>
    public bool RemoveItem(ItemData item)
    {
        if (item == null) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].item.itemID == item.itemID)
            {
                slots[i].item = null;
                Debug.Log($"[InventorySystem] Removed item '{item.displayName}' from slot {i}");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 通过物品ID移除物品
    /// </summary>
    public bool RemoveItemByID(string itemID)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty && slots[i].item.itemID == itemID)
            {
                string name = slots[i].item.displayName;
                slots[i].item = null;
                Debug.Log($"[InventorySystem] Removed item '{name}' from slot {i}");
                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 交换两个槽位的物品
    /// </summary>
    public void SwapItems(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= slots.Count || indexB < 0 || indexB >= slots.Count)
        {
            Debug.LogError("[InventorySystem] Invalid slot index for swap!");
            return;
        }

        ItemData temp = slots[indexA].item;
        slots[indexA].item = slots[indexB].item;
        slots[indexB].item = temp;

        Debug.Log($"[InventorySystem] Swapped slots {indexA} and {indexB}");
        OnInventoryChanged?.Invoke();

        // ⭐ 交换后自动保存
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    /// <summary>
    /// 检查是否拥有某物品
    /// </summary>
    public bool HasItem(string itemID)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item.itemID == itemID)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 通过ID获取物品
    /// </summary>
    public ItemData GetItemByID(string itemID)
    {
        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item.itemID == itemID)
            {
                return slot.item;
            }
        }
        return null;
    }

    /// <summary>
    /// 获取所有槽位
    /// </summary>
    public List<InventorySlot> GetSlots()
    {
        return slots;
    }

    /// <summary>
    /// 清空背包
    /// </summary>
    public void ClearInventory()
    {
        foreach (var slot in slots)
        {
            slot.item = null;
        }
        Debug.Log("[InventorySystem] Inventory cleared.");
        OnInventoryChanged?.Invoke();
    }

    // ============ 存档相关方法（新版 - 保留位置） ============

    /// <summary>
    /// 获取所有槽位数据（用于保存，包含位置信息）
    /// </summary>
    public List<SlotSaveData> GetSlotsData()
    {
        List<SlotSaveData> slotsData = new List<SlotSaveData>();

        for (int i = 0; i < slots.Count; i++)
        {
            string itemID = slots[i].IsEmpty ? "" : slots[i].item.itemID;
            slotsData.Add(new SlotSaveData(i, itemID));
        }

        Debug.Log($"[InventorySystem] Retrieved {slotsData.Count} slots data for saving.");
        return slotsData;
    }

    /// <summary>
    /// 从槽位数据恢复背包（用于读取存档，保留位置信息）
    /// </summary>
    public void LoadFromSlotsData(List<SlotSaveData> slotsData)
    {
        // 先清空背包
        ClearInventory();

        if (slotsData == null || slotsData.Count == 0)
        {
            Debug.Log("[InventorySystem] No slots data to load.");
            return;
        }

        Debug.Log($"[InventorySystem] Loading {slotsData.Count} slots from save data...");

        foreach (SlotSaveData slotData in slotsData)
        {
            // 跳过空槽位
            if (string.IsNullOrEmpty(slotData.itemID))
            {
                continue;
            }

            // 确保槽位索引有效
            if (slotData.slotIndex < 0 || slotData.slotIndex >= slots.Count)
            {
                Debug.LogWarning($"[InventorySystem] Invalid slot index: {slotData.slotIndex}");
                continue;
            }

            // 加载物品数据
            ItemData item = LoadItemDataByID(slotData.itemID);

            if (item != null)
            {
                // 直接放入指定槽位（保留位置）
                slots[slotData.slotIndex].item = item;
                Debug.Log($"[InventorySystem] Loaded '{item.displayName}' to slot {slotData.slotIndex}");
            }
            else
            {
                Debug.LogWarning($"[InventorySystem] Could not load item with ID: {slotData.itemID}");
            }
        }

        // 触发UI更新
        OnInventoryChanged?.Invoke();
        Debug.Log("[InventorySystem] Inventory loaded from save data.");
    }

    /// <summary>
    /// 通过ID从Resources加载ItemData
    /// </summary>
    private ItemData LoadItemDataByID(string itemID)
    {
        // 方法1：直接用ID作为文件名
        ItemData item = Resources.Load<ItemData>($"Prefabs/Items/{itemID}");
        if (item != null) return item;

        // 方法2：加上 "_Item" 后缀
        item = Resources.Load<ItemData>($"Prefabs/Items/{itemID}_Item");
        if (item != null) return item;

        // 方法3：遍历所有 ItemData 查找匹配的 ID
        ItemData[] allItems = Resources.LoadAll<ItemData>("Prefabs/Items");
        foreach (var i in allItems)
        {
            if (i.itemID == itemID)
            {
                return i;
            }
        }

        Debug.LogWarning($"[InventorySystem] ItemData not found for ID: {itemID}");
        return null;
    }

    // ============ 旧版方法（保留兼容性） ============

    /// <summary>
    /// 获取背包中所有物品的ID列表（旧版，不保留位置）
    /// </summary>
    public List<string> GetAllItemIDs()
    {
        List<string> itemIDs = new List<string>();

        foreach (var slot in slots)
        {
            if (!slot.IsEmpty && slot.item != null)
            {
                itemIDs.Add(slot.item.itemID);
            }
        }

        return itemIDs;
    }

    /// <summary>
    /// 从物品ID列表恢复背包（旧版，不保留位置）
    /// </summary>
    public void LoadFromItemIDs(List<string> itemIDs)
    {
        ClearInventory();

        if (itemIDs == null || itemIDs.Count == 0)
        {
            return;
        }

        foreach (string itemID in itemIDs)
        {
            ItemData item = LoadItemDataByID(itemID);
            if (item != null)
            {
                AddItem(item);
            }
        }
    }
}