using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 设置按钮组件 - 附加到场景中的设置图标按钮上
/// 点击后打开设置面板
/// </summary>
[RequireComponent(typeof(Button))]
public class SettingsButton : MonoBehaviour
{
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnSettingsButtonClicked);
            Debug.Log("[SettingsButton] Settings button initialized");
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

    private void OnSettingsButtonClicked()
    {
        Debug.Log("[SettingsButton] Settings button clicked");

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OpenSettings();
        }
        else
        {
            Debug.LogError("[SettingsButton] SettingsManager.Instance is null!");
        }
    }
}
