using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置界面UI - 管理设置面板的显示和交互
/// </summary>
public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button musicToggleButton;
    [SerializeField] private Button sfxToggleButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button instructionsButton;

    [Header("Button Text")]
    [SerializeField] private Text musicButtonText;
    [SerializeField] private Text sfxButtonText;

    [Header("Instructions Popup")]
    [SerializeField] private GameObject instructionsPopup;
    [SerializeField] private Button closeInstructionsButton;

    private string previousSceneName = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 初始化按钮事件
        SetupButtons();

        // 初始化面板状态
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (instructionsPopup != null)
        {
            instructionsPopup.SetActive(false);
        }

        // 更新按钮文本
        UpdateButtonTexts();

        // 订阅设置变化事件
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnMusicEnabledChanged.AddListener(OnMusicEnabledChanged);
            SettingsManager.Instance.OnSFXEnabledChanged.AddListener(OnSFXEnabledChanged);
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnMusicEnabledChanged.RemoveListener(OnMusicEnabledChanged);
            SettingsManager.Instance.OnSFXEnabledChanged.RemoveListener(OnSFXEnabledChanged);
        }
    }

    /// <summary>
    /// 设置按钮事件
    /// </summary>
    private void SetupButtons()
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

        if (instructionsButton != null)
        {
            instructionsButton.onClick.AddListener(OnInstructionsClicked);
        }

        if (closeInstructionsButton != null)
        {
            closeInstructionsButton.onClick.AddListener(OnCloseInstructionsClicked);
        }
    }

    /// <summary>
    /// 显示设置面板
    /// </summary>
    public void ShowSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            UpdateButtonTexts();
            Debug.Log("[SettingsUI] Settings panel opened");
        }
    }

    /// <summary>
    /// 隐藏设置面板
    /// </summary>
    public void HideSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("[SettingsUI] Settings panel closed");
        }

        // 同时关闭说明弹窗
        if (instructionsPopup != null && instructionsPopup.activeSelf)
        {
            instructionsPopup.SetActive(false);
        }
    }

    /// <summary>
    /// 继续按钮 - 返回上一页
    /// </summary>
    private void OnContinueClicked()
    {
        Debug.Log("[SettingsUI] Continue button clicked");
        HideSettings();

        // 播放按钮音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
    }

    /// <summary>
    /// 音乐开关按钮
    /// </summary>
    private void OnMusicToggleClicked()
    {
        Debug.Log("[SettingsUI] Music toggle button clicked");

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ToggleMusic();
        }

        // 播放按钮音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
    }

    /// <summary>
    /// 音效开关按钮
    /// </summary>
    private void OnSFXToggleClicked()
    {
        Debug.Log("[SettingsUI] SFX toggle button clicked");

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ToggleSFX();
        }

        // 播放按钮音效（在切换之前播放）
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
    }

    /// <summary>
    /// 主菜单按钮 - 返回LandingPage
    /// </summary>
    private void OnMainMenuClicked()
    {
        Debug.Log("[SettingsUI] Main menu button clicked");

        HideSettings();

        // 播放按钮音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }

        // 返回主菜单
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("LandingPage");
        }
    }

    /// <summary>
    /// 说明按钮 - 显示游戏说明
    /// </summary>
    private void OnInstructionsClicked()
    {
        Debug.Log("[SettingsUI] Instructions button clicked");

        if (instructionsPopup != null)
        {
            instructionsPopup.SetActive(true);
        }

        // 播放按钮音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
    }

    /// <summary>
    /// 关闭说明弹窗
    /// </summary>
    private void OnCloseInstructionsClicked()
    {
        Debug.Log("[SettingsUI] Close instructions button clicked");

        if (instructionsPopup != null)
        {
            instructionsPopup.SetActive(false);
        }

        // 播放按钮音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
    }

    /// <summary>
    /// 更新按钮文本
    /// </summary>
    private void UpdateButtonTexts()
    {
        if (SettingsManager.Instance == null) return;

        if (musicButtonText != null)
        {
            musicButtonText.text = SettingsManager.Instance.IsMusicEnabled ? "音乐: 开" : "音乐: 关";
        }

        if (sfxButtonText != null)
        {
            sfxButtonText.text = SettingsManager.Instance.IsSFXEnabled ? "音效: 开" : "音效: 关";
        }
    }

    /// <summary>
    /// 音乐设置变化回调
    /// </summary>
    private void OnMusicEnabledChanged(bool enabled)
    {
        if (musicButtonText != null)
        {
            musicButtonText.text = enabled ? "音乐: 开" : "音乐: 关";
        }
    }

    /// <summary>
    /// 音效设置变化回调
    /// </summary>
    private void OnSFXEnabledChanged(bool enabled)
    {
        if (sfxButtonText != null)
        {
            sfxButtonText.text = enabled ? "音效: 开" : "音效: 关";
        }
    }

    /// <summary>
    /// 检查设置面板是否打开
    /// </summary>
    public bool IsSettingsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }
}
