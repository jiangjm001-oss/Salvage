// Assets/Scripts/GamePlay/DraggablePiece.cs
// 可拖动的拼图碎片组件
using UnityEngine;

/// <summary>
/// 可拖动的拼图碎片
/// 由 PhotoPuzzleController 动态创建和管理
/// </summary>
public class DraggablePiece : MonoBehaviour
{
    // 碎片索引 (0-3)
    public int PieceIndex { get; private set; }

    // 是否已锁定（吸附到正确位置）
    public bool IsLocked { get; private set; } = false;

    // 引用
    private PhotoPuzzleController controller;
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;

    // 拖动相关
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    // 正确位置和吸附距离
    private Vector2 correctLocalPosition;
    private float snapDistance;

    // 排序层级
    private int normalSortingOrder;
    private int draggingSortingOrder;

    /// <summary>
    /// 初始化碎片
    /// </summary>
    public void Initialize(PhotoPuzzleController ctrl, int index, Vector2 correctPos, float snapDist)
    {
        controller = ctrl;
        PieceIndex = index;
        correctLocalPosition = correctPos;
        snapDistance = snapDist;

        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        mainCamera = Camera.main;

        Debug.Log($"[DraggablePiece] 初始化碎片 {index + 1}, 正确位置: {correctPos}");
    }

    /// <summary>
    /// 设置排序层级
    /// </summary>
    public void SetSortingOrders(int normal, int dragging)
    {
        normalSortingOrder = normal;
        draggingSortingOrder = dragging;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = normalSortingOrder;
        }
    }

    private void OnMouseDown()
    {
        if (IsLocked)
            return;

        // 开始拖动
        isDragging = true;

        // 计算鼠标到物体中心的偏移
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        dragOffset = transform.position - mouseWorldPos;

        // 提升层级
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = draggingSortingOrder;
        }

        Debug.Log($"[DraggablePiece] 开始拖动碎片 {PieceIndex + 1}");
    }

    private void OnMouseDrag()
    {
        if (!isDragging || IsLocked)
            return;

        // 跟随鼠标
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        transform.position = mouseWorldPos + dragOffset;
    }

    private void OnMouseUp()
    {
        if (!isDragging)
            return;

        isDragging = false;

        // 恢复层级
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = normalSortingOrder;
        }

        // 检查是否接近正确位置
        CheckSnapToCorrectPosition();

        Debug.Log($"[DraggablePiece] 结束拖动碎片 {PieceIndex + 1}");
    }

    /// <summary>
    /// 检查并吸附到正确位置
    /// </summary>
    private void CheckSnapToCorrectPosition()
    {
        if (controller == null)
            return;

        // 计算当前本地位置到正确位置的距离
        Vector2 currentLocalPos = new Vector2(transform.localPosition.x, transform.localPosition.y);
        float distance = Vector2.Distance(currentLocalPos, correctLocalPosition);

        Debug.Log($"[DraggablePiece] 碎片 {PieceIndex + 1} 距离正确位置: {distance:F2}, 吸附距离: {snapDistance}");

        if (distance <= snapDistance)
        {
            // 吸附到正确位置
            SnapToPosition();
        }
    }

    /// <summary>
    /// 吸附到正确位置
    /// </summary>
    private void SnapToPosition()
    {
        // 移动到正确位置
        transform.localPosition = new Vector3(correctLocalPosition.x, correctLocalPosition.y, 0);

        // 锁定
        IsLocked = true;

        // 禁用碰撞器（不可再交互）
        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        Debug.Log($"[DraggablePiece] ✓ 碎片 {PieceIndex + 1} 已吸附到正确位置");

        // 通知控制器
        if (controller != null)
        {
            controller.OnPieceSnapped(PieceIndex);
        }
    }

    /// <summary>
    /// 强制锁定到正确位置（用于存档恢复）
    /// </summary>
    public void ForceSnapToPosition()
    {
        SnapToPosition();
    }
}