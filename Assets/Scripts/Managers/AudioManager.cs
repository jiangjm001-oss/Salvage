// Assets/Scripts/Managers/AudioManager.cs - 完整升级版
// 新增功能：
// 1. BGM 淡入淡出切换
// 2. SFX Library 支持（拖拽配置音效）
// 【重要】完全兼容现有代码，无需修改任何谜题脚本

using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ============ 音频源 ============
    [Header("音频源")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    // ============ 音效库 ============
    [Header("音效库（拖入 SFXLibrary 资产）")]
    [Tooltip("拖入 SFXLibrary.asset，可在其中集中配置所有音效")]
    public SFXLibrary sfxLibrary;

    // ============ BGM 配置 ============
    [Header("BGM 配置（直接拖入音频文件）")]
    [Tooltip("主菜单 BGM")]
    public AudioClip bgm1_MainMenu;

    [Tooltip("第一关过渡动画 BGM")]
    public AudioClip bgm2_Level1Transition;

    [Tooltip("第二关 BGM")]
    public AudioClip bgm3_Level2;

    [Tooltip("第二关 Dream ZoomView BGM")]
    public AudioClip bgm4_Dream;

    [Tooltip("水晶谜题完成后 BGM")]
    public AudioClip bgm5_CrystalComplete;

    [Header("BGM 切换设置")]
    [Tooltip("淡入淡出时长")]
    [Range(0.5f, 3f)]
    public float fadeDuration = 1.5f;

    [Tooltip("BGM 音量")]
    [Range(0f, 1f)]
    public float musicVolume = 0.7f;

    // ============ 状态追踪 ============
    [Header("状态（只读）")]
    [SerializeField] private string currentBGMName = "None";
    [SerializeField] private bool isTransitioning = false;

    private AudioClip currentBGM = null;
    private Coroutine fadeCoroutine = null;
    private bool isMusicEnabled = true;
    private bool isSFXEnabled = true;

    // 音效缓存（Resources 加载的音效）
    private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();

    // 事件
    public UnityEvent<bool> OnMusicToggled = new UnityEvent<bool>();
    public UnityEvent<bool> OnSFXToggled = new UnityEvent<bool>();
    public UnityEvent<string> OnBGMChanged = new UnityEvent<string>();

    // ============ BGM 枚举 ============
    public enum BGMType
    {
        None,
        BGM1_MainMenu,
        BGM2_Level1Transition,
        BGM3_Level2,
        BGM4_Dream,
        BGM5_CrystalComplete
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[AudioManager] Instance has been set.");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Duplicate detected on {gameObject.name}! Destroying this component.");
            Destroy(this);
            return;
        }

        LoadSettings();
        ApplySettings();

        // 验证 SFXLibrary
        if (sfxLibrary == null)
        {
            Debug.LogWarning("[AudioManager] SFXLibrary not assigned! Sound effects will use Resources.Load as fallback.");
        }
        else
        {
            sfxLibrary.BuildCache();
            Debug.Log($"[AudioManager] SFXLibrary loaded with {sfxLibrary.sfxEntries.Count} entries.");
        }
    }

    // ============ 🎵 BGM 快捷播放方法 ============

    public void PlayBGM_MainMenu()
    {
        SwitchBGM(bgm1_MainMenu, "BGM1_MainMenu");
    }

    public void PlayBGM_Level1Transition()
    {
        SwitchBGM(bgm2_Level1Transition, "BGM2_Level1Transition");
    }

    public void PlayBGM_Level2()
    {
        SwitchBGM(bgm3_Level2, "BGM3_Level2");
    }

    public void PlayBGM_Dream()
    {
        SwitchBGM(bgm4_Dream, "BGM4_Dream");
    }

    public void PlayBGM_CrystalComplete()
    {
        SwitchBGM(bgm5_CrystalComplete, "BGM5_CrystalComplete");
    }

    public void PlayBGM(BGMType bgmType)
    {
        switch (bgmType)
        {
            case BGMType.BGM1_MainMenu:
                PlayBGM_MainMenu();
                break;
            case BGMType.BGM2_Level1Transition:
                PlayBGM_Level1Transition();
                break;
            case BGMType.BGM3_Level2:
                PlayBGM_Level2();
                break;
            case BGMType.BGM4_Dream:
                PlayBGM_Dream();
                break;
            case BGMType.BGM5_CrystalComplete:
                PlayBGM_CrystalComplete();
                break;
            case BGMType.None:
                StopBGMWithFade();
                break;
        }
    }

    // ============ 🎵 BGM 核心切换逻辑 ============

    public void SwitchBGM(AudioClip newClip, string clipName = "")
    {
        if (newClip == null)
        {
            Debug.LogWarning($"[AudioManager] BGM clip is null: {clipName}");
            return;
        }

        if (currentBGM == newClip && musicSource.isPlaying)
        {
            Debug.Log($"[AudioManager] BGM already playing: {clipName}");
            return;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(CrossfadeBGM(newClip, clipName));
    }

    public void StopBGMWithFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutBGM());
    }

    public void StopBGMImmediate()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
            currentBGM = null;
            currentBGMName = "None";
        }
    }

    private IEnumerator CrossfadeBGM(AudioClip newClip, string clipName)
    {
        isTransitioning = true;
        float halfDuration = fadeDuration / 2f;

        // 淡出
        if (musicSource.isPlaying && musicSource.volume > 0)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float smoothT = Mathf.SmoothStep(0, 1, t);
                musicSource.volume = Mathf.Lerp(startVolume, 0f, smoothT);
                yield return null;
            }

            musicSource.Stop();
        }

        // 切换并淡入
        currentBGM = newClip;
        currentBGMName = string.IsNullOrEmpty(clipName) ? newClip.name : clipName;

        musicSource.clip = newClip;
        musicSource.loop = true;
        musicSource.volume = 0f;

        if (isMusicEnabled)
        {
            musicSource.Play();

            float elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float smoothT = Mathf.SmoothStep(0, 1, t);
                musicSource.volume = Mathf.Lerp(0f, musicVolume, smoothT);
                yield return null;
            }

            musicSource.volume = musicVolume;
        }

        isTransitioning = false;
        OnBGMChanged?.Invoke(currentBGMName);
        Debug.Log($"[AudioManager] ♫ BGM switched to: {currentBGMName}");
    }

    private IEnumerator FadeOutBGM()
    {
        isTransitioning = true;

        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                float smoothT = Mathf.SmoothStep(0, 1, t);
                musicSource.volume = Mathf.Lerp(startVolume, 0f, smoothT);
                yield return null;
            }

            musicSource.Stop();
            musicSource.clip = null;
        }

        currentBGM = null;
        currentBGMName = "None";
        isTransitioning = false;
        OnBGMChanged?.Invoke("None");
        Debug.Log("[AudioManager] ♫ BGM stopped");
    }

    // ============ 音乐开关 ============

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

        SwitchBGM(clip, clip?.name ?? "Unknown");
    }

    public void StopMusic()
    {
        StopBGMWithFade();
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
                if (!isTransitioning)
                {
                    musicSource.volume = musicVolume;
                }
            }
            else
            {
                musicSource.Pause();
            }
        }
    }

    // ============ 🔊 音效控制 ============

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
    /// 播放音效 - AudioClip 版本
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
    /// 播放音效 - 带音量版本
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
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

        sfxSource.PlayOneShot(clip, volumeScale);
    }

    /// <summary>
    /// 播放音效 - 字符串路径版本
    /// 【核心改进】优先从 SFXLibrary 查找，找不到再用 Resources 加载
    /// 完全兼容现有代码！
    /// </summary>
    public void PlaySFX(string sfxPath)
    {
        if (!isSFXEnabled) return;

        if (string.IsNullOrEmpty(sfxPath))
        {
            Debug.LogWarning("[AudioManager] SFX path is null or empty!");
            return;
        }

        AudioClip clip = null;

        // ⭐ 优先从 SFXLibrary 查找
        if (sfxLibrary != null)
        {
            clip = sfxLibrary.GetClip(sfxPath);
            if (clip != null)
            {
                float volume = sfxLibrary.GetVolume(sfxPath);
                PlaySFX(clip, volume);
                return;
            }
        }

        // 尝试从缓存获取
        if (sfxCache.TryGetValue(sfxPath, out clip))
        {
            PlaySFX(clip);
            return;
        }

        // 从 Resources 加载（兜底方案）
        clip = Resources.Load<AudioClip>(sfxPath);
        if (clip != null)
        {
            sfxCache[sfxPath] = clip;
            PlaySFX(clip);
            Debug.Log($"[AudioManager] Loaded SFX from Resources: {sfxPath}");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] SFX not found: {sfxPath} (Check SFXLibrary or Resources folder)");
        }
    }

    /// <summary>
    /// 播放音效 - 带音量的字符串版本
    /// </summary>
    public void PlaySFXWithVolume(string sfxPath, float volumeScale)
    {
        if (!isSFXEnabled) return;

        if (string.IsNullOrEmpty(sfxPath))
        {
            Debug.LogWarning("[AudioManager] SFX path is null or empty!");
            return;
        }

        // 从 SFXLibrary 查找
        if (sfxLibrary != null)
        {
            var clip = sfxLibrary.GetClip(sfxPath);
            if (clip != null)
            {
                PlaySFX(clip, volumeScale);
                return;
            }
        }

        Debug.LogWarning($"[AudioManager] SFX not found for volume playback: '{sfxPath}'");
    }

    /// <summary>
    /// 检查音效是否存在
    /// </summary>
    public bool HasSFX(string sfxName)
    {
        if (sfxLibrary != null && sfxLibrary.HasSFX(sfxName))
        {
            return true;
        }

        if (sfxCache.ContainsKey(sfxName))
        {
            return true;
        }

        return false;
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
    public string GetCurrentBGMName() => currentBGMName;
    public bool IsTransitioning() => isTransitioning;

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

    public void ClearSFXCache()
    {
        sfxCache.Clear();
        Debug.Log("[AudioManager] SFX cache cleared");
    }

    /// <summary>
    /// 重新加载音效库（编辑器中修改后调用）
    /// </summary>
    public void ReloadSFXLibrary()
    {
        if (sfxLibrary != null)
        {
            sfxLibrary.ClearCache();
            sfxLibrary.BuildCache();
            Debug.Log("[AudioManager] SFX Library reloaded");
        }
    }

    /// <summary>
    /// 获取音效库中所有音效名称（用于调试）
    /// </summary>
    public List<string> GetAllSFXNames()
    {
        if (sfxLibrary != null)
        {
            return sfxLibrary.GetAllSFXNames();
        }
        return new List<string>();
    }
}