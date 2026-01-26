// Assets/Scripts/GamePlay/Letter/LetterManager.cs
// 信纸状态管理器 - 简化版
// ZoomView：分层显示 | 背包：固定图标（玩家只会有一张信纸）
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 信纸状态管理器
/// 管理信纸的三个独立状态：收件人、标题、Logo
/// 
/// 显示方式：
/// - ZoomView 中：使用 LetterDisplay 组件管理子物体的显示/隐藏
/// - 背包图标：固定一张图（玩家不会同时有两张信纸）
/// </summary>
public class LetterManager : MonoBehaviour
{
    public static LetterManager Instance { get; private set; }

    // ============ 信纸状态 ============
    [Header("信纸状态")]
    [Tooltip("是否已有收件人（打字机完成）")]
    public bool hasRecipient = false;

    [Tooltip("是否已有标题（羽毛笔桌面粘贴完成）")]
    public bool hasTitle = false;

    [Tooltip("是否已有 Logo（羽毛笔涂抹完成）")]
    public bool hasLogo = false;

    // ============ 物品配置 ============
    [Header("物品配置")]
    [Tooltip("背包中的信纸物品数据")]
    public ItemData letterItemData;

    // ============ 事件 ============
    [Header("事件")]
    [Tooltip("信纸状态变化时触发（参数为状态编号 0-7）")]
    public UnityEvent<int> OnLetterStateChanged;

    [Tooltip("信纸三项全部完成时触发")]
    public UnityEvent OnLetterCompleted;

    [Tooltip("收件人完成时触发")]
    public UnityEvent OnRecipientComplete;

    [Tooltip("标题完成时触发")]
    public UnityEvent OnTitleComplete;

    [Tooltip("Logo完成时触发")]
    public UnityEvent OnLogoComplete;

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ============ 状态查询 ============

    /// <summary>
    /// 获取当前状态编号 (0-7)，用于调试或特殊需求
    /// </summary>
    public int GetStateIndex()
    {
        int index = 0;
        if (hasRecipient) index += 1;
        if (hasTitle) index += 2;
        if (hasLogo) index += 4;
        return index;
    }

    /// <summary>
    /// 检查是否全部完成
    /// </summary>
    public bool IsComplete()
    {
        return hasRecipient && hasTitle && hasLogo;
    }

    /// <summary>
    /// 检查背包中是否有信纸
    /// </summary>
    public bool HasLetterInInventory()
    {
        if (letterItemData == null || InventorySystem.Instance == null)
            return false;

        return InventorySystem.Instance.HasItem(letterItemData.itemID);
    }

    // ============ 状态设置 ============

    /// <summary>
    /// 设置收件人完成（打字机打出 BlackHat）
    /// </summary>
    public void SetRecipientComplete()
    {
        if (hasRecipient)
        {
            Debug.Log("[LetterManager] 收件人已完成，跳过");
            return;
        }

        hasRecipient = true;
        Debug.Log("[LetterManager] ✓ 收件人完成");

        OnRecipientComplete?.Invoke();
        NotifyStateChanged();
    }

    /// <summary>
    /// 设置标题完成（羽毛笔桌面粘贴标题）
    /// </summary>
    public void SetTitleComplete()
    {
        if (hasTitle)
        {
            Debug.Log("[LetterManager] 标题已完成，跳过");
            return;
        }

        hasTitle = true;
        Debug.Log("[LetterManager] ✓ 标题完成");

        OnTitleComplete?.Invoke();
        NotifyStateChanged();
    }

    /// <summary>
    /// 设置 Logo 完成（羽毛笔涂抹 Logo）
    /// </summary>
    public void SetLogoComplete()
    {
        if (hasLogo)
        {
            Debug.Log("[LetterManager] Logo 已完成，跳过");
            return;
        }

        hasLogo = true;
        Debug.Log("[LetterManager] ✓ Logo 完成");

        OnLogoComplete?.Invoke();
        NotifyStateChanged();
    }

    // ============ 背包操作 ============

    /// <summary>
    /// 从背包移除信纸
    /// </summary>
    public void RemoveLetterFromInventory()
    {
        if (letterItemData == null || InventorySystem.Instance == null)
            return;

        InventorySystem.Instance.RemoveItem(letterItemData);
        Debug.Log("[LetterManager] 从背包移除信纸");
    }

    /// <summary>
    /// 添加信纸到背包
    /// </summary>
    public void AddLetterToInventory()
    {
        if (letterItemData == null || InventorySystem.Instance == null)
            return;

        InventorySystem.Instance.AddItem(letterItemData);
        Debug.Log("[LetterManager] 添加信纸到背包");
    }

    // ============ 内部方法 ============

    /// <summary>
    /// 通知状态变更
    /// </summary>
    private void NotifyStateChanged()
    {
        int stateIndex = GetStateIndex();
        Debug.Log($"[LetterManager] 状态变更: {stateIndex} (R={hasRecipient}, T={hasTitle}, L={hasLogo})");

        // 触发状态变化事件（LetterDisplay 会监听这个事件自动刷新显示）
        OnLetterStateChanged?.Invoke(stateIndex);

        // 检查是否全部完成
        if (IsComplete())
        {
            Debug.Log("[LetterManager] ★ 信纸全部完成！");
            OnLetterCompleted?.Invoke();
        }

        // 自动保存
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ 存档相关 ============

    /// <summary>
    /// 获取存档数据
    /// </summary>
    public LetterSaveData GetSaveData()
    {
        return new LetterSaveData(hasRecipient, hasTitle, hasLogo);
    }

    /// <summary>
    /// 从存档恢复
    /// </summary>
    public void RestoreFromSave(LetterSaveData data)
    {
        if (data == null) return;

        hasRecipient = data.hasRecipient;
        hasTitle = data.hasTitle;
        hasLogo = data.hasLogo;

        // 触发一次状态变化事件，让所有 LetterDisplay 刷新
        OnLetterStateChanged?.Invoke(GetStateIndex());

        Debug.Log($"[LetterManager] 恢复状态: R={hasRecipient}, T={hasTitle}, L={hasLogo}");
    }

    /// <summary>
    /// 重置状态（用于新游戏）
    /// </summary>
    public void ResetState()
    {
        hasRecipient = false;
        hasTitle = false;
        hasLogo = false;
        OnLetterStateChanged?.Invoke(0);
        Debug.Log("[LetterManager] 状态已重置");
    }

    // ============ 调试方法 ============

    [ContextMenu("Debug: 完成收件人")]
    private void DebugSetRecipient() => SetRecipientComplete();

    [ContextMenu("Debug: 完成标题")]
    private void DebugSetTitle() => SetTitleComplete();

    [ContextMenu("Debug: 完成Logo")]
    private void DebugSetLogo() => SetLogoComplete();

    [ContextMenu("Debug: 重置所有状态")]
    private void DebugReset() => ResetState();

    [ContextMenu("Debug: 打印当前状态")]
    private void DebugPrintState()
    {
        Debug.Log($"[LetterManager] 当前状态: Index={GetStateIndex()}, R={hasRecipient}, T={hasTitle}, L={hasLogo}, Complete={IsComplete()}");
    }
}

/// <summary>
/// 信纸存档数据
/// </summary>
[System.Serializable]
public class LetterSaveData
{
    public bool hasRecipient;
    public bool hasTitle;
    public bool hasLogo;

    public LetterSaveData() { }

    public LetterSaveData(bool r, bool t, bool l)
    {
        hasRecipient = r;
        hasTitle = t;
        hasLogo = l;
    }
}