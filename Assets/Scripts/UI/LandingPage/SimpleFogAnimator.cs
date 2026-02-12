// Assets/Scripts/UI/LandingPage/SimpleFogAnimator.cs
// 简化版雾气动画组件 - 适用于单个UI Image的雾气效果
// 无需复杂设置，拖放即用
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class SimpleFogAnimator : MonoBehaviour
{
    public enum FogType
    {
        PathEndFog,      // 路尽头的雾（垂直脉动）
        TitleFog,        // 标题萦绕雾（水平漂移）
        AmbientFog       // 环境雾（全方向漂浮）
    }

    [Header("=== 雾气类型 ===")]
    [SerializeField] private FogType fogType = FogType.TitleFog;

    [Header("=== 移动设置 ===")]
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float moveAmplitudeX = 30f;
    [SerializeField] private float moveAmplitudeY = 15f;

    [Header("=== 缩放呼吸 ===")]
    [SerializeField] private bool enableBreathing = true;
    [SerializeField] private float breathingSpeed = 0.5f;
    [SerializeField] private float breathingAmplitude = 0.05f;

    [Header("=== 透明度脉动 ===")]
    [SerializeField] private bool enableAlphaPulse = true;
    [SerializeField] private float alphaPulseSpeed = 0.3f;
    [SerializeField][Range(0f, 1f)] private float minAlpha = 0.4f;
    [SerializeField][Range(0f, 1f)] private float maxAlpha = 0.8f;

    [Header("=== 旋转漂移 ===")]
    [SerializeField] private bool enableRotation = false;
    [SerializeField] private float rotationSpeed = 0.2f;
    [SerializeField] private float rotationAmplitude = 5f;

    [Header("=== 渐入设置 ===")]
    [SerializeField] private bool fadeInOnEnable = true;
    [SerializeField] private float fadeInDuration = 2f;

    // 私有变量
    private RectTransform rectTransform;
    private Image image;
    private CanvasGroup canvasGroup;

    private Vector2 originalPosition;
    private Vector3 originalScale;
    private Quaternion originalRotation;
    private float originalAlpha;

    private float timeOffset;
    private float currentAlphaMultiplier = 1f;
    private bool isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        canvasGroup = GetComponent<CanvasGroup>();

        // 保存原始状态
        originalPosition = rectTransform.anchoredPosition;
        originalScale = rectTransform.localScale;
        originalRotation = rectTransform.localRotation;

        if (image != null)
        {
            originalAlpha = image.color.a;
        }
        else if (canvasGroup != null)
        {
            originalAlpha = canvasGroup.alpha;
        }

        // 随机相位偏移，使多个雾气不同步
        timeOffset = Random.Range(0f, Mathf.PI * 2f);

        // 根据类型应用预设
        ApplyTypePreset();

        isInitialized = true;
    }

    private void OnEnable()
    {
        Initialize();

        if (fadeInOnEnable)
        {
            currentAlphaMultiplier = 0f;
            StartCoroutine(FadeInCoroutine());
        }
    }

    private System.Collections.IEnumerator FadeInCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            currentAlphaMultiplier = Mathf.Clamp01(elapsed / fadeInDuration);
            yield return null;
        }

        currentAlphaMultiplier = 1f;
    }

    private void Update()
    {
        float time = Time.time + timeOffset;

        // 位置动画
        UpdatePosition(time);

        // 缩放呼吸
        if (enableBreathing)
        {
            UpdateBreathing(time);
        }

        // 透明度脉动
        if (enableAlphaPulse)
        {
            UpdateAlpha(time);
        }

        // 旋转漂移
        if (enableRotation)
        {
            UpdateRotation(time);
        }
    }

    private void UpdatePosition(float time)
    {
        float offsetX = 0f;
        float offsetY = 0f;

        switch (fogType)
        {
            case FogType.PathEndFog:
                // 垂直为主的脉动
                offsetY = Mathf.Sin(time * moveSpeed) * moveAmplitudeY;
                offsetX = Mathf.Sin(time * moveSpeed * 0.7f) * moveAmplitudeX * 0.3f;
                break;

            case FogType.TitleFog:
                // 水平为主的漂移
                offsetX = Mathf.Sin(time * moveSpeed) * moveAmplitudeX;
                offsetY = Mathf.Sin(time * moveSpeed * 1.3f) * moveAmplitudeY * 0.5f;
                break;

            case FogType.AmbientFog:
                // 全方向漂浮（使用不同频率避免规律感）
                offsetX = Mathf.Sin(time * moveSpeed * 0.8f) * moveAmplitudeX;
                offsetY = Mathf.Sin(time * moveSpeed * 1.1f) * moveAmplitudeY;
                break;
        }

        rectTransform.anchoredPosition = originalPosition + new Vector2(offsetX, offsetY);
    }

    private void UpdateBreathing(float time)
    {
        float scale = 1f + Mathf.Sin(time * breathingSpeed) * breathingAmplitude;
        rectTransform.localScale = originalScale * scale;
    }

    private void UpdateAlpha(float time)
    {
        float normalizedValue = (Mathf.Sin(time * alphaPulseSpeed) + 1f) * 0.5f;
        float targetAlpha = Mathf.Lerp(minAlpha, maxAlpha, normalizedValue) * currentAlphaMultiplier;

        if (image != null)
        {
            Color c = image.color;
            c.a = targetAlpha;
            image.color = c;
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
        }
    }

    private void UpdateRotation(float time)
    {
        float angle = Mathf.Sin(time * rotationSpeed) * rotationAmplitude;
        rectTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// 根据雾气类型应用预设值
    /// </summary>
    private void ApplyTypePreset()
    {
        switch (fogType)
        {
            case FogType.PathEndFog:
                moveSpeed = 0.5f;
                moveAmplitudeX = 10f;
                moveAmplitudeY = 20f;
                breathingSpeed = 0.3f;
                breathingAmplitude = 0.03f;
                alphaPulseSpeed = 0.4f;
                minAlpha = 0.5f;
                maxAlpha = 0.9f;
                enableRotation = false;
                break;

            case FogType.TitleFog:
                moveSpeed = 0.8f;
                moveAmplitudeX = 40f;
                moveAmplitudeY = 10f;
                breathingSpeed = 0.4f;
                breathingAmplitude = 0.05f;
                alphaPulseSpeed = 0.5f;
                minAlpha = 0.3f;
                maxAlpha = 0.6f;
                enableRotation = true;
                rotationSpeed = 0.2f;
                rotationAmplitude = 3f;
                break;

            case FogType.AmbientFog:
                moveSpeed = 0.6f;
                moveAmplitudeX = 25f;
                moveAmplitudeY = 25f;
                breathingSpeed = 0.35f;
                breathingAmplitude = 0.04f;
                alphaPulseSpeed = 0.45f;
                minAlpha = 0.2f;
                maxAlpha = 0.5f;
                enableRotation = true;
                rotationSpeed = 0.15f;
                rotationAmplitude = 2f;
                break;
        }
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 设置雾气可见性
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 渐显雾气
    /// </summary>
    public void FadeIn(float duration)
    {
        fadeInDuration = duration;
        currentAlphaMultiplier = 0f;
        gameObject.SetActive(true);
        StartCoroutine(FadeInCoroutine());
    }

    /// <summary>
    /// 渐隐雾气
    /// </summary>
    public void FadeOut(float duration)
    {
        StartCoroutine(FadeOutCoroutine(duration));
    }

    private System.Collections.IEnumerator FadeOutCoroutine(float duration)
    {
        float elapsed = 0f;
        float startAlpha = currentAlphaMultiplier;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            currentAlphaMultiplier = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        currentAlphaMultiplier = 0f;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 重置到原始状态
    /// </summary>
    public void ResetToOriginal()
    {
        rectTransform.anchoredPosition = originalPosition;
        rectTransform.localScale = originalScale;
        rectTransform.localRotation = originalRotation;

        if (image != null)
        {
            Color c = image.color;
            c.a = originalAlpha;
            image.color = c;
        }
        else if (canvasGroup != null)
        {
            canvasGroup.alpha = originalAlpha;
        }
    }

    // ============ 编辑器辅助 ============

    [ContextMenu("Apply Path End Fog Preset")]
    private void ApplyPathEndFogPreset()
    {
        fogType = FogType.PathEndFog;
        ApplyTypePreset();
    }

    [ContextMenu("Apply Title Fog Preset")]
    private void ApplyTitleFogPreset()
    {
        fogType = FogType.TitleFog;
        ApplyTypePreset();
    }

    [ContextMenu("Apply Ambient Fog Preset")]
    private void ApplyAmbientFogPreset()
    {
        fogType = FogType.AmbientFog;
        ApplyTypePreset();
    }
}