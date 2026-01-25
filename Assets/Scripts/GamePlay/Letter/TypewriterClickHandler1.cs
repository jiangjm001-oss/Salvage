// Assets/Scripts/GamePlay/Letter/TypewriterClickHandler.cs
// 打字机区域点击处理组件
using UnityEngine;

/// <summary>
/// 打字机区域点击处理器
/// 用于在打字机 ZoomView 中点击放置信纸
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TypewriterClickHandler : MonoBehaviour
{
    [Header("关联")]
    [Tooltip("关联的打字机控制器")]
    public TypewriterController controller;

    [Header("提示设置")]
    [Tooltip("需要信纸时的提示")]
    public string needLetterHint = "需要一张信纸";

    [Tooltip("是否显示提示")]
    public bool showHints = true;

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleClick();
    }

    private void HandleClick()
    {
        if (controller == null)
        {
            Debug.LogError("[TypewriterClickHandler] 未设置 TypewriterController！");
            return;
        }

        // 尝试放置信纸
        controller.TryPlaceLetter();
    }

    private void ShowHint(string hint)
    {
        if (string.IsNullOrEmpty(hint)) return;

        Debug.Log($"[TypewriterClickHandler] 提示: {hint}");
    }
}