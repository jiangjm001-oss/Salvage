// Assets/Scripts/Managers/AudioManager.cs
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Settings")]
    private bool musicEnabled = true;
    private bool sfxEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // 创建AudioSource组件（如果没有）
        SetupAudioSources();
    }

    /// <summary>
    /// 设置AudioSource组件
    /// </summary>
    private void SetupAudioSources()
    {
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            Debug.Log("[AudioManager] Music AudioSource created");
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            Debug.Log("[AudioManager] SFX AudioSource created");
        }
    }

    /// <summary>
    /// 设置音乐开关
    /// </summary>
    public void SetMusicEnabled(bool enabled)
    {
        musicEnabled = enabled;

        if (musicSource != null)
        {
            if (!musicEnabled && musicSource.isPlaying)
            {
                musicSource.Pause();
            }
            else if (musicEnabled && !musicSource.isPlaying && musicSource.clip != null)
            {
                musicSource.UnPause();
            }
        }

        Debug.Log($"[AudioManager] Music {(musicEnabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 设置音效开关
    /// </summary>
    public void SetSFXEnabled(bool enabled)
    {
        sfxEnabled = enabled;
        Debug.Log($"[AudioManager] SFX {(sfxEnabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayMusic(string musicName)
    {
        Debug.Log($"[AudioManager] Playing music '{musicName}'.");
        // TODO: 实现根据 musicName 查找并播放 Audio Clip 的逻辑

        if (musicSource != null && musicEnabled)
        {
            // 示例：如果你有音乐资源，可以这样加载和播放
            // AudioClip clip = Resources.Load<AudioClip>($"Audio/Music/{musicName}");
            // if (clip != null)
            // {
            //     musicSource.clip = clip;
            //     musicSource.Play();
            // }
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public void PlaySFX(string sfxName)
    {
        if (!sfxEnabled || sfxSource == null)
            return;

        Debug.Log($"[AudioManager] Playing SFX '{sfxName}'.");
        // TODO: 实现播放音效逻辑

        // 示例：如果你有音效资源，可以这样加载和播放
        // AudioClip clip = Resources.Load<AudioClip>($"Audio/SFX/{sfxName}");
        // if (clip != null)
        // {
        //     sfxSource.PlayOneShot(clip);
        // }
    }

    /// <summary>
    /// 停止音乐
    /// </summary>
    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// 获取音乐开关状态
    /// </summary>
    public bool IsMusicEnabled()
    {
        return musicEnabled;
    }

    /// <summary>
    /// 获取音效开关状态
    /// </summary>
    public bool IsSFXEnabled()
    {
        return sfxEnabled;
    }
}