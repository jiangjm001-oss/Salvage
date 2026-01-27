// Assets/Scripts/GamePlay/Typewriter/TypewriterKey.cs
// 打字机单个按键组件 - 修复版
using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 打字机单个按键
/// 按键背景用 Sprite，字母用 TextMeshPro 显示
/// 
/// 使用方式：
/// 1. 创建按键物体，添加 SpriteRenderer 和 Collider2D
/// 2. 添加此组件
/// 3. 配置 keyCharacter 或勾选 isEnterKey/isBackspaceKey
/// 4. 拖入 TypewriterController 引用
/// 5. 拖入放大视图根物体到 zoomViewRoot
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class TypewriterKey : MonoBehaviour
{
    // ============ 按键配置 ============
    [Header("按键配置")]
    [Tooltip("这个按键代表的字符")]
    public char keyCharacter = 'A';

    [Tooltip("是否是回车键")]
    public bool isEnterKey = false;

    [Tooltip("是否是退格键")]
    public bool isBackspaceKey = false;

    // ============ 文字显示 ============
    [Header("文字显示")]
    [Tooltip("按键上的文字组件（子物体）")]
    public TextMeshPro keyLabel;

    [Tooltip("是否自动设置文字为 keyCharacter")]
    public bool autoSetLabel = true;

    // ============ 按键背景 ============
    [Header("按键背景")]
    [Tooltip("普通状态的背景 Sprite")]
    public Sprite normalSprite;

    [Tooltip("按下状态的背景 Sprite（可选，不设置则用颜色变化）")]
    public Sprite pressedSprite;

    // ============ 颜色配置 ============
    [Header("颜色配置")]
    [Tooltip("普通状态背景颜色")]
    public Color normalBgColor = Color.white;

    [Tooltip("按下状态背景颜色")]
    public Color pressedBgColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Tooltip("普通状态文字颜色")]
    public Color normalTextColor = Color.black;

    [Tooltip("按下状态文字颜色")]
    public Color pressedTextColor = Color.black;

    // ============ 动画 ============
    [Header("动画")]
    [Tooltip("按下动画持续时间")]
    public float pressDuration = 0.1f;

    [Tooltip("是否启用按下位移效果")]
    public bool enablePressOffset = true;

    [Tooltip("按下时的位移距离")]
    public Vector3 pressOffset = new Vector3(0, -0.02f, 0);

    // ============ 引用 ============
    [Header("引用")]
    [Tooltip("打字机控制器【必须配置】")]
    public TypewriterController controller;

    [Header("视图检查")]
    [Tooltip("放大视图根物体（从 Hierarchy 拖入）\n当此物体激活时才响应点击\n留空则不检查视图状态")]
    public GameObject zoomViewRoot;

    [Tooltip("是否检查视图状态")]
    public bool checkViewState = true;

    // ============ 内部变量 ============
    private SpriteRenderer spriteRenderer;
    private bool isAnimating = false;
    private Vector3 originalPosition;

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalPosition = transform.localPosition;

        // 保存普通状态 Sprite
        if (normalSprite == null && spriteRenderer != null)
        {
            normalSprite = spriteRenderer.sprite;
        }
    }

    private void Start()
    {
        // 自动设置文字
        if (autoSetLabel && keyLabel != null)
        {
            UpdateKeyLabel();
        }

        // 初始化视觉状态
        SetVisualState(false);

        // 配置验证
        ValidateConfiguration();
    }

    // ============ 配置验证 ============

    private void ValidateConfiguration()
    {
        if (controller == null)
        {
            Debug.LogError($"[TypewriterKey] ⚠️ {gameObject.name}: controller 未配置！");
        }

        if (spriteRenderer == null)
        {
            Debug.LogError($"[TypewriterKey] ⚠️ {gameObject.name}: 缺少 SpriteRenderer 组件！");
        }

        // 检查 Collider
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"[TypewriterKey] ⚠️ {gameObject.name}: 缺少 Collider2D 组件！");
        }
        else if (!col.enabled)
        {
            Debug.LogWarning($"[TypewriterKey] ⚠️ {gameObject.name}: Collider2D 被禁用！");
        }

        // 视图检查提示
        if (checkViewState && zoomViewRoot == null)
        {
            // 尝试自动查找（向上查找名称包含 "zoom" 的父物体）
            Transform parent = transform.parent;
            while (parent != null)
            {
                if (parent.name.ToLower().Contains("zoom"))
                {
                    zoomViewRoot = parent.gameObject;
                    Debug.Log($"[TypewriterKey] ✓ {gameObject.name}: 自动找到放大视图: {zoomViewRoot.name}");
                    break;
                }
                parent = parent.parent;
            }

            if (zoomViewRoot == null)
            {
                Debug.LogWarning($"[TypewriterKey] ⚠️ {gameObject.name}: zoomViewRoot 未配置，点击检测可能有问题");
            }
        }
    }

    // ============ 视图检查 ============

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
        return gameObject.activeInHierarchy;
    }

    // ============ 按键响应 ============

    /// <summary>
    /// 按键被按下时调用
    /// </summary>
    public void OnKeyPressed()
    {
        if (controller == null)
        {
            Debug.LogError($"[TypewriterKey] {gameObject.name}: controller 为空！");
            return;
        }

        if (isAnimating) return;

        // 检查信纸是否已放置
        if (!controller.IsPaperPlaced)
        {
            Debug.Log($"[TypewriterKey] {gameObject.name}: 信纸未放置，忽略按键");
            return;
        }

        // 检查谜题是否已解决
        if (controller.IsPuzzleSolved)
        {
            Debug.Log($"[TypewriterKey] {gameObject.name}: 谜题已解决，忽略按键");
            return;
        }

        // 播放按下动画
        StartCoroutine(PressAnimation());

        // 发送按键到控制器
        if (isEnterKey)
        {
            controller.PressEnter();
        }
        else if (isBackspaceKey)
        {
            controller.Backspace();
        }
        else
        {
            controller.TypeCharacter(keyCharacter);
        }
    }

    // ============ 动画 ============

    private IEnumerator PressAnimation()
    {
        isAnimating = true;

        // 按下状态
        SetVisualState(true);

        // 位移效果
        if (enablePressOffset)
        {
            transform.localPosition = originalPosition + pressOffset;
        }

        yield return new WaitForSeconds(pressDuration);

        // 恢复状态
        SetVisualState(false);

        // 恢复位置
        if (enablePressOffset)
        {
            transform.localPosition = originalPosition;
        }

        isAnimating = false;
    }

    /// <summary>
    /// 设置视觉状态
    /// </summary>
    private void SetVisualState(bool isPressed)
    {
        if (spriteRenderer == null) return;

        if (isPressed)
        {
            // 按下状态
            if (pressedSprite != null)
            {
                spriteRenderer.sprite = pressedSprite;
            }
            spriteRenderer.color = pressedBgColor;

            if (keyLabel != null)
            {
                keyLabel.color = pressedTextColor;
            }
        }
        else
        {
            // 普通状态
            if (normalSprite != null)
            {
                spriteRenderer.sprite = normalSprite;
            }
            spriteRenderer.color = normalBgColor;

            if (keyLabel != null)
            {
                keyLabel.color = normalTextColor;
            }
        }
    }

    // ============ 点击检测 ============

    private void OnMouseDown()
    {
        // 检查是否在正确的视图中
        if (!IsInCorrectView())
        {
            Debug.Log($"[TypewriterKey] {gameObject.name}: 不在正确视图中，忽略点击");
            return;
        }

        OnKeyPressed();
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 更新按键标签文字
    /// </summary>
    private void UpdateKeyLabel()
    {
        if (keyLabel == null) return;

        if (isEnterKey)
        {
            keyLabel.text = "↵";
        }
        else if (isBackspaceKey)
        {
            keyLabel.text = "←";
        }
        else
        {
            keyLabel.text = keyCharacter.ToString().ToUpper();
        }
    }

    /// <summary>
    /// 设置放大视图根物体（可通过代码设置）
    /// </summary>
    public void SetZoomViewRoot(GameObject root)
    {
        zoomViewRoot = root;
    }

    /// <summary>
    /// 设置按键字符并更新显示
    /// </summary>
    public void SetKeyCharacter(char c)
    {
        keyCharacter = c;
        isEnterKey = false;
        isBackspaceKey = false;
        UpdateKeyLabel();
    }

    /// <summary>
    /// 设置为回车键
    /// </summary>
    public void SetAsEnterKey()
    {
        isEnterKey = true;
        isBackspaceKey = false;
        UpdateKeyLabel();
    }

    /// <summary>
    /// 设置为退格键
    /// </summary>
    public void SetAsBackspaceKey()
    {
        isBackspaceKey = true;
        isEnterKey = false;
        UpdateKeyLabel();
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        // 在编辑器中实时预览文字
        if (keyLabel != null && autoSetLabel)
        {
            UpdateKeyLabel();
        }
    }

    // ============ 调试方法 ============

    [ContextMenu("Debug: 模拟按下")]
    private void DebugPress()
    {
        OnKeyPressed();
    }

    [ContextMenu("Debug: 检查视图状态")]
    private void DebugCheckView()
    {
        bool inView = IsInCorrectView();
        Debug.Log($"[TypewriterKey] {gameObject.name}: 当前是否在正确视图: {inView}");

        if (zoomViewRoot != null)
        {
            Debug.Log($"[TypewriterKey] zoomViewRoot: {zoomViewRoot.name}, active: {zoomViewRoot.activeInHierarchy}");
        }
        else
        {
            Debug.Log("[TypewriterKey] zoomViewRoot 未配置");
        }
    }

    [ContextMenu("Debug: 验证配置")]
    private void DebugValidate()
    {
        ValidateConfiguration();
    }
}