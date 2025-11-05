// Assets/Scripts/Gameplay/ItemData.cs
using UnityEngine;

// 使用 [CreateAssetMenu] 可以让我们在 Unity 中直接创建 .asset 文件
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemID; // 唯一标识符，例如 "key_level1"
    public string displayName = "New Item"; // 显示名称
    public string description = "This is a new item."; // 描述
    public Sprite icon; // 物品在UI中显示的图标
}