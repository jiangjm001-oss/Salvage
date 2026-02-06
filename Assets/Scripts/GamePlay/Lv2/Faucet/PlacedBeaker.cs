// Assets/Scripts/GamePlay/PlacedBeaker.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 放置的烧杯 - 点击后拾取
/// 根据水龙头状态决定拾取空烧杯还是有水的烧杯
/// 
/// 挂载到放置的烧杯物体上，需要 Collider2D 用于点击检测
/// </summary>
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlacedBeaker : MonoBehaviour
{
    [Header("系统引用")]
    [Tooltip("关联的水龙头系统（留空则自动查找）")]
    public FaucetWaterSystem faucetSystem;

    [Header("精灵图设置")]
    [Tooltip("空烧杯精灵图")]
    public Sprite emptySprite;

    [Tooltip("有水烧杯精灵图")]
    public Sprite filledSprite;

    [Header("接水动画设置")]
    [Tooltip("接水时的缩放动画")]
    public bool useScaleAnimation = true;

    [Tooltip("接水时最大缩放")]
    public float fillScaleMultiplier = 1.1f;

    [Tooltip("缩放动画时间")]
    public float scaleAnimationDuration = 0.3f;

    [Header("水面上升动画（可选）")]
    [Tooltip("是否使用水面上升效果")]
    public bool useWaterRiseEffect = false;

    [Tooltip("水面遮罩物体")]
    public SpriteMask waterMask;

    [Tooltip("水面上升时间")]
    public float waterRiseDuration = 0.8f;

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private bool isAnimating = false;
    private bool isFilled = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;
    }

    private void Start()
    {
        // 自动查找系统
        if (faucetSystem == null)
        {
            faucetSystem = GetComponentInParent<FaucetWaterSystem>();
            if (faucetSystem == null)
            {
                faucetSystem = FaucetWaterSystem.Instance;
            }
        }

        if (faucetSystem == null)
        {
            Debug.LogError("[PlacedBeaker] 未找到 FaucetWaterSystem！");
            return;
        }

        // 订阅水龙头事件
        faucetSystem.OnFaucetTurnOn.AddListener(OnFaucetTurnedOn);
        faucetSystem.OnFaucetTurnOff.AddListener(OnFaucetTurnedOff);
        faucetSystem.OnBeakerPlaced.AddListener(OnBeakerPlaced);

        // 初始化外观
        UpdateAppearance();
    }

    private void OnDestroy()
    {
        if (faucetSystem != null)
        {
            faucetSystem.OnFaucetTurnOn.RemoveListener(OnFaucetTurnedOn);
            faucetSystem.OnFaucetTurnOff.RemoveListener(OnFaucetTurnedOff);
            faucetSystem.OnBeakerPlaced.RemoveListener(OnBeakerPlaced);
        }
    }

    private void OnEnable()
    {
        // 每次显示时重置状态
        transform.localScale = originalScale;
        UpdateAppearance();
    }

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (isAnimating) return;

        HandleClick();
    }

    /// <summary>
    /// 处理点击
    /// </summary>
    private void HandleClick()
    {
        if (faucetSystem == null)
        {
            Debug.LogError("[PlacedBeaker] FaucetWaterSystem 未设置！");
            return;
        }

        Debug.Log("[PlacedBeaker] 点击放置的烧杯");

        // 尝试拾取
        bool success = faucetSystem.TryPickupBeaker();

        if (success)
        {
            Debug.Log("[PlacedBeaker] 拾取成功");
        }
    }

    /// <summary>
    /// 水龙头打开事件
    /// </summary>
    private void OnFaucetTurnedOn()
    {
        if (!gameObject.activeInHierarchy) return;

        Debug.Log("[PlacedBeaker] 水龙头打开，开始接水");
        StartCoroutine(FillWithWaterAnimation());
    }

    /// <summary>
    /// 水龙头关闭事件
    /// </summary>
    private void OnFaucetTurnedOff()
    {
        // 水龙头关闭时，烧杯保持当前状态（如果已经有水就还是有水）
        // 这里不做任何操作，因为水不会自己消失
    }

    /// <summary>
    /// 烧杯刚被放置事件
    /// </summary>
    private void OnBeakerPlaced()
    {
        // 重置状态
        isFilled = false;
        UpdateAppearance();

        // 如果水龙头是开的，立即开始接水动画
        if (faucetSystem != null && faucetSystem.IsFaucetOn)
        {
            StartCoroutine(FillWithWaterAnimation());
        }
    }

    /// <summary>
    /// 接水动画
    /// </summary>
    private IEnumerator FillWithWaterAnimation()
    {
        isAnimating = true;

        // 缩放动画（模拟水倒入的冲击）
        if (useScaleAnimation)
        {
            yield return StartCoroutine(ScalePulseAnimation());
        }

        // 水面上升动画
        if (useWaterRiseEffect && waterMask != null)
        {
            yield return StartCoroutine(WaterRiseAnimation());
        }
        else
        {
            // 没有水面动画，直接等待一小段时间
            yield return new WaitForSeconds(0.3f);
        }

        // 更新为有水状态
        isFilled = true;
        UpdateAppearance();

        isAnimating = false;

        Debug.Log("[PlacedBeaker] 接水动画完成");
    }

    /// <summary>
    /// 缩放脉冲动画
    /// </summary>
    private IEnumerator ScalePulseAnimation()
    {
        float elapsed = 0f;
        float halfDuration = scaleAnimationDuration / 2f;

        // 放大
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(1f, fillScaleMultiplier, Mathf.SmoothStep(0f, 1f, t));
            transform.localScale = originalScale * scale;
            yield return null;
        }

        // 缩小回原始大小
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scale = Mathf.Lerp(fillScaleMultiplier, 1f, Mathf.SmoothStep(0f, 1f, t));
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
    }

    /// <summary>
    /// 水面上升动画（需要配合 SpriteMask）
    /// </summary>
    private IEnumerator WaterRiseAnimation()
    {
        if (waterMask == null) yield break;

        // 获取遮罩的初始和目标位置
        Vector3 startPos = waterMask.transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * 0.5f; // 根据实际情况调整

        float elapsed = 0f;

        while (elapsed < waterRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / waterRiseDuration);
            waterMask.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        waterMask.transform.localPosition = endPos;
    }

    /// <summary>
    /// 更新烧杯外观
    /// </summary>
    private void UpdateAppearance()
    {
        if (spriteRenderer == null) return;

        // 优先使用系统中的精灵图设置
        if (faucetSystem != null)
        {
            Sprite targetSprite;

            if (isFilled || faucetSystem.IsFaucetOn)
            {
                targetSprite = faucetSystem.placedFilledSprite ?? filledSprite;
            }
            else
            {
                targetSprite = faucetSystem.placedEmptySprite ?? emptySprite;
            }

            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
        }
        else
        {
            // 使用本地设置
            Sprite targetSprite = isFilled ? filledSprite : emptySprite;
            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
        }
    }

    /// <summary>
    /// 手动设置填充状态（用于存档恢复）
    /// </summary>
    public void SetFilledState(bool filled)
    {
        isFilled = filled;
        UpdateAppearance();
    }
}