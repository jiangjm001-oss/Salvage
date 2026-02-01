// Assets/Scripts/GamePlay/Experimenter/DraggableMagnifier.cs
// 可拖动放大镜组件
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// 可拖动放大镜
/// 支持鼠标拖拽，检测是否到达目标区域
/// </summary>
public class DraggableMagnifier : MonoBehaviour
{
    #region ========== 拖拽设置 ==========

    [Header("拖拽设置")]
    [Tooltip("是否可以拖动")]
    [SerializeField] private bool isDraggable = false;

    [Tooltip("拖拽时的层级提升")]
    [SerializeField] private int dragSortingOrderBoost = 100;

    [Tooltip("拖拽移动的平滑度（0=无平滑，1=最大平滑）")]
    [Range(0f, 0.95f)]
    [SerializeField] private float dragSmoothing = 0.1f;

    [Tooltip("限制拖拽区域（如果为空则不限制）")]
    [SerializeField] private Collider2D dragBounds;

    #endregion

    #region ========== 目标区域设置 ==========

    [Header("目标区域")]
    [Tooltip("目标区域的Transform")]
    public Transform targetZone;

    [Tooltip("到达目标的距离阈值")]
    [SerializeField] private float targetReachDistance = 0.5f;

    [Tooltip("到达目标后是否锁定位置")]
    [SerializeField] private bool lockOnTarget = true;

    [Tooltip("锁定时是否使用平滑动画")]
    [SerializeField] private bool smoothLockAnimation = true;

    [Tooltip("锁定动画时长")]
    [SerializeField] private float lockAnimationDuration = 0.3f;

    #endregion

    #region ========== 视觉效果 ==========

    [Header("视觉效果")]
    [Tooltip("拖拽时的缩放")]
    [SerializeField] private float dragScale = 1.1f;

    [Tooltip("接近目标时的提示效果")]
    [SerializeField] private bool showNearTargetHint = true;

    [Tooltip("接近目标的距离")]
    [SerializeField] private float nearTargetDistance = 1.5f;

    [Tooltip("接近目标时的颜色")]
    [SerializeField] private Color nearTargetColor = new Color(1f, 1f, 0.7f, 1f);

    #endregion

    #region ========== 音效 ==========

    [Header("音效")]
    [Tooltip("开始拖拽音效")]
    public string dragStartSound = "";

    [Tooltip("结束拖拽音效")]
    public string dragEndSound = "";

    [Tooltip("到达目标音效")]
    public string reachTargetSound = "Audio/SFX/puzzle_correct";

    #endregion

    #region ========== 事件 ==========

    [Header("事件")]
    public UnityEvent OnDragStarted;
    public UnityEvent OnDragEnded;
    public UnityEvent OnReachedTarget;
    public UnityEvent OnNearTarget;

    #endregion

    #region ========== 私有变量 ==========

    private bool isDragging = false;
    private bool hasReachedTarget = false;
    private bool isLocking = false;

    private Vector3 dragOffset;
    private Vector3 originalScale;
    private Vector3 targetPosition;

    private int originalSortingOrder;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private Camera mainCamera;

    #endregion

    #region ========== Unity 生命周期 ==========

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;

        if (spriteRenderer != null)
        {
            originalSortingOrder = spriteRenderer.sortingOrder;
            originalColor = spriteRenderer.color;
        }

        originalScale = transform.localScale;
    }

    private void Update()
    {
        if (!isDraggable || hasReachedTarget || isLocking) return;

        HandleDragInput();

        if (isDragging)
        {
            UpdateDragPosition();
            CheckNearTarget();
        }
    }

    #endregion

    #region ========== 拖拽处理 ==========

    private void HandleDragInput()
    {
        // 开始拖拽
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            if (IsMouseOverThis())
            {
                StartDrag();
            }
        }

        // 结束拖拽
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    private bool IsMouseOverThis()
    {
        // 检查是否点击在UI上
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return false;
        }

        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Collider2D collider = GetComponent<Collider2D>();

        if (collider != null)
        {
            return collider.OverlapPoint(mousePos);
        }

        // 如果没有碰撞体，使用 SpriteRenderer 的边界
        if (spriteRenderer != null)
        {
            return spriteRenderer.bounds.Contains(mousePos);
        }

        return false;
    }

    private void StartDrag()
    {
        isDragging = true;

        Vector3 mouseWorldPos = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPos;

        // 提升层级
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder + dragSortingOrderBoost;
        }

        // 放大效果
        transform.localScale = originalScale * dragScale;

        // 播放音效
        PlaySound(dragStartSound);

        OnDragStarted?.Invoke();

        Debug.Log("[DraggableMagnifier] 开始拖拽");
    }

    private void EndDrag()
    {
        isDragging = false;

        // 恢复层级
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
            spriteRenderer.color = originalColor;
        }

        // 恢复缩放
        transform.localScale = originalScale;

        // 播放音效
        PlaySound(dragEndSound);

        // 检查是否到达目标
        CheckReachedTarget();

        OnDragEnded?.Invoke();

        Debug.Log("[DraggableMagnifier] 结束拖拽");
    }

    private void UpdateDragPosition()
    {
        Vector3 targetPos = GetMouseWorldPosition() + dragOffset;

        // 限制在边界内
        if (dragBounds != null)
        {
            targetPos = ClampToBounds(targetPos);
        }

        // 平滑移动
        if (dragSmoothing > 0)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, 1f - dragSmoothing);
        }
        else
        {
            transform.position = targetPos;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -mainCamera.transform.position.z;
        return mainCamera.ScreenToWorldPoint(mousePos);
    }

    private Vector3 ClampToBounds(Vector3 position)
    {
        if (dragBounds == null) return position;

        Bounds bounds = dragBounds.bounds;
        position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        position.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);

        return position;
    }

    #endregion

    #region ========== 目标检测 ==========

    private void CheckNearTarget()
    {
        if (targetZone == null || !showNearTargetHint) return;

        float distance = Vector2.Distance(transform.position, targetZone.position);

        if (distance <= nearTargetDistance)
        {
            // 接近目标，显示提示颜色
            if (spriteRenderer != null)
            {
                float t = 1f - (distance / nearTargetDistance);
                spriteRenderer.color = Color.Lerp(originalColor, nearTargetColor, t);
            }

            OnNearTarget?.Invoke();
        }
        else
        {
            // 恢复原色
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
    }

    private void CheckReachedTarget()
    {
        if (targetZone == null) return;

        float distance = Vector2.Distance(transform.position, targetZone.position);

        Debug.Log($"[DraggableMagnifier] 与目标距离: {distance:F2}, 阈值: {targetReachDistance}");

        if (distance <= targetReachDistance)
        {
            ReachTarget();
        }
    }

    private void ReachTarget()
    {
        if (hasReachedTarget) return;

        hasReachedTarget = true;
        isDraggable = false;

        Debug.Log("[DraggableMagnifier] ✓ 到达目标位置！");

        // 播放音效
        PlaySound(reachTargetSound);

        // 锁定到目标位置
        if (lockOnTarget && targetZone != null)
        {
            if (smoothLockAnimation)
            {
                StartCoroutine(SmoothLockToTarget());
            }
            else
            {
                transform.position = targetZone.position;
                OnReachedTarget?.Invoke();
            }
        }
        else
        {
            OnReachedTarget?.Invoke();
        }
    }

    private System.Collections.IEnumerator SmoothLockToTarget()
    {
        isLocking = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = targetZone.position;
        float elapsed = 0f;

        while (elapsed < lockAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lockAnimationDuration;

            // 使用缓动曲线
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, endPos, t);

            yield return null;
        }

        transform.position = endPos;
        isLocking = false;

        OnReachedTarget?.Invoke();
    }

    #endregion

    #region ========== 公共接口 ==========

    /// <summary>
    /// 启用拖拽功能
    /// </summary>
    public void EnableDragging()
    {
        isDraggable = true;
        hasReachedTarget = false;
        Debug.Log("[DraggableMagnifier] 拖拽功能已启用");
    }

    /// <summary>
    /// 禁用拖拽功能
    /// </summary>
    public void DisableDragging()
    {
        isDraggable = false;

        if (isDragging)
        {
            EndDrag();
        }

        Debug.Log("[DraggableMagnifier] 拖拽功能已禁用");
    }

    /// <summary>
    /// 设置目标区域
    /// </summary>
    public void SetTargetZone(Transform target)
    {
        targetZone = target;
    }

    /// <summary>
    /// 重置状态
    /// </summary>
    public void ResetState()
    {
        hasReachedTarget = false;
        isDragging = false;
        isLocking = false;

        if (spriteRenderer != null)
        {
            spriteRenderer.sortingOrder = originalSortingOrder;
            spriteRenderer.color = originalColor;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// 是否已到达目标
    /// </summary>
    public bool HasReachedTarget => hasReachedTarget;

    /// <summary>
    /// 是否正在拖拽
    /// </summary>
    public bool IsDragging => isDragging;

    #endregion

    #region ========== 辅助方法 ==========

    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    #endregion

    #region ========== 编辑器辅助 ==========

    private void OnDrawGizmosSelected()
    {
        // 绘制目标区域
        if (targetZone != null)
        {
            // 到达距离
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(targetZone.position, targetReachDistance);

            // 接近距离
            if (showNearTargetHint)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(targetZone.position, nearTargetDistance);
            }

            // 连线
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, targetZone.position);
        }

        // 绘制拖拽边界
        if (dragBounds != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(dragBounds.bounds.center, dragBounds.bounds.size);
        }
    }

    #endregion
}