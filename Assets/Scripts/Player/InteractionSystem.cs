// Assets/Scripts/Player/InteractionSystem.cs
// 修复版 - 支持重叠碰撞体检测，优先响应 Pickup 类型
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

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
        // 先检查鼠标是否在UI上
        if (IsPointerOverUI())
        {
            Debug.Log("InteractionSystem: Click was on UI, ignoring scene interaction.");
            return;
        }

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // ⭐ 关键修改：使用 RaycastAll 检测所有重叠的碰撞体
        RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, Vector2.zero, Mathf.Infinity, interactableLayer);

        if (hits.Length == 0)
        {
            Debug.Log("InteractionSystem: Raycast did not hit anything on the Interactable layer.");
            return;
        }

        // 找到最优先的交互对象
        InteractableObject bestTarget = FindBestInteractable(hits);

        if (bestTarget != null)
        {
            Debug.Log($"InteractionSystem: Interacting with {bestTarget.displayName} (Type: {bestTarget.interactionType})");
            bestTarget.Interact();
        }
    }

    /// <summary>
    /// 从多个命中物体中选择最合适的交互对象
    /// 优先级：Pickup > 其他类型 > Sorting Order 更高
    /// </summary>
    private InteractableObject FindBestInteractable(RaycastHit2D[] hits)
    {
        List<InteractableObject> candidates = new List<InteractableObject>();

        // 收集所有有效的 InteractableObject
        foreach (var hit in hits)
        {
            InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
            if (interactable != null && interactable.gameObject.activeInHierarchy)
            {
                candidates.Add(interactable);
            }
        }

        if (candidates.Count == 0) return null;
        if (candidates.Count == 1) return candidates[0];

        // ⭐ 优先级排序
        // 1. Pickup 类型优先（确保可拾取物品能被点击）
        // 2. 同类型时，Sorting Order 更高的优先（更靠近玩家视角）
        candidates.Sort((a, b) =>
        {
            // 优先级1：Pickup 类型最高优先
            bool aIsPickup = a.interactionType == InteractableObject.InteractionType.Pickup && a.isPickupable;
            bool bIsPickup = b.interactionType == InteractableObject.InteractionType.Pickup && b.isPickupable;

            if (aIsPickup && !bIsPickup) return -1;  // a 优先
            if (!aIsPickup && bIsPickup) return 1;   // b 优先

            // 优先级2：Sorting Order 更高的优先
            SpriteRenderer srA = a.GetComponent<SpriteRenderer>();
            SpriteRenderer srB = b.GetComponent<SpriteRenderer>();

            int orderA = srA != null ? srA.sortingOrder : 0;
            int orderB = srB != null ? srB.sortingOrder : 0;

            if (orderA != orderB) return orderB.CompareTo(orderA); // 降序

            // 优先级3：Z 值更小的优先（更靠近相机）
            return a.transform.position.z.CompareTo(b.transform.position.z);
        });

        Debug.Log($"InteractionSystem: Found {candidates.Count} overlapping objects, selected: {candidates[0].displayName}");
        return candidates[0];
    }

    /// <summary>
    /// 检查鼠标是否在UI上
    /// </summary>
    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}