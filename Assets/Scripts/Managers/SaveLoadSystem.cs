// Assets/Scripts/Managers/SaveLoadSystem.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 单个槽位的存档数据
/// </summary>
[System.Serializable]
public class SlotSaveData
{
    public int slotIndex;
    public string itemID;

    public SlotSaveData() { }

    public SlotSaveData(int index, string id)
    {
        slotIndex = index;
        itemID = id;
    }
}

/// <summary>
/// 状态切换存档数据
/// </summary>
[System.Serializable]
public class StateSwitchSaveData
{
    public string objectID;
    public bool hasStateSwitch;

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
    public string mirrorID;
    public int mirrorState;

    public MirrorSaveData() { }

    public MirrorSaveData(string id, int state)
    {
        mirrorID = id;
        mirrorState = state;
    }
}

/// <summary>
/// 禁用物体存档数据
/// </summary>
[System.Serializable]
public class DisabledObjectSaveData
{
    public string objectID;

    public DisabledObjectSaveData() { }

    public DisabledObjectSaveData(string id)
    {
        objectID = id;
    }
}

/// <summary>
/// 容器状态存档数据
/// </summary>
[System.Serializable]
public class ContainerSaveData
{
    public string objectID;
    public bool isUnlocked;
    public bool isOpen;

    public ContainerSaveData() { }

    public ContainerSaveData(string id, bool unlocked, bool open)
    {
        objectID = id;
        isUnlocked = unlocked;
        isOpen = open;
    }
}

/// <summary>
/// 存档数据结构 - 保存所有需要持久化的游戏数据
/// </summary>
[System.Serializable]
public class SaveData
{
    public int currentLevel;
    public string currentSceneName;
    public int currentViewState;
    public List<SlotSaveData> inventorySlots;
    public List<string> pickedUpObjectIDs;
    public long saveTimestamp;

    // 状态切换数据
    public List<StateSwitchSaveData> stateSwitchData;
    public List<MirrorSaveData> mirrorData;
    public List<DisabledObjectSaveData> disabledObjects;

    // 黑影追逐进度
    public int shadowChasePhase;

    // 物体切换解锁数据
    public List<string> swapUnlockedObjectIDs;

    // ⭐ 新增：容器状态数据
    public List<ContainerSaveData> containerData;

    public SaveData()
    {
        inventorySlots = new List<SlotSaveData>();
        pickedUpObjectIDs = new List<string>();
        stateSwitchData = new List<StateSwitchSaveData>();
        mirrorData = new List<MirrorSaveData>();
        disabledObjects = new List<DisabledObjectSaveData>();
        shadowChasePhase = 0;
        swapUnlockedObjectIDs = new List<string>();
        containerData = new List<ContainerSaveData>();
    }
}

/// <summary>
/// 存档系统 - 负责游戏进度的保存和读取
/// </summary>
public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance { get; private set; }

    private const string SAVE_KEY = "GameSaveData";
    private const string HAS_SAVE_KEY = "HasSaveData";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        Debug.Log("[SaveLoadSystem] Instance initialized.");
    }

    // ============ 保存游戏 ============

    public void SaveGame()
    {
        Debug.Log("[SaveLoadSystem] Saving game...");

        SaveData data = new SaveData();

        // 1. 保存场景信息
        if (GameManager.Instance != null)
        {
            data.currentLevel = (int)GameManager.Instance.CurrentGameState;
            GameManager.ViewState viewStateToSave = GameManager.Instance.GetViewStateForSave();
            data.currentViewState = (int)viewStateToSave;
        }

        if (SceneController.Instance != null)
        {
            data.currentSceneName = SceneController.Instance.GetCurrentSceneName();
        }

        // 2. 保存背包物品
        if (InventorySystem.Instance != null)
        {
            data.inventorySlots = InventorySystem.Instance.GetSlotsData();
        }

        // 3. 保存已拾取的物品ID
        data.pickedUpObjectIDs = GetPickedUpObjectIDs();

        // 4. 保存时间戳
        data.saveTimestamp = System.DateTimeOffset.Now.ToUnixTimeSeconds();

        // 5. 保存状态切换数据
        data.stateSwitchData = GetStateSwitchData();

        // 6. 保存镜子状态数据
        data.mirrorData = GetMirrorData();

        // 7. 保存被禁用的物体数据
        data.disabledObjects = GetDisabledObjectsData();

        // 8. 保存黑影追逐进度
        data.shadowChasePhase = GetShadowChasePhase();

        // 9. 保存物体切换解锁数据
        data.swapUnlockedObjectIDs = GetSwapUnlockedObjectIDs();

        // ⭐ 10. 保存容器状态数据
        data.containerData = GetContainerData();

        // 序列化为JSON并保存
        string json = JsonUtility.ToJson(data, true);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.SetInt(HAS_SAVE_KEY, 1);
        PlayerPrefs.Save();

        Debug.Log($"[SaveLoadSystem] Game saved successfully!");
    }

    // ============ 读取游戏 ============

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

        // 兼容旧存档
        if (data.stateSwitchData == null)
            data.stateSwitchData = new List<StateSwitchSaveData>();
        if (data.mirrorData == null)
            data.mirrorData = new List<MirrorSaveData>();
        if (data.disabledObjects == null)
            data.disabledObjects = new List<DisabledObjectSaveData>();
        if (data.swapUnlockedObjectIDs == null)
            data.swapUnlockedObjectIDs = new List<string>();
        if (data.containerData == null)
            data.containerData = new List<ContainerSaveData>();

        Debug.Log($"[SaveLoadSystem] Game loaded successfully!");
        return data;
    }

    public void ApplySaveData(SaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[SaveLoadSystem] Cannot apply null save data!");
            return;
        }

        Debug.Log("[SaveLoadSystem] Applying save data...");

        // 1. 恢复背包物品
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.LoadFromSlotsData(data.inventorySlots);
        }

        // 2. 恢复已拾取物品状态
        ApplyPickedUpObjects(data.pickedUpObjectIDs);

        // 3. 恢复视图状态
        if (GameManager.Instance != null)
        {
            GameManager.ViewState viewState = (GameManager.ViewState)data.currentViewState;
            GameManager.Instance.RestoreViewState(viewState);
        }

        // 4. 恢复状态切换数据
        ApplyStateSwitchData(data.stateSwitchData);

        // 5. 恢复镜子状态数据
        ApplyMirrorData(data.mirrorData);

        // 6. 恢复被禁用的物体
        ApplyDisabledObjectsData(data.disabledObjects);

        // 7. 恢复黑影追逐进度
        ApplyShadowChasePhase(data.shadowChasePhase);

        // 8. 恢复物体切换解锁状态
        ApplySwapUnlockedObjects(data.swapUnlockedObjectIDs);

        // ⭐ 9. 恢复容器状态
        ApplyContainerData(data.containerData);

        Debug.Log("[SaveLoadSystem] Save data applied successfully!");
    }

    // ============ 删除存档 ============

    public void DeleteSaveData()
    {
        Debug.Log("[SaveLoadSystem] Deleting save data...");
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(HAS_SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[SaveLoadSystem] Save data deleted.");
    }

    // ============ 检查存档 ============

    public bool HasSaveData()
    {
        return PlayerPrefs.GetInt(HAS_SAVE_KEY, 0) == 1 && PlayerPrefs.HasKey(SAVE_KEY);
    }

    // ============ 拾取物品相关 ============

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

        return pickedUpIDs;
    }

    public void ApplyPickedUpObjects(List<string> pickedUpIDs)
    {
        if (pickedUpIDs == null || pickedUpIDs.Count == 0) return;

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (!string.IsNullOrEmpty(obj.objectID) && pickedUpIDs.Contains(obj.objectID))
            {
                // ⭐ 标记为已拾取（供容器判断是否显示）
                obj.MarkAsPickedUp();

                obj.gameObject.SetActive(false);
            }
        }
    }

    public void OnItemPickedUp(string objectID)
    {
        Debug.Log($"[SaveLoadSystem] Item picked up: {objectID}. Auto-saving...");
        SaveGame();
    }

    // ============ 状态切换相关 ============

    private List<StateSwitchSaveData> GetStateSwitchData()
    {
        List<StateSwitchSaveData> data = new List<StateSwitchSaveData>();
        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (obj.interactionType == InteractableObject.InteractionType.StateSwitch
                && obj.hasStateSwitch
                && !string.IsNullOrEmpty(obj.objectID))
            {
                data.Add(new StateSwitchSaveData(obj.objectID, obj.hasStateSwitch));
            }
        }

        return data;
    }

    private void ApplyStateSwitchData(List<StateSwitchSaveData> data)
    {
        if (data == null || data.Count == 0) return;

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (string.IsNullOrEmpty(obj.objectID)) continue;

            foreach (var savedState in data)
            {
                if (obj.objectID == savedState.objectID)
                {
                    obj.RestoreStateSwitch(savedState.hasStateSwitch);
                    break;
                }
            }
        }
    }

    // ============ 镜子状态相关 ============

    private List<MirrorSaveData> GetMirrorData()
    {
        List<MirrorSaveData> data = new List<MirrorSaveData>();
        MirrorController[] allMirrors = FindObjectsOfType<MirrorController>(true);

        foreach (var mirror in allMirrors)
        {
            InteractableObject interactable = mirror.GetComponent<InteractableObject>();
            string mirrorID = interactable != null ? interactable.objectID : mirror.gameObject.name;
            data.Add(new MirrorSaveData(mirrorID, mirror.GetStateForSave()));
        }

        return data;
    }

    private void ApplyMirrorData(List<MirrorSaveData> data)
    {
        if (data == null || data.Count == 0) return;

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
                    break;
                }
            }
        }
    }

    // ============ 禁用物体相关 ============

    private List<DisabledObjectSaveData> GetDisabledObjectsData()
    {
        List<DisabledObjectSaveData> data = new List<DisabledObjectSaveData>();
        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (!obj.gameObject.activeSelf &&
                obj.interactionType != InteractableObject.InteractionType.Pickup &&
                !string.IsNullOrEmpty(obj.objectID))
            {
                data.Add(new DisabledObjectSaveData(obj.objectID));
            }
        }

        return data;
    }

    private void ApplyDisabledObjectsData(List<DisabledObjectSaveData> data)
    {
        if (data == null || data.Count == 0) return;

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (string.IsNullOrEmpty(obj.objectID)) continue;

            foreach (var disabledObj in data)
            {
                if (obj.objectID == disabledObj.objectID)
                {
                    obj.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    // ============ ⭐ 黑影追逐相关（新增） ============

    private int GetShadowChasePhase()
    {
        if (ShadowChaseController.Instance != null)
        {
            return ShadowChaseController.Instance.GetPhaseForSave();
        }
        return 0;
    }

    private void ApplyShadowChasePhase(int phase)
    {
        if (ShadowChaseController.Instance != null)
        {
            ShadowChaseController.Instance.RestorePhase(phase);
        }
    }

    // ============ ⭐ 物体切换解锁相关（新增） ============

    /// <summary>
    /// 获取所有已解锁的物体切换ID
    /// </summary>
    private List<string> GetSwapUnlockedObjectIDs()
    {
        List<string> unlockedIDs = new List<string>();
        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (obj.interactionType == InteractableObject.InteractionType.ObjectSwap
                && obj.isSwapUnlocked
                && !string.IsNullOrEmpty(obj.objectID))
            {
                unlockedIDs.Add(obj.objectID);
            }
        }

        return unlockedIDs;
    }

    /// <summary>
    /// 恢复物体切换解锁状态
    /// </summary>
    private void ApplySwapUnlockedObjects(List<string> unlockedIDs)
    {
        if (unlockedIDs == null || unlockedIDs.Count == 0) return;

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (obj.interactionType == InteractableObject.InteractionType.ObjectSwap
                && !string.IsNullOrEmpty(obj.objectID)
                && unlockedIDs.Contains(obj.objectID))
            {
                obj.RestoreSwapUnlocked(true);
            }
        }
    }

    // ============ ⭐ 容器状态相关（新增） ============

    /// <summary>
    /// 获取所有容器的状态数据
    /// </summary>
    private List<ContainerSaveData> GetContainerData()
    {
        List<ContainerSaveData> data = new List<ContainerSaveData>();
        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (obj.interactionType == InteractableObject.InteractionType.Container
                && !string.IsNullOrEmpty(obj.objectID))
            {
                data.Add(new ContainerSaveData(obj.objectID, obj.isContainerUnlocked, obj.isContainerOpen));
            }
        }

        return data;
    }

    /// <summary>
    /// 恢复容器状态
    /// </summary>
    private void ApplyContainerData(List<ContainerSaveData> data)
    {
        if (data == null || data.Count == 0) return;

        InteractableObject[] allObjects = FindObjectsOfType<InteractableObject>(true);

        foreach (var obj in allObjects)
        {
            if (obj.interactionType != InteractableObject.InteractionType.Container) continue;
            if (string.IsNullOrEmpty(obj.objectID)) continue;

            foreach (var savedContainer in data)
            {
                if (obj.objectID == savedContainer.objectID)
                {
                    obj.RestoreContainerState(savedContainer.isUnlocked, savedContainer.isOpen);
                    break;
                }
            }
        }
    }
}