// Assets/Scripts/GamePlay/Experimenter/MagnifierTargetZone.cs
// 放大镜目标区域检测组件
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 放大镜目标区域
/// 用于检测放大镜是否进入/停留在目标区域
/// 可以添加视觉提示效果
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MagnifierTargetZone : MonoBehaviour
{
    #region ========== 设置 ==========

    [Header("检测设置")]
    [Tooltip("需要检测的放大镜标签")]
    [SerializeField] private string magnifierTag = "Magnifier";

    [Tooltip("是否使用触发器检测（否则使用距离检测）")]
    [SerializeField] private bool useTriggerDetection = true;

    #endregion

    #region ========== 视觉效果 ==========

    [Header("视觉效果")]
    [Tooltip("目标区域精灵渲染器")]
    [SerializeField] private SpriteRenderer zoneRenderer;

    [Tooltip("正常状态颜色")]
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.3f);

    [Tooltip("放大镜进入时的颜色")]
    [SerializeField] private Color highlightColor = new Color(0f, 1f, 0f, 0.5f);

    [Tooltip("是否显示脉冲动画")]
    [SerializeField] private bool showPulseAnimation = true;

    [Tooltip("脉冲动画速度")]
    [SerializeField] private float pulseSpeed = 2f;

    [Tooltip("脉冲动画幅度")]
    [SerializeField] private float pulseAmplitude = 0.2f;

    #endregion

    #region ========== 事件 ==========

    [Header("事件")]
    public UnityEvent OnMagnifierEnter;
    public UnityEvent OnMagnifierExit;
    public UnityEvent OnMagnifierStay;

    #endregion

    #region ========== 私有变量 ==========

    private bool isMagnifierInZone = false;
    private float pulseTimer = 0f;
    private Color currentBaseColor;

    #endregion

    #region ========== Unity 生命周期 ==========

    private void Awake()
    {
        // 确保碰撞体是触发器
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null && useTriggerDetection)
        {
            collider.isTrigger = true;
        }

        // 初始化渲染器
        if (zoneRenderer == null)
        {
            zoneRenderer = GetComponent<SpriteRenderer>();
        }

        currentBaseColor = normalColor;

        if (zoneRenderer != null)
        {
            zoneRenderer.color = normalColor;
        }
    }

    private void Update()
    {
        UpdatePulseAnimation();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!useTriggerDetection) return;

        if (IsMagnifier(other.gameObject))
        {
            MagnifierEntered();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!useTriggerDetection) return;

        if (IsMagnifier(other.gameObject))
        {
            OnMagnifierStay?.Invoke();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!useTriggerDetection) return;

        if (IsMagnifier(other.gameObject))
        {
            MagnifierExited();
        }
    }

    #endregion

    #region ========== 检测逻辑 ==========

    private bool IsMagnifier(GameObject obj)
    {
        // 检查标签
        if (!string.IsNullOrEmpty(magnifierTag) && obj.CompareTag(magnifierTag))
        {
            return true;
        }

        // 检查组件
        return obj.GetComponent<DraggableMagnifier>() != null;
    }

    private void MagnifierEntered()
    {
        if (isMagnifierInZone) return;

        isMagnifierInZone = true;
        currentBaseColor = highlightColor;

        Debug.Log("[MagnifierTargetZone] 放大镜进入目标区域");

        OnMagnifierEnter?.Invoke();
    }

    private void MagnifierExited()
    {
        if (!isMagnifierInZone) return;

        isMagnifierInZone = false;
        currentBaseColor = normalColor;

        Debug.Log("[MagnifierTargetZone] 放大镜离开目标区域");

        OnMagnifierExit?.Invoke();
    }

    #endregion

    #region ========== 视觉效果 ==========

    private void UpdatePulseAnimation()
    {
        if (zoneRenderer == null) return;

        if (showPulseAnimation)
        {
            pulseTimer += Time.deltaTime * pulseSpeed;
            float pulse = 1f + Mathf.Sin(pulseTimer) * pulseAmplitude;

            Color animatedColor = currentBaseColor;
            animatedColor.a = currentBaseColor.a * pulse;

            zoneRenderer.color = animatedColor;
        }
        else
        {
            zoneRenderer.color = currentBaseColor;
        }
    }

    #endregion

    #region ========== 公共接口 ==========

    /// <summary>
    /// 是否有放大镜在区域内
    /// </summary>
    public bool IsMagnifierInZone => isMagnifierInZone;

    /// <summary>
    /// 设置区域可见性
    /// </summary>
    public void SetVisible(bool visible)
    {
        if (zoneRenderer != null)
        {
            zoneRenderer.enabled = visible;
        }
    }

    /// <summary>
    /// 高亮显示区域
    /// </summary>
    public void Highlight()
    {
        currentBaseColor = highlightColor;
    }

    /// <summary>
    /// 取消高亮
    /// </summary>
    public void Unhighlight()
    {
        currentBaseColor = normalColor;
    }

    #endregion

    #region ========== 编辑器辅助 ==========

    private void OnDrawGizmos()
    {
        // 绘制目标区域
        Collider2D collider = GetComponent<Collider2D>();

        if (collider != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);

            if (collider is CircleCollider2D circle)
            {
                Gizmos.DrawSphere(transform.position + (Vector3)circle.offset, circle.radius * transform.lossyScale.x);
            }
            else if (collider is BoxCollider2D box)
            {
                Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size * transform.lossyScale.x);
            }
        }
    }

    #endregion
}