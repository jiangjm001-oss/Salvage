// Assets/Scripts/Player/InventorySlot.cs
// 这个类不需要继承MonoBehaviour，它只是一个数据容器
[System.Serializable]
public class InventorySlot
{
    public ItemData item;
    public int index;

    public InventorySlot()
    {
        item = null;
        index = -1;
    }

    public InventorySlot(ItemData newItem, int slotIndex)
    {
        item = newItem;
        index = slotIndex;
    }

    public bool IsEmpty => item == null;
}