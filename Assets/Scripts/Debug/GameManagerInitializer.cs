using UnityEngine;

/// <summary>
/// 强制确保 GameManager 在 Awake 时正确初始化
/// 此脚本执行顺序应该在 GameManager 之前
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManagerInitializer : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[GameManagerInitializer] Checking GameManager...");

        // 查找 GameManager 组件
        GameManager gameManager = GetComponent<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("[GameManagerInitializer] GameManager component not found on this GameObject!");

            // 尝试在子对象中查找
            gameManager = GetComponentInChildren<GameManager>(true);
            if (gameManager != null)
            {
                Debug.LogWarning($"[GameManagerInitializer] GameManager found in child: {gameManager.gameObject.name}");
            }
        }
        else
        {
            Debug.Log("[GameManagerInitializer] GameManager component found!");
            Debug.Log($"[GameManagerInitializer] GameManager enabled: {gameManager.enabled}");
            Debug.Log($"[GameManagerInitializer] GameObject active: {gameManager.gameObject.activeInHierarchy}");

            // 强制启用
            if (!gameManager.enabled)
            {
                Debug.LogWarning("[GameManagerInitializer] GameManager was disabled! Enabling it now...");
                gameManager.enabled = true;
            }

            if (!gameManager.gameObject.activeInHierarchy)
            {
                Debug.LogError("[GameManagerInitializer] GameManager GameObject is inactive!");
            }
        }
    }

    private void Start()
    {
        Debug.Log($"[GameManagerInitializer] Start() - GameManager.Instance is null: {GameManager.Instance == null}");

        if (GameManager.Instance == null)
        {
            Debug.LogError("[GameManagerInitializer] CRITICAL: GameManager.Instance is still null after Awake!");

            // 尝试手动查找并修复
            GameManager[] allGameManagers = FindObjectsOfType<GameManager>(true);
            Debug.Log($"[GameManagerInitializer] Found {allGameManagers.Length} GameManager(s) in scene");

            foreach (var gm in allGameManagers)
            {
                Debug.Log($"[GameManagerInitializer] GameManager on: {gm.gameObject.name}");
                Debug.Log($"  - Active: {gm.gameObject.activeInHierarchy}");
                Debug.Log($"  - Enabled: {gm.enabled}");
            }
        }
        else
        {
            Debug.Log("[GameManagerInitializer] GameManager.Instance is correctly set!");
        }
    }
}
