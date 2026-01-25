// Assets/Scripts/GamePlay/PuzzleTile.cs
using UnityEngine;

/// <summary>
/// 华容道方块组件
/// 自动生成，处理点击事件
/// </summary>
public class PuzzleTile : MonoBehaviour
{
    [HideInInspector]
    public SlidingPuzzle puzzle;

    [HideInInspector]
    public int boardIndex; // 保留兼容

    [HideInInspector]
    public int tileValue; // ⭐ 方块的值 (1-8)，这个不会变

    private void OnMouseDown()
    {
        Debug.Log($"[PuzzleTile] 点击了 {gameObject.name}，tileValue={tileValue}");

        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("[PuzzleTile] 点击被 UI 阻挡");
            return;
        }

        if (puzzle != null)
        {
            // ⭐ 使用 tileValue 而不是 boardIndex
            puzzle.TryMoveTileByValue(tileValue);
        }
        else
        {
            Debug.LogError("[PuzzleTile] puzzle 引用为空！");
        }
    }
}