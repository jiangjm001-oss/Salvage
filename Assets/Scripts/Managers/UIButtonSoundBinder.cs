// Assets/Scripts/Managers/UIButtonSoundBinder.cs
// 自动为所有子按钮绑定点击音效
using UnityEngine;
using UnityEngine.UI;

public class UIButtonSoundBinder : MonoBehaviour
{
    [Header("音效设置")]
    [Tooltip("直接拖入点击音效文件")]
    [SerializeField] private AudioClip clickSound;

    [Header("调试")]
    [SerializeField] private bool showDebugLog = false;

    private void Start()
    {
        BindAllButtons();
    }

    /// <summary>
    /// 查找所有子按钮并绑定点击音效
    /// </summary>
    private void BindAllButtons()
    {
        Button[] allButtons = GetComponentsInChildren<Button>(true);

        int boundCount = 0;
        foreach (Button button in allButtons)
        {
            button.onClick.AddListener(PlayClickSound);
            boundCount++;

            if (showDebugLog)
            {
                Debug.Log($"[UIButtonSoundBinder] 已绑定: {button.gameObject.name}");
            }
        }

        Debug.Log($"[UIButtonSoundBinder] 共绑定 {boundCount} 个按钮");
    }

    private void PlayClickSound()
    {
        if (AudioManager.Instance != null && clickSound != null)
        {
            AudioManager.Instance.PlaySFX(clickSound);
        }
    }
}