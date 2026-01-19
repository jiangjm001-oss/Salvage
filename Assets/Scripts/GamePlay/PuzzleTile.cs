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
    public int boardIndex;

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (puzzle != null)
        {
            puzzle.TryMoveTile(boardIndex);
        }
    }
}