using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 淡入效果组件 - 当GameObject被激活时自动播放淡入动画
/// 支持UI元素(通过CanvasGroup)和2D精灵(通过SpriteRenderer)
/// </summary>
public class FadeInOnEnable : MonoBehaviour
{
    [Header("淡入设置")]
    [Tooltip("淡入持续时间(秒)")]
    [SerializeField] private float fadeDuration = 0.5f;

    [Tooltip("淡入延迟时间(秒)")]
    [SerializeField] private float fadeDelay = 0f;

    [Tooltip("淡入曲线(可选，默认线性)")]
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("可选：位移动画")]
    [Tooltip("是否同时播放位移动画")]
    [SerializeField] private bool useSlideIn = false;

    [Tooltip("起始偏移位置")]
    [SerializeField] private Vector2 slideOffset = new Vector2(0, -30f);

    // 组件引用
    private CanvasGroup canvasGroup;
    private SpriteRenderer spriteRenderer;
    private RectTransform rectTransform;

    // 原始位置
    private Vector3 originalPosition;
    private bool hasOriginalPosition = false;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        // 尝试获取组件
        canvasGroup = GetComponent<CanvasGroup>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rectTransform = GetComponent<RectTransform>();

        // 如果是UI元素但没有CanvasGroup，自动添加一个
        if (canvasGroup == null && rectTransform != null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 记录原始位置
        if (rectTransform != null)
        {
            originalPosition = rectTransform.anchoredPosition;
        }
        else
        {
            originalPosition = transform.localPosition;
        }
        hasOriginalPosition = true;
    }

    private void OnEnable()
    {
        // 每次激活时播放淡入效果
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    private void OnDisable()
    {
        // 停止协程并重置状态
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        // 重置透明度为0，为下次激活做准备
        SetAlpha(0f);

        // 重置位置
        if (useSlideIn && hasOriginalPosition)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
            else
            {
                transform.localPosition = originalPosition;
            }
        }
    }

    private IEnumerator FadeInCoroutine()
    {
        // 初始化：完全透明
        SetAlpha(0f);

        // 设置起始位置（如果使用位移动画）
        Vector3 startPosition = originalPosition;
        if (useSlideIn)
        {
            if (rectTransform != null)
            {
                startPosition = originalPosition + (Vector3)slideOffset;
                rectTransform.anchoredPosition = startPosition;
            }
            else
            {
                startPosition = originalPosition + (Vector3)slideOffset;
                transform.localPosition = startPosition;
            }
        }

        // 等待延迟
        if (fadeDelay > 0)
        {
            yield return new WaitForSeconds(fadeDelay);
        }

        // 执行淡入动画
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / fadeDuration);
            float curveValue = fadeCurve.Evaluate(progress);

            // 更新透明度
            SetAlpha(curveValue);

            // 更新位置（如果使用位移动画）
            if (useSlideIn)
            {
                Vector3 currentPos = Vector3.Lerp(startPosition, originalPosition, curveValue);
                if (rectTransform != null)
                {
                    rectTransform.anchoredPosition = currentPos;
                }
                else
                {
                    transform.localPosition = currentPos;
                }
            }

            yield return null;
        }

        // 确保最终状态
        SetAlpha(1f);
        if (useSlideIn)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
            else
            {
                transform.localPosition = originalPosition;
            }
        }

        fadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
        else if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = alpha;
            spriteRenderer.color = color;
        }
    }

    /// <summary>
    /// 手动触发淡入效果（如果需要从其他脚本调用）
    /// </summary>
    public void PlayFadeIn()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeInCoroutine());
    }

    /// <summary>
    /// 立即显示（跳过动画）
    /// </summary>
    public void ShowImmediately()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
        SetAlpha(1f);

        if (useSlideIn && hasOriginalPosition)
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }
            else
            {
                transform.localPosition = originalPosition;
            }
        }
    }
}