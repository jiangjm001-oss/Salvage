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
/// 状态切换存档数据
/// 用于保存 InteractableObject 的 StateSwitch 状态
/// </summary>
[System.Serializable]
public class StateSwitchSaveData
{
    public string objectID;         // 物体ID
    public bool hasStateSwitch;     // 是否已切换状态

    public StateSwitchSaveData() { }

    public StateSwitchSaveData(string id, bool switched)
    {
        objectID = id;
        hasStateSwitch = switched;
    }
}

/// <summary>
/// 镜子状态存档数据
/// </summary>
[System.Serializable]
public class MirrorSaveData
{
    public string mirrorID;     // 镜子物体ID
    public int mirrorState;     // 镜子状态（0=Dirty, 1=Clean, 2=Special）

    public MirrorSaveData() { }

    public MirrorSaveData(string id, int state)
    {
        mirrorID = id;
        mirrorState = state;
    }
}

/// <summary>
/// 禁用物体存档数据（用于 ItemCombine 等交互后禁用的物体）
/// </summary>
[System.Serializable]
public class DisabledObjectSaveData
{
    public string objectID;     // 物体ID

    public DisabledObjectSaveData() { }

    public DisabledObjectSaveData(string id)
    {
        objectID = id;
    }
}

/// <summary>
/// 存档数据结构 - 保存所有需要持久化的游戏数据
/// </summary>
[System.Serializable]
public class SaveData
{
    public int currentLevel;                            // 当前关卡 (GameState 枚举值)
    public string currentSceneName;                     // 当前场景名称
    public int currentViewState;                        // 当前视图状态 (ViewState 枚举值) - 只保存墙面状态
    public List<SlotSaveData> inventorySlots;           // 背包槽位数据（保留位置信息）
    public List<string> pickedUpObjectIDs;              // 已被拾取的物品ID列表
    public long saveTimestamp;                          // 保存时间戳

    // ⭐ 新增：状态切换数据
    public List<StateSwitchSaveData> stateSwitchData;   // 状态切换物体数据
    public List<MirrorSaveData> mirrorData;             // 镜子状态数据
    public List<DisabledObjectSaveData> disabledObjects; // 被禁用的物体数据

    public SaveData()
    {
        inventorySlots = new List<SlotSaveData>();
        pickedUpObjectIDs = new List<string>();
        stateSwitchData = new List<StateSwitchSaveData>();
        mirrorData = new List<MirrorSaveData>();
        disabledObjects = new List<DisabledObjectSaveData>();
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

        // ⭐ 5. 保存状态切换数据
        data.stateSwitchData = GetStateSwitchData();

        // ⭐ 6. 保存镜子状态数据
        data.mirrorData = GetMirrorData();

        // ⭐ 7. 保存被禁用的物体数据（ItemCombine 等交互后禁用的物体）
        data.disabledObjects = GetDisabledObjectsData();

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

        // 确保新增的列表不为 null（兼容旧存档）
        if (data.stateSwitchData == null)
            data.stateSwitchData = new List<StateSwitchSaveData>();
        if (data.mirrorData == null)
            data.mirrorData = new List<MirrorSaveData>();
        if (data.disabledObjects == null)
            data.disabledObjects = new List<DisabledObjectSaveData>();

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

        // ⭐ 4. 恢复状态切换数据
        ApplyStateSwitchData(data.stateSwitchData);

        // ⭐ 5. 恢复镜子状态数据
        ApplyMirrorData(data.mirrorData);

        // ⭐ 6. 恢复被禁用的物体
        ApplyDisabledObjectsData(data.disabledObjects);

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

    // ============ 拾取物品相关 ============

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

    // ============ ⭐ 状态切换相关（新增） ============

    /// <summary>
    /// 获取所有已切换状态的物体数据
    /// </summary>
    private List<StateSwitchSaveData> GetStateSwitchData()
    {
        List<StateSwitchSaveData> data = new List<StateSwitchSaveData>();

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            // 只保存 StateSwitch 类型且已切换状态的物体
            if (obj.interactionType == InteractableObject.InteractionType.StateSwitch
                && obj.hasStateSwitch
                && !string.IsNullOrEmpty(obj.objectID))
            {
                data.Add(new StateSwitchSaveData(obj.objectID, obj.hasStateSwitch));
                Debug.Log($"[SaveLoadSystem] Saving state switch: {obj.objectID} = {obj.hasStateSwitch}");
            }
        }

        Debug.Log($"[SaveLoadSystem] Found {data.Count} state switched objects.");
        return data;
    }

    /// <summary>
    /// 应用状态切换数据
    /// </summary>
    private void ApplyStateSwitchData(List<StateSwitchSaveData> data)
    {
        if (data == null || data.Count == 0)
        {
            return;
        }

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (string.IsNullOrEmpty(obj.objectID)) continue;

            foreach (var savedState in data)
            {
                if (obj.objectID == savedState.objectID)
                {
                    obj.RestoreStateSwitch(savedState.hasStateSwitch);
                    Debug.Log($"[SaveLoadSystem] Restored state switch: {obj.objectID} = {savedState.hasStateSwitch}");
                    break;
                }
            }
        }
    }

    // ============ ⭐ 镜子状态相关（新增） ============

    /// <summary>
    /// 获取所有镜子状态数据
    /// </summary>
    private List<MirrorSaveData> GetMirrorData()
    {
        List<MirrorSaveData> data = new List<MirrorSaveData>();

        MirrorController[] allMirrors = FindObjectsOfType<MirrorController>(true);

        foreach (var mirror in allMirrors)
        {
            // 获取关联的 InteractableObject 的 objectID
            InteractableObject interactable = mirror.GetComponent<InteractableObject>();
            string mirrorID = interactable != null ? interactable.objectID : mirror.gameObject.name;

            data.Add(new MirrorSaveData(mirrorID, mirror.GetStateForSave()));
            Debug.Log($"[SaveLoadSystem] Saving mirror state: {mirrorID} = {mirror.currentState}");
        }

        Debug.Log($"[SaveLoadSystem] Found {data.Count} mirrors.");
        return data;
    }

    /// <summary>
    /// 应用镜子状态数据
    /// </summary>
    private void ApplyMirrorData(List<MirrorSaveData> data)
    {
        if (data == null || data.Count == 0)
        {
            return;
        }

        MirrorController[] allMirrors = FindObjectsOfType<MirrorController>(true);

        foreach (var mirror in allMirrors)
        {
            InteractableObject interactable = mirror.GetComponent<InteractableObject>();
            string mirrorID = interactable != null ? interactable.objectID : mirror.gameObject.name;

            foreach (var savedMirror in data)
            {
                if (mirrorID == savedMirror.mirrorID)
                {
                    mirror.RestoreState(savedMirror.mirrorState);
                    Debug.Log($"[SaveLoadSystem] Restored mirror state: {mirrorID} = {savedMirror.mirrorState}");
                    break;
                }
            }
        }
    }

    // ============ ⭐ 禁用物体相关（新增） ============

    /// <summary>
    /// 获取所有被禁用的物体数据（非 Pickup 类型）
    /// 用于保存 ItemCombine 等交互后禁用的物体
    /// </summary>
    private List<DisabledObjectSaveData> GetDisabledObjectsData()
    {
        List<DisabledObjectSaveData> data = new List<DisabledObjectSaveData>();

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            // 只保存非 Pickup 类型且被禁用的物体
            // （Pickup 类型已经通过 pickedUpObjectIDs 保存了）
            if (!obj.gameObject.activeSelf &&
                obj.interactionType != InteractableObject.InteractionType.Pickup &&
                !string.IsNullOrEmpty(obj.objectID))
            {
                data.Add(new DisabledObjectSaveData(obj.objectID));
                Debug.Log($"[SaveLoadSystem] Saving disabled object: {obj.objectID}");
            }
        }

        Debug.Log($"[SaveLoadSystem] Found {data.Count} disabled objects (non-pickup).");
        return data;
    }

    /// <summary>
    /// 应用被禁用的物体数据
    /// </summary>
    private void ApplyDisabledObjectsData(List<DisabledObjectSaveData> data)
    {
        if (data == null || data.Count == 0)
        {
            return;
        }

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (string.IsNullOrEmpty(obj.objectID)) continue;

            foreach (var disabledObj in data)
            {
                if (obj.objectID == disabledObj.objectID)
                {
                    obj.gameObject.SetActive(false);
                    Debug.Log($"[SaveLoadSystem] Disabled object from save: {obj.objectID}");
                    break;
                }
            }
        }
    }
}