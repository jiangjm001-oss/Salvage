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
    private Coroutine currentAnimationCoroutine; // ⭐ 新增：跟踪当前协程

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
        if (hasBeenClicked)
        {
            Debug.Log($"[ShadowFigure] {belongsToWall} 已被点击过，不再显示");
            return;
        }

        // 确保已初始化
        if (!isInitialized) Initialize();

        // ⭐ 如果正在播放动画，不要重置状态
        if (isAnimating)
        {
            Debug.Log($"[ShadowFigure] {belongsToWall} 正在播放动画，跳过 Show");
            return;
        }

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
    /// 隐藏黑影（不改变 hasBeenClicked 状态）
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

        // ⭐ 新增：也禁用 GameObject，确保完全隐藏
        // 注意：这会停止协程，所以只在非动画时调用
        if (!isAnimating)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ⭐ 新增：临时隐藏（切换视图时调用，不停止协程）
    /// </summary>
    public void HideTemporary()
    {
        if (hasBeenClicked) return; // 已点击的不需要处理

        Debug.Log($"[ShadowFigure] 临时隐藏 {belongsToWall}");

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
        // ⭐ 改进：更详细的日志
        if (hasBeenClicked)
        {
            Debug.Log($"[ShadowFigure] {belongsToWall} 已被点击过，忽略");
            return;
        }

        if (isAnimating)
        {
            Debug.Log($"[ShadowFigure] {belongsToWall} 正在动画中，忽略");
            return;
        }

        Debug.Log($"[ShadowFigure] 黑影被点击: {belongsToWall}");
        hasBeenClicked = true;

        // 播放点击音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(clickSound))
        {
            AudioManager.Instance.PlaySFX(clickSound);
        }

        // ⭐ 先启动协程，确保动画开始
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
        }
        currentAnimationCoroutine = StartCoroutine(MoveAndFadeCoroutine());

        // 通知控制器（放在协程启动之后）
        if (ShadowChaseController.Instance != null)
        {
            ShadowChaseController.Instance.OnShadowClicked(this);
        }
    }

    /// <summary>
    /// 移动和淡出协程
    /// </summary>
    private IEnumerator MoveAndFadeCoroutine()
    {
        isAnimating = true;
        Debug.Log($"[ShadowFigure] {belongsToWall} 开始移动淡出动画");

        // 禁用碰撞（防止重复点击）
        if (col != null)
        {
            col.enabled = false;
        }

        // ⭐ 确保 spriteRenderer 可用
        if (spriteRenderer == null)
        {
            Debug.LogError($"[ShadowFigure] {belongsToWall} spriteRenderer 为空，无法播放动画！");
            isAnimating = false;
            yield break;
        }

        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.right * moveDistance;

        Color startColor = spriteRenderer.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

        float elapsed = 0f;
        float totalDuration = fadeWhileMoving ? Mathf.Max(moveDuration, fadeDuration) : moveDuration + fadeDuration;

        Debug.Log($"[ShadowFigure] {belongsToWall} 动画参数: startPos={startPos}, endPos={endPos}, totalDuration={totalDuration}");

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
                if (elapsed < fadeDuration)
                {
                    float fadeT = elapsed / fadeDuration;
                    spriteRenderer.color = Color.Lerp(startColor, endColor, fadeT);
                }
                else
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
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }
        }

        // 确保最终状态
        transform.localPosition = endPos;
        spriteRenderer.color = endColor;

        // 播放消失音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(vanishSound))
        {
            AudioManager.Instance.PlaySFX(vanishSound);
        }

        Debug.Log($"[ShadowFigure] {belongsToWall} 动画完成，隐藏");

        // ⭐ 先设置 isAnimating = false，再调用 Hide
        isAnimating = false;
        currentAnimationCoroutine = null;

        // 完全隐藏
        spriteRenderer.enabled = false;
        if (col != null)
        {
            col.enabled = false;
        }
        gameObject.SetActive(false);

        Debug.Log($"[ShadowFigure] 黑影消失: {belongsToWall}");
    }

    /// <summary>
    /// 重置黑影状态（用于调试或重新开始）
    /// </summary>
    public void ResetShadow()
    {
        if (currentAnimationCoroutine != null)
        {
            StopCoroutine(currentAnimationCoroutine);
            currentAnimationCoroutine = null;
        }
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