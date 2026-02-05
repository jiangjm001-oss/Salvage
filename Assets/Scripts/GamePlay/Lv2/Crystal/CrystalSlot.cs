// Assets/Scripts/GamePlay/CrystalSlot.cs
using UnityEngine;
using System.Collections;

/// <summary>
/// 水晶放置槽位 - 处理单个槽位的交互和视觉反馈
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CrystalSlot : MonoBehaviour
{
    [Header("槽位设置")]
    [Tooltip("此槽位需要的水晶碎片物品ID")]
    public string requiredItemID;

    [Tooltip("放置后显示的水晶精灵")]
    public Sprite placedCrystalSprite;

    [Header("视觉组件")]
    [Tooltip("槽位背景Renderer（用于高亮效果）")]
    public SpriteRenderer slotRenderer;

    [Tooltip("放置后的水晶Renderer")]
    public SpriteRenderer crystalRenderer;

    [Header("悬停高亮效果")]
    [Tooltip("正常状态颜色")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.3f);

    [Tooltip("悬停时颜色（选中正确物品）")]
    public Color hoverValidColor = new Color(0.5f, 1f, 0.5f, 0.6f);

    [Tooltip("悬停时颜色（未选中或错误物品）")]
    public Color hoverInvalidColor = new Color(1f, 1f, 1f, 0.5f);

    [Tooltip("已放置后颜色")]
    public Color placedColor = new Color(1f, 1f, 1f, 0f);

    [Header("放置动画")]
    [Tooltip("放置动画持续时间")]
    public float placeAnimDuration = 0.5f;

    [Tooltip("放置时的发光颜色")]
    public Color placeGlowColor = new Color(0.7f, 0.9f, 1f, 1f);

    [Tooltip("发光强度")]
    [Range(1f, 4f)]
    public float placeGlowIntensity = 2f;

    [Tooltip("放置时的缩放效果（从小变大）")]
    public float placeStartScale = 0.3f;

    [Tooltip("放置时的旋转效果（度数）")]
    public float placeRotation = 15f;

    [Tooltip("放置动画曲线")]
    public AnimationCurve placeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("发光脉冲（放置后）")]
    [Tooltip("放置后是否有短暂脉冲")]
    public bool pulseAfterPlace = true;

    [Tooltip("脉冲次数")]
    public int pulseCount = 2;

    [Tooltip("单次脉冲时间")]
    public float pulseDuration = 0.3f;

    [Header("状态（只读）")]
    [SerializeField] private bool isPlaced = false;
    [SerializeField] private bool isHovering = false;

    // 属性访问
    public bool IsPlaced => isPlaced;
    public string RequiredItemID => requiredItemID;

    // 私有变量
    private CrystalPlacementPuzzle parentPuzzle;
    private int slotIndex;
    private Collider2D slotCollider;
    private Color originalCrystalColor;
    private bool isAnimating = false;

    private void Awake()
    {
        slotCollider = GetComponent<Collider2D>();

        // 自动获取Renderer
        if (slotRenderer == null)
        {
            slotRenderer = GetComponent<SpriteRenderer>();
        }

        // 如果没有单独的crystalRenderer，在子物体中查找或创建
        if (crystalRenderer == null)
        {
            Transform crystalChild = transform.Find("Crystal");
            if (crystalChild != null)
            {
                crystalRenderer = crystalChild.GetComponent<SpriteRenderer>();
            }
        }

        // 保存原始颜色
        if (crystalRenderer != null)
        {
            originalCrystalColor = crystalRenderer.color;
        }
    }

    private void Start()
    {
        // 初始化显示状态
        UpdateVisual();
    }

    /// <summary>
    /// 初始化（由父谜题调用）
    /// </summary>
    public void Initialize(CrystalPlacementPuzzle puzzle, int index)
    {
        parentPuzzle = puzzle;
        slotIndex = index;

        Debug.Log($"[CrystalSlot] 槽位 {index} 初始化，需要物品: {requiredItemID}");
    }

    private void OnMouseEnter()
    {
        if (isPlaced || isAnimating) return;

        isHovering = true;
        UpdateHoverVisual();
    }

    private void OnMouseExit()
    {
        if (isPlaced) return;

        isHovering = false;
        UpdateHoverVisual();
    }

    private void OnMouseDown()
    {
        if (isPlaced || isAnimating) return;

        TryPlaceCrystal();
    }

    /// <summary>
    /// 尝试放置水晶
    /// </summary>
    private void TryPlaceCrystal()
    {
        // 检查是否选中了物品
        if (UIManager.Instance == null || !UIManager.Instance.HasSelectedItem())
        {
            Debug.Log($"[CrystalSlot] 槽位 {slotIndex}: 未选中任何物品");
            ShowInvalidFeedback();
            return;
        }

        // 获取选中的物品
        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log($"[CrystalSlot] 槽位 {slotIndex}: 选中物品为空");
            return;
        }

        // 检查是否是正确的水晶碎片
        if (selectedItem.itemID != requiredItemID)
        {
            Debug.Log($"[CrystalSlot] 槽位 {slotIndex}: 物品不匹配 (需要: {requiredItemID}, 实际: {selectedItem.itemID})");
            ShowInvalidFeedback();
            return;
        }

        // 放置成功
        Debug.Log($"[CrystalSlot] ✓ 槽位 {slotIndex}: 放置 {selectedItem.displayName}");

        // 消耗物品
        UIManager.Instance.ConsumeSelectedItem();

        // 执行放置
        PlaceCrystal();
    }

    /// <summary>
    /// 执行放置逻辑
    /// </summary>
    private void PlaceCrystal()
    {
        isPlaced = true;
        isHovering = false;

        // 开始放置动画
        StartCoroutine(PlaceAnimationCoroutine());

        // 通知父谜题
        if (parentPuzzle != null)
        {
            parentPuzzle.OnSlotPlaced(slotIndex);
        }
    }

    /// <summary>
    /// 放置动画协程
    /// </summary>
    private IEnumerator PlaceAnimationCoroutine()
    {
        isAnimating = true;

        // 显示水晶
        if (crystalRenderer != null)
        {
            crystalRenderer.gameObject.SetActive(true);

            if (placedCrystalSprite != null)
            {
                crystalRenderer.sprite = placedCrystalSprite;
            }

            // 初始状态：小尺寸、发光、略微旋转
            crystalRenderer.transform.localScale = Vector3.one * placeStartScale;
            crystalRenderer.transform.localRotation = Quaternion.Euler(0, 0, placeRotation);
            crystalRenderer.color = placeGlowColor * placeGlowIntensity;
        }

        // 隐藏槽位背景
        if (slotRenderer != null)
        {
            slotRenderer.color = placedColor;
        }

        float elapsed = 0f;

        while (elapsed < placeAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = placeCurve.Evaluate(elapsed / placeAnimDuration);

            if (crystalRenderer != null)
            {
                // 缩放：从小到正常
                float scale = Mathf.Lerp(placeStartScale, 1f, t);
                crystalRenderer.transform.localScale = Vector3.one * scale;

                // 旋转：从偏转回到0
                float rotation = Mathf.Lerp(placeRotation, 0f, t);
                crystalRenderer.transform.localRotation = Quaternion.Euler(0, 0, rotation);

                // 颜色：从发光渐变到正常
                Color currentColor = Color.Lerp(
                    placeGlowColor * placeGlowIntensity,
                    originalCrystalColor,
                    t
                );
                crystalRenderer.color = currentColor;
            }

            yield return null;
        }

        // 确保最终状态
        if (crystalRenderer != null)
        {
            crystalRenderer.transform.localScale = Vector3.one;
            crystalRenderer.transform.localRotation = Quaternion.identity;
            crystalRenderer.color = originalCrystalColor;
        }

        // 放置后脉冲效果
        if (pulseAfterPlace && crystalRenderer != null)
        {
            yield return StartCoroutine(PulseEffectCoroutine());
        }

        isAnimating = false;
    }

    /// <summary>
    /// 脉冲发光效果
    /// </summary>
    private IEnumerator PulseEffectCoroutine()
    {
        for (int i = 0; i < pulseCount; i++)
        {
            // 变亮
            float elapsed = 0f;
            while (elapsed < pulseDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration * 0.5f);

                Color color = Color.Lerp(originalCrystalColor, placeGlowColor, t * 0.5f);
                crystalRenderer.color = color;

                yield return null;
            }

            // 变暗
            elapsed = 0f;
            while (elapsed < pulseDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / (pulseDuration * 0.5f);

                Color color = Color.Lerp(placeGlowColor, originalCrystalColor, t * 2f);
                if (t > 0.5f) color = originalCrystalColor;
                crystalRenderer.color = color;

                yield return null;
            }
        }

        crystalRenderer.color = originalCrystalColor;
    }

    /// <summary>
    /// 显示无效操作反馈
    /// </summary>
    private void ShowInvalidFeedback()
    {
        StartCoroutine(InvalidShakeCoroutine());
    }

    /// <summary>
    /// 无效操作摇晃反馈
    /// </summary>
    private IEnumerator InvalidShakeCoroutine()
    {
        if (slotRenderer == null) yield break;

        Color flashColor = new Color(1f, 0.5f, 0.5f, 0.8f);
        Color originalColor = isHovering ? hoverInvalidColor : normalColor;

        // 闪烁红色
        for (int i = 0; i < 2; i++)
        {
            slotRenderer.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            slotRenderer.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }

        UpdateHoverVisual();
    }

    /// <summary>
    /// 更新悬停视觉效果
    /// </summary>
    private void UpdateHoverVisual()
    {
        if (slotRenderer == null || isPlaced) return;

        if (isHovering)
        {
            // 检查是否选中了正确物品
            bool hasValidItem = false;
            if (UIManager.Instance != null && UIManager.Instance.HasSelectedItem())
            {
                ItemData selected = UIManager.Instance.GetSelectedItem();
                hasValidItem = selected != null && selected.itemID == requiredItemID;
            }

            slotRenderer.color = hasValidItem ? hoverValidColor : hoverInvalidColor;
        }
        else
        {
            slotRenderer.color = normalColor;
        }
    }

    /// <summary>
    /// 更新整体视觉状态
    /// </summary>
    private void UpdateVisual()
    {
        if (isPlaced)
        {
            // 已放置状态
            if (slotRenderer != null)
            {
                slotRenderer.color = placedColor;
            }

            if (crystalRenderer != null)
            {
                crystalRenderer.gameObject.SetActive(true);
                if (placedCrystalSprite != null)
                {
                    crystalRenderer.sprite = placedCrystalSprite;
                }
            }
        }
        else
        {
            // 未放置状态
            if (slotRenderer != null)
            {
                slotRenderer.color = normalColor;
            }

            if (crystalRenderer != null)
            {
                crystalRenderer.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 恢复已放置状态（用于读档）
    /// </summary>
    public void RestorePlaced()
    {
        isPlaced = true;
        isHovering = false;
        UpdateVisual();

        Debug.Log($"[CrystalSlot] 槽位 {slotIndex} 状态已恢复（已放置）");
    }

    /// <summary>
    /// 重置槽位（用于调试）
    /// </summary>
    public void ResetSlot()
    {
        StopAllCoroutines();

        isPlaced = false;
        isHovering = false;
        isAnimating = false;

        if (crystalRenderer != null)
        {
            crystalRenderer.gameObject.SetActive(false);
            crystalRenderer.transform.localScale = Vector3.one;
            crystalRenderer.transform.localRotation = Quaternion.identity;
            crystalRenderer.color = originalCrystalColor;
        }

        UpdateVisual();

        Debug.Log($"[CrystalSlot] 槽位 {slotIndex} 已重置");
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        // 自动设置normalColor透明度
        if (normalColor.a > 0.5f)
        {
            normalColor.a = 0.3f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制槽位范围
        Gizmos.color = isPlaced ? Color.green : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.25f);

        // 显示需要的物品ID
#if UNITY_EDITOR
        UnityEditor.Handles.Label(
            transform.position + Vector3.up * 0.4f,
            $"需要: {requiredItemID}"
        );
#endif
    }
}