// Assets/Scripts/Audio/ZoomViewBGMController.cs
// ZoomView BGM 控制器 - 用于在放大视图中临时切换 BGM
// 功能：进入放大视图时播放特定 BGM，退出时恢复之前的 BGM

using UnityEngine;
using UnityEngine.Events;

public class ZoomViewBGMController : MonoBehaviour
{
    [Header("ZoomView BGM 设置")]
    [Tooltip("进入此放大视图时播放的 BGM")]
    public AudioManager.BGMType zoomViewBGM = AudioManager.BGMType.None;

    [Tooltip("退出时恢复之前的 BGM")]
    public bool restoreOnExit = true;

    [Tooltip("退出时恢复的 BGM（如果不设置，自动记忆进入前的 BGM）")]
    public AudioManager.BGMType exitBGM = AudioManager.BGMType.None;

    [Header("触发方式")]
    [Tooltip("OnEnable 时自动播放")]
    public bool playOnEnable = true;

    [Tooltip("OnDisable 时自动恢复")]
    public bool restoreOnDisable = true;

    [Header("事件")]
    public UnityEvent OnZoomBGMStarted;
    public UnityEvent OnBGMRestored;

    [Header("状态（只读）")]
    [SerializeField] private AudioManager.BGMType previousBGM = AudioManager.BGMType.None;
    [SerializeField] private bool hasCapturedPrevious = false;

    private void OnEnable()
    {
        if (playOnEnable)
        {
            CapturePreviousBGM();
            PlayZoomViewBGM();
        }
    }

    private void OnDisable()
    {
        if (restoreOnDisable && restoreOnExit)
        {
            RestorePreviousBGM();
        }
    }

    /// <summary>
    /// 记忆进入前的 BGM
    /// </summary>
    private void CapturePreviousBGM()
    {
        if (AudioManager.Instance == null) return;

        // 尝试从当前播放的 BGM 名称推断类型
        string currentBGMName = AudioManager.Instance.GetCurrentBGMName();

        if (!string.IsNullOrEmpty(currentBGMName) && currentBGMName != "None")
        {
            // 尝试解析枚举
            if (System.Enum.TryParse<AudioManager.BGMType>(currentBGMName, out var bgmType))
            {
                previousBGM = bgmType;
                hasCapturedPrevious = true;
                Debug.Log($"[ZoomViewBGMController] Captured previous BGM: {previousBGM}");
            }
        }
    }

    /// <summary>
    /// 播放放大视图 BGM
    /// </summary>
    public void PlayZoomViewBGM()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[ZoomViewBGMController] AudioManager not found!");
            return;
        }

        if (zoomViewBGM == AudioManager.BGMType.None)
        {
            Debug.Log("[ZoomViewBGMController] No ZoomView BGM configured");
            return;
        }

        AudioManager.Instance.PlayBGM(zoomViewBGM);
        OnZoomBGMStarted?.Invoke();
        Debug.Log($"[ZoomViewBGMController] ZoomView BGM started: {zoomViewBGM}");
    }

    /// <summary>
    /// 恢复之前的 BGM
    /// </summary>
    public void RestorePreviousBGM()
    {
        if (AudioManager.Instance == null) return;

        AudioManager.BGMType targetBGM = AudioManager.BGMType.None;

        // 优先使用配置的退出 BGM
        if (exitBGM != AudioManager.BGMType.None)
        {
            targetBGM = exitBGM;
        }
        // 其次使用记忆的 BGM
        else if (hasCapturedPrevious && previousBGM != AudioManager.BGMType.None)
        {
            targetBGM = previousBGM;
        }

        if (targetBGM != AudioManager.BGMType.None)
        {
            AudioManager.Instance.PlayBGM(targetBGM);
            OnBGMRestored?.Invoke();
            Debug.Log($"[ZoomViewBGMController] BGM restored to: {targetBGM}");
        }

        // 重置状态
        hasCapturedPrevious = false;
    }

    /// <summary>
    /// 手动设置退出时的 BGM
    /// </summary>
    public void SetExitBGM(AudioManager.BGMType bgm)
    {
        exitBGM = bgm;
    }
}