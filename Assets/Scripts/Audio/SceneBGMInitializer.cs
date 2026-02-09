// Assets/Scripts/Audio/SceneBGMInitializer.cs
// 场景 BGM 初始化器 - 挂载到每个场景的空物体上
// 在场景加载时自动播放指定 BGM

using UnityEngine;

public class SceneBGMInitializer : MonoBehaviour
{
    [Header("场景 BGM 设置")]
    [Tooltip("此场景的默认 BGM")]
    public AudioManager.BGMType sceneBGM = AudioManager.BGMType.None;

    [Tooltip("是否在 Start 时播放")]
    public bool playOnStart = true;

    [Tooltip("等待帧数后播放（确保 AudioManager 已初始化）")]
    [Range(0, 5)]
    public int delayFrames = 1;

    [Header("条件")]
    [Tooltip("仅当 BGM 不同时才切换（避免重复触发）")]
    public bool onlyIfDifferent = true;

    private void Start()
    {
        if (playOnStart)
        {
            StartCoroutine(InitializeBGMCoroutine());
        }
    }

    private System.Collections.IEnumerator InitializeBGMCoroutine()
    {
        // 等待指定帧数
        for (int i = 0; i < delayFrames; i++)
        {
            yield return null;
        }

        PlaySceneBGM();
    }

    /// <summary>
    /// 播放此场景的 BGM（可被外部调用）
    /// </summary>
    public void PlaySceneBGM()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("[SceneBGMInitializer] AudioManager not found!");
            return;
        }

        if (sceneBGM == AudioManager.BGMType.None)
        {
            Debug.Log("[SceneBGMInitializer] No BGM configured for this scene");
            return;
        }

        // 检查是否需要切换
        if (onlyIfDifferent)
        {
            string targetName = sceneBGM.ToString();
            string currentName = AudioManager.Instance.GetCurrentBGMName();

            if (currentName == targetName)
            {
                Debug.Log($"[SceneBGMInitializer] BGM already playing: {targetName}");
                return;
            }
        }

        AudioManager.Instance.PlayBGM(sceneBGM);
        Debug.Log($"[SceneBGMInitializer] Scene BGM started: {sceneBGM}");
    }
}