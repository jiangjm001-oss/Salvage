// Assets/Scripts/SceneSpecific/LandingPageUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections; // 引入协程命名空间

/// <summary>
/// 这个脚本只存在于 LandingPage 场景，负责初始化该场景的UI事件。
/// 它是连接场景内UI和全局管理器的桥梁。
/// </summary>
public class LandingPageUI : MonoBehaviour
{
    [Header("主菜单按钮引用")]
    [SerializeField] private Button startNewGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button quitGameButton;

    private void Start()
    {
        // ✅ 修改：不再直接调用，而是启动一个协程
        StartCoroutine(InitializeButtonsCoroutine());
    }

    /// <summary>
    /// 使用协程来延迟初始化，确保所有管理器都已准备就绪
    /// </summary>
    private IEnumerator InitializeButtonsCoroutine()
    {
        Debug.Log("=== LandingPageUI Initialization Started ===");

        // 等待多帧，让管理器有时间初始化
        int maxWaitFrames = 30; // 最多等待30帧（约0.5秒）
        int framesWaited = 0;

        while (framesWaited < maxWaitFrames)
        {
            framesWaited++;

            // 每5帧打印一次状态
            if (framesWaited % 5 == 0)
            {
                Debug.Log($"LandingPageUI: Waiting for managers... (frame {framesWaited}/{maxWaitFrames})");
                Debug.Log($"  - GameManager.Instance: {GameManager.Instance != null}");
                Debug.Log($"  - UIManager.Instance: {UIManager.Instance != null}");
            }

            // 如果管理器都就绪，立即退出等待
            if (GameManager.Instance != null && UIManager.Instance != null)
            {
                Debug.Log($"LandingPageUI: Managers found after {framesWaited} frames!");
                break;
            }

            yield return null;
        }

        // 最终检查
        if (GameManager.Instance == null || UIManager.Instance == null)
        {
            Debug.LogError("=== LandingPageUI CRITICAL ERROR ===");
            Debug.LogError($"Managers still null after {framesWaited} frames!");
            Debug.LogError($"  - GameManager.Instance: {GameManager.Instance}");
            Debug.LogError($"  - UIManager.Instance: {UIManager.Instance}");
            Debug.LogError("Please ensure you start the game from the Bootstrap scene!");
            Debug.LogError("Check that _Managers_Prefab is assigned in Bootstrap scene.");
            yield break;
        }

        Debug.Log("LandingPageUI: All managers ready. Initializing buttons...");

        // 绑定"开始新游戏"按钮
        if (startNewGameButton != null)
        {
            startNewGameButton.onClick.AddListener(() => {
                Debug.Log("LandingPageUI: 'Start New Game' button clicked.");
                GameManager.Instance.StartNewGame();
            });
        }
        else
        {
            Debug.LogWarning("LandingPageUI: 'Start New Game' button is not assigned in the Inspector!");
        }

        // 绑定“继续游戏”按钮
        if (continueGameButton != null)
        {
            continueGameButton.onClick.AddListener(() => {
                Debug.Log("LandingPageUI: 'Continue Game' button clicked.");
                GameManager.Instance.ContinueGame();
            });
        }
        else
        {
            Debug.LogWarning("LandingPageUI: 'Continue Game' button is not assigned in the Inspector!");
        }

        // 绑定“退出游戏”按钮
        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(() => {
                Debug.Log("LandingPageUI: 'Quit Game' button clicked.");
                GameManager.Instance.QuitGame();
            });
        }
        else
        {
            Debug.LogWarning("LandingPageUI: 'Quit Game' button is not assigned in the Inspector!");
        }

        Debug.Log("=== LandingPageUI Initialization Complete ===");
    }
}