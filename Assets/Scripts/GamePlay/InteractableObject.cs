// Assets/_Scripts/Gameplay/InteractableObject.cs
using UnityEngine;

/// <summary>
/// �ɽ����������
/// ֧�����ֽ������ͣ�ʰȡ��Ʒ���Ŵ�鿴�������¼�
/// </summary>
public class InteractableObject : MonoBehaviour
{
    [Header("���������Ϣ")]
    [Tooltip("�����Ψһ��ʶ��")]
    public string objectID;

    [Tooltip("�������ʾ����")]
    public string displayName;

    [Header("������������")]
    [Tooltip("ѡ�������Ľ�������")]
    public InteractionType interactionType = InteractionType.Pickup;

    [Header("ʰȡ��Ʒ���� (Pickup)")]
    [Tooltip("�������������Ʒ���ݣ�������������ΪPickupʱʹ�ã�")]
    public ItemData item;

    [Tooltip("�Ƿ���Ա�ʰȡ��������������ΪPickupʱʹ�ã�")]
    public bool isPickupable = true;

    [Header("�Ŵ���ͼ���� (ZoomView)")]
    [Tooltip("ѡ�������������ķŴ���ͼ��������������ΪZoomViewʱʹ�ã�")]
    public GameManager.ViewState associatedZoomView;

    [Header("��Ч���ã���ѡ��")]
    [Tooltip("ʰȡ��Ʒʱ���ŵ���Ч����")]
    public string pickupSoundName = "Audio/SFX/item_pickup";

    [Tooltip("����Ŵ���ͼʱ���ŵ���Ч����")]
    public string zoomSoundName = "Audio/SFX/zoom_in";

    [Tooltip("�����¼�ʱ���ŵ���Ч����")]
    public string triggerSoundName = "Audio/SFX/trigger";

    [Header("�����¼����� (Trigger)")]
    [Tooltip("�������Ƿ���ô�����")]
    public bool disableAfterTrigger = false;

    /// <summary>
    /// ��������ö��
    /// </summary>
    public enum InteractionType
    {
        Pickup,      // ʰȡ��Ʒ������
        ZoomView,    // �Ŵ�鿴
        Trigger      // �����¼�/����
    }

    /// <summary>
    /// ���������� - ��InteractionSystem����
    /// </summary>
    public void Interact()
    {
        Debug.Log($"[InteractableObject] ��������� '{displayName}' (ID: {objectID}) �����˽�������������: {interactionType}");

        // ���ݽ������ͷַ�����ͬ�Ĵ�������
        switch (interactionType)
        {
            case InteractionType.Pickup:
                HandlePickup();
                break;

            case InteractionType.ZoomView:
                // Ӧ�õ��� GameManager �ķ���
                GameManager.Instance.EnterZoomView(associatedZoomView);
                break;

            case InteractionType.Trigger:
                HandleTrigger();
                break;

            default:
                Debug.LogWarning($"[InteractableObject] δ֪�Ľ�������: {interactionType}");
                break;
        }
    }

    /// <summary>
    /// ����ʰȡ��Ʒ����
    /// </summary>
    private void HandlePickup()
    {
        if (!isPickupable)
        {
            Debug.Log($"[InteractableObject] ���� '{displayName}' �޷���ʰȡ��isPickupable = false����");
            return;
        }

        if (item == null)
        {
            Debug.LogError($"[InteractableObject] ���� '{displayName}' û�з���ItemData������Inspector�����á�");
            return;
        }

        // ���ñ���ϵͳ������Ʒ
        InventorySystem.Instance.AddItem(item);

        Debug.Log($"[InteractableObject] �ɹ�ʰȡ��Ʒ: {item.displayName}");

        // ����ʰȡ��Ч
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSoundName))
        {
            AudioManager.Instance.PlaySFX(pickupSoundName);
        }

        // �ӳ������Ƴ��������
        gameObject.SetActive(false);
        // ����ʹ�� Destroy(gameObject); �������Ҫ�ٴμ���
    }

    /// <summary>
    /// �����Ŵ���ͼ����
    /// </summary>
    private void HandleZoomView()
    {
        // ��֤�Ƿ�����Ч�ķŴ���ͼö��ֵ
        string viewStateName = associatedZoomView.ToString();

        if (!viewStateName.Contains("Zoom"))
        {
            Debug.LogError($"[InteractableObject] ���� '{displayName}' ��Associated Zoom View���ô���" +
                          $"��ǰֵ: {associatedZoomView}������ѡ�����'Zoom'����ͼ״̬��");
            return;
        }

        // ���GameManager�Ƿ����
        if (GameManager.Instance == null)
        {
            Debug.LogError("[InteractableObject] GameManager�����ڣ��޷��л���ͼ��");
            return;
        }

        // �л����Ŵ���ͼ
        Debug.Log($"[InteractableObject] ����Ŵ���ͼ: {associatedZoomView}");
        GameManager.Instance.EnterZoomView(associatedZoomView);

        // ���ŷŴ���Ч
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(zoomSoundName))
        {
            AudioManager.Instance.PlaySFX(zoomSoundName);
        }
    }

    /// <summary>
    /// ���������¼�����
    /// </summary>
    private void HandleTrigger()
    {
        Debug.Log($"[InteractableObject] ����������: {displayName} (ID: {objectID})");

        // ���Ŵ�����Ч
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(triggerSoundName))
        {
            AudioManager.Instance.PlaySFX(triggerSoundName);
        }

        // �����Զ����¼�����������������д��ͨ��UnityEvent��չ��
        OnTriggered();

        // ��������˴�������ã�����ô�����
        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// �����¼����鷽����������������д��ʵ���ض��߼�
    /// ���磺���š�������ء����������
    /// </summary>
    protected virtual void OnTriggered()
    {
        // ����Ĭ��ʵ��Ϊ��
        // ���������д���������ʵ���ض��Ĵ����߼�

        // ʾ���������PuzzleManager������֪ͨ��
        // if (PuzzleManager.Instance != null)
        // {
        //     PuzzleManager.Instance.OnObjectTriggered(objectID);
        // }
    }

    /// <summary>
    /// �༭����������Scene��ͼ����ʾ������Ϣ
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // ���ݽ���������ʾ��ͬ��ɫ��Gizmo
        switch (interactionType)
        {
            case InteractionType.Pickup:
                Gizmos.color = Color.green;
                break;
            case InteractionType.ZoomView:
                Gizmos.color = Color.blue;
                break;
            case InteractionType.Trigger:
                Gizmos.color = Color.yellow;
                break;
        }

        // ������λ�û���һ��С��
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }

    /// <summary>
    /// �༭����������֤����
    /// </summary>
    private void OnValidate()
    {
        // �ڱ༭����ʵʱ��֤����
        if (interactionType == InteractionType.Pickup && item == null)
        {
            Debug.LogWarning($"[InteractableObject] ���� '{gameObject.name}' �Ľ�������ΪPickup����û������ItemData��", this);
        }

        if (interactionType == InteractionType.ZoomView)
        {
            string viewStateName = associatedZoomView.ToString();
            if (!viewStateName.Contains("Zoom"))
            {
                Debug.LogWarning($"[InteractableObject] ���� '{gameObject.name}' �Ľ�������ΪZoomView����Associated Zoom View���ÿ��ܲ���ȷ����ǰ: {associatedZoomView}����", this);
            }
        }
    }
}