// Assets/Scripts/Audio/SFXPlayer.cs
// 通用音效播放器 - 可在 Inspector 中配置音效，通过 Unity Event 触发
// 适用于按钮点击、动画事件、碰撞触发等场景

using UnityEngine;

public class SFXPlayer : MonoBehaviour
{
    [Header("音效配置")]
    [Tooltip("直接拖入音效文件")]
    public AudioClip[] soundEffects;

    [Header("播放设置")]
    [Tooltip("随机播放（从数组中随机选择）")]
    public bool randomPlay = false;

    [Tooltip("音量")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("音调变化范围（用于增加变化感）")]
    [Range(0f, 0.3f)]
    public float pitchVariation = 0f;

    private int lastPlayedIndex = -1;

    /// <summary>
    /// 播放音效（可被 Unity Event 调用）
    /// </summary>
    public void PlaySound()
    {
        if (soundEffects == null || soundEffects.Length == 0)
        {
            Debug.LogWarning($"[SFXPlayer] {gameObject.name}: No sound effects configured!");
            return;
        }

        AudioClip clipToPlay = null;

        if (randomPlay && soundEffects.Length > 1)
        {
            // 随机选择（避免连续播放相同音效）
            int index;
            do
            {
                index = Random.Range(0, soundEffects.Length);
            } while (index == lastPlayedIndex && soundEffects.Length > 1);

            lastPlayedIndex = index;
            clipToPlay = soundEffects[index];
        }
        else
        {
            clipToPlay = soundEffects[0];
        }

        if (clipToPlay != null)
        {
            PlayClip(clipToPlay);
        }
    }

    /// <summary>
    /// 播放指定索引的音效
    /// </summary>
    public void PlaySoundAtIndex(int index)
    {
        if (index >= 0 && index < soundEffects.Length && soundEffects[index] != null)
        {
            PlayClip(soundEffects[index]);
        }
    }

    private void PlayClip(AudioClip clip)
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SFXPlayer] AudioManager not found!");
            return;
        }

        // 如果有音调变化，使用临时 AudioSource
        if (pitchVariation > 0)
        {
            PlayWithPitchVariation(clip);
        }
        else
        {
            AudioManager.Instance.PlaySFX(clip);
        }
    }

    private void PlayWithPitchVariation(AudioClip clip)
    {
        // 创建临时 AudioSource
        GameObject tempGO = new GameObject("TempAudio");
        AudioSource tempSource = tempGO.AddComponent<AudioSource>();

        tempSource.clip = clip;
        tempSource.volume = volume;
        tempSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        tempSource.Play();

        // 播放完成后销毁
        Destroy(tempGO, clip.length + 0.1f);
    }
}