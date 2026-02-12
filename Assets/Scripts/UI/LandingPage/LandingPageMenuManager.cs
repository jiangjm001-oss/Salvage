// Assets/Scripts/UI/LandingPage/LandingPageMenuManager.cs
// Landing Page 菜单管理器 - 处理按钮点击事件
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class LandingPageMenuManager : MonoBehaviour
{
    [Header("=== 按钮引用 ===")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button settingsButton;

    [Header("=== 场景过渡 ===")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private string firstLevelSceneName = "Level1_Room";

    [Header("=== 音频设置 ===")]
    [SerializeField] private AudioSource uiAudioSource;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;

    [Header("=== Continue按钮状态 ===")]
    [SerializeField] private CanvasGroup continueButtonCanvasGroup;
    [SerializeField] private float disabledAlpha = 0.5f;

    private bool isTransitioning = false;

    private void Start()
    {
        // 绑定按钮事件
        SetupButtons();

        // 检查是否有存档，更新Continue按钮状态
        UpdateContinueButtonState();

        // 初始化淡出遮罩
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }
    }

    private void SetupButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartClicked);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(OnSettingsClicked);
        }
    }

    /// <summary>
    /// 更新Continue按钮的可用状态
    /// </summary>
    private void UpdateContinueButtonState()
    {
        bool hasSaveData = false;

        // 检查SaveLoadSystem是否存在以及是否有存档
        if (SaveLoadSystem.Instance != null)
        {
            hasSaveData = SaveLoadSystem.Instance.HasSaveData();
        }
        else
        {
            // 直接检查PlayerPrefs
            hasSaveData = PlayerPrefs.HasKey("SaveData");
        }

        if (continueButton != null)
        {
            continueButton.interactable = hasSaveData;
        }

        if (continueButtonCanvasGroup != null)
        {
            continueButtonCanvasGroup.alpha = hasSaveData ? 1f : disabledAlpha;
        }

        Debug.Log($"[LandingPageMenuManager] 存档状态: {(hasSaveData ? "有存档" : "无存档")}");
    }

    // ============ 按钮点击处理 ============

    private void OnStartClicked()
    {
        if (isTransitioning) return;

        PlayClickSound();
        Debug.Log("[LandingPageMenuManager] Start 按钮点击");

        StartCoroutine(TransitionToGame(true));
    }

    private void OnContinueClicked()
    {
        if (isTransitioning) return;

        PlayClickSound();
        Debug.Log("[LandingPageMenuManager] Continue 按钮点击");

        StartCoroutine(TransitionToGame(false));
    }

    private void OnExitClicked()
    {
        PlayClickSound();
        Debug.Log("[LandingPageMenuManager] Exit 按钮点击");

        // 退出游戏
        if (GameManager.Instance != null)
        {
            GameManager.Instance.QuitGame();
        }
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    private void OnSettingsClicked()
    {
        PlayClickSound();
        Debug.Log("[LandingPageMenuManager] Settings 按钮点击");

        // 打开设置面板
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OpenSettings();
        }
        else
        {
            Debug.LogWarning("[LandingPageMenuManager] SettingsManager 未找到");
        }
    }

    // ============ 场景过渡 ============

    /// <summary>
    /// 过渡到游戏场景
    /// </summary>
    private IEnumerator TransitionToGame(bool isNewGame)
    {
        isTransitioning = true;

        // 禁用所有按钮
        SetButtonsInteractable(false);

        // 淡出动画
        if (fadeOverlay != null)
        {
            fadeOverlay.blocksRaycasts = true;
            yield return StartCoroutine(FadeOut());
        }

        // 加载游戏
        if (isNewGame)
        {
            // 新游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartNewGame();
            }
            else if (SceneController.Instance != null)
            {
                SceneController.Instance.LoadScene(firstLevelSceneName);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(firstLevelSceneName);
            }
        }
        else
        {
            // 继续游戏
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ContinueGame();
            }
            else
            {
                Debug.LogWarning("[LandingPageMenuManager] GameManager 未找到，无法继续游戏");
            }
        }
    }

    /// <summary>
    /// 淡出动画
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            fadeOverlay.alpha = EaseInQuad(t);
            yield return null;
        }

        fadeOverlay.alpha = 1f;
    }

    /// <summary>
    /// 设置所有按钮的可交互状态
    /// </summary>
    private void SetButtonsInteractable(bool interactable)
    {
        if (startButton != null) startButton.interactable = interactable;
        if (continueButton != null && continueButton.interactable)
            continueButton.interactable = interactable;
        if (exitButton != null) exitButton.interactable = interactable;
        if (settingsButton != null) settingsButton.interactable = interactable;
    }

    // ============ 音频 ============

    private void PlayClickSound()
    {
        if (uiAudioSource != null && buttonClickSound != null)
        {
            uiAudioSource.PlayOneShot(buttonClickSound);
        }
        else if (AudioManager.Instance != null)
        {
            // 使用你项目中 AudioManager 的 PlaySFX 方法
            // 音效文件路径需要放在 Resources 文件夹下
            AudioManager.Instance.PlaySFX("Audio/SFX/ui_click");
        }
    }

    public void PlayHoverSound()
    {
        if (uiAudioSource != null && buttonHoverSound != null)
        {
            uiAudioSource.PlayOneShot(buttonHoverSound, 0.5f);
        }
        else if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Audio/SFX/ui_hover");
        }
    }

    // ============ 缓动函数 ============

    private float EaseInQuad(float t)
    {
        return t * t;
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 刷新Continue按钮状态（供外部调用）
    /// </summary>
    public void RefreshContinueButton()
    {
        UpdateContinueButtonState();
    }

    private void OnDestroy()
    {
        // 移除事件监听
        if (startButton != null) startButton.onClick.RemoveListener(OnStartClicked);
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
    }
}