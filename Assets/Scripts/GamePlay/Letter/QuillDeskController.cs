// Assets/Scripts/GamePlay/Letter/QuillDeskController.cs
// 羽毛笔桌面控制器 - 修复版
// 修复：1.羽毛笔初始位置 2.涂抹痕迹即时反馈 3.笔尖检测偏移
using UnityEngine;
using UnityEngine.Events;

public class QuillDeskController : MonoBehaviour
{
    public enum DeskState
    {
        Empty,
        LetterPlaced,
        GlueApplied,
        ReadyForPickup
    }

    [Header("当前状态（调试用）")]
    public DeskState currentState = DeskState.Empty;

    // ============ 桌面设置 ============
    [Header("桌面设置")]
    public GameObject letterOnDesk;
    public LetterDisplay letterDisplay;

    // ============ 标题粘贴设置 ============
    [Header("标题粘贴设置")]
    public ItemData glueItem;
    public ItemData titleItem;
    public GameObject glueEffect;

    // ============ Logo 涂抹设置 ============
    [Header("Logo 涂抹设置")]
    public GameObject quillPen;
    public Collider2D paintArea;

    [Range(0.1f, 1f)]
    public float requiredPaintPercent = 0.6f;
    public float paintSpeed = 0.3f;
    public SpriteRenderer paintProgressIndicator;

    // ============ 【新增】笔尖偏移设置 ============
    [Header("笔尖偏移设置")]
    [Tooltip("笔尖相对于羽毛笔中心的偏移（本地坐标），通常是左下角")]
    public Vector2 penTipOffset = new Vector2(-0.3f, -0.5f);

    [Tooltip("在 Scene 视图中显示笔尖位置（调试用）")]
    public bool showPenTipGizmo = true;

    // ============ 【新增】涂抹轨迹设置 ============
    [Header("涂抹轨迹设置")]
    [Tooltip("Trail Renderer 组件（挂在羽毛笔上或笔尖子物体上）")]
    public TrailRenderer paintTrail;

    [Tooltip("涂抹轨迹的颜色")]
    public Color trailColor = new Color(0.2f, 0.2f, 0.5f, 0.8f);

    [Tooltip("轨迹宽度")]
    public float trailWidth = 0.1f;

    [Tooltip("轨迹持续时间")]
    public float trailDuration = 2f;

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
    private bool isInitialized = false;  // 【新增】初始化标记

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        // 【修复1】在 Awake 中记录原始位置，确保在 OnEnable 之前执行
        InitializeQuillPosition();
    }

    private void Start()
    {
        // 双重保险：确保位置已记录
        InitializeQuillPosition();

        // 初始隐藏
        HideAll();

        // 【新增】设置涂抹轨迹
        SetupPaintTrail();
    }

    private void OnEnable()
    {
        // 【修复1】确保位置已初始化
        InitializeQuillPosition();

        // 重置桌面状态
        ResetDeskState();

        // 检查 Logo 是否已完成
        if (LetterManager.Instance != null)
        {
            logoCompleted = LetterManager.Instance.hasLogo;
            quillCanDrag = !logoCompleted;
        }
    }

    // ============ 【新增】初始化方法 ============

    /// <summary>
    /// 初始化羽毛笔位置（确保只执行一次）
    /// </summary>
    private void InitializeQuillPosition()
    {
        if (isInitialized) return;

        if (quillPen != null)
        {
            quillOriginalPos = quillPen.transform.localPosition;
            isInitialized = true;
            Debug.Log($"[QuillDeskController] 记录羽毛笔原始位置: {quillOriginalPos}");
        }
    }

    /// <summary>
    /// 设置涂抹轨迹效果
    /// </summary>
    private void SetupPaintTrail()
    {
        // 如果没有指定 TrailRenderer，尝试在羽毛笔上创建一个
        if (paintTrail == null && quillPen != null)
        {
            // 创建笔尖子物体
            GameObject penTipObj = new GameObject("PenTip");
            penTipObj.transform.SetParent(quillPen.transform);
            penTipObj.transform.localPosition = penTipOffset;

            // 添加 TrailRenderer
            paintTrail = penTipObj.AddComponent<TrailRenderer>();
        }

        if (paintTrail != null)
        {
            // 配置轨迹
            paintTrail.time = trailDuration;
            paintTrail.startWidth = trailWidth;
            paintTrail.endWidth = trailWidth * 0.5f;
            paintTrail.material = new Material(Shader.Find("Sprites/Default"));
            paintTrail.startColor = trailColor;
            paintTrail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            paintTrail.sortingOrder = 100; // 确保显示在最上层
            paintTrail.emitting = false;   // 初始不发射
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
        if (quillPen != null && isInitialized)
        {
            quillPen.transform.localPosition = quillOriginalPos;
        }

        // 【新增】清除轨迹
        if (paintTrail != null)
        {
            paintTrail.Clear();
            paintTrail.emitting = false;
        }
    }

    // ============ 桌面点击 - 放置信纸 ============

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

        if (LetterManager.Instance.letterItemData == null) return false;
        if (selectedItem.itemID != LetterManager.Instance.letterItemData.itemID) return false;

        UIManager.Instance.ConsumeSelectedItem();
        PlaceLetter();
        return true;
    }

    private void PlaceLetter()
    {
        Debug.Log("[QuillDeskController] 放置信纸到桌面");

        if (letterOnDesk != null)
        {
            letterOnDesk.SetActive(true);
        }

        if (letterDisplay != null)
        {
            letterDisplay.RefreshDisplay();
        }

        currentState = DeskState.LetterPlaced;

        PlaySound(placeLetterSound);
        OnLetterPlaced?.Invoke();
    }

    // ============ 信纸点击 ============

    public void OnLetterClicked()
    {
        Debug.Log($"[QuillDeskController] 点击信纸，当前状态: {currentState}");

        switch (currentState)
        {
            case DeskState.LetterPlaced:
                TryApplyGlue();
                break;

            case DeskState.GlueApplied:
                TryStickTitle();
                break;

            case DeskState.ReadyForPickup:
                PickupLetter();
                break;
        }
    }

    // ============ 标题流程 ============

    private void TryApplyGlue()
    {
        if (LetterManager.Instance != null && LetterManager.Instance.hasTitle)
        {
            Debug.Log("[QuillDeskController] 标题已完成，无需涂胶水");
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

        UIManager.Instance.ConsumeSelectedItem();

        if (glueEffect != null)
        {
            glueEffect.SetActive(true);
        }

        hasGlueApplied = true;
        currentState = DeskState.GlueApplied;

        PlaySound(applyGlueSound);
        OnGlueApplied?.Invoke();
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

        UIManager.Instance.ConsumeSelectedItem();

        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.SetTitleComplete();
        }

        if (letterDisplay != null)
        {
            letterDisplay.RefreshDisplay();
        }

        if (glueEffect != null)
        {
            glueEffect.SetActive(false);
        }

        hasGlueApplied = false;
        currentState = DeskState.ReadyForPickup;

        PlaySound(stickTitleSound);
        OnTitleStuck?.Invoke();
    }

    // ============ Logo 涂抹流程 ============

    public void OnQuillDragStart()
    {
        if (!quillCanDrag)
        {
            Debug.Log("[QuillDeskController] 羽毛笔不可拖动");
            return;
        }

        if (currentState == DeskState.GlueApplied)
        {
            Debug.Log("[QuillDeskController] 当前在胶水状态，请先完成标题粘贴");
            return;
        }

        if (currentState == DeskState.Empty)
        {
            Debug.Log("[QuillDeskController] 需要先放置信纸");
            return;
        }

        if (LetterManager.Instance != null && LetterManager.Instance.hasLogo)
        {
            quillCanDrag = false;
            Debug.Log("[QuillDeskController] Logo 已完成，羽毛笔不可拖动");
            return;
        }

        isPainting = true;

        // 【新增】开始发射轨迹
        if (paintTrail != null)
        {
            paintTrail.Clear();
            paintTrail.emitting = true;
        }

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

        // 【修复3】计算笔尖的世界坐标位置
        Vector3 penTipWorldPos = GetPenTipWorldPosition();

        // 使用笔尖位置进行检测
        if (paintArea != null && paintArea.OverlapPoint(penTipWorldPos))
        {
            AddPaintProgress();
        }
    }

    /// <summary>
    /// 【新增】获取笔尖的世界坐标位置
    /// </summary>
    private Vector3 GetPenTipWorldPosition()
    {
        if (quillPen == null) return Vector3.zero;

        // 将本地偏移转换为世界坐标
        // 考虑羽毛笔的旋转和缩放
        Vector3 worldOffset = quillPen.transform.TransformVector(penTipOffset);
        return quillPen.transform.position + worldOffset;
    }

    public void OnQuillDragEnd()
    {
        isPainting = false;

        // 【新增】停止发射轨迹
        if (paintTrail != null)
        {
            paintTrail.emitting = false;
        }

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

        UpdatePaintProgressIndicator();

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

        // 停止轨迹
        if (paintTrail != null)
        {
            paintTrail.emitting = false;
        }

        if (quillPen != null)
        {
            quillPen.transform.localPosition = quillOriginalPos;
        }

        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.SetLogoComplete();
        }

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

        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.AddLetterToInventory();
        }

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

    public float GetPaintProgress()
    {
        return currentPaintPercent / requiredPaintPercent;
    }

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

    // 【新增】Scene 视图中绘制笔尖位置
    private void OnDrawGizmos()
    {
        if (!showPenTipGizmo || quillPen == null) return;

        // 绘制笔尖位置
        Vector3 penTipPos = quillPen.transform.position +
                           quillPen.transform.TransformVector(penTipOffset);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(penTipPos, 0.05f);
        Gizmos.DrawLine(quillPen.transform.position, penTipPos);

        // 绘制涂抹区域
        if (paintArea != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(paintArea.bounds.center, paintArea.bounds.size);
        }
    }
}