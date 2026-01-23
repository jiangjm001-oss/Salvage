// Assets/Scripts/GamePlay/AlarmClockPuzzle.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 闹钟谜题控制器
/// 玩家通过点击指针调整时间，达到目标时间后弹出暗盒
/// </summary>
public class AlarmClockPuzzle : MonoBehaviour
{
    [Header("指针引用")]
    [Tooltip("时针 Transform")]
    public Transform hourHand;

    [Tooltip("分针 Transform")]
    public Transform minuteHand;

    [Tooltip("秒针 Transform")]
    public Transform secondHand;

    [Header("目标时间")]
    [Tooltip("目标小时 (1-12)")]
    [Range(1, 12)]
    public int targetHour = 11;

    [Tooltip("目标分钟 (0-59)")]
    [Range(0, 59)]
    public int targetMinute = 20;

    [Tooltip("目标秒钟 (0-59)")]
    [Range(0, 59)]
    public int targetSecond = 25;

    [Header("旋转设置")]
    [Tooltip("时针每次点击旋转角度（对应1小时 = 30度）")]
    public float hourStep = 30f;

    [Tooltip("分针每次点击旋转角度（对应5分钟 = 30度）")]
    public float minuteStep = 30f;

    [Tooltip("秒针每次点击旋转角度（对应5秒 = 30度）")]
    public float secondStep = 30f;

    [Tooltip("旋转动画时间")]
    public float rotationDuration = 0.2f;

    [Header("暗盒设置")]
    [Tooltip("暗盒 GameObject（完成后显示）")]
    public GameObject secretCompartment;

    [Tooltip("暗盒内的物品（胶水等）")]
    public GameObject[] containedItems;

    [Tooltip("暗盒弹出动画时间")]
    public float compartmentAnimDuration = 0.5f;

    [Tooltip("暗盒弹出偏移")]
    public Vector3 compartmentOpenOffset = new Vector3(0, -0.5f, 0);

    [Header("音效")]
    [Tooltip("指针旋转音效")]
    public string tickSoundPath = "Audio/SFX/clock_tick";

    [Tooltip("谜题完成音效")]
    public string completeSoundPath = "Audio/SFX/puzzle_complete";

    [Tooltip("暗盒弹出音效")]
    public string compartmentOpenSoundPath = "Audio/SFX/compartment_open";

    [Header("事件")]
    public UnityEvent OnPuzzleCompleted;

    [Header("存档")]
    [Tooltip("唯一标识符")]
    public string puzzleID = "alarm_clock_puzzle_01";

    // 内部状态
    private int currentHour = 12;      // 当前小时 (1-12, 12表示0点位置)
    private int currentMinute = 0;     // 当前分钟 (0-59)
    private int currentSecond = 0;     // 当前秒钟 (0-59)
    private bool isSolved = false;
    private bool isAnimating = false;
    private Vector3 compartmentClosedPos;

    private void Start()
    {
        // 记录暗盒初始位置
        if (secretCompartment != null)
        {
            compartmentClosedPos = secretCompartment.transform.localPosition;
            secretCompartment.SetActive(false);
        }

        // 隐藏暗盒内物品
        HideContainedItems();

        // 尝试恢复存档
        LoadState();

        // 如果已完成，直接显示暗盒
        if (isSolved)
        {
            ShowCompartmentImmediate();
        }
        else
        {
            // 更新指针显示
            UpdateHandsVisual();
        }
    }

    /// <summary>
    /// 点击时针
    /// </summary>
    public void OnHourHandClicked()
    {
        if (isSolved || isAnimating) return;

        currentHour++;
        if (currentHour > 12) currentHour = 1;

        StartCoroutine(RotateHand(hourHand, -hourStep));
        PlayTickSound();
        CheckSolution();
        SaveState();
    }

    /// <summary>
    /// 点击分针
    /// </summary>
    public void OnMinuteHandClicked()
    {
        if (isSolved || isAnimating) return;

        currentMinute += 5;
        if (currentMinute >= 60) currentMinute = 0;

        StartCoroutine(RotateHand(minuteHand, -minuteStep));
        PlayTickSound();
        CheckSolution();
        SaveState();
    }

    /// <summary>
    /// 点击秒针
    /// </summary>
    public void OnSecondHandClicked()
    {
        if (isSolved || isAnimating) return;

        currentSecond += 5;
        if (currentSecond >= 60) currentSecond = 0;

        StartCoroutine(RotateHand(secondHand, -secondStep));
        PlayTickSound();
        CheckSolution();
        SaveState();
    }

    /// <summary>
    /// 旋转指针动画
    /// </summary>
    private IEnumerator RotateHand(Transform hand, float angle)
    {
        if (hand == null) yield break;

        isAnimating = true;

        Quaternion startRot = hand.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0, 0, angle);

        float elapsed = 0f;
        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / rotationDuration);
            hand.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        hand.localRotation = endRot;
        isAnimating = false;
    }

    /// <summary>
    /// 检查是否完成
    /// </summary>
    private void CheckSolution()
    {
        // 将12点转换为0进行比较
        int checkHour = currentHour == 12 ? 0 : currentHour;
        int targetH = targetHour == 12 ? 0 : targetHour;

        bool hourMatch = checkHour == targetH;
        bool minuteMatch = currentMinute == targetMinute;
        bool secondMatch = currentSecond == targetSecond;

        Debug.Log($"[AlarmClockPuzzle] 当前时间: {currentHour}:{currentMinute:D2}:{currentSecond:D2}, " +
                  $"目标: {targetHour}:{targetMinute:D2}:{targetSecond:D2}");

        if (hourMatch && minuteMatch && secondMatch)
        {
            OnSolved();
        }
    }

    /// <summary>
    /// 谜题完成
    /// </summary>
    private void OnSolved()
    {
        isSolved = true;
        Debug.Log("[AlarmClockPuzzle] ✓ 谜题完成！时间正确！");

        // 播放完成音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(completeSoundPath))
        {
            AudioManager.Instance.PlaySFX(completeSoundPath);
        }

        // 弹出暗盒
        StartCoroutine(OpenCompartment());

        // 触发事件
        OnPuzzleCompleted?.Invoke();

        // 保存状态
        SaveState();
    }

    /// <summary>
    /// 暗盒弹出动画
    /// </summary>
    private IEnumerator OpenCompartment()
    {
        if (secretCompartment == null) yield break;

        // 显示暗盒
        secretCompartment.SetActive(true);

        // 播放暗盒音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(compartmentOpenSoundPath))
        {
            AudioManager.Instance.PlaySFX(compartmentOpenSoundPath);
        }

        // 弹出动画
        Vector3 startPos = compartmentClosedPos;
        Vector3 endPos = compartmentClosedPos + compartmentOpenOffset;

        float elapsed = 0f;
        while (elapsed < compartmentAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / compartmentAnimDuration);
            secretCompartment.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        secretCompartment.transform.localPosition = endPos;

        // 显示内部物品
        ShowContainedItems();
    }

    /// <summary>
    /// 立即显示暗盒（用于存档恢复）
    /// </summary>
    private void ShowCompartmentImmediate()
    {
        if (secretCompartment == null) return;

        secretCompartment.SetActive(true);
        secretCompartment.transform.localPosition = compartmentClosedPos + compartmentOpenOffset;
        ShowContainedItems();
    }

    /// <summary>
    /// 显示暗盒内物品
    /// </summary>
    private void ShowContainedItems()
    {
        if (containedItems == null) return;

        foreach (var item in containedItems)
        {
            if (item == null) continue;

            // 检查是否已被拾取
            InteractableObject interactable = item.GetComponent<InteractableObject>();
            if (interactable != null && interactable.hasBeenPickedUp)
            {
                continue;
            }

            item.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏暗盒内物品
    /// </summary>
    private void HideContainedItems()
    {
        if (containedItems == null) return;

        foreach (var item in containedItems)
        {
            if (item != null)
            {
                item.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 更新指针显示（根据当前状态）
    /// </summary>
    private void UpdateHandsVisual()
    {
        // 计算时针角度：12点为0度，顺时针为负
        // 每小时30度
        float hourAngle = -((currentHour % 12) * 30f);
        if (hourHand != null)
        {
            hourHand.localRotation = Quaternion.Euler(0, 0, hourAngle);
        }

        // 计算分针角度：每分钟6度
        float minuteAngle = -(currentMinute * 6f);
        if (minuteHand != null)
        {
            minuteHand.localRotation = Quaternion.Euler(0, 0, minuteAngle);
        }

        // 计算秒针角度：每秒6度
        float secondAngle = -(currentSecond * 6f);
        if (secondHand != null)
        {
            secondHand.localRotation = Quaternion.Euler(0, 0, secondAngle);
        }
    }

    /// <summary>
    /// 播放指针旋转音效
    /// </summary>
    private void PlayTickSound()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(tickSoundPath))
        {
            AudioManager.Instance.PlaySFX(tickSoundPath);
        }
    }

    // ============ 存档功能 ============

    private void SaveState()
    {
        string key = $"Puzzle_{puzzleID}";
        string data = $"{currentHour},{currentMinute},{currentSecond},{(isSolved ? 1 : 0)}";
        PlayerPrefs.SetString(key, data);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        string key = $"Puzzle_{puzzleID}";
        if (!PlayerPrefs.HasKey(key)) return;

        string data = PlayerPrefs.GetString(key);
        string[] parts = data.Split(',');

        if (parts.Length >= 4)
        {
            currentHour = int.Parse(parts[0]);
            currentMinute = int.Parse(parts[1]);
            currentSecond = int.Parse(parts[2]);
            isSolved = parts[3] == "1";

            Debug.Log($"[AlarmClockPuzzle] 恢复状态: {currentHour}:{currentMinute:D2}:{currentSecond:D2}, 已完成: {isSolved}");
        }
    }

    private void OnDisable()
    {
        SaveState();
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 获取当前时间字符串（用于调试或UI显示）
    /// </summary>
    public string GetCurrentTimeString()
    {
        return $"{currentHour}:{currentMinute:D2}:{currentSecond:D2}";
    }

    /// <summary>
    /// 检查谜题是否已完成
    /// </summary>
    public bool IsSolved => isSolved;

    // ============ 编辑器辅助 ============

    private void OnDrawGizmosSelected()
    {
        // 绘制暗盒弹出位置
        if (secretCompartment != null)
        {
            Gizmos.color = Color.green;
            Vector3 openPos = secretCompartment.transform.position + compartmentOpenOffset;
            Gizmos.DrawWireCube(openPos, new Vector3(0.5f, 0.3f, 0.1f));
        }
    }
}