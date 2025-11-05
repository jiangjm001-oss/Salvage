// Assets/Scripts/Managers/BootstrapLoader.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class BootstrapLoader : MonoBehaviour
{
    [Header("管理器预制体")]
    public GameObject managersPrefab; // 在Inspector中分配_Managers_Prefab

    [Header("调试选项")]
    [SerializeField] private int maxRetries = 10; // 最大重试次数
    [SerializeField] private float retryDelay = 0.1f; // 每次重试延迟（秒）

    void Start()
    {
        // 使用协程确保管理器完全初始化后再加载场景
        StartCoroutine(InitializeAndLoadScene());
    }

    private IEnumerator InitializeAndLoadScene()
    {
        Debug.Log("=== Bootstrap Initialization Started ===");

        // 实例化管理器预制体
        if (managersPrefab == null)
        {
            Debug.LogError("Bootstrap: Managers Prefab is not assigned in Inspector!");
            yield break;
        }

        GameObject managersObject = Instantiate(managersPrefab);
        managersObject.name = "_Managers"; // 重命名以便调试
        Debug.Log($"Bootstrap: Managers Prefab instantiated: {managersObject.name}");

        // 等待多帧，确保所有 Awake() 和 DontDestroyOnLoad 都处理完毕
        yield return null;
        yield return null;
        yield return null;

        // 使用重试机制验证管理器初始化
        bool managersReady = false;
        for (int i = 0; i < maxRetries; i++)
        {
            Debug.Log($"Bootstrap: Checking managers (attempt {i + 1}/{maxRetries})...");
            Debug.Log($"  - GameManager.Instance: {GameManager.Instance != null}");
            Debug.Log($"  - UIManager.Instance: {UIManager.Instance != null}");
            Debug.Log($"  - SceneController.Instance: {SceneController.Instance != null}");
            Debug.Log($"  - InventorySystem.Instance: {InventorySystem.Instance != null}");

            if (GameManager.Instance != null &&
                UIManager.Instance != null &&
                SceneController.Instance != null)
            {
                managersReady = true;
                Debug.Log($"Bootstrap: All critical managers initialized successfully on attempt {i + 1}!");
                break;
            }

            // 等待一小段时间后重试
            yield return new WaitForSeconds(retryDelay);
        }

        if (!managersReady)
        {
            Debug.LogError("Bootstrap: CRITICAL ERROR - Managers failed to initialize after all retries!");
            Debug.LogError($"  - GameManager.Instance: {GameManager.Instance}");
            Debug.LogError($"  - UIManager.Instance: {UIManager.Instance}");
            Debug.LogError($"  - SceneController.Instance: {SceneController.Instance}");
            yield break;
        }

        // 再等待一帧，确保 Start() 也执行完毕
        yield return null;

        Debug.Log("Bootstrap: Loading LandingPage scene...");
        Debug.Log("=== Bootstrap Initialization Complete ===");

        // 使用 SceneController 而不是直接调用 SceneManager
        SceneManager.LoadScene("LandingPage");
    }
}