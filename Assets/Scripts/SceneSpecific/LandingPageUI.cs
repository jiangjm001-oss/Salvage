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
        // 等待一帧，让所有其他脚本的Awake()和Start()都有机会执行
        yield return null;

        Debug.Log("LandingPageUI: Coroutine started. Checking for manager instances...");

        // 现在再次检查，此时管理器应该已经初始化完成
        if (GameManager.Instance == null || UIManager.Instance == null)
        {
            Debug.LogError("LandingPageUI: GameManager or UIManager instance is still null after waiting! Check your Bootstrap scene.");
            // 如果还是找不到，就不再继续执行，避免更多错误
            yield break;
        }

        Debug.Log("LandingPageUI: Manager instances found. Initializing buttons.");

        // 绑定“开始新游戏”按钮
        if (startNewGameButton != null)
        {
            startNewGameButton.onClick.AddListener(() => {
                Debug.Log("=== BUTTON CLICKED! ===");
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
    }
}