// Assets/Scripts/Data/ItemData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基本信息")]
    public string itemID;           // 唯一标识符
    public string displayName;      // 显示名称

    [TextArea(3, 5)]
    public string description;      // 物品描述

    [Header("显示")]
    public Sprite icon;             // 背包图标

    [Header("分类")]
    public ItemCategory category;   // 物品分类

    public enum ItemCategory
    {
        Key,        // 钥匙类
        Tool,       // 工具类
        Clue,       // 线索类
        Consumable  // 消耗品
    }
}