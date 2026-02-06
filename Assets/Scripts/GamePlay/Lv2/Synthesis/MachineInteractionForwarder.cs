// Assets/Scripts/GamePlay/Lv2/Synthesis/MachineInteractionForwarder.cs
// 机器交互转发器 - 简化版
// 只转发点击事件到SynthesisMachine

using UnityEngine;

/// <summary>
/// 机器交互转发器
/// 将GameObject上的鼠标点击事件转发给SynthesisMachine
/// 
/// 使用方法：
/// 1. 挂载到需要接收点击的GameObject上（机器、按钮、陶罐、碎片等）
/// 2. 设置InteractionType指定这是什么物体
/// 3. 拖入SynthesisMachine的引用
/// 4. GameObject必须有Collider2D组件才能接收点击
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MachineInteractionForwarder : MonoBehaviour
{
    // 交互类型枚举
    public enum InteractionType
    {
        Machine,        // 机器主体
        Button,         // 按钮
        DisplayPot,     // 展示的陶罐
        DisplayShard    // 展示的水晶碎片
    }

    [Header("配置")]
    [Tooltip("交互类型")]
    public InteractionType interactionType = InteractionType.Machine;

    [Tooltip("合成机器引用")]
    public SynthesisMachine synthesisMachine;

    // 缓存
    private Collider2D col;

    void Start()
    {
        // 获取Collider组件
        col = GetComponent<Collider2D>();
        if (col == null)
        {
            Debug.LogError($"[MachineInteractionForwarder] {gameObject.name} 缺少Collider2D组件！");
        }

        // 检查SynthesisMachine引用
        if (synthesisMachine == null)
        {
            Debug.LogError($"[MachineInteractionForwarder] {gameObject.name} 未设置SynthesisMachine引用！");
        }
    }

    // 鼠标点击事件
    void OnMouseDown()
    {
        if (synthesisMachine == null) return;

        // 根据类型转发到对应的方法
        switch (interactionType)
        {
            case InteractionType.Machine:
                synthesisMachine.OnMachineClicked();
                break;

            case InteractionType.Button:
                synthesisMachine.OnButtonClicked();
                break;

            case InteractionType.DisplayPot:
                synthesisMachine.OnPotClicked();
                break;

            case InteractionType.DisplayShard:
                synthesisMachine.OnShardClicked();
                break;
        }
    }

    // 可视化调试
    void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示交互区域
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Gizmos.color = GetDebugColor();
            Gizmos.DrawWireCube(collider.bounds.center, collider.bounds.size);
        }
    }

    // 根据交互类型返回不同的调试颜色
    private Color GetDebugColor()
    {
        switch (interactionType)
        {
            case InteractionType.Machine:
                return Color.cyan;
            case InteractionType.Button:
                return Color.yellow;
            case InteractionType.DisplayPot:
                return Color.green;
            case InteractionType.DisplayShard:
                return Color.magenta;
            default:
                return Color.white;
        }
    }
}