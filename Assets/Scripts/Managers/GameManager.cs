using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ============ ��Ϸ״̬���� ============
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

    // ============ ��ͼ״̬���� ============
    public enum ViewState
    {
        Wall_A, Wall_B, Wall_C, Wall_D,

        // Level 1 �Ŵ���ͼ
        Level1_Zoom_Mirror,
        Level1_Zoom_LowCabinet,
        Level1_Zoom_GrandfatherClock,
        Level1_Zoom_CoalHeater,

        // Level 2 �Ŵ���ͼ
        Level2_Zoom_Mirror,
        Level2_Zoom_Painting,
        Level2_Zoom_Safe,
    }

    public ViewState CurrentViewState { get; private set; } = ViewState.Wall_A;
    public UnityEvent<ViewState> OnViewStateChanged = new UnityEvent<ViewState>();

    private ViewState lastWallBeforeZoom = ViewState.Wall_A;

    // ============ ��������������(v3.0����) ============
    private WallManager currentWallManager;
    private FurnitureZoomController currentZoomController;

    /// <summary>
    /// ��ݷ���:��ǰ������WallManager
    /// </summary>
    public static WallManager CurrentWallManager => Instance?.currentWallManager;

    /// <summary>
    /// ��ݷ���:��ǰ������FurnitureZoomController
    /// </summary>
    public static FurnitureZoomController CurrentZoomController => Instance?.currentZoomController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameManager] Instance initialized and marked as DontDestroyOnLoad");
        }
        else
        {
            Debug.LogWarning($"[GameManager] Duplicate instance detected on {gameObject.name}, destroying...");
            Destroy(gameObject);
            return;
        }
    }


    private void Start()
    {
        // BootstrapLoader ���Ѿ�������������ʼ������������������
        // ���ﲻ����Զ�����ת������������������

        // �������������л�ʱ������Ϸ״̬
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (currentSceneName != "Bootstrap")
        {
            UpdateGameStateBasedOnScene(currentSceneName);
        }
        else
        {
            Debug.Log("[GameManager] Initialized in Bootstrap scene, waiting for scene transition...");
        }
    }


    // ============ ��Ϸ״̬�������� ============

    /// <summary>
    /// ���ݳ������Ƹ�����Ϸ״̬
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
    /// �ı���Ϸ״̬
    /// </summary>
    public void ChangeGameState(GameState newState)
    {
        if (CurrentGameState == newState) return;

        CurrentGameState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] Game state changed to: {newState}");
    }

    /// <summary>
    /// ��ʼ����Ϸ
    /// </summary>
    public void StartNewGame()
    {
        Debug.Log("[GameManager] Starting new game.");
        
        // ɾ���κ����д浵
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteSaveData();
        }
        
        // ���ص�һ��
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
    /// ������Ϸ
    /// </summary>
    public void ContinueGame()
    {
        Debug.Log("[GameManager] Continuing game.");
        
        if (SaveLoadSystem.Instance != null)
        {
            SaveData saveData = SaveLoadSystem.Instance.LoadGame();
            
            if (saveData != null)
            {
                // ���ر���ĳ���
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
    /// �˳���Ϸ
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting game.");
        
        // �ڱ༭��ģʽ��ֹͣ����
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // �ڹ�����Ӧ�����˳�
        Application.Quit();
        #endif
    }

    // ============ ����������ע��(v3.0����) ============

    /// <summary>
    /// �ɳ�����������Awakeʱ�Զ�����
    /// </summary>
    public void RegisterWallManager(WallManager manager)
    {
        currentWallManager = manager;
        Debug.Log($"[GameManager] WallManager registered: {manager.gameObject.scene.name}");
    }

    /// <summary>
    /// �ɳ�����������Awakeʱ�Զ�����
    /// </summary>
    public void RegisterZoomController(FurnitureZoomController controller)
    {
        currentZoomController = controller;
        Debug.Log($"[GameManager] FurnitureZoomController registered: {controller.gameObject.scene.name}");
    }

    /// <summary>
    /// �����л�ʱ��������
    /// </summary>
    public void UnregisterSceneManagers()
    {
        currentWallManager = null;
        currentZoomController = null;
        Debug.Log("[GameManager] Scene managers unregistered");
    }

    // ============ ��ͼ�л����� ============

    public void SwitchToView(ViewState targetView)
    {
        if (CurrentViewState == targetView)
            return;

        ViewState previousView = CurrentViewState;
        CurrentViewState = targetView;

        OnViewStateChanged?.Invoke(targetView);

        Debug.Log($"[GameManager] View: {previousView} �� {targetView}");
    }
    /// <summary>
    /// �л�����һ��ǽ (ѭ��: A��B��C��D��A)
    /// </summary>
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
    /// <summary>
    /// �л�����һ��ǽ (ѭ��: A��D��C��B��A)
    /// </summary>
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
    /// ����Ŵ���ͼ
    /// </summary>
    public void EnterZoomView(ViewState zoomView)
    {
        if (IsInWallView())
            lastWallBeforeZoom = CurrentViewState;

        SwitchToView(zoomView);
    }
    /// <summary>
    /// �˳��Ŵ���ͼ,���ص�����ǰ��ǽ��
    /// </summary>
    public void ExitZoomView()
    {
        if (IsInWallView())
        {
            Debug.LogWarning("[GameManager] Already in wall view!");
            return;
        }

        SwitchToView(lastWallBeforeZoom);
    }
    /// <summary>
    /// �жϵ�ǰ�Ƿ���ǽ����ͼ(���ǷŴ���ͼ)
    /// </summary>
    public bool IsInWallView()
    {
        return CurrentViewState == ViewState.Wall_A ||
               CurrentViewState == ViewState.Wall_B ||
               CurrentViewState == ViewState.Wall_C ||
               CurrentViewState == ViewState.Wall_D;
    }
}