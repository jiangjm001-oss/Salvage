// Assets/Scripts/GamePlay/Synthesis/PotRecipe.cs
// 陶罐配方数据 - ScriptableObject
using UnityEngine;

/// <summary>
/// 陶罐配方数据
/// 定义需要放入陶罐的物品组合，以及对应产出的水晶碎片
/// </summary>
[CreateAssetMenu(fileName = "NewPotRecipe", menuName = "Game/Pot Recipe", order = 1)]
public class PotRecipe : ScriptableObject
{
    [Header("配方信息")]
    [Tooltip("配方ID（用于存档和识别）")]
    public string recipeID;

    [Tooltip("配方名称（显示用）")]
    public string recipeName;

    [Header("配方材料")]
    [Tooltip("需要放入陶罐的物品列表")]
    public ItemData[] requiredItems;

    [Header("产出结果")]
    [Tooltip("合成产出的水晶碎片")]
    public ItemData resultShard;

    [Header("视觉效果（可选）")]
    [Tooltip("陶罐装满时的显示精灵")]
    public Sprite filledPotSprite;

    [Tooltip("水晶碎片的显示精灵（用于机器内展示）")]
    public Sprite shardDisplaySprite;

    /// <summary>
    /// 检查物品列表是否匹配此配方
    /// </summary>
    /// <param name="items">要检查的物品ID列表</param>
    /// <returns>是否完全匹配</returns>
    public bool MatchesRecipe(System.Collections.Generic.List<string> itemIDs)
    {
        if (requiredItems == null || requiredItems.Length == 0)
            return false;

        if (itemIDs == null || itemIDs.Count != requiredItems.Length)
            return false;

        // 创建一个临时列表用于匹配检查
        var tempItemIDs = new System.Collections.Generic.List<string>(itemIDs);

        foreach (var requiredItem in requiredItems)
        {
            if (requiredItem == null)
                continue;

            bool found = tempItemIDs.Remove(requiredItem.itemID);
            if (!found)
                return false;
        }

        // 所有物品都匹配且没有多余的物品
        return tempItemIDs.Count == 0;
    }

    /// <summary>
    /// 获取配方所需物品数量
    /// </summary>
    public int GetRequiredItemCount()
    {
        return requiredItems != null ? requiredItems.Length : 0;
    }

    /// <summary>
    /// 获取配方所需物品的ID列表（用于调试）
    /// </summary>
    public string GetRequiredItemsString()
    {
        if (requiredItems == null || requiredItems.Length == 0)
            return "无";

        var names = new System.Collections.Generic.List<string>();
        foreach (var item in requiredItems)
        {
            if (item != null)
                names.Add(item.displayName);
        }
        return string.Join(", ", names);
    }
}