// Assets/Scripts/GamePlay/Letter/QuillDeskController.cs
// 羽毛笔桌面控制器 - 放在羽毛笔 ZoomView 中
// 处理：信纸放置、胶水+标题粘贴、羽毛笔涂抹 Logo
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

    [Header("桌面设置")]
    [Tooltip("桌面上的信纸物体")]
    public GameObject letterOnDesk;

    [Tooltip("信纸的 SpriteRenderer")]
    public SpriteRenderer letterSpriteRenderer;

    [Header("标题粘贴设置")]
    [Tooltip("胶水物品")]
    public ItemData glueItem;

    [Tooltip("标题物品")]
    public ItemData titleItem;

    [Tooltip("胶水效果覆盖层（涂胶水后显示）")]
    public GameObject glueOverlay;

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

    [Tooltip("Logo 完成后显示的覆盖层")]
    public GameObject logoOverlay;

    [Tooltip("涂抹进度指示器（可选）")]
    public SpriteRenderer paintProgressIndicator;

    [Header("音效")]
    public string placeLetterSound = "paper_place";
    public string applyGlueSound = "glue_apply";
    public string stickTitleSound = "paper_stick";
    public string paintLoopSound = "pen_scratch";
    public string paintCompleteSound = "paint_complete";
    public string pickupSound = "paper_place";

    [Header("事件")]
    public UnityEvent OnLetterPlaced;
    public UnityEvent OnGlueApplied;
    public UnityEvent OnTitleStuck;
    public UnityEvent OnLogoPainted;
    public UnityEvent OnLetterPickedUp;

    // 内部状态
    private bool hasGlueApplied = false;
    private float currentPaintPercent = 0f;
    private bool isPainting = false;
    private Vector3 quillOriginalPos;
    private bool quillCanDrag = true;
    private bool logoCompleted = false;

    private void Start()
    {
        // 记录羽毛笔原始位置
        if (quillPen != null)
        {
            quillOriginalPos = quillPen.transform.localPosition;
        }

        // 初始隐藏所有覆盖层
        HideAllOverlays();
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

    private void HideAllOverlays()
    {
        if (letterOnDesk != null) letterOnDesk.SetActive(false);
        if (glueOverlay != null) glueOverlay.SetActive(false);
        if (logoOverlay != null) logoOverlay.SetActive(false);
    }

    private void ResetDeskState()
    {
        currentState = DeskState.Empty;
        hasGlueApplied = false;
        currentPaintPercent = 0f;
        isPainting = false;

        HideAllOverlays();

        // 重置羽毛笔位置
        if (quillPen != null)
        {
            quillPen.transform.localPosition = quillOriginalPos;
        }
    }

    // ============ 桌面点击 ============

    /// <summary>
    /// 点击桌面区域 - 放置信纸
    /// </summary>
    public void OnDeskClicked()
    {
        Debug.Log($"[QuillDeskController] 点击桌面，当前状态: {currentState}");

        if (currentState != DeskState.Empty) return;

        // 检查是否选中了信纸
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

            // 更新精灵
            if (letterSpriteRenderer != null && LetterManager.Instance != null)
            {
                letterSpriteRenderer.sprite = LetterManager.Instance.GetCurrentSprite();
            }
        }

        currentState = DeskState.LetterPlaced;

        PlaySound(placeLetterSound);
        OnLetterPlaced?.Invoke();
    }

    // ============ 信纸点击 ============

    /// <summary>
    /// 点击桌面上的信纸
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
                // 尝试粘贴标题
                TryStickTitle();
                break;

            case DeskState.ReadyForPickup:
                // 拾取信纸
                PickupLetter();
                break;
        }
    }

    // ============ 标题粘贴流程 ============

    private void TryApplyGlue()
    {
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return;

        // 检查是否是胶水
        if (glueItem != null && selectedItem.itemID == glueItem.itemID)
        {
            ApplyGlue();
        }
    }

    private void ApplyGlue()
    {
        Debug.Log("[QuillDeskController] 涂抹胶水");

        // 消耗胶水
        UIManager.Instance.ConsumeSelectedItem();

        hasGlueApplied = true;
        currentState = DeskState.GlueApplied;

        // 显示胶水效果
        if (glueOverlay != null)
        {
            glueOverlay.SetActive(true);
        }

        PlaySound(applyGlueSound);
        OnGlueApplied?.Invoke();
    }

    private void TryStickTitle()
    {
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return;

        // 检查是否是标题
        if (titleItem != null && selectedItem.itemID == titleItem.itemID)
        {
            StickTitle();
        }
    }

    private void StickTitle()
    {
        Debug.Log("[QuillDeskController] 粘贴标题");

        // 消耗标题
        UIManager.Instance.ConsumeSelectedItem();

        // 通知 LetterManager
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.SetTitleComplete();
        }

        // 更新信纸精灵
        UpdateLetterSprite();

        // 隐藏胶水效果
        if (glueOverlay != null)
        {
            glueOverlay.SetActive(false);
        }

        hasGlueApplied = false;
        currentState = DeskState.ReadyForPickup;

        PlaySound(stickTitleSound);
        OnTitleStuck?.Invoke();
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
        if (currentState != DeskState.LetterPlaced)
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

        // 检查标题是否已完成（标题和 Logo 使用同一个桌面）
        // 如果标题未完成但选择涂抹 Logo 也是允许的
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

        // 更新进度指示器（如果有）
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
            // 可以用颜色透明度或缩放表示进度
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

        // 显示 Logo 覆盖层
        if (logoOverlay != null)
        {
            logoOverlay.SetActive(true);
        }

        // 更新信纸精灵
        UpdateLetterSprite();

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

    private void UpdateLetterSprite()
    {
        if (letterSpriteRenderer != null && LetterManager.Instance != null)
        {
            letterSpriteRenderer.sprite = LetterManager.Instance.GetCurrentSprite();
        }
    }

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
}