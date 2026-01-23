using UnityEngine;
using System.Collections;

/// <summary>
/// 水汽浮现效果控制器
/// 功能：显示图片 + 水汽粒子效果
/// </summary>
public class SteamRevealController : MonoBehaviour
{
    [Header("图片设置")]
    [Tooltip("要浮现的图片（子物体，初始隐藏）")]
    public SpriteRenderer revealImage;

    [Tooltip("淡入持续时间")]
    public float fadeDuration = 1.5f;

    [Header("水汽效果")]
    [Tooltip("水汽粒子系统（可选）")]
    public ParticleSystem steamParticles;

    [Tooltip("水汽持续时间")]
    public float steamDuration = 2f;

    [Header("音效")]
    [Tooltip("水汽音效名称")]
    public string steamSoundName = "steam_reveal";

    // 状态
    private bool hasRevealed = false;

    private void Start()
    {
        // 确保图片初始隐藏
        if (revealImage != null)
        {
            Color c = revealImage.color;
            c.a = 0f;
            revealImage.color = c;
            revealImage.gameObject.SetActive(true);
        }

        // 确保粒子系统初始停止
        if (steamParticles != null)
        {
            steamParticles.Stop();
        }
    }

    /// <summary>
    /// 触发水汽浮现效果（由 UnityEvent 调用）
    /// </summary>
    public void TriggerReveal()
    {
        if (hasRevealed) return;
        hasRevealed = true;

        Debug.Log("[SteamRevealController] 触发水汽浮现效果");

        // 播放水汽粒子
        if (steamParticles != null)
        {
            steamParticles.Play();
        }

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(steamSoundName))
        {
            AudioManager.Instance.PlaySFX(steamSoundName);
        }

        // 开始图片淡入
        StartCoroutine(FadeInImage());
    }

    private IEnumerator FadeInImage()
    {
        if (revealImage == null) yield break;

        // 等待一小段时间让水汽先出现
        yield return new WaitForSeconds(0.3f);

        float elapsed = 0f;
        Color color = revealImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            revealImage.color = color;
            yield return null;
        }

        // 确保完全显示
        color.a = 1f;
        revealImage.color = color;

        // 停止粒子（可选，让它自然消散）
        yield return new WaitForSeconds(steamDuration);
        if (steamParticles != null)
        {
            steamParticles.Stop();
        }

        Debug.Log("[SteamRevealController] 浮现效果完成");

        // 保存进度
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ 存档相关 ============

    public bool HasRevealed => hasRevealed;

    public void RestoreState(bool revealed)
    {
        hasRevealed = revealed;
        if (revealed && revealImage != null)
        {
            Color c = revealImage.color;
            c.a = 1f;
            revealImage.color = c;
        }
    }
}