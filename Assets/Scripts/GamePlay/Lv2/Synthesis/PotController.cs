// Assets/Scripts/GamePlay/Lv2/Synthesis/PotController.cs
// 陶罐控制器 - 管理陶罐的物品收集、状态和交互
// v2.0 - 新增：与 LinkedWorldObject 的联动支持
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 陶罐控制器
/// 负责管理陶罐的物品收集、配方验证、拾取和放置
/// 
/// 使用流程：
/// 1. 陶罐在桌面上，玩家选中物品点击陶罐 → 物品放入陶罐
/// 2. 放入正确配方的所有物品后 → 陶罐可拾取
/// 3. 陶罐放入合成机器 → 由 SynthesisMachine 处理
/// 4. 合成完成后 → 陶罐变空，可放回桌面
/// 
/// ⭐ v2.0 新增：
/// - 通过 WorldObjectStateManager 与全景视图的 LinkedWorldObject 联动
/// - 支持精灵图状态同步
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class PotController : MonoBehaviour
{
    // ============ 状态枚举 ============
    public enum PotState
    {
        OnTable_Empty,      // 在桌上 - 空的，等待放入物品
        OnTable_Filling,    // 在桌上 - 正在填充，物品数量不足
        OnTable_Ready,      // 在桌上 - 已装满正确配方，可拾取
        InInventory_Filled, // 在背包中 - 已装满（准备放入机器）
        InInventory_Empty,  // 在背包中 - 空的（可放回桌面）
        InMachine           // 在机器中
    }

    // ============ 基本配置 ============
    [Header("基本信息")]
    [Tooltip("物体唯一ID（用于与 LinkedWorldObject 联动）")]
    public string objectID = "pot";

    [Tooltip("显示名称")]
    public string displayName = "陶罐";

    [Header("陶罐物品数据")]
    [Tooltip("陶罐对应的背包物品（空陶罐）")]
    public ItemData potItemEmpty;

    [Tooltip("陶罐对应的背包物品（装满）")]
    public ItemData potItemFilled;

    // ============ 配方系统 ============
    [Header("配方配置")]
    [Tooltip("所有可用的配方列表")]
    public PotRecipe[] availableRecipes;

    [Tooltip("已完成的配方ID列表（用于防止重复）")]
    [HideInInspector]
    public List<string> completedRecipeIDs = new List<string>();

    // ============ 精灵图 ============
    [Header("精灵图设置")]
    [Tooltip("空陶罐精灵")]
    public Sprite emptySprite;

    [Tooltip("正在填充的陶罐精灵")]
    public Sprite fillingSprite;

    [Tooltip("装满的陶罐精灵（通用）")]
    public Sprite filledSprite;

    // ============ 视觉反馈 ============
    [Header("视觉反馈 - 颜色")]
    [Tooltip("正常颜色")]
    public Color normalColor = Color.white;

    [Tooltip("鼠标悬停时的高亮颜色")]
    public Color hoverColor = new Color(1f, 1f, 0.7f, 1f);

    [Tooltip("物品放入成功时的闪烁颜色")]
    public Color successColor = new Color(0.5f, 1f, 0.5f, 1f);

    [Tooltip("物品放入失败时的闪烁颜色")]
    public Color failColor = new Color(1f, 0.5f, 0.5f, 1f);

    [Tooltip("陶罐准备就绪时的脉冲颜色")]
    public Color readyPulseColor = new Color(1f, 1f, 0.6f, 1f);

    [Header("视觉反馈 - 过渡设置")]
    [Tooltip("颜色过渡时间（秒）")]
    public float colorTransitionDuration = 0.15f;

    [Tooltip("闪烁持续时间（秒）")]
    public float flashDuration = 0.3f;

    [Tooltip("是否启用准备就绪时的脉冲效果")]
    public bool enableReadyPulse = true;

    [Tooltip("脉冲速度")]
    public float pulseSpeed = 2f;

    // ============ 提示文本 ============
    [Header("提示文本")]
    [Tooltip("未选中物品时的提示")]
    public string noItemHint = "需要选中物品才能放入陶罐";

    [Tooltip("物品不属于任何配方时的提示")]
    public string wrongItemHint = "这个东西放不进陶罐...";

    [Tooltip("配方已完成时的提示")]
    public string recipeCompletedHint = "这个配方已经完成过了";

    [Tooltip("陶罐已满时的提示")]
    public string potFullHint = "陶罐已装满，可以拾取了";

    [Tooltip("陶罐未装满时的提示")]
    public string potNotReadyHint = "还需要放入更多材料...";

    // ============ 音效 ============
    [Header("音效设置")]
    [Tooltip("放入物品音效")]
    public string addItemSFX = "Audio/SFX/item_drop";

    [Tooltip("配方完成音效")]
    public string recipeCompleteSFX = "Audio/SFX/recipe_complete";

    [Tooltip("拾取音效")]
    public string pickupSFX = "Audio/SFX/item_pickup";

    [Tooltip("放置音效")]
    public string placeSFX = "Audio/SFX/item_place";

    [Tooltip("错误音效")]
    public string errorSFX = "Audio/SFX/error";

    // ============ 事件 ============
    [Header("事件")]
    public UnityEvent OnItemAdded;
    public UnityEvent OnRecipeCompleted;
    public UnityEvent OnPotPickedUp;
    public UnityEvent OnPotPlaced;
    public UnityEvent OnPotEmptied;

    // ⭐ 新增：精灵图变化事件（用于 LinkedWorldObject 同步）
    public static event System.Action<string, Sprite> OnPotSpriteChanged;
    public static event System.Action<string, PotState> OnPotStateChanged;

    // ============ 运行时数据 ============
    [Header("调试信息（只读）")]
    [SerializeField] private PotState currentState = PotState.OnTable_Empty;
    [SerializeField] private List<string> containedItemIDs = new List<string>();
    [SerializeField] private PotRecipe matchedRecipe = null;

    // 缓存
    private SpriteRenderer spriteRenderer;
    private Collider2D potCollider;

    // 颜色过渡相关
    private Color currentColor;
    private Color targetColor;
    private Coroutine colorTransitionCoroutine;
    private Coroutine flashCoroutine;
    private bool isFlashing = false;
    private bool isHovering = false;

    // ============ 属性 ============
    public PotState CurrentState => currentState;
    public PotRecipe MatchedRecipe => matchedRecipe;
    public List<string> ContainedItemIDs => new List<string>(containedItemIDs);

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        potCollider = GetComponent<Collider2D>();

        if (spriteRenderer != null)
        {
            currentColor = spriteRenderer.color;
        }
    }

    private void Start()
    {
        // ⭐ 新增：注册到 WorldObjectStateManager
        RegisterToWorldObjectStateManager();

        UpdateVisuals();
    }

    private void Update()
    {
        // 准备就绪时的脉冲效果
        if (enableReadyPulse && currentState == PotState.OnTable_Ready && !isFlashing)
        {
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            Color pulseColor = Color.Lerp(normalColor, readyPulseColor, pulse * 0.3f);

            if (!isHovering)
            {
                spriteRenderer.color = pulseColor;
            }
        }
    }

    private void OnMouseEnter()
    {
        if (currentState == PotState.OnTable_Empty ||
            currentState == PotState.OnTable_Filling ||
            currentState == PotState.OnTable_Ready)
        {
            isHovering = true;
            if (!isFlashing)
            {
                TransitionToColor(hoverColor);
            }
        }
    }

    private void OnMouseExit()
    {
        isHovering = false;
        if (!isFlashing && currentState != PotState.OnTable_Ready)
        {
            TransitionToColor(normalColor);
        }
    }

    private void OnMouseDown()
    {
        Interact();
    }

    // ============ ⭐ 新增：WorldObjectStateManager 联动 ============

    /// <summary>
    /// 注册到世界物品状态管理器
    /// </summary>
    private void RegisterToWorldObjectStateManager()
    {
        if (string.IsNullOrEmpty(objectID)) return;
        if (WorldObjectStateManager.Instance == null) return;

        // 注册初始状态（在桌上 = 激活）
        bool isOnTable = currentState == PotState.OnTable_Empty ||
                         currentState == PotState.OnTable_Filling ||
                         currentState == PotState.OnTable_Ready;

        WorldObjectStateManager.Instance.RegisterObject(objectID, isOnTable);

        Debug.Log($"[PotController] 注册到 WorldObjectStateManager: {objectID}, 在桌上: {isOnTable}");
    }

    /// <summary>
    /// 通知世界物品状态管理器状态变化
    /// </summary>
    private void NotifyWorldObjectStateManager(bool isVisible)
    {
        if (string.IsNullOrEmpty(objectID)) return;
        if (WorldObjectStateManager.Instance == null) return;

        WorldObjectStateManager.Instance.SetObjectState(objectID, isVisible);

        Debug.Log($"[PotController] 通知状态变化: {objectID} → {(isVisible ? "显示" : "隐藏")}");
    }

    /// <summary>
    /// 通知精灵图变化（用于 LinkedWorldObject 同步）
    /// </summary>
    private void NotifySpriteChanged(Sprite newSprite)
    {
        if (string.IsNullOrEmpty(objectID)) return;

        OnPotSpriteChanged?.Invoke(objectID, newSprite);

        Debug.Log($"[PotController] 通知精灵图变化: {objectID} → {(newSprite != null ? newSprite.name : "null")}");
    }

    /// <summary>
    /// 通知状态变化（用于更复杂的同步需求）
    /// </summary>
    private void NotifyStateChanged()
    {
        if (string.IsNullOrEmpty(objectID)) return;

        OnPotStateChanged?.Invoke(objectID, currentState);
    }

    // ============ 颜色过渡方法 ============

    private void TransitionToColor(Color target)
    {
        if (colorTransitionCoroutine != null)
        {
            StopCoroutine(colorTransitionCoroutine);
        }
        colorTransitionCoroutine = StartCoroutine(ColorTransitionCoroutine(target));
    }

    private IEnumerator ColorTransitionCoroutine(Color target)
    {
        Color startColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = SmoothStep(elapsed / colorTransitionDuration);
            spriteRenderer.color = Color.Lerp(startColor, target, t);
            yield return null;
        }

        spriteRenderer.color = target;
        currentColor = target;
        colorTransitionCoroutine = null;
    }

    /// <summary>
    /// 闪烁颜色效果
    /// </summary>
    public void FlashColor(Color flashColor)
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashColorCoroutine(flashColor));
    }

    private IEnumerator FlashColorCoroutine(Color flashColor)
    {
        isFlashing = true;

        // 快速过渡到闪烁颜色
        float fadeInTime = flashDuration * 0.3f;
        float fadeOutTime = flashDuration * 0.7f;
        float elapsed = 0f;

        Color startColor = spriteRenderer.color;

        while (elapsed < fadeInTime)
        {
            elapsed += Time.deltaTime;
            float t = SmoothStep(elapsed / fadeInTime);
            spriteRenderer.color = Color.Lerp(startColor, flashColor, t);
            yield return null;
        }

        spriteRenderer.color = flashColor;

        // 淡出回原色
        Color returnColor = isHovering ? hoverColor : normalColor;
        elapsed = 0f;
        while (elapsed < fadeOutTime)
        {
            elapsed += Time.deltaTime;
            float t = SmoothStep(elapsed / fadeOutTime);
            spriteRenderer.color = Color.Lerp(flashColor, returnColor, t);
            yield return null;
        }

        spriteRenderer.color = returnColor;
        currentColor = returnColor;

        isFlashing = false;
        flashCoroutine = null;
    }

    /// <summary>
    /// 平滑步进函数 (SmoothStep)
    /// </summary>
    private float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    // ============ 主交互逻辑 ============
    public void Interact()
    {
        Debug.Log($"[PotController] 交互，当前状态: {currentState}");

        switch (currentState)
        {
            case PotState.OnTable_Empty:
            case PotState.OnTable_Filling:
                TryAddItem();
                break;

            case PotState.OnTable_Ready:
                TryPickup();
                break;

            default:
                Debug.Log($"[PotController] 当前状态 {currentState} 不支持交互");
                break;
        }
    }

    // ============ 物品添加逻辑 ============
    private void TryAddItem()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogError("[PotController] UIManager.Instance 为空！");
            return;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        // 检查是否选中了物品
        if (selectedItem == null)
        {
            Debug.Log($"[PotController] {noItemHint}");
            ShowHint(noItemHint);
            FlashColor(failColor);
            return;
        }

        // 检查物品是否属于任何配方
        if (!IsItemValidForAnyRecipe(selectedItem.itemID))
        {
            Debug.Log($"[PotController] {wrongItemHint}: {selectedItem.displayName}");
            ShowHint(wrongItemHint);
            PlaySFX(errorSFX);
            FlashColor(failColor);
            return;
        }

        // 检查物品是否已经在陶罐中
        if (containedItemIDs.Contains(selectedItem.itemID))
        {
            Debug.Log($"[PotController] 物品已在陶罐中: {selectedItem.displayName}");
            ShowHint("这个物品已经放进去了");
            PlaySFX(errorSFX);
            FlashColor(failColor);
            return;
        }

        // 添加物品
        AddItem(selectedItem);
    }

    private void AddItem(ItemData item)
    {
        Debug.Log($"[PotController] 添加物品: {item.displayName}");

        // 消耗背包中的物品
        UIManager.Instance.ConsumeSelectedItem();

        // 添加到已有列表
        containedItemIDs.Add(item.itemID);

        // 播放音效
        PlaySFX(addItemSFX);

        // 闪烁成功颜色
        FlashColor(successColor);

        // 触发事件
        OnItemAdded?.Invoke();

        // 检查配方匹配
        CheckRecipeMatch();

        // 更新状态
        UpdateState();

        // 更新视觉
        UpdateVisuals();

        // 保存
        SaveLoadSystem.Instance?.SaveGame();
    }

    private bool IsItemValidForAnyRecipe(string itemID)
    {
        if (availableRecipes == null) return false;

        foreach (var recipe in availableRecipes)
        {
            if (recipe == null) continue;

            // 跳过已完成的配方
            if (completedRecipeIDs.Contains(recipe.recipeID)) continue;

            // 检查物品是否在此配方中
            if (recipe.requiredItems != null)
            {
                foreach (var requiredItem in recipe.requiredItems)
                {
                    if (requiredItem != null && requiredItem.itemID == itemID)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    private void CheckRecipeMatch()
    {
        matchedRecipe = null;

        if (availableRecipes == null) return;

        foreach (var recipe in availableRecipes)
        {
            // 跳过已完成的配方
            if (completedRecipeIDs.Contains(recipe.recipeID))
                continue;

            if (recipe.MatchesRecipe(containedItemIDs))
            {
                matchedRecipe = recipe;
                Debug.Log($"[PotController] ✓ 配方匹配: {recipe.recipeName}");

                // 播放完成音效
                PlaySFX(recipeCompleteSFX);

                // 触发事件
                OnRecipeCompleted?.Invoke();

                break;
            }
        }
    }

    private void UpdateState()
    {
        PotState oldState = currentState;

        if (containedItemIDs.Count == 0)
        {
            currentState = PotState.OnTable_Empty;
        }
        else if (matchedRecipe != null)
        {
            currentState = PotState.OnTable_Ready;
        }
        else
        {
            currentState = PotState.OnTable_Filling;
        }

        // ⭐ 新增：状态变化时通知
        if (oldState != currentState)
        {
            NotifyStateChanged();
        }
    }

    // ============ 拾取逻辑 ============
    private void TryPickup()
    {
        if (currentState != PotState.OnTable_Ready)
        {
            Debug.Log($"[PotController] {potNotReadyHint}");
            ShowHint(potNotReadyHint);
            return;
        }

        if (potItemFilled == null)
        {
            Debug.LogError("[PotController] potItemFilled 未设置！");
            return;
        }

        // 添加到背包
        bool added = InventorySystem.Instance.AddItem(potItemFilled);
        if (!added)
        {
            Debug.LogWarning("[PotController] 背包已满，无法拾取陶罐");
            ShowHint("背包已满");
            return;
        }

        Debug.Log($"[PotController] 拾取陶罐（配方: {matchedRecipe?.recipeName}）");

        // 播放音效
        PlaySFX(pickupSFX);

        // 更新状态
        currentState = PotState.InInventory_Filled;

        // 触发事件
        OnPotPickedUp?.Invoke();

        // ⭐ 新增：通知 WorldObjectStateManager（隐藏）
        NotifyWorldObjectStateManager(false);
        NotifyStateChanged();

        // 隐藏场景中的陶罐
        gameObject.SetActive(false);

        // 保存
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ 放置逻辑（从背包放回桌面）============
    /// <summary>
    /// 从背包放置陶罐到桌面
    /// 由外部调用（如桌面的交互脚本）
    /// </summary>
    public void PlaceOnTable(bool isEmpty)
    {
        Debug.Log($"[PotController] 放置陶罐到桌面，isEmpty: {isEmpty}");

        // 显示陶罐
        gameObject.SetActive(true);

        if (isEmpty)
        {
            // 清空内容
            ClearContents();
            currentState = PotState.OnTable_Empty;
        }
        else
        {
            currentState = PotState.OnTable_Ready;
        }

        // 播放音效
        PlaySFX(placeSFX);

        // 重置颜色状态
        isFlashing = false;
        isHovering = false;
        spriteRenderer.color = normalColor;
        currentColor = normalColor;

        // 更新视觉
        UpdateVisuals();

        // 触发事件
        OnPotPlaced?.Invoke();

        // ⭐ 新增：通知 WorldObjectStateManager（显示）
        NotifyWorldObjectStateManager(true);
        NotifyStateChanged();

        // 保存
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ 清空陶罐 ============
    /// <summary>
    /// 清空陶罐内容（合成后调用）
    /// </summary>
    public void ClearContents()
    {
        containedItemIDs.Clear();
        matchedRecipe = null;
        currentState = PotState.OnTable_Empty;

        Debug.Log("[PotController] 陶罐已清空");

        UpdateVisuals();
        OnPotEmptied?.Invoke();

        // ⭐ 新增：通知状态变化
        NotifyStateChanged();
    }

    /// <summary>
    /// 标记当前配方为已完成
    /// </summary>
    public void MarkRecipeCompleted()
    {
        if (matchedRecipe != null && !completedRecipeIDs.Contains(matchedRecipe.recipeID))
        {
            completedRecipeIDs.Add(matchedRecipe.recipeID);
            Debug.Log($"[PotController] 配方已完成: {matchedRecipe.recipeName}，总完成数: {completedRecipeIDs.Count}");
        }
    }

    /// <summary>
    /// 获取已完成配方数量
    /// </summary>
    public int GetCompletedRecipeCount()
    {
        return completedRecipeIDs.Count;
    }

    /// <summary>
    /// 检查是否所有配方都已完成
    /// </summary>
    public bool AreAllRecipesCompleted()
    {
        if (availableRecipes == null) return true;
        return completedRecipeIDs.Count >= availableRecipes.Length;
    }

    // ============ 视觉更新 ============
    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;

        Sprite targetSprite = null;

        switch (currentState)
        {
            case PotState.OnTable_Empty:
            case PotState.InInventory_Empty:
                targetSprite = emptySprite;
                break;

            case PotState.OnTable_Filling:
                targetSprite = fillingSprite != null ? fillingSprite : emptySprite;
                break;

            case PotState.OnTable_Ready:
            case PotState.InInventory_Filled:
                // 优先使用配方特定的精灵图
                if (matchedRecipe != null && matchedRecipe.filledPotSprite != null)
                {
                    targetSprite = matchedRecipe.filledPotSprite;
                }
                else
                {
                    targetSprite = filledSprite != null ? filledSprite : emptySprite;
                }
                break;

            default:
                targetSprite = emptySprite;
                break;
        }

        if (targetSprite != null && spriteRenderer.sprite != targetSprite)
        {
            spriteRenderer.sprite = targetSprite;

            // ⭐ 新增：通知精灵图变化
            NotifySpriteChanged(targetSprite);
        }
    }

    // ============ 工具方法 ============
    private void PlaySFX(string sfxName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(sfxName))
        {
            AudioManager.Instance.PlaySFX(sfxName);
        }
    }

    private void ShowHint(string hint)
    {
        // 这里可以对接你的提示系统
        Debug.Log($"[提示] {hint}");

        // 如果有 TextPromptManager
        if (TextPromptManager.Instance != null)
        {
            TextPromptManager.Instance.ShowPrompt(hint);
        }
    }

    // ============ 存档相关 ============

    /// <summary>
    /// 获取存档数据
    /// </summary>
    public PotSaveData GetSaveData()
    {
        return new PotSaveData
        {
            objectID = objectID,
            currentState = (int)currentState,
            containedItemIDs = new List<string>(containedItemIDs),
            matchedRecipeID = matchedRecipe?.recipeID ?? "",
            completedRecipeIDs = new List<string>(completedRecipeIDs)
        };
    }

    /// <summary>
    /// 恢复存档数据
    /// </summary>
    public void RestoreSaveData(PotSaveData data)
    {
        if (data == null) return;

        currentState = (PotState)data.currentState;
        containedItemIDs = new List<string>(data.containedItemIDs);
        completedRecipeIDs = new List<string>(data.completedRecipeIDs);

        // 恢复匹配的配方
        if (!string.IsNullOrEmpty(data.matchedRecipeID) && availableRecipes != null)
        {
            foreach (var recipe in availableRecipes)
            {
                if (recipe.recipeID == data.matchedRecipeID)
                {
                    matchedRecipe = recipe;
                    break;
                }
            }
        }

        // 根据状态决定是否显示
        bool shouldBeVisible = currentState == PotState.OnTable_Empty ||
                               currentState == PotState.OnTable_Filling ||
                               currentState == PotState.OnTable_Ready;

        gameObject.SetActive(shouldBeVisible);

        UpdateVisuals();

        Debug.Log($"[PotController] 恢复存档: 状态={currentState}, 物品数={containedItemIDs.Count}");
    }
}

/// <summary>
/// 陶罐存档数据
/// </summary>
[System.Serializable]
public class PotSaveData
{
    public string objectID;
    public int currentState;
    public List<string> containedItemIDs;
    public string matchedRecipeID;
    public List<string> completedRecipeIDs;

    public PotSaveData()
    {
        containedItemIDs = new List<string>();
        completedRecipeIDs = new List<string>();
    }
}