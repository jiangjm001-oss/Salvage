// Assets/Scripts/GamePlay/TweezersJumpScareController.cs
// 镊子Jump Scare控制器 - 管理镊子拖拽触发恐怖跳吓效果
// 流程：选中镊子→点击嘴巴→镊子出现→拖动镊子→急速推镜→眼睛睁开→点击眼睛→黑屏转场→Dream_Zoomview
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 镊子Jump Scare控制器
/// 管理整个镊子拖拽和恐怖跳吓效果的完整流程
/// </summary>
public class TweezersJumpScareController : MonoBehaviour
{
    #region ========== 枚举定义 ==========

    /// <summary>
    /// 谜题状态
    /// </summary>
    public enum PuzzleState
    {
        WaitingForTweezers,     // 等待玩家使用镊子点击嘴巴
        Dragging,               // 正在拖拽镊子
        JumpScareTriggered,     // Jump Scare已触发，等待点击眼睛
        Completed               // 已完成，进入Dream
    }

    #endregion

    #region ========== Inspector 配置 ==========

    [Header("===== 当前状态 =====")]
    [Tooltip("当前谜题状态（只读）")]
    [SerializeField] private PuzzleState currentState = PuzzleState.WaitingForTweezers;

    [Header("===== 物品配置 =====")]
    [Tooltip("需要的物品：镊子")]
    public ItemData requiredTweezersItem;

    [Tooltip("使用后是否消耗镊子")]
    public bool consumeTweezers = true;

    [Header("===== 物体引用 =====")]
    [Tooltip("嘴巴触发区域（BoxCollider2D）")]
    public Collider2D mouthTriggerArea;

    [Tooltip("可拖拽的镊子物体（初始隐藏）")]
    public GameObject tweezersObject;

    [Tooltip("镊子的SpriteRenderer")]
    public SpriteRenderer tweezersRenderer;

    [Tooltip("闭眼物体")]
    public GameObject closedEyesObject;

    [Tooltip("睁眼物体（初始隐藏）")]
    public GameObject openEyesObject;

    [Tooltip("睁眼的可点击区域")]
    public Collider2D openEyesClickArea;

    [Header("===== 拖拽配置 =====")]
    [Tooltip("镊子初始位置（嘴巴上）")]
    public Transform tweezersStartPosition;

    [Tooltip("镊子目标位置（触发Jump Scare）")]
    public Transform tweezersTargetPosition;

    [Tooltip("触发Jump Scare的距离阈值")]
    public float triggerDistance = 0.3f;

    [Tooltip("拖拽时的层级提升")]
    public int dragSortingOrderBoost = 10;

    [Tooltip("是否限制只能向上拖拽")]
    public bool restrictToUpward = true;

    [Tooltip("拖拽平滑度（0=无平滑，越大越平滑）")]
    [Range(0f, 0.95f)]
    public float dragSmoothing = 0.1f;

    [Header("===== 相机推进配置 =====")]
    [Tooltip("推镜目标位置（眼睛位置）")]
    public Transform cameraTargetPosition;

    [Tooltip("推镜的目标正交大小（越小越近）")]
    public float targetOrthographicSize = 2f;

    [Tooltip("推镜持续时间")]
    public float zoomDuration = 0.3f;

    [Tooltip("推镜动画曲线")]
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("推镜完成后的停顿时间")]
    public float pauseAfterZoom = 0.1f;

    [Header("===== 屏幕震动配置 =====")]
    [Tooltip("是否启用屏幕震动")]
    public bool enableScreenShake = true;

    [Tooltip("震动强度")]
    public float shakeIntensity = 0.15f;

    [Tooltip("震动持续时间")]
    public float shakeDuration = 0.2f;

    [Header("===== 黑屏转场配置 =====")]
    [Tooltip("黑屏遮罩（CanvasGroup或Image）")]
    public CanvasGroup blackScreenOverlay;

    [Tooltip("淡入黑屏时间")]
    public float fadeInDuration = 0.3f;

    [Tooltip("黑屏停留时间")]
    public float blackScreenHoldTime = 0.5f;

    [Tooltip("淡出黑屏时间")]
    public float fadeOutDuration = 0.3f;

    [Header("===== 目标视图 =====")]
    [Tooltip("点击眼睛后进入的放大视图")]
    public GameObject dreamZoomViewTarget;

    [Header("===== 音效配置 =====")]
    [Tooltip("镊子出现音效")]
    public string tweezersAppearSound = "Audio/SFX/tweezers_appear";

    [Tooltip("拖拽中的音效")]
    public string draggingSound = "Audio/SFX/tape_pulling";

    [Tooltip("Jump Scare音效（贴脸吓人）")]
    public string jumpScareSound = "Audio/SFX/jumpscare_scream";

    [Tooltip("点击眼睛音效")]
    public string eyeClickSound = "Audio/SFX/eye_click";

    [Tooltip("转场音效")]
    public string transitionSound = "Audio/SFX/whoosh";

    [Header("===== 提示配置 =====")]
    [Tooltip("未选中镊子时的提示")]
    public string noTweezersHint = "需要用什么东西来处理...";

    [Tooltip("选错物品的提示")]
    public string wrongItemHint = "这个在这里没用...";

    [Header("===== 事件 =====")]
    public UnityEvent OnTweezersPlaced;
    public UnityEvent OnJumpScareTriggered;
    public UnityEvent OnEyeClicked;
    public UnityEvent OnPuzzleCompleted;

    #endregion

    #region ========== 私有变量 ==========

    private Camera mainCamera;
    private Vector3 originalCameraPosition;
    private float originalOrthographicSize;

    private bool isDragging = false;
    private Vector3 dragOffset;
    private int originalSortingOrder;

    private bool isProcessingJumpScare = false;
    private bool isProcessingTransition = false;

    // 拖拽音效控制
    private bool isDraggingSoundPlaying = false;

    #endregion

    #region ========== Unity 生命周期 ==========

    private void Awake()
    {
        mainCamera = Camera.main;

        // 初始化状态
        InitializeState();
    }

    private void Start()
    {
        // 记录相机初始状态
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
            originalOrthographicSize = mainCamera.orthographicSize;
        }

        // 记录镊子的原始排序层级
        if (tweezersRenderer != null)
        {
            originalSortingOrder = tweezersRenderer.sortingOrder;
        }

        // 确保黑屏遮罩初始透明
        if (blackScreenOverlay != null)
        {
            blackScreenOverlay.alpha = 0f;
            blackScreenOverlay.blocksRaycasts = false;
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case PuzzleState.WaitingForTweezers:
                HandleWaitingForTweezers();
                break;

            case PuzzleState.Dragging:
                HandleDragging();
                break;

            case PuzzleState.JumpScareTriggered:
                HandleJumpScareState();
                break;
        }
    }

    private void OnDestroy()
    {
        // 确保恢复相机状态
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.orthographicSize = originalOrthographicSize;
        }
    }

    #endregion

    #region ========== 初始化 ==========

    /// <summary>
    /// 初始化状态
    /// </summary>
    private void InitializeState()
    {
        // 隐藏镊子
        if (tweezersObject != null)
        {
            tweezersObject.SetActive(false);
        }

        // 显示闭眼，隐藏睁眼
        if (closedEyesObject != null)
        {
            closedEyesObject.SetActive(true);
        }

        if (openEyesObject != null)
        {
            openEyesObject.SetActive(false);
        }

        currentState = PuzzleState.WaitingForTweezers;

        Debug.Log("[TweezersJumpScare] 初始化完成，等待镊子");
    }

    #endregion

    #region ========== 状态处理：等待镊子 ==========

    /// <summary>
    /// 处理等待镊子状态
    /// </summary>
    private void HandleWaitingForTweezers()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // 检查是否点击在嘴巴区域
        if (!IsClickOnMouth()) return;

        // 检查是否选中了物品
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        if (selectedItem == null)
        {
            // 未选中任何物品，显示提示
            ShowHint(noTweezersHint);
            return;
        }

        if (requiredTweezersItem != null && selectedItem.itemID != requiredTweezersItem.itemID)
        {
            // 选错物品，显示提示
            ShowHint(wrongItemHint);
            return;
        }

        // 正确！使用镊子
        UseTweezers();
    }

    /// <summary>
    /// 检查是否点击在嘴巴区域
    /// </summary>
    private bool IsClickOnMouth()
    {
        if (mouthTriggerArea == null) return false;

        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        return mouthTriggerArea.OverlapPoint(mousePos);
    }

    /// <summary>
    /// 使用镊子
    /// </summary>
    private void UseTweezers()
    {
        Debug.Log("[TweezersJumpScare] 使用镊子");

        // 消耗物品
        if (consumeTweezers)
        {
            UIManager.Instance.ConsumeSelectedItem();
        }
        else
        {
            UIManager.Instance.DeselectItem();
        }

        // 显示镊子
        if (tweezersObject != null)
        {
            tweezersObject.SetActive(true);

            // 设置到起始位置
            if (tweezersStartPosition != null)
            {
                tweezersObject.transform.position = tweezersStartPosition.position;
            }
        }

        // 播放音效
        PlaySound(tweezersAppearSound);

        // 切换状态
        currentState = PuzzleState.Dragging;

        // 触发事件
        OnTweezersPlaced?.Invoke();

        Debug.Log("[TweezersJumpScare] 镊子已放置，开始拖拽阶段");
    }

    #endregion

    #region ========== 状态处理：拖拽镊子 ==========

    /// <summary>
    /// 处理拖拽状态
    /// </summary>
    private void HandleDragging()
    {
        // 开始拖拽
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            TryStartDrag();
        }

        // 拖拽中
        if (isDragging)
        {
            UpdateDrag();
            CheckTriggerDistance();
        }

        // 结束拖拽
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    /// <summary>
    /// 尝试开始拖拽
    /// </summary>
    private void TryStartDrag()
    {
        if (tweezersObject == null) return;

        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // 检查是否点击在镊子上
        Collider2D tweezersCollider = tweezersObject.GetComponent<Collider2D>();
        if (tweezersCollider != null && tweezersCollider.OverlapPoint(mousePos))
        {
            StartDrag(mousePos);
        }
    }

    /// <summary>
    /// 开始拖拽
    /// </summary>
    private void StartDrag(Vector2 mousePos)
    {
        isDragging = true;

        // 计算偏移
        dragOffset = tweezersObject.transform.position - (Vector3)mousePos;

        // 提升层级
        if (tweezersRenderer != null)
        {
            tweezersRenderer.sortingOrder = originalSortingOrder + dragSortingOrderBoost;
        }

        // 开始播放拖拽音效（如果是循环音效）
        if (!isDraggingSoundPlaying && !string.IsNullOrEmpty(draggingSound))
        {
            // 这里可以改成循环播放
            isDraggingSoundPlaying = true;
        }

        Debug.Log("[TweezersJumpScare] 开始拖拽镊子");
    }

    /// <summary>
    /// 更新拖拽
    /// </summary>
    private void UpdateDrag()
    {
        if (tweezersObject == null) return;

        Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = tweezersObject.transform.position.z;

        Vector3 targetPos = mousePos + dragOffset;

        // 限制只能向上拖拽
        if (restrictToUpward && tweezersStartPosition != null)
        {
            // 不能低于起始位置
            if (targetPos.y < tweezersStartPosition.position.y)
            {
                targetPos.y = tweezersStartPosition.position.y;
            }

            // 限制水平移动范围（可选）
            float maxHorizontalOffset = 1f;
            float horizontalDiff = targetPos.x - tweezersStartPosition.position.x;
            if (Mathf.Abs(horizontalDiff) > maxHorizontalOffset)
            {
                targetPos.x = tweezersStartPosition.position.x + Mathf.Sign(horizontalDiff) * maxHorizontalOffset;
            }
        }

        // 应用平滑
        if (dragSmoothing > 0)
        {
            tweezersObject.transform.position = Vector3.Lerp(
                tweezersObject.transform.position,
                targetPos,
                1f - dragSmoothing
            );
        }
        else
        {
            tweezersObject.transform.position = targetPos;
        }
    }

    /// <summary>
    /// 检查是否到达触发距离
    /// </summary>
    private void CheckTriggerDistance()
    {
        if (tweezersTargetPosition == null) return;

        float distance = Vector2.Distance(
            tweezersObject.transform.position,
            tweezersTargetPosition.position
        );

        if (distance <= triggerDistance)
        {
            // 到达目标位置，触发Jump Scare！
            TriggerJumpScare();
        }
    }

    /// <summary>
    /// 结束拖拽
    /// </summary>
    private void EndDrag()
    {
        isDragging = false;
        isDraggingSoundPlaying = false;

        // 恢复层级
        if (tweezersRenderer != null)
        {
            tweezersRenderer.sortingOrder = originalSortingOrder;
        }

        Debug.Log("[TweezersJumpScare] 结束拖拽");
    }

    #endregion

    #region ========== Jump Scare 触发 ==========

    /// <summary>
    /// 触发Jump Scare
    /// </summary>
    private void TriggerJumpScare()
    {
        if (isProcessingJumpScare) return;

        Debug.Log("[TweezersJumpScare] ⚡ 触发Jump Scare!");

        isProcessingJumpScare = true;
        isDragging = false;
        isDraggingSoundPlaying = false;

        // 锁定镊子到目标位置
        if (tweezersObject != null && tweezersTargetPosition != null)
        {
            tweezersObject.transform.position = tweezersTargetPosition.position;
        }

        // 开始Jump Scare序列
        StartCoroutine(JumpScareSequence());
    }

    /// <summary>
    /// Jump Scare序列协程
    /// </summary>
    private IEnumerator JumpScareSequence()
    {
        // 1. 急速推镜到眼睛
        yield return StartCoroutine(ZoomToEyes());

        // 2. 短暂停顿
        yield return new WaitForSeconds(pauseAfterZoom);

        // 3. 显示睁开的眼睛 + 播放恐怖音效 + 屏幕震动
        ShowOpenEyes();
        PlaySound(jumpScareSound);

        if (enableScreenShake)
        {
            StartCoroutine(ScreenShake());
        }

        // 4. 切换状态
        currentState = PuzzleState.JumpScareTriggered;
        isProcessingJumpScare = false;

        // 5. 触发事件
        OnJumpScareTriggered?.Invoke();

        Debug.Log("[TweezersJumpScare] Jump Scare完成，等待点击眼睛");
    }

    /// <summary>
    /// 相机急速推进到眼睛
    /// </summary>
    private IEnumerator ZoomToEyes()
    {
        if (mainCamera == null || cameraTargetPosition == null)
        {
            yield break;
        }

        Vector3 startPos = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;

        Vector3 targetPos = new Vector3(
            cameraTargetPosition.position.x,
            cameraTargetPosition.position.y,
            startPos.z
        );

        float elapsed = 0f;

        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / zoomDuration);
            float curvedT = zoomCurve.Evaluate(t);

            // 移动相机
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, curvedT);

            // 缩放（推近）
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetOrthographicSize, curvedT);

            yield return null;
        }

        // 确保到达最终位置
        mainCamera.transform.position = targetPos;
        mainCamera.orthographicSize = targetOrthographicSize;

        Debug.Log("[TweezersJumpScare] 相机推镜完成");
    }

    /// <summary>
    /// 显示睁开的眼睛
    /// </summary>
    private void ShowOpenEyes()
    {
        // 隐藏闭眼
        if (closedEyesObject != null)
        {
            closedEyesObject.SetActive(false);
        }

        // 显示睁眼
        if (openEyesObject != null)
        {
            openEyesObject.SetActive(true);

            // 添加出现动画效果
            StartCoroutine(EyeOpenAnimation());
        }

        // 隐藏镊子（可选）
        if (tweezersObject != null)
        {
            tweezersObject.SetActive(false);
        }

        Debug.Log("[TweezersJumpScare] 眼睛睁开!");
    }

    /// <summary>
    /// 眼睛睁开动画
    /// </summary>
    private IEnumerator EyeOpenAnimation()
    {
        if (openEyesObject == null) yield break;

        SpriteRenderer eyeRenderer = openEyesObject.GetComponent<SpriteRenderer>();
        if (eyeRenderer == null) yield break;

        // 快速缩放弹出效果
        Vector3 originalScale = openEyesObject.transform.localScale;
        Vector3 punchScale = originalScale * 1.15f;

        // 弹出
        float punchDuration = 0.08f;
        float elapsed = 0f;

        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            openEyesObject.transform.localScale = Vector3.Lerp(originalScale, punchScale, t);
            yield return null;
        }

        // 回弹
        elapsed = 0f;
        while (elapsed < punchDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / punchDuration;
            openEyesObject.transform.localScale = Vector3.Lerp(punchScale, originalScale, t);
            yield return null;
        }

        openEyesObject.transform.localScale = originalScale;
    }

    /// <summary>
    /// 屏幕震动效果
    /// </summary>
    private IEnumerator ScreenShake()
    {
        if (mainCamera == null) yield break;

        Vector3 shakeStartPos = mainCamera.transform.position;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            // 随着时间衰减的震动
            float decay = 1f - (elapsed / shakeDuration);
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity) * decay;
            float offsetY = Random.Range(-shakeIntensity, shakeIntensity) * decay;

            mainCamera.transform.position = shakeStartPos + new Vector3(offsetX, offsetY, 0);

            yield return null;
        }

        // 恢复位置（保持推镜后的位置）
        mainCamera.transform.position = shakeStartPos;
    }

    #endregion

    #region ========== 状态处理：等待点击眼睛 ==========

    /// <summary>
    /// 处理Jump Scare触发后的状态
    /// </summary>
    private void HandleJumpScareState()
    {
        if (isProcessingTransition) return;

        if (!Input.GetMouseButtonDown(0)) return;

        // 检查是否点击在眼睛上
        if (IsClickOnEyes())
        {
            OnEyesClicked();
        }
    }

    /// <summary>
    /// 检查是否点击在眼睛上
    /// </summary>
    private bool IsClickOnEyes()
    {
        if (openEyesClickArea == null && openEyesObject == null) return false;

        Vector2 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);

        // 优先使用指定的点击区域
        if (openEyesClickArea != null)
        {
            return openEyesClickArea.OverlapPoint(mousePos);
        }

        // 使用眼睛物体的碰撞器
        Collider2D eyeCollider = openEyesObject.GetComponent<Collider2D>();
        if (eyeCollider != null)
        {
            return eyeCollider.OverlapPoint(mousePos);
        }

        return false;
    }

    /// <summary>
    /// 眼睛被点击
    /// </summary>
    private void OnEyesClicked()
    {
        Debug.Log("[TweezersJumpScare] 眼睛被点击!");

        isProcessingTransition = true;

        // 播放音效
        PlaySound(eyeClickSound);

        // 触发事件
        OnEyeClicked?.Invoke();

        // 开始黑屏转场序列
        StartCoroutine(TransitionSequence());
    }

    #endregion

    #region ========== 黑屏转场 ==========

    /// <summary>
    /// 转场序列协程
    /// </summary>
    private IEnumerator TransitionSequence()
    {
        // 1. 淡入黑屏
        yield return StartCoroutine(FadeToBlack());

        // 2. 播放转场音效
        PlaySound(transitionSound);

        // 3. 黑屏停留
        yield return new WaitForSeconds(blackScreenHoldTime);

        // 4. 恢复相机状态
        ResetCameraState();

        // 5. 标记完成状态（在切换视图前）
        currentState = PuzzleState.Completed;
        isProcessingTransition = false;

        // 触发完成事件
        OnPuzzleCompleted?.Invoke();

        Debug.Log("[TweezersJumpScare] 谜题完成!");

        // ⭐ 关键：先进入Dream视图，再延迟淡出黑屏
        // 因为进入新视图后当前GameObject会被禁用，所以需要用其他方式处理淡出
        EnterDreamZoomViewWithFadeOut();
    }

    /// <summary>
    /// 进入Dream视图并处理淡出
    /// </summary>
    private void EnterDreamZoomViewWithFadeOut()
    {
        if (dreamZoomViewTarget == null)
        {
            Debug.LogError("[TweezersJumpScare] 未设置Dream放大视图目标!");
            // 仍然需要淡出黑屏
            FadeOutBlackScreenSafe();
            return;
        }

        // 进入新视图前，先在新视图上设置淡出
        // 方案1：使用ScreenFadeOverlay单例（如果存在）
        // 方案2：使用协程管理器
        // 方案3：直接在这里启动延迟淡出（使用Invoke或独立协程）

        // 先进入新视图
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EnterZoomViewDirect(dreamZoomViewTarget);
            Debug.Log($"[TweezersJumpScare] 进入放大视图: {dreamZoomViewTarget.name}");
        }

        // 使用安全的方式淡出黑屏（不依赖当前GameObject）
        FadeOutBlackScreenSafe();
    }

    /// <summary>
    /// 安全地淡出黑屏（不依赖当前GameObject的激活状态）
    /// </summary>
    private void FadeOutBlackScreenSafe()
    {
        if (blackScreenOverlay == null) return;

        // 方案1：使用ScreenFadeOverlay组件（如果有）
        ScreenFadeOverlay fadeOverlay = blackScreenOverlay.GetComponent<ScreenFadeOverlay>();
        if (fadeOverlay != null)
        {
            fadeOverlay.FadeOut(fadeOutDuration);
            return;
        }

        // 方案2：使用GameManager的协程（因为它是DontDestroyOnLoad）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartCoroutine(FadeOutCoroutineStatic(blackScreenOverlay, fadeOutDuration));
            return;
        }

        // 方案3：直接设置透明（无动画，作为后备）
        blackScreenOverlay.alpha = 0f;
        blackScreenOverlay.blocksRaycasts = false;
    }

    /// <summary>
    /// 静态淡出协程（可在任何MonoBehaviour上运行）
    /// </summary>
    private static IEnumerator FadeOutCoroutineStatic(CanvasGroup canvasGroup, float duration)
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        Debug.Log("[TweezersJumpScare] 黑屏淡出完成");
    }

    /// <summary>
    /// 淡入黑屏
    /// </summary>
    private IEnumerator FadeToBlack()
    {
        if (blackScreenOverlay == null)
        {
            Debug.LogWarning("[TweezersJumpScare] 未设置黑屏遮罩!");
            yield break;
        }

        blackScreenOverlay.blocksRaycasts = true;

        float elapsed = 0f;
        float startAlpha = blackScreenOverlay.alpha;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            blackScreenOverlay.alpha = Mathf.Lerp(startAlpha, 1f, t);
            yield return null;
        }

        blackScreenOverlay.alpha = 1f;
    }

    /// <summary>
    /// 淡出黑屏
    /// </summary>
    private IEnumerator FadeFromBlack()
    {
        if (blackScreenOverlay == null) yield break;

        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            blackScreenOverlay.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        blackScreenOverlay.alpha = 0f;
        blackScreenOverlay.blocksRaycasts = false;
    }

    /// <summary>
    /// 恢复相机状态
    /// </summary>
    private void ResetCameraState()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.position = originalCameraPosition;
            mainCamera.orthographicSize = originalOrthographicSize;
        }
    }

    #endregion

    #region ========== 辅助方法 ==========

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(string soundPath)
    {
        if (string.IsNullOrEmpty(soundPath)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundPath);
        }
    }

    /// <summary>
    /// 显示提示
    /// </summary>
    private void ShowHint(string hint)
    {
        if (string.IsNullOrEmpty(hint)) return;

        // 使用UIManager显示提示（如果有）
        Debug.Log($"[TweezersJumpScare] 提示: {hint}");

        // 可以在这里调用你的提示系统
        // UIManager.Instance?.ShowHint(hint);
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public PuzzleState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 重置谜题（用于调试）
    /// </summary>
    [ContextMenu("Reset Puzzle")]
    public void ResetPuzzle()
    {
        StopAllCoroutines();

        // 恢复相机
        ResetCameraState();

        // 恢复黑屏
        if (blackScreenOverlay != null)
        {
            blackScreenOverlay.alpha = 0f;
            blackScreenOverlay.blocksRaycasts = false;
        }

        // 重置状态
        isProcessingJumpScare = false;
        isProcessingTransition = false;
        isDragging = false;

        // 重新初始化
        InitializeState();

        Debug.Log("[TweezersJumpScare] 谜题已重置");
    }

    #endregion

    #region ========== 编辑器辅助 ==========

    private void OnDrawGizmosSelected()
    {
        // 绘制嘴巴触发区域
        if (mouthTriggerArea != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(mouthTriggerArea.bounds.center, mouthTriggerArea.bounds.size);
        }

        // 绘制镊子起始位置
        if (tweezersStartPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(tweezersStartPosition.position, 0.1f);
            Gizmos.DrawIcon(tweezersStartPosition.position, "d_Animation.Record", true);
        }

        // 绘制镊子目标位置和触发范围
        if (tweezersTargetPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(tweezersTargetPosition.position, triggerDistance);
            Gizmos.DrawIcon(tweezersTargetPosition.position, "d_Animation.Play", true);
        }

        // 绘制镊子拖拽路径
        if (tweezersStartPosition != null && tweezersTargetPosition != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(tweezersStartPosition.position, tweezersTargetPosition.position);
        }

        // 绘制相机目标位置
        if (cameraTargetPosition != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(cameraTargetPosition.position, 0.2f);

            // 绘制目标视野范围
            float aspect = Camera.main != null ? Camera.main.aspect : 16f / 9f;
            float height = targetOrthographicSize * 2;
            float width = height * aspect;
            Gizmos.DrawWireCube(cameraTargetPosition.position, new Vector3(width, height, 0));
        }
    }

    #endregion
}