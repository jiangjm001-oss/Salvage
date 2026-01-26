// Assets/Scripts/GamePlay/Letter/QuillDeskController.cs
// 羽毛笔桌面控制器 - 优化版
// 处理：信纸放置、胶水+标题粘贴、羽毛笔涂抹 Logo
// 使用 LetterDisplay 组件管理信纸的分层显示
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 羽毛笔桌面控制器
/// 处理两种操作：
/// 1. 标题粘贴：放置信纸 → 涂胶水 → 贴标题 → 拾取
/// 2. Logo 涂抹：放置信纸 → 拖动羽毛笔涂抹 → Logo 自动显示 → 拾取
/// </summary>
public class QuillDeskController : MonoBehaviour
{
    /// <summary>
    /// 桌面状态
    /// </summary>
    public enum DeskState
    {
        Empty,              // 桌面空
        LetterPlaced,       // 信纸已放置
        GlueApplied,        // 已涂胶水（等待贴标题）
        ReadyForPickup      // 操作完成，等待拾取
    }

    [Header("当前状态（调试用）")]
    public DeskState currentState = DeskState.Empty;

    // ============ 桌面设置 ============
    [Header("桌面设置")]
    [Tooltip("桌面上的信纸物体（包含 LetterDisplay 组件）")]
    public GameObject letterOnDesk;

    [Tooltip("信纸的 LetterDisplay 组件（用于分层显示）")]
    public LetterDisplay letterDisplay;

    // ============ 标题粘贴设置 ============
    [Header("标题粘贴设置")]
    [Tooltip("胶水物品")]
    public ItemData glueItem;

    [Tooltip("标题物品")]
    public ItemData titleItem;

    [Tooltip("胶水效果覆盖层（涂胶水后显示，独立于信纸）")]
    public GameObject glueEffect;

    // ============ Logo 涂抹设置 ============
    [Header("Logo 涂抹设置")]
    [Tooltip("羽毛笔物体")]
    public GameObject quillPen;

    [Tooltip("涂抹检测区域（Collider2D）")]
    public Collider2D paintArea;

    [Tooltip("完成涂抹所需的百分比 (0-1)")]
    [Range(0.1f, 1f)]
    public float requiredPaintPercent = 0.6f;

    [Tooltip("涂抹速度（每秒增加的百分比）")]
    public float paintSpeed = 0.3f;

    [Tooltip("涂抹进度指示器（可选）")]
    public SpriteRenderer paintProgressIndicator;

    // ============ 音效 ============
    [Header("音效")]
    public string placeLetterSound = "paper_place";
    public string applyGlueSound = "glue_apply";
    public string stickTitleSound = "paper_stick";
    public string paintCompleteSound = "paint_complete";
    public string pickupSound = "paper_place";

    // ============ 事件 ============
    [Header("事件")]
    public UnityEvent OnLetterPlaced;
    public UnityEvent OnGlueApplied;
    public UnityEvent OnTitleStuck;
    public UnityEvent OnLogoPainted;
    public UnityEvent OnLetterPickedUp;

    // ============ 内部状态 ============
    private bool hasGlueApplied = false;
    private float currentPaintPercent = 0f;
    private bool isPainting = false;
    private Vector3 quillOriginalPos;
    private bool quillCanDrag = true;
    private bool logoCompleted = false;

    // ============ Unity 生命周期 ============

    private void Start()
    {
        // 记录羽毛笔原始位置
        if (quillPen != null)
        {
            quillOriginalPos = quillPen.transform.localPosition;
        }

        // 初始隐藏
        HideAll();
    }

    private void OnEnable()
    {
        // 每次进入 ZoomView 时重置桌面状态
        ResetDeskState();

        // 检查 Logo 是否已完成（影响羽毛笔是否可拖动）
        if (LetterManager.Instance != null)
        {
            logoCompleted = LetterManager.Instance.hasLogo;
            quillCanDrag = !logoCompleted;
        }
    }

    // ============ 初始化 ============

    private void HideAll()
    {
        if (letterOnDesk != null) letterOnDesk.SetActive(false);
        if (glueEffect != null) glueEffect.SetActive(false);
    }

    private void ResetDeskState()
    {
        currentState = DeskState.Empty;
        hasGlueApplied = false;
        currentPaintPercent = 0f;
        isPainting = false;

        HideAll();

        // 重置羽毛笔位置
        if (quillPen != null)
        {
            quillPen.transform.localPosition = quillOriginalPos;
        }
    }

    // ============ 桌面点击 - 放置信纸 ============

    /// <summary>
    /// 点击桌面区域 - 放置信纸
    /// </summary>
    public void OnDeskClicked()
    {
        Debug.Log($"[QuillDeskController] 点击桌面，当前状态: {currentState}");

        if (currentState != DeskState.Empty) return;

        if (!TryPlaceLetterFromInventory())
        {
            Debug.Log("[QuillDeskController] 需要先选中信纸");
        }
    }

    private bool TryPlaceLetterFromInventory()
    {
        if (UIManager.Instance == null || LetterManager.Instance == null) return false;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return false;

        // 检查是否是信纸
        if (LetterManager.Instance.letterItemData == null) return false;
        if (selectedItem.itemID != LetterManager.Instance.letterItemData.itemID) return false;

        // 从背包移除信纸
        UIManager.Instance.ConsumeSelectedItem();

        // 放置信纸
        PlaceLetter();
        return true;
    }

    private void PlaceLetter()
    {
        Debug.Log("[QuillDeskController] 放置信纸到桌面");

        // 显示信纸
        if (letterOnDesk != null)
        {
            letterOnDesk.SetActive(true);
        }

        // 刷新 LetterDisplay 显示当前状态的部件
        if (letterDisplay != null)
        {
            letterDisplay.RefreshDisplay();
        }

        currentState = DeskState.LetterPlaced;

        PlaySound(placeLetterSound);
        OnLetterPlaced?.Invoke();
    }

    // ============ 信纸点击 - 胶水/标题/拾取 ============

    /// <summary>
    /// 点击信纸 - 涂胶水、贴标题或拾取
    /// </summary>
    public void OnLetterClicked()
    {
        Debug.Log($"[QuillDeskController] 点击信纸，当前状态: {currentState}");

        switch (currentState)
        {
            case DeskState.LetterPlaced:
                // 尝试涂胶水
                TryApplyGlue();
                break;

            case DeskState.GlueApplied:
                // 尝试贴标题
                TryStickTitle();
                break;

            case DeskState.ReadyForPickup:
                // 拾取信纸
                PickupLetter();
                break;
        }
    }

    // ============ 标题流程 ============

    private void TryApplyGlue()
    {
        // 检查标题是否已完成
        if (LetterManager.Instance != null && LetterManager.Instance.hasTitle)
        {
            Debug.Log("[QuillDeskController] 标题已完成，无需涂胶水");
            // 直接进入可拾取状态
            currentState = DeskState.ReadyForPickup;
            return;
        }

        if (UIManager.Instance == null || glueItem == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log("[QuillDeskController] 需要选中胶水");
            return;
        }

        if (selectedItem.itemID != glueItem.itemID)
        {
            Debug.Log($"[QuillDeskController] 需要胶水，不是 {selectedItem.displayName}");
            return;
        }

        // 消耗胶水
        UIManager.Instance.ConsumeSelectedItem();

        // 显示胶水效果
        if (glueEffect != null)
        {
            glueEffect.SetActive(true);
        }

        hasGlueApplied = true;
        currentState = DeskState.GlueApplied;

        PlaySound(applyGlueSound);
        OnGlueApplied?.Invoke();

        Debug.Log("[QuillDeskController] ✓ 涂胶水完成");
    }

    private void TryStickTitle()
    {
        if (UIManager.Instance == null || titleItem == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log("[QuillDeskController] 需要选中标题");
            return;
        }

        if (selectedItem.itemID != titleItem.itemID)
        {
            Debug.Log($"[QuillDeskController] 需要标题，不是 {selectedItem.displayName}");
            return;
        }

        // 消耗标题
        UIManager.Instance.ConsumeSelectedItem();

        // 通知 LetterManager 标题完成
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.SetTitleComplete();
        }

        // LetterDisplay 会自动刷新显示标题
        // 如果没有自动刷新，手动调用
        if (letterDisplay != null)
        {
            letterDisplay.RefreshDisplay();
        }

        // 隐藏胶水效果
        if (glueEffect != null)
        {
            glueEffect.SetActive(false);
        }

        hasGlueApplied = false;
        currentState = DeskState.ReadyForPickup;

        PlaySound(stickTitleSound);
        OnTitleStuck?.Invoke();

        Debug.Log("[QuillDeskController] ✓ 标题粘贴完成");
    }

    // ============ Logo 涂抹流程 ============

    /// <summary>
    /// 开始拖动羽毛笔
    /// </summary>
    public void OnQuillDragStart()
    {
        if (!quillCanDrag)
        {
            Debug.Log("[QuillDeskController] 羽毛笔不可拖动");
            return;
        }

        // 必须先放置信纸
        if (currentState != DeskState.LetterPlaced && currentState != DeskState.Empty)
        {
            // 如果在胶水状态，也不允许涂抹
            if (currentState == DeskState.GlueApplied)
            {
                Debug.Log("[QuillDeskController] 当前在胶水状态，请先完成标题粘贴");
                return;
            }
        }

        if (currentState == DeskState.Empty)
        {
            Debug.Log("[QuillDeskController] 需要先放置信纸");
            return;
        }

        // 检查 Logo 是否已完成
        if (LetterManager.Instance != null && LetterManager.Instance.hasLogo)
        {
            quillCanDrag = false;
            Debug.Log("[QuillDeskController] Logo 已完成，羽毛笔不可拖动");
            return;
        }

        isPainting = true;
        Debug.Log("[QuillDeskController] 开始涂抹");
    }

    /// <summary>
    /// 拖动羽毛笔
    /// </summary>
    public void OnQuillDrag(Vector3 worldPos)
    {
        if (!isPainting || !quillCanDrag) return;

        // 移动羽毛笔
        if (quillPen != null)
        {
            quillPen.transform.position = worldPos;
        }

        // 检测是否在涂抹区域内
        if (paintArea != null && paintArea.OverlapPoint(worldPos))
        {
            AddPaintProgress();
        }
    }

    /// <summary>
    /// 结束拖动羽毛笔
    /// </summary>
    public void OnQuillDragEnd()
    {
        isPainting = false;

        // 如果未完成，羽毛笔回到原位
        if (quillCanDrag && quillPen != null)
        {
            quillPen.transform.localPosition = quillOriginalPos;
        }

        Debug.Log($"[QuillDeskController] 停止涂抹，当前进度: {currentPaintPercent:P0}");
    }

    private void AddPaintProgress()
    {
        if (logoCompleted) return;

        currentPaintPercent += Time.deltaTime * paintSpeed;

        // 更新进度指示器
        UpdatePaintProgressIndicator();

        // 检查是否完成
        if (currentPaintPercent >= requiredPaintPercent)
        {
            CompletePainting();
        }
    }

    private void UpdatePaintProgressIndicator()
    {
        if (paintProgressIndicator != null)
        {
            Color color = paintProgressIndicator.color;
            color.a = currentPaintPercent / requiredPaintPercent;
            paintProgressIndicator.color = color;
        }
    }

    private void CompletePainting()
    {
        if (logoCompleted) return;

        logoCompleted = true;
        isPainting = false;
        quillCanDrag = false;

        Debug.Log("[QuillDeskController] ✓ Logo 涂抹完成");

        // 羽毛笔回到原位并锁定
        if (quillPen != null)
        {
            quillPen.transform.localPosition = quillOriginalPos;
        }

        // 通知 LetterManager
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.SetLogoComplete();
        }

        // LetterDisplay 会自动刷新显示 Logo
        // 如果没有自动刷新，手动调用
        if (letterDisplay != null)
        {
            letterDisplay.RefreshDisplay();
        }

        currentState = DeskState.ReadyForPickup;

        PlaySound(paintCompleteSound);
        OnLogoPainted?.Invoke();
    }

    // ============ 拾取信纸 ============

    private void PickupLetter()
    {
        Debug.Log("[QuillDeskController] 拾取信纸");

        // 添加回背包
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.AddLetterToInventory();
        }

        // 重置桌面状态
        ResetDeskState();

        PlaySound(pickupSound);
        OnLetterPickedUp?.Invoke();
    }

    // ============ 辅助方法 ============

    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    /// <summary>
    /// 获取当前涂抹进度（用于 UI 显示）
    /// </summary>
    public float GetPaintProgress()
    {
        return currentPaintPercent / requiredPaintPercent;
    }

    /// <summary>
    /// 检查是否可以涂抹 Logo
    /// </summary>
    public bool CanPaintLogo()
    {
        return quillCanDrag && currentState == DeskState.LetterPlaced && !logoCompleted;
    }

    // ============ 调试方法 ============

    [ContextMenu("Debug: 重置桌面")]
    private void DebugReset() => ResetDeskState();

    [ContextMenu("Debug: 完成Logo涂抹")]
    private void DebugCompleteLogo()
    {
        if (currentState == DeskState.LetterPlaced)
        {
            currentPaintPercent = requiredPaintPercent;
            CompletePainting();
        }
    }
}