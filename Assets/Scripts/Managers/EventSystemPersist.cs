// Assets/Scripts/Managers/EventSystemPersist.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemPersist : MonoBehaviour
{
    private void Awake()
    {
        // 检查是否已经有其他 EventSystem 存在
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

        if (eventSystems.Length > 1)
        {
            // 如果有多个，销毁这个（保留场景中原有的）
            Destroy(gameObject);
            return;
        }

        // 让这个 EventSystem 持久化
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        Debug.Log("EventSystemPersist: EventSystem is now persistent across scenes.");
    }
}