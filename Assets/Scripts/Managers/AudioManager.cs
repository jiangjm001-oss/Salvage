// Assets/Scripts/Managers/AudioManager.cs - ���°汾
// ��ԭ��AudioManager.cs�Ļ������������·���

using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("��ƵԴ")]
    public AudioSource musicSource;
    public AudioSource sfxSource;

    [Header("��Ƶ״̬")]
    private bool isMusicEnabled = true;
    private bool isSFXEnabled = true;

    [Header("��Ч��Դ����")]
    private Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>();

    // �¼�
    public UnityEvent<bool> OnMusicToggled = new UnityEvent<bool>();
    public UnityEvent<bool> OnSFXToggled = new UnityEvent<bool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[AudioManager] Instance has been set.");
        }
        else
        {
            Debug.LogWarning($"[AudioManager] Duplicate AudioManager detected on {gameObject.name}! Destroying this component only.");
            Destroy(this);  // 只销毁组件，不销毁整个 GameObject
            return;
        }

        LoadSettings();
        ApplySettings();
    }

    // ============ ���ֿ��� ============

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

    // ============ ��Ч���� ============

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
    /// ������Ч - AudioClip�汾
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
    /// ������Ч - �ַ���·���汾(���ݾɴ���)
    /// ��Resources�ļ��м�����Ч
    /// </summary>
    public void PlaySFX(string sfxPath)
    {
        if (!isSFXEnabled) return;

        if (string.IsNullOrEmpty(sfxPath))
        {
            Debug.LogWarning("[AudioManager] SFX path is null or empty!");
            return;
        }

        // ���Դӻ����ȡ
        if (sfxCache.ContainsKey(sfxPath))
        {
            PlaySFX(sfxCache[sfxPath]);
            return;
        }

        // ��Resources����
        AudioClip clip = Resources.Load<AudioClip>(sfxPath);
        if (clip != null)
        {
            sfxCache[sfxPath] = clip; // ��������
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

    // ============ ��ȡ״̬ ============

    public bool IsMusicEnabled() => isMusicEnabled;
    public bool IsSFXEnabled() => isSFXEnabled;

    // ============ ���ݳ־û� ============

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
    /// �����Ч����(��ѡ,�����ڴ����)
    /// </summary>
    public void ClearSFXCache()
    {
        sfxCache.Clear();
        Debug.Log("[AudioManager] SFX cache cleared");
    }
}