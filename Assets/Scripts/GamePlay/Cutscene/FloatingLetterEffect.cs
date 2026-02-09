// Assets/Scripts/GamePlay/Cutscene/FloatingLetterEffect.cs
// 信纸飘落效果组件
// 可独立使用，实现精美的纸张飘落动画
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 信纸飘落效果组件
/// 模拟真实的纸张飘落物理效果：摆动、旋转、空气阻力
/// </summary>
public class FloatingLetterEffect : MonoBehaviour
{
    // ============ 配置 ============
    [Header("目标设置")]
    [Tooltip("要控制的Image组件（留空则自动获取）")]
    public Image targetImage;

    [Tooltip("要控制的RectTransform（留空则自动获取）")]
    public RectTransform targetRect;

    [Header("运动设置")]
    [Tooltip("起始位置")]
    public Vector2 startPosition = new Vector2(0, 600);

    [Tooltip("结束位置")]
    public Vector2 endPosition = new Vector2(0, -100);

    [Tooltip("飘落总时间（秒）")]
    public float floatDuration = 3.5f;

    [Header("摆动效果")]
    [Tooltip("水平摆动幅度")]
    public float swayAmplitudeX = 80f;

    [Tooltip("摆动频率")]
    public float swayFrequency = 1.5f;

    [Tooltip("摆动随机性")]
    [Range(0f, 1f)]
    public float swayRandomness = 0.3f;

    [Header("旋转效果")]
    [Tooltip("最大旋转角度")]
    public float maxRotation = 20f;

    [Tooltip("旋转跟随摆动")]
    public bool rotationFollowsSway = true;

    [Tooltip("额外旋转速度")]
    public float extraRotationSpeed = 0.5f;

    [Header("缩放效果")]
    [Tooltip("起始缩放")]
    public float startScale = 0.8f;

    [Tooltip("结束缩放")]
    public float endScale = 1f;

    [Header("透明度")]
    [Tooltip("淡入时间（占总时间的比例）")]
    [Range(0f, 0.5f)]
    public float fadeInRatio = 0.2f;

    [Header("物理模拟")]
    [Tooltip("下落加速度曲线（0=均匀，1=先快后慢）")]
    public AnimationCurve fallCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("模拟空气阻力（减速效果）")]
    public bool simulateAirResistance = true;

    [Tooltip("空气阻力强度")]
    [Range(0f, 1f)]
    public float airResistance = 0.3f;

    [Header("特殊效果")]
    [Tooltip("添加轻微抖动")]
    public bool addJitter = true;

    [Tooltip("抖动强度")]
    public float jitterStrength = 2f;

    [Tooltip("落地时的轻微弹跳")]
    public bool bounceOnLanding = true;

    [Tooltip("弹跳高度")]
    public float bounceHeight = 20f;

    [Tooltip("弹跳次数")]
    public int bounceCount = 2;

    [Header("事件")]
    public UnityEngine.Events.UnityEvent OnFloatStart;
    public UnityEngine.Events.UnityEvent OnFloatComplete;
    public UnityEngine.Events.UnityEvent OnLanded;

    // ============ 状态 ============
    [Header("运行时状态（只读）")]
    [SerializeField] private bool isFloating = false;
    [SerializeField] private float progress = 0f;

    // ============ 内部变量 ============
    private Vector2 currentVelocity;
    private float randomSeed;
    private Coroutine floatCoroutine;
    private Color originalColor;

    // ============ Unity生命周期 ============

    private void Awake()
    {
        // 自动获取组件
        if (targetImage == null)
            targetImage = GetComponent<Image>();
        if (targetRect == null)
            targetRect = GetComponent<RectTransform>();

        // 保存原始颜色
        if (targetImage != null)
            originalColor = targetImage.color;

        // 生成随机种子
        randomSeed = Random.Range(0f, 1000f);
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 开始飘落动画
    /// </summary>
    [ContextMenu("开始飘落")]
    public void StartFloat()
    {
        if (isFloating) return;

        if (floatCoroutine != null)
            StopCoroutine(floatCoroutine);

        floatCoroutine = StartCoroutine(FloatAnimation());
    }

    /// <summary>
    /// 停止飘落动画
    /// </summary>
    public void StopFloat()
    {
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
            floatCoroutine = null;
        }
        isFloating = false;
    }

    /// <summary>
    /// 重置到起始位置
    /// </summary>
    [ContextMenu("重置位置")]
    public void ResetToStart()
    {
        StopFloat();

        if (targetRect != null)
        {
            targetRect.anchoredPosition = startPosition;
            targetRect.localRotation = Quaternion.identity;
            targetRect.localScale = Vector3.one * startScale;
        }

        if (targetImage != null)
        {
            Color c = originalColor;
            c.a = 0f;
            targetImage.color = c;
        }

        progress = 0f;
    }

    /// <summary>
    /// 直接设置到结束位置
    /// </summary>
    public void SetToEnd()
    {
        StopFloat();

        if (targetRect != null)
        {
            targetRect.anchoredPosition = endPosition;
            targetRect.localRotation = Quaternion.identity;
            targetRect.localScale = Vector3.one * endScale;
        }

        if (targetImage != null)
        {
            targetImage.color = originalColor;
        }

        progress = 1f;
    }

    // ============ 飘落动画协程 ============

    private IEnumerator FloatAnimation()
    {
        isFloating = true;
        progress = 0f;
        OnFloatStart?.Invoke();

        // 初始化
        if (targetRect != null)
        {
            targetRect.anchoredPosition = startPosition;
            targetRect.localScale = Vector3.one * startScale;
        }

        if (targetImage != null)
        {
            Color c = originalColor;
            c.a = 0f;
            targetImage.color = c;
        }

        float elapsed = 0f;
        Vector2 previousPos = startPosition;

        while (elapsed < floatDuration)
        {
            elapsed += Time.deltaTime;
            progress = Mathf.Clamp01(elapsed / floatDuration);

            // 计算各种效果
            Vector2 position = CalculatePosition(progress, elapsed);
            float rotation = CalculateRotation(progress, elapsed, position - previousPos);
            float scale = CalculateScale(progress);
            float alpha = CalculateAlpha(progress);

            // 应用
            if (targetRect != null)
            {
                targetRect.anchoredPosition = position;
                targetRect.localRotation = Quaternion.Euler(0, 0, rotation);
                targetRect.localScale = Vector3.one * scale;
            }

            if (targetImage != null)
            {
                Color c = originalColor;
                c.a = alpha;
                targetImage.color = c;
            }

            previousPos = position;
            yield return null;
        }

        // 确保最终位置
        if (targetRect != null)
        {
            targetRect.anchoredPosition = endPosition;
        }

        // 落地事件
        OnLanded?.Invoke();

        // 弹跳效果
        if (bounceOnLanding)
        {
            yield return StartCoroutine(BounceAnimation());
        }

        // 完成
        isFloating = false;
        progress = 1f;
        OnFloatComplete?.Invoke();
    }

    // ============ 计算方法 ============

    private Vector2 CalculatePosition(float t, float time)
    {
        // 应用下落曲线
        float curvedT = fallCurve.Evaluate(t);

        // 基础垂直位置
        float y = Mathf.Lerp(startPosition.y, endPosition.y, curvedT);

        // 空气阻力效果（减速）
        if (simulateAirResistance)
        {
            float resistanceFactor = 1f - airResistance * (1f - t);
            y = Mathf.Lerp(startPosition.y, endPosition.y, curvedT * resistanceFactor + (1f - resistanceFactor) * t);
        }

        // 水平摆动
        float swayPhase = time * swayFrequency * Mathf.PI * 2f;
        float randomOffset = Mathf.PerlinNoise(time * 2f + randomSeed, 0f) * swayRandomness;
        float sway = Mathf.Sin(swayPhase + randomOffset * Mathf.PI) * swayAmplitudeX;

        // 摆动幅度随下落减小
        sway *= (1f - t * 0.6f);

        float x = startPosition.x + sway;

        // 抖动
        if (addJitter)
        {
            float jitterX = (Mathf.PerlinNoise(time * 15f + randomSeed, 0f) - 0.5f) * jitterStrength;
            float jitterY = (Mathf.PerlinNoise(0f, time * 15f + randomSeed) - 0.5f) * jitterStrength;
            x += jitterX;
            y += jitterY;
        }

        return new Vector2(x, y);
    }

    private float CalculateRotation(float t, float time, Vector2 velocity)
    {
        float rotation = 0f;

        if (rotationFollowsSway)
        {
            // 根据水平速度计算旋转（模拟真实纸张）
            float horizontalSpeed = velocity.x;
            rotation = Mathf.Clamp(horizontalSpeed * 2f, -maxRotation, maxRotation);
        }

        // 额外的缓慢旋转
        rotation += Mathf.Sin(time * extraRotationSpeed * Mathf.PI * 2f) * maxRotation * 0.3f;

        // 旋转随下落减小
        rotation *= (1f - t * 0.7f);

        return rotation;
    }

    private float CalculateScale(float t)
    {
        // 缩放从 startScale 到 endScale
        return Mathf.Lerp(startScale, endScale, t);
    }

    private float CalculateAlpha(float t)
    {
        // 淡入效果
        if (t < fadeInRatio)
        {
            return Mathf.Lerp(0f, originalColor.a, t / fadeInRatio);
        }
        return originalColor.a;
    }

    // ============ 弹跳动画 ============

    private IEnumerator BounceAnimation()
    {
        if (targetRect == null) yield break;

        Vector2 landPosition = endPosition;
        float currentBounceHeight = bounceHeight;

        for (int i = 0; i < bounceCount; i++)
        {
            // 向上弹
            float upDuration = 0.1f;
            float elapsed = 0f;
            Vector2 startPos = targetRect.anchoredPosition;
            Vector2 peakPos = new Vector2(startPos.x, startPos.y + currentBounceHeight);

            while (elapsed < upDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / upDuration;
                t = 1f - (1f - t) * (1f - t); // 缓出
                targetRect.anchoredPosition = Vector2.Lerp(startPos, peakPos, t);
                yield return null;
            }

            // 向下落
            float downDuration = 0.15f;
            elapsed = 0f;
            startPos = targetRect.anchoredPosition;

            while (elapsed < downDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / downDuration;
                t = t * t; // 缓入（加速下落）
                targetRect.anchoredPosition = Vector2.Lerp(startPos, landPosition, t);
                yield return null;
            }

            // 每次弹跳高度减半
            currentBounceHeight *= 0.4f;

            // 短暂停顿
            yield return new WaitForSeconds(0.02f);
        }

        // 确保最终位置
        targetRect.anchoredPosition = landPosition;
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 设置飘落路径
    /// </summary>
    public void SetPath(Vector2 start, Vector2 end)
    {
        startPosition = start;
        endPosition = end;
    }

    /// <summary>
    /// 获取当前进度（0-1）
    /// </summary>
    public float GetProgress() => progress;

    /// <summary>
    /// 是否正在飘落
    /// </summary>
    public bool IsFloating() => isFloating;

    // ============ 编辑器 ============

#if UNITY_EDITOR
    [Header("编辑器调试")]
    public bool showPathInEditor = true;

    private void OnDrawGizmosSelected()
    {
        if (!showPathInEditor) return;

        // 绘制起点
        Gizmos.color = Color.green;
        Vector3 worldStart = transform.TransformPoint(startPosition);
        Gizmos.DrawWireSphere(worldStart, 20f);

        // 绘制终点
        Gizmos.color = Color.red;
        Vector3 worldEnd = transform.TransformPoint(endPosition);
        Gizmos.DrawWireSphere(worldEnd, 20f);

        // 绘制路径
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(worldStart, worldEnd);

        // 绘制摆动范围
        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
        Vector3 leftBound = transform.TransformPoint(new Vector2(startPosition.x - swayAmplitudeX, (startPosition.y + endPosition.y) / 2f));
        Vector3 rightBound = transform.TransformPoint(new Vector2(startPosition.x + swayAmplitudeX, (startPosition.y + endPosition.y) / 2f));
        Gizmos.DrawLine(leftBound, rightBound);
    }
#endif
}