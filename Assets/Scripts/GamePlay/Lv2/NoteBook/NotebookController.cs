// Assets/Scripts/GamePlay/Notebook/NotebookController.cs
// 笔记本翻页控制器 - 修复版
// 修复：确保OnEnable时正确显示页面内容
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 笔记本翻页控制器
/// 管理多页笔记本的翻页逻辑和动画
/// </summary>
public class NotebookController : MonoBehaviour
{
    // ============ 页面内容配置 ============
    [System.Serializable]
    public class PageSpread
    {
        [Tooltip("左页内容精灵")]
        public Sprite leftPageSprite;

        [Tooltip("右页内容精灵")]
        public Sprite rightPageSprite;

        [Tooltip("左页描述（可选，用于调试）")]
        public string leftPageDescription;

        [Tooltip("右页描述（可选，用于调试）")]
        public string rightPageDescription;
    }

    [Header("页面配置")]
    [Tooltip("所有页面内容（每组包含左右两页）")]
    public List<PageSpread> pageSpreads = new List<PageSpread>();

    [Tooltip("当前显示的页面组索引")]
    [SerializeField]
    private int currentSpreadIndex = 0;

    // ============ 显示组件引用 ============
    [Header("显示组件")]
    [Tooltip("左页 SpriteRenderer")]
    public SpriteRenderer leftPageRenderer;

    [Tooltip("右页 SpriteRenderer")]
    public SpriteRenderer rightPageRenderer;

    [Tooltip("翻页动画用的页面（可选，不影响基本功能）")]
    public SpriteRenderer flipPageRenderer;

    // ============ 页面位置调整 ============
    [Header("页面位置调整")]
    [Tooltip("左页位置偏移（相对于笔记本中心）")]
    public Vector2 leftPageOffset = new Vector2(-160.3f, 0f);

    [Tooltip("右页位置偏移（相对于笔记本中心）")]
    public Vector2 rightPageOffset = new Vector2(170.8f, 0f);

    [Tooltip("左页缩放")]
    public Vector2 leftPageScale = new Vector2(0.5f, 0.5f);

    [Tooltip("右页缩放")]
    public Vector2 rightPageScale = new Vector2(0.5f, 0.5f);

    // ============ 页面大小调整（可选） ============
    [Header("页面大小调整（可选）")]
    [Tooltip("启用后使用统一缩放")]
    public bool useUniformScale = false;

    [Tooltip("统一缩放值")]
    public float uniformScale = 1f;

    [Tooltip("编辑时实时预览")]
    public bool livePreview = true;

    // ============ 点击区域 ============
    [Header("点击区域")]
    [Tooltip("左侧点击区域 Collider")]
    public BoxCollider2D leftClickArea;

    [Tooltip("右侧点击区域 Collider")]
    public BoxCollider2D rightClickArea;

    [Tooltip("点击区域是否跟随页面位置")]
    public bool clickAreaFollowsPage = true;

    [Tooltip("左侧点击区域大小")]
    public Vector2 leftClickAreaSize = new Vector2(200f, 300f);

    [Tooltip("右侧点击区域大小")]
    public Vector2 rightClickAreaSize = new Vector2(200f, 300f);

    // ============ 翻页动画 ============
    [Header("翻页动画")]
    [Tooltip("翻页动画持续时间")]
    public float flipDuration = 0.4f;

    [Tooltip("翻页动画曲线")]
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("启用淡入淡出效果")]
    public bool enableFadeEffect = true;

    [Tooltip("启用阴影效果")]
    public bool enableShadowEffect = false;

    [Tooltip("阴影渲染器（可选）")]
    public SpriteRenderer shadowRenderer;

    // ============ 音效 ============
    [Header("音效")]
    [Tooltip("翻页音效名称")]
    public string flipSoundName = "Audio/SFX/page_flip";

    [Tooltip("到达边界音效")]
    public string edgeSoundName = "Audio/SFX/page_edge";

    // ============ 事件 ============
    [Header("事件")]
    [Tooltip("页面变化时触发")]
    public UnityEvent<int> OnPageChanged;

    [Tooltip("到达首页时触发")]
    public UnityEvent OnFirstPage;

    [Tooltip("到达末页时触发")]
    public UnityEvent OnLastPage;

    // ============ 调试显示 ============
    [Header("调试")]
    [Tooltip("在Scene视图显示页面边界")]
    public bool showPageBounds = true;

    [Tooltip("在Scene视图显示点击区域")]
    public bool showClickAreas = true;

    [Tooltip("启用调试日志")]
    public bool enableDebugLog = true;

    // ============ 内部状态 ============
    private bool isFlipping = false;
    private Coroutine flipCoroutine;
    private bool isInitialized = false;

    // ============ 属性 ============
    public int CurrentSpreadIndex => currentSpreadIndex;
    public int TotalSpreads => pageSpreads.Count;
    public bool IsFirstSpread => currentSpreadIndex == 0;
    public bool IsLastSpread => currentSpreadIndex >= pageSpreads.Count - 1;
    public bool IsFlipping => isFlipping;

    // ============ 生命周期 ============

    private void Awake()
    {
        LogDebug("Awake called");

        // 初始化阴影
        if (shadowRenderer != null)
        {
            SetSpriteAlpha(shadowRenderer, 0f);
        }

        // 初始化翻页页面
        if (flipPageRenderer != null)
        {
            flipPageRenderer.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        LogDebug("Start called");
        Initialize();
    }

    /// <summary>
    /// 每次启用时重新初始化显示
    /// 这是修复进入ZoomView后不显示的关键！
    /// </summary>
    private void OnEnable()
    {
        LogDebug("OnEnable called - Reinitializing display");

        // 延迟一帧初始化，确保所有组件都已就绪
        StartCoroutine(DelayedInitialize());
    }

    private IEnumerator DelayedInitialize()
    {
        // 等待一帧，确保所有组件激活
        yield return null;

        Initialize();

        // 强制刷新显示
        ForceRefreshDisplay();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    private void Initialize()
    {
        // 验证配置
        if (!ValidateConfiguration())
        {
            LogDebug("Configuration validation failed!");
            return;
        }

        // ⭐ 首先重置透明度（修复编辑器中 Alpha=0 的问题）
        ResetPageAlpha();

        // 应用页面位置和大小
        ApplyPageTransforms();

        // 更新点击区域
        UpdateClickAreas();

        // 显示当前页面
        UpdatePageDisplay();

        isInitialized = true;
        LogDebug($"Initialized successfully. Current spread: {currentSpreadIndex}, Total spreads: {pageSpreads.Count}");
    }

    /// <summary>
    /// 重置页面透明度为1（修复编辑器中误设为0的问题）
    /// </summary>
    private void ResetPageAlpha()
    {
        if (leftPageRenderer != null)
        {
            Color c = leftPageRenderer.color;
            if (c.a < 1f)
            {
                c.a = 1f;
                leftPageRenderer.color = c;
                LogDebug($"Reset left page alpha from {leftPageRenderer.color.a} to 1");
            }
        }

        if (rightPageRenderer != null)
        {
            Color c = rightPageRenderer.color;
            if (c.a < 1f)
            {
                c.a = 1f;
                rightPageRenderer.color = c;
                LogDebug($"Reset right page alpha from {rightPageRenderer.color.a} to 1");
            }
        }
    }

    /// <summary>
    /// 强制刷新显示
    /// </summary>
    public void ForceRefreshDisplay()
    {
        LogDebug("ForceRefreshDisplay called");

        ApplyPageTransforms();
        UpdatePageDisplay();

        // 确保渲染器已启用，并重置透明度为1（修复 Alpha=0 的问题）
        if (leftPageRenderer != null)
        {
            leftPageRenderer.enabled = true;
            // ⭐ 强制重置颜色透明度为1
            Color leftColor = leftPageRenderer.color;
            leftColor.a = 1f;
            leftPageRenderer.color = leftColor;
            LogDebug($"Left page renderer enabled: {leftPageRenderer.enabled}, sprite: {leftPageRenderer.sprite?.name ?? "null"}, alpha: {leftPageRenderer.color.a}");
        }

        if (rightPageRenderer != null)
        {
            rightPageRenderer.enabled = true;
            // ⭐ 强制重置颜色透明度为1
            Color rightColor = rightPageRenderer.color;
            rightColor.a = 1f;
            rightPageRenderer.color = rightColor;
            LogDebug($"Right page renderer enabled: {rightPageRenderer.enabled}, sprite: {rightPageRenderer.sprite?.name ?? "null"}, alpha: {rightPageRenderer.color.a}");
        }
    }

    private void Update()
    {
        // 检测鼠标点击
        if (Input.GetMouseButtonDown(0) && !isFlipping)
        {
            CheckClickArea();
        }
    }

    // ============ 配置验证 ============

    /// <summary>
    /// 验证配置是否正确
    /// </summary>
    private bool ValidateConfiguration()
    {
        bool isValid = true;

        if (leftPageRenderer == null)
        {
            Debug.LogError("[NotebookController] ⚠️ leftPageRenderer 未配置！请在 Inspector 中拖入左页 SpriteRenderer");
            isValid = false;
        }

        if (rightPageRenderer == null)
        {
            Debug.LogError("[NotebookController] ⚠️ rightPageRenderer 未配置！请在 Inspector 中拖入右页 SpriteRenderer");
            isValid = false;
        }

        if (pageSpreads == null || pageSpreads.Count == 0)
        {
            Debug.LogError("[NotebookController] ⚠️ pageSpreads 为空！请配置页面内容");
            isValid = false;
        }
        else
        {
            // 检查每个页面配置
            for (int i = 0; i < pageSpreads.Count; i++)
            {
                var spread = pageSpreads[i];
                if (spread.leftPageSprite == null && spread.rightPageSprite == null)
                {
                    Debug.LogWarning($"[NotebookController] ⚠️ PageSpread[{i}] 左右页都没有配置精灵！");
                }
            }
        }

        if (leftClickArea == null)
        {
            Debug.LogWarning("[NotebookController] leftClickArea 未配置，将使用射线检测");
        }

        if (rightClickArea == null)
        {
            Debug.LogWarning("[NotebookController] rightClickArea 未配置，将使用射线检测");
        }

        return isValid;
    }

    // ============ 页面位置和大小应用 ============

    /// <summary>
    /// 应用页面位置和大小设置
    /// </summary>
    public void ApplyPageTransforms()
    {
        // 计算实际缩放
        Vector2 leftScale = useUniformScale ? Vector2.one * uniformScale : leftPageScale;
        Vector2 rightScale = useUniformScale ? Vector2.one * uniformScale : rightPageScale;

        // 应用左页变换
        if (leftPageRenderer != null)
        {
            leftPageRenderer.transform.localPosition = new Vector3(leftPageOffset.x, leftPageOffset.y, 0);
            leftPageRenderer.transform.localScale = new Vector3(leftScale.x, leftScale.y, 1);
            LogDebug($"Left page position: {leftPageRenderer.transform.localPosition}, scale: {leftPageRenderer.transform.localScale}");
        }

        // 应用右页变换
        if (rightPageRenderer != null)
        {
            rightPageRenderer.transform.localPosition = new Vector3(rightPageOffset.x, rightPageOffset.y, 0);
            rightPageRenderer.transform.localScale = new Vector3(rightScale.x, rightScale.y, 1);
            LogDebug($"Right page position: {rightPageRenderer.transform.localPosition}, scale: {rightPageRenderer.transform.localScale}");
        }
    }

    /// <summary>
    /// 更新点击区域位置和大小
    /// </summary>
    public void UpdateClickAreas()
    {
        if (clickAreaFollowsPage)
        {
            // 左侧点击区域
            if (leftClickArea != null)
            {
                leftClickArea.transform.localPosition = new Vector3(leftPageOffset.x, leftPageOffset.y, 0);
                leftClickArea.size = leftClickAreaSize;
            }

            // 右侧点击区域
            if (rightClickArea != null)
            {
                rightClickArea.transform.localPosition = new Vector3(rightPageOffset.x, rightPageOffset.y, 0);
                rightClickArea.size = rightClickAreaSize;
            }
        }
    }

    // ============ 页面显示更新 ============

    /// <summary>
    /// 更新当前页面显示
    /// </summary>
    private void UpdatePageDisplay()
    {
        if (pageSpreads == null || pageSpreads.Count == 0)
        {
            LogDebug("No page spreads configured!");
            return;
        }

        // 确保索引在有效范围内
        currentSpreadIndex = Mathf.Clamp(currentSpreadIndex, 0, pageSpreads.Count - 1);

        PageSpread currentSpread = pageSpreads[currentSpreadIndex];

        // 更新左页
        if (leftPageRenderer != null)
        {
            leftPageRenderer.sprite = currentSpread.leftPageSprite;
            leftPageRenderer.enabled = currentSpread.leftPageSprite != null;

            // ⭐ 强制重置透明度为1（修复 Alpha=0 的问题）
            Color leftColor = leftPageRenderer.color;
            leftColor.a = 1f;
            leftPageRenderer.color = leftColor;

            LogDebug($"Updated left page: {currentSpread.leftPageSprite?.name ?? "null"}, enabled: {leftPageRenderer.enabled}, alpha: {leftPageRenderer.color.a}");
        }

        // 更新右页
        if (rightPageRenderer != null)
        {
            rightPageRenderer.sprite = currentSpread.rightPageSprite;
            rightPageRenderer.enabled = currentSpread.rightPageSprite != null;

            // ⭐ 强制重置透明度为1（修复 Alpha=0 的问题）
            Color rightColor = rightPageRenderer.color;
            rightColor.a = 1f;
            rightPageRenderer.color = rightColor;

            LogDebug($"Updated right page: {currentSpread.rightPageSprite?.name ?? "null"}, enabled: {rightPageRenderer.enabled}, alpha: {rightPageRenderer.color.a}");
        }

        LogDebug($"Page display updated: Spread {currentSpreadIndex + 1}/{pageSpreads.Count}");
    }

    // ============ 点击检测 ============

    /// <summary>
    /// 检测点击区域
    /// </summary>
    private void CheckClickArea()
    {
        if (Camera.main == null) return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 优先使用Collider检测
        if (leftClickArea != null && rightClickArea != null)
        {
            if (leftClickArea.OverlapPoint(mouseWorldPos))
            {
                LogDebug("Left click area clicked - Previous page");
                TryFlipToPrevious();
                return;
            }

            if (rightClickArea.OverlapPoint(mouseWorldPos))
            {
                LogDebug("Right click area clicked - Next page");
                TryFlipToNext();
                return;
            }
        }
        else
        {
            // 备用：基于页面位置的简单检测
            Vector3 localPos = transform.InverseTransformPoint(mouseWorldPos);

            // 检测是否在书本区域内（简化判断）
            float bookHalfWidth = Mathf.Max(Mathf.Abs(leftPageOffset.x), Mathf.Abs(rightPageOffset.x)) + 100f;
            float bookHalfHeight = 200f;

            if (Mathf.Abs(localPos.x) < bookHalfWidth && Mathf.Abs(localPos.y) < bookHalfHeight)
            {
                if (localPos.x < 0)
                {
                    LogDebug("Left side clicked (fallback detection) - Previous page");
                    TryFlipToPrevious();
                }
                else
                {
                    LogDebug("Right side clicked (fallback detection) - Next page");
                    TryFlipToNext();
                }
            }
        }
    }

    // ============ 翻页逻辑 ============

    /// <summary>
    /// 尝试翻到下一页
    /// </summary>
    public void TryFlipToNext()
    {
        if (isFlipping)
        {
            LogDebug("Already flipping, ignoring");
            return;
        }

        if (IsLastSpread)
        {
            LogDebug("Already at last spread");
            PlayEdgeFeedback();
            OnLastPage?.Invoke();
            return;
        }

        flipCoroutine = StartCoroutine(FlipToPage(currentSpreadIndex + 1, true));
    }

    /// <summary>
    /// 尝试翻到上一页
    /// </summary>
    public void TryFlipToPrevious()
    {
        if (isFlipping)
        {
            LogDebug("Already flipping, ignoring");
            return;
        }

        if (IsFirstSpread)
        {
            LogDebug("Already at first spread");
            PlayEdgeFeedback();
            OnFirstPage?.Invoke();
            return;
        }

        flipCoroutine = StartCoroutine(FlipToPage(currentSpreadIndex - 1, false));
    }

    /// <summary>
    /// 直接跳转到指定页面（无动画）
    /// </summary>
    public void GoToPage(int spreadIndex)
    {
        if (spreadIndex < 0 || spreadIndex >= pageSpreads.Count) return;

        currentSpreadIndex = spreadIndex;
        UpdatePageDisplay();
        OnPageChanged?.Invoke(currentSpreadIndex);
    }

    /// <summary>
    /// 翻页协程
    /// </summary>
    private IEnumerator FlipToPage(int targetIndex, bool isForward)
    {
        isFlipping = true;

        // 播放翻页音效
        PlayFlipSound();

        if (enableFadeEffect)
        {
            // 淡入淡出动画
            yield return StartCoroutine(FadeFlipAnimation(targetIndex, isForward));
        }
        else
        {
            // 简单切换
            yield return new WaitForSeconds(flipDuration * 0.5f);
            currentSpreadIndex = targetIndex;
            UpdatePageDisplay();
            yield return new WaitForSeconds(flipDuration * 0.5f);
        }

        OnPageChanged?.Invoke(currentSpreadIndex);

        // 检查是否到达边界
        if (IsFirstSpread)
        {
            OnFirstPage?.Invoke();
        }
        else if (IsLastSpread)
        {
            OnLastPage?.Invoke();
        }

        isFlipping = false;
    }

    /// <summary>
    /// 淡入淡出翻页动画
    /// </summary>
    private IEnumerator FadeFlipAnimation(int targetIndex, bool isForward)
    {
        float halfDuration = flipDuration * 0.5f;

        // 淡出当前页面
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / halfDuration);
            float alpha = 1f - t;

            if (leftPageRenderer != null) SetSpriteAlpha(leftPageRenderer, alpha);
            if (rightPageRenderer != null) SetSpriteAlpha(rightPageRenderer, alpha);

            // 阴影效果
            if (enableShadowEffect && shadowRenderer != null)
            {
                SetSpriteAlpha(shadowRenderer, t * 0.3f);
            }

            yield return null;
        }

        // 切换页面
        currentSpreadIndex = targetIndex;
        UpdatePageDisplay();

        // 淡入新页面
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / halfDuration);
            float alpha = t;

            if (leftPageRenderer != null) SetSpriteAlpha(leftPageRenderer, alpha);
            if (rightPageRenderer != null) SetSpriteAlpha(rightPageRenderer, alpha);

            // 阴影效果
            if (enableShadowEffect && shadowRenderer != null)
            {
                SetSpriteAlpha(shadowRenderer, (1f - t) * 0.3f);
            }

            yield return null;
        }

        // 确保最终状态正确
        if (leftPageRenderer != null) SetSpriteAlpha(leftPageRenderer, 1f);
        if (rightPageRenderer != null) SetSpriteAlpha(rightPageRenderer, 1f);
        if (shadowRenderer != null) SetSpriteAlpha(shadowRenderer, 0f);
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 设置精灵透明度
    /// </summary>
    private void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        Color color = renderer.color;
        color.a = alpha;
        renderer.color = color;
    }

    /// <summary>
    /// 播放翻页音效
    /// </summary>
    private void PlayFlipSound()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(flipSoundName))
        {
            AudioManager.Instance.PlaySFX(flipSoundName);
        }
    }

    /// <summary>
    /// 播放边界反馈
    /// </summary>
    private void PlayEdgeFeedback()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(edgeSoundName))
        {
            AudioManager.Instance.PlaySFX(edgeSoundName);
        }

        // 可以添加抖动效果
        StartCoroutine(EdgeBounceEffect());
    }

    /// <summary>
    /// 边界弹跳效果
    /// </summary>
    private IEnumerator EdgeBounceEffect()
    {
        SpriteRenderer targetRenderer = IsFirstSpread ? leftPageRenderer : rightPageRenderer;
        if (targetRenderer == null) yield break;

        Vector3 originalPos = targetRenderer.transform.localPosition;
        float bounceDistance = 5f;
        float bounceDuration = 0.15f;

        // 向外弹
        float elapsed = 0f;
        Vector3 bounceDirection = IsFirstSpread ? Vector3.left : Vector3.right;

        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Sin(elapsed / bounceDuration * Mathf.PI);
            targetRenderer.transform.localPosition = originalPos + bounceDirection * bounceDistance * t;
            yield return null;
        }

        targetRenderer.transform.localPosition = originalPos;
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    private void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[NotebookController] {message}");
        }
    }

    // ============ 存档相关 ============

    /// <summary>
    /// 获取存档数据
    /// </summary>
    public int GetSaveData()
    {
        return currentSpreadIndex;
    }

    /// <summary>
    /// 加载存档数据
    /// </summary>
    public void LoadSaveData(int savedIndex)
    {
        currentSpreadIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, pageSpreads.Count - 1));
        UpdatePageDisplay();
    }

    // ============ 编辑器辅助 ============

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 编辑器中实时预览
        if (livePreview && !Application.isPlaying)
        {
            ApplyPageTransforms();
            UpdateClickAreas();

            // 使用 delayCall 延迟更新精灵，避免 SendMessage 警告
            UnityEditor.EditorApplication.delayCall += DelayedSpriteUpdate;
        }
    }

    /// <summary>
    /// 延迟更新精灵（避免 OnValidate 中的 SendMessage 警告）
    /// </summary>
    private void DelayedSpriteUpdate()
    {
        // 检查对象是否还存在（可能已被销毁）
        if (this == null) return;
        if (leftPageRenderer == null || rightPageRenderer == null) return;
        if (pageSpreads == null || pageSpreads.Count == 0) return;

        int idx = Mathf.Clamp(currentSpreadIndex, 0, pageSpreads.Count - 1);
        var spread = pageSpreads[idx];

        leftPageRenderer.sprite = spread.leftPageSprite;
        rightPageRenderer.sprite = spread.rightPageSprite;
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制页面边界
        if (showPageBounds)
        {
            // 左页边界
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // 橙色
            Vector3 leftCenter = transform.position + new Vector3(leftPageOffset.x, leftPageOffset.y, 0f);
            Vector3 leftSize = new Vector3(leftClickAreaSize.x, leftClickAreaSize.y, 1f);
            Gizmos.DrawWireCube(leftCenter, leftSize);

            // 右页边界
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f); // 蓝色
            Vector3 rightCenter = transform.position + new Vector3(rightPageOffset.x, rightPageOffset.y, 0f);
            Vector3 rightSize = new Vector3(rightClickAreaSize.x, rightClickAreaSize.y, 1f);
            Gizmos.DrawWireCube(rightCenter, rightSize);
        }

        // 绘制点击区域
        if (showClickAreas)
        {
            // 左侧点击区域
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 绿色
            Vector3 leftClickCenter = transform.position + new Vector3(leftPageOffset.x, leftPageOffset.y, 0f);
            Gizmos.DrawCube(leftClickCenter, new Vector3(leftClickAreaSize.x, leftClickAreaSize.y, 1f));

            // 右侧点击区域
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 红色
            Vector3 rightClickCenter = transform.position + new Vector3(rightPageOffset.x, rightPageOffset.y, 0f);
            Gizmos.DrawCube(rightClickCenter, new Vector3(rightClickAreaSize.x, rightClickAreaSize.y, 1f));
        }

        // 绘制中心线
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            transform.position + Vector3.up * 200f,
            transform.position + Vector3.down * 200f
        );
    }
#endif

    // ============ 右键菜单调试 ============

    [ContextMenu("Debug: 强制刷新显示")]
    private void DebugForceRefresh()
    {
        ForceRefreshDisplay();
    }

    [ContextMenu("Debug: 打印状态")]
    private void DebugPrintState()
    {
        Debug.Log($"[NotebookController] === 状态信息 ===");
        Debug.Log($"  当前页面索引: {currentSpreadIndex}");
        Debug.Log($"  总页面数: {pageSpreads?.Count ?? 0}");
        Debug.Log($"  是否正在翻页: {isFlipping}");
        Debug.Log($"  是否已初始化: {isInitialized}");

        if (leftPageRenderer != null)
        {
            Debug.Log($"  左页渲染器: enabled={leftPageRenderer.enabled}, sprite={leftPageRenderer.sprite?.name ?? "null"}");
            Debug.Log($"    位置: {leftPageRenderer.transform.localPosition}");
            Debug.Log($"    缩放: {leftPageRenderer.transform.localScale}");
        }
        else
        {
            Debug.Log("  左页渲染器: 未配置!");
        }

        if (rightPageRenderer != null)
        {
            Debug.Log($"  右页渲染器: enabled={rightPageRenderer.enabled}, sprite={rightPageRenderer.sprite?.name ?? "null"}");
            Debug.Log($"    位置: {rightPageRenderer.transform.localPosition}");
            Debug.Log($"    缩放: {rightPageRenderer.transform.localScale}");
        }
        else
        {
            Debug.Log("  右页渲染器: 未配置!");
        }

        if (pageSpreads != null)
        {
            for (int i = 0; i < pageSpreads.Count; i++)
            {
                var spread = pageSpreads[i];
                Debug.Log($"  PageSpread[{i}]: Left={spread.leftPageSprite?.name ?? "null"}, Right={spread.rightPageSprite?.name ?? "null"}");
            }
        }
    }

    [ContextMenu("Debug: 下一页")]
    private void DebugNextPage()
    {
        TryFlipToNext();
    }

    [ContextMenu("Debug: 上一页")]
    private void DebugPreviousPage()
    {
        TryFlipToPrevious();
    }

    [ContextMenu("重置页面位置")]
    private void ResetPagePositions()
    {
        leftPageOffset = new Vector2(-160f, 0f);
        rightPageOffset = new Vector2(160f, 0f);
        leftPageScale = Vector2.one;
        rightPageScale = Vector2.one;
        useUniformScale = false;
        uniformScale = 1f;
        ApplyPageTransforms();
    }

    [ContextMenu("自动计算页面位置")]
    private void AutoCalculatePositions()
    {
        if (leftPageRenderer != null && leftPageRenderer.sprite != null)
        {
            float halfWidth = leftPageRenderer.sprite.bounds.size.x * leftPageScale.x * 0.5f;
            leftPageOffset.x = -halfWidth - 10f; // 10像素间距
        }

        if (rightPageRenderer != null && rightPageRenderer.sprite != null)
        {
            float halfWidth = rightPageRenderer.sprite.bounds.size.x * rightPageScale.x * 0.5f;
            rightPageOffset.x = halfWidth + 10f;
        }

        ApplyPageTransforms();
        Debug.Log($"[NotebookController] 自动计算位置: Left={leftPageOffset}, Right={rightPageOffset}");
    }

    [ContextMenu("⭐ 自动创建点击区域")]
    private void AutoCreateClickAreas()
    {
        // 创建左侧点击区域
        if (leftClickArea == null)
        {
            GameObject leftClickObj = new GameObject("LeftClickArea");
            leftClickObj.transform.SetParent(transform);
            leftClickObj.transform.localPosition = new Vector3(leftPageOffset.x, leftPageOffset.y, 0);
            leftClickObj.transform.localRotation = Quaternion.identity;
            leftClickObj.transform.localScale = Vector3.one;

            leftClickArea = leftClickObj.AddComponent<BoxCollider2D>();
            leftClickArea.size = leftClickAreaSize;
            leftClickArea.isTrigger = true;

            Debug.Log("[NotebookController] ✓ 已创建 LeftClickArea");
        }

        // 创建右侧点击区域
        if (rightClickArea == null)
        {
            GameObject rightClickObj = new GameObject("RightClickArea");
            rightClickObj.transform.SetParent(transform);
            rightClickObj.transform.localPosition = new Vector3(rightPageOffset.x, rightPageOffset.y, 0);
            rightClickObj.transform.localRotation = Quaternion.identity;
            rightClickObj.transform.localScale = Vector3.one;

            rightClickArea = rightClickObj.AddComponent<BoxCollider2D>();
            rightClickArea.size = rightClickAreaSize;
            rightClickArea.isTrigger = true;

            Debug.Log("[NotebookController] ✓ 已创建 RightClickArea");
        }

        Debug.Log("[NotebookController] ⭐ 点击区域创建完成！请在 Scene 视图中调整大小");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("⭐ 重置页面透明度为1")]
    private void DebugResetAlpha()
    {
        if (leftPageRenderer != null)
        {
            Color c = leftPageRenderer.color;
            c.a = 1f;
            leftPageRenderer.color = c;
            Debug.Log($"[NotebookController] LeftPage alpha 已重置为 1");
        }

        if (rightPageRenderer != null)
        {
            Color c = rightPageRenderer.color;
            c.a = 1f;
            rightPageRenderer.color = c;
            Debug.Log($"[NotebookController] RightPage alpha 已重置为 1");
        }
    }
}