// Assets/Scripts/GamePlay/Cutscene/LetterCompleteCutsceneController.cs
// 信纸完成过场动画控制器
// 信纸补全后的完整演出流程：渐黑→背景+飘落信纸→点击→信纸消失→黑影出现→动画→点击→渐黑→切换到LV2
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 信纸完成过场动画控制器
/// 管理LV1结束到LV2开始之间的完整过场演出
/// </summary>
public class LetterCompleteCutsceneController : MonoBehaviour
{
    public static LetterCompleteCutsceneController Instance { get; private set; }

    // ============ 阶段枚举 ============
    public enum CutscenePhase
    {
        Idle,               // 等待触发
        FadeToBlack1,       // 第一次渐黑
        ShowBackgroundAndLetter, // 显示背景+飘落信纸
        WaitingForClick1,   // 等待第一次点击
        LetterFadeOut,      // 信纸消失
        ShadowAppear,       // 黑影出现（放大缩小）
        ShadowAnimation,    // 黑影扭曲动画
        WaitingForClick2,   // 等待第二次点击
        FadeToBlack2,       // 第二次渐黑
        LoadLevel2,         // 加载LV2
        Completed           // 完成
    }

    [Header("当前状态")]
    [SerializeField] private CutscenePhase currentPhase = CutscenePhase.Idle;

    // ============ UI引用 ============
    [Header("UI组件引用")]
    [Tooltip("渐黑遮罩面板（需要一个全屏黑色Image）")]
    public CanvasGroup fadePanel;

    [Tooltip("过场画面容器（包含背景和信纸）")]
    public GameObject cutsceneContainer;

    [Tooltip("背景图片")]
    public UnityEngine.UI.Image backgroundImage;

    [Tooltip("飘落的信纸图片")]
    public UnityEngine.UI.Image letterImage;

    [Tooltip("黑影图片")]
    public UnityEngine.UI.Image shadowImage;

    [Tooltip("点击提示文字（可选）")]
    public TMPro.TextMeshProUGUI clickHintText;

    // ============ 时间配置 ============
    [Header("时间配置")]
    [Tooltip("第一次渐黑时间")]
    public float fadeToBlackDuration1 = 2f;

    [Tooltip("信纸飘落时间")]
    public float letterFloatDuration = 3f;

    [Tooltip("信纸消失时间")]
    public float letterFadeOutDuration = 1.5f;

    [Tooltip("黑影出现动画时间")]
    public float shadowAppearDuration = 0.8f;

    [Tooltip("黑影扭曲动画时间")]
    public float shadowAnimationDuration = 3f;

    [Tooltip("第二次渐黑时间")]
    public float fadeToBlackDuration2 = 2f;

    // ============ 信纸飘落配置 ============
    [Header("信纸飘落效果")]
    [Tooltip("信纸起始位置（屏幕上方）")]
    public Vector2 letterStartPosition = new Vector2(0, 500);

    [Tooltip("信纸最终位置（屏幕中央偏下）")]
    public Vector2 letterEndPosition = new Vector2(0, -100);

    [Tooltip("飘落时左右摆动幅度")]
    public float swayAmplitude = 50f;

    [Tooltip("摆动频率")]
    public float swayFrequency = 2f;

    [Tooltip("飘落时的旋转角度范围")]
    public float rotationRange = 15f;

    [Tooltip("飘落缓动曲线")]
    public AnimationCurve floatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ============ 黑影效果配置 ============
    [Header("黑影出现效果")]
    [Tooltip("黑影初始缩放")]
    public float shadowStartScale = 3f;

    [Tooltip("黑影最终缩放")]
    public float shadowEndScale = 1f;

    [Tooltip("黑影出现缓动曲线")]
    public AnimationCurve shadowAppearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("黑影扭曲效果")]
    [Tooltip("扭曲强度")]
    [Range(0f, 0.1f)]
    public float distortionStrength = 0.02f;

    [Tooltip("扭曲速度")]
    public float distortionSpeed = 2f;

    [Tooltip("波纹数量")]
    public int waveCount = 3;

    // ============ 音效 ============
    [Header("音效")]
    public string fadeSound = "cutscene_fade";
    public string letterFloatSound = "paper_flutter";
    public string letterFadeSound = "paper_fade";
    public string shadowAppearSound = "shadow_appear";
    public string shadowPulseSound = "shadow_pulse";
    public string clickSound = "ui_click";

    // ============ 事件 ============
    [Header("事件")]
    public UnityEvent OnCutsceneStart;
    public UnityEvent OnCutsceneEnd;
    public UnityEvent OnPhaseChanged;

    // ============ 内部变量 ============
    private bool isPlaying = false;
    private bool waitingForClick = false;
    private Material shadowMaterial;
    private Material originalShadowMaterial;
    private RectTransform letterRect;
    private RectTransform shadowRect;
    private Coroutine currentCoroutine;

    // ============ Unity生命周期 ============

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Initialize();

        // 订阅信纸完成事件
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.OnLetterCompleted.AddListener(OnLetterCompleted);
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.OnLetterCompleted.RemoveListener(OnLetterCompleted);
        }

        // 清理材质
        if (shadowMaterial != null && shadowMaterial != originalShadowMaterial)
        {
            Destroy(shadowMaterial);
        }
    }

    private void Update()
    {
        // 检测点击
        if (waitingForClick && Input.GetMouseButtonDown(0))
        {
            OnPlayerClick();
        }
    }

    // ============ 初始化 ============

    private void Initialize()
    {
        // 获取RectTransform
        if (letterImage != null)
        {
            letterRect = letterImage.GetComponent<RectTransform>();
        }

        if (shadowImage != null)
        {
            shadowRect = shadowImage.GetComponent<RectTransform>();
            // 保存原始材质引用
            originalShadowMaterial = shadowImage.material;
        }

        // 初始隐藏所有元素
        HideAllElements();

        Debug.Log("[LetterCutscene] 初始化完成");
    }

    private void HideAllElements()
    {
        if (fadePanel != null)
        {
            fadePanel.alpha = 0f;
            fadePanel.gameObject.SetActive(false);
        }

        if (cutsceneContainer != null)
        {
            cutsceneContainer.SetActive(false);
        }

        if (letterImage != null)
        {
            letterImage.gameObject.SetActive(false);
        }

        if (shadowImage != null)
        {
            shadowImage.gameObject.SetActive(false);
        }

        if (clickHintText != null)
        {
            clickHintText.gameObject.SetActive(false);
        }
    }

    // ============ 触发过场 ============

    /// <summary>
    /// 信纸完成时触发（由 LetterManager 调用）
    /// </summary>
    private void OnLetterCompleted()
    {
        Debug.Log("[LetterCutscene] 信纸已完成，启动过场动画");
        StartCutscene();
    }

    /// <summary>
    /// 手动启动过场（也可以外部调用）
    /// </summary>
    [ContextMenu("Debug: 启动过场")]
    public void StartCutscene()
    {
        if (isPlaying)
        {
            Debug.LogWarning("[LetterCutscene] 过场已在播放中");
            return;
        }

        isPlaying = true;
        currentCoroutine = StartCoroutine(PlayCutsceneSequence());
    }

    // ============ 主过场序列 ============

    private IEnumerator PlayCutsceneSequence()
    {
        Debug.Log("[LetterCutscene] ========== 过场开始 ==========");
        OnCutsceneStart?.Invoke();

        // 隐藏游戏UI
        HideGameUI();

        // ===== 阶段1: 第一次渐黑 =====
        SetPhase(CutscenePhase.FadeToBlack1);
        yield return StartCoroutine(FadeToBlack(fadeToBlackDuration1));

        // ===== 阶段2: 显示背景和飘落信纸 =====
        SetPhase(CutscenePhase.ShowBackgroundAndLetter);
        yield return StartCoroutine(ShowBackgroundAndFloatingLetter());

        // ===== 阶段3: 等待第一次点击 =====
        SetPhase(CutscenePhase.WaitingForClick1);
        ShowClickHint("点击继续");
        waitingForClick = true;
        yield return new WaitUntil(() => !waitingForClick);
        HideClickHint();
        PlaySound(clickSound);

        // ===== 阶段4: 信纸消失 =====
        SetPhase(CutscenePhase.LetterFadeOut);
        yield return StartCoroutine(FadeOutLetter());

        // ===== 阶段5: 黑影出现（放大缩小效果） =====
        SetPhase(CutscenePhase.ShadowAppear);
        yield return StartCoroutine(ShadowAppearAnimation());

        // ===== 阶段6: 黑影扭曲动画 =====
        SetPhase(CutscenePhase.ShadowAnimation);
        yield return StartCoroutine(ShadowDistortionAnimation());

        // ===== 阶段7: 等待第二次点击 =====
        SetPhase(CutscenePhase.WaitingForClick2);
        ShowClickHint("点击继续");
        waitingForClick = true;
        yield return new WaitUntil(() => !waitingForClick);
        HideClickHint();
        PlaySound(clickSound);

        // ===== 阶段8: 第二次渐黑 =====
        SetPhase(CutscenePhase.FadeToBlack2);
        yield return StartCoroutine(FadeToBlackFinal(fadeToBlackDuration2));

        // ===== 阶段9: 加载LV2 =====
        SetPhase(CutscenePhase.LoadLevel2);
        LoadLevel2();

        // ===== 完成 =====
        SetPhase(CutscenePhase.Completed);
        isPlaying = false;
        OnCutsceneEnd?.Invoke();
        Debug.Log("[LetterCutscene] ========== 过场结束 ==========");
    }

    // ============ 各阶段动画实现 ============

    /// <summary>
    /// 第一次渐黑（从透明到全黑）
    /// </summary>
    private IEnumerator FadeToBlack(float duration)
    {
        Debug.Log("[LetterCutscene] 开始渐黑转场");
        PlaySound(fadeSound);

        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // 使用平滑的缓动
            t = Mathf.SmoothStep(0f, 1f, t);
            fadePanel.alpha = t;
            yield return null;
        }

        fadePanel.alpha = 1f;
        Debug.Log("[LetterCutscene] 渐黑完成");
    }

    /// <summary>
    /// 显示背景并让信纸飘落
    /// </summary>
    private IEnumerator ShowBackgroundAndFloatingLetter()
    {
        Debug.Log("[LetterCutscene] 显示背景和飘落信纸");
        PlaySound(letterFloatSound);

        // 显示背景
        if (cutsceneContainer != null)
        {
            cutsceneContainer.SetActive(true);
        }

        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            // 背景淡入
            yield return StartCoroutine(FadeInImage(backgroundImage, 0.5f));
        }

        // 渐黑面板淡出，显示背景
        if (fadePanel != null)
        {
            float elapsed = 0f;
            float fadeDuration = 1f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                fadePanel.alpha = 1f - t;
                yield return null;
            }
            fadePanel.alpha = 0f;
        }

        // 稍等一下再开始信纸飘落
        yield return new WaitForSeconds(0.5f);

        // 显示信纸并开始飘落
        if (letterImage != null && letterRect != null)
        {
            letterImage.gameObject.SetActive(true);
            letterRect.anchoredPosition = letterStartPosition;

            // 信纸初始透明
            Color letterColor = letterImage.color;
            letterColor.a = 0f;
            letterImage.color = letterColor;

            // 淡入信纸
            yield return StartCoroutine(FadeInImage(letterImage, 0.5f));

            // 飘落动画
            yield return StartCoroutine(FloatLetterDown());
        }

        Debug.Log("[LetterCutscene] 信纸飘落完成");
    }

    /// <summary>
    /// 信纸飘落动画（带左右摆动和轻微旋转）
    /// </summary>
    private IEnumerator FloatLetterDown()
    {
        if (letterRect == null) yield break;

        Vector2 startPos = letterStartPosition;
        Vector2 endPos = letterEndPosition;

        float elapsed = 0f;
        while (elapsed < letterFloatDuration)
        {
            elapsed += Time.deltaTime;
            float t = floatCurve.Evaluate(elapsed / letterFloatDuration);

            // 垂直位置（向下飘落）
            float y = Mathf.Lerp(startPos.y, endPos.y, t);

            // 水平位置（左右摆动）
            float swayOffset = Mathf.Sin(elapsed * swayFrequency * Mathf.PI * 2f) * swayAmplitude;
            // 摆动幅度随时间递减（越靠近底部摆动越小）
            swayOffset *= (1f - t * 0.7f);
            float x = startPos.x + swayOffset;

            letterRect.anchoredPosition = new Vector2(x, y);

            // 旋转（跟随摆动方向）
            float rotation = Mathf.Sin(elapsed * swayFrequency * Mathf.PI * 2f) * rotationRange;
            rotation *= (1f - t * 0.8f); // 旋转也递减
            letterRect.localRotation = Quaternion.Euler(0, 0, rotation);

            yield return null;
        }

        // 最终位置
        letterRect.anchoredPosition = endPos;
        letterRect.localRotation = Quaternion.identity;
    }

    /// <summary>
    /// 信纸消失
    /// </summary>
    private IEnumerator FadeOutLetter()
    {
        Debug.Log("[LetterCutscene] 信纸开始消失");
        PlaySound(letterFadeSound);

        if (letterImage == null) yield break;

        // 淡出信纸
        yield return StartCoroutine(FadeOutImage(letterImage, letterFadeOutDuration));

        letterImage.gameObject.SetActive(false);
        Debug.Log("[LetterCutscene] 信纸消失完成");
    }

    /// <summary>
    /// 黑影出现动画（突然出现+先放大再缩小）
    /// </summary>
    private IEnumerator ShadowAppearAnimation()
    {
        Debug.Log("[LetterCutscene] 黑影开始出现");
        PlaySound(shadowAppearSound);

        if (shadowImage == null || shadowRect == null) yield break;

        // 显示黑影
        shadowImage.gameObject.SetActive(true);

        // 设置初始状态（透明+放大）
        Color shadowColor = shadowImage.color;
        shadowColor.a = 0f;
        shadowImage.color = shadowColor;
        shadowRect.localScale = Vector3.one * shadowStartScale;

        // 第一阶段：快速淡入+放大（冲击效果）
        float phase1Duration = shadowAppearDuration * 0.3f;
        float elapsed = 0f;

        while (elapsed < phase1Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / phase1Duration;

            // 快速淡入
            shadowColor.a = t;
            shadowImage.color = shadowColor;

            // 继续放大一点点，制造冲击感
            float scale = shadowStartScale + (shadowStartScale * 0.2f) * t;
            shadowRect.localScale = Vector3.one * scale;

            yield return null;
        }

        // 第二阶段：缩小到正常大小
        float phase2Duration = shadowAppearDuration * 0.7f;
        elapsed = 0f;
        float startScale = shadowStartScale * 1.2f;

        while (elapsed < phase2Duration)
        {
            elapsed += Time.deltaTime;
            float t = shadowAppearCurve.Evaluate(elapsed / phase2Duration);

            // 缩小
            float scale = Mathf.Lerp(startScale, shadowEndScale, t);
            shadowRect.localScale = Vector3.one * scale;

            yield return null;
        }

        // 最终状态
        shadowColor.a = 1f;
        shadowImage.color = shadowColor;
        shadowRect.localScale = Vector3.one * shadowEndScale;

        Debug.Log("[LetterCutscene] 黑影出现完成");
    }

    /// <summary>
    /// 黑影扭曲动画（液化效果）
    /// </summary>
    private IEnumerator ShadowDistortionAnimation()
    {
        Debug.Log("[LetterCutscene] 黑影扭曲动画开始");
        PlaySound(shadowPulseSound);

        if (shadowImage == null) yield break;

        // 创建材质实例（用于Shader效果）
        shadowMaterial = new Material(Shader.Find("Custom/ShadowDistortion"));
        if (shadowMaterial == null)
        {
            // 如果没有自定义Shader，使用默认的UI Shader并通过缩放模拟
            Debug.LogWarning("[LetterCutscene] 未找到 Custom/ShadowDistortion Shader，使用缩放动画替代");
            yield return StartCoroutine(FallbackShadowAnimation());
            yield break;
        }

        // 应用材质
        shadowImage.material = shadowMaterial;
        shadowMaterial.SetTexture("_MainTex", shadowImage.mainTexture);

        float elapsed = 0f;
        while (elapsed < shadowAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shadowAnimationDuration;

            // 更新Shader参数
            float time = elapsed * distortionSpeed;
            shadowMaterial.SetFloat("_Time", time);
            shadowMaterial.SetFloat("_DistortionStrength", distortionStrength * (1f + Mathf.Sin(time * 2f) * 0.5f));
            shadowMaterial.SetFloat("_WaveCount", waveCount);

            yield return null;
        }

        // 还原材质
        shadowImage.material = originalShadowMaterial;
        if (shadowMaterial != null)
        {
            Destroy(shadowMaterial);
            shadowMaterial = null;
        }

        Debug.Log("[LetterCutscene] 黑影扭曲动画完成");
    }

    /// <summary>
    /// 备用黑影动画（当Shader不可用时使用缩放+颜色动画）
    /// </summary>
    private IEnumerator FallbackShadowAnimation()
    {
        if (shadowRect == null) yield break;

        float elapsed = 0f;
        Vector3 baseScale = shadowRect.localScale;
        Color baseColor = shadowImage.color;

        while (elapsed < shadowAnimationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / shadowAnimationDuration;

            // 呼吸式缩放
            float breathe = 1f + Mathf.Sin(elapsed * 3f) * 0.1f;
            // 脉动效果
            float pulse = 1f + Mathf.Sin(elapsed * 8f) * 0.03f;

            shadowRect.localScale = baseScale * breathe * pulse;

            // 轻微的颜色明暗变化
            float brightness = 1f + Mathf.Sin(elapsed * 5f) * 0.15f;
            shadowImage.color = new Color(
                baseColor.r * brightness,
                baseColor.g * brightness,
                baseColor.b * brightness,
                baseColor.a
            );

            // 轻微抖动
            float shakeX = Mathf.PerlinNoise(elapsed * 10f, 0f) * 5f - 2.5f;
            float shakeY = Mathf.PerlinNoise(0f, elapsed * 10f) * 5f - 2.5f;
            shadowRect.anchoredPosition = new Vector2(shakeX, shakeY);

            yield return null;
        }

        // 恢复
        shadowRect.localScale = baseScale;
        shadowImage.color = baseColor;
        shadowRect.anchoredPosition = Vector2.zero;
    }

    /// <summary>
    /// 最终渐黑（完全黑屏后加载LV2）
    /// </summary>
    private IEnumerator FadeToBlackFinal(float duration)
    {
        Debug.Log("[LetterCutscene] 最终渐黑");
        PlaySound(fadeSound);

        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            fadePanel.alpha = t;
            yield return null;
        }

        fadePanel.alpha = 1f;
        Debug.Log("[LetterCutscene] 最终渐黑完成");
    }

    // ============ 辅助方法 ============

    private void SetPhase(CutscenePhase phase)
    {
        currentPhase = phase;
        Debug.Log($"[LetterCutscene] 阶段切换: {phase}");
        OnPhaseChanged?.Invoke();
    }

    private void OnPlayerClick()
    {
        if (!waitingForClick) return;
        waitingForClick = false;
        Debug.Log("[LetterCutscene] 玩家点击");
    }

    private IEnumerator FadeInImage(UnityEngine.UI.Image image, float duration)
    {
        if (image == null) yield break;

        Color color = image.color;
        color.a = 0f;
        image.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            color.a = elapsed / duration;
            image.color = color;
            yield return null;
        }

        color.a = 1f;
        image.color = color;
    }

    private IEnumerator FadeOutImage(UnityEngine.UI.Image image, float duration)
    {
        if (image == null) yield break;

        Color color = image.color;
        float startAlpha = color.a;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // 使用缓动使消失更自然
            t = Mathf.SmoothStep(0f, 1f, t);
            color.a = Mathf.Lerp(startAlpha, 0f, t);
            image.color = color;
            yield return null;
        }

        color.a = 0f;
        image.color = color;
    }

    private void ShowClickHint(string text)
    {
        if (clickHintText == null) return;

        clickHintText.text = text;
        clickHintText.gameObject.SetActive(true);

        // 可以添加闪烁效果
        StartCoroutine(BlinkClickHint());
    }

    private void HideClickHint()
    {
        if (clickHintText == null) return;
        clickHintText.gameObject.SetActive(false);
    }

    private IEnumerator BlinkClickHint()
    {
        if (clickHintText == null) yield break;

        while (waitingForClick && clickHintText.gameObject.activeInHierarchy)
        {
            // 淡入淡出闪烁
            float t = (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f;
            Color color = clickHintText.color;
            color.a = Mathf.Lerp(0.3f, 1f, t);
            clickHintText.color = color;
            yield return null;
        }
    }

    private void HideGameUI()
    {
        // 隐藏游戏中的UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideInventoryUI();
        }

        // 隐藏导航按钮等
        Debug.Log("[LetterCutscene] 隐藏游戏UI");
    }

    private void LoadLevel2()
    {
        Debug.Log("[LetterCutscene] 加载 Level2_Room");

        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("Level2_Room");
        }
        else
        {
            // 备用方案
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level2_Room");
        }
    }

    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    // ============ 调试方法 ============

    [ContextMenu("Debug: 跳过到黑影阶段")]
    private void DebugSkipToShadow()
    {
        if (!isPlaying) return;

        StopAllCoroutines();
        StartCoroutine(DebugPlayFromShadow());
    }

    private IEnumerator DebugPlayFromShadow()
    {
        HideAllElements();
        cutsceneContainer.SetActive(true);
        backgroundImage.gameObject.SetActive(true);

        SetPhase(CutscenePhase.ShadowAppear);
        yield return StartCoroutine(ShadowAppearAnimation());

        SetPhase(CutscenePhase.ShadowAnimation);
        yield return StartCoroutine(ShadowDistortionAnimation());

        SetPhase(CutscenePhase.WaitingForClick2);
        ShowClickHint("点击继续");
        waitingForClick = true;
    }

    [ContextMenu("Debug: 重置过场")]
    private void DebugReset()
    {
        StopAllCoroutines();
        isPlaying = false;
        waitingForClick = false;
        currentPhase = CutscenePhase.Idle;
        HideAllElements();
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 获取当前阶段
    /// </summary>
    public CutscenePhase GetCurrentPhase() => currentPhase;

    /// <summary>
    /// 是否正在播放
    /// </summary>
    public bool IsPlaying() => isPlaying;
}