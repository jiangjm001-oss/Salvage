// Assets/Scripts/Managers/EventSystemPersist.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class EventSystemPersist : MonoBehaviour
{
    private void Awake()
    {
        // ����Ƿ��Ѿ������� EventSystem ����
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();

        if (eventSystems.Length > 1)
        {
            // ����ж�����������������������ԭ�еģ�
            Debug.LogWarning($"[EventSystemPersist] Multiple EventSystems detected! Destroying this component only.");
            Destroy(this);  // 只销毁组件，不销毁整个 GameObject
            return;
        }

        // GameManager 已经在同一个 GameObject 上调用了 DontDestroyOnLoad
        // 不需要重复调用

        Debug.Log("[EventSystemPersist] EventSystem is now persistent across scenes.");
    }
}