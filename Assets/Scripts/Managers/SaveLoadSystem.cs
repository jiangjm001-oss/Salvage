// Assets/Scripts/Managers/SaveLoadSystem.cs
using UnityEngine;

// �浵���ݽṹ
[System.Serializable]
public class SaveData
{
    public int currentLevel; // 0: Main Menu, 1: Level1, 2: Level2
    public string currentSceneName;
    // ������������Ҫ������������ݣ��米����Ʒ������״̬��
    // public InventoryData inventory;
}

public class SaveLoadSystem : MonoBehaviour
{
    public static SaveLoadSystem Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[SaveLoadSystem] Duplicate SaveLoadSystem detected on {gameObject.name}! Destroying this component only.");
            Destroy(this);  // 只销毁组件，不销毁整个 GameObject
            return;
        }
        Instance = this;
        Debug.Log("[SaveLoadSystem] Instance has been set.");

        // GameManager 已经在同一个 GameObject 上调用了 DontDestroyOnLoad
        // 不需要重复调用
    }

    public void SaveGame()
    {
        Debug.Log("SaveLoadSystem: Saving game...");
        SaveData data = new SaveData
        {
            currentLevel = (int)GameManager.Instance.CurrentGameState,
            currentSceneName = SceneController.Instance.GetCurrentSceneName()
        };
        // TODO: ʵ�ֽ� data �������л�Ϊ JSON ������Ʋ�д���ļ����߼�
        // PlayerPrefs.SetString("SaveData", JsonUtility.ToJson(data));
    }

    public SaveData LoadGame()
    {
        Debug.Log("SaveLoadSystem: Loading game...");
        // TODO: ʵ�ִ��ļ���ȡ���ݲ������л�Ϊ SaveData ������߼�
        // if (PlayerPrefs.HasKey("SaveData"))
        // {
        //     return JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString("SaveData"));
        // }
        return null; // ��ʱ���� null����ʾû�д浵
    }

    public void DeleteSaveData()
    {
        Debug.Log("SaveLoadSystem: Deleting save data.");
        // TODO: ʵ��ɾ���浵�ļ����߼�
        // PlayerPrefs.DeleteKey("SaveData");
    }
}