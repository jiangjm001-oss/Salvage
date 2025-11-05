// Assets/Scripts/Managers/SaveLoadSystem.cs
using UnityEngine;

// 存档数据结构
[System.Serializable]
public class SaveData
{
    public int currentLevel; // 0: Main Menu, 1: Level1, 2: Level2
    public string currentSceneName;
    // 在这里添加需要保存的其他数据，如背包物品、谜题状态等
    // public InventoryData inventory;
}

public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // --- 确认这里有这两行，并且顺序正确 ---
        transform.SetParent(null); // 先把自己从父对象中解放出来
        DontDestroyOnLoad(gameObject); // 再设置为持久存在
    }

    public void SaveGame()
    {
        Debug.Log("SaveLoadSystem: Saving game...");
        SaveData data = new SaveData
        {
            currentLevel = (int)GameManager.Instance.CurrentGameState,
            currentSceneName = SceneController.Instance.GetCurrentSceneName()
        };
        // TODO: 实现将 data 对象序列化为 JSON 或二进制并写入文件的逻辑
        // PlayerPrefs.SetString("SaveData", JsonUtility.ToJson(data));
    }

    public SaveData LoadGame()
    {
        Debug.Log("SaveLoadSystem: Loading game...");
        // TODO: 实现从文件读取数据并反序列化为 SaveData 对象的逻辑
        // if (PlayerPrefs.HasKey("SaveData"))
        // {
        //     return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString("SaveData"));
        // }
        return null; // 暂时返回 null，表示没有存档
    }

    public void DeleteSaveData()
    {
        Debug.Log("SaveLoadSystem: Deleting save data.");
        // TODO: 实现删除存档文件的逻辑
        // PlayerPrefs.DeleteKey("SaveData");
    }
}