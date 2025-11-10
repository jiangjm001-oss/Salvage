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
        GameObject managersObject = null;

        // 实例化管理器预制体
        if (managersPrefab != null)
        {
            managersObject = Instantiate(managersPrefab);
            Debug.Log($"Bootstrap: Managers Prefab instantiated. GameObject name: {managersObject.name}");

            // 立即检查 Instance 状态（在 Awake 之后）
            Debug.Log($"Bootstrap: [Immediate] GameManager.Instance = {GameManager.Instance != null}");
            Debug.Log($"Bootstrap: [Immediate] UIManager.Instance = {UIManager.Instance != null}");

            // 显式确保 DontDestroyOnLoad
            if (managersObject != null)
            {
                DontDestroyOnLoad(managersObject);
                Debug.Log("Bootstrap: DontDestroyOnLoad called on Managers GameObject.");
            }
        }
        else
        {
            Debug.LogError("Bootstrap: Managers Prefab is not assigned!");
            yield break;
        }

        // 等待一帧，确保所有 Awake() 都执行完毕
        yield return null;

        // 检查 GameObject 是否还存在
        if (managersObject == null)
        {
            Debug.LogError("Bootstrap: Managers GameObject was destroyed!");
            yield break;
        }

        // 验证关键管理器是否初始化成功
        Debug.Log($"Bootstrap: [After yield] GameManager.Instance = {GameManager.Instance != null}");

        if (GameManager.Instance == null)
        {
            Debug.LogError("Bootstrap: GameManager failed to initialize!");

            // 诊断信息
            GameManager[] allGMs = FindObjectsOfType<GameManager>(true);
            Debug.LogError($"Bootstrap: Found {allGMs.Length} GameManager(s) in scene");
            yield break;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("Bootstrap: UIManager failed to initialize!");
            yield break;
        }

        Debug.Log("Bootstrap: All managers initialized successfully.");

        // 再等待一帧，确保 Start() 也都执行完毕
        yield return null;

        Debug.Log("Bootstrap: Loading LandingPage scene...");

        // 加载着陆页面场景
        SceneManager.LoadScene("LandingPage");
    }
}