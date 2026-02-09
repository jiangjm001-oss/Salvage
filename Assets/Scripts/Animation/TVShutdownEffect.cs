// Assets/Scripts/Animation/TVShutdownEffect.cs
// 电视关机效果 - 十字白光收缩消失动画
// 支持程序化生成，无需预制图片

using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 电视关机效果组件
/// 实现类似老式CRT电视关机的十字白光效果
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class TVShutdownEffect : MonoBehaviour
{
    // ============ 效果设置 ============
    [Header("效果设置")]
    [Tooltip("水平线Image")]
    public Image horizontalLine;

    [Tooltip("垂直线Image（用于十字效果）")]
    public Image verticalLine;

    [Tooltip("中心光点Image")]
    public Image centerDot;

    [Tooltip("整体闪光Image")]
    public Image flashOverlay;

    [Header("动画参数")]
    [Tooltip("总动画时长")]
    public float totalDuration = 0.6f;

    [Tooltip("屏幕收缩阶段占比")]
    [Range(0f, 1f)]
    public float shrinkPhaseRatio = 0.4f;

    [Tooltip("横线消失阶段占比")]
    [Range(0f, 1f)]
    public float linePhaseRatio = 0.35f;

    [Tooltip("光点消失阶段占比")]
    [Range(0f, 1f)]
    public float dotPhaseRatio = 0.25f;

    [Header("尺寸设置")]
    [Tooltip("横线高度")]
    public float lineHeight = 6f;

    [Tooltip("光点大小")]
    public float dotSize = 30f;

    [Tooltip("初始闪光亮度")]
    [Range(1f, 3f)]
    public float initialFlashIntensity = 1.5f;

    [Header("颜色设置")]
    [Tooltip("主体颜色")]
    public Color mainColor = Color.white;

    [Tooltip("发光颜色（用于HDR效果）")]
    public Color glowColor = new Color(0.9f, 0.95f, 1f, 1f);

    // ============ 私有变量 ============
    private bool isPlaying = false;
    private RectTransform canvasRect;
    private Vector2 screenSize;
    private Coroutine effectCoroutine;

    // ============ 生命周期 ============

    private void Awake()
    {
        // 尝试获取Canvas尺寸
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            screenSize = canvasRect.sizeDelta;

            // 如果sizeDelta为0，使用屏幕尺寸
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                screenSize = new Vector2(Screen.width, Screen.height);
            }
        }
        else
        {
            screenSize = new Vector2(Screen.width, Screen.height);
        }

        // 初始化：隐藏所有元素
        SetAllAlpha(0f);
        gameObject.SetActive(false);
    }

    // ============ 公共API ============

    /// <summary>
    /// 播放电视关机效果
    /// </summary>
    public void Play()
    {
        if (isPlaying) return;

        gameObject.SetActive(true);
        isPlaying = true;

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
        }

        effectCoroutine = StartCoroutine(PlayEffectCoroutine());
    }

    /// <summary>
    /// 立即停止效果
    /// </summary>
    public void Stop()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        isPlaying = false;
        SetAllAlpha(0f);
        gameObject.SetActive(false);
    }

    // ============ 效果协程 ============

    private IEnumerator PlayEffectCoroutine()
    {
        // 计算各阶段时长
        float shrinkDuration = totalDuration * shrinkPhaseRatio;
        float lineDuration = totalDuration * linePhaseRatio;
        float dotDuration = totalDuration * dotPhaseRatio;

        // ========== 阶段0：初始闪光 ==========
        if (flashOverlay != null)
        {
            flashOverlay.color = glowColor * initialFlashIntensity;
            SetImageAlpha(flashOverlay, 1f);
        }

        // 短暂延迟让闪光可见
        yield return new WaitForSeconds(0.02f);

        // 快速淡出闪光
        float flashFadeTime = 0.1f;
        float elapsed = 0f;

        while (elapsed < flashFadeTime && flashOverlay != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashFadeTime;
            SetImageAlpha(flashOverlay, 1f - t);
            yield return null;
        }

        if (flashOverlay != null)
        {
            SetImageAlpha(flashOverlay, 0f);
        }

        // ========== 阶段1：画面收缩成横线 ==========
        // 初始化横线
        if (horizontalLine != null)
        {
            horizontalLine.color = mainColor;
            SetImageAlpha(horizontalLine, 1f);

            RectTransform hrt = horizontalLine.rectTransform;
            hrt.sizeDelta = new Vector2(screenSize.x * 1.2f, screenSize.y * 1.2f);
        }

        elapsed = 0f;
        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shrinkDuration;

            if (horizontalLine != null)
            {
                RectTransform hrt = horizontalLine.rectTransform;

                // 高度从全屏快速收缩到横线
                // 使用EaseInQuad让开始慢结束快
                float heightT = Mathf.Pow(t, 0.6f);
                float height = Mathf.Lerp(screenSize.y * 1.2f, lineHeight, heightT);

                hrt.sizeDelta = new Vector2(screenSize.x * 1.2f, height);

                // 轻微的亮度闪烁
                float flicker = 1f + Mathf.Sin(t * Mathf.PI * 6f) * 0.1f;
                horizontalLine.color = mainColor * flicker;
            }

            yield return null;
        }

        // 确保最终状态
        if (horizontalLine != null)
        {
            horizontalLine.rectTransform.sizeDelta = new Vector2(screenSize.x * 1.2f, lineHeight);
            horizontalLine.color = mainColor;
        }

        // ========== 阶段2：横线从两端收缩到中心 ==========
        elapsed = 0f;
        float startWidth = screenSize.x * 1.2f;

        while (elapsed < lineDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lineDuration;

            // 使用EaseInOut让动画更自然
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            if (horizontalLine != null)
            {
                RectTransform hrt = horizontalLine.rectTransform;

                // 宽度收缩
                float width = Mathf.Lerp(startWidth, 0f, smoothT);
                hrt.sizeDelta = new Vector2(width, lineHeight);

                // 亮度增加（收缩时变亮）
                float brightness = 1f + smoothT * 0.5f;
                horizontalLine.color = mainColor * brightness;
            }

            yield return null;
        }

        // 隐藏横线
        if (horizontalLine != null)
        {
            SetImageAlpha(horizontalLine, 0f);
        }

        // ========== 阶段3：中心光点闪烁消失 ==========
        if (centerDot != null)
        {
            centerDot.color = glowColor * 2f; // 亮度翻倍
            SetImageAlpha(centerDot, 1f);

            RectTransform drt = centerDot.rectTransform;
            drt.sizeDelta = new Vector2(dotSize, dotSize);
        }

        // 光点闪烁并消失
        elapsed = 0f;
        int flashCount = 0;

        while (elapsed < dotDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dotDuration;

            if (centerDot != null)
            {
                // 尺寸脉动
                float pulse = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.3f;
                float scale = Mathf.Lerp(1f, 0f, Mathf.Pow(t, 2f)) * pulse;

                centerDot.rectTransform.sizeDelta = new Vector2(dotSize * scale, dotSize * scale);

                // 亮度闪烁
                float brightness = Mathf.Lerp(2f, 0f, t);
                brightness *= 1f + Mathf.Sin(t * Mathf.PI * 8f) * 0.3f;
                centerDot.color = glowColor * brightness;

                // Alpha淡出
                SetImageAlpha(centerDot, 1f - Mathf.Pow(t, 3f));
            }

            yield return null;
        }

        // 完全隐藏
        if (centerDot != null)
        {
            SetImageAlpha(centerDot, 0f);
        }

        // ========== 效果完成 ==========
        isPlaying = false;
        gameObject.SetActive(false);

        Debug.Log("[TVShutdownEffect] 电视关机效果完成");
    }

    // ============ 辅助方法 ============

    private void SetAllAlpha(float alpha)
    {
        SetImageAlpha(horizontalLine, alpha);
        SetImageAlpha(verticalLine, alpha);
        SetImageAlpha(centerDot, alpha);
        SetImageAlpha(flashOverlay, alpha);
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null) return;

        Color c = image.color;
        c.a = Mathf.Clamp01(alpha);
        image.color = c;
    }

    // ============ 编辑器功能 ============

    [ContextMenu("测试效果")]
    private void TestEffect()
    {
        if (Application.isPlaying)
        {
            Play();
        }
    }

    [ContextMenu("自动创建子元素")]
    private void AutoCreateChildren()
    {
        // 创建横线
        if (horizontalLine == null)
        {
            GameObject hLineObj = new GameObject("HorizontalLine");
            hLineObj.transform.SetParent(transform);
            hLineObj.transform.localPosition = Vector3.zero;
            hLineObj.transform.localScale = Vector3.one;

            horizontalLine = hLineObj.AddComponent<Image>();
            horizontalLine.color = mainColor;

            RectTransform hrt = hLineObj.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.5f, 0.5f);
            hrt.anchorMax = new Vector2(0.5f, 0.5f);
            hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = new Vector2(100f, lineHeight);
        }

        // 创建光点
        if (centerDot == null)
        {
            GameObject dotObj = new GameObject("CenterDot");
            dotObj.transform.SetParent(transform);
            dotObj.transform.localPosition = Vector3.zero;
            dotObj.transform.localScale = Vector3.one;

            centerDot = dotObj.AddComponent<Image>();
            centerDot.color = glowColor;

            RectTransform drt = dotObj.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0.5f, 0.5f);
            drt.anchorMax = new Vector2(0.5f, 0.5f);
            drt.pivot = new Vector2(0.5f, 0.5f);
            drt.sizeDelta = new Vector2(dotSize, dotSize);
        }

        // 创建闪光层
        if (flashOverlay == null)
        {
            GameObject flashObj = new GameObject("FlashOverlay");
            flashObj.transform.SetParent(transform);
            flashObj.transform.localPosition = Vector3.zero;
            flashObj.transform.localScale = Vector3.one;
            flashObj.transform.SetAsFirstSibling(); // 放到最底层

            flashOverlay = flashObj.AddComponent<Image>();
            flashOverlay.color = glowColor;

            RectTransform frt = flashObj.GetComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = Vector2.zero;
            frt.offsetMax = Vector2.zero;
        }

        Debug.Log("[TVShutdownEffect] 子元素创建完成");
    }
}
