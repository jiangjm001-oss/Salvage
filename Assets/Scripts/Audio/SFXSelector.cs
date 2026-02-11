// Assets/Scripts/Audio/SFXSelector.cs
// 音效选择器 - 为谜题脚本提供便捷的音效选择方式

using UnityEngine;

/// <summary>
/// 音效选择器组件
/// 可以直接拖拽AudioClip，或者输入音效名称
/// 用于不想修改现有代码的情况
/// </summary>
[System.Serializable]
public class SFXSelector
{
    [Tooltip("方式1: 直接拖拽AudioClip（优先使用）")]
    public AudioClip directClip;

    [Tooltip("方式2: 输入音效名称（从SFXLibrary查找）\n例如: Burn, LampOpen, 倒水声")]
    public string sfxName;

    /// <summary>
    /// 播放此音效
    /// </summary>
    public void Play()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SFXSelector] AudioManager not found!");
            return;
        }

        // 优先使用直接引用的AudioClip
        if (directClip != null)
        {
            AudioManager.Instance.PlaySFX(directClip);
            return;
        }

        // 其次使用名称查找
        if (!string.IsNullOrEmpty(sfxName))
        {
            AudioManager.Instance.PlaySFX(sfxName);
            return;
        }

        // 两个都没配置
        Debug.LogWarning("[SFXSelector] No audio configured (neither clip nor name)");
    }

    /// <summary>
    /// 播放此音效（带音量）
    /// </summary>
    public void Play(float volumeScale)
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SFXSelector] AudioManager not found!");
            return;
        }

        if (directClip != null)
        {
            AudioManager.Instance.PlaySFX(directClip, volumeScale);
            return;
        }

        if (!string.IsNullOrEmpty(sfxName))
        {
            AudioManager.Instance.PlaySFXWithVolume(sfxName, volumeScale);
            return;
        }
    }

    /// <summary>
    /// 检查是否已配置音效
    /// </summary>
    public bool IsConfigured => directClip != null || !string.IsNullOrEmpty(sfxName);

    /// <summary>
    /// 获取配置的音效名称（用于调试）
    /// </summary>
    public string GetDisplayName()
    {
        if (directClip != null) return directClip.name;
        if (!string.IsNullOrEmpty(sfxName)) return sfxName;
        return "(未配置)";
    }
}

/// <summary>
/// 音效播放助手 - 静态工具类
/// 提供便捷的音效播放方法
/// </summary>
public static class SFXHelper
{
    /// <summary>
    /// 播放音效（通过名称）
    /// </summary>
    /// <param name="sfxName">音效名称</param>
    public static void Play(string sfxName)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfxName);
        }
    }

    /// <summary>
    /// 播放音效（通过AudioClip）
    /// </summary>
    public static void Play(AudioClip clip)
    {
        if (AudioManager.Instance != null && clip != null)
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    /// <summary>
    /// 播放音效（带音量）
    /// </summary>
    public static void Play(string sfxName, float volume)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFXWithVolume(sfxName, volume);
        }
    }
}