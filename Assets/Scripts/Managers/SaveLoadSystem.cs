// Assets/Scripts/Managers/SaveLoadSystem.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 单个槽位的存档数据
/// </summary>
[System.Serializable]
public class SlotSaveData
{
    public int slotIndex;       // 槽位索引
    public string itemID;       // 物品ID（空槽位为空字符串）

    public SlotSaveData() { }

    public SlotSaveData(int index, string id)
    {
        slotIndex = index;
        itemID = id;
    }
}

/// <summary>
/// 存档数据结构 - 保存所有需要持久化的游戏数据
/// </summary>
[System.Serializable]
public class SaveData
{
    public int currentLevel;                    // 当前关卡 (GameState 枚举值)
    public string currentSceneName;             // 当前场景名称
    public int currentViewState;                // 当前视图状态 (ViewState 枚举值) - 只保存墙面状态
    public List<SlotSaveData> inventorySlots;   // 背包槽位数据（保留位置信息）
    public List<string> pickedUpObjectIDs;      // 已被拾取的物品ID列表
    public long saveTimestamp;                  // 保存时间戳

    public SaveData()
    {
        inventorySlots = new List<SlotSaveData>();
        pickedUpObjectIDs = new List<string>();
    }
}

/// <summary>
/// 存档系统 - 负责游戏进度的保存和读取
/// </summary>
public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance { get; private set; }

    // PlayerPrefs 存档键名
    private const string SAVE_KEY = "GameSaveData";
    private const string HAS_SAVE_KEY = "HasSaveData";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SaveLoadSystem] Duplicate detected! Destroying this component.");
            Destroy(this);
            return;
        }
        Instance = this;
        Debug.Log("[SaveLoadSystem] Instance initialized.");
    }

    // ============ 保存游戏 ============

    /// <summary>
    /// 保存当前游戏进度
    /// </summary>
    public void SaveGame()
    {
        Debug.Log("[SaveLoadSystem] Saving game...");

        SaveData data = new SaveData();

        // 1. 保存场景信息
        if (GameManager.Instance != null)
        {
            data.currentLevel = (int)GameManager.Instance.CurrentGameState;

            // ⭐ 关键修改：使用 GetViewStateForSave() 获取应该保存的视图状态
            // 如果当前在放大视图中，会自动转换为上一个墙面状态
            GameManager.ViewState viewStateToSave = GameManager.Instance.GetViewStateForSave();
            data.currentViewState = (int)viewStateToSave;

            Debug.Log($"[SaveLoadSystem] Saving ViewState: {viewStateToSave} (Current: {GameManager.Instance.CurrentViewState})");
        }

        if (SceneController.Instance != null)
        {
            data.currentSceneName = SceneController.Instance.GetCurrentSceneName();
        }

        // 2. 保存背包物品（包含位置信息）
        if (InventorySystem.Instance != null)
        {
            data.inventorySlots = InventorySystem.Instance.GetSlotsData();
        }

        // 3. 保存已拾取的物品ID
        data.pickedUpObjectIDs = GetPickedUpObjectIDs();

        // 4. 保存时间戳
        data.saveTimestamp = System.DateTimeOffset.Now.ToUnixTimeSeconds();

        // 序列化为JSON并保存
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.SetInt(HAS_SAVE_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log($"[SaveLoadSystem] Game saved successfully!\n{json}");
    }

    // ============ 读取游戏 ============

    /// <summary>
    /// 读取存档数据
    /// </summary>
    public SaveData LoadGame()
    {
        Debug.Log("[SaveLoadSystem] Loading game...");

        if (!HasSaveData())
        {
            Debug.LogWarning("[SaveLoadSystem] No save data found!");
            return null;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        Debug.Log($"[SaveLoadSystem] Game loaded successfully!\n{json}");
        return data;
    }

    /// <summary>
    /// 应用存档数据到游戏中
    /// </summary>
    public void ApplySaveData(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SaveLoadSystem] Cannot apply null save data!");
            return;
        }

        Debug.Log("[SaveLoadSystem] Applying save data...");

        // 1. 恢复背包物品（包含位置信息）
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.LoadFromSlotsData(data.inventorySlots);
        }

        // 2. 恢复已拾取物品状态
        ApplyPickedUpObjects(data.pickedUpObjectIDs);

        // 3. 恢复视图状态（现在一定是墙面状态，不会是放大视图）
        if (GameManager.Instance != null)
        {
            GameManager.ViewState viewState = (GameManager.ViewState)data.currentViewState;
            Debug.Log($"[SaveLoadSystem] Restoring ViewState: {viewState}");
            GameManager.Instance.RestoreViewState(viewState);
        }

        Debug.Log("[SaveLoadSystem] Save data applied successfully!");
    }

    // ============ 删除存档 ============

    /// <summary>
    /// 删除所有存档数据
    /// </summary>
    public void DeleteSaveData()
    {
        Debug.Log("[SaveLoadSystem] Deleting save data...");

        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(HAS_SAVE_KEY);
        PlayerPrefs.Save();

        Debug.Log("[SaveLoadSystem] Save data deleted.");
    }

    // ============ 检查存档 ============

    /// <summary>
    /// 检查是否有存档
    /// </summary>
    public bool HasSaveData()
    {
        return PlayerPrefs.GetInt(HAS_SAVE_KEY, 0) == 1 && PlayerPrefs.HasKey(SAVE_KEY);
    }

    // ============ 辅助方法 ============

    /// <summary>
    /// 获取所有已被拾取的物品ID
    /// </summary>
    private List<string> GetPickedUpObjectIDs()
    {
        List<string> pickedUpIDs = new List<string>();

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (!obj.gameObject.activeSelf &&
                obj.interactionType == InteractableObject.InteractionType.Pickup &&
                !string.IsNullOrEmpty(obj.objectID))
            {
                pickedUpIDs.Add(obj.objectID);
            }
        }

        Debug.Log($"[SaveLoadSystem] Found {pickedUpIDs.Count} picked up objects.");
        return pickedUpIDs;
    }

    /// <summary>
    /// 应用已拾取物品状态
    /// </summary>
    public void ApplyPickedUpObjects(List<string> pickedUpIDs)
    {
        if (pickedUpIDs == null || pickedUpIDs.Count == 0)
        {
            return;
        }

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (!string.IsNullOrEmpty(obj.objectID) && pickedUpIDs.Contains(obj.objectID))
            {
                obj.gameObject.SetActive(false);
                Debug.Log($"[SaveLoadSystem] Disabled picked up object: {obj.objectID}");
            }
        }
    }

    /// <summary>
    /// 记录物品被拾取（实时保存）
    /// </summary>
    public void OnItemPickedUp(string objectID)
    {
        Debug.Log($"[SaveLoadSystem] Item picked up: {objectID}. Auto-saving...");
        SaveGame();
    }
}