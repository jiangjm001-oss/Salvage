// Assets/Scripts/GamePlay/Lv2/NoteBook/NotebookController.cs
// 笔记本控制器 - 简化版
// 在编辑器中手动摆放页面位置，脚本只负责切换Sprite

using UnityEngine;
using System;

/// <summary>
/// 笔记本控制器
/// 显示两页（左/右），点击翻页切换内容
/// 
/// 使用方法：
/// 1. 在Scene视图中手动摆放 LeftPage 和 RightPage 到正确位置
/// 2. 配置 pageSprites 数组（按顺序1,2,3...11）
/// 3. 配置点击区域
/// </summary>
public class NotebookController : MonoBehaviour
{
    // ============ 页面配置 ============
    [Header("页面配置")]
    [Tooltip("所有页面图片，按顺序排列（1,2,3,4...11）")]
    public Sprite[] pageSprites;

    [Tooltip("当前显示的跨页索引（0=显示第1-2页）")]
    [SerializeField] private int currentSpreadIndex = 0;

    // ============ 显示组件 ============
    [Header("显示组件（在Scene中手动摆放位置）")]
    [Tooltip("左页 SpriteRenderer")]
    public SpriteRenderer leftPageRenderer;

    [Tooltip("右页 SpriteRenderer")]
    public SpriteRenderer rightPageRenderer;

    // ============ 点击区域 ============
    [Header("点击区域（在Scene中手动摆放位置）")]
    [Tooltip("左侧点击区域（往前翻）")]
    public Collider2D leftClickArea;

    [Tooltip("右侧点击区域（往后翻）")]
    public Collider2D rightClickArea;

    // ============ 音效 ============
    [Header("音效")]
    public string flipPageSound = "page_flip";

    // ============ 调试 ============
    [Header("调试")]
    public bool enableDebugLog = true;

    [Tooltip("在编辑器中实时预览当前页")]
    public bool livePreview = true;

    // ============ 私有变量 ============
    private int totalSpreads;
    private Camera mainCamera;

    // ============ 事件 ============
    public event Action<int> OnPageChanged;

    // ============ 属性 ============
    public int CurrentSpreadIndex => currentSpreadIndex;
    public int TotalSpreads => totalSpreads;

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        HandleInput();
    }

    // ============ 编辑器预览 ============

    private void OnValidate()
    {
        // 在编辑器中修改 currentSpreadIndex 时实时预览
        if (livePreview && pageSprites != null && pageSprites.Length > 0)
        {
            totalSpreads = Mathf.CeilToInt(pageSprites.Length / 2f);
            currentSpreadIndex = Mathf.Clamp(currentSpreadIndex, 0, totalSpreads - 1);
            UpdatePageDisplay();
        }
    }

    // ============ 初始化 ============

    private void Initialize()
    {
        if (pageSprites != null && pageSprites.Length > 0)
        {
            totalSpreads = Mathf.CeilToInt(pageSprites.Length / 2f);
        }
        else
        {
            totalSpreads = 0;
            LogDebug("警告: pageSprites 未配置！");
            return;
        }

        // 确保索引有效
        currentSpreadIndex = Mathf.Clamp(currentSpreadIndex, 0, totalSpreads - 1);

        // 显示初始页面
        UpdatePageDisplay();

        LogDebug($"初始化完成: 共 {pageSprites.Length} 张图片, {totalSpreads} 个跨页");
    }

    // ============ 输入处理 ============

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            CheckClickArea();
        }
    }

    private void CheckClickArea()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // 检查左侧点击区域 - 往前翻
        if (leftClickArea != null && leftClickArea.OverlapPoint(mousePos))
        {
            LogDebug("点击左侧区域");
            FlipToPreviousSpread();
            return;
        }

        // 检查右侧点击区域 - 往后翻
        if (rightClickArea != null && rightClickArea.OverlapPoint(mousePos))
        {
            LogDebug("点击右侧区域");
            FlipToNextSpread();
            return;
        }
    }

    // ============ 翻页逻辑 ============

    /// <summary>
    /// 翻到下一个跨页（点击右侧）
    /// </summary>
    public void FlipToNextSpread()
    {
        if (currentSpreadIndex >= totalSpreads - 1)
        {
            LogDebug("已经是最后一页");
            return;
        }

        currentSpreadIndex++;
        OnFlipPage();
    }

    /// <summary>
    /// 翻到上一个跨页（点击左侧）
    /// </summary>
    public void FlipToPreviousSpread()
    {
        if (currentSpreadIndex <= 0)
        {
            LogDebug("已经是第一页");
            return;
        }

        currentSpreadIndex--;
        OnFlipPage();
    }

    /// <summary>
    /// 跳转到指定跨页
    /// </summary>
    public void GoToSpread(int spreadIndex)
    {
        if (spreadIndex < 0 || spreadIndex >= totalSpreads)
        {
            LogDebug($"无效的跨页索引: {spreadIndex}");
            return;
        }

        currentSpreadIndex = spreadIndex;
        OnFlipPage();
    }

    private void OnFlipPage()
    {
        PlaySound(flipPageSound);
        UpdatePageDisplay();
        OnPageChanged?.Invoke(currentSpreadIndex);
        LogDebug($"翻页到: Spread {currentSpreadIndex} (显示第{currentSpreadIndex * 2 + 1}-{currentSpreadIndex * 2 + 2}页)");
    }

    // ============ 页面显示更新 ============

    private void UpdatePageDisplay()
    {
        if (pageSprites == null || pageSprites.Length == 0) return;

        // 计算当前spread对应的页码索引
        int leftPageIndex = currentSpreadIndex * 2;
        int rightPageIndex = leftPageIndex + 1;

        // 更新左页
        if (leftPageRenderer != null)
        {
            if (leftPageIndex < pageSprites.Length && pageSprites[leftPageIndex] != null)
            {
                leftPageRenderer.sprite = pageSprites[leftPageIndex];
                leftPageRenderer.enabled = true;
            }
            else
            {
                leftPageRenderer.enabled = false;
            }
        }

        // 更新右页
        if (rightPageRenderer != null)
        {
            if (rightPageIndex < pageSprites.Length && pageSprites[rightPageIndex] != null)
            {
                rightPageRenderer.sprite = pageSprites[rightPageIndex];
                rightPageRenderer.enabled = true;
            }
            else
            {
                // 最后一页如果是单数，右页为空
                rightPageRenderer.enabled = false;
            }
        }
    }

    // ============ 音效 ============

    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    // ============ 调试 ============

    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[NotebookController] {message}");
        }
    }

    [ContextMenu("Debug: 打印状态")]
    public void DebugPrintState()
    {
        Debug.Log($"=== NotebookController 状态 ===");
        Debug.Log($"当前Spread: {currentSpreadIndex} / {totalSpreads - 1}");
        Debug.Log($"显示页码: 第{currentSpreadIndex * 2 + 1}页 和 第{currentSpreadIndex * 2 + 2}页");
        Debug.Log($"总图片数: {pageSprites?.Length ?? 0}");

        if (leftPageRenderer != null)
            Debug.Log($"左页Sprite: {leftPageRenderer.sprite?.name ?? "null"}, 启用: {leftPageRenderer.enabled}");
        if (rightPageRenderer != null)
            Debug.Log($"右页Sprite: {rightPageRenderer.sprite?.name ?? "null"}, 启用: {rightPageRenderer.enabled}");
    }
}