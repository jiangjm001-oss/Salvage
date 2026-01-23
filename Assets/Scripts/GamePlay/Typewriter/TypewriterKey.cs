using TMPro;
using UnityEngine;

//Assets / Scripts / Puzzles / Typewriter / TypewriterKey.cs
using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 打字机单个按键
/// 按键背景用Sprite，字母用TextMeshPro显示
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class TypewriterKey : MonoBehaviour
{
    [Header("按键配置")]
    [Tooltip("这个按键代表的字符")]
    public char keyCharacter = 'A';

    [Tooltip("是否是回车键")]
    public bool isEnterKey = false;

    [Tooltip("是否是退格键")]
    public bool isBackspaceKey = false;

    [Header("文字显示")]
    [Tooltip("按键上的文字组件（子物体）")]
    public TextMeshPro keyLabel;

    [Tooltip("是否自动设置文字为 keyCharacter")]
    public bool autoSetLabel = true;

    [Header("按键背景")]
    [Tooltip("普通状态的背景Sprite")]
    public Sprite normalSprite;

    [Tooltip("按下状态的背景Sprite（可选，不设置则用颜色变化）")]
    public Sprite pressedSprite;

    [Header("颜色配置")]
    [Tooltip("普通状态背景颜色")]
    public Color normalBgColor = Color.white;

    [Tooltip("按下状态背景颜色")]
    public Color pressedBgColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Tooltip("普通状态文字颜色")]
    public Color normalTextColor = Color.black;

    [Tooltip("按下状态文字颜色")]
    public Color pressedTextColor = Color.black;

    [Header("动画")]
    [Tooltip("按下动画持续时间")]
    public float pressDuration = 0.1f;

    [Header("引用")]
    [Tooltip("打字机控制器")]
    public TypewriterController controller;

    private SpriteRenderer spriteRenderer;
    private bool isAnimating = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 保存普通状态Sprite
        if (normalSprite == null)
        {
            normalSprite = spriteRenderer.sprite;
        }

        // 自动查找子物体的TextMeshPro
        if (keyLabel == null)
        {
            keyLabel = GetComponentInChildren<TextMeshPro>();
        }

        // 自动查找控制器
        if (controller == null)
        {
            controller = GetComponentInParent<TypewriterController>();
        }
    }

    private void Start()
    {
        // 自动设置按键文字
        if (autoSetLabel && keyLabel != null)
        {
            if (isEnterKey)
            {
                keyLabel.text = "↵";  // 或 "Enter"
            }
            else if (isBackspaceKey)
            {
                keyLabel.text = "←";  // 或 "Del"
            }
            else
            {
                keyLabel.text = keyCharacter.ToString().ToUpper();
            }
        }

        // 设置初始颜色
        SetVisualState(false);
    }

    /// <summary>
    /// 按键被点击
    /// </summary>
    public void OnKeyPressed()
    {
        if (isAnimating) return;
        if (controller == null) return;

        StartCoroutine(KeyPressAnimation());

        // 执行对应操作
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

    /// <summary>
    /// 按键动画：亮 → 暗 → 亮
    /// </summary>
    private IEnumerator KeyPressAnimation()
    {
        isAnimating = true;

        // 按下状态（变暗）
        SetVisualState(true);

        yield return new WaitForSeconds(pressDuration);

        // 恢复状态（变亮）
        SetVisualState(false);

        isAnimating = false;
    }

    /// <summary>
    /// 设置视觉状态
    /// </summary>
    private void SetVisualState(bool isPressed)
    {
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
            spriteRenderer.sprite = normalSprite;
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
        // 确保在打字机放大视图中
        if (GameManager.Instance?.CurrentViewState != GameManager.ViewState.lv1_B_zoom_Typewriter)
        {
            return;
        }

        OnKeyPressed();
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        // 在编辑器中实时预览文字
        if (keyLabel != null && autoSetLabel)
        {
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
    }
}