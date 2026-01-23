// Assets/Scripts/GamePlay/ClockHandClick.cs
using UnityEngine;

/// <summary>
/// 闹钟指针点击组件
/// 放在每根指针上，点击时通知 AlarmClockPuzzle
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ClockHandClick : MonoBehaviour
{
    public enum HandType
    {
        Hour,
        Minute,
        Second
    }

    [Header("设置")]
    [Tooltip("指针类型")]
    public HandType handType = HandType.Hour;

    [Tooltip("关联的闹钟谜题控制器（如果为空则自动查找父级）")]
    public AlarmClockPuzzle puzzleController;

    private void Start()
    {
        // 自动查找父级的 AlarmClockPuzzle
        if (puzzleController == null)
        {
            puzzleController = GetComponentInParent<AlarmClockPuzzle>();
        }

        if (puzzleController == null)
        {
            Debug.LogError($"[ClockHandClick] {gameObject.name} 找不到 AlarmClockPuzzle 控制器！");
        }

        // 确保有 Collider2D
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogWarning($"[ClockHandClick] {gameObject.name} 没有 Collider2D，添加 BoxCollider2D");
            gameObject.AddComponent<BoxCollider2D>();
        }
    }

    private void OnMouseDown()
    {
        if (puzzleController == null) return;

        switch (handType)
        {
            case HandType.Hour:
                puzzleController.OnHourHandClicked();
                break;
            case HandType.Minute:
                puzzleController.OnMinuteHandClicked();
                break;
            case HandType.Second:
                puzzleController.OnSecondHandClicked();
                break;
        }

        Debug.Log($"[ClockHandClick] 点击了 {handType} 指针");
    }
}