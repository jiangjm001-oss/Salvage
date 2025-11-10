// Assets/Scripts/Managers/AudioManager.cs - 更新版本
// 在原有AudioManager.cs的基础上添加以下方法

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音频源")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("音频状态")]
    private bool isMusicEnabled = true;
    private bool isSFXEnabled = true;

    [Header("音效资源缓存")]
    private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();

    // 事件
    public UnityEvent<bool> OnMusicToggled = new UnityEvent<bool>();
    public UnityEvent<bool> OnSFXToggled = new UnityEvent<bool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        LoadSettings();
        ApplySettings();
    }

    // ============ 音乐控制 ============

    public void ToggleMusic()
    {
        isMusicEnabled = !isMusicEnabled;
        ApplyMusicSettings();
        SaveSettings();
        OnMusicToggled.Invoke(isMusicEnabled);
        Debug.Log($"[AudioManager] Music toggled: {isMusicEnabled}");
    }

    public void SetMusicEnabled(bool enabled)
    {
        if (isMusicEnabled == enabled) return;
        isMusicEnabled = enabled;
        ApplyMusicSettings();
        SaveSettings();
        OnMusicToggled.Invoke(isMusicEnabled);
        Debug.Log($"[AudioManager] Music set to: {isMusicEnabled}");
    }

    public void PlayMusic(AudioClip clip, bool loop = true)
    {
        if (musicSource == null)
        {
            Debug.LogError("[AudioManager] Music AudioSource is null!");
            return;
        }

        musicSource.clip = clip;
        musicSource.loop = loop;
        if (isMusicEnabled)
        {
            musicSource.Play();
            Debug.Log($"[AudioManager] Playing music: {clip.name}");
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    private void ApplyMusicSettings()
    {
        if (musicSource != null)
        {
            if (isMusicEnabled)
            {
                if (!musicSource.isPlaying && musicSource.clip != null)
                {
                    musicSource.Play();
                }
            }
            else
            {
                musicSource.Pause();
            }
        }
    }

    // ============ 音效控制 ============

    public void ToggleSFX()
    {
        isSFXEnabled = !isSFXEnabled;
        ApplySFXSettings();
        SaveSettings();
        OnSFXToggled.Invoke(isSFXEnabled);
        Debug.Log($"[AudioManager] SFX toggled: {isSFXEnabled}");
    }

    public void SetSFXEnabled(bool enabled)
    {
        if (isSFXEnabled == enabled) return;
        isSFXEnabled = enabled;
        ApplySFXSettings();
        SaveSettings();
        OnSFXToggled.Invoke(isSFXEnabled);
        Debug.Log($"[AudioManager] SFX set to: {isSFXEnabled}");
    }

    /// <summary>
    /// 播放音效 - AudioClip版本
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (!isSFXEnabled) return;

        if (sfxSource == null)
        {
            Debug.LogError("[AudioManager] SFX AudioSource is null!");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] AudioClip is null!");
            return;
        }

        sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 播放音效 - 字符串路径版本(兼容旧代码)
    /// 从Resources文件夹加载音效
    /// </summary>
    public void PlaySFX(string sfxPath)
    {
        if (!isSFXEnabled) return;

        if (string.IsNullOrEmpty(sfxPath))
        {
            Debug.LogWarning("[AudioManager] SFX path is null or empty!");
            return;
        }

        // 尝试从缓存获取
        if (sfxCache.ContainsKey(sfxPath))
        {
            PlaySFX(sfxCache[sfxPath]);
            return;
        }

        // 从Resources加载
        AudioClip clip = Resources.Load<AudioClip>(sfxPath);
        if (clip != null)
        {
            sfxCache[sfxPath] = clip; // 缓存起来
            PlaySFX(clip);
            Debug.Log($"[AudioManager] Loaded and cached SFX: {sfxPath}");
        }
        else
        {
            Debug.LogError($"[AudioManager] Failed to load SFX from Resources: {sfxPath}");
        }
    }

    private void ApplySFXSettings()
    {
        if (sfxSource != null)
        {
            sfxSource.mute = !isSFXEnabled;
        }
    }

    // ============ 获取状态 ============

    public bool IsMusicEnabled() => isMusicEnabled;
    public bool IsSFXEnabled() => isSFXEnabled;

    // ============ 数据持久化 ============

    private void LoadSettings()
    {
        isMusicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        isSFXEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
        Debug.Log($"[AudioManager] Settings loaded - Music: {isMusicEnabled}, SFX: {isSFXEnabled}");
    }

    private void SaveSettings()
    {
        PlayerPrefs.SetInt("MusicEnabled", isMusicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", isSFXEnabled ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("[AudioManager] Settings saved");
    }

    private void ApplySettings()
    {
        ApplyMusicSettings();
        ApplySFXSettings();
    }

    /// <summary>
    /// 清除音效缓存(可选,用于内存管理)
    /// </summary>
    public void ClearSFXCache()
    {
        sfxCache.Clear();
        Debug.Log("[AudioManager] SFX cache cleared");
    }
}