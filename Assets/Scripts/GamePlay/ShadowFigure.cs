// Assets/Scripts/GamePlay/ShadowFigure.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 黑影实体 - 处理单个黑影的显示、移动、淡出行为
/// </summary>
public class ShadowFigure : MonoBehaviour
{
    [Header("所属墙面")]
    [Tooltip("这个黑影属于哪面墙")]
    public GameManager.ViewState belongsToWall;

    [Header("移动设置")]
    [Tooltip("点击后向右移动的距离")]
    public float moveDistance = 2f;

    [Tooltip("移动持续时间（秒）")]
    public float moveDuration = 0.5f;

    [Tooltip("移动使用的缓动曲线")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("淡出设置")]
    [Tooltip("淡出持续时间（秒）")]
    public float fadeDuration = 0.8f;

    [Tooltip("移动和淡出是否同时进行")]
    public bool fadeWhileMoving = true;

    [Header("音效")]
    [Tooltip("出现时的音效")]
    public string appearSound = "";

    [Tooltip("点击时的音效")]
    public string clickSound = "";

    [Tooltip("消失时的音效")]
    public string vanishSound = "";

    [Header("状态（只读）")]
    [Tooltip("是否已被点击过")]
    public bool hasBeenClicked = false;

    [Tooltip("是否正在播放动画")]
    public bool isAnimating = false;

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Vector3 originalPosition;
    private Color originalColor;
    private bool isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化组件引用
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;

        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // 记录原始位置
        originalPosition = transform.localPosition;

        // 记录原始颜色（需要有 SpriteRenderer）
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        else
        {
            originalColor = Color.white;
            Debug.LogWarning($"[ShadowFigure] '{gameObject.name}' 没有 SpriteRenderer 组件！");
        }

        if (col == null)
        {
            Debug.LogWarning($"[ShadowFigure] '{gameObject.name}' 没有 Collider2D 组件！");
        }

        isInitialized = true;

        // 初始隐藏
        Hide();
    }

    /// <summary>
    /// 显示黑影
    /// </summary>
    public void Show()
    {
        if (hasBeenClicked) return;

        // 确保已初始化
        if (!isInitialized) Initialize();

        Debug.Log($"[ShadowFigure] 黑影显示在 {belongsToWall}");

        // 重置位置和颜色
        transform.localPosition = originalPosition;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
            spriteRenderer.enabled = true;
        }

        // 启用显示和碰撞
        gameObject.SetActive(true);
        if (col != null)
        {
            col.enabled = true;
        }

        // 播放出现音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(appearSound))
        {
            AudioManager.Instance.PlaySFX(appearSound);
        }
    }

    /// <summary>
    /// 隐藏黑影
    /// </summary>
    public void Hide()
    {
        // 确保已初始化
        if (!isInitialized)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            col = GetComponent<Collider2D>();
        }

        // 安全隐藏
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        if (col != null)
        {
            col.enabled = false;
        }
    }

    /// <summary>
    /// 点击黑影（由 InteractionSystem 或点击事件调用）
    /// </summary>
    public void OnClick()
    {
        if (hasBeenClicked || isAnimating) return;

        Debug.Log($"[ShadowFigure] 黑影被点击: {belongsToWall}");
        hasBeenClicked = true;

        // 播放点击音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(clickSound))
        {
            AudioManager.Instance.PlaySFX(clickSound);
        }

        // 通知控制器
        if (ShadowChaseController.Instance != null)
        {
            ShadowChaseController.Instance.OnShadowClicked(this);
        }

        // 开始移动和淡出动画
        StartCoroutine(MoveAndFadeCoroutine());
    }

    /// <summary>
    /// 移动和淡出协程
    /// </summary>
    private IEnumerator MoveAndFadeCoroutine()
    {
        isAnimating = true;

        // 禁用碰撞（防止重复点击）
        if (col != null)
        {
            col.enabled = false;
        }

        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.right * moveDistance;

        Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float elapsed = 0f;
        float totalDuration = fadeWhileMoving ? Mathf.Max(moveDuration, fadeDuration) : moveDuration + fadeDuration;

        if (fadeWhileMoving)
        {
            // 同时移动和淡出
            while (elapsed < totalDuration)
            {
                elapsed += Time.deltaTime;

                // 移动
                if (elapsed < moveDuration)
                {
                    float moveT = moveCurve.Evaluate(elapsed / moveDuration);
                    transform.localPosition = Vector3.Lerp(startPos, endPos, moveT);
                }
                else
                {
                    transform.localPosition = endPos;
                }

                // 淡出
                if (spriteRenderer != null && elapsed < fadeDuration)
                {
                    float fadeT = elapsed / fadeDuration;
                    spriteRenderer.color = Color.Lerp(startColor, endColor, fadeT);
                }
                else if (spriteRenderer != null)
                {
                    spriteRenderer.color = endColor;
                }

                yield return null;
            }
        }
        else
        {
            // 先移动
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = moveCurve.Evaluate(elapsed / moveDuration);
                transform.localPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            transform.localPosition = endPos;

            // 再淡出
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(startColor, endColor, t);
                }
                yield return null;
            }
        }

        // 确保最终状态
        transform.localPosition = endPos;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = endColor;
        }

        // 播放消失音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(vanishSound))
        {
            AudioManager.Instance.PlaySFX(vanishSound);
        }

        // 完全隐藏
        Hide();
        isAnimating = false;

        Debug.Log($"[ShadowFigure] 黑影消失: {belongsToWall}");
    }

    /// <summary>
    /// 重置黑影状态（用于调试或重新开始）
    /// </summary>
    public void ResetShadow()
    {
        StopAllCoroutines();
        hasBeenClicked = false;
        isAnimating = false;
        transform.localPosition = originalPosition;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        Hide();
    }

    // ============ 点击检测 ============

    private void OnMouseDown()
    {
        // 简单的点击检测
        OnClick();
    }
}