// Assets/Scripts/SceneSpecific/LandingPageUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 主菜单 UI 控制器
/// 负责初始化主菜单场景的 UI 事件
/// </summary>
public class LandingPageUI : MonoBehaviour
{
    [Header("主菜单按钮引用")]
    [SerializeField] private Button startNewGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button quitGameButton;

    [Header("可选：按钮文本引用")]
    [SerializeField] private Text continueButtonText;

    private void Start()
    {
        StartCoroutine(InitializeButtonsCoroutine());
    }

    /// <summary>
    /// 使用协程延迟初始化，确保所有管理器都已准备就绪
    /// </summary>
    private IEnumerator InitializeButtonsCoroutine()
    {
        // 等待一帧，让所有脚本的 Awake() 和 Start() 都执行完成
        yield return null;

        Debug.Log("[LandingPageUI] Initializing buttons...");

        // 检查管理器是否存在
        if (GameManager.Instance == null)
        {
            Debug.LogError("[LandingPageUI] GameManager.Instance is null!");
            yield break;
        }

        // ============ 绑定按钮事件 ============

        // 开始新游戏按钮
        if (startNewGameButton != null)
        {
            startNewGameButton.onClick.AddListener(OnStartNewGameClicked);
            Debug.Log("[LandingPageUI] 'Start New Game' button bound.");
        }
        else
        {
            Debug.LogWarning("[LandingPageUI] 'Start New Game' button is not assigned!");
        }

        // 继续游戏按钮
        if (continueGameButton != null)
        {
            continueGameButton.onClick.AddListener(OnContinueGameClicked);

            // 检查是否有存档，决定按钮是否可用
            UpdateContinueButtonState();

            Debug.Log("[LandingPageUI] 'Continue Game' button bound.");
        }
        else
        {
            Debug.LogWarning("[LandingPageUI] 'Continue Game' button is not assigned!");
        }

        // 退出游戏按钮
        if (quitGameButton != null)
        {
            quitGameButton.onClick.AddListener(OnQuitGameClicked);
            Debug.Log("[LandingPageUI] 'Quit Game' button bound.");
        }
        else
        {
            Debug.LogWarning("[LandingPageUI] 'Quit Game' button is not assigned!");
        }

        Debug.Log("[LandingPageUI] Button initialization complete.");
    }

    /// <summary>
    /// 更新"继续游戏"按钮状态
    /// </summary>
    private void UpdateContinueButtonState()
    {
        if (continueGameButton == null) return;

        bool hasSave = false;

        if (SaveLoadSystem.Instance != null)
        {
            hasSave = SaveLoadSystem.Instance.HasSaveData();
        }

        // 设置按钮可交互性
        continueGameButton.interactable = hasSave;

        // 更新按钮文本（如果有引用）
        if (continueButtonText != null)
        {
            continueButtonText.text = hasSave ? "继续游戏" : "继续游戏（无存档）";
            continueButtonText.color = hasSave ? Color.white : Color.gray;
        }

        Debug.Log($"[LandingPageUI] Continue button state: {(hasSave ? "Enabled" : "Disabled")}");
    }

    // ============ 按钮点击事件处理 ============

    private void OnStartNewGameClicked()
    {
        Debug.Log("[LandingPageUI] 'Start New Game' button clicked.");

        // 如果有存档，可以显示确认对话框（可选）
        // 这里直接开始新游戏
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StartNewGame();
        }
    }

    private void OnContinueGameClicked()
    {
        Debug.Log("[LandingPageUI] 'Continue Game' button clicked.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ContinueGame();
        }
    }

    private void OnQuitGameClicked()
    {
        Debug.Log("[LandingPageUI] 'Quit Game' button clicked.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }

    private void OnDestroy()
    {
        // 清理按钮事件监听器
        if (startNewGameButton != null)
        {
            startNewGameButton.onClick.RemoveListener(OnStartNewGameClicked);
        }

        if (continueGameButton != null)
        {
            continueGameButton.onClick.RemoveListener(OnContinueGameClicked);
        }

        if (quitGameButton != null)
        {
            quitGameButton.onClick.RemoveListener(OnQuitGameClicked);
        }
    }
}