using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

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

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        // 清理旧场景的管理器引用
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UnregisterSceneManagers();
        }

        // 异步加载场景
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            Debug.Log($"Loading {sceneName}: {progress * 100}%");
            yield return null;
        }

        // ✅ 新场景的WallManager和FurnitureZoomController会在它们的Awake自动注册
        // ✅ 无需手动调用绑定方法
        // ✅ 无隐藏依赖!

        // ⭐ 添加这部分 - 根据场景名称更新游戏状态
        UpdateGameStateBasedOnScene(sceneName);

        Debug.Log($"[SceneController] Scene loaded: {sceneName}");

        // 根据场景控制背包UI显示
        if (sceneName == "Level1_Room" || sceneName == "Level2_Room")
        {
            UIManager.Instance?.ShowInventoryUI();
        }
        else
        {
            UIManager.Instance?.HideInventoryUI();
        }

        // 通知GameManager更新游戏状态
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateGameStateBasedOnScene(sceneName);
        }

        // 如果是关卡场景,重置到Wall_A
        if (sceneName == "Level1_Room" || sceneName == "Level2_Room")
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SwitchToView(GameManager.ViewState.Wall_A);
            }
        }
    }
    // ⭐ 添加这个新方法
    private void UpdateGameStateBasedOnScene(string sceneName)
    {
        GameManager.GameState newState = sceneName switch
        {
            "LandingPage" => GameManager.GameState.MainMenu,
            "Level1_Room" => GameManager.GameState.Level1,
            "Level2_Room" => GameManager.GameState.Level2,
            "EndingScene" => GameManager.GameState.Ending,
            _ => GameManager.Instance.CurrentGameState
        };

        GameManager.Instance.ChangeGameState(newState);
    }


    /// <summary>
    /// 获取当前活动场景的名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
}