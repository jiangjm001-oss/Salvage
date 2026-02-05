// Assets/Scripts/GamePlay/Synthesis/MachineInteractionForwarder.cs
// 机器交互转发器 - 将各部件的点击和鼠标事件转发给主控制器
// 优化版：支持平滑颜色过渡
using UnityEngine;

/// <summary>
/// 机器交互转发器
/// 附加到机器的各个可点击部件上（机器主体、盖子、按钮、陶罐展示、碎片展示）
/// 将鼠标事件转发给 SynthesisMachine 主控制器
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MachineInteractionForwarder : MonoBehaviour
{
    public enum InteractionTarget
    {
        MachineBody,    // 机器主体（打开盖子、放入陶罐）
        Lid,            // 盖子（开/关）
        Button,         // 按钮（启动合成）
        DisplayPot,     // 展示的陶罐（用于拾取）
        DisplayShard    // 展示的水晶碎片（用于拾取）
    }

    [Header("配置")]
    [Tooltip("交互目标类型")]
    public InteractionTarget targetType = InteractionTarget.MachineBody;

    [Tooltip("合成机器主控制器")]
    public SynthesisMachine machine;

    private void Awake()
    {
        // 自动查找机器控制器
        if (machine == null)
        {
            machine = GetComponentInParent<SynthesisMachine>();
        }
    }

    private void OnMouseEnter()
    {
        if (machine == null) return;

        // 检查是否在UI上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 转发鼠标进入事件
        switch (targetType)
        {
            case InteractionTarget.MachineBody:
                machine.OnMachineMouseEnter();
                break;
            case InteractionTarget.Lid:
                machine.OnLidMouseEnter();
                break;
            case InteractionTarget.Button:
                machine.OnButtonMouseEnter();
                break;
            case InteractionTarget.DisplayPot:
                machine.OnDisplayPotMouseEnter();
                break;
            case InteractionTarget.DisplayShard:
                machine.OnDisplayShardMouseEnter();
                break;
        }
    }

    private void OnMouseExit()
    {
        if (machine == null) return;

        // 转发鼠标离开事件
        switch (targetType)
        {
            case InteractionTarget.MachineBody:
                machine.OnMachineMouseExit();
                break;
            case InteractionTarget.Lid:
                machine.OnLidMouseExit();
                break;
            case InteractionTarget.Button:
                machine.OnButtonMouseExit();
                break;
            case InteractionTarget.DisplayPot:
                machine.OnDisplayPotMouseExit();
                break;
            case InteractionTarget.DisplayShard:
                machine.OnDisplayShardMouseExit();
                break;
        }
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
            case InteractionTarget.MachineBody:
                machine.OnMachineClicked();
                break;
            case InteractionTarget.Lid:
                machine.OnLidClicked();
                break;
            case InteractionTarget.Button:
                machine.OnButtonClicked();
                break;
            case InteractionTarget.DisplayPot:
                machine.OnDisplayPotClicked();
                break;
            case InteractionTarget.DisplayShard:
                machine.OnDisplayShardClicked();
                break;
        }
    }
}