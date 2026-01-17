// Assets/Scripts/GamePlay/PuzzleTile.cs
using UnityEngine;

/// <summary>
/// 华容道谜题方块 - 处理点击事件
/// 由 SlidingPuzzleBox 自动创建，无需手动配置
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PuzzleTile : MonoBehaviour
{
    private SlidingPuzzleBox puzzleBox;
    private int tileValue;

    /// <summary>
    /// 初始化方块（由 SlidingPuzzleBox 调用）
    /// </summary>
    public void Initialize(SlidingPuzzleBox box, int value)
    {
        puzzleBox = box;
        tileValue = value;
    }

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 尝试移动方块
        if (puzzleBox != null)
        {
            puzzleBox.TryMoveTile(tileValue);
        }
    }
}