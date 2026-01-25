// Assets/Scripts/GamePlay/ShadowChaseController.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 黑影追逐控制器 - 管理跨墙面的黑影追逐谜题
/// 流程：镜子变Special → WallA黑影 → WallB黑影 → WallC黑影 → WallD黑影 → 掉落Note
/// </summary>
public class ShadowChaseController : MonoBehaviour
{
    public static ShadowChaseController Instance { get; private set; }

    /// <summary>
    /// 追逐阶段
    /// </summary>
    public enum ChasePhase
    {
        NotStarted = 0,     // 未开始（镜子还没变成Special）
        WallA = 1,          // 黑影在 WallA
        WallB = 2,          // 黑影在 WallB
        WallC = 3,          // 黑影在 WallC
        WallD = 4,          // 黑影在 WallD
        Completed = 5       // 已完成（Note已出现）
    }

    [Header("当前状态")]
    [Tooltip("当前追逐阶段")]
    public ChasePhase currentPhase = ChasePhase.NotStarted;

    [Header("黑影引用")]
    [Tooltip("WallA 上的黑影")]
    public ShadowFigure shadowOnWallA;

    [Tooltip("WallB 上的黑影")]
    public ShadowFigure shadowOnWallB;

    [Tooltip("WallC 上的黑影")]
    public ShadowFigure shadowOnWallC;

    [Tooltip("WallD 上的黑影")]
    public ShadowFigure shadowOnWallD;

    [Header("镜子引用")]
    [Tooltip("关联的镜子控制器")]
    public MirrorController mirrorController;

    [Header("最终奖励")]
    [Tooltip("WallD 黑影消失后出现的物品")]
    public GameObject noteObject;

    [Header("事件")]
    public UnityEvent OnChaseStarted;
    public UnityEvent OnChaseCompleted;

    // ⭐ 新增：记录上一个视图状态
    private GameManager.ViewState previousViewState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 初始化：隐藏所有黑影和Note
        HideAllShadows();
        if (noteObject != null)
        {
            noteObject.SetActive(false);
        }

        // 订阅镜子状态变化
        if (mirrorController != null)
        {
            mirrorController.OnMirrorStateChanged.AddListener(OnMirrorStateChanged);

            // 如果镜子已经是Special状态（读档情况），立即检查
            if (mirrorController.currentState == MirrorController.MirrorState.Special)
            {
                StartChase();
            }
        }

        // 订阅视图切换事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.AddListener(OnViewStateChanged);
            // ⭐ 记录初始视图状态
            previousViewState = GameManager.Instance.CurrentViewState;
        }
    }

    private void OnDestroy()
    {
        if (mirrorController != null)
        {
            mirrorController.OnMirrorStateChanged.RemoveListener(OnMirrorStateChanged);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.RemoveListener(OnViewStateChanged);
        }
    }

    /// <summary>
    /// 镜子状态变化回调
    /// </summary>
    private void OnMirrorStateChanged(MirrorController.MirrorState newState)
    {
        // 当镜子变成 Special 状态时，开始追逐
        if (newState == MirrorController.MirrorState.Special && currentPhase == ChasePhase.NotStarted)
        {
            StartChase();
        }
    }

    /// <summary>
    /// 视图切换回调 - 当切换到对应视图时显示黑影
    /// </summary>
    private void OnViewStateChanged(GameManager.ViewState newState)
    {
        Debug.Log($"[ShadowChaseController] 视图切换: {previousViewState} → {newState}, 当前阶段: {currentPhase}");

        // ⭐ 新增：先隐藏上一个视图的黑影（如果有）
        HideShadowForView(previousViewState);

        // 记录新的视图状态
        previousViewState = newState;

        // 根据当前阶段和切换到的视图，显示对应黑影
        switch (currentPhase)
        {
            case ChasePhase.WallA:
                // 第一阶段：黑影在镜子放大视图中（Mirror_ZoomView）
                string viewName = newState.ToString().ToLower();
                if (viewName.Contains("mirror") && viewName.Contains("zoom"))
                {
                    ShowShadow(shadowOnWallA);
                }
                break;

            case ChasePhase.WallB:
                if (newState == GameManager.ViewState.Wall_B)
                {
                    ShowShadow(shadowOnWallB);
                }
                break;

            case ChasePhase.WallC:
                if (newState == GameManager.ViewState.Wall_C)
                {
                    ShowShadow(shadowOnWallC);
                }
                break;

            case ChasePhase.WallD:
                if (newState == GameManager.ViewState.Wall_D)
                {
                    ShowShadow(shadowOnWallD);
                }
                break;
        }
    }

    /// <summary>
    /// ⭐ 新增：根据视图隐藏对应的黑影
    /// </summary>
    private void HideShadowForView(GameManager.ViewState viewState)
    {
        string viewName = viewState.ToString().ToLower();

        // 镜子放大视图 → 隐藏 WallA 黑影
        if (viewName.Contains("mirror") && viewName.Contains("zoom"))
        {
            if (shadowOnWallA != null && !shadowOnWallA.hasBeenClicked)
            {
                shadowOnWallA.HideTemporary();
            }
        }

        // Wall_B → 隐藏 WallB 黑影
        if (viewState == GameManager.ViewState.Wall_B)
        {
            if (shadowOnWallB != null && !shadowOnWallB.hasBeenClicked)
            {
                shadowOnWallB.HideTemporary();
            }
        }

        // Wall_C → 隐藏 WallC 黑影
        if (viewState == GameManager.ViewState.Wall_C)
        {
            if (shadowOnWallC != null && !shadowOnWallC.hasBeenClicked)
            {
                shadowOnWallC.HideTemporary();
            }
        }

        // Wall_D → 隐藏 WallD 黑影
        if (viewState == GameManager.ViewState.Wall_D)
        {
            if (shadowOnWallD != null && !shadowOnWallD.hasBeenClicked)
            {
                shadowOnWallD.HideTemporary();
            }
        }
    }

    /// <summary>
    /// 开始追逐
    /// </summary>
    public void StartChase()
    {
        if (currentPhase != ChasePhase.NotStarted) return;

        Debug.Log("[ShadowChaseController] 黑影追逐开始！");
        currentPhase = ChasePhase.WallA;

        OnChaseStarted?.Invoke();

        // 如果当前在镜子放大视图中，立即显示黑影
        if (GameManager.Instance != null)
        {
            string viewName = GameManager.Instance.CurrentViewState.ToString().ToLower();
            if (viewName.Contains("mirror") && viewName.Contains("zoom"))
            {
                ShowShadow(shadowOnWallA);
            }
        }

        // 保存进度
        SaveProgress();
    }

    /// <summary>
    /// 黑影被点击后调用（由 ShadowFigure 调用）
    /// </summary>
    public void OnShadowClicked(ShadowFigure shadow)
    {
        Debug.Log($"[ShadowChaseController] 黑影被点击，当前阶段: {currentPhase}");

        // 根据当前阶段处理
        switch (currentPhase)
        {
            case ChasePhase.WallA:
                if (shadow == shadowOnWallA)
                {
                    currentPhase = ChasePhase.WallB;
                    Debug.Log("[ShadowChaseController] 进入阶段: WallB");
                }
                break;

            case ChasePhase.WallB:
                if (shadow == shadowOnWallB)
                {
                    currentPhase = ChasePhase.WallC;
                    Debug.Log("[ShadowChaseController] 进入阶段: WallC");
                }
                break;

            case ChasePhase.WallC:
                if (shadow == shadowOnWallC)
                {
                    currentPhase = ChasePhase.WallD;
                    Debug.Log("[ShadowChaseController] 进入阶段: WallD");
                }
                break;

            case ChasePhase.WallD:
                if (shadow == shadowOnWallD)
                {
                    CompleteChase();
                }
                break;
        }

        // 保存进度
        SaveProgress();
    }

    /// <summary>
    /// 完成追逐 - 显示 Note
    /// </summary>
    private void CompleteChase()
    {
        Debug.Log("[ShadowChaseController] 黑影追逐完成！显示 Note");
        currentPhase = ChasePhase.Completed;

        // 显示 Note 物品
        if (noteObject != null)
        {
            noteObject.SetActive(true);
        }

        OnChaseCompleted?.Invoke();

        // 保存进度
        SaveProgress();
    }

    /// <summary>
    /// 显示指定黑影
    /// </summary>
    private void ShowShadow(ShadowFigure shadow)
    {
        if (shadow != null && !shadow.hasBeenClicked)
        {
            shadow.Show();
        }
    }

    /// <summary>
    /// 隐藏所有黑影
    /// </summary>
    private void HideAllShadows()
    {
        if (shadowOnWallA != null) shadowOnWallA.Hide();
        if (shadowOnWallB != null) shadowOnWallB.Hide();
        if (shadowOnWallC != null) shadowOnWallC.Hide();
        if (shadowOnWallD != null) shadowOnWallD.Hide();
    }

    /// <summary>
    /// 保存进度
    /// </summary>
    private void SaveProgress()
    {
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    // ============ 存档相关 ============

    /// <summary>
    /// 获取当前阶段（用于存档）
    /// </summary>
    public int GetPhaseForSave()
    {
        return (int)currentPhase;
    }

    /// <summary>
    /// 恢复阶段（用于读档）
    /// </summary>
    public void RestorePhase(int phase)
    {
        currentPhase = (ChasePhase)phase;
        Debug.Log($"[ShadowChaseController] 恢复追逐阶段: {currentPhase}");

        // 根据阶段恢复状态
        if (currentPhase == ChasePhase.Completed)
        {
            // 已完成，显示 Note
            if (noteObject != null)
            {
                noteObject.SetActive(true);
            }
            HideAllShadows();

            // ⭐ 标记所有黑影为已点击
            MarkAllShadowsAsClicked();
        }
        else if (currentPhase != ChasePhase.NotStarted)
        {
            // ⭐ 标记已经过的阶段的黑影为已点击
            MarkPreviousShadowsAsClicked();

            // 追逐中，根据当前墙面显示黑影
            if (GameManager.Instance != null)
            {
                OnViewStateChanged(GameManager.Instance.CurrentViewState);
            }
        }
    }

    /// <summary>
    /// ⭐ 新增：标记所有黑影为已点击
    /// </summary>
    private void MarkAllShadowsAsClicked()
    {
        if (shadowOnWallA != null) shadowOnWallA.hasBeenClicked = true;
        if (shadowOnWallB != null) shadowOnWallB.hasBeenClicked = true;
        if (shadowOnWallC != null) shadowOnWallC.hasBeenClicked = true;
        if (shadowOnWallD != null) shadowOnWallD.hasBeenClicked = true;
    }

    /// <summary>
    /// ⭐ 新增：根据当前阶段标记之前的黑影为已点击
    /// </summary>
    private void MarkPreviousShadowsAsClicked()
    {
        // 根据当前阶段，标记之前阶段的黑影
        switch (currentPhase)
        {
            case ChasePhase.WallD:
                if (shadowOnWallC != null) shadowOnWallC.hasBeenClicked = true;
                goto case ChasePhase.WallC;

            case ChasePhase.WallC:
                if (shadowOnWallB != null) shadowOnWallB.hasBeenClicked = true;
                goto case ChasePhase.WallB;

            case ChasePhase.WallB:
                if (shadowOnWallA != null) shadowOnWallA.hasBeenClicked = true;
                break;
        }

        Debug.Log($"[ShadowChaseController] 已标记阶段 {currentPhase} 之前的黑影为已点击");
    }
}