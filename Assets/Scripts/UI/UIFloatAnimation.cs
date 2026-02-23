// Assets/Scripts/UI/UIFloatAnimation.cs
using UnityEngine;

/// <summary>
/// UI 浮动动画组件
/// 实现平滑的上下浮动效果，适用于主菜单标题、按钮等UI元素
/// 使用正弦波函数实现自然的浮动感
/// </summary>
public class UIFloatAnimation : MonoBehaviour
{
    // ============ 动画参数 ============
    [Header("浮动设置")]
    [Tooltip("浮动幅度（像素）")]
    [SerializeField] private float floatAmount = 10f;

    [Tooltip("浮动速度（越大越快）")]
    [SerializeField] private float floatSpeed = 1.5f;

    [Tooltip("是否使用随机初始相位（多个元素错开浮动）")]
    [SerializeField] private bool useRandomPhase = false;

    [Tooltip("自定义初始相位（0-1，仅当不使用随机相位时有效）")]
    [Range(0f, 1f)]
    [SerializeField] private float customPhase = 0f;

    // ============ 启动设置 ============
    [Header("启动设置")]
    [Tooltip("启动延迟（秒）- 等待淡入动画完成后再开始浮动")]
    [SerializeField] private float startDelay = 0f;

    [Tooltip("是否在 Start 时自动开始")]
    [SerializeField] private bool autoStart = true;

    [Tooltip("淡入启动浮动（平滑过渡）")]
    [SerializeField] private bool fadeInFloat = true;

    [Tooltip("淡入持续时间")]
    [SerializeField] private float fadeInDuration = 0.5f;

    // ============ 内部变量 ============
    private RectTransform rectTransform;
    private Transform normalTransform;
    private bool isUI = false;

    private Vector3 originalPosition;
    private float timeOffset = 0f;
    private bool isFloating = false;
    private float currentAmplitude = 0f;
    private float fadeInTimer = 0f;

    // ============ 生命周期 ============

    private void Awake()
    {
        // 检测是UI元素还是普通GameObject
        rectTransform = GetComponent<RectTransform>();
        isUI = rectTransform != null;
        normalTransform = transform;

        // 记录初始位置
        originalPosition = isUI ? rectTransform.anchoredPosition3D : transform.localPosition;

        // 设置相位偏移
        if (useRandomPhase)
        {
            timeOffset = Random.Range(0f, Mathf.PI * 2f);
        }
        else
        {
            timeOffset = customPhase * Mathf.PI * 2f;
        }
    }

    private void Start()
    {
        if (autoStart)
        {
            if (startDelay > 0)
            {
                Invoke(nameof(StartFloating), startDelay);
            }
            else
            {
                StartFloating();
            }
        }
    }

    private void Update()
    {
        if (!isFloating) return;

        // 处理淡入
        if (fadeInFloat && fadeInTimer < fadeInDuration)
        {
            fadeInTimer += Time.deltaTime;
            float t = Mathf.Clamp01(fadeInTimer / fadeInDuration);
            // 使用平滑的缓入曲线
            currentAmplitude = floatAmount * EaseOutQuad(t);
        }
        else
        {
            currentAmplitude = floatAmount;
        }

        // 计算浮动偏移
        float yOffset = Mathf.Sin((Time.time * floatSpeed) + timeOffset) * currentAmplitude;

        // 应用位置
        if (isUI)
        {
            rectTransform.anchoredPosition3D = new Vector3(
                originalPosition.x,
                originalPosition.y + yOffset,
                originalPosition.z
            );
        }
        else
        {
            normalTransform.localPosition = new Vector3(
                originalPosition.x,
                originalPosition.y + yOffset,
                originalPosition.z
            );
        }
    }

    private void OnDisable()
    {
        // 禁用时恢复原始位置
        ResetPosition();
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 开始浮动动画
    /// </summary>
    public void StartFloating()
    {
        isFloating = true;
        fadeInTimer = 0f;

        if (!fadeInFloat)
        {
            currentAmplitude = floatAmount;
        }

        Debug.Log($"[UIFloatAnimation] {gameObject.name} 开始浮动动画");
    }

    /// <summary>
    /// 停止浮动动画
    /// </summary>
    public void StopFloating()
    {
        isFloating = false;
        ResetPosition();
        Debug.Log($"[UIFloatAnimation] {gameObject.name} 停止浮动动画");
    }

    /// <summary>
    /// 暂停浮动（保持当前位置）
    /// </summary>
    public void PauseFloating()
    {
        isFloating = false;
    }

    /// <summary>
    /// 恢复浮动
    /// </summary>
    public void ResumeFloating()
    {
        isFloating = true;
    }

    /// <summary>
    /// 重置到原始位置
    /// </summary>
    public void ResetPosition()
    {
        if (isUI && rectTransform != null)
        {
            rectTransform.anchoredPosition3D = originalPosition;
        }
        else if (normalTransform != null)
        {
            normalTransform.localPosition = originalPosition;
        }
    }

    /// <summary>
    /// 更新原始位置（当基础位置改变时调用）
    /// </summary>
    public void UpdateOriginalPosition()
    {
        originalPosition = isUI ? rectTransform.anchoredPosition3D : transform.localPosition;
    }

    /// <summary>
    /// 设置浮动参数
    /// </summary>
    public void SetFloatParameters(float amount, float speed)
    {
        floatAmount = amount;
        floatSpeed = speed;
    }

    // ============ 缓动函数 ============

    /// <summary>
    /// 缓出二次方
    /// </summary>
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    // ============ 属性访问 ============

    /// <summary>
    /// 是否正在浮动
    /// </summary>
    public bool IsFloating => isFloating;

    /// <summary>
    /// 浮动幅度
    /// </summary>
    public float FloatAmount
    {
        get => floatAmount;
        set => floatAmount = value;
    }

    /// <summary>
    /// 浮动速度
    /// </summary>
    public float FloatSpeed
    {
        get => floatSpeed;
        set => floatSpeed = value;
    }

    // ============ 编辑器辅助 ============

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 限制参数范围
        floatAmount = Mathf.Max(0f, floatAmount);
        floatSpeed = Mathf.Max(0.1f, floatSpeed);
        startDelay = Mathf.Max(0f, startDelay);
        fadeInDuration = Mathf.Max(0.1f, fadeInDuration);
    }

    /// <summary>
    /// 在Scene视图中显示浮动范围
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying)
        {
            Vector3 pos = transform.position;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);

            // 绘制浮动范围线
            Vector3 topPos = pos + Vector3.up * floatAmount;
            Vector3 bottomPos = pos - Vector3.up * floatAmount;

            Gizmos.DrawLine(topPos - Vector3.right * 0.5f, topPos + Vector3.right * 0.5f);
            Gizmos.DrawLine(bottomPos - Vector3.right * 0.5f, bottomPos + Vector3.right * 0.5f);
            Gizmos.DrawLine(pos + Vector3.up * floatAmount, pos - Vector3.up * floatAmount);
        }
    }
#endif
}