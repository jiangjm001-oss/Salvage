// Assets/Scripts/GamePlay/OrganJar.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 器官罐子 - 单个罐子组件
/// 负责处理点击交互和器官显示/隐藏
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class OrganJar : MonoBehaviour
{
    [Header("器官信息")]
    [Tooltip("器官名称（用于显示和调试）")]
    public string organName = "器官";

    [Tooltip("器官图片（子对象的SpriteRenderer）")]
    public SpriteRenderer organSprite;

    [Header("罐子外观（可选）")]
    [Tooltip("罐子精灵")]
    public SpriteRenderer jarSprite;

    [Tooltip("器官被收集后的罐子外观")]
    public Sprite emptyJarSprite;

    [Tooltip("器官在罐子里时的外观")]
    public Sprite filledJarSprite;

    [Header("交互设置")]
    [Tooltip("是否可以被点击")]
    public bool isInteractable = true;

    [Header("音效")]
    public string clickSound = "Audio/SFX/jar_click";

    [Header("事件")]
    public UnityEvent OnOrganCollected;
    public UnityEvent OnOrganRestored;

    // 内部状态
    private OrganCollectionPuzzle puzzleController;
    private bool isCollected = false;
    private Sprite originalJarSprite;

    /// <summary>
    /// 是否已被收集
    /// </summary>
    public bool IsCollected => isCollected;

    private void Awake()
    {
        // 缓存原始罐子精灵
        if (jarSprite != null)
        {
            originalJarSprite = jarSprite.sprite;
        }

        // 如果没有指定器官精灵，尝试自动查找子对象
        if (organSprite == null)
        {
            Transform organChild = transform.Find("Organ");
            if (organChild != null)
            {
                organSprite = organChild.GetComponent<SpriteRenderer>();
            }
        }
    }

    private void OnMouseDown()
    {
        if (!isInteractable) return;
        if (isCollected) return;

        Debug.Log($"[OrganJar] 点击罐子: {organName}");

        // 播放点击音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(clickSound))
        {
            AudioManager.Instance.PlaySFX(clickSound);
        }

        // 通知谜题控制器
        if (puzzleController != null)
        {
            puzzleController.OnJarClicked(this);
        }
        else
        {
            Debug.LogWarning($"[OrganJar] {organName} 没有关联的谜题控制器！");
        }
    }

    /// <summary>
    /// 设置谜题控制器引用
    /// </summary>
    public void SetPuzzleController(OrganCollectionPuzzle controller)
    {
        puzzleController = controller;
    }

    /// <summary>
    /// 收集器官（从罐子中消失）
    /// </summary>
    public void CollectOrgan()
    {
        if (isCollected) return;

        Debug.Log($"[OrganJar] 收集器官: {organName}");

        isCollected = true;

        // 隐藏器官图片
        if (organSprite != null)
        {
            organSprite.enabled = false;
        }

        // 切换罐子外观为空罐
        if (jarSprite != null && emptyJarSprite != null)
        {
            jarSprite.sprite = emptyJarSprite;
        }

        OnOrganCollected?.Invoke();
    }

    /// <summary>
    /// 恢复器官（重新出现在罐子中）
    /// </summary>
    public void RestoreOrgan()
    {
        if (!isCollected) return;

        Debug.Log($"[OrganJar] 恢复器官: {organName}");

        isCollected = false;

        // 显示器官图片
        if (organSprite != null)
        {
            organSprite.enabled = true;
        }

        // 恢复罐子外观
        if (jarSprite != null)
        {
            if (filledJarSprite != null)
            {
                jarSprite.sprite = filledJarSprite;
            }
            else if (originalJarSprite != null)
            {
                jarSprite.sprite = originalJarSprite;
            }
        }

        OnOrganRestored?.Invoke();
    }

    /// <summary>
    /// 设置交互状态
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        isInteractable = interactable;
    }

    /// <summary>
    /// 高亮显示（可选，用于提示玩家）
    /// </summary>
    public void SetHighlight(bool highlight)
    {
        if (jarSprite != null)
        {
            jarSprite.color = highlight ? Color.yellow : Color.white;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug - 收集器官")]
    private void DebugCollect()
    {
        CollectOrgan();
    }


    [ContextMenu("Debug - 恢复器官")]
    private void DebugRestore()
    {
        RestoreOrgan();
    }
#endif
}