// Assets/Scripts/GamePlay/OrganCollectionPuzzle.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 器官收集谜题 - 主控制器
/// 玩家需要在背包中选中容器，然后按正确顺序点击器官罐子
/// </summary>
public class OrganCollectionPuzzle : MonoBehaviour
{
    [System.Serializable]
    public class OrganSequenceItem
    {
        [Tooltip("器官名称（用于调试）")]
        public string organName;

        [Tooltip("对应的罐子对象")]
        public OrganJar jar;
    }

    [Header("容器物品")]
    [Tooltip("玩家需要先选中这个容器才能开始收集")]
    public ItemData requiredContainerItem;

    [Header("收集顺序配置")]
    [Tooltip("按照数组顺序配置：心→肝→肺→脾→左肾→右肾")]
    public OrganSequenceItem[] correctSequence = new OrganSequenceItem[6];

    [Header("所有罐子引用")]
    [Tooltip("第一层罐子（6个）：心、大肠、肺、脑、左肾、膀胱")]
    public OrganJar[] firstRowJars = new OrganJar[6];

    [Tooltip("第二层罐子（6个）：胃、肝、小肠、脾、胆、右肾")]
    public OrganJar[] secondRowJars = new OrganJar[6];

    [Header("音效")]
    public string correctPickSound = "Audio/SFX/organ_collect";
    public string wrongPickSound = "Audio/SFX/puzzle_fail";
    public string completeSound = "Audio/SFX/puzzle_complete";
    public string noContainerSound = "Audio/SFX/ui_error";

    [Header("提示文本（可选）")]
    [Tooltip("未选中容器时的提示")]
    public string noContainerHint = "需要先选中容器才能收集器官";

    [Tooltip("选错器官时的提示")]
    public string wrongOrganHint = "顺序错误，需要重新开始";

    [Header("事件")]
    public UnityEvent OnPuzzleComplete;
    public UnityEvent OnSequenceReset;
    public UnityEvent<int> OnCorrectPick;  // 参数：当前进度（0-5）

    [Header("存档")]
    public string puzzleID = "organ_collection_01";

    // 内部状态
    private int currentStep = 0;
    private bool isPuzzleComplete = false;
    private List<OrganJar> allJars = new List<OrganJar>();

    private void Awake()
    {
        // 收集所有罐子引用
        CollectAllJars();

        // 注册所有罐子的点击回调
        RegisterJarCallbacks();
    }

    private void Start()
    {
        // 尝试从存档恢复
        RestoreFromSave();
    }

    /// <summary>
    /// 收集所有罐子到列表中
    /// </summary>
    private void CollectAllJars()
    {
        allJars.Clear();

        foreach (var jar in firstRowJars)
        {
            if (jar != null) allJars.Add(jar);
        }

        foreach (var jar in secondRowJars)
        {
            if (jar != null) allJars.Add(jar);
        }

        Debug.Log($"[OrganCollectionPuzzle] 收集到 {allJars.Count} 个罐子");
    }

    /// <summary>
    /// 注册所有罐子的点击回调
    /// </summary>
    private void RegisterJarCallbacks()
    {
        foreach (var jar in allJars)
        {
            if (jar != null)
            {
                jar.SetPuzzleController(this);
            }
        }
    }

    /// <summary>
    /// 当玩家点击某个罐子时调用
    /// </summary>
    public void OnJarClicked(OrganJar clickedJar)
    {
        if (isPuzzleComplete)
        {
            Debug.Log("[OrganCollectionPuzzle] 谜题已完成");
            return;
        }

        // 检查是否选中了容器
        if (!IsContainerSelected())
        {
            Debug.Log("[OrganCollectionPuzzle] 未选中容器");
            PlaySound(noContainerSound);
            ShowHint(noContainerHint);
            return;
        }

        // 检查点击的是否是当前需要的器官
        if (currentStep < correctSequence.Length)
        {
            OrganJar expectedJar = correctSequence[currentStep].jar;

            if (clickedJar == expectedJar)
            {
                // 正确！
                HandleCorrectPick(clickedJar);
            }
            else
            {
                // 错误！
                HandleWrongPick(clickedJar);
            }
        }
    }

    /// <summary>
    /// 检查是否选中了正确的容器
    /// </summary>
    private bool IsContainerSelected()
    {
        if (requiredContainerItem == null)
        {
            // 如果没有配置必需容器，则总是允许
            return true;
        }

        if (UIManager.Instance == null) return false;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return false;

        return selectedItem.itemID == requiredContainerItem.itemID;
    }

    /// <summary>
    /// 处理正确点击
    /// </summary>
    private void HandleCorrectPick(OrganJar jar)
    {
        string organName = correctSequence[currentStep].organName;
        Debug.Log($"[OrganCollectionPuzzle] ✓ 正确收集: {organName} (步骤 {currentStep + 1}/6)");

        // 播放正确音效
        PlaySound(correctPickSound);

        // 隐藏器官（从罐子中消失）
        jar.CollectOrgan();

        // 触发事件
        OnCorrectPick?.Invoke(currentStep);

        // 进入下一步
        currentStep++;

        // 检查是否完成
        if (currentStep >= correctSequence.Length)
        {
            CompletePuzzle();
        }

        // 保存进度
        SaveState();
    }

    /// <summary>
    /// 处理错误点击
    /// </summary>
    private void HandleWrongPick(OrganJar clickedJar)
    {
        Debug.Log($"[OrganCollectionPuzzle] ✗ 错误点击: {clickedJar.organName}，需要重新开始");

        // 播放错误音效
        PlaySound(wrongPickSound);

        // 显示提示
        ShowHint(wrongOrganHint);

        // 重置谜题
        ResetSequence();
    }

    /// <summary>
    /// 重置收集顺序（所有器官重新出现）
    /// </summary>
    public void ResetSequence()
    {
        Debug.Log("[OrganCollectionPuzzle] 重置谜题");

        currentStep = 0;

        // 恢复所有在正确顺序中的器官
        foreach (var item in correctSequence)
        {
            if (item.jar != null)
            {
                item.jar.RestoreOrgan();
            }
        }

        OnSequenceReset?.Invoke();
        SaveState();
    }

    /// <summary>
    /// 完成谜题
    /// </summary>
    private void CompletePuzzle()
    {
        Debug.Log("[OrganCollectionPuzzle] ★ 谜题完成！");

        isPuzzleComplete = true;

        // 播放完成音效
        PlaySound(completeSound);

        // 触发完成事件
        OnPuzzleComplete?.Invoke();

        SaveState();
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    /// <summary>
    /// 显示提示（如果有提示系统）
    /// </summary>
    private void ShowHint(string hint)
    {
        // 这里可以接入你的提示系统
        // 例如：UIManager.Instance?.ShowHint(hint);
        Debug.Log($"[OrganCollectionPuzzle] 提示: {hint}");
    }

    // ============ 存档系统 ============

    private void SaveState()
    {
        if (SaveLoadSystem.Instance == null) return;

        string key = $"puzzle_{puzzleID}";

        // 保存当前进度和完成状态
        PlayerPrefs.SetInt($"{key}_step", currentStep);
        PlayerPrefs.SetInt($"{key}_complete", isPuzzleComplete ? 1 : 0);

        // 保存每个序列中罐子的收集状态
        for (int i = 0; i < correctSequence.Length; i++)
        {
            bool collected = (correctSequence[i].jar != null && correctSequence[i].jar.IsCollected);
            PlayerPrefs.SetInt($"{key}_jar_{i}", collected ? 1 : 0);
        }

        PlayerPrefs.Save();
    }

    private void RestoreFromSave()
    {
        string key = $"puzzle_{puzzleID}";

        // 检查是否有存档
        if (!PlayerPrefs.HasKey($"{key}_step")) return;

        currentStep = PlayerPrefs.GetInt($"{key}_step", 0);
        isPuzzleComplete = PlayerPrefs.GetInt($"{key}_complete", 0) == 1;

        // 恢复每个罐子的状态
        for (int i = 0; i < correctSequence.Length; i++)
        {
            bool collected = PlayerPrefs.GetInt($"{key}_jar_{i}", 0) == 1;
            if (collected && correctSequence[i].jar != null)
            {
                correctSequence[i].jar.CollectOrgan();
            }
        }

        Debug.Log($"[OrganCollectionPuzzle] 从存档恢复: 步骤={currentStep}, 完成={isPuzzleComplete}");
    }

    /// <summary>
    /// 清除存档（用于调试或重新开始游戏）
    /// </summary>
    public void ClearSave()
    {
        string key = $"puzzle_{puzzleID}";

        PlayerPrefs.DeleteKey($"{key}_step");
        PlayerPrefs.DeleteKey($"{key}_complete");

        for (int i = 0; i < correctSequence.Length; i++)
        {
            PlayerPrefs.DeleteKey($"{key}_jar_{i}");
        }

        PlayerPrefs.Save();
        Debug.Log("[OrganCollectionPuzzle] 存档已清除");
    }

    // ============ 调试方法 ============

    /// <summary>
    /// 获取当前进度（用于UI显示）
    /// </summary>
    public int GetCurrentStep() => currentStep;

    /// <summary>
    /// 获取是否完成
    /// </summary>
    public bool IsComplete() => isPuzzleComplete;

    /// <summary>
    /// 获取下一个需要收集的器官名称
    /// </summary>
    public string GetNextOrganName()
    {
        if (currentStep < correctSequence.Length)
        {
            return correctSequence[currentStep].organName;
        }
        return "";
    }

#if UNITY_EDITOR
    [ContextMenu("Debug - 打印状态")]
    private void DebugPrintStatus()
    {
        Debug.Log($"[OrganCollectionPuzzle] 当前状态:");
        Debug.Log($"  - 步骤: {currentStep}/6");
        Debug.Log($"  - 完成: {isPuzzleComplete}");
        Debug.Log($"  - 下一个: {GetNextOrganName()}");
        Debug.Log($"  - 容器已选中: {IsContainerSelected()}");
    }

    [ContextMenu("Debug - 重置谜题")]
    private void DebugResetPuzzle()
    {
        isPuzzleComplete = false;
        ResetSequence();
    }

    [ContextMenu("Debug - 清除存档")]
    private void DebugClearSave()
    {
        ClearSave();
    }
#endif
}