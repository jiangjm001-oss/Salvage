// Assets/Scripts/Managers/SettingsManager.cs
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("�����������")]
    public GameObject settingsPanel;          // ������������
    public GameObject tutorialPanel;          // ˵���������

    [Header("��ť����")]
    public Button continueButton;             // ������ť
    public Button musicToggleButton;          // �����л���ť
    public Button sfxToggleButton;            // ��Ч�л���ť
    public Button mainMenuButton;             // ���˵���ť
    public Button tutorialButton;             // ˵����ť
    public Button closeTutorialButton;        // �ر�˵����ť

    [Header("��ť����")]
    public Text musicButtonText;              // ���ְ�ť����
    public Text sfxButtonText;                // ��Ч��ť����

    [Header("˵������")]
    public Text tutorialText;                 // ˵����������

    private string currentSceneBeforeSettings; // ��¼������ǰ�ĳ���

    // ============ ������ʼ�� ============
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[SettingsManager] Instance has been set.");
        }
        else
        {
            Debug.LogWarning($"[SettingsManager] Duplicate SettingsManager detected on {gameObject.name}! Destroying this component only.");
            Destroy(this);  // 只销毁组件，不销毁整个 GameObject
            return;
        }
    }

    private void Start()
    {
        // ��ʼ��:�������
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        // �󶨰�ť�¼�
        BindButtonEvents();

        // ����AudioManager�¼��Ը��°�ť����
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMusicToggled.AddListener(UpdateMusicButtonText);
            AudioManager.Instance.OnSFXToggled.AddListener(UpdateSFXButtonText);
        }

        // ��ʼ����ť����
        UpdateMusicButtonText(AudioManager.Instance?.IsMusicEnabled() ?? true);
        UpdateSFXButtonText(AudioManager.Instance?.IsSFXEnabled() ?? true);

        Debug.Log("[SettingsManager] Initialized");
    }

    private void OnDestroy()
    {
        // ȡ������
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.OnMusicToggled.RemoveListener(UpdateMusicButtonText);
            AudioManager.Instance.OnSFXToggled.RemoveListener(UpdateSFXButtonText);
        }

        UnbindButtonEvents();
    }

    // ============ ��ť�¼��� ============

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

    // ============ ���������� ============

    /// <summary>
    /// ���������
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            Debug.Log("[SettingsManager] Settings panel opened");

            // ��¼��ǰ����
            if (SceneController.Instance != null)
            {
                currentSceneBeforeSettings = SceneController.Instance.GetCurrentSceneName();
            }
        }
    }

    /// <summary>
    /// �ر��������
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("[SettingsManager] Settings panel closed");
        }

        // ͬʱ�ر�˵�����
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    // ============ ��ť������� ============

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

        // �������˵�����
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

    // ============ UI���� ============

    private void UpdateMusicButtonText(bool isEnabled)
    {
        if (musicButtonText != null)
        {
            musicButtonText.text = isEnabled ? "����: ��" : "����: ��";
        }
    }

    private void UpdateSFXButtonText(bool isEnabled)
    {
        if (sfxButtonText != null)
        {
            sfxButtonText.text = isEnabled ? "��Ч: ��" : "��Ч: ��";
        }
    }

    /// <summary>
    /// ����˵����������
    /// </summary>
    public void SetTutorialText(string text)
    {
        if (tutorialText != null)
        {
            tutorialText.text = text;
        }
    }
}