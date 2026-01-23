// Assets/Scripts/Puzzles/Typewriter/TypewriterPaperInteraction.cs
using UnityEngine;

/// <summary>
/// 信纸交互组件
/// 处理放置信纸和拾取结果信纸
/// </summary>
public class TypewriterPaperInteraction : MonoBehaviour
{
    [Header("引用")]
    public TypewriterController controller;

    private void OnMouseDown()
    {
        // 确保在正确的视图状态
        if (GameManager.Instance?.CurrentViewState != GameManager.ViewState.lv1_B_zoom_Typewriter)
        {
            return;
        }

        if (controller == null) return;

        // 尝试拾取结果（如果已解谜）
        controller.TryPickupResultPaper();
    }
}