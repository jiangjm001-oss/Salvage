// Assets/Scripts/GamePlay/FaucetHandle.cs
using UnityEngine;

/// <summary>
/// 水龙头手柄 - 点击切换水龙头开关状态
/// 
/// 挂载到水龙头物体上，需要 Collider2D 用于点击检测
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FaucetHandle : MonoBehaviour
{
    [Header("系统引用")]
    [Tooltip("关联的水龙头系统（留空则自动查找）")]
    public FaucetWaterSystem faucetSystem;

    [Header("视觉反馈")]
    [Tooltip("水龙头关闭状态的精灵图")]
    public Sprite faucetOffSprite;

    [Tooltip("水龙头打开状态的精灵图")]
    public Sprite faucetOnSprite;

    [Header("旋转动画（可选）")]
    [Tooltip("是否使用旋转动画表示开关")]
    public bool useRotationAnimation = false;

    [Tooltip("关闭状态的旋转角度")]
    public float offRotation = 0f;

    [Tooltip("打开状态的旋转角度")]
    public float onRotation = 90f;

    [Tooltip("旋转动画时间")]
    public float rotationDuration = 0.2f;

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private bool isAnimating = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

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
            Debug.LogError("[FaucetHandle] 未找到 FaucetWaterSystem！");
            return;
        }

        // 初始化外观
        UpdateAppearance(faucetSystem.IsFaucetOn, false);

        // 订阅事件
        faucetSystem.OnFaucetTurnOn.AddListener(OnFaucetOn);
        faucetSystem.OnFaucetTurnOff.AddListener(OnFaucetOff);
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (faucetSystem != null)
        {
            faucetSystem.OnFaucetTurnOn.RemoveListener(OnFaucetOn);
            faucetSystem.OnFaucetTurnOff.RemoveListener(OnFaucetOff);
        }
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
            Debug.LogError("[FaucetHandle] FaucetWaterSystem 未设置！");
            return;
        }

        Debug.Log("[FaucetHandle] 点击水龙头");
        faucetSystem.ToggleFaucet();
    }

    /// <summary>
    /// 水龙头打开事件回调
    /// </summary>
    private void OnFaucetOn()
    {
        UpdateAppearance(true, true);
    }

    /// <summary>
    /// 水龙头关闭事件回调
    /// </summary>
    private void OnFaucetOff()
    {
        UpdateAppearance(false, true);
    }

    /// <summary>
    /// 更新水龙头外观
    /// </summary>
    private void UpdateAppearance(bool isOn, bool animate)
    {
        // 更新精灵图
        if (spriteRenderer != null)
        {
            Sprite targetSprite = isOn ? faucetOnSprite : faucetOffSprite;
            if (targetSprite != null)
            {
                spriteRenderer.sprite = targetSprite;
            }
        }

        // 旋转动画
        if (useRotationAnimation)
        {
            float targetRotation = isOn ? onRotation : offRotation;

            if (animate)
            {
                StartCoroutine(AnimateRotation(targetRotation));
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0, 0, targetRotation);
            }
        }
    }

    /// <summary>
    /// 旋转动画
    /// </summary>
    private System.Collections.IEnumerator AnimateRotation(float targetAngle)
    {
        isAnimating = true;

        float startAngle = transform.localRotation.eulerAngles.z;
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / rotationDuration);
            float currentAngle = Mathf.LerpAngle(startAngle, targetAngle, t);
            transform.localRotation = Quaternion.Euler(0, 0, currentAngle);
            yield return null;
        }

        transform.localRotation = Quaternion.Euler(0, 0, targetAngle);
        isAnimating = false;
    }
}