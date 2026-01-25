// Assets/Scripts/GamePlay/Letter/DeskClickHandler.cs
// 桌面点击转发组件 - 挂在桌面区域上
using UnityEngine;

/// <summary>
/// 桌面点击处理器
/// 将点击事件转发给 QuillDeskController
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DeskClickHandler : MonoBehaviour
{
    [Header("关联")]
    [Tooltip("关联的桌面控制器")]
    public QuillDeskController controller;

    [Header("提示设置")]
    [Tooltip("没有选中信纸时显示的提示")]
    public string noLetterSelectedHint = "需要先选中信纸";

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
            Debug.LogError("[DeskClickHandler] 未设置 QuillDeskController！");
            return;
        }

        // 检查是否选中了物品
        if (UIManager.Instance != null)
        {
            ItemData selectedItem = UIManager.Instance.GetSelectedItem();

            if (selectedItem == null && showHints)
            {
                // 显示提示
                ShowHint(noLetterSelectedHint);
                return;
            }
        }

        // 转发点击事件
        controller.OnDeskClicked();
    }

    private void ShowHint(string hint)
    {
        if (string.IsNullOrEmpty(hint)) return;

        // 如果有 UI 提示系统，使用它
        // UIManager.Instance?.ShowHint(hint);

        Debug.Log($"[DeskClickHandler] 提示: {hint}");
    }
}