// Assets/Scripts/GamePlay/CounterTop.cs
using UnityEngine;

/// <summary>
/// 台面 - 选中空烧杯后点击可放置
/// 
/// 挂载到台面物体上，需要 Collider2D 用于点击检测
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CounterTop : MonoBehaviour
{
    [Header("系统引用")]
    [Tooltip("关联的水龙头系统（留空则自动查找）")]
    public FaucetWaterSystem faucetSystem;

    [Header("视觉反馈")]
    [Tooltip("鼠标悬停时的颜色变化（可选）")]
    public bool useHoverEffect = false;

    [Tooltip("悬停时的颜色")]
    public Color hoverColor = new Color(1f, 1f, 1f, 0.8f);

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

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
            Debug.LogError("[CounterTop] 未找到 FaucetWaterSystem！");
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

        HandleClick();
    }

    private void OnMouseEnter()
    {
        if (useHoverEffect && spriteRenderer != null)
        {
            // 检查是否选中了空烧杯
            if (IsHoldingEmptyBeaker())
            {
                spriteRenderer.color = hoverColor;
            }
        }
    }

    private void OnMouseExit()
    {
        if (useHoverEffect && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// 处理点击
    /// </summary>
    private void HandleClick()
    {
        if (faucetSystem == null)
        {
            Debug.LogError("[CounterTop] FaucetWaterSystem 未设置！");
            return;
        }

        Debug.Log("[CounterTop] 点击台面");

        // 尝试放置烧杯
        bool success = faucetSystem.TryPlaceBeaker();

        if (success)
        {
            Debug.Log("[CounterTop] 烧杯放置成功");
        }
    }

    /// <summary>
    /// 检查玩家是否手持空烧杯
    /// </summary>
    private bool IsHoldingEmptyBeaker()
    {
        if (UIManager.Instance == null) return false;
        if (!UIManager.Instance.HasSelectedItem()) return false;
        if (faucetSystem == null || faucetSystem.emptyBeakerItem == null) return false;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        return selectedItem != null && selectedItem.itemID == faucetSystem.emptyBeakerItem.itemID;
    }
}