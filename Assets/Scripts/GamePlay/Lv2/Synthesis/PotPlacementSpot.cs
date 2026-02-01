// Assets/Scripts/GamePlay/Synthesis/PotPlacementSpot.cs
// 陶罐放置点 - 用于玩家将空陶罐从背包放回桌面
using UnityEngine;
using UnityEngine.Events;

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

    [Tooltip("可放置时的高亮颜色")]
    public Color highlightColor = new Color(0.7f, 1f, 0.7f, 0.5f);

    [Tooltip("正常颜色")]
    public Color normalColor = new Color(1f, 1f, 1f, 0f);

    [Tooltip("是否显示放置提示（当选中空陶罐时）")]
    public bool showPlacementHint = true;

    [Header("提示文本")]
    public string noItemHint = "需要选中空陶罐";
    public string wrongItemHint = "只能放置空陶罐";
    public string potAlreadyOnTableHint = "陶罐已经在桌上了";

    [Header("音效")]
    public string placeSFX = "Audio/SFX/item_place";

    [Header("事件")]
    public UnityEvent OnPotPlaced;

    // 缓存
    private Color originalColor;
    private bool isHighlighting = false;

    private void Awake()
    {
        if (spotRenderer != null)
        {
            originalColor = spotRenderer.color;
        }
    }

    private void Update()
    {
        // 检测是否选中了空陶罐，动态显示高亮
        if (showPlacementHint && spotRenderer != null)
        {
            bool shouldHighlight = ShouldShowPlacementHint();

            if (shouldHighlight && !isHighlighting)
            {
                spotRenderer.color = highlightColor;
                isHighlighting = true;
            }
            else if (!shouldHighlight && isHighlighting)
            {
                spotRenderer.color = normalColor;
                isHighlighting = false;
            }
        }
    }

    private bool ShouldShowPlacementHint()
    {
        if (UIManager.Instance == null) return false;
        if (potController == null) return false;

        // 如果陶罐已经在桌上，不显示高亮
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

    private void OnMouseEnter()
    {
        if (spotRenderer != null && ShouldShowPlacementHint())
        {
            spotRenderer.color = highlightColor * 1.2f; // 更亮的高亮
        }
    }

    private void OnMouseExit()
    {
        if (spotRenderer != null)
        {
            if (isHighlighting)
            {
                spotRenderer.color = highlightColor;
            }
            else
            {
                spotRenderer.color = normalColor;
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

        // 检查陶罐是否已在桌上
        if (potController.gameObject.activeSelf &&
            (potController.CurrentState == PotController.PotState.OnTable_Empty ||
             potController.CurrentState == PotController.PotState.OnTable_Filling ||
             potController.CurrentState == PotController.PotState.OnTable_Ready))
        {
            ShowHint(potAlreadyOnTableHint);
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("[PotPlacementSpot] UIManager.Instance 为空！");
            return;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        if (selectedItem == null)
        {
            ShowHint(noItemHint);
            return;
        }

        // 检查是否是空陶罐
        if (emptyPotItem == null || selectedItem.itemID != emptyPotItem.itemID)
        {
            ShowHint(wrongItemHint);
            return;
        }

        // 放置陶罐
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