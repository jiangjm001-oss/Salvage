// Assets/Scripts/Player/InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    [Header("背包设置")]
    public int inventorySize = 12; // 背包总容量

    // 使用新的数据结构
    private List<InventorySlot> slots;

    // 事件参数从 List<ItemData> 变为整个 InventorySystem
    public UnityEvent OnInventoryChanged = new UnityEvent();

    private void Awake()
    {
        Debug.Log("InventorySystem.Awake() called.");

        // --- 这是唯一需要的单例逻辑 ---
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("InventorySystem: Another instance already exists, destroying this one.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Debug.Log("InventorySystem.Instance has been set.");

        // 确保自己成为根对象并持久存在
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        InitializeInventory();
    }

    private void Start()
    {
        // 在Start时初始化背包格子
        //InitializeInventory();
        Debug.Log("InventorySystem.Start() called.");
    }

    private void InitializeInventory()
    {
        slots = new List<InventorySlot>();
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new InventorySlot(null, i));
        }
    }

    /// <summary>
    /// 向第一个空格子添加物品
    /// </summary>
    public void AddItem(ItemData itemData)
    {
        foreach (var slot in slots)
        {
            if (slot.IsEmpty)
            {
                slot.item = itemData;
                Debug.Log($"InventorySystem: Added '{itemData.displayName}' to slot {slot.index}.");
                OnInventoryChanged.Invoke(); // 通知UI更新
                return;
            }
        }
        Debug.LogWarning("InventorySystem: Inventory is full!");
    }

    /// <summary>
    /// 交换两个格子的物品
    /// </summary>
    public void SwapItems(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= inventorySize || indexB < 0 || indexB >= inventorySize) return;

        ItemData tempItem = slots[indexA].item;
        slots[indexA].item = slots[indexB].item;
        slots[indexB].item = tempItem;

        Debug.Log($"InventorySystem: Swapped items in slot {indexA} and {indexB}.");
        OnInventoryChanged.Invoke(); // 通知UI更新
    }

    /// <summary>
    /// 获取所有格子数据（供UI使用）
    /// </summary>
    public List<InventorySlot> GetSlots()
    {
        return slots;
    }
}