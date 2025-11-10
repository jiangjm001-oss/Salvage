using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 诊断脚本 - 检查 GameManager 初始化问题
/// 将此脚本添加到 Bootstrap 场景中的任何 GameObject 上
/// </summary>
public class ManagersDiagnostic : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("=== [ManagersDiagnostic] Starting diagnostic ===");
        Debug.Log($"[ManagersDiagnostic] Current scene: {SceneManager.GetActiveScene().name}");
    }

    private void Start()
    {
        Debug.Log("=== [ManagersDiagnostic] Checking managers ===");

        // 查找所有 GameManager 实例
        GameManager[] gameManagers = FindObjectsOfType<GameManager>(true);
        Debug.Log($"[ManagersDiagnostic] Found {gameManagers.Length} GameManager(s) in scene");

        for (int i = 0; i < gameManagers.Length; i++)
        {
            var gm = gameManagers[i];
            Debug.Log($"[ManagersDiagnostic] GameManager {i}:");
            Debug.Log($"  - GameObject: {gm.gameObject.name}");
            Debug.Log($"  - Active: {gm.gameObject.activeInHierarchy}");
            Debug.Log($"  - Enabled: {gm.enabled}");
            Debug.Log($"  - Is Instance: {GameManager.Instance == gm}");
        }

        // 检查 GameManager.Instance
        Debug.Log($"[ManagersDiagnostic] GameManager.Instance is null: {GameManager.Instance == null}");

        // 查找所有其他管理器
        AudioManager[] audioManagers = FindObjectsOfType<AudioManager>(true);
        Debug.Log($"[ManagersDiagnostic] Found {audioManagers.Length} AudioManager(s)");

        UIManager[] uiManagers = FindObjectsOfType<UIManager>(true);
        Debug.Log($"[ManagersDiagnostic] Found {uiManagers.Length} UIManager(s)");

        SettingsManager[] settingsManagers = FindObjectsOfType<SettingsManager>(true);
        Debug.Log($"[ManagersDiagnostic] Found {settingsManagers.Length} SettingsManager(s)");

        // 查找 _Managers_Prefab
        GameObject[] allObjects = FindObjectsOfType<GameObject>(true);
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Managers"))
            {
                Debug.Log($"[ManagersDiagnostic] Found GameObject: {obj.name}");
                Debug.Log($"  - Active: {obj.activeInHierarchy}");
                Debug.Log($"  - Components: {obj.GetComponents<Component>().Length}");

                var components = obj.GetComponents<MonoBehaviour>();
                foreach (var comp in components)
                {
                    if (comp != null)
                    {
                        Debug.Log($"    - Component: {comp.GetType().Name} (Enabled: {comp.enabled})");
                    }
                }
            }
        }

        Debug.Log("=== [ManagersDiagnostic] Diagnostic complete ===");
    }
}
