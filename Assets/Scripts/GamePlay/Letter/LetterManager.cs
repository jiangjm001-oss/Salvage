// Assets/Scripts/GamePlay/Letter/LetterManager.cs
// 信纸状态管理器 - 全局单例
// 管理信纸的三个独立状态：收件人、标题、Logo
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 信纸状态管理器
/// 信纸有 8 种组合状态（3 个独立 bool）
/// 状态编号: 0=空, 1=收件人, 2=标题, 3=收件人+标题, 
///          4=Logo, 5=收件人+Logo, 6=标题+Logo, 7=全部完成
/// </summary>
public class LetterManager : MonoBehaviour
{
    public static LetterManager Instance { get; private set; }

    [Header("信纸状态")]
    [Tooltip("是否已有收件人（打字机完成）")]
    public bool hasRecipient = false;

    [Tooltip("是否已有标题（羽毛笔桌面粘贴完成）")]
    public bool hasTitle = false;

    [Tooltip("是否已有 Logo（羽毛笔涂抹完成）")]
    public bool hasLogo = false;

    [Header("信纸物品配置")]
    [Tooltip("背包中的信纸物品数据")]
    public ItemData letterItemData;

    [Header("精灵图配置")]
    [Tooltip("按状态编号 0-7 配置精灵图")]
    public Sprite[] letterSprites = new Sprite[8];

    [Header("事件")]
    [Tooltip("信纸状态变化时触发（参数为状态编号 0-7）")]
    public UnityEvent<int> OnLetterStateChanged;

    [Tooltip("信纸三项全部完成时触发")]
    public UnityEvent OnLetterCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// 获取当前状态编号 (0-7)
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
    /// 获取当前状态对应的精灵图
    /// </summary>
    public Sprite GetCurrentSprite()
    {
        int index = GetStateIndex();
        if (index >= 0 && index < letterSprites.Length && letterSprites[index] != null)
        {
            return letterSprites[index];
        }
        // 默认返回空信纸或第一张可用的
        return letterSprites.Length > 0 ? letterSprites[0] : null;
    }

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
        NotifyStateChanged();
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

        // HasItem 需要 string itemID 参数
        return InventorySystem.Instance.HasItem(letterItemData.itemID);
    }

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

        // 先更新图标
        UpdateLetterIcon();

        InventorySystem.Instance.AddItem(letterItemData);
        Debug.Log("[LetterManager] 添加信纸到背包");
    }

    private void NotifyStateChanged()
    {
        int stateIndex = GetStateIndex();
        Debug.Log($"[LetterManager] 状态变更: {stateIndex} (R={hasRecipient}, T={hasTitle}, L={hasLogo})");

        // 更新背包中信纸的图标
        UpdateLetterIcon();

        // 触发事件
        OnLetterStateChanged?.Invoke(stateIndex);

        // 检查是否全部完成
        if (IsComplete())
        {
            Debug.Log("[LetterManager] ★ 信纸全部完成！");
            OnLetterCompleted?.Invoke();
        }

        // 自动保存
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    /// <summary>
    /// 更新背包中信纸的图标
    /// </summary>
    private void UpdateLetterIcon()
    {
        if (letterItemData != null)
        {
            Sprite newIcon = GetCurrentSprite();
            if (newIcon != null)
            {
                letterItemData.icon = newIcon;
            }

            // 通过 InventorySystem 的事件触发 UI 更新
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.OnInventoryChanged?.Invoke();
            }
        }
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

        // 更新图标（不触发事件）
        UpdateLetterIcon();

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
        UpdateLetterIcon();
        Debug.Log("[LetterManager] 状态已重置");
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