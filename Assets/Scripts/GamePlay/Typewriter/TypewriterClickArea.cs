// Assets/Scripts/GamePlay/Typewriter/TypewriterClickArea.cs
// 打字机点击区域 - 触发放置信纸或拾取信纸
using UnityEngine;

/// <summary>
/// 打字机点击区域
/// 放在打字机的可点击区域上，用于触发放置信纸或拾取完成的信纸
/// 
/// 使用方式：
/// 1. 在打字机放大视图中创建一个带有 Collider2D 的物体
/// 2. 添加此组件
/// 3. 拖入 TypewriterController 引用
/// 4. 拖入放大视图根物体到 zoomViewRoot（用于检测是否在正确视图）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TypewriterClickArea : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("打字机控制器【必须配置】")]
    public TypewriterController controller;

    [Header("视图检查")]
    [Tooltip("放大视图根物体（从 Hierarchy 拖入）\n当此物体激活时才响应点击\n留空则不检查视图状态")]
    public GameObject zoomViewRoot;

    [Tooltip("是否检查视图状态（关闭则任何时候都响应）")]
    public bool checkViewState = true;

    [Header("点击行为")]
    [Tooltip("点击时是否尝试放置信纸")]
    public bool tryPlacePaper = true;

    [Tooltip("点击时是否尝试拾取信纸（谜题完成后）")]
    public bool tryPickupPaper = true;

    private void Start()
    {
        // 配置验证
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        if (controller == null)
        {
            Debug.LogError($"[TypewriterClickArea] ⚠️ {gameObject.name}: controller 未配置！");

            // 尝试在父物体中查找
            controller = GetComponentInParent<TypewriterController>();
            if (controller != null)
            {
                Debug.Log($"[TypewriterClickArea] ✓ 自动找到 TypewriterController");
            }
        }

        // 确保有 Collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"[TypewriterClickArea] ⚠️ {gameObject.name}: 缺少 Collider2D！");
        }

        // 视图检查提示
        if (checkViewState && zoomViewRoot == null)
        {
            Debug.LogWarning($"[TypewriterClickArea] ⚠️ {gameObject.name}: zoomViewRoot 未配置，将尝试自动查找父级放大视图");

            // 尝试自动查找（假设命名包含 "zoom"）
            Transform parent = transform.parent;
            while (parent != null)
            {
                if (parent.name.ToLower().Contains("zoom"))
                {
                    zoomViewRoot = parent.gameObject;
                    Debug.Log($"[TypewriterClickArea] ✓ 自动找到放大视图: {zoomViewRoot.name}");
                    break;
                }
                parent = parent.parent;
            }
        }
    }

    private void OnMouseDown()
    {
        // 检查视图状态
        if (!IsInCorrectView())
        {
            return;
        }

        HandleClick();
    }

    /// <summary>
    /// 检查是否在正确的视图中
    /// </summary>
    private bool IsInCorrectView()
    {
        // 如果不检查视图状态，直接返回 true
        if (!checkViewState)
        {
            return true;
        }

        // 如果配置了放大视图根物体，检查其是否激活
        if (zoomViewRoot != null)
        {
            return zoomViewRoot.activeInHierarchy;
        }

        // 如果没有配置 zoomViewRoot，检查自身是否在激活的层级中
        // 这是一个备用方案
        return gameObject.activeInHierarchy;
    }

    /// <summary>
    /// 处理点击
    /// </summary>
    private void HandleClick()
    {
        if (controller == null)
        {
            Debug.LogError("[TypewriterClickArea] controller 为空！");
            return;
        }

        // 优先尝试拾取（如果谜题已完成）
        if (tryPickupPaper && controller.CanPickupResult)
        {
            controller.TryPickupResultPaper();
            Debug.Log("[TypewriterClickArea] 触发拾取信纸");
            return;
        }

        // 尝试放置信纸
        if (tryPlacePaper && !controller.IsPaperPlaced)
        {
            controller.TryPlacePaper();
            Debug.Log("[TypewriterClickArea] 触发放置信纸");
            return;
        }

        Debug.Log("[TypewriterClickArea] 点击但无操作（信纸已放置且未完成）");
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 设置放大视图根物体（可通过代码设置）
    /// </summary>
    public void SetZoomViewRoot(GameObject root)
    {
        zoomViewRoot = root;
    }

    /// <summary>
    /// 手动触发点击（用于其他脚本调用）
    /// </summary>
    public void TriggerClick()
    {
        if (IsInCorrectView())
        {
            HandleClick();
        }
    }

    // ============ 调试方法 ============

    [ContextMenu("Debug: 模拟点击")]
    private void DebugClick()
    {
        HandleClick();
    }

    [ContextMenu("Debug: 检查视图状态")]
    private void DebugCheckView()
    {
        bool inView = IsInCorrectView();
        Debug.Log($"[TypewriterClickArea] 当前是否在正确视图: {inView}");

        if (zoomViewRoot != null)
        {
            Debug.Log($"[TypewriterClickArea] zoomViewRoot: {zoomViewRoot.name}, active: {zoomViewRoot.activeInHierarchy}");
        }
        else
        {
            Debug.Log("[TypewriterClickArea] zoomViewRoot 未配置");
        }
    }

    [ContextMenu("Debug: 验证配置")]
    private void DebugValidate()
    {
        ValidateConfiguration();
    }
}