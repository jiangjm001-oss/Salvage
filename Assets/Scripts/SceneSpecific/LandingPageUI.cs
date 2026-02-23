// Assets/Scripts/SceneSpecific/LandingPageUI.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 主菜单 UI 控制器
/// 负责初始化主菜单场景的 UI 事件，并协调按钮动画和场景转场
/// </summary>
public class LandingPageUI : MonoBehaviour
{
    [Header("主菜单按钮引用")]
    [SerializeField] private Button startNewGameButton;
    [SerializeField] private Button continueGameButton;
    [SerializeField] private Button quitGameButton;

    [Header("可选：按钮文本引用")]
    [SerializeField] private Text continueButtonText;

    [Header("=== 转场设置 ===")]
    [Tooltip("点击按钮后，等待按钮退出动画完成再开始全屏渐隐")]
    [SerializeField] private bool waitForButtonAnimation = true;

    [Tooltip("所有按钮同时播放退出动画")]
    [SerializeField] private bool animateAllButtonsOnExit = true;

    [Tooltip("按钮退出动画的交错延迟（秒）")]
    [SerializeField] private float buttonExitStagger = 0.05f;

    // 按钮动画器缓存
    private MenuButtonAnimator startButtonAnimator;
    private MenuButtonAnimator continueButtonAnimator;
    private MenuButtonAnimator quitButtonAnimator;
    private List<MenuButtonAnimator> allButtonAnimators = new List<MenuButtonAnimator>();

    // 状态标记
    private bool isTransitioning = false;

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

        // ============ 获取按钮动画器 ============
        CacheButtonAnimators();

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
    /// 缓存按钮动画器引用
    /// </summary>
    private void CacheButtonAnimators()
    {
        allButtonAnimators.Clear();

        if (startNewGameButton != null)
        {
            startButtonAnimator = startNewGameButton.GetComponent<MenuButtonAnimator>();
            if (startButtonAnimator != null)
                allButtonAnimators.Add(startButtonAnimator);
        }

        if (continueGameButton != null)
        {
            continueButtonAnimator = continueGameButton.GetComponent<MenuButtonAnimator>();
            if (continueButtonAnimator != null)
                allButtonAnimators.Add(continueButtonAnimator);
        }

        if (quitGameButton != null)
        {
            quitButtonAnimator = quitGameButton.GetComponent<MenuButtonAnimator>();
            if (quitButtonAnimator != null)
                allButtonAnimators.Add(quitButtonAnimator);
        }

        Debug.Log($"[LandingPageUI] Cached {allButtonAnimators.Count} button animators.");
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
        if (isTransitioning) return;

        Debug.Log("[LandingPageUI] 'Start New Game' button clicked.");
        StartCoroutine(StartNewGameWithTransition());
    }

    private void OnContinueGameClicked()
    {
        if (isTransitioning) return;

        Debug.Log("[LandingPageUI] 'Continue Game' button clicked.");
        StartCoroutine(ContinueGameWithTransition());
    }

    private void OnQuitGameClicked()
    {
        Debug.Log("[LandingPageUI] 'Quit Game' button clicked.");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
    }

    // ============ 带动画的场景切换 ============

    /// <summary>
    /// 开始新游戏（带按钮动画 + 全屏转场）
    /// </summary>
    private IEnumerator StartNewGameWithTransition()
    {
        isTransitioning = true;

        // 1. 播放按钮退出动画
        if (waitForButtonAnimation)
        {
            yield return StartCoroutine(PlayAllButtonsExitAnimation());
        }

        // 2. 清除旧存档和背包
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteSaveData();
        }

        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearInventory();
        }

        // 注意：ViewState 会在场景加载后由 SceneController 自动重置为 Wall_A

        // 3. 使用转场管理器切换场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithTransition("Level1_Room");
        }
        else
        {
            // 回退：直接使用 SceneController
            Debug.LogWarning("[LandingPageUI] SceneTransitionManager not found, using SceneController.");
            if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadScene("Level1_Room");
            }
        }

        // 注意：场景切换后此协程会被销毁，无需重置 isTransitioning
    }

    /// <summary>
    /// 继续游戏（带按钮动画 + 全屏转场）
    /// </summary>
    private IEnumerator ContinueGameWithTransition()
    {
        isTransitioning = true;

        // 1. 播放按钮退出动画
        if (waitForButtonAnimation)
        {
            yield return StartCoroutine(PlayAllButtonsExitAnimation());
        }

        // 2. 加载存档
        if (SaveLoadSystem.Instance != null && SaveLoadSystem.Instance.HasSaveData())
        {
            SaveData saveData = SaveLoadSystem.Instance.LoadGame();

            if (saveData != null && !string.IsNullOrEmpty(saveData.currentSceneName))
            {
                // 设置待恢复的存档数据
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPendingSaveData(saveData);
                }

                // 3. 使用转场管理器切换场景
                if (SceneTransitionManager.Instance != null)
                {
                    SceneTransitionManager.Instance.LoadSceneWithTransition(saveData.currentSceneName);
                }
                else
                {
                    // 回退
                    if (SceneController.Instance != null)
                    {
                        SceneController.Instance.LoadSceneFromSave(saveData.currentSceneName);
                    }
                }
            }
            else
            {
                Debug.LogWarning("[LandingPageUI] Save data invalid, starting new game.");
                yield return StartCoroutine(StartNewGameWithTransition());
            }
        }
        else
        {
            Debug.LogWarning("[LandingPageUI] No save data, starting new game.");
            yield return StartCoroutine(StartNewGameWithTransition());
        }
    }

    /// <summary>
    /// 播放所有按钮的退出动画
    /// </summary>
    private IEnumerator PlayAllButtonsExitAnimation()
    {
        if (allButtonAnimators.Count == 0)
        {
            Debug.Log("[LandingPageUI] No button animators found, skipping exit animation.");
            yield break;
        }

        Debug.Log("[LandingPageUI] Playing button exit animations...");

        float maxDuration = 0f;

        // 启动所有按钮的退出动画（可选交错）
        for (int i = 0; i < allButtonAnimators.Count; i++)
        {
            var animator = allButtonAnimators[i];
            if (animator != null)
            {
                // 计算该按钮的退出时长
                float duration = animator.GetExitDuration();
                float totalTime = duration + (i * buttonExitStagger);
                if (totalTime > maxDuration)
                    maxDuration = totalTime;

                // 延迟启动（交错效果）
                if (buttonExitStagger > 0 && i > 0)
                {
                    StartCoroutine(DelayedExitAnimation(animator, i * buttonExitStagger));
                }
                else
                {
                    animator.PlayExitAnimation();
                }
            }
        }

        // 等待最长的动画完成
        yield return new WaitForSeconds(maxDuration);

        Debug.Log("[LandingPageUI] All button exit animations complete.");
    }

    /// <summary>
    /// 延迟播放退出动画（用于交错效果）
    /// </summary>
    private IEnumerator DelayedExitAnimation(MenuButtonAnimator animator, float delay)
    {
        yield return new WaitForSeconds(delay);
        animator?.PlayExitAnimation();
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