// Assets/Scripts/GamePlay/Notebook/NotebookArrowHint.cs
// 翻页箭头提示 - 显示可翻页方向的箭头动画
using UnityEngine;
using System.Collections;

/// <summary>
/// 笔记本翻页箭头提示
/// 根据当前页面状态显示/隐藏左右箭头，并添加呼吸动画
/// </summary>
public class NotebookArrowHint : MonoBehaviour
{
    [Header("关联控制器")]
    [Tooltip("笔记本控制器引用")]
    public NotebookController notebookController;

    [Header("箭头引用")]
    [Tooltip("左箭头 SpriteRenderer")]
    public SpriteRenderer leftArrow;

    [Tooltip("右箭头 SpriteRenderer")]
    public SpriteRenderer rightArrow;

    [Header("动画设置")]
    [Tooltip("启用呼吸动画")]
    public bool enablePulseAnimation = true;

    [Tooltip("呼吸动画周期")]
    public float pulseDuration = 1.5f;

    [Tooltip("最小透明度")]
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;

    [Tooltip("最大透明度")]
    [Range(0f, 1f)]
    public float maxAlpha = 0.8f;

    [Tooltip("启用轻微移动动画")]
    public bool enableMoveAnimation = true;

    [Tooltip("移动距离")]
    public float moveDistance = 5f;

    [Header("淡入淡出")]
    [Tooltip("箭头显示/隐藏淡入淡出时间")]
    public float fadeDuration = 0.3f;

    [Header("悬停效果")]
    [Tooltip("启用鼠标悬停效果")]
    public bool enableHoverEffect = true;

    [Tooltip("悬停时的缩放")]
    public float hoverScale = 1.2f;

    [Tooltip("悬停缩放动画时间")]
    public float hoverScaleDuration = 0.1f;

    // 内部状态
    private Coroutine leftPulseCoroutine;
    private Coroutine rightPulseCoroutine;
    private Vector3 leftArrowOriginalPos;
    private Vector3 rightArrowOriginalPos;
    private bool isLeftHovered = false;
    private bool isRightHovered = false;

    private void Start()
    {
        // 记录原始位置
        if (leftArrow != null)
        {
            leftArrowOriginalPos = leftArrow.transform.localPosition;
        }

        if (rightArrow != null)
        {
            rightArrowOriginalPos = rightArrow.transform.localPosition;
        }

        // 获取控制器引用
        if (notebookController == null)
        {
            notebookController = GetComponentInParent<NotebookController>();
        }

        if (notebookController != null)
        {
            // 订阅事件
            notebookController.OnPageChanged.AddListener(OnPageChanged);
            notebookController.OnFirstPage.AddListener(OnFirstPage);
            notebookController.OnLastPage.AddListener(OnLastPage);

            // 初始化显示状态
            UpdateArrowVisibility();
        }
        else
        {
            Debug.LogWarning("[NotebookArrowHint] 未找到 NotebookController！");
        }

        // 开始动画
        StartPulseAnimations();
    }

    private void OnDestroy()
    {
        if (notebookController != null)
        {
            notebookController.OnPageChanged.RemoveListener(OnPageChanged);
            notebookController.OnFirstPage.RemoveListener(OnFirstPage);
            notebookController.OnLastPage.RemoveListener(OnLastPage);
        }
    }

    private void Update()
    {
        if (!enableHoverEffect) return;

        // 检测鼠标悬停
        CheckHover();
    }

    // ============ 事件回调 ============

    private void OnPageChanged(int newIndex)
    {
        UpdateArrowVisibility();
    }

    private void OnFirstPage()
    {
        // 可选：播放左箭头抖动效果
        if (leftArrow != null)
        {
            StartCoroutine(ShakeArrow(leftArrow, true));
        }
    }

    private void OnLastPage()
    {
        // 可选：播放右箭头抖动效果
        if (rightArrow != null)
        {
            StartCoroutine(ShakeArrow(rightArrow, false));
        }
    }

    // ============ 显示更新 ============

    /// <summary>
    /// 更新箭头显示状态
    /// </summary>
    private void UpdateArrowVisibility()
    {
        if (notebookController == null) return;

        // 左箭头：不在首页时显示
        bool showLeft = !notebookController.IsFirstSpread;
        SetArrowVisible(leftArrow, showLeft, ref leftPulseCoroutine, true);

        // 右箭头：不在末页时显示
        bool showRight = !notebookController.IsLastSpread;
        SetArrowVisible(rightArrow, showRight, ref rightPulseCoroutine, false);
    }

    /// <summary>
    /// 设置箭头显示状态
    /// </summary>
    private void SetArrowVisible(SpriteRenderer arrow, bool visible, ref Coroutine pulseCoroutine, bool isLeft)
    {
        if (arrow == null) return;

        // 停止当前动画
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }

        if (visible)
        {
            // 淡入
            StartCoroutine(FadeArrow(arrow, maxAlpha, fadeDuration));

            // 开始呼吸动画
            if (enablePulseAnimation)
            {
                pulseCoroutine = StartCoroutine(PulseAnimation(arrow, isLeft));
            }
        }
        else
        {
            // 淡出
            StartCoroutine(FadeArrow(arrow, 0f, fadeDuration));
        }
    }

    // ============ 动画 ============

    /// <summary>
    /// 开始所有呼吸动画
    /// </summary>
    private void StartPulseAnimations()
    {
        if (!enablePulseAnimation) return;

        if (leftArrow != null && notebookController != null && !notebookController.IsFirstSpread)
        {
            leftPulseCoroutine = StartCoroutine(PulseAnimation(leftArrow, true));
        }

        if (rightArrow != null && notebookController != null && !notebookController.IsLastSpread)
        {
            rightPulseCoroutine = StartCoroutine(PulseAnimation(rightArrow, false));
        }
    }

    /// <summary>
    /// 呼吸动画
    /// </summary>
    private IEnumerator PulseAnimation(SpriteRenderer arrow, bool isLeft)
    {
        if (arrow == null) yield break;

        Vector3 originalPos = isLeft ? leftArrowOriginalPos : rightArrowOriginalPos;
        float moveDir = isLeft ? -1f : 1f;

        while (true)
        {
            float elapsed = 0f;

            // 呼吸周期
            while (elapsed < pulseDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / pulseDuration;

                // 使用正弦波实现平滑呼吸
                float wave = (Mathf.Sin(t * Mathf.PI * 2f - Mathf.PI / 2f) + 1f) / 2f;

                // 透明度变化
                float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);
                SetSpriteAlpha(arrow, alpha);

                // 位置轻微移动
                if (enableMoveAnimation)
                {
                    float offset = Mathf.Sin(t * Mathf.PI * 2f) * moveDistance * moveDir;
                    arrow.transform.localPosition = originalPos + new Vector3(offset, 0f, 0f);
                }

                yield return null;
            }
        }
    }

    /// <summary>
    /// 淡入淡出动画
    /// </summary>
    private IEnumerator FadeArrow(SpriteRenderer arrow, float targetAlpha, float duration)
    {
        if (arrow == null) yield break;

        float startAlpha = arrow.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            SetSpriteAlpha(arrow, alpha);
            yield return null;
        }

        SetSpriteAlpha(arrow, targetAlpha);
    }

    /// <summary>
    /// 箭头抖动效果（到达边界时）
    /// </summary>
    private IEnumerator ShakeArrow(SpriteRenderer arrow, bool isLeft)
    {
        if (arrow == null) yield break;

        Vector3 originalPos = arrow.transform.localPosition;
        float shakeDuration = 0.3f;
        float shakeIntensity = 8f;
        int shakeCount = 3;

        for (int i = 0; i < shakeCount; i++)
        {
            float offset = (i % 2 == 0 ? 1f : -1f) * shakeIntensity * (isLeft ? -1f : 1f);
            arrow.transform.localPosition = originalPos + new Vector3(offset, 0f, 0f);
            yield return new WaitForSeconds(shakeDuration / shakeCount / 2f);

            arrow.transform.localPosition = originalPos;
            yield return new WaitForSeconds(shakeDuration / shakeCount / 2f);

            shakeIntensity *= 0.6f; // 衰减
        }

        arrow.transform.localPosition = originalPos;
    }

    // ============ 悬停检测 ============

    /// <summary>
    /// 检测鼠标悬停
    /// </summary>
    private void CheckHover()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 检测左箭头悬停
        if (leftArrow != null)
        {
            Collider2D leftCollider = leftArrow.GetComponent<Collider2D>();
            if (leftCollider != null)
            {
                bool hovered = leftCollider.OverlapPoint(mouseWorldPos);
                if (hovered != isLeftHovered)
                {
                    isLeftHovered = hovered;
                    StartCoroutine(ScaleArrow(leftArrow, hovered ? hoverScale : 1f));
                }
            }
        }

        // 检测右箭头悬停
        if (rightArrow != null)
        {
            Collider2D rightCollider = rightArrow.GetComponent<Collider2D>();
            if (rightCollider != null)
            {
                bool hovered = rightCollider.OverlapPoint(mouseWorldPos);
                if (hovered != isRightHovered)
                {
                    isRightHovered = hovered;
                    StartCoroutine(ScaleArrow(rightArrow, hovered ? hoverScale : 1f));
                }
            }
        }
    }

    /// <summary>
    /// 箭头缩放动画
    /// </summary>
    private IEnumerator ScaleArrow(SpriteRenderer arrow, float targetScale)
    {
        if (arrow == null) yield break;

        Vector3 startScale = arrow.transform.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float elapsed = 0f;

        while (elapsed < hoverScaleDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / hoverScaleDuration;
            arrow.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        arrow.transform.localScale = endScale;
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 设置精灵透明度
    /// </summary>
    private void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        Color c = renderer.color;
        renderer.color = new Color(c.r, c.g, c.b, alpha);
    }
}