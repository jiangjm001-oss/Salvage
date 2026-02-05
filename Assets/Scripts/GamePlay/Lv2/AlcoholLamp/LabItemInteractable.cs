// Assets/Scripts/GamePlay/LabItemInteractable.cs
// 实验物品交互组件 - 用于酒精灯、火焰、试管等物体的点击响应
// 挂载到每个实验物体上，点击后调用 AlcoholLampExperiment 的对应方法
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 实验物品类型枚举
/// </summary>
public enum LabItemType
{
    AlcoholLamp,        // 酒精灯
    Flame,              // 火焰
    TestTube,           // 试管（放在火焰上的）
    CookedPowder        // 烧熟的粉末（可拾取）
}

/// <summary>
/// 实验物品交互组件
/// 挂载到酒精灯、火焰、试管、熟粉末等物体上
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LabItemInteractable : MonoBehaviour
{
    [Header("交互类型")]
    [Tooltip("选择此物体在实验中的角色")]
    public LabItemType itemType;

    [Header("显示名称")]
    [Tooltip("物体的显示名称（用于调试）")]
    public string displayName;

    [Header("控制器引用（可选）")]
    [Tooltip("留空则自动查找 AlcoholLampExperiment.Instance")]
    public AlcoholLampExperiment controller;

    private void Start()
    {
        // 如果没有手动指定，尝试获取单例
        if (controller == null)
        {
            controller = AlcoholLampExperiment.Instance;
        }

        // 确保有 Collider2D
        if (GetComponent<Collider2D>() == null)
        {
            Debug.LogError($"[LabItemInteractable] '{displayName}' 缺少 Collider2D！");
        }
    }

    /// <summary>
    /// 鼠标点击检测
    /// </summary>
    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 执行交互
        Interact();
    }

    /// <summary>
    /// 执行交互（也可由其他系统调用）
    /// </summary>
    public void Interact()
    {
        Debug.Log($"[LabItemInteractable] 点击: {displayName} ({itemType})");

        // 确保控制器存在
        if (controller == null)
        {
            controller = AlcoholLampExperiment.Instance;
        }

        if (controller == null)
        {
            Debug.LogError("[LabItemInteractable] AlcoholLampExperiment 控制器不存在！");
            return;
        }

        // 根据类型调用对应的控制器方法
        switch (itemType)
        {
            case LabItemType.AlcoholLamp:
                controller.ClickAlcoholLamp();
                break;

            case LabItemType.Flame:
                controller.ClickFlame();
                break;

            case LabItemType.TestTube:
                controller.ClickTestTube();
                break;

            case LabItemType.CookedPowder:
                controller.ClickCookedPowder();
                break;

            default:
                Debug.LogWarning($"[LabItemInteractable] 未知的交互类型: {itemType}");
                break;
        }
    }
}