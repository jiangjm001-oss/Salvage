// Assets/Scripts/UI/TextPromptManager.cs
// 提示文字管理器 - 全局单例
// 功能：在屏幕上方显示提示文字，支持多行文字点击切换，3秒自动消失
// 动效：淡入淡出、打字机效果、弹出动画
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 提示文字管理器
/// 在屏幕上方显示提示文字，支持多行文字点击切换
/// 
/// 使用方式：
/// 1. 将此脚本挂载到场景中的 UI Canvas 下的 TextPromptPanel 物体上
/// 2. 在 Inspector 中配置 UI 引用
/// 3. 调用 TextPromptManager.Instance.ShowPrompt(messages) 显示文字
/// 
/// 或者使用 TextPromptTrigger 组件自动触发
/// </summary>
public class TextPromptManager : MonoBehaviour
{
    // ============ 单例 ============
    public static TextPromptManager Instance { get; private set; }

    // ============ UI 引用 ============
    [Header("UI 引用")]
    [Tooltip("文字显示面板（整个提示框）")]
    [SerializeField] private GameObject promptPanel;

    [Tooltip("文字显示组件（TextMeshProUGUI）")]
    [SerializeField] private TextMeshProUGUI promptText;

    [Tooltip("背景图片（用于动画）")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("继续提示图标（如小箭头，多行时显示）")]
    [SerializeField] private GameObject continueIndicator;

    [Tooltip("页码显示（可选，如 1/3）")]
    [SerializeField] private TextMeshProUGUI pageIndicatorText;

    // ============ 显示设置 ============
    [Header("显示设置")]
    [Tooltip("自动消失时间（秒）")]
    [SerializeField] private float autoHideDelay = 3f;

    [Tooltip("是否启用自动消失")]
    [SerializeField] private bool enableAutoHide = true;

    [Tooltip("多行文字时，是否在切换到最后一句后自动消失")]
    [SerializeField] private bool autoHideOnLastMessage = true;

    // ============ 打字机效果 ============
    [Header("打字机效果")]
    [Tooltip("是否启用打字机效果")]
    [SerializeField] private bool enableTypewriter = true;

    [Tooltip("每个字符的显示间隔（秒）")]
    [SerializeField] private float typewriterInterval = 0.03f;

    [Tooltip("打字机音效（每个字符）")]
    [SerializeField] private string typewriterSFX = "";

    [Tooltip("打字机音效播放间隔（每N个字符播放一次）")]
    [SerializeField] private int sfxPlayInterval = 2;

    // ============ 动画设置 ============
    [Header("动画设置")]
    [Tooltip("淡入时间")]
    [SerializeField] private float fadeInDuration = 0.25f;

    [Tooltip("淡出时间")]
    [SerializeField] private float fadeOutDuration = 0.2f;

    [Tooltip("面板弹出动画")]
    [SerializeField] private bool enablePopAnimation = true;

    [Tooltip("弹出动画起始缩放")]
    [SerializeField] private float popStartScale = 0.8f;

    [Tooltip("弹出动画曲线")]
    [SerializeField] private AnimationCurve popCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("文字切换时的淡出淡入时间")]
    [SerializeField] private float textTransitionDuration = 0.15f;

    // ============ 视觉样式 ============
    [Header("视觉样式")]
    [Tooltip("普通文字颜色")]
    [SerializeField] private Color normalTextColor = Color.white;

    [Tooltip("强调文字颜色（用于特殊标记）")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.9f, 0.5f);

    [Tooltip("背景颜色")]
    [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.75f);

    // ============ 音效 ============
    [Header("音效")]
    [Tooltip("显示时的音效")]
    [SerializeField] private string showSFX = "";

    [Tooltip("翻页/切换时的音效")]
    [SerializeField] private string nextPageSFX = "";

    [Tooltip("关闭时的音效")]
    [SerializeField] private string hideSFX = "";

    // ============ 事件 ============
    [Header("事件")]
    [Tooltip("文字开始显示时触发")]
    public UnityEvent OnPromptShow;

    [Tooltip("文字隐藏时触发")]
    public UnityEvent OnPromptHide;

    [Tooltip("翻页时触发")]
    public UnityEvent OnPageChanged;

    [Tooltip("所有文字显示完毕时触发")]
    public UnityEvent OnAllMessagesComplete;

    // ============ 内部状态 ============
    private List<string> currentMessages = new List<string>();
    private int currentIndex = 0;
    private bool isShowing = false;
    private bool isAnimating = false;
    private bool isTypewriting = false;
    private Coroutine autoHideCoroutine;
    private Coroutine typewriterCoroutine;
    private Coroutine animationCoroutine;

    // 缓存
    private CanvasGroup panelCanvasGroup;
    private RectTransform panelRectTransform;
    private Vector3 originalScale;
    private string fullCurrentText = "";

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        // 单例设置
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[TextPromptManager] 检测到重复实例，销毁: {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 初始化组件
        InitializeComponents();
    }

    private void Start()
    {
        // 确保初始状态为隐藏
        HideImmediate();
    }

    private void Update()
    {
        // 检测点击（切换到下一条消息或关闭）
        if (isShowing && Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ============ 初始化 ============

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void InitializeComponents()
    {
        // 获取或创建 CanvasGroup
        if (promptPanel != null)
        {
            panelCanvasGroup = promptPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = promptPanel.AddComponent<CanvasGroup>();
            }

            panelRectTransform = promptPanel.GetComponent<RectTransform>();
            if (panelRectTransform != null)
            {
                originalScale = panelRectTransform.localScale;
            }
        }

        // 设置初始颜色
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor;
        }

        if (promptText != null)
        {
            promptText.color = normalTextColor;
        }

        Debug.Log("[TextPromptManager] 初始化完成");
    }

    // ============ 公共接口 ============

    /// <summary>
    /// 显示单条提示文字
    /// </summary>
    /// <param name="message">要显示的文字</param>
    public void ShowPrompt(string message)
    {
        ShowPrompt(new string[] { message });
    }

    /// <summary>
    /// 显示多条提示文字（点击切换）
    /// </summary>
    /// <param name="messages">文字数组</param>
    public void ShowPrompt(string[] messages)
    {
        if (messages == null || messages.Length == 0) return;

        // 停止之前的所有协程
        StopAllPromptCoroutines();

        // 存储消息
        currentMessages.Clear();
        currentMessages.AddRange(messages);
        currentIndex = 0;

        // ✅ 关键修复：先激活脚本所在的 GameObject
        // TextPromptManager 挂在 TextPromptPanel 上，必须激活才能启动协程
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // 然后再启动协程
        StartCoroutine(ShowPromptSequence());
    }
    /// <summary>
    /// 显示多条提示文字（List 版本）
    /// </summary>
    public void ShowPrompt(List<string> messages)
    {
        ShowPrompt(messages.ToArray());
    }

    /// <summary>
    /// 立即隐藏提示
    /// </summary>
    public void HidePrompt()
    {
        if (!isShowing) return;

        StopAllPromptCoroutines();
        StartCoroutine(HidePromptSequence());
    }

    /// <summary>
    /// 立即隐藏（无动画）
    /// </summary>
    public void HideImmediate()
    {
        StopAllPromptCoroutines();

        isShowing = false;
        isAnimating = false;
        isTypewriting = false;

        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
        }

        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }

        currentMessages.Clear();
        currentIndex = 0;
    }

    /// <summary>
    /// 检查是否正在显示
    /// </summary>
    public bool IsShowing => isShowing;

    /// <summary>
    /// 获取当前页码信息
    /// </summary>
    public (int current, int total) GetPageInfo()
    {
        return (currentIndex + 1, currentMessages.Count);
    }

    // ============ 点击处理 ============

    /// <summary>
    /// 处理点击事件
    /// </summary>
    private void HandleClick()
    {
        // 如果正在打字机动画中，点击立即显示完整文字
        if (isTypewriting)
        {
            CompleteTypewriter();
            return;
        }

        // 如果正在其他动画中，忽略点击
        if (isAnimating) return;

        // 切换到下一条消息
        if (currentIndex < currentMessages.Count - 1)
        {
            currentIndex++;
            StartCoroutine(SwitchToNextMessage());
            OnPageChanged?.Invoke();
        }
        else
        {
            // 已经是最后一条，关闭提示
            HidePrompt();
            OnAllMessagesComplete?.Invoke();
        }
    }

    /// <summary>
    /// 完成打字机效果，立即显示完整文字
    /// </summary>
    private void CompleteTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        isTypewriting = false;

        if (promptText != null)
        {
            promptText.text = fullCurrentText;
        }

        // 重置自动隐藏计时器
        RestartAutoHideTimer();
    }

    // ============ 显示序列 ============

    /// <summary>
    /// 显示提示的完整序列
    /// </summary>
    private IEnumerator ShowPromptSequence()
    {
        isShowing = true;
        isAnimating = true;

        // 播放显示音效
        PlaySFX(showSFX);

        // 准备面板
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
        }

        // 初始化透明度
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
        }

        // 设置初始缩放（弹出动画）
        if (enablePopAnimation && panelRectTransform != null)
        {
            panelRectTransform.localScale = originalScale * popStartScale;
        }

        // 更新页码显示
        UpdatePageIndicator();

        // 更新继续指示器
        UpdateContinueIndicator();

        // 设置文字（先清空）
        if (promptText != null)
        {
            promptText.text = "";
        }

        // 淡入 + 弹出动画
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float easedT = popCurve.Evaluate(t);

            // 透明度
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = t;
            }

            // 弹出缩放
            if (enablePopAnimation && panelRectTransform != null)
            {
                float scale = Mathf.Lerp(popStartScale, 1f, easedT);
                panelRectTransform.localScale = originalScale * scale;
            }

            yield return null;
        }

        // 确保最终状态
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 1f;
        }

        if (panelRectTransform != null)
        {
            panelRectTransform.localScale = originalScale;
        }

        isAnimating = false;

        // 触发事件
        OnPromptShow?.Invoke();

        // 开始打字机效果显示第一条消息
        yield return StartCoroutine(DisplayCurrentMessage());

        // 启动自动隐藏计时器
        if (enableAutoHide && (currentMessages.Count == 1 || autoHideOnLastMessage))
        {
            RestartAutoHideTimer();
        }
    }

    /// <summary>
    /// 隐藏提示的完整序列
    /// </summary>
    private IEnumerator HidePromptSequence()
    {
        isAnimating = true;

        // 播放隐藏音效
        PlaySFX(hideSFX);

        // 淡出动画
        float elapsed = 0f;
        float startAlpha = panelCanvasGroup != null ? panelCanvasGroup.alpha : 1f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            }

            // 缩小动画
            if (enablePopAnimation && panelRectTransform != null)
            {
                float scale = Mathf.Lerp(1f, popStartScale, t);
                panelRectTransform.localScale = originalScale * scale;
            }

            yield return null;
        }

        // 最终状态
        if (panelCanvasGroup != null)
        {
            panelCanvasGroup.alpha = 0f;
        }

        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        isShowing = false;
        isAnimating = false;

        // 清理
        currentMessages.Clear();
        currentIndex = 0;

        // 触发事件
        OnPromptHide?.Invoke();
    }

    // ============ 消息显示 ============

    /// <summary>
    /// 显示当前索引的消息
    /// </summary>
    private IEnumerator DisplayCurrentMessage()
    {
        if (currentIndex >= currentMessages.Count) yield break;

        fullCurrentText = currentMessages[currentIndex];

        // 更新 UI 状态
        UpdatePageIndicator();
        UpdateContinueIndicator();

        // 打字机效果
        if (enableTypewriter)
        {
            yield return StartCoroutine(TypewriterEffect(fullCurrentText));
        }
        else
        {
            if (promptText != null)
            {
                promptText.text = fullCurrentText;
            }
        }
    }

    /// <summary>
    /// 切换到下一条消息
    /// </summary>
    private IEnumerator SwitchToNextMessage()
    {
        isAnimating = true;

        // 播放翻页音效
        PlaySFX(nextPageSFX);

        // 取消自动隐藏
        CancelAutoHideTimer();

        // 淡出当前文字
        yield return StartCoroutine(FadeText(1f, 0f, textTransitionDuration));

        // 显示新消息
        yield return StartCoroutine(DisplayCurrentMessage());

        // 淡入新文字
        yield return StartCoroutine(FadeText(0f, 1f, textTransitionDuration));

        isAnimating = false;

        // 如果是最后一条且启用自动隐藏
        if (enableAutoHide && autoHideOnLastMessage && currentIndex == currentMessages.Count - 1)
        {
            RestartAutoHideTimer();
        }
    }

    /// <summary>
    /// 打字机效果
    /// </summary>
    private IEnumerator TypewriterEffect(string text)
    {
        isTypewriting = true;

        if (promptText == null)
        {
            isTypewriting = false;
            yield break;
        }

        promptText.text = "";
        int charCount = 0;

        foreach (char c in text)
        {
            promptText.text += c;
            charCount++;

            // 播放打字音效
            if (!string.IsNullOrEmpty(typewriterSFX) && charCount % sfxPlayInterval == 0)
            {
                PlaySFX(typewriterSFX);
            }

            yield return new WaitForSeconds(typewriterInterval);
        }

        isTypewriting = false;
    }

    /// <summary>
    /// 文字淡入淡出
    /// </summary>
    private IEnumerator FadeText(float from, float to, float duration)
    {
        if (promptText == null) yield break;

        float elapsed = 0f;
        Color originalColor = promptText.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(from, to, t);

            promptText.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        promptText.color = new Color(originalColor.r, originalColor.g, originalColor.b, to);
    }

    // ============ UI 更新 ============

    /// <summary>
    /// 更新页码显示
    /// </summary>
    private void UpdatePageIndicator()
    {
        if (pageIndicatorText == null) return;

        if (currentMessages.Count > 1)
        {
            pageIndicatorText.gameObject.SetActive(true);
            pageIndicatorText.text = $"{currentIndex + 1}/{currentMessages.Count}";
        }
        else
        {
            pageIndicatorText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 更新继续指示器
    /// </summary>
    private void UpdateContinueIndicator()
    {
        if (continueIndicator == null) return;

        // 如果还有下一条消息，显示继续指示器
        bool hasMore = currentIndex < currentMessages.Count - 1;
        continueIndicator.SetActive(hasMore);

        // 如果显示，添加闪烁动画
        if (hasMore)
        {
            StartCoroutine(BlinkIndicator());
        }
    }

    /// <summary>
    /// 继续指示器闪烁动画
    /// </summary>
    private IEnumerator BlinkIndicator()
    {
        if (continueIndicator == null) yield break;

        Image indicatorImage = continueIndicator.GetComponent<Image>();
        if (indicatorImage == null) yield break;

        while (continueIndicator.activeSelf && isShowing)
        {
            // 淡出
            float elapsed = 0f;
            while (elapsed < 0.5f && continueIndicator.activeSelf)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0.3f, elapsed / 0.5f);
                indicatorImage.color = new Color(indicatorImage.color.r, indicatorImage.color.g, indicatorImage.color.b, alpha);
                yield return null;
            }

            // 淡入
            elapsed = 0f;
            while (elapsed < 0.5f && continueIndicator.activeSelf)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0.3f, 1f, elapsed / 0.5f);
                indicatorImage.color = new Color(indicatorImage.color.r, indicatorImage.color.g, indicatorImage.color.b, alpha);
                yield return null;
            }
        }
    }

    // ============ 自动隐藏计时器 ============

    /// <summary>
    /// 重启自动隐藏计时器
    /// </summary>
    private void RestartAutoHideTimer()
    {
        CancelAutoHideTimer();

        if (enableAutoHide)
        {
            autoHideCoroutine = StartCoroutine(AutoHideTimer());
        }
    }

    /// <summary>
    /// 取消自动隐藏计时器
    /// </summary>
    private void CancelAutoHideTimer()
    {
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }
    }

    /// <summary>
    /// 自动隐藏计时器协程
    /// </summary>
    private IEnumerator AutoHideTimer()
    {
        yield return new WaitForSeconds(autoHideDelay);

        // 单条消息或最后一条消息时自动隐藏
        if (currentMessages.Count == 1 || (autoHideOnLastMessage && currentIndex == currentMessages.Count - 1))
        {
            HidePrompt();
            OnAllMessagesComplete?.Invoke();
        }
    }

    // ============ 工具方法 ============

    /// <summary>
    /// 停止所有相关协程
    /// </summary>
    private void StopAllPromptCoroutines()
    {
        CancelAutoHideTimer();

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        isTypewriting = false;
        isAnimating = false;
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySFX(string sfxName)
    {
        if (string.IsNullOrEmpty(sfxName)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(sfxName);
        }
    }

    // ============ 编辑器辅助 ============

#if UNITY_EDITOR
    [Header("调试")]
    [SerializeField] private string[] testMessages = new string[] { "这是第一条测试消息。", "这是第二条测试消息。", "这是最后一条消息！" };

    [ContextMenu("测试显示")]
    private void TestShow()
    {
        ShowPrompt(testMessages);
    }

    [ContextMenu("测试隐藏")]
    private void TestHide()
    {
        HidePrompt();
    }
#endif
}
