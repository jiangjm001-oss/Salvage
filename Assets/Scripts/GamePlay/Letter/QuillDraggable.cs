// Assets/Scripts/GamePlay/Letter/QuillDraggable.cs
// 羽毛笔拖动组件 - 挂在羽毛笔物体上
using UnityEngine;

/// <summary>
/// 羽毛笔拖动组件
/// 处理鼠标拖动逻辑，将拖动事件转发给 QuillDeskController
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class QuillDraggable : MonoBehaviour
{
    [Header("关联")]
    [Tooltip("关联的桌面控制器")]
    public QuillDeskController deskController;

    [Header("拖动设置")]
    [Tooltip("拖动时的 Z 轴偏移（确保羽毛笔在最上层）")]
    public float dragZOffset = -1f;

    [Tooltip("是否限制在屏幕范围内")]
    public bool clampToScreen = true;

    [Header("视觉反馈")]
    [Tooltip("拖动时的缩放")]
    public float dragScale = 1.1f;

    [Tooltip("悬停时的颜色")]
    public Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

    private Camera mainCamera;
    private bool isDragging = false;
    private Vector3 originalScale;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private float originalZ;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        originalScale = transform.localScale;
        originalZ = transform.localPosition.z;
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }
    }

    private void OnMouseEnter()
    {
        if (!enabled) return;

        // 悬停效果
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hoverColor;
        }
    }

    private void OnMouseExit()
    {
        if (!enabled || isDragging) return;

        // 恢复颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void OnMouseDown()
    {
        if (!enabled) return;

        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        StartDrag();
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        UpdateDrag();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        EndDrag();
    }

    private void StartDrag()
    {
        isDragging = true;

        // 通知控制器
        if (deskController != null)
        {
            deskController.OnQuillDragStart();
        }

        // 视觉反馈：放大
        transform.localScale = originalScale * dragScale;

        // 调整 Z 轴
        Vector3 pos = transform.localPosition;
        pos.z = originalZ + dragZOffset;
        transform.localPosition = pos;

        Debug.Log("[QuillDraggable] 开始拖动");
    }

    private void UpdateDrag()
    {
        if (mainCamera == null) return;

        // 获取鼠标世界坐标
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(mousePos);

        // 限制在屏幕范围内
        if (clampToScreen)
        {
            worldPos = ClampToScreenBounds(worldPos);
        }

        // 通知控制器
        if (deskController != null)
        {
            deskController.OnQuillDrag(worldPos);
        }
    }

    private void EndDrag()
    {
        isDragging = false;

        // 通知控制器
        if (deskController != null)
        {
            deskController.OnQuillDragEnd();
        }

        // 恢复视觉效果
        transform.localScale = originalScale;

        // 恢复 Z 轴
        Vector3 pos = transform.localPosition;
        pos.z = originalZ;
        transform.localPosition = pos;

        // 恢复颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        Debug.Log("[QuillDraggable] 结束拖动");
    }

    private Vector3 ClampToScreenBounds(Vector3 worldPos)
    {
        if (mainCamera == null) return worldPos;

        Vector3 viewportPos = mainCamera.WorldToViewportPoint(worldPos);
        viewportPos.x = Mathf.Clamp01(viewportPos.x);
        viewportPos.y = Mathf.Clamp01(viewportPos.y);

        return mainCamera.ViewportToWorldPoint(viewportPos);
    }

    /// <summary>
    /// 强制结束拖动（外部调用）
    /// </summary>
    public void ForceEndDrag()
    {
        if (isDragging)
        {
            EndDrag();
        }
    }

    /// <summary>
    /// 设置是否可拖动
    /// </summary>
    public void SetDraggable(bool draggable)
    {
        enabled = draggable;

        // 如果禁用时正在拖动，强制结束
        if (!draggable && isDragging)
        {
            EndDrag();
        }
    }
}