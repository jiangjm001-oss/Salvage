// Assets/Scripts/GamePlay/CoffeeCupController.cs
using UnityEngine;

/// <summary>
/// 咖啡杯控制器 - 多阶段状态机（带外部条件依赖）
/// 
/// 阶段流程：
/// A(empty) --[咖啡粉]--> B(powder) --[热水]--> C(water) --[牛奶]--> D(milk) --[滤纸B存在]--> 拾取
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class CoffeeCupController : MonoBehaviour
{
    public enum CupState
    {
        Empty,      // A - 空杯，需要咖啡粉
        Powder,     // B - 有咖啡粉，需要热水
        Water,      // C - 加了热水，需要牛奶
        Milk,       // D - 完成，需要滤纸B才能拾取
        Collected   // 已拾取
    }

    [Header("基本信息")]
    public string objectID = "coffee_cup";
    public string displayName = "咖啡杯";

    [Header("当前状态")]
    public CupState currentState = CupState.Empty;

    [Header("精灵图设置")]
    public Sprite spriteEmpty;
    public Sprite spritePowder;
    public Sprite spriteWater;
    public Sprite spriteMilk;

    [Header("阶段物品设置")]
    [Tooltip("A→B 需要的物品（咖啡粉）")]
    public ItemData itemForPowder;

    [Tooltip("B→C 需要的物品（热水）")]
    public ItemData itemForWater;

    [Tooltip("C→D 需要的物品（牛奶）")]
    public ItemData itemForMilk;

    [Header("拾取设置")]
    [Tooltip("拾取时获得的物品（咖啡杯D）")]
    public ItemData pickupItem;

    [Tooltip("拾取条件：需要此物体激活（滤纸B）")]
    public GameObject requiredObjectForPickup;

    [Tooltip("拾取条件不满足时的提示")]
    public string pickupBlockedHint = "好像还需要做点什么...";

    [Header("触发设置")]
    [Tooltip("C→D 时显示的物体（滤纸A）")]
    public GameObject objectToShowOnMilk;

    [Header("音效设置")]
    public string pourSoundName = "";
    public string collectSoundName = "Audio/SFX/item_pickup";

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Interact();
    }

    public void Interact()
    {
        Debug.Log($"[CoffeeCupController] 交互，当前状态: {currentState}");

        switch (currentState)
        {
            case CupState.Empty:
                TryAddItem(itemForPowder, CupState.Powder, "咖啡粉");
                break;

            case CupState.Powder:
                TryAddItem(itemForWater, CupState.Water, "热水");
                break;

            case CupState.Water:
                TryAddMilk();
                break;

            case CupState.Milk:
                TryCollect();
                break;

            case CupState.Collected:
                Debug.Log("[CoffeeCupController] 咖啡杯已被拾取");
                break;
        }
    }

    /// <summary>
    /// 通用：尝试添加物品并切换状态
    /// </summary>
    private void TryAddItem(ItemData requiredItem, CupState nextState, string itemName)
    {
        if (requiredItem == null)
        {
            Debug.LogError($"[CoffeeCupController] 未设置 {itemName} 的 ItemData!");
            return;
        }

        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log($"[CoffeeCupController] 需要选中{itemName}");
            return;
        }

        if (selectedItem.itemID != requiredItem.itemID)
        {
            Debug.Log($"[CoffeeCupController] 需要{itemName}，不是 {selectedItem.displayName}");
            return;
        }

        // 消耗物品
        UIManager.Instance.ConsumeSelectedItem();

        // 播放音效
        PlaySound(pourSoundName);

        // 切换状态
        SetState(nextState);
        Debug.Log($"[CoffeeCupController] 添加{itemName}成功 → {nextState}");
    }

    /// <summary>
    /// 特殊：添加牛奶（同时触发滤纸出现）
    /// </summary>
    private void TryAddMilk()
    {
        if (itemForMilk == null)
        {
            Debug.LogError("[CoffeeCupController] 未设置牛奶的 ItemData!");
            return;
        }

        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log("[CoffeeCupController] 需要选中牛奶");
            return;
        }

        if (selectedItem.itemID != itemForMilk.itemID)
        {
            Debug.Log($"[CoffeeCupController] 需要牛奶，不是 {selectedItem.displayName}");
            return;
        }

        // 消耗物品
        UIManager.Instance.ConsumeSelectedItem();

        // 播放音效
        PlaySound(pourSoundName);

        // 切换状态
        SetState(CupState.Milk);

        // ⭐ 显示滤纸A
        if (objectToShowOnMilk != null)
        {
            objectToShowOnMilk.SetActive(true);
            Debug.Log($"[CoffeeCupController] 显示: {objectToShowOnMilk.name}");
        }

        Debug.Log("[CoffeeCupController] 添加牛奶成功 → Milk，滤纸出现");
    }

    /// <summary>
    /// 尝试拾取咖啡杯
    /// </summary>
    private void TryCollect()
    {
        // 检查外部条件（滤纸B是否激活）
        if (requiredObjectForPickup != null && !requiredObjectForPickup.activeInHierarchy)
        {
            Debug.Log($"[CoffeeCupController] 拾取条件不满足: {requiredObjectForPickup.name} 未激活");
            // 可选：显示提示
            // UIManager.Instance?.ShowHint(pickupBlockedHint);
            return;
        }

        if (pickupItem == null)
        {
            Debug.LogError("[CoffeeCupController] 未设置 pickupItem!");
            return;
        }

        if (InventorySystem.Instance == null) return;

        // 添加到背包
        bool added = InventorySystem.Instance.AddItem(pickupItem);
        if (!added)
        {
            Debug.Log("[CoffeeCupController] 背包已满");
            return;
        }

        // 播放音效
        PlaySound(collectSoundName);

        // 隐藏自己
        SetState(CupState.Collected);
        gameObject.SetActive(false);

        Debug.Log($"[CoffeeCupController] 拾取 {pickupItem.displayName}");
    }

    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    private void SetState(CupState newState)
    {
        currentState = newState;
        UpdateSprite();
        SaveLoadSystem.Instance?.SaveGame();
    }

    private void UpdateSprite()
    {
        if (spriteRenderer == null) return;

        Sprite targetSprite = currentState switch
        {
            CupState.Empty => spriteEmpty,
            CupState.Powder => spritePowder,
            CupState.Water => spriteWater,
            CupState.Milk => spriteMilk,
            CupState.Collected => spriteMilk,
            _ => spriteEmpty
        };

        if (targetSprite != null)
        {
            spriteRenderer.sprite = targetSprite;
        }
    }

    // ============ 存档相关 ============

    public void RestoreState(int stateIndex)
    {
        currentState = (CupState)stateIndex;
        UpdateSprite();

        if (currentState == CupState.Collected)
        {
            gameObject.SetActive(false);
        }
    }

    public int GetStateForSave()
    {
        return (int)currentState;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"coffee_cup_{GetInstanceID()}";
        }
    }
}