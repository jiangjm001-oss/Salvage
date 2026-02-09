// Assets/Scripts/GamePlay/Lv2/Crystal/FifthCrystalEndingTrigger.cs
// 第五水晶结局触发器
// 替代原有的拾取逻辑，点击后触发结局演出

using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 第五水晶结局触发器
/// 挂载到第五水晶物体上，点击时触发结局演出而非拾取
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FifthCrystalEndingTrigger : MonoBehaviour
{
    [Header("触发设置")]
    [Tooltip("是否已激活（由CrystalPlacementPuzzle控制）")]
    public bool isActivated = false;

    [Tooltip("是否已触发过")]
    [SerializeField] private bool hasTriggered = false;

    [Header("视觉效果")]
    [Tooltip("Sprite渲染器（用于发光效果）")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("悬停发光颜色")]
    public Color hoverGlowColor = new Color(0.8f, 0.9f, 1f, 1f);

    [Tooltip("悬停发光强度")]
    [Range(1f, 2f)]
    public float hoverGlowIntensity = 1.3f;

    [Tooltip("发光过渡时间")]
    public float glowTransitionDuration = 0.2f;

    [Header("脉冲发光")]
    [Tooltip("是否启用脉冲发光")]
    public bool enablePulse = true;

    [Tooltip("脉冲速度")]
    public float pulseSpeed = 2f;

    [Tooltip("脉冲最小亮度")]
    [Range(0.5f, 1f)]
    public float pulseMinBrightness = 0.7f;

    [Header("音效")]
    [Tooltip("点击音效")]
    public string clickSoundPath = "Audio/SFX/crystal_click";

    [Header("事件")]
    [Tooltip("触发结局时调用")]
    public UnityEvent OnEndingTriggered;

    // ============ 私有变量 ============
    private Color originalColor;
    private Color targetColor;
    private bool isHovering = false;
    private Collider2D col;
    private Coroutine colorCoroutine;
    private Coroutine pulseCoroutine;

    // ============ 生命周期 ============

    private void Awake()
    {
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            targetColor = originalColor;
        }

        // 初始状态禁用交互
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void Start()
    {
        // 如果已激活，启用交互和脉冲
        if (isActivated && !hasTriggered)
        {
            EnableInteraction();
        }
    }

    private void OnMouseEnter()
    {
        if (!isActivated || hasTriggered) return;

        isHovering = true;
        StopPulse();
        TransitionToColor(hoverGlowColor * hoverGlowIntensity);
    }

    private void OnMouseExit()
    {
        if (!isActivated || hasTriggered) return;

        isHovering = false;
        TransitionToColor(originalColor);

        if (enablePulse)
        {
            StartPulse();
        }
    }

    private void OnMouseDown()
    {
        if (!isActivated || hasTriggered) return;

        TriggerEnding();
    }

    // ============ 公共API ============

    /// <summary>
    /// 激活水晶（由CrystalPlacementPuzzle在第五水晶浮现后调用）
    /// </summary>
    public void Activate()
    {
        if (isActivated) return;

        Debug.Log("[FifthCrystalEndingTrigger] 第五水晶已激活，等待点击触发结局");
        isActivated = true;
        EnableInteraction();
    }

    /// <summary>
    /// 触发结局演出
    /// </summary>
    public void TriggerEnding()
    {
        if (hasTriggered)
        {
            Debug.LogWarning("[FifthCrystalEndingTrigger] 结局已触发过");
            return;
        }

        Debug.Log("[FifthCrystalEndingTrigger] ========== 触发结局演出 ==========");
        hasTriggered = true;

        // 停止所有效果
        StopPulse();
        StopColorTransition();

        // 播放点击音效
        PlaySound(clickSoundPath);

        // 禁用交互
        if (col != null)
        {
            col.enabled = false;
        }

        // 触发事件
        OnEndingTriggered?.Invoke();

        // 调用结局演出控制器
        if (EndingCutsceneController.Instance != null)
        {
            EndingCutsceneController.Instance.StartCutscene();
        }
        else
        {
            Debug.LogError("[FifthCrystalEndingTrigger] EndingCutsceneController.Instance 不存在！");
        }
    }

    // ============ 内部方法 ============

    private void EnableInteraction()
    {
        if (col != null)
        {
            col.enabled = true;
        }

        if (enablePulse)
        {
            StartPulse();
        }
    }

    // ============ 发光效果 ============

    private void StartPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        pulseCoroutine = StartCoroutine(PulseCoroutine());
    }

    private void StopPulse()
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    private System.Collections.IEnumerator PulseCoroutine()
    {
        while (isActivated && !hasTriggered && !isHovering)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
            float brightness = Mathf.Lerp(pulseMinBrightness, 1f, t);

            if (spriteRenderer != null)
            {
                Color pulseColor = new Color(
                    originalColor.r * brightness + hoverGlowColor.r * (1f - brightness) * 0.2f,
                    originalColor.g * brightness + hoverGlowColor.g * (1f - brightness) * 0.2f,
                    originalColor.b * brightness + hoverGlowColor.b * (1f - brightness) * 0.2f,
                    originalColor.a
                );
                spriteRenderer.color = pulseColor;
            }

            yield return null;
        }

        // 恢复原始颜色
        if (spriteRenderer != null && !isHovering)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void TransitionToColor(Color target)
    {
        targetColor = target;

        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
        }
        colorCoroutine = StartCoroutine(ColorTransitionCoroutine(target));
    }

    private void StopColorTransition()
    {
        if (colorCoroutine != null)
        {
            StopCoroutine(colorCoroutine);
            colorCoroutine = null;
        }
    }

    private System.Collections.IEnumerator ColorTransitionCoroutine(Color target)
    {
        if (spriteRenderer == null) yield break;

        Color startColor = spriteRenderer.color;
        float elapsed = 0f;

        while (elapsed < glowTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / glowTransitionDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            spriteRenderer.color = Color.Lerp(startColor, target, t);
            yield return null;
        }

        spriteRenderer.color = target;
    }

    // ============ 辅助方法 ============

    private void PlaySound(string soundPath)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundPath))
        {
            AudioManager.Instance.PlaySFX(soundPath);
        }
    }

    // ============ 存档相关 ============

    /// <summary>
    /// 重置状态（用于新游戏）
    /// </summary>
    public void ResetState()
    {
        hasTriggered = false;
        isActivated = false;
        isHovering = false;

        StopPulse();
        StopColorTransition();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (col != null)
        {
            col.enabled = false;
        }
    }

    // ============ 编辑器功能 ============

    [ContextMenu("测试触发结局")]
    private void TestTrigger()
    {
        if (Application.isPlaying)
        {
            isActivated = true;
            TriggerEnding();
        }
    }
}