// Assets/Scripts/GamePlay/Letter/LetterDisplay.cs
// 信纸分层显示组件
// 根据 LetterManager 的状态自动显示/隐藏各个图层
using UnityEngine;

/// <summary>
/// 信纸分层显示组件
/// 管理信纸的多个图层（底纸、收件人、标题、Logo）的显示状态
/// 
/// 使用方式：
/// 1. 将此组件添加到信纸根物体上
/// 2. 在 Inspector 中配置各个图层的 GameObject 引用
/// 3. 调用 RefreshDisplay() 或自动在 OnEnable 时刷新
/// 
/// 图层结构：
/// - basePaper: 信纸底图（始终显示）
/// - recipientOverlay: 收件人图层（打字机完成后显示）
/// - titleOverlay: 标题图层（羽毛笔桌面粘贴后显示）
/// - logoOverlay: Logo图层（羽毛笔涂抹后显示）
/// </summary>
public class LetterDisplay : MonoBehaviour
{
    // ============ 图层引用 ============
    [Header("图层引用")]
    [Tooltip("信纸底图（始终显示）")]
    public GameObject basePaper;

    [Tooltip("收件人图层（打字机完成后显示）")]
    public GameObject recipientOverlay;

    [Tooltip("标题图层（羽毛笔桌面粘贴后显示）")]
    public GameObject titleOverlay;

    [Tooltip("Logo图层（羽毛笔涂抹后显示）")]
    public GameObject logoOverlay;

    // ============ 设置 ============
    [Header("设置")]
    [Tooltip("启用时自动刷新显示")]
    public bool autoRefreshOnEnable = true;

    [Tooltip("是否监听 LetterManager 的状态变化事件")]
    public bool listenToManagerEvents = true;

    // ============ 调试 ============
    [Header("调试信息（只读）")]
    [SerializeField] private bool _hasRecipient;
    [SerializeField] private bool _hasTitle;
    [SerializeField] private bool _hasLogo;

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        // 初始化时确保底纸显示
        if (basePaper != null)
        {
            basePaper.SetActive(true);
        }
    }

    private void OnEnable()
    {
        // 订阅 LetterManager 事件
        if (listenToManagerEvents && LetterManager.Instance != null)
        {
            LetterManager.Instance.OnLetterStateChanged.AddListener(OnStateChanged);
        }

        // 自动刷新
        if (autoRefreshOnEnable)
        {
            RefreshDisplay();
        }
    }

    private void OnDisable()
    {
        // 取消订阅事件
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.OnLetterStateChanged.RemoveListener(OnStateChanged);
        }
    }

    // ============ 事件处理 ============

    private void OnStateChanged(int stateIndex)
    {
        RefreshDisplay();
    }

    // ============ 核心方法 ============

    /// <summary>
    /// 刷新显示状态
    /// 根据 LetterManager 的当前状态更新各图层的显示/隐藏
    /// </summary>
    public void RefreshDisplay()
    {
        if (LetterManager.Instance == null)
        {
            Debug.LogWarning("[LetterDisplay] LetterManager.Instance 为空，无法刷新显示");
            SetAllOverlaysHidden();
            return;
        }

        // 获取当前状态
        bool hasRecipient = LetterManager.Instance.hasRecipient;
        bool hasTitle = LetterManager.Instance.hasTitle;
        bool hasLogo = LetterManager.Instance.hasLogo;

        // 更新调试信息
        _hasRecipient = hasRecipient;
        _hasTitle = hasTitle;
        _hasLogo = hasLogo;

        // 更新图层显示
        UpdateLayerVisibility(basePaper, true); // 底纸始终显示
        UpdateLayerVisibility(recipientOverlay, hasRecipient);
        UpdateLayerVisibility(titleOverlay, hasTitle);
        UpdateLayerVisibility(logoOverlay, hasLogo);

        Debug.Log($"[LetterDisplay] 刷新显示: R={hasRecipient}, T={hasTitle}, L={hasLogo}");
    }

    /// <summary>
    /// 手动设置显示状态（不依赖 LetterManager）
    /// </summary>
    public void SetDisplayState(bool showRecipient, bool showTitle, bool showLogo)
    {
        _hasRecipient = showRecipient;
        _hasTitle = showTitle;
        _hasLogo = showLogo;

        UpdateLayerVisibility(basePaper, true);
        UpdateLayerVisibility(recipientOverlay, showRecipient);
        UpdateLayerVisibility(titleOverlay, showTitle);
        UpdateLayerVisibility(logoOverlay, showLogo);

        Debug.Log($"[LetterDisplay] 手动设置: R={showRecipient}, T={showTitle}, L={showLogo}");
    }

    /// <summary>
    /// 隐藏所有覆盖层（只保留底纸）
    /// </summary>
    public void SetAllOverlaysHidden()
    {
        UpdateLayerVisibility(basePaper, true);
        UpdateLayerVisibility(recipientOverlay, false);
        UpdateLayerVisibility(titleOverlay, false);
        UpdateLayerVisibility(logoOverlay, false);

        _hasRecipient = false;
        _hasTitle = false;
        _hasLogo = false;
    }

    /// <summary>
    /// 显示所有图层
    /// </summary>
    public void SetAllLayersVisible()
    {
        UpdateLayerVisibility(basePaper, true);
        UpdateLayerVisibility(recipientOverlay, true);
        UpdateLayerVisibility(titleOverlay, true);
        UpdateLayerVisibility(logoOverlay, true);

        _hasRecipient = true;
        _hasTitle = true;
        _hasLogo = true;
    }

    // ============ 单独图层控制 ============

    /// <summary>
    /// 显示收件人图层
    /// </summary>
    public void ShowRecipient()
    {
        UpdateLayerVisibility(recipientOverlay, true);
        _hasRecipient = true;
    }

    /// <summary>
    /// 显示标题图层
    /// </summary>
    public void ShowTitle()
    {
        UpdateLayerVisibility(titleOverlay, true);
        _hasTitle = true;
    }

    /// <summary>
    /// 显示 Logo 图层
    /// </summary>
    public void ShowLogo()
    {
        UpdateLayerVisibility(logoOverlay, true);
        _hasLogo = true;
    }

    // ============ 辅助方法 ============

    private void UpdateLayerVisibility(GameObject layer, bool visible)
    {
        if (layer != null && layer.activeSelf != visible)
        {
            layer.SetActive(visible);
        }
    }

    // ============ 配置验证 ============

    /// <summary>
    /// 验证配置是否正确
    /// </summary>
    public bool ValidateConfiguration()
    {
        bool isValid = true;

        if (basePaper == null)
        {
            Debug.LogError("[LetterDisplay] ⚠️ basePaper 未配置！");
            isValid = false;
        }

        if (recipientOverlay == null)
        {
            Debug.LogWarning("[LetterDisplay] ⚠️ recipientOverlay 未配置");
        }

        if (titleOverlay == null)
        {
            Debug.LogWarning("[LetterDisplay] ⚠️ titleOverlay 未配置");
        }

        if (logoOverlay == null)
        {
            Debug.LogWarning("[LetterDisplay] ⚠️ logoOverlay 未配置");
        }

        return isValid;
    }

    // ============ 调试方法 ============

    [ContextMenu("Debug: 刷新显示")]
    private void DebugRefresh()
    {
        RefreshDisplay();
    }

    [ContextMenu("Debug: 显示所有图层")]
    private void DebugShowAll()
    {
        SetAllLayersVisible();
    }

    [ContextMenu("Debug: 隐藏所有覆盖层")]
    private void DebugHideOverlays()
    {
        SetAllOverlaysHidden();
    }

    [ContextMenu("Debug: 验证配置")]
    private void DebugValidate()
    {
        ValidateConfiguration();
    }

    [ContextMenu("Debug: 打印状态")]
    private void DebugPrintState()
    {
        Debug.Log($"[LetterDisplay] 当前状态: R={_hasRecipient}, T={_hasTitle}, L={_hasLogo}");
        Debug.Log($"[LetterDisplay] 图层配置: base={basePaper != null}, recipient={recipientOverlay != null}, title={titleOverlay != null}, logo={logoOverlay != null}");
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        // 在编辑器中修改时自动刷新（仅在播放模式）
        if (Application.isPlaying && autoRefreshOnEnable)
        {
            RefreshDisplay();
        }
    }
}