// Assets/Scripts/GamePlay/FilterPaperController.cs
using UnityEngine;

/// <summary>
/// 滤纸控制器 - 简单单向切换
/// 点击滤纸A → 滤纸A消失，滤纸B出现
/// </summary>
public class FilterPaperController : MonoBehaviour
{
    [Header("基本信息")]
    public string objectID = "filter_paper_a";
    public string displayName = "滤纸";

    [Header("切换设置")]
    [Tooltip("点击后显示的物体（滤纸B）")]
    public GameObject nextObject;

    [Header("音效设置")]
    public string switchSoundName = "";

    [HideInInspector]
    public bool hasBeenUsed = false;

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Interact();
    }

    public void Interact()
    {
        if (hasBeenUsed) return;

        Debug.Log($"[FilterPaperController] 点击 {displayName}");

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(switchSoundName))
        {
            AudioManager.Instance.PlaySFX(switchSoundName);
        }

        // 显示下一个物体
        if (nextObject != null)
        {
            nextObject.SetActive(true);
            Debug.Log($"[FilterPaperController] 显示: {nextObject.name}");
        }

        // 标记并隐藏自己
        hasBeenUsed = true;
        gameObject.SetActive(false);

        SaveLoadSystem.Instance?.SaveGame();
    }

    public void RestoreState(bool used)
    {
        hasBeenUsed = used;
        if (hasBeenUsed)
        {
            gameObject.SetActive(false);
        }
    }
}