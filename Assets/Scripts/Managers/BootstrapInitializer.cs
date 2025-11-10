using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// ����ű�ֻ������ Bootstrap ���������������й�������ʼ����ɺ�
/// �Զ����ز��������˵�������
/// </summary>
public class BootstrapInitializer : MonoBehaviour
{
    [Header("������������")]
    [Tooltip("Bootstrap ��ɺ�Ҫ���صĵ�һ������")]
    [SerializeField] private string firstSceneToLoad = "LandingPage";

    [Tooltip("�ڼ��س���ǰ�ȴ���������ȷ�����й��������ѳ�ʼ��")]
    [SerializeField] private float waitTimeBeforeLoad = 0.5f;

    private void Start()
    {
        // If BootstrapLoader exists in the scene, let it handle initialization
        if (FindObjectOfType<BootstrapLoader>() != null)
        {
            Debug.Log("[BootstrapInitializer] BootstrapLoader detected. Skipping initialization.");
            return;
        }

        // ����Э�������س���
        StartCoroutine(LoadFirstSceneAfterDelay());
    }

    private IEnumerator LoadFirstSceneAfterDelay()
    {
        Debug.Log("[Bootstrap] Waiting for managers to initialize...");

        // �ȴ�ָ����ʱ��
        yield return new WaitForSeconds(waitTimeBeforeLoad);

        Debug.Log($"[Bootstrap] Loading scene: {firstSceneToLoad}");

        // ʹ�� SceneController �����س������������Ա����߼�ͳһ
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadScene(firstSceneToLoad);
        }
        else
        {
            // ��� SceneController �����ã���ֱ�Ӽ���
            Debug.LogWarning("[Bootstrap] SceneController.Instance not found. Loading scene directly.");
            SceneManager.LoadScene(firstSceneToLoad);
        }
    }
}