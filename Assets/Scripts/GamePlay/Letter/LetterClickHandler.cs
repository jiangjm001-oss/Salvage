// Assets/Scripts/GamePlay/Letter/LetterClickHandler.cs
// 信纸点击转发组件 - 挂在桌面上的信纸物体上
using UnityEngine;

/// <summary>
/// 信纸点击处理器（桌面上的信纸）
/// 将点击事件转发给 QuillDeskController
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LetterClickHandler : MonoBehaviour
{
    [Header("关联")]
    [Tooltip("关联的桌面控制器")]
    public QuillDeskController controller;

    [Header("提示设置")]
    [Tooltip("需要胶水时的提示")]
    public string needGlueHint = "需要涂上胶水";

    [Tooltip("需要标题时的提示")]
    public string needTitleHint = "需要贴上标题";

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
            Debug.LogError("[LetterClickHandler] 未设置 QuillDeskController！");
            return;
        }

        // 检查当前状态并给出提示
        if (showHints && UIManager.Instance != null)
        {
            ItemData selectedItem = UIManager.Instance.GetSelectedItem();

            // 根据桌面状态给出提示
            switch (controller.currentState)
            {
                case QuillDeskController.DeskState.LetterPlaced:
                    if (selectedItem == null)
                    {
                        ShowHint(needGlueHint);
                    }
                    break;

                case QuillDeskController.DeskState.GlueApplied:
                    if (selectedItem == null)
                    {
                        ShowHint(needTitleHint);
                    }
                    break;
            }
        }

        // 转发点击事件
        controller.OnLetterClicked();
    }

    private void ShowHint(string hint)
    {
        if (string.IsNullOrEmpty(hint)) return;

        // 如果有 UI 提示系统，使用它
        // UIManager.Instance?.ShowHint(hint);

        Debug.Log($"[LetterClickHandler] 提示: {hint}");
    }
}