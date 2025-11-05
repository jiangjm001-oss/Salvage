using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ============ 游戏状态管理 ============
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

    // ============ 视图状态管理 ============
    public enum ViewState
    {
        Wall_A, Wall_B, Wall_C, Wall_D,

        // Level 1 放大视图
        Level1_Zoom_Mirror,
        Level1_Zoom_LowCabinet,
        Level1_Zoom_GrandfatherClock,
        Level1_Zoom_CoalHeater,

        // Level 2 放大视图
        Level2_Zoom_Mirror,
        Level2_Zoom_Painting,
        Level2_Zoom_Safe,
    }

    public ViewState CurrentViewState { get; private set; } = ViewState.Wall_A;
    public UnityEvent<ViewState> OnViewStateChanged = new UnityEvent<ViewState>();

    private ViewState lastWallBeforeZoom = ViewState.Wall_A;

    // ============ 场景管理器引用(v3.0新增) ============
    private WallManager currentWallManager;
    private FurnitureZoomController currentZoomController;

    /// <summary>
    /// 便捷访问:当前场景的WallManager
    /// </summary>
    public static WallManager CurrentWallManager => Instance?.currentWallManager;

    /// <summary>
    /// 便捷访问:当前场景的FurnitureZoomController
    /// </summary>
    public static FurnitureZoomController CurrentZoomController => Instance?.currentZoomController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始化游戏状态
        string currentSceneName = SceneManager.GetActiveScene().name;
        UpdateGameStateBasedOnScene(currentSceneName);
    }

    // ============ 游戏状态管理方法 ============

    /// <summary>
    /// 根据场景名称更新游戏状态
    /// </summary>
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

    /// <summary>
    /// 改变游戏状态
    /// </summary>
    public void ChangeGameState(GameState newState)
    {
        if (CurrentGameState == newState) return;

        CurrentGameState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] Game state changed to: {newState}");
    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("[GameManager] Starting new game.");
        
        // 删除任何现有存档
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteSaveData();
        }
        
        // 加载第一关
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("Level1_Room");
        }
        else
        {
            Debug.LogError("[GameManager] SceneController instance not found!");
        }
    }

    /// <summary>
    /// 继续游戏
    /// </summary>
    public void ContinueGame()
    {
        Debug.Log("[GameManager] Continuing game.");
        
        if (SaveLoadSystem.Instance != null)
        {
            SaveData saveData = SaveLoadSystem.Instance.LoadGame();
            
            if (saveData != null)
            {
                // 加载保存的场景
                if (SceneController.Instance != null)
                {
                    SceneController.Instance.LoadScene(saveData.currentSceneName);
                }
                else
                {
                    Debug.LogError("[GameManager] SceneController instance not found!");
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] No save data found. Starting new game instead.");
                StartNewGame();
            }
        }
        else
        {
            Debug.LogError("[GameManager] SaveLoadSystem instance not found!");
        }
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game.");
        
        // 在编辑器模式下停止播放
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 在构建的应用中退出
        Application.Quit();
        #endif
    }

    // ============ 场景管理器注册(v3.0新增) ============

    /// <summary>
    /// 由场景管理器在Awake时自动调用
    /// </summary>
    public void RegisterWallManager(WallManager manager)
    {
        currentWallManager = manager;
        Debug.Log($"[GameManager] WallManager registered: {manager.gameObject.scene.name}");
    }

    /// <summary>
    /// 由场景管理器在Awake时自动调用
    /// </summary>
    public void RegisterZoomController(FurnitureZoomController controller)
    {
        currentZoomController = controller;
        Debug.Log($"[GameManager] FurnitureZoomController registered: {controller.gameObject.scene.name}");
    }

    /// <summary>
    /// 场景切换时清理引用
    /// </summary>
    public void UnregisterSceneManagers()
    {
        currentWallManager = null;
        currentZoomController = null;
        Debug.Log("[GameManager] Scene managers unregistered");
    }

    // ============ 视图切换方法 ============

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
        if (IsInWallView()) return;
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