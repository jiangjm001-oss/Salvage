// Assets/Scripts/Managers/SceneTransitionManager.cs
// 场景转场管理器 - 全屏黑幕渐隐渐显效果
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

/// <summary>
/// 场景转场管理器
/// 提供全屏黑幕渐隐渐显的转场效果
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("=== 转场设置 ===")]
    [SerializeField] private float fadeOutDuration = 0.5f;  // 渐隐时间（当前场景变黑）
    [SerializeField] private float fadeInDuration = 0.5f;   // 渐显时间（新场景显现）
    [SerializeField] private Color fadeColor = Color.black; // 转场颜色
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("=== 转场前延迟（等待按钮动画）===")]
    [SerializeField] private float preTransitionDelay = 0f; // 开始转场前的延迟

    // 内部组件
    private Canvas transitionCanvas;
    private CanvasGroup fadeCanvasGroup;
    private Image fadeImage;

    // 状态
    private bool isTransitioning = false;
    public bool IsTransitioning => isTransitioning;

    // 事件
    public event Action OnFadeOutComplete;  // 渐隐完成（场景切换前）
    public event Action OnFadeInComplete;   // 渐显完成（场景切换后）
    public event Action<string> OnSceneTransitionStart; // 转场开始
    public event Action<string> OnSceneTransitionEnd;   // 转场结束

    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            CreateTransitionUI();
            Debug.Log("[SceneTransitionManager] Instance initialized.");
        }
        else
        {
            Debug.LogWarning("[SceneTransitionManager] Duplicate detected, destroying.");
            Destroy(this);
        }
    }

    /// <summary>
    /// 创建转场UI（动态生成，无需手动配置）
    /// </summary>
    private void CreateTransitionUI()
    {
        // 创建专用 Canvas
        GameObject canvasObj = new GameObject("TransitionCanvas");
        canvasObj.transform.SetParent(transform);

        transitionCanvas = canvasObj.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 9999; // 确保在最上层

        // 添加 CanvasScaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 添加 GraphicRaycaster（阻挡点击）
        canvasObj.AddComponent<GraphicRaycaster>();

        // 创建遮罩 Image
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        fadeImage = imageObj.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = true; // 转场时阻挡所有点击

        // 设置全屏
        RectTransform rect = imageObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 添加 CanvasGroup 用于控制透明度
        fadeCanvasGroup = imageObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false; // 初始不阻挡
        fadeCanvasGroup.interactable = false;

        Debug.Log("[SceneTransitionManager] Transition UI created.");
    }

    // ============ 公共API ============

    /// <summary>
    /// 带转场效果加载场景（主要方法）
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    /// <param name="onBeforeSceneLoad">场景加载前的回调（可选）</param>
    public void LoadSceneWithTransition(string sceneName, Action onBeforeSceneLoad = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Already transitioning, ignoring request.");
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneName, onBeforeSceneLoad));
    }

    /// <summary>
    /// 带转场效果加载场景（带预延迟，用于等待按钮动画）
    /// </summary>
    /// <param name="sceneName">目标场景名</param>
    /// <param name="delayBeforeFade">开始渐隐前的延迟时间</param>
    /// <param name="onBeforeSceneLoad">场景加载前的回调</param>
    public void LoadSceneWithTransition(string sceneName, float delayBeforeFade, Action onBeforeSceneLoad = null)
    {
        if (isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] Already transitioning, ignoring request.");
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneName, onBeforeSceneLoad, delayBeforeFade));
    }

    /// <summary>
    /// 仅播放渐隐效果（不切换场景）
    /// </summary>
    public void FadeOut(Action onComplete = null)
    {
        StartCoroutine(FadeOutCoroutine(onComplete));
    }

    /// <summary>
    /// 仅播放渐显效果（不切换场景）
    /// </summary>
    public void FadeIn(Action onComplete = null)
    {
        StartCoroutine(FadeInCoroutine(onComplete));
    }

    /// <summary>
    /// 立即设置为全黑（用于场景初始化）
    /// </summary>
    public void SetFullBlack()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// 立即设置为全透明
    /// </summary>
    public void SetFullClear()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    // ============ 核心协程 ============

    /// <summary>
    /// 完整转场流程
    /// </summary>
    private IEnumerator TransitionCoroutine(string sceneName, Action onBeforeSceneLoad, float delayBeforeFade = -1f)
    {
        isTransitioning = true;
        OnSceneTransitionStart?.Invoke(sceneName);
        Debug.Log($"[SceneTransitionManager] Starting transition to: {sceneName}");

        // 使用参数或默认值
        float actualDelay = delayBeforeFade >= 0 ? delayBeforeFade : preTransitionDelay;

        // 1. 预延迟（等待按钮动画等）
        if (actualDelay > 0)
        {
            Debug.Log($"[SceneTransitionManager] Waiting {actualDelay}s before fade...");
            yield return new WaitForSeconds(actualDelay);
        }

        // 2. 渐隐（当前场景变黑）
        yield return StartCoroutine(FadeOutCoroutine(null));
        OnFadeOutComplete?.Invoke();

        // 3. 执行场景加载前回调
        onBeforeSceneLoad?.Invoke();

        // 4. 清理旧场景管理器引用
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterSceneManagers();
        }

        // 5. 加载新场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        Debug.Log($"[SceneTransitionManager] Scene loaded: {sceneName}");

        // 6. 等待一帧让新场景初始化
        yield return null;

        // 7. 通知 GameManager 更新状态
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateGameStateBasedOnScene(sceneName);
        }

        // 8. 控制背包UI显示
        if (sceneName == "Level1_Room" || sceneName == "Level2_Room")
        {
            UIManager.Instance?.ShowInventoryUI();
        }
        else
        {
            UIManager.Instance?.HideInventoryUI();
        }

        // 9. 渐显（新场景显现）
        yield return StartCoroutine(FadeInCoroutine(null));
        OnFadeInComplete?.Invoke();

        isTransitioning = false;
        OnSceneTransitionEnd?.Invoke(sceneName);
        Debug.Log($"[SceneTransitionManager] Transition complete: {sceneName}");
    }

    /// <summary>
    /// 渐隐协程
    /// </summary>
    private IEnumerator FadeOutCoroutine(Action onComplete)
    {
        fadeCanvasGroup.blocksRaycasts = true; // 开始阻挡点击

        float elapsed = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float curvedT = fadeCurve.Evaluate(t);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, curvedT);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 渐显协程
    /// </summary>
    private IEnumerator FadeInCoroutine(Action onComplete)
    {
        float elapsed = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float curvedT = fadeCurve.Evaluate(t);
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, curvedT);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false; // 结束阻挡点击
        onComplete?.Invoke();
    }

    // ============ 设置方法 ============

    /// <summary>
    /// 设置渐隐时间
    /// </summary>
    public void SetFadeOutDuration(float duration)
    {
        fadeOutDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// 设置渐显时间
    /// </summary>
    public void SetFadeInDuration(float duration)
    {
        fadeInDuration = Mathf.Max(0.1f, duration);
    }

    /// <summary>
    /// 设置转场颜色
    /// </summary>
    public void SetFadeColor(Color color)
    {
        fadeColor = color;
        if (fadeImage != null)
        {
            fadeImage.color = new Color(color.r, color.g, color.b, fadeImage.color.a);
        }
    }
}