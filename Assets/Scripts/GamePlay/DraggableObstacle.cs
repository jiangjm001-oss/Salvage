using UnityEngine;
using UnityEngine.Events;

//Assets / Scripts / GamePlay / DraggableObstacle.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 可拖动的遮挡物
/// 用于"移开花瓶发现钥匙"这类谜题
/// </summary>
public class DraggableObstacle : MonoBehaviour
{
    [Header("拖拽设置")]
    [Tooltip("是否可以拖动")]
    public bool isDraggable = true;

    [Tooltip("拖拽范围限制（相对于父物体的本地坐标）")]
    public Rect dragBounds = new Rect(-2, -1, 4, 2);

    [Header("遮挡设置")]
    [Tooltip("被遮挡的物体（如钥匙）")]
    public GameObject hiddenObject;

    [Tooltip("遮挡检测半径")]
    public float coverRadius = 0.5f;

    [Header("音效")]
    [Tooltip("开始拖动时的音效")]
    public string dragStartSound = "";

    [Tooltip("放下时的音效")]
    public string dragEndSound = "";

    [Header("事件")]
    public UnityEvent OnDragStart;
    public UnityEvent OnDragEnd;
    public UnityEvent OnHiddenObjectRevealed;  // 当隐藏物体首次显露时触发

    // 私有变量
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;
    private bool hasRevealed = false;  // 是否已经触发过显露事件

    private void Start()
    {
        mainCamera = Camera.main;

        // 初始检查：如果已经不遮挡，启用隐藏物体的交互
        UpdateHiddenObjectState();
    }

    private void OnMouseDown()
    {
        if (!isDraggable) return;

        isDragging = true;

        // 计算鼠标点击位置与物体中心的偏移
        Vector3 mouseWorldPos = GetMouseWorldPosition();
        dragOffset = transform.localPosition - transform.parent.InverseTransformPoint(mouseWorldPos);

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(dragStartSound))
        {
            AudioManager.Instance.PlaySFX(dragStartSound);
        }

        OnDragStart?.Invoke();
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        Vector3 targetLocalPos = transform.parent.InverseTransformPoint(mouseWorldPos) + dragOffset;

        // 限制在拖拽范围内
        targetLocalPos.x = Mathf.Clamp(targetLocalPos.x, dragBounds.xMin, dragBounds.xMax);
        targetLocalPos.y = Mathf.Clamp(targetLocalPos.y, dragBounds.yMin, dragBounds.yMax);
        targetLocalPos.z = transform.localPosition.z;  // 保持Z轴不变

        transform.localPosition = targetLocalPos;

        // 实时更新隐藏物体状态
        UpdateHiddenObjectState();
    }

    private void OnMouseUp()
    {
        if (!isDragging) return;

        isDragging = false;

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(dragEndSound))
        {
            AudioManager.Instance.PlaySFX(dragEndSound);
        }

        OnDragEnd?.Invoke();
    }

    /// <summary>
    /// 获取鼠标的世界坐标
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    /// <summary>
    /// 检查是否遮挡了隐藏物体
    /// </summary>
    private bool IsCoveringHiddenObject()
    {
        if (hiddenObject == null) return false;

        float distance = Vector2.Distance(
            transform.position,
            hiddenObject.transform.position
        );

        return distance < coverRadius;
    }

    /// <summary>
    /// 更新隐藏物体的可交互状态
    /// </summary>
    private void UpdateHiddenObjectState()
    {
        if (hiddenObject == null) return;

        bool isCovered = IsCoveringHiddenObject();

        // 获取隐藏物体的碰撞器
        Collider2D hiddenCollider = hiddenObject.GetComponent<Collider2D>();
        if (hiddenCollider != null)
        {
            hiddenCollider.enabled = !isCovered;
        }

        // 首次显露时触发事件
        if (!isCovered && !hasRevealed)
        {
            hasRevealed = true;
            OnHiddenObjectRevealed?.Invoke();
            Debug.Log($"[DraggableObstacle] 隐藏物体 '{hiddenObject.name}' 已显露！");
        }
    }

    // ============ 编辑器辅助 ============
    private void OnDrawGizmosSelected()
    {
        // 绘制拖拽范围
        Gizmos.color = Color.yellow;
        Vector3 center = transform.parent != null
            ? transform.parent.TransformPoint(dragBounds.center)
            : (Vector3)dragBounds.center;
        Vector3 size = new Vector3(dragBounds.width, dragBounds.height, 0.1f);
        Gizmos.DrawWireCube(center, size);

        // 绘制遮挡检测范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, coverRadius);

        // 绘制到隐藏物体的连线
        if (hiddenObject != null)
        {
            Gizmos.color = IsCoveringHiddenObject() ? Color.red : Color.green;
            Gizmos.DrawLine(transform.position, hiddenObject.transform.position);
        }
    }
}