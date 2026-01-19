// Assets/Scripts/GamePlay/GrinderController.cs
using UnityEngine;

/// <summary>
/// 研磨器控制器 - 多阶段状态机
/// 
/// 阶段流程：
/// A(empty) --[需要咖啡豆]--> B(beans) --[点击]--> C(powder) --[点击+拾取]--> D(empty)
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class GrinderController : MonoBehaviour
{
    public enum GrinderState
    {
        Empty,      // A - 空的，需要咖啡豆
        Beans,      // B - 有咖啡豆，可研磨
        Powder,     // C - 有咖啡粉，可拾取
        Used        // D - 用完了
    }

    [Header("基本信息")]
    public string objectID = "grinder";
    public string displayName = "研磨器";

    [Header("当前状态")]
    public GrinderState currentState = GrinderState.Empty;

    [Header("精灵图设置")]
    [Tooltip("A - 空研磨器")]
    public Sprite spriteEmpty;
    [Tooltip("B - 装有咖啡豆")]
    public Sprite spriteBeans;
    [Tooltip("C - 研磨后的咖啡粉")]
    public Sprite spritePowder;
    [Tooltip("D - 取走咖啡粉后")]
    public Sprite spriteUsed;

    [Header("物品设置")]
    [Tooltip("A→B 需要的物品（咖啡豆）")]
    public ItemData requiredItem;
    [Tooltip("是否消耗物品")]
    public bool consumeRequiredItem = true;

    [Tooltip("C→D 拾取的物品（咖啡粉）")]
    public ItemData outputItem;

    [Header("音效设置")]
    public string addBeansSoundName = "";
    public string grindSoundName = "";
    public string collectSoundName = "Audio/SFX/item_pickup";

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSprite();
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
        Debug.Log($"[GrinderController] 交互，当前状态: {currentState}");

        switch (currentState)
        {
            case GrinderState.Empty:
                TryAddBeans();
                break;

            case GrinderState.Beans:
                Grind();
                break;

            case GrinderState.Powder:
                CollectPowder();
                break;

            case GrinderState.Used:
                Debug.Log("[GrinderController] 研磨器已用完");
                break;
        }
    }

    /// <summary>
    /// A→B：尝试添加咖啡豆
    /// </summary>
    private void TryAddBeans()
    {
        if (requiredItem == null)
        {
            Debug.LogError("[GrinderController] 未设置 requiredItem!");
            return;
        }

        if (UIManager.Instance == null) return;

        // 检查是否选中了正确的物品
        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log("[GrinderController] 需要选中咖啡豆");
            return;
        }

        if (selectedItem.itemID != requiredItem.itemID)
        {
            Debug.Log("[GrinderController] 这个物品不对");
            return;
        }

        // 消耗物品
        if (consumeRequiredItem)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(addBeansSoundName))
        {
            AudioManager.Instance.PlaySFX(addBeansSoundName);
        }

        // 切换状态
        SetState(GrinderState.Beans);
        Debug.Log("[GrinderController] 添加咖啡豆成功 → Beans");
    }

    /// <summary>
    /// B→C：研磨
    /// </summary>
    private void Grind()
    {
        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(grindSoundName))
        {
            AudioManager.Instance.PlaySFX(grindSoundName);
        }

        // 切换状态
        SetState(GrinderState.Powder);
        Debug.Log("[GrinderController] 研磨完成 → Powder");
    }

    /// <summary>
    /// C→D：收集咖啡粉
    /// </summary>
    private void CollectPowder()
    {
        if (outputItem == null)
        {
            Debug.LogError("[GrinderController] 未设置 outputItem!");
            SetState(GrinderState.Used);
            return;
        }

        if (InventorySystem.Instance == null) return;

        // 添加到背包
        bool added = InventorySystem.Instance.AddItem(outputItem);
        if (!added)
        {
            Debug.Log("[GrinderController] 背包已满");
            return;
        }

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(collectSoundName))
        {
            AudioManager.Instance.PlaySFX(collectSoundName);
        }

        // 切换状态
        SetState(GrinderState.Used);
        Debug.Log($"[GrinderController] 拾取 {outputItem.displayName} → Used");
    }

    /// <summary>
    /// 设置状态并更新显示
    /// </summary>
    private void SetState(GrinderState newState)
    {
        currentState = newState;
        UpdateSprite();
        SaveLoadSystem.Instance?.SaveGame();
    }

    /// <summary>
    /// 根据状态更新精灵图
    /// </summary>
    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        Sprite targetSprite = currentState switch
        {
            GrinderState.Empty => spriteEmpty,
            GrinderState.Beans => spriteBeans,
            GrinderState.Powder => spritePowder,
            GrinderState.Used => spriteUsed,
            _ => spriteEmpty
        };

        if (targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;
        }
    }

    /// <summary>
    /// 存档恢复
    /// </summary>
    public void RestoreState(int stateIndex)
    {
        currentState = (GrinderState)stateIndex;
        UpdateSprite();
        Debug.Log($"[GrinderController] 恢复状态: {currentState}");
    }

    /// <summary>
    /// 获取状态用于存档
    /// </summary>
    public int GetStateForSave()
    {
        return (int)currentState;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"grinder_{GetInstanceID()}";
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 显示当前状态
        Gizmos.color = currentState switch
        {
            GrinderState.Empty => Color.gray,
            GrinderState.Beans => new Color(0.6f, 0.4f, 0.2f), // 棕色
            GrinderState.Powder => new Color(0.4f, 0.2f, 0.1f), // 深棕
            GrinderState.Used => Color.white,
            _ => Color.gray
        };
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}