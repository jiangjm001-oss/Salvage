// Assets/Scripts/Managers/AudioManager.cs
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // --- 确认这里有这两行，并且顺序正确 ---
        transform.SetParent(null); // 先把自己从父对象中解放出来
        DontDestroyOnLoad(gameObject); // 再设置为持久存在
    }


    public void PlayMusic(string musicName)
    {
        Debug.Log($"AudioManager: Playing music '{musicName}'.");
        // TODO: 实现根据 musicName 查找并播放 Audio Clip 的逻辑
    }

    public void PlaySFX(string sfxName)
    {
        Debug.Log($"AudioManager: Playing SFX '{sfxName}'.");
        // TODO: 实现播放音效的逻辑
    }
}