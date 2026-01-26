// Assets/Scripts/GamePlay/Letter/QuillDeskClickArea.cs
// 羽毛笔桌面点击区域 - 处理桌面和信纸的点击
using UnityEngine;

/// <summary>
/// 羽毛笔桌面点击区域
/// 用于检测桌面点击（放置信纸）和信纸点击（胶水/标题/拾取）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class QuillDeskClickArea : MonoBehaviour
{
    public enum ClickAreaType
    {
        Desk,       // 桌面区域（用于放置信纸）
        Letter      // 信纸区域（用于胶水/标题/拾取）
    }

    [Header("设置")]
    [Tooltip("点击区域类型")]
    public ClickAreaType areaType = ClickAreaType.Desk;

    [Tooltip("羽毛笔桌面控制器")]
    public QuillDeskController deskController;

    private void OnMouseDown()
    {
        if (deskController == null)
        {
            Debug.LogWarning("[QuillDeskClickArea] deskController 未设置！");
            return;
        }

        switch (areaType)
        {
            case ClickAreaType.Desk:
                deskController.OnDeskClicked();
                break;

            case ClickAreaType.Letter:
                deskController.OnLetterClicked();
                break;
        }
    }
}