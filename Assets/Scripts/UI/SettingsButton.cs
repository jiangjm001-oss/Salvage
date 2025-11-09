using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置按钮 - 打开设置面板
/// 此脚本附加到各个场景的设置图标按钮上
/// </summary>
public class SettingsButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        if (button != null)
        {
            button.onClick.AddListener(OnSettingsButtonClicked);
        }
        else
        {
            Debug.LogError("[SettingsButton] Button component not found!");
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnSettingsButtonClicked);
        }
    }

    /// <summary>
    /// 设置按钮点击事件
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        Debug.Log("[SettingsButton] Settings button clicked");

        if (SettingsUI.Instance != null)
        {
            SettingsUI.Instance.ShowSettings();
        }
        else
        {
            Debug.LogError("[SettingsButton] SettingsUI.Instance not found!");
        }

        // 播放按钮音效
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("ButtonClick");
        }
    }
}
