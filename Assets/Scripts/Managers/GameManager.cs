// Assets/Scripts/Managers/GameManager.cs
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ============ 游戏状态定义 ============
    public enum GameState
    {
        MainMenu,
        Level1,
        Level2,
        Paused,
        Ending
    }

    public GameState CurrentGameState { get; private set; } = GameState.MainMenu;
    public UnityEvent<GameState> OnGameStateChanged = new UnityEvent<GameState>();

    // ============ 视图状态定义 ============
    public enum ViewState
    {
        Wall_A, Wall_B, Wall_C, Wall_D,

        // Level 1 放大视图
        Level1_Zoom_Mirror,
        lv1_A_zoom_lowCabinet,
        lv1_A_zoom_GrandfatherClock,
        lv1_A_zoom_Heater,
        lv1_A_zoom_Towel,

        lv1_B_zoom_Window,
        lv1_B_zoom_Desk,
        lv1_B_zoom_GroupPhoto,
        Level1_Zoom_TrashCan,

        lv1_C_zoom_Fireplace,
        lv1_C_zoom_OilPainting,
        lv1_C_zoom_EmptyPhotoFrame,

        lv1_D_zoom_sofa,
        lv1_D_zoom_coffeeTable,
        lv1_D_zoom_foldingScreen,
        lv1_D_zoom_Hammer,

        // Level 2 放大视图
        Level2_Zoom_Mirror,
        Level2_Zoom_Painting,
        Level2_Zoom_Safe,
    }

    public ViewState CurrentViewState { get; private set; } = ViewState.Wall_A;
    public UnityEvent<ViewState> OnViewStateChanged = new UnityEvent<ViewState>();

    private ViewState lastWallBeforeZoom = ViewState.Wall_A;

    // ============ 场景管理器引用 ============
    private WallManager currentWallManager;
    private FurnitureZoomController currentZoomController;

    public static WallManager CurrentWallManager => Instance?.currentWallManager;
    public static FurnitureZoomController CurrentZoomController => Instance?.currentZoomController;

    // ============ 存档相关 ============
    private SaveData pendingSaveData = null;
    private ViewState pendingViewState = ViewState.Wall_A;
    private bool hasPendingViewState = false;

    private void Awake()
    {
        Debug.Log("[GameManager] Awake() called.");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameManager] Instance initialized successfully.");
        }
        else
        {
            Debug.LogWarning("[GameManager] Duplicate detected! Destroying this instance.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("[GameManager] Start() called.");

        string currentSceneName = SceneManager.GetActiveScene().name;

        if (currentSceneName != "Bootstrap")
        {
            UpdateGameStateBasedOnScene(currentSceneName);
        }

        // 订阅场景加载完成事件
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 场景加载完成回调
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: {scene.name}");

        // 如果有待应用的存档数据，在场景加载后应用
        if (pendingSaveData != null)
        {
            StartCoroutine(ApplySaveDataDelayed());
        }
    }

    /// <summary>
    /// 延迟应用存档数据（等待场景完全初始化）
    /// </summary>
    private IEnumerator ApplySaveDataDelayed()
    {
        // 等待几帧，确保 WallManager 和其他组件都已初始化
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        if (pendingSaveData != null && SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.ApplySaveData(pendingSaveData);
            pendingSaveData = null;
        }
    }

    // ============ 游戏状态管理 ============

    public void UpdateGameStateBasedOnScene(string sceneName)
    {
        GameState newState = sceneName switch
        {
            "LandingPage" => GameState.MainMenu,
            "Level1_Room" => GameState.Level1,
            "Level2_Room" => GameState.Level2,
            "EndingScene" => GameState.Ending,
            _ => GameState.MainMenu
        };

        if (newState != CurrentGameState)
        {
            ChangeGameState(newState);
        }
    }

    public void ChangeGameState(GameState newState)
    {
        if (CurrentGameState == newState) return;

        CurrentGameState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] Game state changed to: {newState}");
    }

    // ============ 开始游戏 / 继续游戏 ============

    /// <summary>
    /// 开始新游戏 - 清除所有存档，从头开始
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("[GameManager] ========== Starting NEW game ==========");

        // 1. 删除所有存档数据
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteSaveData();
        }

        // 2. 清空背包
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearInventory();
        }

        // 3. 重置状态
        CurrentViewState = ViewState.Wall_A;
        lastWallBeforeZoom = ViewState.Wall_A;
        pendingSaveData = null;
        hasPendingViewState = false;

        // 4. 加载第一关
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("Level1_Room");
        }
        else
        {
            Debug.LogError("[GameManager] SceneController instance not found!");
            SceneManager.LoadScene("Level1_Room");
        }

        Debug.Log("[GameManager] New game started.");
    }

    /// <summary>
    /// 继续游戏 - 读取存档恢复进度
    /// </summary>
    public void ContinueGame()
    {
        Debug.Log("[GameManager] ========== Continuing game ==========");

        if (SaveLoadSystem.Instance == null)
        {
            Debug.LogError("[GameManager] SaveLoadSystem instance not found!");
            StartNewGame();
            return;
        }

        // 1. 检查是否有存档
        if (!SaveLoadSystem.Instance.HasSaveData())
        {
            Debug.LogWarning("[GameManager] No save data found. Starting new game instead.");
            StartNewGame();
            return;
        }

        // 2. 读取存档
        SaveData saveData = SaveLoadSystem.Instance.LoadGame();

        if (saveData == null)
        {
            Debug.LogWarning("[GameManager] Failed to load save data. Starting new game instead.");
            StartNewGame();
            return;
        }

        // 3. 保存存档数据，等场景加载后再应用
        pendingSaveData = saveData;

        // 4. 加载存档中的场景
        string sceneToLoad = saveData.currentSceneName;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = "Level1_Room";
        }

        Debug.Log($"[GameManager] Loading saved scene: {sceneToLoad}");

        if (SceneController.Instance != null)
        {
            // ⭐ 使用专门的存档加载方法，不会重置视图状态
            SceneController.Instance.LoadSceneFromSave(sceneToLoad);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }

        Debug.Log("[GameManager] Game continued from save.");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game.");

        // 退出前自动保存
        if (SaveLoadSystem.Instance != null &&
            CurrentGameState != GameState.MainMenu)
        {
            SaveLoadSystem.Instance.SaveGame();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============ 场景管理器注册 ============

    public void RegisterWallManager(WallManager manager)
    {
        currentWallManager = manager;
        Debug.Log($"[GameManager] WallManager registered: {manager.gameObject.scene.name}");

        // 如果有待恢复的视图状态，现在应用
        if (hasPendingViewState)
        {
            StartCoroutine(ApplyPendingViewStateDelayed());
        }
    }

    public void RegisterZoomController(FurnitureZoomController controller)
    {
        currentZoomController = controller;
        Debug.Log($"[GameManager] FurnitureZoomController registered: {controller.gameObject.scene.name}");
    }

    public void UnregisterSceneManagers()
    {
        currentWallManager = null;
        currentZoomController = null;
        Debug.Log("[GameManager] Scene managers unregistered");
    }

    // ============ 视图状态恢复 ============

    /// <summary>
    /// 恢复视图状态（由 SaveLoadSystem 调用）
    /// </summary>
    public void RestoreViewState(ViewState viewState)
    {
        Debug.Log($"[GameManager] RestoreViewState called: {viewState}");

        // 如果 WallManager 已注册，直接切换
        if (currentWallManager != null)
        {
            SwitchToView(viewState);

            // 如果是墙面视图，也更新 lastWallBeforeZoom
            if (IsWallView(viewState))
            {
                lastWallBeforeZoom = viewState;
            }
        }
        else
        {
            // WallManager 还没注册，保存待应用状态
            Debug.Log($"[GameManager] WallManager not ready, pending view state: {viewState}");
            pendingViewState = viewState;
            hasPendingViewState = true;
        }
    }

    /// <summary>
    /// 延迟应用待恢复的视图状态
    /// </summary>
    private IEnumerator ApplyPendingViewStateDelayed()
    {
        // 等待一帧确保 WallManager 完全初始化
        yield return null;

        if (hasPendingViewState && currentWallManager != null)
        {
            Debug.Log($"[GameManager] Applying pending view state: {pendingViewState}");
            SwitchToView(pendingViewState);

            if (IsWallView(pendingViewState))
            {
                lastWallBeforeZoom = pendingViewState;
            }

            hasPendingViewState = false;
        }
    }

    /// <summary>
    /// 判断是否是墙面视图
    /// </summary>
    private bool IsWallView(ViewState state)
    {
        return state == ViewState.Wall_A ||
               state == ViewState.Wall_B ||
               state == ViewState.Wall_C ||
               state == ViewState.Wall_D;
    }

    // ============ 存档辅助方法 ============

    /// <summary>
    /// 获取应该保存的视图状态（如果在放大视图中，返回上一个墙面）
    /// </summary>
    public ViewState GetViewStateForSave()
    {
        // 如果当前在墙面视图，直接返回当前状态
        if (IsInWallView())
        {
            return CurrentViewState;
        }

        // 如果在放大视图中，返回上一个墙面状态
        // 因为放大视图是临时的，玩家不希望继续游戏时还在放大视图里
        Debug.Log($"[GameManager] Currently in zoom view, saving wall state instead: {lastWallBeforeZoom}");
        return lastWallBeforeZoom;
    }

    /// <summary>
    /// 获取 lastWallBeforeZoom（用于存档）
    /// </summary>
    public ViewState GetLastWallBeforeZoom()
    {
        return lastWallBeforeZoom;
    }

    // ============ 视图切换功能 ============

    public void SwitchToView(ViewState targetView)
    {
        if (CurrentViewState == targetView)
            return;

        ViewState previousView = CurrentViewState;
        CurrentViewState = targetView;

        OnViewStateChanged?.Invoke(targetView);

        Debug.Log($"[GameManager] View: {previousView} → {targetView}");
    }

    public void SwitchToNextWall()
    {
        if (!IsInWallView()) return;

        ViewState nextWall = CurrentViewState switch
        {
            ViewState.Wall_A => ViewState.Wall_B,
            ViewState.Wall_B => ViewState.Wall_C,
            ViewState.Wall_C => ViewState.Wall_D,
            ViewState.Wall_D => ViewState.Wall_A,
            _ => CurrentViewState
        };

        SwitchToView(nextWall);
    }

    public void SwitchToPreviousWall()
    {
        if (!IsInWallView()) return;

        ViewState prevWall = CurrentViewState switch
        {
            ViewState.Wall_A => ViewState.Wall_D,
            ViewState.Wall_B => ViewState.Wall_A,
            ViewState.Wall_C => ViewState.Wall_B,
            ViewState.Wall_D => ViewState.Wall_C,
            _ => CurrentViewState
        };

        SwitchToView(prevWall);
    }

    public void EnterZoomView(ViewState zoomView)
    {
        if (IsInWallView())
            lastWallBeforeZoom = CurrentViewState;

        SwitchToView(zoomView);
    }

    public void ExitZoomView()
    {
        if (IsInWallView())
        {
            Debug.LogWarning("[GameManager] Already in wall view!");
            return;
        }

        SwitchToView(lastWallBeforeZoom);
    }

    public bool IsInWallView()
    {
        return CurrentViewState == ViewState.Wall_A ||
               CurrentViewState == ViewState.Wall_B ||
               CurrentViewState == ViewState.Wall_C ||
               CurrentViewState == ViewState.Wall_D;
    }
}