using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 设置管理器 - 管理游戏设置（音乐、音效等）
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("设置状态")]
    [SerializeField] private bool musicEnabled = true;
    [SerializeField] private bool sfxEnabled = true;

    // 设置变化事件
    public UnityEvent<bool> OnMusicEnabledChanged = new UnityEvent<bool>();
    public UnityEvent<bool> OnSFXEnabledChanged = new UnityEvent<bool>();

    // 属性访问器
    public bool IsMusicEnabled => musicEnabled;
    public bool IsSFXEnabled => sfxEnabled;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 应用初始设置
        ApplyMusicSetting();
        ApplySFXSetting();
    }

    /// <summary>
    /// 切换音乐开关
    /// </summary>
    public void ToggleMusic()
    {
        musicEnabled = !musicEnabled;
        SaveSettings();
        ApplyMusicSetting();
        OnMusicEnabledChanged?.Invoke(musicEnabled);
        Debug.Log($"[SettingsManager] Music {(musicEnabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 切换音效开关
    /// </summary>
    public void ToggleSFX()
    {
        sfxEnabled = !sfxEnabled;
        SaveSettings();
        ApplySFXSetting();
        OnSFXEnabledChanged?.Invoke(sfxEnabled);
        Debug.Log($"[SettingsManager] SFX {(sfxEnabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 设置音乐开关
    /// </summary>
    public void SetMusicEnabled(bool enabled)
    {
        if (musicEnabled == enabled) return;

        musicEnabled = enabled;
        SaveSettings();
        ApplyMusicSetting();
        OnMusicEnabledChanged?.Invoke(musicEnabled);
        Debug.Log($"[SettingsManager] Music {(musicEnabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 设置音效开关
    /// </summary>
    public void SetSFXEnabled(bool enabled)
    {
        if (sfxEnabled == enabled) return;

        sfxEnabled = enabled;
        SaveSettings();
        ApplySFXSetting();
        OnSFXEnabledChanged?.Invoke(sfxEnabled);
        Debug.Log($"[SettingsManager] SFX {(sfxEnabled ? "enabled" : "disabled")}");
    }

    /// <summary>
    /// 应用音乐设置
    /// </summary>
    private void ApplyMusicSetting()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicEnabled(musicEnabled);
        }
    }

    /// <summary>
    /// 应用音效设置
    /// </summary>
    private void ApplySFXSetting()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXEnabled(sfxEnabled);
        }
    }

    /// <summary>
    /// 保存设置到PlayerPrefs
    /// </summary>
    private void SaveSettings()
    {
        PlayerPrefs.SetInt("MusicEnabled", musicEnabled ? 1 : 0);
        PlayerPrefs.SetInt("SFXEnabled", sfxEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从PlayerPrefs加载设置
    /// </summary>
    private void LoadSettings()
    {
        musicEnabled = PlayerPrefs.GetInt("MusicEnabled", 1) == 1;
        sfxEnabled = PlayerPrefs.GetInt("SFXEnabled", 1) == 1;
        Debug.Log($"[SettingsManager] Settings loaded - Music: {musicEnabled}, SFX: {sfxEnabled}");
    }
}
