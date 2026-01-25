// Assets/Scripts/GamePlay/DraggableFragment.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 可拖动的拼图碎片
/// 由 PhotoFramePuzzle 动态生成
/// </summary>
public class DraggableFragment : MonoBehaviour
{
    private PhotoFramePuzzle puzzle;
    private int fragmentIndex;
    private Vector2 targetPosition;
    private bool isSnapped = false;
    private bool isDragging = false;
    private Vector3 dragOffset;
    private Camera mainCamera;

    /// <summary>
    /// 初始化碎片
    /// </summary>
    /// <param name="puzzle">所属的拼图系统</param>
    /// <param name="index">碎片索引</param>
    /// <param name="target">目标位置（世界坐标）</param>
    public void Initialize(PhotoFramePuzzle puzzle, int index, Vector2 target)
    {
        this.puzzle = puzzle;
        this.fragmentIndex = index;
        this.targetPosition = target;
        mainCamera = Camera.main;

        Debug.Log($"[DraggableFragment] 初始化碎片 {index}, 目标位置: {target}");
    }

    /// <summary>
    /// 强制吸附到目标位置（用于恢复存档）
    /// </summary>
    public void ForceSnap()
    {
        isSnapped = true;
        transform.position = new Vector3(targetPosition.x, targetPosition.y, transform.position.z);
        Debug.Log($"[DraggableFragment] 碎片 {fragmentIndex} 强制吸附");
    }

    /// <summary>
    /// 检查是否已吸附
    /// </summary>
    public bool IsSnapped()
    {
        return isSnapped;
    }

    private void OnMouseDown()
    {
        if (isSnapped) return; // 已吸附，不能拖动

        isDragging = true;

        // 计算鼠标与物体的偏移量
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        dragOffset = transform.position - mouseWorldPos;

        Debug.Log($"[DraggableFragment] 开始拖动碎片 {fragmentIndex}");
    }

    private void OnMouseDrag()
    {
        if (!isDragging || isSnapped) return;

        // 跟随鼠标移动
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        transform.position = mouseWorldPos + dragOffset;
    }

    private void OnMouseUp()
    {
        if (!isDragging || isSnapped) return;
        isDragging = false;

        // 检查是否靠近目标位置
        float distance = Vector2.Distance(transform.position, targetPosition);

        Debug.Log($"[DraggableFragment] 松开碎片 {fragmentIndex}, 距离目标: {distance}, 阈值: {puzzle.snapDistance}");

        if (distance <= puzzle.snapDistance)
        {
            // 吸附到正确位置
            StartCoroutine(SnapToTarget());
        }
    }

    /// <summary>
    /// 平滑吸附到目标位置的协程
    /// </summary>
    private IEnumerator SnapToTarget()
    {
        isSnapped = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPosition.x, targetPosition.y, startPos.z);

        float elapsed = 0f;
        float duration = puzzle.snapDuration;

        // 平滑移动动画
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        // 确保精确到达目标位置
        transform.position = endPos;

        Debug.Log($"[DraggableFragment] 碎片 {fragmentIndex} 吸附完成");

        // 通知拼图系统
        puzzle.OnFragmentSnapped(fragmentIndex);
    }

    // ============ 编辑器辅助 ============

    private void OnDrawGizmosSelected()
    {
        if (puzzle == null) return;

        // 绘制到目标位置的连线
        Gizmos.color = isSnapped ? Color.green : Color.yellow;
        Gizmos.DrawLine(transform.position, targetPosition);
        Gizmos.DrawWireSphere(targetPosition, 0.1f);
    }
}