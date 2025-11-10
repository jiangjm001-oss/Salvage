// Assets/Scripts/Managers/SettingsManager.cs
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("设置面板引用")]
    public GameObject settingsPanel;          // 设置面板根对象
    public GameObject tutorialPanel;          // 说明弹窗面板

    [Header("按钮引用")]
    public Button continueButton;             // 继续按钮
    public Button musicToggleButton;          // 音乐切换按钮
    public Button sfxToggleButton;            // 音效切换按钮
    public Button mainMenuButton;             // 主菜单按钮
    public Button tutorialButton;             // 说明按钮
    public Button closeTutorialButton;        // 关闭说明按钮

    [Header("按钮文字")]
    public Text musicButtonText;              // 音乐按钮文字
    public Text sfxButtonText;                // 音效按钮文字

    [Header("说明内容")]
    public Text tutorialText;                 // 说明文字内容

    private string currentSceneBeforeSettings; // 记录打开设置前的场景

    // ============ 单例初始化 ============
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 初始化:隐藏面板
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        // 绑定按钮事件
        BindButtonEvents();

        // 订阅AudioManager事件以更新按钮文字
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMusicToggled.AddListener(UpdateMusicButtonText);
            AudioManager.Instance.OnSFXToggled.AddListener(UpdateSFXButtonText);
        }

        // 初始化按钮文字
        UpdateMusicButtonText(AudioManager.Instance?.IsMusicEnabled() ?? true);
        UpdateSFXButtonText(AudioManager.Instance?.IsSFXEnabled() ?? true);

        Debug.Log("[SettingsManager] Initialized");
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMusicToggled.RemoveListener(UpdateMusicButtonText);
            AudioManager.Instance.OnSFXToggled.RemoveListener(UpdateSFXButtonText);
        }

        UnbindButtonEvents();
    }

    // ============ 按钮事件绑定 ============

    private void BindButtonEvents()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.AddListener(OnMusicToggleClicked);
        }

        if (sfxToggleButton != null)
        {
            sfxToggleButton.onClick.AddListener(OnSFXToggleClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (tutorialButton != null)
        {
            tutorialButton.onClick.AddListener(OnTutorialClicked);
        }

        if (closeTutorialButton != null)
        {
            closeTutorialButton.onClick.AddListener(OnCloseTutorialClicked);
        }
    }

    private void UnbindButtonEvents()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }

        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.RemoveListener(OnMusicToggleClicked);
        }

        if (sfxToggleButton != null)
        {
            sfxToggleButton.onClick.RemoveListener(OnSFXToggleClicked);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        }

        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveListener(OnTutorialClicked);
        }

        if (closeTutorialButton != null)
        {
            closeTutorialButton.onClick.RemoveListener(OnCloseTutorialClicked);
        }
    }

    // ============ 设置面板控制 ============

    /// <summary>
    /// 打开设置面板
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            Debug.Log("[SettingsManager] Settings panel opened");

            // 记录当前场景
            if (SceneController.Instance != null)
            {
                currentSceneBeforeSettings = SceneController.Instance.GetCurrentSceneName();
            }
        }
    }

    /// <summary>
    /// 关闭设置面板
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("[SettingsManager] Settings panel closed");
        }

        // 同时关闭说明面板
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    // ============ 按钮点击处理 ============

    private void OnContinueClicked()
    {
        Debug.Log("[SettingsManager] Continue button clicked");
        CloseSettings();
    }

    private void OnMusicToggleClicked()
    {
        Debug.Log("[SettingsManager] Music toggle button clicked");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleMusic();
        }
    }

    private void OnSFXToggleClicked()
    {
        Debug.Log("[SettingsManager] SFX toggle button clicked");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ToggleSFX();
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("[SettingsManager] Main menu button clicked");
        CloseSettings();

        // 加载主菜单场景
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("LandingPage");
        }
    }

    private void OnTutorialClicked()
    {
        Debug.Log("[SettingsManager] Tutorial button clicked");
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }

    private void OnCloseTutorialClicked()
    {
        Debug.Log("[SettingsManager] Close tutorial button clicked");
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    // ============ UI更新 ============

    private void UpdateMusicButtonText(bool isEnabled)
    {
        if (musicButtonText != null)
        {
            musicButtonText.text = isEnabled ? "音乐: 开" : "音乐: 关";
        }
    }

    private void UpdateSFXButtonText(bool isEnabled)
    {
        if (sfxButtonText != null)
        {
            sfxButtonText.text = isEnabled ? "音效: 开" : "音效: 关";
        }
    }

    /// <summary>
    /// 设置说明文字内容
    /// </summary>
    public void SetTutorialText(string text)
    {
        if (tutorialText != null)
        {
            tutorialText.text = text;
        }
    }
}