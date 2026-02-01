// Assets/Scripts/GamePlay/Synthesis/MachineInteractionForwarder.cs
// 机器交互转发器 - 将各部件的点击转发给主控制器
using UnityEngine;

/// <summary>
/// 机器交互转发器
/// 附加到机器的各个可点击部件上（盖子、按钮、内部、陶罐展示、碎片展示）
/// 将点击事件转发给 SynthesisMachine 主控制器
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MachineInteractionForwarder : MonoBehaviour
{
    public enum InteractionTarget
    {
        Lid,            // 盖子
        Button,         // 按钮
        Interior,       // 机器内部（放置陶罐的区域）
        DisplayPot,     // 展示的陶罐（用于拾取）
        DisplayShard    // 展示的水晶碎片（用于拾取）
    }

    [Header("配置")]
    [Tooltip("交互目标类型")]
    public InteractionTarget targetType = InteractionTarget.Lid;

    [Tooltip("合成机器主控制器")]
    public SynthesisMachine machine;

    [Header("视觉反馈")]
    [Tooltip("此部件的 SpriteRenderer（可选）")]
    public SpriteRenderer spriteRenderer;

    [Tooltip("鼠标悬停时的高亮颜色")]
    public Color hoverColor = new Color(1f, 1f, 0.8f, 1f);

    [Tooltip("是否启用悬停高亮")]
    public bool enableHoverHighlight = true;

    // 缓存
    private Color originalColor;
    private bool isHovering = false;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 自动查找机器控制器
        if (machine == null)
        {
            machine = GetComponentInParent<SynthesisMachine>();
        }
    }

    private void OnMouseEnter()
    {
        if (!enableHoverHighlight) return;
        if (spriteRenderer == null) return;
        if (!IsInteractable()) return;

        isHovering = true;
        spriteRenderer.color = hoverColor;
    }

    private void OnMouseExit()
    {
        if (spriteRenderer == null) return;

        isHovering = false;
        spriteRenderer.color = originalColor;
    }

    private void OnMouseDown()
    {
        // 检查是否点击在UI上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (machine == null)
        {
            Debug.LogError($"[MachineInteractionForwarder] {gameObject.name}: machine 未设置！");
            return;
        }

        // 转发点击事件
        switch (targetType)
        {
            case InteractionTarget.Lid:
                machine.OnLidClicked();
                break;

            case InteractionTarget.Button:
                machine.OnButtonClicked();
                break;

            case InteractionTarget.Interior:
                machine.OnInteriorClicked();
                break;

            case InteractionTarget.DisplayPot:
                machine.OnDisplayPotClicked();
                break;

            case InteractionTarget.DisplayShard:
                machine.OnDisplayShardClicked();
                break;
        }
    }

    /// <summary>
    /// 检查当前部件是否可交互（用于视觉反馈）
    /// </summary>
    private bool IsInteractable()
    {
        if (machine == null) return false;

        var state = machine.CurrentState;

        switch (targetType)
        {
            case InteractionTarget.Lid:
                // 盖子大部分时候都可以交互
                return state != SynthesisMachine.MachineState.Processing;

            case InteractionTarget.Button:
                // 按钮只在陶罐放入且盖子关闭时可用
                return state == SynthesisMachine.MachineState.PotInserted_LidClosed;

            case InteractionTarget.Interior:
                // 内部只在盖子打开且没有陶罐时可用
                return state == SynthesisMachine.MachineState.Idle_LidOpen;

            case InteractionTarget.DisplayPot:
                // 展示的陶罐只在合成完成后可用
                return (state == SynthesisMachine.MachineState.Complete_LidOpen ||
                        state == SynthesisMachine.MachineState.ResultCollecting);

            case InteractionTarget.DisplayShard:
                // 展示的碎片只在合成完成后可用
                return (state == SynthesisMachine.MachineState.Complete_LidOpen ||
                        state == SynthesisMachine.MachineState.ResultCollecting);

            default:
                return true;
        }
    }

    /// <summary>
    /// 更新原始颜色（当精灵图改变时调用）
    /// </summary>
    public void UpdateOriginalColor(Color newColor)
    {
        originalColor = newColor;
        if (!isHovering && spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    /// <summary>
    /// 重置颜色到原始状态
    /// </summary>
    public void ResetColor()
    {
        isHovering = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
}