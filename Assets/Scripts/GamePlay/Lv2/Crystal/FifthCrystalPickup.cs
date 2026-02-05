// Assets/Scripts/GamePlay/FifthCrystalPickup.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 第五水晶拾取组件 - 处理点击拾取逻辑
/// 挂载到第五水晶物体上
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FifthCrystalPickup : MonoBehaviour
{
    [Header("关联")]
    [Tooltip("父级谜题控制器")]
    public CrystalPlacementPuzzle parentPuzzle;

    [Header("悬停效果")]
    [Tooltip("是否启用悬停缩放")]
    public bool enableHoverScale = true;

    [Tooltip("悬停时的缩放比例")]
    public float hoverScale = 1.1f;

    [Tooltip("缩放动画速度")]
    public float scaleSpeed = 8f;

    [Header("悬停发光")]
    [Tooltip("是否启用悬停发光增强")]
    public bool enableHoverGlow = true;

    [Tooltip("悬停时的发光增强")]
    [Range(1f, 2f)]
    public float hoverGlowMultiplier = 1.3f;

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private Vector3 originalScale;
    private Color originalColor;
    private bool isHovering = false;
    private float currentScale = 1f;
    private float targetScale = 1f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 自动查找父级谜题
        if (parentPuzzle == null)
        {
            parentPuzzle = GetComponentInParent<CrystalPlacementPuzzle>();
        }
    }

    private void Start()
    {
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    private void Update()
    {
        // 平滑缩放动画
        if (enableHoverScale)
        {
            currentScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleSpeed);
            transform.localScale = originalScale * currentScale;
        }
    }

    private void OnMouseEnter()
    {
        isHovering = true;
        targetScale = hoverScale;

        // 增强发光
        if (enableHoverGlow && spriteRenderer != null)
        {
            Color glowColor = new Color(
                originalColor.r * hoverGlowMultiplier,
                originalColor.g * hoverGlowMultiplier,
                originalColor.b * hoverGlowMultiplier,
                originalColor.a
            );
            spriteRenderer.color = glowColor;
        }

        // 改变鼠标光标（可选）
        // Cursor.SetCursor(...);
    }

    private void OnMouseExit()
    {
        isHovering = false;
        targetScale = 1f;

        // 恢复颜色（注意：可能被父级的脉冲动画覆盖）
        // 这里只在不脉冲时恢复
        if (enableHoverGlow && spriteRenderer != null)
        {
            // 不直接恢复，让父级的脉冲动画继续控制
        }
    }

    private void OnMouseDown()
    {
        if (parentPuzzle != null)
        {
            // 点击缩小反馈
            StartCoroutine(ClickFeedbackCoroutine());

            // 尝试拾取
            parentPuzzle.TryPickupFifthCrystal();
        }
        else
        {
            Debug.LogWarning("[FifthCrystalPickup] 未关联父级谜题！");
        }
    }

    /// <summary>
    /// 点击反馈动画
    /// </summary>
    private IEnumerator ClickFeedbackCoroutine()
    {
        // 快速缩小
        float clickScale = 0.9f;
        transform.localScale = originalScale * clickScale;

        yield return new WaitForSeconds(0.05f);

        // 恢复（如果还存在）
        if (this != null && gameObject.activeInHierarchy)
        {
            targetScale = isHovering ? hoverScale : 1f;
        }
    }

    private void OnDisable()
    {
        // 重置状态
        isHovering = false;
        currentScale = 1f;
        targetScale = 1f;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}