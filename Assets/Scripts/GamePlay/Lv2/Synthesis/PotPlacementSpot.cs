// Assets/Scripts/GamePlay/Synthesis/PotPlacementSpot.cs
// 陶罐放置点 - 用于玩家将空陶罐从背包放回桌面
// 优化版：添加平滑颜色过渡
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 陶罐放置点
/// 玩家选中背包中的空陶罐，点击此放置点，陶罐会被放回桌面
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PotPlacementSpot : MonoBehaviour
{
    [Header("基本信息")]
    [Tooltip("物体唯一ID")]
    public string objectID = "pot_placement_spot";

    [Tooltip("显示名称")]
    public string displayName = "桌面";

    [Header("组件引用")]
    [Tooltip("场景中的陶罐控制器")]
    public PotController potController;

    [Header("物品配置")]
    [Tooltip("空陶罐物品数据")]
    public ItemData emptyPotItem;

    [Header("视觉反馈")]
    [Tooltip("放置点的 SpriteRenderer（可选）")]
    public SpriteRenderer spotRenderer;

    [Tooltip("正常颜色（通常透明）")]
    public Color normalColor = new Color(1f, 1f, 1f, 0f);

    [Tooltip("可放置时的提示颜色")]
    public Color canPlaceColor = new Color(0.5f, 1f, 0.5f, 0.3f);

    [Tooltip("鼠标悬停时的高亮颜色")]
    public Color hoverColor = new Color(0.7f, 1f, 0.7f, 0.5f);

    [Tooltip("颜色过渡时间（秒）")]
    public float colorTransitionDuration = 0.15f;

    [Tooltip("是否显示放置提示（当选中空陶罐时）")]
    public bool showPlacementHint = true;

    [Header("提示文本")]
    public string noItemHint = "需要选中空陶罐";
    public string wrongItemHint = "只能放置空陶罐";
    public string potAlreadyOnTableHint = "陶罐已经在桌上了";

    [Header("音效")]
    public string placeSFX = "Audio/SFX/item_place";
    public string errorSFX = "Audio/SFX/error";

    [Header("事件")]
    public UnityEvent OnPotPlaced;

    // 缓存
    private Coroutine colorCoroutine;
    private Collider2D spotCollider;
    private bool isHovering = false;
    private bool wasShowingHint = false;

    private void Awake()
    {
        spotCollider = GetComponent<Collider2D>();

        if (spotRenderer != null)
        {
            // originalColor 移到这里初始化
        }
    }

    private void Update()
    {
        // 核心逻辑：当陶罐在桌上时，禁用 Collider，让点击穿透到陶罐
        UpdateColliderState();

        // 检测是否选中了空陶罐，动态显示提示
        if (showPlacementHint && spotRenderer != null)
        {
            bool shouldShowHint = ShouldShowPlacementHint();

            if (shouldShowHint && !wasShowingHint)
            {
                // 开始显示提示
                TransitionToColor(isHovering ? hoverColor : canPlaceColor);
                wasShowingHint = true;
            }
            else if (!shouldShowHint && wasShowingHint)
            {
                // 停止显示提示
                TransitionToColor(normalColor);
                wasShowingHint = false;
            }
        }
    }

    /// <summary>
    /// 根据陶罐状态更新 Collider 启用状态
    /// 当陶罐在桌上时禁用，让点击事件穿透到陶罐
    /// </summary>
    private void UpdateColliderState()
    {
        if (spotCollider == null || potController == null) return;

        bool potOnTable = potController.gameObject.activeSelf &&
            (potController.CurrentState == PotController.PotState.OnTable_Empty ||
             potController.CurrentState == PotController.PotState.OnTable_Filling ||
             potController.CurrentState == PotController.PotState.OnTable_Ready);

        // 陶罐在桌上时禁用 Collider，否则启用
        bool shouldEnable = !potOnTable;

        if (spotCollider.enabled != shouldEnable)
        {
            spotCollider.enabled = shouldEnable;
            Debug.Log($"[PotPlacementSpot] Collider {(shouldEnable ? "启用" : "禁用")} (陶罐在桌上: {potOnTable})");

            // 如果禁用了 Collider，重置视觉状态
            if (!shouldEnable)
            {
                isHovering = false;
                wasShowingHint = false;
                if (spotRenderer != null)
                {
                    TransitionToColor(normalColor);
                }
            }
        }
    }

    private bool ShouldShowPlacementHint()
    {
        if (UIManager.Instance == null) return false;
        if (potController == null) return false;

        // 如果陶罐已经在桌上，不显示提示
        if (potController.gameObject.activeSelf &&
            (potController.CurrentState == PotController.PotState.OnTable_Empty ||
             potController.CurrentState == PotController.PotState.OnTable_Filling ||
             potController.CurrentState == PotController.PotState.OnTable_Ready))
        {
            return false;
        }

        // 检查是否选中了空陶罐
        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem != null && emptyPotItem != null && selectedItem.itemID == emptyPotItem.itemID)
        {
            return true;
        }

        return false;
    }

    // ============ 颜色过渡系统 ============

    private void TransitionToColor(Color targetColor)
    {
        if (spotRenderer == null) return;

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }

        colorCoroutine = StartCoroutine(ColorTransitionCoroutine(targetColor));
    }

    private IEnumerator ColorTransitionCoroutine(Color targetColor)
    {
        Color startColor = spotRenderer.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = SmoothStep(elapsed / colorTransitionDuration);
            spotRenderer.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        spotRenderer.color = targetColor;
        colorCoroutine = null;
    }

    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    // ============ 鼠标事件 ============

    private void OnMouseEnter()
    {
        isHovering = true;

        if (spotRenderer != null && ShouldShowPlacementHint())
        {
            TransitionToColor(hoverColor);
        }
    }

    private void OnMouseExit()
    {
        isHovering = false;

        if (spotRenderer != null)
        {
            if (wasShowingHint)
            {
                TransitionToColor(canPlaceColor);
            }
            else
            {
                TransitionToColor(normalColor);
            }
        }
    }

    private void OnMouseDown()
    {
        // 检查是否点击在UI上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Interact();
    }

    public void Interact()
    {
        Debug.Log("[PotPlacementSpot] 放置点被点击");

        if (potController == null)
        {
            Debug.LogError("[PotPlacementSpot] potController 未设置！");
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("[PotPlacementSpot] UIManager.Instance 为空！");
            return;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        // 如果没有选中物品，不做任何事（让其他物体处理点击）
        if (selectedItem == null)
        {
            Debug.Log("[PotPlacementSpot] 没有选中物品，忽略点击");
            return;
        }

        // ⭐ 严格检查：只接受空陶罐！
        if (emptyPotItem == null)
        {
            Debug.LogError("[PotPlacementSpot] emptyPotItem 未设置！");
            return;
        }

        // 如果选中的不是空陶罐，不处理（让其他物体处理）
        if (selectedItem.itemID != emptyPotItem.itemID)
        {
            Debug.Log($"[PotPlacementSpot] 选中的是 {selectedItem.itemID}，不是空陶罐 {emptyPotItem.itemID}，忽略");
            return;
        }

        // 检查陶罐是否已在桌上（安全检查，正常情况下 Collider 已禁用）
        if (potController.gameObject.activeSelf &&
            (potController.CurrentState == PotController.PotState.OnTable_Empty ||
             potController.CurrentState == PotController.PotState.OnTable_Filling ||
             potController.CurrentState == PotController.PotState.OnTable_Ready))
        {
            Debug.Log("[PotPlacementSpot] 陶罐已在桌上，静默忽略");
            return; // 静默返回，不显示提示
        }

        // 放置空陶罐
        PlacePot();
    }

    private void PlacePot()
    {
        Debug.Log("[PotPlacementSpot] 放置空陶罐到桌面");

        // 消耗背包中的陶罐
        UIManager.Instance.ConsumeSelectedItem();

        // 通知陶罐控制器
        potController.PlaceOnTable(true); // true = 空陶罐

        // 播放音效
        PlaySFX(placeSFX);

        // 隐藏放置提示
        wasShowingHint = false;
        TransitionToColor(normalColor);

        // 触发事件
        OnPotPlaced?.Invoke();

        // 保存
        SaveLoadSystem.Instance?.SaveGame();
    }

    private void PlaySFX(string sfxName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxName))
        {
            AudioManager.Instance.PlaySFX(sfxName);
        }
    }

    private void ShowHint(string hint)
    {
        Debug.Log($"[提示] {hint}");
    }
}