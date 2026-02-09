// Assets/Scripts/UI/ScreenFadeOverlay.cs
// 黑屏转场UI组件 - 可复用的全屏遮罩淡入淡出效果
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 屏幕淡入淡出遮罩
/// 用于转场、Jump Scare等效果
/// 支持动态创建或预制体使用
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeOverlay : MonoBehaviour
{
    public static ScreenFadeOverlay Instance { get; private set; }

    [Header("配置")]
    [Tooltip("默认淡入时间")]
    public float defaultFadeInDuration = 0.3f;

    [Tooltip("默认淡出时间")]
    public float defaultFadeOutDuration = 0.3f;

    [Tooltip("遮罩颜色")]
    public Color overlayColor = Color.black;

    private CanvasGroup canvasGroup;
    private Image overlayImage;
    private Coroutine currentFadeCoroutine;

    private void Awake()
    {
        // 单例模式（可选）
        if (Instance == null)
        {
            Instance = this;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        overlayImage = GetComponent<Image>();

        if (overlayImage != null)
        {
            overlayImage.color = overlayColor;
        }

        // 初始透明
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 淡入到黑屏
    /// </summary>
    public void FadeIn(float duration = -1f, System.Action onComplete = null)
    {
        if (duration < 0) duration = defaultFadeInDuration;

        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeCoroutine(1f, duration, onComplete));
    }

    /// <summary>
    /// 从黑屏淡出
    /// </summary>
    public void FadeOut(float duration = -1f, System.Action onComplete = null)
    {
        if (duration < 0) duration = defaultFadeOutDuration;

        if (currentFadeCoroutine != null)
        {
            StopCoroutine(currentFadeCoroutine);
        }

        currentFadeCoroutine = StartCoroutine(FadeCoroutine(0f, duration, onComplete));
    }

    /// <summary>
    /// 立即设置为黑屏
    /// </summary>
    public void SetBlack()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// 立即设置为透明
    /// </summary>
    public void SetClear()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// 执行完整的淡入-停留-淡出转场
    /// </summary>
    public void DoTransition(float fadeInDuration, float holdDuration, float fadeOutDuration,
        System.Action onBlackScreen = null, System.Action onComplete = null)
    {
        StartCoroutine(TransitionCoroutine(fadeInDuration, holdDuration, fadeOutDuration, onBlackScreen, onComplete));
    }

    private IEnumerator FadeCoroutine(float targetAlpha, float duration, System.Action onComplete)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        // 开始阻挡点击（如果是淡入）
        if (targetAlpha > 0.5f)
        {
            canvasGroup.blocksRaycasts = true;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        // 结束阻挡点击（如果是淡出）
        if (targetAlpha < 0.5f)
        {
            canvasGroup.blocksRaycasts = false;
        }

        currentFadeCoroutine = null;
        onComplete?.Invoke();
    }

    private IEnumerator TransitionCoroutine(float fadeInDuration, float holdDuration, float fadeOutDuration,
        System.Action onBlackScreen, System.Action onComplete)
    {
        // 淡入
        yield return FadeCoroutine(1f, fadeInDuration, null);

        // 黑屏时回调
        onBlackScreen?.Invoke();

        // 停留
        yield return new WaitForSeconds(holdDuration);

        // 淡出
        yield return FadeCoroutine(0f, fadeOutDuration, null);

        // 完成回调
        onComplete?.Invoke();
    }

    /// <summary>
    /// 动态创建一个全屏遮罩
    /// </summary>
    public static ScreenFadeOverlay CreateOverlay(Canvas parentCanvas = null)
    {
        // 查找或创建Canvas
        if (parentCanvas == null)
        {
            parentCanvas = FindObjectOfType<Canvas>();

            if (parentCanvas == null)
            {
                GameObject canvasObj = new GameObject("FadeOverlayCanvas");
                parentCanvas = canvasObj.AddComponent<Canvas>();
                parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                parentCanvas.sortingOrder = 9999; // 最上层
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }
        }

        // 创建遮罩GameObject
        GameObject overlayObj = new GameObject("ScreenFadeOverlay");
        overlayObj.transform.SetParent(parentCanvas.transform, false);

        // 设置RectTransform为全屏
        RectTransform rect = overlayObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 添加Image组件
        Image img = overlayObj.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;

        // 添加CanvasGroup
        CanvasGroup cg = overlayObj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        // 添加本组件
        ScreenFadeOverlay overlay = overlayObj.AddComponent<ScreenFadeOverlay>();

        Debug.Log("[ScreenFadeOverlay] 动态创建了全屏遮罩");

        return overlay;
    }
}