// Assets/Scripts/UI/TextPromptTrigger.cs
// 提示文字触发器 - 挂载到可点击物体上
// 点击 Collider 后触发 TextPromptManager 显示文字
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 提示文字触发器
/// 挂载到带有 Collider2D 的物体上，点击后显示配置的文字
/// 
/// 使用方式：
/// 1. 确保物体有 Collider2D 组件
/// 2. 确保物体在 Interactable 层
/// 3. 在 Inspector 中配置要显示的文字
/// 
/// 支持三种触发方式：
/// - 直接鼠标点击检测（OnMouseDown）
/// - 通过 InteractableObject 触发（OnTrigger 事件）
/// - 手动调用 TriggerPrompt()
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TextPromptTrigger : MonoBehaviour
{
    // ============ 文字配置 ============
    [Header("文字配置")]
    [Tooltip("要显示的文字内容（支持多行，每个元素为一句话）")]
    [TextArea(2, 5)]
    [SerializeField] private string[] messages = new string[] { "这是一段提示文字。" };

    [Tooltip("是否随机打乱文字顺序")]
    [SerializeField] private bool shuffleMessages = false;

    // ============ 触发设置 ============
    [Header("触发设置")]
    [Tooltip("是否启用直接点击触发（OnMouseDown）")]
    [SerializeField] private bool enableDirectClick = true;

    [Tooltip("是否只能触发一次")]
    [SerializeField] private bool triggerOnce = false;

    [Tooltip("触发后的冷却时间（秒，0=无冷却）")]
    [SerializeField] private float cooldownTime = 0.5f;

    [Tooltip("是否需要特定物品才能触发")]
    [SerializeField] private bool requireItem = false;

    [Tooltip("需要的物品（如果启用 requireItem）")]
    [SerializeField] private ItemData requiredItemData;

    [Tooltip("需要物品时，没有物品的提示")]
    [SerializeField] private string noItemMessage = "这里需要什么东西...";

    // ============ 条件设置 ============
    [Header("条件设置")]
    [Tooltip("是否检查游戏状态")]
    [SerializeField] private bool checkGameState = false;

    [Tooltip("仅在此游戏状态下触发")]
    [SerializeField] private GameManager.GameState allowedGameState = GameManager.GameState.Level1;

    [Tooltip("是否检查视图状态")]
    [SerializeField] private bool checkViewState = false;

    [Tooltip("仅在此视图状态下触发")]
    [SerializeField] private GameManager.ViewState allowedViewState = GameManager.ViewState.Wall_A;

    // ============ 状态存档 ============
    [Header("状态存档")]
    [Tooltip("物体唯一ID（用于存档，留空则不存档）")]
    [SerializeField] private string objectID = "";

    [Tooltip("是否已触发过（仅当 triggerOnce = true 时使用）")]
    [HideInInspector]
    [SerializeField] private bool hasTriggered = false;

    // ============ 事件 ============
    [Header("事件")]
    [Tooltip("触发显示文字时")]
    public UnityEvent OnTriggerPrompt;

    [Tooltip("文字全部显示完毕时")]
    public UnityEvent OnPromptComplete;

    // ============ 内部状态 ============
    private bool isOnCooldown = false;
    private Collider2D col;

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        // 加载存档状态
        LoadState();
    }

    private void OnEnable()
    {
        // 订阅 TextPromptManager 的完成事件
        if (TextPromptManager.Instance != null)
        {
            TextPromptManager.Instance.OnAllMessagesComplete.AddListener(OnMessagesComplete);
        }
    }

    private void OnDisable()
    {
        if (TextPromptManager.Instance != null)
        {
            TextPromptManager.Instance.OnAllMessagesComplete.RemoveListener(OnMessagesComplete);
        }
    }

    // ============ 鼠标点击检测 ============

    private void OnMouseDown()
    {
        if (!enableDirectClick) return;

        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        TriggerPrompt();
    }

    // ============ 公共接口 ============

    /// <summary>
    /// 触发显示提示文字
    /// 可被 InteractableObject 的 OnTrigger 事件调用
    /// </summary>
    public void TriggerPrompt()
    {
        // 检查是否可以触发
        if (!CanTrigger())
        {
            Debug.Log($"[TextPromptTrigger] {gameObject.name} 无法触发（条件不满足）");
            return;
        }

        // 检查物品需求
        if (requireItem)
        {
            if (!CheckRequiredItem())
            {
                // 显示无物品提示
                ShowNoItemMessage();
                return;
            }
        }

        // 获取要显示的消息
        string[] messagesToShow = GetMessagesToShow();

        if (messagesToShow.Length == 0)
        {
            Debug.LogWarning($"[TextPromptTrigger] {gameObject.name} 没有配置文字内容");
            return;
        }

        // 调用 TextPromptManager 显示
        if (TextPromptManager.Instance != null)
        {
            TextPromptManager.Instance.ShowPrompt(messagesToShow);
            Debug.Log($"[TextPromptTrigger] {gameObject.name} 触发显示 {messagesToShow.Length} 条消息");

            // 触发事件
            OnTriggerPrompt?.Invoke();

            // 标记已触发
            if (triggerOnce)
            {
                hasTriggered = true;
                SaveState();
            }

            // 启动冷却
            if (cooldownTime > 0)
            {
                StartCoroutine(CooldownCoroutine());
            }
        }
        else
        {
            Debug.LogError("[TextPromptTrigger] TextPromptManager.Instance 为空！请确保场景中有 TextPromptManager");
        }
    }

    /// <summary>
    /// 强制显示指定文字（忽略配置的文字）
    /// </summary>
    public void TriggerWithCustomMessage(string message)
    {
        TriggerWithCustomMessages(new string[] { message });
    }

    /// <summary>
    /// 强制显示指定的多条文字（忽略配置的文字）
    /// </summary>
    public void TriggerWithCustomMessages(string[] customMessages)
    {
        if (!CanTrigger()) return;

        if (TextPromptManager.Instance != null)
        {
            TextPromptManager.Instance.ShowPrompt(customMessages);
            OnTriggerPrompt?.Invoke();

            if (cooldownTime > 0)
            {
                StartCoroutine(CooldownCoroutine());
            }
        }
    }

    /// <summary>
    /// 重置触发状态（允许再次触发）
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
        isOnCooldown = false;

        if (!string.IsNullOrEmpty(objectID))
        {
            PlayerPrefs.DeleteKey(GetSaveKey());
            PlayerPrefs.Save();
        }

        Debug.Log($"[TextPromptTrigger] {gameObject.name} 触发状态已重置");
    }

    /// <summary>
    /// 设置新的文字内容
    /// </summary>
    public void SetMessages(string[] newMessages)
    {
        messages = newMessages;
    }

    /// <summary>
    /// 添加一条文字
    /// </summary>
    public void AddMessage(string message)
    {
        var list = new List<string>(messages);
        list.Add(message);
        messages = list.ToArray();
    }

    // ============ 内部方法 ============

    /// <summary>
    /// 检查是否可以触发
    /// </summary>
    private bool CanTrigger()
    {
        // 已触发过且只能触发一次
        if (triggerOnce && hasTriggered)
        {
            Debug.Log($"[TextPromptTrigger] {gameObject.name} 已触发过，跳过");
            return false;
        }

        // 冷却中
        if (isOnCooldown)
        {
            Debug.Log($"[TextPromptTrigger] {gameObject.name} 冷却中，跳过");
            return false;
        }

        // 检查游戏状态
        if (checkGameState && GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentGameState != allowedGameState)
            {
                return false;
            }
        }

        // 检查视图状态
        if (checkViewState && GameManager.Instance != null)
        {
            if (GameManager.Instance.CurrentViewState != allowedViewState)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 检查是否有需要的物品
    /// </summary>
    private bool CheckRequiredItem()
    {
        if (!requireItem || requiredItemData == null) return true;

        // 通过 UIManager 获取选中的物品
        if (UIManager.Instance != null)
        {
            ItemData selectedItem = UIManager.Instance.GetSelectedItem();
            return selectedItem != null && selectedItem == requiredItemData;
        }

        return false;
    }

    /// <summary>
    /// 显示无物品时的提示
    /// </summary>
    private void ShowNoItemMessage()
    {
        if (TextPromptManager.Instance != null && !string.IsNullOrEmpty(noItemMessage))
        {
            TextPromptManager.Instance.ShowPrompt(noItemMessage);
        }
    }

    /// <summary>
    /// 获取要显示的消息
    /// </summary>
    private string[] GetMessagesToShow()
    {
        if (messages == null || messages.Length == 0)
        {
            return new string[0];
        }

        // 过滤空消息
        var filteredMessages = new List<string>();
        foreach (var msg in messages)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                filteredMessages.Add(msg);
            }
        }

        // 随机打乱
        if (shuffleMessages && filteredMessages.Count > 1)
        {
            for (int i = filteredMessages.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                string temp = filteredMessages[i];
                filteredMessages[i] = filteredMessages[randomIndex];
                filteredMessages[randomIndex] = temp;
            }
        }

        return filteredMessages.ToArray();
    }

    /// <summary>
    /// 冷却协程
    /// </summary>
    private System.Collections.IEnumerator CooldownCoroutine()
    {
        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }

    /// <summary>
    /// 消息显示完毕回调
    /// </summary>
    private void OnMessagesComplete()
    {
        // 这里可以添加额外逻辑
        OnPromptComplete?.Invoke();
    }

    // ============ 存档系统 ============

    private string GetSaveKey()
    {
        return $"TextPromptTrigger_{objectID}_Triggered";
    }

    private void SaveState()
    {
        if (string.IsNullOrEmpty(objectID)) return;

        PlayerPrefs.SetInt(GetSaveKey(), hasTriggered ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        if (string.IsNullOrEmpty(objectID)) return;

        hasTriggered = PlayerPrefs.GetInt(GetSaveKey(), 0) == 1;
    }

    // ============ 编辑器辅助 ============

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 绘制触发器范围
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Gizmos.color = hasTriggered ? new Color(0.5f, 0.5f, 0.5f, 0.3f) : new Color(0f, 1f, 0.5f, 0.3f);
            Gizmos.DrawCube(collider.bounds.center, collider.bounds.size);

            Gizmos.color = hasTriggered ? Color.gray : Color.green;
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }
    }

    [ContextMenu("测试触发")]
    private void TestTrigger()
    {
        if (Application.isPlaying)
        {
            TriggerPrompt();
        }
        else
        {
            Debug.Log("[TextPromptTrigger] 请在运行时测试");
        }
    }

    [ContextMenu("重置状态")]
    private void EditorResetTrigger()
    {
        ResetTrigger();
    }
#endif
}