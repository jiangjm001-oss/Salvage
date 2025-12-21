// Assets/Scripts/GamePlay/ItemData.cs
using UnityEngine;

/// <summary>
/// 物品数据 - ScriptableObject
/// 用于定义游戏中的可拾取物品
/// 
/// 创建方法：在 Project 窗口右键 -> Create -> Game/Item Data
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Game/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("基础信息")]
    [Tooltip("物品的唯一标识符（用于存档）")]
    public string itemID;

    [Tooltip("物品显示名称")]
    public string displayName;

    [Tooltip("物品描述")]
    [TextArea(2, 5)]
    public string description;

    [Header("视觉")]
    [Tooltip("物品图标（显示在背包中）")]
    public Sprite icon;

    [Header("可选设置")]
    [Tooltip("是否可以使用")]
    public bool isUsable = false;

    [Tooltip("是否可以与其他物品组合")]
    public bool isCombinable = false;

    /// <summary>
    /// 编辑器验证 - 确保 itemID 不为空
    /// </summary>
    private void OnValidate()
    {
        // 如果 itemID 为空，自动使用文件名
        if (string.IsNullOrEmpty(itemID))
        {
            itemID = name;
        }
    }
}