// Assets/Scripts/GamePlay/StateAudioPlayer.cs
using UnityEngine;

/// <summary>
/// 状态音频播放器 - 根据另一物体的状态播放对应音频
/// 适用场景：收音机、留声机、电视机等
/// 点击播放 → 再点击停止 → 再点击从头播放
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class StateAudioPlayer : MonoBehaviour
{
    [Header("状态源")]
    [Tooltip("拖入控制状态的物体（如天线）")]
    public CycleStateObject stateSource;

    [Header("音频配置")]
    [Tooltip("每个状态对应的音频（顺序与 stateSource 的状态对应）")]
    public AudioClip[] stateAudioClips;

    [Header("播放设置")]
    [Tooltip("是否循环播放")]
    public bool loopAudio = false;

    [Tooltip("音量（0-1）")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("运行时状态（只读）")]
    [SerializeField]
    private bool isPlaying = false;

    // 私有 AudioSource（在此物体上播放）
    private AudioSource audioSource;

    private void Awake()
    {
        // 获取或创建 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 配置 AudioSource
        audioSource.playOnAwake = false;
        audioSource.loop = loopAudio;
        audioSource.volume = volume;
    }

    /// <summary>
    /// 切换播放/停止（点击触发）
    /// </summary>
    public void TogglePlay()
    {
        if (isPlaying)
        {
            StopAudio();
        }
        else
        {
            PlayCurrentStateAudio();
        }
    }

    /// <summary>
    /// 播放当前状态对应的音频
    /// </summary>
    public void PlayCurrentStateAudio()
    {
        if (stateAudioClips == null || stateAudioClips.Length == 0)
        {
            Debug.LogWarning($"[StateAudioPlayer] '{gameObject.name}' 没有配置音频！");
            return;
        }

        // 获取状态索引
        int stateIndex = 0;
        if (stateSource != null)
        {
            stateIndex = stateSource.CurrentStateIndex;
        }

        // 边界检查
        if (stateIndex < 0 || stateIndex >= stateAudioClips.Length)
        {
            Debug.LogWarning($"[StateAudioPlayer] 状态索引 {stateIndex} 超出音频数组范围！");
            return;
        }

        AudioClip clipToPlay = stateAudioClips[stateIndex];
        if (clipToPlay == null)
        {
            Debug.LogWarning($"[StateAudioPlayer] 状态 {stateIndex} 的音频为空！");
            return;
        }

        // 停止当前播放，从头开始
        audioSource.Stop();
        audioSource.clip = clipToPlay;
        audioSource.loop = loopAudio;
        audioSource.volume = volume;
        audioSource.Play();

        isPlaying = true;

        Debug.Log($"[StateAudioPlayer] 播放状态 {stateIndex} 的音频: {clipToPlay.name}");
    }

    /// <summary>
    /// 停止播放
    /// </summary>
    public void StopAudio()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
        isPlaying = false;

        Debug.Log($"[StateAudioPlayer] 停止播放");
    }

    /// <summary>
    /// 检查是否正在播放
    /// </summary>
    public bool IsPlaying => isPlaying && audioSource != null && audioSource.isPlaying;

    private void Update()
    {
        // 同步播放状态（处理音频自然结束的情况）
        if (isPlaying && audioSource != null && !audioSource.isPlaying && !loopAudio)
        {
            isPlaying = false;
        }
    }

    // ============ 点击检测 ============

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TogglePlay();
    }

    private void OnDisable()
    {
        // 物体禁用时停止播放
        StopAudio();
    }
}