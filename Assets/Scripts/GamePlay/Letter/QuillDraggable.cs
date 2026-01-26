// Assets/Scripts/GamePlay/Letter/QuillDraggable.cs
// 羽毛笔拖拽组件 - 处理鼠标拖拽交互
using UnityEngine;

/// <summary>
/// 羽毛笔拖拽组件
/// 放在羽毛笔物体上，处理拖拽交互
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class QuillDraggable : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("羽毛笔桌面控制器")]
    public QuillDeskController deskController;

    [Header("设置")]
    [Tooltip("拖动时的层级提升")]
    public int dragSortingOrder = 100;

    private SpriteRenderer spriteRenderer;
    private int originalSortingOrder;
    private bool isDragging = false;
    private Camera mainCamera;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
        }

        mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        if (deskController == null) return;

        // 开始拖动
        isDragging = true;

        // 提升层级
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = dragSortingOrder;
        }

        // 通知控制器
        deskController.OnQuillDragStart();
    }

    private void OnMouseDrag()
    {
        if (!isDragging || deskController == null) return;

        // 获取鼠标世界坐标
        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;

        // 通知控制器（控制器会移动羽毛笔并检测涂抹）
        deskController.OnQuillDrag(mousePos);
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;

        // 恢复层级
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
        }

        // 通知控制器
        if (deskController != null)
        {
            deskController.OnQuillDragEnd();
        }
    }
}