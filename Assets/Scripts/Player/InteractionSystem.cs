// Assets/Scripts/Player/InteractionSystem.cs
using UnityEngine;
using UnityEngine.EventSystems; // ✅ 重要：添加这个命名空间

public class InteractionSystem : MonoBehaviour
{
    public static InteractionSystem Instance { get; private set; }

    [SerializeField] private LayerMask interactableLayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PerformInteractionCheck();
        }
    }

    private void PerformInteractionCheck()
    {
        // ✅ 关键修改：先检查鼠标是否在UI上
        if (IsPointerOverUI())
        {
            Debug.Log("InteractionSystem: Click was on UI, ignoring scene interaction.");
            return; // 如果在UI上，直接返回，不检测场景物体
        }

        // 原有的射线检测逻辑
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, interactableLayer);

        if (hit.collider != null)
        {
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
            if (interactable != null)
            {
                Debug.Log($"InteractionSystem: Interacting with {interactable.displayName}");
                interactable.Interact();
            }
        }
        else
        {
            Debug.Log("InteractionSystem: Raycast did not hit anything on the Interactable layer.");
        }
    }

    // ✅ 新增：检查鼠标是否在UI上的方法
    private bool IsPointerOverUI()
    {
        // 检查当前鼠标位置是否在UI元素上
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}