using UnityEngine;

//Assets / Scripts / Puzzles / Typewriter / TypewriterPaperSlot.cs
using UnityEngine;

/// <summary>
/// 打字机信纸放置区域
/// 点击时尝试放置信纸
/// </summary>
public class TypewriterPaperSlot : MonoBehaviour
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

        // 尝试放置信纸
        controller.TryPlacePaper();
    }
}