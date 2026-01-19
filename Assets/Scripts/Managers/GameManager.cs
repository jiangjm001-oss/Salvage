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
        lv1_A_zoom_Desktop,
        lv1_A_zoom_GrandfatherClock,
        lv1_A_zoom_Heater,
        lv1_A_zoom_Towel,

        lv1_B_zoom_Window,
        lv1_B_zoom_RightDrawer,
        lv1_B_zoom_LeftDrawer,
        lv1_B_zoom_GroupPhoto,
        Level1_Zoom_TrashCan,
        lv1_B_zoom_Typewriter,
        lv1_B_zoom_Quill,
        lv1_B_zoom_Ink,
        lv1_B_zoom_ThreeBooks,

        lv1_C_zoom_Fireplace,
        lv1_C_zoom_OilPainting,
        lv1_C_zoom_EmptyPhotoFrame,

        lv1_D_zoom_sofa,
        lv1_D_zoom_coffeeTable,
        lv1_D_zoom_foldingScreen,
        lv1_D_zoom_Hammer,
        lv1_D_zoom_Clock,

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

    // ============ ⭐ 直接引用放大视图（新增）============
    private GameObject currentZoomViewObject = null;

    /// <summary>
    /// ⭐ 标志：是否正在使用直接引用模式的放大视图
    /// FurnitureZoomController 检查此标志，为 true 时不处理
    /// </summary>
    public bool IsUsingDirectZoomView { get; private set; } = false;

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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] Scene loaded: {scene.name}");

        // 清除直接引用的放大视图（场景切换后旧引用失效）
        currentZoomViewObject = null;
        IsUsingDirectZoomView = false;

        if (pendingSaveData != null)
        {
            StartCoroutine(ApplySaveDataDelayed());
        }
    }

    private IEnumerator ApplySaveDataDelayed()
    {
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

    public void StartNewGame()
    {
        Debug.Log("[GameManager] ========== Starting NEW game ==========");

        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteSaveData();
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearInventory();
        }

        CurrentViewState = ViewState.Wall_A;
        lastWallBeforeZoom = ViewState.Wall_A;
        pendingSaveData = null;
        hasPendingViewState = false;
        currentZoomViewObject = null;
        IsUsingDirectZoomView = false;

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

    public void ContinueGame()
    {
        Debug.Log("[GameManager] ========== Continuing game ==========");

        if (SaveLoadSystem.Instance == null)
        {
            Debug.LogError("[GameManager] SaveLoadSystem instance not found!");
            StartNewGame();
            return;
        }

        if (!SaveLoadSystem.Instance.HasSaveData())
        {
            Debug.LogWarning("[GameManager] No save data found. Starting new game instead.");
            StartNewGame();
            return;
        }

        SaveData saveData = SaveLoadSystem.Instance.LoadGame();

        if (saveData == null)
        {
            Debug.LogWarning("[GameManager] Failed to load save data. Starting new game instead.");
            StartNewGame();
            return;
        }

        pendingSaveData = saveData;

        string sceneToLoad = saveData.currentSceneName;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            sceneToLoad = "Level1_Room";
        }

        Debug.Log($"[GameManager] Loading saved scene: {sceneToLoad}");

        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadSceneFromSave(sceneToLoad);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }

        Debug.Log("[GameManager] Game continued from save.");
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game.");

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
        currentZoomViewObject = null;
        IsUsingDirectZoomView = false;
        Debug.Log("[GameManager] Scene managers unregistered");
    }

    // ============ 视图状态恢复 ============

    public void RestoreViewState(ViewState viewState)
    {
        Debug.Log($"[GameManager] RestoreViewState called: {viewState}");

        if (currentWallManager != null)
        {
            SwitchToView(viewState);

            if (IsWallView(viewState))
            {
                lastWallBeforeZoom = viewState;
            }
        }
        else
        {
            Debug.Log($"[GameManager] WallManager not ready, pending view state: {viewState}");
            pendingViewState = viewState;
            hasPendingViewState = true;
        }
    }

    private IEnumerator ApplyPendingViewStateDelayed()
    {
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

    private bool IsWallView(ViewState state)
    {
        return state == ViewState.Wall_A ||
               state == ViewState.Wall_B ||
               state == ViewState.Wall_C ||
               state == ViewState.Wall_D;
    }

    // ============ 存档辅助方法 ============

    public ViewState GetViewStateForSave()
    {
        if (IsInWallView())
        {
            return CurrentViewState;
        }

        Debug.Log($"[GameManager] Currently in zoom view, saving wall state instead: {lastWallBeforeZoom}");
        return lastWallBeforeZoom;
    }

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

        // ⭐ 清除直接引用标志（因为这是枚举模式）
        if (currentZoomViewObject != null)
        {
            currentZoomViewObject.SetActive(false);
            currentZoomViewObject = null;
        }
        IsUsingDirectZoomView = false;

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

    /// <summary>
    /// 进入放大视图（枚举方式，兼容旧代码）
    /// </summary>
    public void EnterZoomView(ViewState zoomView)
    {
        if (IsInWallView())
            lastWallBeforeZoom = CurrentViewState;

        // ⭐ 清除直接引用标志
        currentZoomViewObject = null;
        IsUsingDirectZoomView = false;

        SwitchToView(zoomView);
    }

    /// <summary>
    /// ⭐ 直接进入放大视图（GameObject 引用方式，无需枚举）
    /// </summary>
    public void EnterZoomViewDirect(GameObject zoomViewObject)
    {
        if (zoomViewObject == null)
        {
            Debug.LogError("[GameManager] 放大视图物体为空！");
            return;
        }

        // 记录当前墙面
        if (IsInWallView())
        {
            lastWallBeforeZoom = CurrentViewState;
        }

        // 隐藏之前的直接引用放大视图
        if (currentZoomViewObject != null && currentZoomViewObject != zoomViewObject)
        {
            currentZoomViewObject.SetActive(false);
        }

        // ⭐ 设置直接引用标志（FurnitureZoomController 会检查这个）
        IsUsingDirectZoomView = true;

        // 显示目标放大视图
        zoomViewObject.SetActive(true);
        currentZoomViewObject = zoomViewObject;

        // 更新状态（用于让 WallManager 隐藏墙面，但 FurnitureZoomController 会忽略）
        ViewState previousView = CurrentViewState;
        CurrentViewState = ViewState.Level1_Zoom_Mirror; // 仅表示"不在墙面"

        OnViewStateChanged?.Invoke(CurrentViewState);

        Debug.Log($"[GameManager] 进入放大视图(直接引用): {zoomViewObject.name}（上一墙面: {lastWallBeforeZoom}）");
    }

    /// <summary>
    /// 退出放大视图
    /// </summary>
    public void ExitZoomView()
    {
        // ⭐ 隐藏直接引用的放大视图
        if (currentZoomViewObject != null)
        {
            currentZoomViewObject.SetActive(false);
            currentZoomViewObject = null;
        }
        IsUsingDirectZoomView = false;

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

    public bool IsInZoomView()
    {
        return !IsInWallView();
    }

    public GameObject GetCurrentZoomViewObject()
    {
        return currentZoomViewObject;
    }
}