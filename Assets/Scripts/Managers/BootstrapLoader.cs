// Assets/Scripts/Managers/BootstrapLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoader : MonoBehaviour
{
    [Header("核心预制体")]
    public GameObject managersPrefab; // 在Inspector中拖入_Managers_Prefab

    void Start()
    {
        // 实例化管理器预制体
        if (managersPrefab != null)
        {
            Instantiate(managersPrefab);
            Debug.Log("Bootstrap: Managers Prefab instantiated.");
        }
        else
        {
            Debug.LogError("Bootstrap: Managers Prefab is not assigned!");
        }

        // 立即加载着陆页场景
        SceneManager.LoadScene("LandingPage");
    }
}