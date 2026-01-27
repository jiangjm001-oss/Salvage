// Assets/Scripts/GamePlay/Letter/LetterClickArea.cs
// 信纸点击区域 - 简单版
// 挂载到 LetterOnDesk 或任何需要触发 OnLetterClicked 的物体上
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class LetterClickArea : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("QuillDeskController 引用（留空则自动查找父级）")]
    public QuillDeskController controller;

    private void Awake()
    {
        // 自动查找 Controller
        if (controller == null)
        {
            controller = GetComponentInParent<QuillDeskController>();

            if (controller == null)
            {
                // 尝试在同级或父级的其他子物体中查找
                Transform parent = transform.parent;
                if (parent != null)
                {
                    controller = parent.GetComponentInChildren<QuillDeskController>();
                }
            }
        }

        if (controller == null)
        {
            Debug.LogError($"[LetterClickArea] 找不到 QuillDeskController！请在 Inspector 中手动指定");
        }

        // 确保有 Collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"[LetterClickArea] 缺少 Collider2D 组件！");
        }
    }

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Debug.Log("[LetterClickArea] 检测到点击");

        if (controller != null)
        {
            controller.OnLetterClicked();
        }
        else
        {
            Debug.LogError("[LetterClickArea] Controller 为空，无法处理点击");
        }
    }

    // 如果使用 InteractionSystem 而非 OnMouseDown
    public void OnClick()
    {
        Debug.Log("[LetterClickArea] OnClick 被调用");

        if (controller != null)
        {
            controller.OnLetterClicked();
        }
    }

    // 编辑器中显示点击区域
    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}