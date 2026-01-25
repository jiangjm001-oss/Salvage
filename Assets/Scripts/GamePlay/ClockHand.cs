// Assets/Scripts/GamePlay/ClockHand.cs
using UnityEngine;

/// <summary>
/// 时钟指针 - 支持拖拽旋转
/// 挂载到时针或分针物体上
/// </summary>
public class ClockHand : MonoBehaviour
{
    [Header("指针设置")]
    [Tooltip("是否为时针（false = 分针）")]
    public bool isHourHand = false;

    [Tooltip("旋转步进角度（0 = 自由旋转，30 = 每小时一格，6 = 每分钟一格）")]
    public float snapAngle = 0f;

    [Tooltip("旋转中心点（留空则使用自身位置）")]
    public Transform pivotPoint;

    [Header("调试")]
    [Tooltip("当前角度（只读）")]
    [SerializeField] private float currentAngleDisplay = 0f;

    /// <summary>
    /// 当前角度（0-360，0=12点方向，顺时针增加）
    /// </summary>
    public float CurrentAngle { get; private set; } = 0f;

    // 拖拽状态
    private bool isDragging = false;
    private Camera mainCamera;

    /// <summary>
    /// 事件：角度变化时通知 ClockPuzzle
    /// </summary>
    public System.Action OnAngleChanged;

    private void Start()
    {
        mainCamera = Camera.main;
        if (pivotPoint == null) pivotPoint = transform;

        // 从当前旋转读取初始角度
        // Unity 的 Z 旋转是逆时针为正，所以取负值转换为顺时针
        CurrentAngle = NormalizeAngle(-transform.localEulerAngles.z);
        currentAngleDisplay = CurrentAngle;
    }

    private void OnMouseDown()
    {
        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (!isDragging) return;

        // 获取鼠标世界坐标
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = pivotPoint.position.z;

        // 计算鼠标相对于中心点的方向
        Vector2 direction = mouseWorld - pivotPoint.position;

        // 计算角度（从12点方向顺时针计算）
        // Atan2(x, y) 会让 12点方向 = 0度
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;

        // 应用步进（可选）
        if (snapAngle > 0)
        {
            angle = Mathf.Round(angle / snapAngle) * snapAngle;
        }

        // 标准化并设置角度
        float newAngle = NormalizeAngle(angle);

        // 只有角度变化时才触发事件
        if (Mathf.Abs(newAngle - CurrentAngle) > 0.1f)
        {
            CurrentAngle = newAngle;
            currentAngleDisplay = CurrentAngle;

            // 应用旋转（Unity Z 轴逆时针为正，所以取负值）
            transform.localRotation = Quaternion.Euler(0, 0, -CurrentAngle);

            // 触发事件
            OnAngleChanged?.Invoke();
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    /// <summary>
    /// 获取当前指向的"小时"值 (0-12)
    /// 0度 = 12点, 90度 = 3点, 180度 = 6点, 270度 = 9点
    /// </summary>
    public float GetHourValue()
    {
        // 90度 = 3小时, 所以 角度/30 = 小时
        float hour = CurrentAngle / 30f;
        return hour;
    }

    /// <summary>
    /// 获取当前指向的"分钟"值 (0-60)
    /// 0度 = 0分, 90度 = 15分, 180度 = 30分, 270度 = 45分
    /// </summary>
    public float GetMinuteValue()
    {
        // 360度 = 60分钟, 所以 角度/6 = 分钟
        float minute = CurrentAngle / 6f;
        return minute;
    }

    /// <summary>
    /// 设置指针角度（用于存档恢复）
    /// </summary>
    public void SetAngle(float angle)
    {
        CurrentAngle = NormalizeAngle(angle);
        currentAngleDisplay = CurrentAngle;
        transform.localRotation = Quaternion.Euler(0, 0, -CurrentAngle);
    }

    /// <summary>
    /// 将角度标准化到 0-360 范围
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle < 0) angle += 360f;
        while (angle >= 360f) angle -= 360f;
        return angle;
    }

    // ============ 编辑器辅助 ============
    private void OnDrawGizmosSelected()
    {
        // 绘制指针方向
        Vector3 pivot = pivotPoint != null ? pivotPoint.position : transform.position;

        Gizmos.color = isHourHand ? Color.red : Color.blue;
        Gizmos.DrawLine(pivot, pivot + transform.up * 1f);

        // 绘制12点方向参考线
        Gizmos.color = Color.green;
        Gizmos.DrawLine(pivot, pivot + Vector3.up * 0.5f);
    }
}