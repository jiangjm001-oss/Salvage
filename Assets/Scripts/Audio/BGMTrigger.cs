// Assets/Scripts/Audio/BGMTrigger.cs
// 通用BGM触发器 - 可挂载到任意物体，通过 Unity Event 或代码触发
// 使用方法：
// 1. 将此组件挂载到需要触发BGM的物体上
// 2. 选择要播放的 BGM 类型
// 3. 在 Inspector 中将 TriggerBGM() 连接到其他事件

using UnityEngine;
using UnityEngine.Events;

public class BGMTrigger : MonoBehaviour
{
    [Header("BGM 设置")]
    [Tooltip("要播放的 BGM")]
    public AudioManager.BGMType bgmToPlay = AudioManager.BGMType.None;

    [Header("触发方式")]
    [Tooltip("启用时自动播放（适用于场景启动时）")]
    public bool playOnEnable = false;

    [Tooltip("Start 时播放")]
    public bool playOnStart = false;

    [Tooltip("仅触发一次")]
    public bool triggerOnce = true;

    [Header("延迟设置")]
    [Tooltip("触发前延迟（秒）")]
    public float delayBeforePlay = 0f;

    [Header("事件")]
    [Tooltip("BGM 开始播放时触发")]
    public UnityEvent OnBGMStarted;

    [Header("状态（只读）")]
    [SerializeField] private bool hasTriggered = false;

    private void OnEnable()
    {
        if (playOnEnable && CanTrigger())
        {
            TriggerBGM();
        }
    }

    private void Start()
    {
        if (playOnStart && CanTrigger())
        {
            TriggerBGM();
        }
    }

    /// <summary>
    /// 检查是否可以触发
    /// </summary>
    private bool CanTrigger()
    {
        if (triggerOnce && hasTriggered)
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// 触发 BGM 播放（可被 Unity Event 调用）
    /// </summary>
    public void TriggerBGM()
    {
        if (!CanTrigger())
        {
            Debug.Log($"[BGMTrigger] {gameObject.name}: 已触发过，跳过");
            return;
        }

        hasTriggered = true;

        if (delayBeforePlay > 0)
        {
            StartCoroutine(PlayWithDelay());
        }
        else
        {
            PlayBGMNow();
        }
    }

    private System.Collections.IEnumerator PlayWithDelay()
    {
        yield return new WaitForSeconds(delayBeforePlay);
        PlayBGMNow();
    }

    private void PlayBGMNow()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogError("[BGMTrigger] AudioManager.Instance is null!");
            return;
        }

        if (bgmToPlay == AudioManager.BGMType.None)
        {
            AudioManager.Instance.StopBGMWithFade();
            Debug.Log($"[BGMTrigger] {gameObject.name}: 停止 BGM");
        }
        else
        {
            AudioManager.Instance.PlayBGM(bgmToPlay);
            Debug.Log($"[BGMTrigger] {gameObject.name}: 播放 {bgmToPlay}");
        }

        OnBGMStarted?.Invoke();
    }

    /// <summary>
    /// 重置触发状态（允许再次触发）
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    /// <summary>
    /// 设置要播放的 BGM（可被代码调用）
    /// </summary>
    public void SetBGMType(AudioManager.BGMType type)
    {
        bgmToPlay = type;
    }
}