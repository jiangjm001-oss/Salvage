// Assets/Scripts/GamePlay/GlassBottlePuzzleController.cs
// 玻璃瓶谜题控制器 - 多物体状态联动
// 流程：鱼线放入瓶中 → 系上玻璃珠 → 打碎瓶子 → 拾取眼珠
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 玻璃瓶谜题控制器
/// 管理玻璃瓶、鱼线、玻璃珠、眼珠的多阶段交互
/// </summary>
public class GlassBottlePuzzleController : MonoBehaviour
{
    /// <summary>
    /// 谜题阶段枚举
    /// </summary>
    public enum PuzzleStage
    {
        Initial,            // 初始：玻璃瓶完好，等待放入鱼线
        FishingLineInBottle,// 鱼线在瓶中，等待系上玻璃珠
        MarbleAttached,     // 玻璃珠已系上，等待点击打碎
        Shattered,          // 已打碎，眼珠可拾取
        Completed           // 完成：眼珠已拾取
    }

    [Header("谜题标识")]
    [Tooltip("唯一标识符（用于存档）")]
    public string puzzleID = "glass_bottle_puzzle";

    [Header("场景物体引用")]
    [Tooltip("完整的玻璃瓶（初始显示）")]
    public GameObject glassBottle;

    [Tooltip("瓶中的鱼线（初始隐藏）")]
    public GameObject fishingLineInBottle;

    [Tooltip("系着玻璃珠的鱼线（初始隐藏）")]
    public GameObject fishingLineWithMarble;

    [Tooltip("碎玻璃瓶（初始隐藏）")]
    public GameObject brokenGlassBottle;

    [Tooltip("眼珠物体（初始显示但不可交互）")]
    public GameObject eyeball;

    [Header("所需物品")]
    [Tooltip("鱼线物品数据")]
    public ItemData fishingLineItem;

    [Tooltip("玻璃珠物品数据")]
    public ItemData glassMarbleItem;

    [Tooltip("眼珠物品数据（拾取后添加到背包）")]
    public ItemData eyeballItem;

    [Header("音效设置")]
    [Tooltip("放入鱼线音效")]
    public string putLineSound = "Audio/SFX/item_place";

    [Tooltip("系上玻璃珠音效")]
    public string attachMarbleSound = "Audio/SFX/item_combine";

    [Tooltip("玻璃瓶碎裂音效")]
    public string shatterSound = "Audio/SFX/glass_break";

    [Tooltip("拾取眼珠音效")]
    public string pickupEyeballSound = "Audio/SFX/item_pickup";

    [Header("事件")]
    [Tooltip("放入鱼线后触发")]
    public UnityEvent OnFishingLinePlaced;

    [Tooltip("系上玻璃珠后触发")]
    public UnityEvent OnMarbleAttached;

    [Tooltip("玻璃瓶碎裂后触发")]
    public UnityEvent OnBottleShattered;

    [Tooltip("拾取眼珠后触发")]
    public UnityEvent OnEyeballPickedUp;

    [Tooltip("谜题完全完成后触发")]
    public UnityEvent OnPuzzleCompleted;

    // 当前阶段
    [HideInInspector]
    public PuzzleStage currentStage = PuzzleStage.Initial;

    // 内部状态
    private bool eyeballPickedUp = false;

    // ============ Unity 生命周期 ============

    private void Start()
    {
        // 尝试从存档恢复状态
        TryRestoreState();

        // 初始化物体状态
        UpdateVisualState();

        // 注册点击事件
        RegisterClickHandlers();
    }

    // ============ 点击事件注册 ============

    /// <summary>
    /// 为各个物体注册点击处理
    /// </summary>
    private void RegisterClickHandlers()
    {
        // 玻璃瓶点击
        if (glassBottle != null)
        {
            var bottleClick = glassBottle.GetComponent<ClickHandler>();
            if (bottleClick == null)
                bottleClick = glassBottle.AddComponent<ClickHandler>();
            bottleClick.Initialize(this, ClickTarget.GlassBottle);
        }

        // 瓶中鱼线点击
        if (fishingLineInBottle != null)
        {
            var lineClick = fishingLineInBottle.GetComponent<ClickHandler>();
            if (lineClick == null)
                lineClick = fishingLineInBottle.AddComponent<ClickHandler>();
            lineClick.Initialize(this, ClickTarget.FishingLineInBottle);
        }

        // 系着玻璃珠的鱼线点击
        if (fishingLineWithMarble != null)
        {
            var marbleLineClick = fishingLineWithMarble.GetComponent<ClickHandler>();
            if (marbleLineClick == null)
                marbleLineClick = fishingLineWithMarble.AddComponent<ClickHandler>();
            marbleLineClick.Initialize(this, ClickTarget.FishingLineWithMarble);
        }

        // 眼珠点击
        if (eyeball != null)
        {
            var eyeClick = eyeball.GetComponent<ClickHandler>();
            if (eyeClick == null)
                eyeClick = eyeball.AddComponent<ClickHandler>();
            eyeClick.Initialize(this, ClickTarget.Eyeball);
        }
    }

    // ============ 点击处理 ============

    /// <summary>
    /// 处理物体点击（由 ClickHandler 调用）
    /// </summary>
    public void HandleClick(ClickTarget target)
    {
        switch (target)
        {
            case ClickTarget.GlassBottle:
                OnGlassBottleClicked();
                break;
            case ClickTarget.FishingLineInBottle:
                OnFishingLineInBottleClicked();
                break;
            case ClickTarget.FishingLineWithMarble:
                OnFishingLineWithMarbleClicked();
                break;
            case ClickTarget.Eyeball:
                OnEyeballClicked();
                break;
        }
    }

    /// <summary>
    /// 玻璃瓶被点击
    /// </summary>
    private void OnGlassBottleClicked()
    {
        // 只在初始阶段响应
        if (currentStage != PuzzleStage.Initial) return;

        // 检查是否选中了鱼线
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log("[GlassBottlePuzzle] 需要选中鱼线");
            return;
        }

        if (selectedItem.itemID != fishingLineItem.itemID)
        {
            Debug.Log("[GlassBottlePuzzle] 需要鱼线，当前选中: " + selectedItem.displayName);
            return;
        }

        // ✓ 使用鱼线
        Debug.Log("[GlassBottlePuzzle] 将鱼线放入玻璃瓶");

        // 消耗鱼线
        UIManager.Instance.ConsumeSelectedItem();

        // 播放音效
        PlaySound(putLineSound);

        // 切换到下一阶段
        currentStage = PuzzleStage.FishingLineInBottle;
        UpdateVisualState();

        // 触发事件
        OnFishingLinePlaced?.Invoke();

        // 保存进度
        SaveProgress();
    }

    /// <summary>
    /// 瓶中鱼线被点击
    /// </summary>
    private void OnFishingLineInBottleClicked()
    {
        // 只在鱼线在瓶中阶段响应
        if (currentStage != PuzzleStage.FishingLineInBottle) return;

        // 检查是否选中了玻璃珠
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log("[GlassBottlePuzzle] 需要选中玻璃珠");
            return;
        }

        if (selectedItem.itemID != glassMarbleItem.itemID)
        {
            Debug.Log("[GlassBottlePuzzle] 需要玻璃珠，当前选中: " + selectedItem.displayName);
            return;
        }

        // ✓ 使用玻璃珠
        Debug.Log("[GlassBottlePuzzle] 将玻璃珠系在鱼线上");

        // 消耗玻璃珠
        UIManager.Instance.ConsumeSelectedItem();

        // 播放音效
        PlaySound(attachMarbleSound);

        // 切换到下一阶段
        currentStage = PuzzleStage.MarbleAttached;
        UpdateVisualState();

        // 触发事件
        OnMarbleAttached?.Invoke();

        // 保存进度
        SaveProgress();
    }

    /// <summary>
    /// 系着玻璃珠的鱼线被点击
    /// </summary>
    private void OnFishingLineWithMarbleClicked()
    {
        // 只在玻璃珠已系上阶段响应
        if (currentStage != PuzzleStage.MarbleAttached) return;

        // 直接点击即可打碎
        Debug.Log("[GlassBottlePuzzle] 玻璃瓶被打碎！");

        // 播放碎裂音效
        PlaySound(shatterSound);

        // 切换到打碎阶段
        currentStage = PuzzleStage.Shattered;
        UpdateVisualState();

        // 触发事件
        OnBottleShattered?.Invoke();

        // 保存进度
        SaveProgress();
    }

    /// <summary>
    /// 眼珠被点击
    /// </summary>
    private void OnEyeballClicked()
    {
        // 只在打碎阶段且未拾取时响应
        if (currentStage != PuzzleStage.Shattered) return;
        if (eyeballPickedUp) return;

        // 拾取眼珠
        if (eyeballItem == null)
        {
            Debug.LogError("[GlassBottlePuzzle] 眼珠物品数据未设置！");
            return;
        }

        bool added = InventorySystem.Instance.AddItem(eyeballItem);
        if (added)
        {
            Debug.Log("[GlassBottlePuzzle] 拾取眼珠: " + eyeballItem.displayName);

            // 播放拾取音效
            PlaySound(pickupEyeballSound);

            // 标记已拾取
            eyeballPickedUp = true;
            currentStage = PuzzleStage.Completed;

            // 隐藏眼珠
            if (eyeball != null)
                eyeball.SetActive(false);

            // 触发事件
            OnEyeballPickedUp?.Invoke();
            OnPuzzleCompleted?.Invoke();

            // 保存进度
            SaveProgress();

            // 通知存档系统
            SaveLoadSystem.Instance?.OnItemPickedUp(puzzleID + "_eyeball");
        }
    }

    // ============ 视觉状态更新 ============

    /// <summary>
    /// 根据当前阶段更新所有物体的显示状态
    /// </summary>
    private void UpdateVisualState()
    {
        switch (currentStage)
        {
            case PuzzleStage.Initial:
                // 只显示玻璃瓶和眼珠（眼珠在瓶内）
                SetActive(glassBottle, true);
                SetActive(fishingLineInBottle, false);
                SetActive(fishingLineWithMarble, false);
                SetActive(brokenGlassBottle, false);
                SetActive(eyeball, true);
                SetEyeballInteractable(false);
                break;

            case PuzzleStage.FishingLineInBottle:
                // 玻璃瓶 + 瓶中鱼线
                SetActive(glassBottle, true);
                SetActive(fishingLineInBottle, true);
                SetActive(fishingLineWithMarble, false);
                SetActive(brokenGlassBottle, false);
                SetActive(eyeball, true);
                SetEyeballInteractable(false);
                break;

            case PuzzleStage.MarbleAttached:
                // 玻璃瓶 + 系着玻璃珠的鱼线
                SetActive(glassBottle, true);
                SetActive(fishingLineInBottle, false);
                SetActive(fishingLineWithMarble, true);
                SetActive(brokenGlassBottle, false);
                SetActive(eyeball, true);
                SetEyeballInteractable(false);
                break;

            case PuzzleStage.Shattered:
                // 碎玻璃瓶 + 可拾取的眼珠
                SetActive(glassBottle, false);
                SetActive(fishingLineInBottle, false);
                SetActive(fishingLineWithMarble, false);
                SetActive(brokenGlassBottle, true);
                SetActive(eyeball, !eyeballPickedUp);
                SetEyeballInteractable(true);
                break;

            case PuzzleStage.Completed:
                // 只剩碎玻璃瓶
                SetActive(glassBottle, false);
                SetActive(fishingLineInBottle, false);
                SetActive(fishingLineWithMarble, false);
                SetActive(brokenGlassBottle, true);
                SetActive(eyeball, false);
                break;
        }
    }

    /// <summary>
    /// 安全设置物体激活状态
    /// </summary>
    private void SetActive(GameObject obj, bool active)
    {
        if (obj != null)
            obj.SetActive(active);
    }

    /// <summary>
    /// 设置眼珠是否可交互（通过改变碰撞体或层级）
    /// </summary>
    private void SetEyeballInteractable(bool interactable)
    {
        if (eyeball == null) return;

        // 方式1：启用/禁用碰撞体
        var collider = eyeball.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = interactable;
        }

        // 方式2：也可以通过改变 SortingOrder 或层级
        // 这里我们使用碰撞体方式，更简洁
    }

    // ============ 音效播放 ============

    private void PlaySound(string soundPath)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundPath))
        {
            AudioManager.Instance.PlaySFX(soundPath);
        }
    }

    // ============ 存档/读档 ============

    private void SaveProgress()
    {
        // 使用 PlayerPrefs 保存进度（或集成到 SaveLoadSystem）
        string key = $"Puzzle_{puzzleID}";
        string data = $"{(int)currentStage},{(eyeballPickedUp ? 1 : 0)}";
        PlayerPrefs.SetString(key, data);
        PlayerPrefs.Save();

        Debug.Log($"[GlassBottlePuzzle] 保存进度: Stage={currentStage}, EyeballPickedUp={eyeballPickedUp}");

        // 触发全局存档
        SaveLoadSystem.Instance?.SaveGame();
    }

    private void TryRestoreState()
    {
        string key = $"Puzzle_{puzzleID}";
        if (PlayerPrefs.HasKey(key))
        {
            string data = PlayerPrefs.GetString(key);
            string[] parts = data.Split(',');

            if (parts.Length >= 2)
            {
                currentStage = (PuzzleStage)int.Parse(parts[0]);
                eyeballPickedUp = parts[1] == "1";

                Debug.Log($"[GlassBottlePuzzle] 恢复进度: Stage={currentStage}, EyeballPickedUp={eyeballPickedUp}");
            }
        }
    }

    /// <summary>
    /// 供 SaveLoadSystem 调用的存档数据获取
    /// </summary>
    public string GetSaveData()
    {
        return $"{(int)currentStage},{(eyeballPickedUp ? 1 : 0)}";
    }

    /// <summary>
    /// 供 SaveLoadSystem 调用的存档数据恢复
    /// </summary>
    public void RestoreSaveData(string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        string[] parts = data.Split(',');
        if (parts.Length >= 2)
        {
            currentStage = (PuzzleStage)int.Parse(parts[0]);
            eyeballPickedUp = parts[1] == "1";
            UpdateVisualState();
        }
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(puzzleID))
        {
            puzzleID = $"glass_bottle_{GetInstanceID()}";
        }
    }

    /// <summary>
    /// 编辑器中绘制引用线
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        if (glassBottle != null)
            Gizmos.DrawLine(transform.position, glassBottle.transform.position);
        if (fishingLineInBottle != null)
            Gizmos.DrawLine(transform.position, fishingLineInBottle.transform.position);
        if (fishingLineWithMarble != null)
            Gizmos.DrawLine(transform.position, fishingLineWithMarble.transform.position);
        if (brokenGlassBottle != null)
            Gizmos.DrawLine(transform.position, brokenGlassBottle.transform.position);
        if (eyeball != null)
            Gizmos.DrawLine(transform.position, eyeball.transform.position);
    }
}

// ============ 点击目标枚举 ============

public enum ClickTarget
{
    GlassBottle,
    FishingLineInBottle,
    FishingLineWithMarble,
    Eyeball
}

// ============ 点击处理器组件 ============

/// <summary>
/// 附加到可点击物体上，转发点击事件给控制器
/// </summary>
public class ClickHandler : MonoBehaviour
{
    private GlassBottlePuzzleController controller;
    private ClickTarget target;
    private bool isInitialized = false;

    /// <summary>
    /// 初始化（由控制器调用）
    /// </summary>
    public void Initialize(GlassBottlePuzzleController ctrl, ClickTarget clickTarget)
    {
        controller = ctrl;
        target = clickTarget;
        isInitialized = true;

        // 确保有碰撞体
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogWarning($"[ClickHandler] {gameObject.name} 缺少 Collider2D，请添加！");
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

        if (!isInitialized || controller == null) return;

        controller.HandleClick(target);
    }
}