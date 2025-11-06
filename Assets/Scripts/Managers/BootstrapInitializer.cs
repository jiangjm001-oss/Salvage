using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 这个脚本只存在于 Bootstrap 场景，负责在所有管理器初始化完成后，
/// 自动加载并进入主菜单场景。
/// </summary>
public class BootstrapInitializer : MonoBehaviour
{
    [Header("场景加载设置")]
    [Tooltip("Bootstrap 完成后要加载的第一个场景")]
    [SerializeField] private string firstSceneToLoad = "LandingPage";

    [Tooltip("在加载场景前等待的秒数，确保所有管理器都已初始化")]
    [SerializeField] private float waitTimeBeforeLoad = 0.5f;

    private void Start()
    {
        // 启动协程来加载场景
        StartCoroutine(LoadFirstSceneAfterDelay());
    }

    private IEnumerator LoadFirstSceneAfterDelay()
    {
        Debug.Log("[Bootstrap] Waiting for managers to initialize...");

        // 等待指定的时间
        yield return new WaitForSeconds(waitTimeBeforeLoad);

        Debug.Log($"[Bootstrap] Loading scene: {firstSceneToLoad}");

        // 使用 SceneController 来加载场景，这样可以保持逻辑统一
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene(firstSceneToLoad);
        }
        else
        {
            // 如果 SceneController 不可用，则直接加载
            Debug.LogWarning("[Bootstrap] SceneController.Instance not found. Loading scene directly.");
            SceneManager.LoadScene(firstSceneToLoad);
        }
    }
}