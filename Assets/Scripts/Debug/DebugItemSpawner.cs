// Assets/Scripts/Debug/DebugItemSpawner.cs
// ⚠️ 仅用于测试，发布前记得删除或禁用此脚本
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 调试工具 - 快速添加物品到背包、切换镜子状态等
/// 在游戏运行时按键盘快捷键进行调试操作
/// </summary>
public class DebugItemSpawner : MonoBehaviour
{
    [Header("调试开关")]
    [Tooltip("是否启用调试功能")]
    public bool enableDebug = true;

    [Header("测试物品配置")]
    [Tooltip("按数字键 1-9 添加对应物品（直接拖入 ItemData）")]
    public List<ItemData> testItems = new List<ItemData>();

    [Header("快捷键说明（只读）")]
    [TextArea(10, 14)]
    public string instructions =
        "【物品快捷键】\n" +
        "数字键 1-9：添加对应槽位的物品到背包\n" +
        "\n" +
        "【调试快捷键】\n" +
        "C 键：清空背包\n" +
        "M 键：切换镜子状态（Dirty → Clean → Special → Dirty）\n" +
        "P 键：打印当前背包内容\n" +
        "S 键：手动保存游戏\n" +
        "L 键：打印当前游戏状态\n" +
        "\n" +
        "【跳关快捷键】\n" +
        "F2 键：直接跳转到 Level2";

    private void Update()
    {
        if (!enableDebug) return;

        // 数字键 1-9 添加物品
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                AddTestItem(i);
            }
        }

        // C 键清空背包
        if (Input.GetKeyDown(KeyCode.C))
        {
            ClearInventory();
        }

        // M 键切换镜子状态
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMirrorState();
        }

        // P 键打印背包内容
        if (Input.GetKeyDown(KeyCode.P))
        {
            PrintInventory();
        }

        // S 键手动保存
        if (Input.GetKeyDown(KeyCode.S))
        {
            ManualSave();
        }

        // L 键打印游戏状态
        if (Input.GetKeyDown(KeyCode.L))
        {
            PrintGameState();
        }

        // ⭐ F2 键跳转到 Level2
        if (Input.GetKeyDown(KeyCode.F2))
        {
            JumpToLevel2();
        }
    }

    /// <summary>
    /// 添加测试物品到背包
    /// </summary>
    private void AddTestItem(int index)
    {
        if (index < 0 || index >= testItems.Count)
        {
            Debug.LogWarning($"<color=yellow>[DebugItemSpawner] 槽位 {index + 1} 没有配置物品</color>");
            return;
        }

        ItemData item = testItems[index];
        if (item == null)
        {
            Debug.LogWarning($"<color=yellow>[DebugItemSpawner] 槽位 {index + 1} 的物品为空</color>");
            return;
        }

        if (InventorySystem.Instance != null)
        {
            bool added = InventorySystem.Instance.AddItem(item);
            if (added)
            {
                Debug.Log($"<color=green>[DebugItemSpawner] ✓ 添加物品: {item.displayName} (ID: {item.itemID})</color>");
            }
            else
            {
                Debug.LogWarning($"<color=yellow>[DebugItemSpawner] 无法添加物品（背包可能已满）</color>");
            }
        }
        else
        {
            Debug.LogError("[DebugItemSpawner] InventorySystem.Instance 不存在！");
        }
    }

    /// <summary>
    /// 清空背包
    /// </summary>
    private void ClearInventory()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearInventory();
            Debug.Log("<color=yellow>[DebugItemSpawner] 背包已清空</color>");
        }
    }

    /// <summary>
    /// 切换镜子状态
    /// </summary>
    private void ToggleMirrorState()
    {
        MirrorController mirror = FindObjectOfType<MirrorController>();
        if (mirror != null)
        {
            if (mirror.currentState == MirrorController.MirrorState.Dirty)
            {
                mirror.CleanMirror();
                Debug.Log("<color=cyan>[DebugItemSpawner] 镜子状态 → Clean</color>");
            }
            else if (mirror.currentState == MirrorController.MirrorState.Clean)
            {
                mirror.SetMirrorState(MirrorController.MirrorState.Special);
                Debug.Log("<color=cyan>[DebugItemSpawner] 镜子状态 → Special</color>");
            }
            else
            {
                mirror.ResetMirror();
                Debug.Log("<color=cyan>[DebugItemSpawner] 镜子状态 → Dirty</color>");
            }
        }
        else
        {
            Debug.LogWarning("[DebugItemSpawner] 场景中没有找到 MirrorController");
        }
    }

    /// <summary>
    /// 打印背包内容
    /// </summary>
    private void PrintInventory()
    {
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[DebugItemSpawner] InventorySystem.Instance 不存在！");
            return;
        }

        var slots = InventorySystem.Instance.GetSlots();
        Debug.Log("<color=white>========== 背包内容 ==========</color>");

        int itemCount = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (!slots[i].IsEmpty)
            {
                Debug.Log($"  槽位 {i}: {slots[i].item.displayName} ({slots[i].item.itemID})");
                itemCount++;
            }
        }

        if (itemCount == 0)
        {
            Debug.Log("  （背包为空）");
        }

        Debug.Log($"<color=white>====== 共 {itemCount} 件物品 ======</color>");
    }

    /// <summary>
    /// 手动保存游戏
    /// </summary>
    private void ManualSave()
    {
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
            Debug.Log("<color=green>[DebugItemSpawner] ✓ 游戏已手动保存</color>");
        }
        else
        {
            Debug.LogError("[DebugItemSpawner] SaveLoadSystem.Instance 不存在！");
        }
    }

    /// <summary>
    /// 打印当前游戏状态
    /// </summary>
    private void PrintGameState()
    {
        Debug.Log("<color=white>========== 游戏状态 ==========</color>");

        // GameManager 状态
        if (GameManager.Instance != null)
        {
            Debug.Log($"  GameState: {GameManager.Instance.CurrentGameState}");
            Debug.Log($"  ViewState: {GameManager.Instance.CurrentViewState}");
        }

        // 镜子状态
        MirrorController mirror = FindObjectOfType<MirrorController>();
        if (mirror != null)
        {
            Debug.Log($"  镜子状态: {mirror.currentState}");
        }

        // 黑影追逐状态
        if (ShadowChaseController.Instance != null)
        {
            Debug.Log($"  黑影追逐阶段: {ShadowChaseController.Instance.currentPhase}");
        }

        // 存档状态
        if (SaveLoadSystem.Instance != null)
        {
            Debug.Log($"  有存档: {SaveLoadSystem.Instance.HasSaveData()}");
        }

        Debug.Log("<color=white>==============================</color>");
    }

    /// <summary>
    /// ⭐ 直接跳转到 Level2（跳过 Level1）
    /// </summary>
    private void JumpToLevel2()
    {
        Debug.Log("<color=magenta>========== 跳转到 Level2 ==========</color>");

        // 清空背包（模拟干净状态）
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.ClearInventory();
            Debug.Log("<color=magenta>[DebugItemSpawner] 背包已清空</color>");
        }

        // 删除存档（避免旧数据干扰）
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.DeleteSaveData();
            Debug.Log("<color=magenta>[DebugItemSpawner] 存档已删除</color>");
        }

        // 重置视图状态
        if (GameManager.Instance != null)
        {
            // 这会在场景加载后自动重置
            Debug.Log("<color=magenta>[DebugItemSpawner] 准备加载 Level2_Room...</color>");
        }

        // 加载 Level2 场景
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene("Level2_Room");
            Debug.Log("<color=magenta>[DebugItemSpawner] ✓ 正在跳转到 Level2_Room</color>");
        }
        else
        {
            Debug.LogError("[DebugItemSpawner] SceneController.Instance 不存在！无法跳转场景");
        }

        Debug.Log("<color=magenta>===================================</color>");
    }

    /// <summary>
    /// 在屏幕左上角显示调试提示（运行时可见）
    /// </summary>
    private void OnGUI()
    {
        if (!enableDebug) return;

        // 创建样式
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.fontSize = 14;
        boxStyle.alignment = TextAnchor.UpperLeft;
        boxStyle.normal.textColor = Color.white;

        // 构建显示内容
        string debugInfo = "<b>【调试模式】</b>\n" +
                          "1-9: 添加物品\n" +
                          "C: 清空背包\n" +
                          "M: 切换镜子状态\n" +
                          "P: 打印背包\n" +
                          "S: 手动保存\n" +
                          "L: 打印状态\n" +
                          "<color=yellow>F2: 跳转Level2</color>";

        // 绘制背景框和文字
        GUI.Box(new Rect(10, 10, 150, 160), debugInfo, boxStyle);
    }
}