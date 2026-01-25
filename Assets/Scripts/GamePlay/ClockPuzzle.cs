// Assets/Scripts/GamePlay/ClockPuzzle.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 时钟谜题控制器
/// 当时针和分针指向目标时间后，点击表盘确认，切换到完成状态
/// 
/// 使用方法：
/// 1. 创建 ClockA（带指针）和 ClockB（完成状态）两个物体
/// 2. 在 ClockA 下创建 HourHand 和 MinuteHand，各自添加 ClockHand 组件
/// 3. 将此脚本添加到表盘物体（需要有 Collider2D）
/// 4. 或者将 confirmArea 拖入一个可点击区域
/// </summary>
public class ClockPuzzle : MonoBehaviour
{
    [Header("指针引用")]
    [Tooltip("时针物体（需要有 ClockHand 组件）")]
    public ClockHand hourHand;

    [Tooltip("分针物体（需要有 ClockHand 组件）")]
    public ClockHand minuteHand;

    [Header("确认点击")]
    [Tooltip("点击确认区域（留空则使用自身 Collider）")]
    public Collider2D confirmArea;

    [Header("目标时间")]
    [Range(0, 11)]
    [Tooltip("目标小时（0-11，0代表12点）")]
    public int targetHour = 3;

    [Range(0, 59)]
    [Tooltip("目标分钟（0-59）")]
    public int targetMinute = 30;

    [Header("容差设置")]
    [Tooltip("时针容差（小时值，0.5 = 允许半小时误差）")]
    public float hourTolerance = 0.5f;

    [Tooltip("分针容差（分钟值，3 = 允许3分钟误差）")]
    public float minuteTolerance = 3f;

    [Header("切换对象")]
    [Tooltip("时钟A（带指针，谜题未完成时显示）")]
    public GameObject clockA;

    [Tooltip("时钟B（完成状态，谜题完成后显示）")]
    public GameObject clockB;

    [Header("音效")]
    [Tooltip("拨动指针时的滴答声")]
    public string tickSoundPath = "Audio/SFX/clock_tick";

    [Tooltip("点击表盘但时间错误时的音效")]
    public string errorSoundPath = "Audio/SFX/error";

    [Tooltip("谜题完成时的音效")]
    public string completeSoundPath = "Audio/SFX/puzzle_complete";

    [Header("事件")]
    [Tooltip("谜题完成时触发")]
    public UnityEvent OnPuzzleCompleted;

    [Header("存档")]
    [Tooltip("谜题唯一标识符（用于存档）")]
    public string puzzleID = "clock_puzzle_01";

    // 内部状态
    private bool isSolved = false;
    private bool isTimeCorrect = false;

    private void Start()
    {
        // 检查是否已完成（从存档恢复）
        if (PlayerPrefs.GetInt($"Puzzle_{puzzleID}_Solved", 0) == 1)
        {
            isSolved = true;
            SwitchToClockB();
            Debug.Log($"[ClockPuzzle] 谜题 {puzzleID} 已完成，直接显示 ClockB");
            return;
        }

        // 如果没有指定确认区域，使用自身的 Collider
        if (confirmArea == null)
        {
            confirmArea = GetComponent<Collider2D>();
        }

        // 注册指针变化事件
        if (hourHand != null)
        {
            hourHand.OnAngleChanged += OnHandMoved;
        }
        if (minuteHand != null)
        {
            minuteHand.OnAngleChanged += OnHandMoved;
        }

        // 恢复指针状态
        RestoreState();

        // 检查初始状态是否正确
        CheckSolution();

        Debug.Log($"[ClockPuzzle] 初始化完成，目标时间: {targetHour}:{targetMinute:D2}");
    }

    private void OnDestroy()
    {
        // 取消注册事件
        if (hourHand != null)
        {
            hourHand.OnAngleChanged -= OnHandMoved;
        }
        if (minuteHand != null)
        {
            minuteHand.OnAngleChanged -= OnHandMoved;
        }
    }

    /// <summary>
    /// 点击表盘确认
    /// </summary>
    private void OnMouseDown()
    {
        if (isSolved) return;

        // 检查是否点击在确认区域内
        if (confirmArea != null)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            if (!confirmArea.OverlapPoint(mousePos))
            {
                return; // 点击不在确认区域内
            }
        }

        Debug.Log("[ClockPuzzle] 点击表盘确认...");

        if (isTimeCorrect)
        {
            // 时间正确，完成谜题
            SolvePuzzle();
        }
        else
        {
            // 时间错误，播放错误音效
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(errorSoundPath))
            {
                AudioManager.Instance.PlaySFX(errorSoundPath);
            }
            Debug.Log("[ClockPuzzle] 时间不正确！");
        }
    }

    /// <summary>
    /// 指针被拨动时调用
    /// </summary>
    private void OnHandMoved()
    {
        if (isSolved) return;

        // 播放滴答声
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(tickSoundPath))
        {
            AudioManager.Instance.PlaySFX(tickSoundPath);
        }

        // 保存当前状态
        SaveState();

        // 检查是否正确
        CheckSolution();
    }

    /// <summary>
    /// 检查当前时间是否正确（只更新标志，不触发完成）
    /// </summary>
    private void CheckSolution()
    {
        if (hourHand == null || minuteHand == null) return;

        // 获取当前指针值
        float currentHour = hourHand.GetHourValue();       // 0-12
        float currentMinute = minuteHand.GetMinuteValue(); // 0-60

        // 目标值（时针只检查整点位置，不考虑分针偏移）
        float targetHourValue = targetHour;         // 例：3
        float targetMinuteValue = targetMinute;     // 例：30

        // 检查是否在容差范围内
        bool hourCorrect = Mathf.Abs(currentHour - targetHourValue) <= hourTolerance;
        bool minuteCorrect = Mathf.Abs(currentMinute - targetMinuteValue) <= minuteTolerance;

        // 处理12点的特殊情况（0和12是同一位置）
        if (targetHour == 0 || targetHour == 12)
        {
            hourCorrect = currentHour <= hourTolerance || currentHour >= (12 - hourTolerance);
        }

        // 处理0分的特殊情况（0和60是同一位置）
        if (targetMinute == 0)
        {
            minuteCorrect = currentMinute <= minuteTolerance || currentMinute >= (60 - minuteTolerance);
        }

        // 更新状态标志
        isTimeCorrect = hourCorrect && minuteCorrect;

        Debug.Log($"[ClockPuzzle] 当前: {currentHour:F1}点 {currentMinute:F0}分 | " +
                  $"目标: {targetHourValue}点 {targetMinuteValue}分 | " +
                  $"时间正确: {isTimeCorrect}");
    }

    /// <summary>
    /// 完成谜题
    /// </summary>
    private void SolvePuzzle()
    {
        isSolved = true;
        Debug.Log($"[ClockPuzzle] ✓ 谜题完成！时间: {targetHour}:{targetMinute:D2}");

        // 播放完成音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(completeSoundPath))
        {
            AudioManager.Instance.PlaySFX(completeSoundPath);
        }

        // 保存完成状态
        PlayerPrefs.SetInt($"Puzzle_{puzzleID}_Solved", 1);
        PlayerPrefs.Save();

        // 切换到 ClockB
        SwitchToClockB();

        // 触发完成事件
        OnPuzzleCompleted?.Invoke();

        // 保存游戏
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    /// <summary>
    /// 切换到完成状态（显示 ClockB，隐藏 ClockA）
    /// </summary>
    private void SwitchToClockB()
    {
        if (clockA != null)
        {
            clockA.SetActive(false);
        }
        if (clockB != null)
        {
            clockB.SetActive(true);
        }
    }

    /// <summary>
    /// 保存指针状态
    /// </summary>
    private void SaveState()
    {
        if (hourHand != null)
        {
            PlayerPrefs.SetFloat($"Puzzle_{puzzleID}_HourAngle", hourHand.CurrentAngle);
        }
        if (minuteHand != null)
        {
            PlayerPrefs.SetFloat($"Puzzle_{puzzleID}_MinuteAngle", minuteHand.CurrentAngle);
        }
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 恢复指针状态
    /// </summary>
    private void RestoreState()
    {
        string keyHour = $"Puzzle_{puzzleID}_HourAngle";
        string keyMinute = $"Puzzle_{puzzleID}_MinuteAngle";

        if (PlayerPrefs.HasKey(keyHour) && hourHand != null)
        {
            float angle = PlayerPrefs.GetFloat(keyHour);
            hourHand.SetAngle(angle);
            Debug.Log($"[ClockPuzzle] 恢复时针角度: {angle}");
        }

        if (PlayerPrefs.HasKey(keyMinute) && minuteHand != null)
        {
            float angle = PlayerPrefs.GetFloat(keyMinute);
            minuteHand.SetAngle(angle);
            Debug.Log($"[ClockPuzzle] 恢复分针角度: {angle}");
        }
    }

    /// <summary>
    /// 重置谜题（用于调试）
    /// </summary>
    [ContextMenu("重置谜题")]
    public void ResetPuzzle()
    {
        isSolved = false;

        PlayerPrefs.DeleteKey($"Puzzle_{puzzleID}_Solved");
        PlayerPrefs.DeleteKey($"Puzzle_{puzzleID}_HourAngle");
        PlayerPrefs.DeleteKey($"Puzzle_{puzzleID}_MinuteAngle");
        PlayerPrefs.Save();

        if (hourHand != null) hourHand.SetAngle(0);
        if (minuteHand != null) minuteHand.SetAngle(0);

        if (clockA != null) clockA.SetActive(true);
        if (clockB != null) clockB.SetActive(false);

        Debug.Log("[ClockPuzzle] 谜题已重置");
    }

    // ============ 编辑器辅助 ============
    private void OnValidate()
    {
        // 确保 puzzleID 不为空
        if (string.IsNullOrEmpty(puzzleID))
        {
            puzzleID = $"clock_puzzle_{GetInstanceID()}";
        }
    }
}