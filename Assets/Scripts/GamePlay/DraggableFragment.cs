using UnityEngine;

//Assets / Scripts / GamePlay / DraggableFragment.cs
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

    public void Initialize(PhotoFramePuzzle puzzle, int index, Vector2 target)
    {
        this.puzzle = puzzle;
        this.fragmentIndex = index;
        this.targetPosition = target;
        mainCamera = Camera.main;
    }

    /// <summary>
    /// 强制吸附（用于恢复存档）
    /// </summary>
    public void ForceSnap()
    {
        isSnapped = true;
        transform.position = targetPosition;
    }

    private void OnMouseDown()
    {
        if (isSnapped) return; // 已吸附，不能拖动

        isDragging = true;
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = transform.position.z;
        dragOffset = transform.position - mouseWorldPos;
    }

    private void OnMouseDrag()
    {
        if (!isDragging || isSnapped) return;

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

        if (distance <= puzzle.snapDistance)
        {
            // 吸附到正确位置
            StartCoroutine(SnapToTarget());
        }
    }

    private IEnumerator SnapToTarget()
    {
        isSnapped = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = (Vector3)targetPosition;
        endPos.z = startPos.z;

        float elapsed = 0f;
        float duration = puzzle.snapDuration;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        transform.position = endPos;

        // 通知拼图系统
        puzzle.OnFragmentSnapped(fragmentIndex);
    }
}