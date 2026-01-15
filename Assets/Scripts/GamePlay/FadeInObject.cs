// Assets/Scripts/GamePlay/FadeInObject.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 物体淡入效果组件
/// 让物体以淡入动画的方式显示出来
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FadeInObject : MonoBehaviour
{
    [Header("淡入设置")]
    [Tooltip("淡入持续时间（秒）")]
    public float fadeDuration = 1f;

    [Tooltip("淡入延迟（秒）")]
    public float fadeDelay = 0f;

    [Tooltip("淡入曲线")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("淡入完成后启用碰撞体（允许点击）")]
    public bool enableColliderAfterFade = true;

    [Header("音效（可选）")]
    [Tooltip("开始淡入时播放的音效")]
    public string fadeInSound = "";

    [Header("调试")]
    [Tooltip("启动时自动播放淡入（用于测试）")]
    public bool playOnStart = false;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Color originalColor;
    private bool hasFadedIn = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // 记录原始颜色
        originalColor = spriteRenderer.color;

        // 初始状态：完全透明，禁用碰撞
        SetAlpha(0f);
        if (col != null && enableColliderAfterFade)
        {
            col.enabled = false;
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            FadeIn();
        }
    }

    /// <summary>
    /// 开始淡入效果（供外部调用，如UnityEvent）
    /// </summary>
    public void FadeIn()
    {
        if (hasFadedIn) return;

        Debug.Log($"[FadeInObject] '{gameObject.name}' 开始淡入");
        StartCoroutine(FadeInCoroutine());
    }

    /// <summary>
    /// 显示物体并开始淡入（先激活物体再淡入）
    /// </summary>
    public void ShowAndFadeIn()
    {
        gameObject.SetActive(true);
        FadeIn();
    }

    /// <summary>
    /// 淡入协程
    /// </summary>
    private IEnumerator FadeInCoroutine()
    {
        // 延迟
        if (fadeDelay > 0)
        {
            yield return new WaitForSeconds(fadeDelay);
        }

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(fadeInSound))
        {
            AudioManager.Instance.PlaySFX(fadeInSound);
        }

        // 淡入动画
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);
            SetAlpha(t * originalColor.a);
            yield return null;
        }

        // 确保最终状态
        SetAlpha(originalColor.a);

        // 启用碰撞体
        if (col != null && enableColliderAfterFade)
        {
            col.enabled = true;
        }

        hasFadedIn = true;
        Debug.Log($"[FadeInObject] '{gameObject.name}' 淡入完成，可以点击了");
    }

    /// <summary>
    /// 设置透明度
    /// </summary>
    private void SetAlpha(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
        }
    }

    /// <summary>
    /// 重置状态（用于调试或重玩）
    /// </summary>
    public void ResetFade()
    {
        StopAllCoroutines();
        hasFadedIn = false;
        SetAlpha(0f);
        if (col != null && enableColliderAfterFade)
        {
            col.enabled = false;
        }
    }
}