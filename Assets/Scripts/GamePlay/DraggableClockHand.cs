// Assets/Scripts/Puzzles/DraggableClockHand.cs
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 可拖动旋转的时钟指针
/// </summary>
public class DraggableClockHand : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("设置")]
    [Tooltip("指针类型")]
    public HandType handType;

    [Tooltip("旋转中心点（时钟中心）")]
    public Transform pivotPoint;

    [Tooltip("是否吸附到固定角度")]
    public bool snapToAngle = true;

    [Tooltip("吸附间隔角度（如30度=12格）")]
    public float snapAngle = 30f;

    [Header("当前状态")]
    [SerializeField] private float currentAngle = 0f;

    // 谜题控制器引用
    private ClockPuzzleController puzzleController;
    private bool isDragging = false;

    public enum HandType
    {
        Hour,    // 时针
        Minute,  // 分针
        Second   // 秒针
    }

    /// <summary>
    /// 获取当前角度（0-360度，12点方向为0）
    /// </summary>
    public float CurrentAngle => currentAngle;

    private void Start()
    {
        // 自动查找父物体上的控制器
        puzzleController = GetComponentInParent<ClockPuzzleController>();

        // 如果没有指定旋转中心，使用父物体位置
        if (pivotPoint == null)
        {
            pivotPoint = transform.parent;
        }

        // 初始化当前角度
        currentAngle = GetAngleFromRotation();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        Debug.Log($"[ClockHand] 开始拖动 {handType}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // 计算鼠标相对于中心点的角度
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(eventData.position);
        mouseWorldPos.z = transform.position.z;

        Vector3 direction = mouseWorldPos - pivotPoint.position;
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        // 转换为0-360度范围（顺时针，12点为0）
        angle = -angle;
        if (angle < 0) angle += 360f;

        // 吸附处理
        if (snapToAngle && snapAngle > 0)
        {
            angle = Mathf.Round(angle / snapAngle) * snapAngle;
        }

        // 应用旋转
        currentAngle = angle;
        transform.rotation = Quaternion.Euler(0, 0, -currentAngle);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        Debug.Log($"[ClockHand] {handType} 停在 {currentAngle}度");

        // 通知控制器检查谜题
        if (puzzleController != null)
        {
            puzzleController.CheckPuzzle();
        }
    }

    /// <summary>
    /// 从当前旋转获取角度
    /// </summary>
    private float GetAngleFromRotation()
    {
        float z = transform.eulerAngles.z;
        float angle = -z;
        if (angle < 0) angle += 360f;
        return angle;
    }

    /// <summary>
    /// 设置指针角度（供外部调用，如存档恢复）
    /// </summary>
    public void SetAngle(float angle)
    {
        currentAngle = angle;
        transform.rotation = Quaternion.Euler(0, 0, -currentAngle);
    }
}