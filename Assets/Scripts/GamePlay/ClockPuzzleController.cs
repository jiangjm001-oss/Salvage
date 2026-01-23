using UnityEngine;
using UnityEngine.Events;

//Assets / Scripts / Puzzles / ClockPuzzleController.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 时钟谜题控制器 - 检测三根指针是否在正确位置
/// </summary>
public class ClockPuzzleController : MonoBehaviour
{
    [Header("指针引用")]
    public DraggableClockHand hourHand;
    public DraggableClockHand minuteHand;
    public DraggableClockHand secondHand;

    [Header("正确答案（角度，12点=0度，顺时针增加）")]
    [Tooltip("时针正确角度 (0-360)")]
    [Range(0, 360)] public float correctHourAngle = 90f;   // 3点 = 90度

    [Tooltip("分针正确角度 (0-360)")]
    [Range(0, 360)] public float correctMinuteAngle = 180f; // 30分 = 180度

    [Tooltip("秒针正确角度 (0-360)")]
    [Range(0, 360)] public float correctSecondAngle = 270f; // 45秒 = 270度

    [Tooltip("角度容差（允许的误差范围）")]
    public float angleTolerance = 5f;

    [Header("状态切换")]
    [Tooltip("谜题未解时的时钟图片")]
    public GameObject clockFaceA;

    [Tooltip("谜题解开后的时钟图片")]
    public GameObject clockFaceB;

    [Header("音效")]
    public string tickSound = "clock_tick";
    public string successSound = "puzzle_success";

    [Header("事件")]
    public UnityEvent OnPuzzleSolved;

    [Header("状态")]
    [SerializeField] private bool isSolved = false;

    public bool IsSolved => isSolved;

    private void Start()
    {
        // 确保初始状态正确
        if (!isSolved)
        {
            if (clockFaceA != null) clockFaceA.SetActive(true);
            if (clockFaceB != null) clockFaceB.SetActive(false);
        }
    }

    /// <summary>
    /// 检查谜题是否完成
    /// </summary>
    public void CheckPuzzle()
    {
        if (isSolved) return; // 已解决就不再检查

        bool hourCorrect = IsAngleCorrect(hourHand.CurrentAngle, correctHourAngle);
        bool minuteCorrect = IsAngleCorrect(minuteHand.CurrentAngle, correctMinuteAngle);
        bool secondCorrect = IsAngleCorrect(secondHand.CurrentAngle, correctSecondAngle);

        Debug.Log($"[ClockPuzzle] 检查: 时针{hourHand.CurrentAngle}({hourCorrect}) " +
                  $"分针{minuteHand.CurrentAngle}({minuteCorrect}) " +
                  $"秒针{secondHand.CurrentAngle}({secondCorrect})");

        if (hourCorrect && minuteCorrect && secondCorrect)
        {
            SolvePuzzle();
        }
    }

    /// <summary>
    /// 判断角度是否在容差范围内
    /// </summary>
    private bool IsAngleCorrect(float current, float target)
    {
        float diff = Mathf.Abs(Mathf.DeltaAngle(current, target));
        return diff <= angleTolerance;
    }

    /// <summary>
    /// 谜题解决
    /// </summary>
    private void SolvePuzzle()
    {
        isSolved = true;
        Debug.Log("[ClockPuzzle] ✅ 谜题解决！");

        // 切换时钟图片
        if (clockFaceA != null) clockFaceA.SetActive(false);
        if (clockFaceB != null) clockFaceB.SetActive(true);

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(successSound))
        {
            AudioManager.Instance.PlaySFX(successSound);
        }

        // 触发事件
        OnPuzzleSolved?.Invoke();
    }

    /// <summary>
    /// 重置谜题（测试用）
    /// </summary>
    [ContextMenu("重置谜题")]
    public void ResetPuzzle()
    {
        isSolved = false;
        if (clockFaceA != null) clockFaceA.SetActive(true);
        if (clockFaceB != null) clockFaceB.SetActive(false);

        // 重置指针位置
        hourHand?.SetAngle(0);
        minuteHand?.SetAngle(0);
        secondHand?.SetAngle(0);
    }
}