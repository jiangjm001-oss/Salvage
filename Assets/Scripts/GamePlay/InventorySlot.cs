// Assets/Scripts/GamePlay/InventorySlot.cs
using UnityEngine;

/// <summary>
/// 背包槽位 - 存储单个物品数据
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public ItemData item;

    /// <summary>
    /// 检查槽位是否为空
    /// </summary>
    public bool IsEmpty
    {
        get { return item == null; }
    }

    public InventorySlot()
    {
        item = null;
    }

    public InventorySlot(ItemData itemData)
    {
        item = itemData;
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void Clear()
    {
        item = null;
    }
}