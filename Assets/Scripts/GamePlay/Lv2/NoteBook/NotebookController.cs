// Assets/Scripts/GamePlay/Notebook/NotebookController.cs
// 笔记本翻页控制器 - 支持左右翻页和翻页动画
// 更新：支持页面位置、大小调整
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

    [Tooltip("翻页动画用的页面（可选）")]
    public SpriteRenderer flipPageRenderer;

    // ============ 页面位置和大小调整 ============
    [Header("页面位置调整")]
    [Tooltip("左页位置偏移（相对于笔记本中心）")]
    public Vector2 leftPageOffset = new Vector2(-150f, 0f);

    [Tooltip("右页位置偏移（相对于笔记本中心）")]
    public Vector2 rightPageOffset = new Vector2(150f, 0f);

    [Tooltip("左页缩放")]
    public Vector2 leftPageScale = Vector2.one;

    [Tooltip("右页缩放")]
    public Vector2 rightPageScale = Vector2.one;

    [Header("页面大小调整（可选）")]
    [Tooltip("启用统一缩放（同时调整左右页）")]
    public bool useUniformScale = false;

    [Tooltip("统一缩放值")]
    public float uniformScale = 1f;

    [Tooltip("在编辑器中实时预览位置变化")]
    public bool livePreview = true;

    // ============ 点击区域 ============
    [Header("点击区域")]
    [Tooltip("左侧翻页区域 Collider")]
    public Collider2D leftClickArea;

    [Tooltip("右侧翻页区域 Collider")]
    public Collider2D rightClickArea;

    [Tooltip("点击区域跟随页面位置")]
    public bool clickAreaFollowsPage = true;

    [Tooltip("左侧点击区域大小")]
    public Vector2 leftClickAreaSize = new Vector2(200f, 300f);

    [Tooltip("右侧点击区域大小")]
    public Vector2 rightClickAreaSize = new Vector2(200f, 300f);

    // ============ 翻页动画设置 ============
    [Header("翻页动画")]
    [Tooltip("启用翻页动画")]
    public bool enableFlipAnimation = true;

    [Tooltip("翻页动画时间")]
    [Range(0.1f, 1f)]
    public float flipDuration = 0.4f;

    [Tooltip("翻页动画曲线")]
    public AnimationCurve flipCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("翻页时的最大倾斜角度")]
    [Range(0f, 45f)]
    public float maxTiltAngle = 15f;

    [Tooltip("翻页时的阴影淡入淡出")]
    public bool enableShadowEffect = true;

    [Tooltip("阴影精灵（可选）")]
    public SpriteRenderer shadowRenderer;

    [Tooltip("阴影最大透明度")]
    [Range(0f, 1f)]
    public float maxShadowAlpha = 0.3f;

    // ============ 页面淡入淡出设置 ============
    [Header("页面淡入淡出")]
    [Tooltip("启用页面淡入淡出")]
    public bool enableFadeEffect = true;

    [Tooltip("淡入淡出时间")]
    [Range(0.1f, 0.5f)]
    public float fadeDuration = 0.2f;

    // ============ 页码指示器 ============
    [Header("页码指示器（可选）")]
    [Tooltip("页码文本组件")]
    public TMPro.TextMeshPro pageNumberText;

    [Tooltip("页码格式，如 '{0}/{1}'")]
    public string pageNumberFormat = "{0}/{1}";

    // ============ 音效设置 ============
    [Header("音效设置")]
    [Tooltip("翻页音效")]
    public string flipSoundName = "Audio/SFX/page_flip";

    [Tooltip("到达首页/末页时的音效")]
    public string edgeSoundName = "Audio/SFX/page_edge";

    // ============ 事件 ============
    [Header("事件")]
    [Tooltip("翻页时触发")]
    public UnityEvent<int> OnPageChanged;

    [Tooltip("到达首页时触发")]
    public UnityEvent OnFirstPage;

    [Tooltip("到达末页时触发")]
    public UnityEvent OnLastPage;

    // ============ 调试显示 ============
    [Header("调试显示")]
    [Tooltip("在Scene视图显示页面边界")]
    public bool showPageBounds = true;

    [Tooltip("在Scene视图显示点击区域")]
    public bool showClickAreas = true;

    // ============ 内部状态 ============
    private bool isFlipping = false;
    private Coroutine flipCoroutine;
    private Vector3 leftPageBasePosition;
    private Vector3 rightPageBasePosition;

    // ============ 属性 ============
    public int CurrentSpreadIndex => currentSpreadIndex;
    public int TotalSpreads => pageSpreads.Count;
    public bool IsFirstSpread => currentSpreadIndex == 0;
    public bool IsLastSpread => currentSpreadIndex >= pageSpreads.Count - 1;
    public bool IsFlipping => isFlipping;

    // ============ 生命周期 ============

    private void Awake()
    {
        // 记录基础位置
        if (leftPageRenderer != null)
        {
            leftPageBasePosition = leftPageRenderer.transform.localPosition;
        }
        if (rightPageRenderer != null)
        {
            rightPageBasePosition = rightPageRenderer.transform.localPosition;
        }

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
        // 应用初始位置和大小
        ApplyPageTransforms();

        // 更新点击区域
        UpdateClickAreas();

        // 显示初始页面
        UpdatePageDisplay();
        UpdatePageNumber();
    }

    private void Update()
    {
        // 检测鼠标点击
        if (Input.GetMouseButtonDown(0) && !isFlipping)
        {
            CheckClickArea();
        }
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
            leftPageRenderer.transform.localPosition = new Vector3(leftPageOffset.x, leftPageOffset.y, 0f);
            leftPageRenderer.transform.localScale = new Vector3(leftScale.x, leftScale.y, 1f);
        }

        // 应用右页变换
        if (rightPageRenderer != null)
        {
            rightPageRenderer.transform.localPosition = new Vector3(rightPageOffset.x, rightPageOffset.y, 0f);
            rightPageRenderer.transform.localScale = new Vector3(rightScale.x, rightScale.y, 1f);
        }

        // 更新翻页页面位置（如果有）
        if (flipPageRenderer != null)
        {
            flipPageRenderer.transform.localScale = new Vector3(rightScale.x, rightScale.y, 1f);
        }
    }

    /// <summary>
    /// 更新点击区域位置和大小
    /// </summary>
    public void UpdateClickAreas()
    {
        if (clickAreaFollowsPage)
        {
            // 左侧点击区域跟随左页
            if (leftClickArea != null)
            {
                leftClickArea.transform.localPosition = new Vector3(leftPageOffset.x, leftPageOffset.y, 0f);

                BoxCollider2D leftBox = leftClickArea as BoxCollider2D;
                if (leftBox != null)
                {
                    leftBox.size = leftClickAreaSize;
                }
            }

            // 右侧点击区域跟随右页
            if (rightClickArea != null)
            {
                rightClickArea.transform.localPosition = new Vector3(rightPageOffset.x, rightPageOffset.y, 0f);

                BoxCollider2D rightBox = rightClickArea as BoxCollider2D;
                if (rightBox != null)
                {
                    rightBox.size = rightClickAreaSize;
                }
            }
        }
    }

    /// <summary>
    /// 重置页面到默认位置
    /// </summary>
    [ContextMenu("重置页面位置")]
    public void ResetPagePositions()
    {
        leftPageOffset = new Vector2(-150f, 0f);
        rightPageOffset = new Vector2(150f, 0f);
        leftPageScale = Vector2.one;
        rightPageScale = Vector2.one;
        uniformScale = 1f;

        ApplyPageTransforms();
        UpdateClickAreas();
    }

    /// <summary>
    /// 根据精灵大小自动计算位置
    /// </summary>
    [ContextMenu("自动计算页面位置")]
    public void AutoCalculatePositions()
    {
        if (leftPageRenderer != null && leftPageRenderer.sprite != null)
        {
            float leftWidth = leftPageRenderer.sprite.bounds.size.x * leftPageScale.x;
            leftPageOffset.x = -leftWidth / 2f - 5f; // 5像素间隙
        }

        if (rightPageRenderer != null && rightPageRenderer.sprite != null)
        {
            float rightWidth = rightPageRenderer.sprite.bounds.size.x * rightPageScale.x;
            rightPageOffset.x = rightWidth / 2f + 5f; // 5像素间隙
        }

        ApplyPageTransforms();
        UpdateClickAreas();
    }

    // ============ 点击检测 ============

    /// <summary>
    /// 检测点击区域
    /// </summary>
    private void CheckClickArea()
    {
        // 检查是否点击在UI上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 检查右侧点击区域（翻到下一页）
        if (rightClickArea != null && rightClickArea.OverlapPoint(mouseWorldPos))
        {
            TryFlipNext();
            return;
        }

        // 检查左侧点击区域（翻到上一页）
        if (leftClickArea != null && leftClickArea.OverlapPoint(mouseWorldPos))
        {
            TryFlipPrevious();
            return;
        }
    }

    // ============ 翻页逻辑 ============

    /// <summary>
    /// 尝试翻到下一页
    /// </summary>
    public void TryFlipNext()
    {
        if (isFlipping) return;

        if (IsLastSpread)
        {
            // 已经是最后一页
            PlaySound(edgeSoundName);
            OnLastPage?.Invoke();

            // 播放轻微震动反馈
            StartCoroutine(EdgeBounceEffect(false));
            return;
        }

        // 执行翻页
        if (enableFlipAnimation)
        {
            flipCoroutine = StartCoroutine(FlipToNextAnimation());
        }
        else
        {
            currentSpreadIndex++;
            UpdatePageDisplay();
            UpdatePageNumber();
            PlaySound(flipSoundName);
            OnPageChanged?.Invoke(currentSpreadIndex);
        }
    }

    /// <summary>
    /// 尝试翻到上一页
    /// </summary>
    public void TryFlipPrevious()
    {
        if (isFlipping) return;

        if (IsFirstSpread)
        {
            // 已经是第一页
            PlaySound(edgeSoundName);
            OnFirstPage?.Invoke();

            // 播放轻微震动反馈
            StartCoroutine(EdgeBounceEffect(true));
            return;
        }

        // 执行翻页
        if (enableFlipAnimation)
        {
            flipCoroutine = StartCoroutine(FlipToPreviousAnimation());
        }
        else
        {
            currentSpreadIndex--;
            UpdatePageDisplay();
            UpdatePageNumber();
            PlaySound(flipSoundName);
            OnPageChanged?.Invoke(currentSpreadIndex);
        }
    }

    /// <summary>
    /// 直接跳转到指定页
    /// </summary>
    public void GoToSpread(int index)
    {
        if (isFlipping) return;
        if (index < 0 || index >= pageSpreads.Count) return;
        if (index == currentSpreadIndex) return;

        currentSpreadIndex = index;
        UpdatePageDisplay();
        UpdatePageNumber();
        PlaySound(flipSoundName);
        OnPageChanged?.Invoke(currentSpreadIndex);
    }

    // ============ 翻页动画 ============

    /// <summary>
    /// 向后翻页动画（右→左）
    /// </summary>
    private IEnumerator FlipToNextAnimation()
    {
        isFlipping = true;
        PlaySound(flipSoundName);

        int nextIndex = currentSpreadIndex + 1;
        PageSpread currentSpread = pageSpreads[currentSpreadIndex];
        PageSpread nextSpread = pageSpreads[nextIndex];

        // 设置翻页页面初始状态（显示当前右页）
        if (flipPageRenderer != null)
        {
            flipPageRenderer.sprite = currentSpread.rightPageSprite;
            flipPageRenderer.transform.localPosition = new Vector3(rightPageOffset.x, rightPageOffset.y, 0f);
            flipPageRenderer.transform.localScale = new Vector3(
                useUniformScale ? uniformScale : rightPageScale.x,
                useUniformScale ? uniformScale : rightPageScale.y,
                1f
            );
            flipPageRenderer.transform.localEulerAngles = Vector3.zero;
            flipPageRenderer.gameObject.SetActive(true);
            SetSpriteAlpha(flipPageRenderer, 1f);
        }

        float elapsed = 0f;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / flipDuration);

            // 翻页页面动画：缩放X轴模拟翻转
            if (flipPageRenderer != null)
            {
                float baseScaleX = useUniformScale ? uniformScale : rightPageScale.x;
                float baseScaleY = useUniformScale ? uniformScale : rightPageScale.y;

                if (t < 0.5f)
                {
                    // 前半段：显示当前右页（缩小）
                    float scaleX = Mathf.Lerp(baseScaleX, 0f, t * 2f);
                    flipPageRenderer.transform.localScale = new Vector3(scaleX, baseScaleY, 1f);

                    // 倾斜效果
                    float tilt = Mathf.Sin(t * Mathf.PI) * maxTiltAngle;
                    flipPageRenderer.transform.localEulerAngles = new Vector3(0f, 0f, -tilt);

                    // 位置从右向中间移动
                    float posX = Mathf.Lerp(rightPageOffset.x, 0f, t * 2f);
                    flipPageRenderer.transform.localPosition = new Vector3(posX, rightPageOffset.y, 0f);
                }
                else
                {
                    // 切换到下一页的左页内容
                    if (flipPageRenderer.transform.localScale.x < 0.1f)
                    {
                        flipPageRenderer.sprite = nextSpread.leftPageSprite;
                        baseScaleX = useUniformScale ? uniformScale : leftPageScale.x;
                        baseScaleY = useUniformScale ? uniformScale : leftPageScale.y;
                    }

                    // 后半段：显示下一页左页（放大）
                    float scaleX = Mathf.Lerp(0f, baseScaleX, (t - 0.5f) * 2f);
                    flipPageRenderer.transform.localScale = new Vector3(scaleX, baseScaleY, 1f);

                    // 倾斜效果
                    float tilt = Mathf.Sin(t * Mathf.PI) * maxTiltAngle;
                    flipPageRenderer.transform.localEulerAngles = new Vector3(0f, 0f, tilt);

                    // 位置从中间向左移动
                    float posX = Mathf.Lerp(0f, leftPageOffset.x, (t - 0.5f) * 2f);
                    flipPageRenderer.transform.localPosition = new Vector3(posX, leftPageOffset.y, 0f);
                }
            }

            // 阴影效果
            if (enableShadowEffect && shadowRenderer != null)
            {
                float shadowAlpha = Mathf.Sin(t * Mathf.PI) * maxShadowAlpha;
                SetSpriteAlpha(shadowRenderer, shadowAlpha);
            }

            // 页面淡入淡出
            if (enableFadeEffect)
            {
                if (t < 0.5f)
                {
                    float alpha = Mathf.Lerp(1f, 0.5f, t * 2f);
                    SetSpriteAlpha(rightPageRenderer, alpha);
                }
                else
                {
                    float alpha = Mathf.Lerp(0.5f, 1f, (t - 0.5f) * 2f);
                    SetSpriteAlpha(leftPageRenderer, alpha);
                    SetSpriteAlpha(rightPageRenderer, alpha);
                }
            }

            yield return null;
        }

        // 更新页面索引和显示
        currentSpreadIndex = nextIndex;
        UpdatePageDisplay();
        UpdatePageNumber();

        // 隐藏翻页页面
        if (flipPageRenderer != null)
        {
            flipPageRenderer.gameObject.SetActive(false);
        }

        // 重置阴影
        if (shadowRenderer != null)
        {
            SetSpriteAlpha(shadowRenderer, 0f);
        }

        // 重置页面透明度
        SetSpriteAlpha(leftPageRenderer, 1f);
        SetSpriteAlpha(rightPageRenderer, 1f);

        isFlipping = false;
        OnPageChanged?.Invoke(currentSpreadIndex);

        if (IsLastSpread)
        {
            OnLastPage?.Invoke();
        }
    }

    /// <summary>
    /// 向前翻页动画（左→右）
    /// </summary>
    private IEnumerator FlipToPreviousAnimation()
    {
        isFlipping = true;
        PlaySound(flipSoundName);

        int prevIndex = currentSpreadIndex - 1;
        PageSpread currentSpread = pageSpreads[currentSpreadIndex];
        PageSpread prevSpread = pageSpreads[prevIndex];

        // 设置翻页页面初始状态（显示当前左页）
        if (flipPageRenderer != null)
        {
            flipPageRenderer.sprite = currentSpread.leftPageSprite;
            flipPageRenderer.transform.localPosition = new Vector3(leftPageOffset.x, leftPageOffset.y, 0f);
            flipPageRenderer.transform.localScale = new Vector3(
                useUniformScale ? uniformScale : leftPageScale.x,
                useUniformScale ? uniformScale : leftPageScale.y,
                1f
            );
            flipPageRenderer.transform.localEulerAngles = Vector3.zero;
            flipPageRenderer.gameObject.SetActive(true);
            SetSpriteAlpha(flipPageRenderer, 1f);
        }

        float elapsed = 0f;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = flipCurve.Evaluate(elapsed / flipDuration);

            if (flipPageRenderer != null)
            {
                float baseScaleX = useUniformScale ? uniformScale : leftPageScale.x;
                float baseScaleY = useUniformScale ? uniformScale : leftPageScale.y;

                if (t < 0.5f)
                {
                    // 前半段：当前左页缩小
                    float scaleX = Mathf.Lerp(baseScaleX, 0f, t * 2f);
                    flipPageRenderer.transform.localScale = new Vector3(scaleX, baseScaleY, 1f);

                    float tilt = Mathf.Sin(t * Mathf.PI) * maxTiltAngle;
                    flipPageRenderer.transform.localEulerAngles = new Vector3(0f, 0f, tilt);

                    // 位置从左向中间移动
                    float posX = Mathf.Lerp(leftPageOffset.x, 0f, t * 2f);
                    flipPageRenderer.transform.localPosition = new Vector3(posX, leftPageOffset.y, 0f);
                }
                else
                {
                    // 切换到上一页的右页内容
                    if (flipPageRenderer.transform.localScale.x < 0.1f)
                    {
                        flipPageRenderer.sprite = prevSpread.rightPageSprite;
                        baseScaleX = useUniformScale ? uniformScale : rightPageScale.x;
                        baseScaleY = useUniformScale ? uniformScale : rightPageScale.y;
                    }

                    // 后半段：上一页右页放大
                    float scaleX = Mathf.Lerp(0f, baseScaleX, (t - 0.5f) * 2f);
                    flipPageRenderer.transform.localScale = new Vector3(scaleX, baseScaleY, 1f);

                    float tilt = Mathf.Sin(t * Mathf.PI) * maxTiltAngle;
                    flipPageRenderer.transform.localEulerAngles = new Vector3(0f, 0f, -tilt);

                    // 位置从中间向右移动
                    float posX = Mathf.Lerp(0f, rightPageOffset.x, (t - 0.5f) * 2f);
                    flipPageRenderer.transform.localPosition = new Vector3(posX, rightPageOffset.y, 0f);
                }
            }

            // 阴影效果
            if (enableShadowEffect && shadowRenderer != null)
            {
                float shadowAlpha = Mathf.Sin(t * Mathf.PI) * maxShadowAlpha;
                SetSpriteAlpha(shadowRenderer, shadowAlpha);
            }

            // 页面淡入淡出
            if (enableFadeEffect)
            {
                if (t < 0.5f)
                {
                    float alpha = Mathf.Lerp(1f, 0.5f, t * 2f);
                    SetSpriteAlpha(leftPageRenderer, alpha);
                }
                else
                {
                    float alpha = Mathf.Lerp(0.5f, 1f, (t - 0.5f) * 2f);
                    SetSpriteAlpha(leftPageRenderer, alpha);
                    SetSpriteAlpha(rightPageRenderer, alpha);
                }
            }

            yield return null;
        }

        // 更新页面索引和显示
        currentSpreadIndex = prevIndex;
        UpdatePageDisplay();
        UpdatePageNumber();

        // 隐藏翻页页面
        if (flipPageRenderer != null)
        {
            flipPageRenderer.gameObject.SetActive(false);
        }

        // 重置阴影和透明度
        if (shadowRenderer != null)
        {
            SetSpriteAlpha(shadowRenderer, 0f);
        }

        SetSpriteAlpha(leftPageRenderer, 1f);
        SetSpriteAlpha(rightPageRenderer, 1f);

        isFlipping = false;
        OnPageChanged?.Invoke(currentSpreadIndex);

        if (IsFirstSpread)
        {
            OnFirstPage?.Invoke();
        }
    }

    /// <summary>
    /// 到达边界时的弹跳效果
    /// </summary>
    private IEnumerator EdgeBounceEffect(bool isLeft)
    {
        SpriteRenderer targetRenderer = isLeft ? leftPageRenderer : rightPageRenderer;
        if (targetRenderer == null) yield break;

        Vector3 originalPos = targetRenderer.transform.localPosition;
        float bounceDistance = 10f;
        float bounceDuration = 0.15f;

        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;
            float offset = Mathf.Sin(t * Mathf.PI) * bounceDistance;
            float direction = isLeft ? -1f : 1f;
            targetRenderer.transform.localPosition = originalPos + new Vector3(offset * direction, 0f, 0f);
            yield return null;
        }

        targetRenderer.transform.localPosition = originalPos;
    }

    // ============ 页面显示更新 ============

    /// <summary>
    /// 更新页面显示
    /// </summary>
    private void UpdatePageDisplay()
    {
        if (currentSpreadIndex < 0 || currentSpreadIndex >= pageSpreads.Count)
        {
            Debug.LogWarning($"[NotebookController] 页面索引越界: {currentSpreadIndex}");
            return;
        }

        PageSpread spread = pageSpreads[currentSpreadIndex];

        if (leftPageRenderer != null)
        {
            leftPageRenderer.sprite = spread.leftPageSprite;
        }

        if (rightPageRenderer != null)
        {
            rightPageRenderer.sprite = spread.rightPageSprite;
        }

        Debug.Log($"[NotebookController] 显示页面组 {currentSpreadIndex + 1}/{pageSpreads.Count}");
    }

    /// <summary>
    /// 更新页码显示
    /// </summary>
    private void UpdatePageNumber()
    {
        if (pageNumberText != null)
        {
            pageNumberText.text = string.Format(pageNumberFormat, currentSpreadIndex + 1, pageSpreads.Count);
        }
    }

    // ============ 辅助方法 ============

    private void SetSpriteAlpha(SpriteRenderer renderer, float alpha)
    {
        if (renderer == null) return;
        Color c = renderer.color;
        renderer.color = new Color(c.r, c.g, c.b, alpha);
    }

    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    // ============ 存档系统 ============

    public int GetSaveData()
    {
        return currentSpreadIndex;
    }

    public void LoadSaveData(int savedIndex)
    {
        if (savedIndex >= 0 && savedIndex < pageSpreads.Count)
        {
            currentSpreadIndex = savedIndex;
            UpdatePageDisplay();
            UpdatePageNumber();
        }
    }

    // ============ 调试方法 ============

    [ContextMenu("Debug: 下一页")]
    private void DebugNextPage() => TryFlipNext();

    [ContextMenu("Debug: 上一页")]
    private void DebugPrevPage() => TryFlipPrevious();

    [ContextMenu("Debug: 跳转到首页")]
    private void DebugGoToFirst() => GoToSpread(0);

    [ContextMenu("Debug: 跳转到末页")]
    private void DebugGoToLast() => GoToSpread(pageSpreads.Count - 1);

    [ContextMenu("Debug: 打印当前状态")]
    private void DebugPrintState()
    {
        Debug.Log($"[NotebookController] 当前页: {currentSpreadIndex + 1}/{pageSpreads.Count}, 翻页中: {isFlipping}");
    }

    [ContextMenu("应用位置设置")]
    private void ApplyPositionSettings()
    {
        ApplyPageTransforms();
        UpdateClickAreas();
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        // 确保索引在有效范围内
        if (pageSpreads.Count > 0)
        {
            currentSpreadIndex = Mathf.Clamp(currentSpreadIndex, 0, pageSpreads.Count - 1);
        }

        // 实时预览（仅在编辑器中）
        if (livePreview && !Application.isPlaying)
        {
            ApplyPageTransforms();
            UpdateClickAreas();
        }
    }

    /// <summary>
    /// Scene视图绘制
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 绘制页面边界
        if (showPageBounds)
        {
            // 左页边界
            if (leftPageRenderer != null && leftPageRenderer.sprite != null)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // 橙色
                Vector3 leftCenter = transform.position + new Vector3(leftPageOffset.x, leftPageOffset.y, 0f);
                Vector3 leftSize = leftPageRenderer.sprite.bounds.size;
                leftSize.x *= useUniformScale ? uniformScale : leftPageScale.x;
                leftSize.y *= useUniformScale ? uniformScale : leftPageScale.y;
                Gizmos.DrawWireCube(leftCenter, leftSize);
            }

            // 右页边界
            if (rightPageRenderer != null && rightPageRenderer.sprite != null)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f); // 蓝色
                Vector3 rightCenter = transform.position + new Vector3(rightPageOffset.x, rightPageOffset.y, 0f);
                Vector3 rightSize = rightPageRenderer.sprite.bounds.size;
                rightSize.x *= useUniformScale ? uniformScale : rightPageScale.x;
                rightSize.y *= useUniformScale ? uniformScale : rightPageScale.y;
                Gizmos.DrawWireCube(rightCenter, rightSize);
            }
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
}