// Assets/Scripts/GamePlay/Experimenter/ExperimenterInteractionProxy.cs
// 交互代理 - 将点击事件转发给 ExperimenterPuzzleController
using UnityEngine;

/// <summary>
/// 实验者交互代理
/// 挂载在身体/肋骨等可点击物体上
/// 将 InteractableObject 的触发事件转发给主控制器
/// </summary>
public class ExperimenterInteractionProxy : MonoBehaviour
{
    public enum ProxyType
    {
        Body,   // 身体区域（用于放置放大镜）
        Ribs    // 肋骨区域（用于收集粉末）
    }

    [Header("代理设置")]
    [Tooltip("代理类型")]
    public ProxyType proxyType = ProxyType.Body;

    [Tooltip("主控制器引用")]
    public ExperimenterPuzzleController puzzleController;

    /// <summary>
    /// 由 InteractableObject 的 OnTrigger 事件调用
    /// </summary>
    public void OnInteracted()
    {
        if (puzzleController == null)
        {
            Debug.LogError("[ExperimenterProxy] 未设置 puzzleController！");

            // 尝试在父级查找
            puzzleController = GetComponentInParent<ExperimenterPuzzleController>();

            if (puzzleController == null)
            {
                Debug.LogError("[ExperimenterProxy] 无法找到 ExperimenterPuzzleController！");
                return;
            }
        }

        switch (proxyType)
        {
            case ProxyType.Body:
                puzzleController.OnBodyClicked();
                break;

            case ProxyType.Ribs:
                puzzleController.OnRibsClicked();
                break;
        }
    }
}