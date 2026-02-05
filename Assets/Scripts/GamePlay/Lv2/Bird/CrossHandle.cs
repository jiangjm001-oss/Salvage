// Assets/Scripts/GamePlay/CrossHandle.cs
// 十字控制器的可点击手柄（横杆/竖杆）
// 支持点击检测、视觉反馈动画
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class CrossHandle : MonoBehaviour
{
    // ============ 手柄类型 ============
    public enum HandleType
    {
        Horizontal,  // 横杆
        Vertical     // 竖杆
    }

    // ============ 基本设置 ============
    [Header("基本设置")]
    [Tooltip("手柄类型")]
    public HandleType handleType = HandleType.Horizontal;

    [Tooltip("显示名称")]
    public string displayName = "横杆";

    // ============ 视觉反馈设置 ============
    [Header("视觉反馈")]
    [Tooltip("手柄的 SpriteRenderer")]
    public SpriteRenderer handleRenderer;

    [Tooltip("普通状态颜色")]
    public Color normalColor = Color.white;

    [Tooltip("悬停状态颜色")]
    public Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

    [Tooltip("点击状态颜色")]
    public Color clickColor = new Color(0.9f, 0.9f, 0.7f, 1f);

    [Tooltip("禁用状态颜色（当前状态无法点击）")]
    public Color disabledColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    // ============ 动画设置 ============
    [Header("动画设置")]
    [Tooltip("点击缩放比例")]
    public float clickScale = 0.95f;

    [Tooltip("点击动画时间")]
    public float clickAnimDuration = 0.1f;

    [Tooltip("抖动幅度（无效点击时）")]
    public float shakeAmount = 0.05f;

    [Tooltip("抖动次数")]
    public int shakeCount = 3;

    [Tooltip("抖动频率")]
    public float shakeSpeed = 0.03f;

    [Tooltip("颜色过渡时间")]
    public float colorTransitionDuration = 0.15f;

    // ============ 内部引用 ============
    private BirdCrossController controller;
    private Collider2D col;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private bool isAnimating = false;
    private bool isHovering = false;
    private Coroutine colorCoroutine;

    // ============ 生命周期 ============

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (handleRenderer == null)
        {
            handleRenderer = GetComponent<SpriteRenderer>();
        }

        originalScale = transform.localScale;
        originalPosition = transform.localPosition;
    }

    /// <summary>
    /// 初始化（由 BirdCrossController 调用）
    /// </summary>
    public void Initialize(BirdCrossController ctrl, HandleType type)
    {
        controller = ctrl;
        handleType = type;

        // 设置初始颜色
        if (handleRenderer != null)
        {
            handleRenderer.color = normalColor;
        }

        Debug.Log($"[CrossHandle] 初始化完成: {displayName} ({handleType})");
    }

    // ============ 鼠标交互 ============

    private void OnMouseEnter()
    {
        if (isAnimating) return;

        isHovering = true;

        // 检查是否可以点击
        if (IsClickable())
        {
            TransitionToColor(hoverColor);
        }
        else
        {
            TransitionToColor(disabledColor);
        }
    }

    private void OnMouseExit()
    {
        isHovering = false;

        if (!isAnimating)
        {
            TransitionToColor(normalColor);
        }
    }

    private void OnMouseDown()
    {
        // 检查是否在UI上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (isAnimating) return;

        // 点击视觉反馈
        StartCoroutine(ClickAnimation());

        // 通知控制器
        if (controller != null)
        {
            controller.OnHandleClicked(handleType);
        }
    }

    // ============ 检查是否可点击 ============

    /// <summary>
    /// 检查当前状态下此手柄是否有效
    /// </summary>
    private bool IsClickable()
    {
        if (controller == null) return true;

        switch (controller.currentState)
        {
            case BirdCrossController.BirdState.Neutral:
                // 中立态时两个手柄都可点击
                return true;

            case BirdCrossController.BirdState.Left:
                // 向左看时只有竖杆可点击（回到中立）
                return handleType == HandleType.Vertical;

            case BirdCrossController.BirdState.Right:
                // 向右看时只有横杆可点击（回到中立）
                return handleType == HandleType.Horizontal;

            default:
                return true;
        }
    }

    // ============ 动画效果 ============

    /// <summary>
    /// 点击动画（缩放 + 颜色）
    /// </summary>
    private IEnumerator ClickAnimation()
    {
        isAnimating = true;

        // 切换到点击颜色
        if (handleRenderer != null)
        {
            handleRenderer.color = clickColor;
        }

        // 缩小
        float elapsed = 0f;
        Vector3 targetScale = originalScale * clickScale;

        while (elapsed < clickAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / clickAnimDuration;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            yield return null;
        }

        // 恢复
        elapsed = 0f;
        while (elapsed < clickAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / clickAnimDuration;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
            yield return null;
        }

        transform.localScale = originalScale;

        // 恢复颜色
        if (handleRenderer != null)
        {
            handleRenderer.color = isHovering ? hoverColor : normalColor;
        }

        isAnimating = false;
    }

    /// <summary>
    /// 抖动动画（无效点击时）
    /// </summary>
    public void PlayShakeAnimation()
    {
        if (isAnimating) return;
        StartCoroutine(ShakeAnimation());
    }

    private IEnumerator ShakeAnimation()
    {
        isAnimating = true;

        // 临时变红
        Color originalColor = handleRenderer != null ? handleRenderer.color : Color.white;
        Color shakeColor = new Color(1f, 0.7f, 0.7f, 1f);

        if (handleRenderer != null)
        {
            handleRenderer.color = shakeColor;
        }

        // ⭐ 根据手柄类型决定抖动方向（使用局部坐标，考虑旋转）
        // 横杆沿X轴抖动，竖杆沿Y轴抖动
        Vector3 shakeDirection = handleType == HandleType.Horizontal
            ? transform.right  // 使用手柄自身的右方向
            : transform.up;    // 使用手柄自身的上方向

        Vector3 worldOriginalPos = transform.position;

        // 执行抖动
        for (int i = 0; i < shakeCount; i++)
        {
            // 向正方向
            transform.position = worldOriginalPos + shakeDirection * shakeAmount;
            yield return new WaitForSeconds(shakeSpeed);

            // 向负方向
            transform.position = worldOriginalPos - shakeDirection * shakeAmount;
            yield return new WaitForSeconds(shakeSpeed);
        }

        // 恢复位置
        transform.localPosition = originalPosition;

        // 恢复颜色
        if (handleRenderer != null)
        {
            handleRenderer.color = originalColor;
        }

        isAnimating = false;
    }

    /// <summary>
    /// 平滑过渡到目标颜色
    /// </summary>
    private void TransitionToColor(Color targetColor)
    {
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }
        colorCoroutine = StartCoroutine(ColorTransition(targetColor));
    }

    private IEnumerator ColorTransition(Color targetColor)
    {
        if (handleRenderer == null) yield break;

        Color startColor = handleRenderer.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / colorTransitionDuration;
            handleRenderer.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        handleRenderer.color = targetColor;
    }

    // ============ 脉冲发光效果（可选，用于提示） ============

    /// <summary>
    /// 播放脉冲发光提示（引导玩家注意）
    /// </summary>
    public void PlayPulseHint()
    {
        StartCoroutine(PulseAnimation());
    }

    private IEnumerator PulseAnimation()
    {
        if (handleRenderer == null) yield break;

        Color originalColor = handleRenderer.color;
        Color pulseColor = new Color(1f, 1f, 0.5f, 1f);
        float pulseDuration = 0.5f;
        int pulseCount = 2;

        for (int i = 0; i < pulseCount; i++)
        {
            // 亮起
            float elapsed = 0f;
            while (elapsed < pulseDuration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration / 2);
                handleRenderer.color = Color.Lerp(originalColor, pulseColor, t);
                yield return null;
            }

            // 暗下
            elapsed = 0f;
            while (elapsed < pulseDuration / 2)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration / 2);
                handleRenderer.color = Color.Lerp(pulseColor, originalColor, t);
                yield return null;
            }
        }

        handleRenderer.color = originalColor;
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 设置手柄是否可交互
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (col != null)
        {
            col.enabled = interactable;
        }

        if (handleRenderer != null)
        {
            handleRenderer.color = interactable ? normalColor : disabledColor;
        }
    }

    /// <summary>
    /// 刷新视觉状态
    /// </summary>
    public void RefreshVisualState()
    {
        if (handleRenderer != null)
        {
            handleRenderer.color = IsClickable() ? normalColor : disabledColor;
        }
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        // 自动设置名称
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = handleType == HandleType.Horizontal ? "横杆" : "竖杆";
        }

        // 在编辑器中预览颜色
        if (handleRenderer != null && !Application.isPlaying)
        {
            handleRenderer.color = normalColor;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 显示抖动范围
        Gizmos.color = Color.cyan;
        Vector3 pos = transform.position;
        Vector3 dir = handleType == HandleType.Horizontal ? Vector3.right : Vector3.up;
        Gizmos.DrawLine(pos - dir * shakeAmount, pos + dir * shakeAmount);
    }
}