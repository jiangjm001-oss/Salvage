// Assets/Scripts/UI/TextPromptInteraction.cs
// 与 InteractableObject 系统集成的适配器
// 可以挂载到同一物体上，通过 InteractableObject 的 OnTrigger 事件触发
using UnityEngine;

/// <summary>
/// 提示文字交互适配器
/// 
/// 使用方式一（推荐）：
/// 1. 在物体上添加 InteractableObject 组件，类型设为 Trigger
/// 2. 在同一物体上添加此组件
/// 3. 将 InteractableObject 的 OnTrigger 事件绑定到此组件的 ShowPrompt() 方法
/// 
/// 使用方式二：
/// 直接使用 TextPromptTrigger 组件（不依赖 InteractableObject）
/// </summary>
public class TextPromptInteraction : MonoBehaviour
{
    [Header("文字配置")]
    [Tooltip("要显示的文字（每个元素为一句话，点击切换）")]
    [TextArea(2, 5)]
    [SerializeField] private string[] messages = new string[] { "这是一段提示文字。" };

    [Header("设置")]
    [Tooltip("是否只显示一次（之后再触发无反应）")]
    [SerializeField] private bool showOnce = false;

    [Tooltip("用于存档的唯一ID（留空则不存档）")]
    [SerializeField] private string uniqueID = "";

    // 内部状态
    private bool hasShown = false;

    private void Awake()
    {
        // 加载状态
        if (!string.IsNullOrEmpty(uniqueID))
        {
            hasShown = PlayerPrefs.GetInt($"TextPrompt_{uniqueID}_Shown", 0) == 1;
        }
    }

    /// <summary>
    /// 显示提示文字
    /// 用于绑定到 InteractableObject 的 OnTrigger 事件
    /// </summary>
    public void ShowPrompt()
    {
        if (showOnce && hasShown)
        {
            Debug.Log($"[TextPromptInteraction] {gameObject.name} 已显示过，跳过");
            return;
        }

        if (TextPromptManager.Instance != null)
        {
            TextPromptManager.Instance.ShowPrompt(messages);

            if (showOnce)
            {
                hasShown = true;
                SaveState();
            }
        }
        else
        {
            Debug.LogWarning("[TextPromptInteraction] TextPromptManager 未找到！");
        }
    }

    /// <summary>
    /// 显示自定义文字
    /// </summary>
    public void ShowCustomPrompt(string message)
    {
        if (TextPromptManager.Instance != null)
        {
            TextPromptManager.Instance.ShowPrompt(message);
        }
    }

    /// <summary>
    /// 重置显示状态
    /// </summary>
    public void ResetShowState()
    {
        hasShown = false;
        if (!string.IsNullOrEmpty(uniqueID))
        {
            PlayerPrefs.DeleteKey($"TextPrompt_{uniqueID}_Shown");
            PlayerPrefs.Save();
        }
    }

    private void SaveState()
    {
        if (string.IsNullOrEmpty(uniqueID)) return;
        PlayerPrefs.SetInt($"TextPrompt_{uniqueID}_Shown", 1);
        PlayerPrefs.Save();
    }
}
