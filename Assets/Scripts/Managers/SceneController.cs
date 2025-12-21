// Assets/Scripts/Managers/SceneController.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    // 标记是否正在从存档恢复（避免强制重置视图）
    private bool isRestoringFromSave = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[SceneController] Instance has been set.");
        }
        else
        {
            Debug.LogWarning($"[SceneController] Duplicate detected! Destroying this component.");
            Destroy(this);
        }
    }

    /// <summary>
    /// 加载场景
    /// </summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName, false));
    }

    /// <summary>
    /// 从存档加载场景（不会重置视图状态）
    /// </summary>
    public void LoadSceneFromSave(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName, true));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName, bool fromSave)
    {
        isRestoringFromSave = fromSave;

        // ⭐ 关键修改：如果要返回主菜单，先保存当前游戏进度
        if (sceneName == "LandingPage")
        {
            SaveBeforeReturnToMainMenu();
        }

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

        // 只有在非存档恢复模式下才重置到 Wall_A
        if (!isRestoringFromSave)
        {
            if (sceneName == "Level1_Room" || sceneName == "Level2_Room")
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SwitchToView(GameManager.ViewState.Wall_A);
                }
            }
        }
        else
        {
            Debug.Log("[SceneController] Restoring from save, skipping view reset.");
        }

        // 重置标记
        isRestoringFromSave = false;
    }

    /// <summary>
    /// 返回主菜单前保存游戏进度
    /// </summary>
    private void SaveBeforeReturnToMainMenu()
    {
        // 只在游戏关卡中保存（不在主菜单或Bootstrap中保存）
        if (GameManager.Instance == null)
        {
            return;
        }

        var currentState = GameManager.Instance.CurrentGameState;

        // 只有在关卡中才需要保存
        if (currentState == GameManager.GameState.Level1 ||
            currentState == GameManager.GameState.Level2)
        {
            Debug.Log("[SceneController] Saving game before returning to main menu...");

            if (SaveLoadSystem.Instance != null)
            {
                SaveLoadSystem.Instance.SaveGame();
            }
        }
    }

    /// <summary>
    /// 获取当前活动场景的名称
    /// </summary>
    public string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }
}