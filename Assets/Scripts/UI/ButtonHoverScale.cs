// Assets/Scripts/UI/ButtonHoverScale.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// 按钮悬浮缩放动效组件
/// 鼠标悬浮时放大，移开后恢复原样
/// 可挂载到任何带有 Button 组件的 UI 元素上
/// </summary>
public class ButtonHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("缩放设置")]
    [Tooltip("悬浮时的缩放倍数")]
    [SerializeField] private float hoverScale = 1.1f;

    [Tooltip("缩放动画持续时间（秒）")]
    [SerializeField] private float animationDuration = 0.15f;

    [Tooltip("缩放动画曲线")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("可选设置")]
    [Tooltip("是否在按钮禁用时仍显示动效")]
    [SerializeField] private bool animateWhenDisabled = false;

    [Tooltip("是否播放悬浮音效")]
    [SerializeField] private bool playHoverSound = false;

    [Tooltip("悬浮音效路径（如果启用音效）")]
    [SerializeField] private string hoverSoundPath = "SFX/UI/hover";

    // 内部状态
    private Vector3 originalScale;
    private Coroutine scaleCoroutine;
    private Button button;
    private bool isHovering;

    private void Awake()
    {
        // 记录原始缩放
        originalScale = transform.localScale;

        // 获取 Button 组件（可选，用于检查是否可交互）
        button = GetComponent<Button>();
    }

    private void OnDisable()
    {
        // 禁用时立即恢复原始缩放
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
            scaleCoroutine = null;
        }
        transform.localScale = originalScale;
        isHovering = false;
    }

    /// <summary>
    /// 鼠标进入
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 检查是否应该响应
        if (!ShouldAnimate()) return;

        isHovering = true;

        // 播放音效
        if (playHoverSound && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(hoverSoundPath);
        }

        // 开始放大动画
        StartScaleAnimation(originalScale * hoverScale);
    }

    /// <summary>
    /// 鼠标离开
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        // 恢复原始缩放
        StartScaleAnimation(originalScale);
    }

    /// <summary>
    /// 检查是否应该播放动画
    /// </summary>
    private bool ShouldAnimate()
    {
        // 如果设置了"禁用时也播放动画"，则始终返回 true
        if (animateWhenDisabled) return true;

        // 否则检查按钮是否可交互
        if (button != null && !button.interactable) return false;

        return true;
    }

    /// <summary>
    /// 开始缩放动画
    /// </summary>
    private void StartScaleAnimation(Vector3 targetScale)
    {
        if (scaleCoroutine != null)
        {
            StopCoroutine(scaleCoroutine);
        }
        scaleCoroutine = StartCoroutine(ScaleAnimationCoroutine(targetScale));
    }

    /// <summary>
    /// 缩放动画协程
    /// </summary>
    private IEnumerator ScaleAnimationCoroutine(Vector3 targetScale)
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 使用 unscaledDeltaTime 以支持暂停时的 UI
            float t = scaleCurve.Evaluate(elapsed / animationDuration);
            transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        // 确保最终值准确
        transform.localScale = targetScale;
        scaleCoroutine = null;
    }

    // ============ 公共方法（可选） ============

    /// <summary>
    /// 手动触发放大（用于代码调用）
    /// </summary>
    public void ScaleUp()
    {
        StartScaleAnimation(originalScale * hoverScale);
    }

    /// <summary>
    /// 手动恢复原始大小
    /// </summary>
    public void ScaleDown()
    {
        StartScaleAnimation(originalScale);
    }

    /// <summary>
    /// 设置悬浮缩放比例（运行时调整）
    /// </summary>
    public void SetHoverScale(float scale)
    {
        hoverScale = scale;

        // 如果正在悬浮，立即应用新的缩放
        if (isHovering)
        {
            StartScaleAnimation(originalScale * hoverScale);
        }
    }

    /// <summary>
    /// 重置原始缩放（当手动修改了 transform.localScale 后调用）
    /// </summary>
    public void ResetOriginalScale()
    {
        originalScale = transform.localScale;
    }
}