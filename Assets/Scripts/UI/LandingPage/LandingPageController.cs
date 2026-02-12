// Assets/Scripts/UI/LandingPage/LandingPageController.cs
// Landing Page 主控制器 - 协调所有动画序列
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LandingPageController : MonoBehaviour
{
    [Header("=== 背景设置 ===")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private CanvasGroup backgroundCanvasGroup;
    [SerializeField] private float backgroundFadeDuration = 2.0f;

    [Header("=== 标题设置 ===")]
    [SerializeField] private CanvasGroup titleCanvasGroup;
    [SerializeField] private float titleFadeDuration = 1.5f;
    [SerializeField] private float titleFadeDelay = 0.5f; // 背景完成后延迟

    [Header("=== 雾气效果设置 ===")]
    [SerializeField] private FogEffectController fogEffectController;
    [SerializeField] private float fogStartDelay = 0.3f; // 背景完成后延迟启动雾气

    [Header("=== 菜单按钮设置 ===")]
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private float menuSlideDistance = 300f;
    [SerializeField] private float menuSlideDuration = 0.8f;
    [SerializeField] private AnimationCurve menuSlideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("=== 设置按钮 ===")]
    [SerializeField] private CanvasGroup settingsButtonCanvasGroup;
    [SerializeField] private float settingsButtonFadeDelay = 0.5f;
    [SerializeField] private float settingsButtonFadeDuration = 0.5f;

    [Header("=== 黑色遮罩（用于初始全黑）===")]
    [SerializeField] private Image blackOverlay;
    [SerializeField] private float blackOverlayFadeDuration = 1.5f;

    [Header("=== 调试设置 ===")]
    [SerializeField] private bool skipAnimationInEditor = false;
    [SerializeField] private bool debugLog = true;

    // 内部状态
    private bool animationStarted = false;
    private bool animationCompleted = false;
    private Vector2 menuOriginalPosition;

    // 公共属性
    public bool IsAnimationCompleted => animationCompleted;

    private void Awake()
    {
        // 初始化所有元素为隐藏状态
        InitializeElements();
    }

    private void Start()
    {
        // 开始动画序列
        if (!animationStarted)
        {
            StartCoroutine(PlayLandingSequence());
        }
    }

    /// <summary>
    /// 初始化所有UI元素为初始状态
    /// </summary>
    private void InitializeElements()
    {
        // 黑色遮罩完全不透明
        if (blackOverlay != null)
        {
            blackOverlay.gameObject.SetActive(true);
            Color c = blackOverlay.color;
            c.a = 1f;
            blackOverlay.color = c;
        }

        // 背景透明
        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha = 0f;
        }
        else if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;
        }

        // 标题透明
        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.alpha = 0f;
        }

        // 菜单面板隐藏并移到下方
        if (menuPanel != null)
        {
            menuOriginalPosition = menuPanel.anchoredPosition;
            menuPanel.anchoredPosition = new Vector2(
                menuOriginalPosition.x,
                menuOriginalPosition.y - menuSlideDistance
            );
        }

        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 0f;
        }

        // 设置按钮隐藏
        if (settingsButtonCanvasGroup != null)
        {
            settingsButtonCanvasGroup.alpha = 0f;
            settingsButtonCanvasGroup.interactable = false;
            settingsButtonCanvasGroup.blocksRaycasts = false;
        }

        // 雾气效果初始关闭
        if (fogEffectController != null)
        {
            fogEffectController.StopFog();
        }

        Log("所有元素已初始化为隐藏状态");
    }

    /// <summary>
    /// 播放完整的Landing Page动画序列
    /// </summary>
    private IEnumerator PlayLandingSequence()
    {
        animationStarted = true;
        Log("=== Landing Page 动画序列开始 ===");

#if UNITY_EDITOR
        if (skipAnimationInEditor)
        {
            SkipToEnd();
            yield break;
        }
#endif

        // ========== 第一阶段：黑色遮罩淡出，背景渐显 ==========
        Log("阶段1: 背景渐显");
        yield return StartCoroutine(FadeInBackground());

        // ========== 第二阶段：启动雾气效果 + 标题渐显 ==========
        Log("阶段2: 雾气效果 + 标题渐显");
        yield return new WaitForSeconds(fogStartDelay);

        // 同时启动雾气和标题动画
        if (fogEffectController != null)
        {
            fogEffectController.StartFog();
        }

        yield return new WaitForSeconds(titleFadeDelay);
        StartCoroutine(FadeInTitle());

        // ========== 第三阶段：标题达到75%时，菜单按钮滑入 ==========
        // 计算75%的等待时间
        float waitForMenuTrigger = titleFadeDuration * 0.75f;
        yield return new WaitForSeconds(waitForMenuTrigger);

        Log("阶段3: 菜单按钮滑入");
        StartCoroutine(SlideInMenu());

        // 等待标题剩余的25%
        yield return new WaitForSeconds(titleFadeDuration * 0.25f);

        // ========== 第四阶段：设置按钮渐显 ==========
        yield return new WaitForSeconds(settingsButtonFadeDelay);
        Log("阶段4: 设置按钮渐显");
        yield return StartCoroutine(FadeInSettingsButton());

        // 动画完成
        animationCompleted = true;
        Log("=== Landing Page 动画序列完成 ===");
    }

    /// <summary>
    /// 背景渐显（黑色遮罩淡出）
    /// </summary>
    private IEnumerator FadeInBackground()
    {
        float elapsed = 0f;

        // 同时淡出黑色遮罩和淡入背景
        while (elapsed < backgroundFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / backgroundFadeDuration);

            // 使用缓动曲线使动画更自然
            float easedT = EaseOutQuad(t);

            // 黑色遮罩淡出
            if (blackOverlay != null)
            {
                Color c = blackOverlay.color;
                c.a = 1f - easedT;
                blackOverlay.color = c;
            }

            // 背景淡入
            if (backgroundCanvasGroup != null)
            {
                backgroundCanvasGroup.alpha = easedT;
            }
            else if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                c.a = easedT;
                backgroundImage.color = c;
            }

            yield return null;
        }

        // 确保最终状态
        if (blackOverlay != null)
        {
            blackOverlay.gameObject.SetActive(false);
        }

        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha = 1f;
        }

        Log("背景渐显完成");
    }

    /// <summary>
    /// 标题渐显
    /// </summary>
    private IEnumerator FadeInTitle()
    {
        if (titleCanvasGroup == null)
        {
            Log("警告: titleCanvasGroup 未设置");
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < titleFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / titleFadeDuration);
            float easedT = EaseOutQuad(t);

            titleCanvasGroup.alpha = easedT;

            yield return null;
        }

        titleCanvasGroup.alpha = 1f;
        Log("标题渐显完成");
    }

    /// <summary>
    /// 菜单按钮从下方滑入
    /// </summary>
    private IEnumerator SlideInMenu()
    {
        if (menuPanel == null)
        {
            Log("警告: menuPanel 未设置");
            yield break;
        }

        float elapsed = 0f;
        Vector2 startPos = menuPanel.anchoredPosition;

        // 同时显示透明度
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 0f;
        }

        while (elapsed < menuSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / menuSlideDuration);
            float easedT = menuSlideCurve.Evaluate(t);

            // 位置滑动
            menuPanel.anchoredPosition = Vector2.Lerp(startPos, menuOriginalPosition, easedT);

            // 透明度渐显
            if (menuCanvasGroup != null)
            {
                menuCanvasGroup.alpha = easedT;
            }

            yield return null;
        }

        // 确保最终状态
        menuPanel.anchoredPosition = menuOriginalPosition;
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
        }

        Log("菜单滑入完成");
    }

    /// <summary>
    /// 设置按钮渐显
    /// </summary>
    private IEnumerator FadeInSettingsButton()
    {
        if (settingsButtonCanvasGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < settingsButtonFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settingsButtonFadeDuration);
            settingsButtonCanvasGroup.alpha = EaseOutQuad(t);
            yield return null;
        }

        settingsButtonCanvasGroup.alpha = 1f;
        settingsButtonCanvasGroup.interactable = true;
        settingsButtonCanvasGroup.blocksRaycasts = true;

        Log("设置按钮渐显完成");
    }

    /// <summary>
    /// 跳过动画直接显示最终状态
    /// </summary>
    public void SkipToEnd()
    {
        StopAllCoroutines();

        // 隐藏黑色遮罩
        if (blackOverlay != null)
        {
            blackOverlay.gameObject.SetActive(false);
        }

        // 显示背景
        if (backgroundCanvasGroup != null)
        {
            backgroundCanvasGroup.alpha = 1f;
        }
        else if (backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = 1f;
            backgroundImage.color = c;
        }

        // 显示标题
        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.alpha = 1f;
        }

        // 显示菜单
        if (menuPanel != null)
        {
            menuPanel.anchoredPosition = menuOriginalPosition;
        }
        if (menuCanvasGroup != null)
        {
            menuCanvasGroup.alpha = 1f;
        }

        // 显示设置按钮
        if (settingsButtonCanvasGroup != null)
        {
            settingsButtonCanvasGroup.alpha = 1f;
            settingsButtonCanvasGroup.interactable = true;
            settingsButtonCanvasGroup.blocksRaycasts = true;
        }

        // 启动雾气
        if (fogEffectController != null)
        {
            fogEffectController.StartFog();
        }

        animationCompleted = true;
        Log("动画已跳过，直接显示最终状态");
    }

    /// <summary>
    /// 缓动函数 - EaseOutQuad
    /// </summary>
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    private void Log(string message)
    {
        if (debugLog)
        {
            Debug.Log($"[LandingPageController] {message}");
        }
    }

    // ============ 公共方法供按钮调用 ============

    /// <summary>
    /// 点击任意位置跳过动画
    /// </summary>
    private void Update()
    {
        // 动画进行中时，点击可跳过
        if (animationStarted && !animationCompleted)
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                SkipToEnd();
            }
        }
    }
}