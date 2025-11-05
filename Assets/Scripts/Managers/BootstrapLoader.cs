// Assets/Scripts/Managers/BootstrapLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootstrapLoader : MonoBehaviour
{
    [Header("管理器预制体")]
    public GameObject managersPrefab; // 在Inspector中分配_Managers_Prefab

    void Start()
    {
        // 使用协程确保管理器完全初始化后再加载场景
        StartCoroutine(InitializeAndLoadScene());
    }

    private IEnumerator InitializeAndLoadScene()
    {
        // 实例化管理器预制体
        if (managersPrefab != null)
        {
            GameObject managersObject = Instantiate(managersPrefab);
            Debug.Log("Bootstrap: Managers Prefab instantiated.");
        }
        else
        {
            Debug.LogError("Bootstrap: Managers Prefab is not assigned!");
            yield break;
        }

        // 等待一帧，确保所有 Awake() 都执行完毕
        yield return null;

        // 验证关键管理器是否初始化成功
        if (GameManager.Instance == null)
        {
            Debug.LogError("Bootstrap: GameManager failed to initialize!");
            yield break;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("Bootstrap: UIManager failed to initialize!");
            yield break;
        }

        Debug.Log("Bootstrap: All managers initialized successfully.");
        Debug.Log($"Bootstrap: GameManager.Instance = {GameManager.Instance != null}");
        Debug.Log($"Bootstrap: UIManager.Instance = {UIManager.Instance != null}");

        // 再等待一帧，确保 Start() 也都执行完毕
        yield return null;

        Debug.Log("Bootstrap: Loading LandingPage scene...");

        // 加载着陆页面场景
        SceneManager.LoadScene("LandingPage");
    }
}