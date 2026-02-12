// Assets/Scripts/UI/LandingPage/MenuButtonAnimator.cs
// 菜单按钮动画控制器 - 悬停、点击效果 + 入场动画
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(Button))]
public class MenuButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("=== 组件引用 ===")]
    [SerializeField] private RectTransform buttonTransform;
    [SerializeField] private Image buttonImage;
    [SerializeField] private Text buttonText; // 如果使用 Legacy Text
    [SerializeField] private TMPro.TextMeshProUGUI buttonTextTMP; // 如果使用 TextMeshPro

    [Header("=== 悬停效果 ===")]
    [SerializeField] private bool enableHoverScale = true;
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverDuration = 0.15f;
    [SerializeField] private Color hoverTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private bool enableHoverGlow = true;
    [SerializeField] private float hoverGlowIntensity = 0.1f;

    [Header("=== 点击效果 ===")]
    [SerializeField] private float clickScale = 0.95f;
    [SerializeField] private float clickDuration = 0.08f;

    [Header("=== 入场动画（由父级控制）===")]
    [SerializeField] private float entranceDelay = 0f; // 相对于整体动画的延迟
    [SerializeField] private float entranceOffset = 50f; // 单独的入场偏移

    [Header("=== 闲置动画 ===")]
    [SerializeField] private bool enableIdleAnimation = true;
    [SerializeField] private float idleFloatSpeed = 1.5f;
    [SerializeField] private float idleFloatAmplitude = 2f;

    // 私有变量
    private Button button;
    private Vector3 originalScale;
    private Color originalTextColor;
    private Color originalImageColor;
    private Vector2 originalPosition;
    private bool isHovering = false;
    private bool isPressed = false;
    private Coroutine currentAnimation;
    private float idleTimeOffset;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (buttonTransform == null)
            buttonTransform = GetComponent<RectTransform>();

        if (buttonImage == null)
            buttonImage = GetComponent<Image>();

        // 尝试获取文本组件
        if (buttonText == null && buttonTextTMP == null)
        {
            buttonText = GetComponentInChildren<Text>();
            if (buttonText == null)
            {
                buttonTextTMP = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
        }

        // 保存原始状态
        originalScale = buttonTransform.localScale;
        originalPosition = buttonTransform.anchoredPosition;

        if (buttonText != null)
            originalTextColor = buttonText.color;
        else if (buttonTextTMP != null)
            originalTextColor = buttonTextTMP.color;

        if (buttonImage != null)
            originalImageColor = buttonImage.color;

        // 随机闲置动画相位
        idleTimeOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        // 闲置浮动动画（只在非悬停/点击状态下）
        if (enableIdleAnimation && !isHovering && !isPressed)
        {
            float yOffset = Mathf.Sin((Time.time + idleTimeOffset) * idleFloatSpeed) * idleFloatAmplitude;
            buttonTransform.anchoredPosition = originalPosition + new Vector2(0, yOffset);
        }
    }

    // ============ 指针事件 ============

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isHovering = true;
        PlayHoverEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (!isPressed)
        {
            PlayHoverExit();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isPressed = true;
        PlayClickDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;

        if (isHovering)
        {
            PlayClickUp();
        }
        else
        {
            PlayHoverExit();
        }
    }

    // ============ 动画方法 ============

    private void PlayHoverEnter()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateHoverEnter());
    }

    private void PlayHoverExit()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateHoverExit());
    }

    private void PlayClickDown()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateClickDown());
    }

    private void PlayClickUp()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        currentAnimation = StartCoroutine(AnimateClickUp());
    }

    // ============ 动画协程 ============

    private IEnumerator AnimateHoverEnter()
    {
        float elapsed = 0f;
        Vector3 startScale = buttonTransform.localScale;
        Vector3 targetScale = enableHoverScale ? originalScale * hoverScale : originalScale;
        Color startTextColor = GetCurrentTextColor();
        Color startImageColor = buttonImage != null ? buttonImage.color : Color.white;

        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / hoverDuration));

            // 缩放
            buttonTransform.localScale = Vector3.Lerp(startScale, targetScale, t);

            // 文字颜色
            SetTextColor(Color.Lerp(startTextColor, hoverTextColor, t));

            // 图片发光效果
            if (enableHoverGlow && buttonImage != null)
            {
                Color c = Color.Lerp(startImageColor, originalImageColor + new Color(hoverGlowIntensity, hoverGlowIntensity, hoverGlowIntensity, 0), t);
                buttonImage.color = c;
            }

            yield return null;
        }

        // 确保最终状态
        buttonTransform.localScale = targetScale;
        SetTextColor(hoverTextColor);
    }

    private IEnumerator AnimateHoverExit()
    {
        float elapsed = 0f;
        Vector3 startScale = buttonTransform.localScale;
        Color startTextColor = GetCurrentTextColor();
        Color startImageColor = buttonImage != null ? buttonImage.color : Color.white;

        while (elapsed < hoverDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / hoverDuration));

            buttonTransform.localScale = Vector3.Lerp(startScale, originalScale, t);
            SetTextColor(Color.Lerp(startTextColor, originalTextColor, t));

            if (buttonImage != null)
            {
                buttonImage.color = Color.Lerp(startImageColor, originalImageColor, t);
            }

            yield return null;
        }

        buttonTransform.localScale = originalScale;
        SetTextColor(originalTextColor);
        if (buttonImage != null)
            buttonImage.color = originalImageColor;
    }

    private IEnumerator AnimateClickDown()
    {
        float elapsed = 0f;
        Vector3 startScale = buttonTransform.localScale;
        Vector3 targetScale = originalScale * clickScale;

        while (elapsed < clickDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutQuad(Mathf.Clamp01(elapsed / clickDuration));
            buttonTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        buttonTransform.localScale = targetScale;
    }

    private IEnumerator AnimateClickUp()
    {
        float elapsed = 0f;
        Vector3 startScale = buttonTransform.localScale;
        Vector3 targetScale = enableHoverScale ? originalScale * hoverScale : originalScale;

        while (elapsed < clickDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / clickDuration));
            buttonTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        buttonTransform.localScale = targetScale;
    }

    // ============ 入场动画（供外部调用）============

    /// <summary>
    /// 播放入场动画
    /// </summary>
    public IEnumerator PlayEntranceAnimation(float duration)
    {
        // 初始状态：透明 + 下方偏移
        CanvasGroup cg = GetComponent<CanvasGroup>();
        if (cg == null)
            cg = gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        Vector2 startPos = originalPosition + new Vector2(0, -entranceOffset);
        buttonTransform.anchoredPosition = startPos;

        // 等待延迟
        if (entranceDelay > 0)
            yield return new WaitForSeconds(entranceDelay);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOutBack(Mathf.Clamp01(elapsed / duration));

            cg.alpha = t;
            buttonTransform.anchoredPosition = Vector2.Lerp(startPos, originalPosition, t);

            yield return null;
        }

        cg.alpha = 1f;
        buttonTransform.anchoredPosition = originalPosition;
    }

    // ============ 辅助方法 ============

    private Color GetCurrentTextColor()
    {
        if (buttonText != null)
            return buttonText.color;
        if (buttonTextTMP != null)
            return buttonTextTMP.color;
        return Color.white;
    }

    private void SetTextColor(Color color)
    {
        if (buttonText != null)
            buttonText.color = color;
        if (buttonTextTMP != null)
            buttonTextTMP.color = color;
    }

    // ============ 缓动函数 ============

    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 重置到原始状态
    /// </summary>
    public void ResetToOriginal()
    {
        if (currentAnimation != null)
            StopCoroutine(currentAnimation);

        buttonTransform.localScale = originalScale;
        buttonTransform.anchoredPosition = originalPosition;
        SetTextColor(originalTextColor);

        if (buttonImage != null)
            buttonImage.color = originalImageColor;

        isHovering = false;
        isPressed = false;
    }

    /// <summary>
    /// 设置入场延迟（用于错开动画）
    /// </summary>
    public void SetEntranceDelay(float delay)
    {
        entranceDelay = delay;
    }
}