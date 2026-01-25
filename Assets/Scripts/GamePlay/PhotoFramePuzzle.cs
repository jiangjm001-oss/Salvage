// Assets/Scripts/GamePlay/PhotoFramePuzzle.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 照片拼图谜题
/// 放在相框放大视图中，玩家使用碎片物品放置并拖动到正确位置
/// </summary>
public class PhotoFramePuzzle : MonoBehaviour
{
    [System.Serializable]
    public class FragmentSlot
    {
        [Tooltip("碎片物品ID（如 note1）")]
        public string fragmentItemID;

        [Tooltip("碎片精灵图")]
        public Sprite fragmentSprite;

        [Tooltip("正确位置（相对于拼图中心）")]
        public Vector2 targetPosition;

        [HideInInspector]
        public bool isPlaced = false;  // 是否已放置到场景

        [HideInInspector]
        public bool isSnapped = false; // 是否已吸附到正确位置
    }

    [Header("碎片配置")]
    [Tooltip("配置4个碎片槽位")]
    public FragmentSlot[] fragments = new FragmentSlot[4];

    [Header("放置设置")]
    [Tooltip("碎片初始放置位置（相对于拼图中心）")]
    public Vector2 initialPlaceOffset = new Vector2(0, -1f);

    [Tooltip("初始放置随机范围")]
    public float randomRange = 0.5f;

    [Tooltip("碎片缩放比例（调整碎片显示大小）")]
    public float fragmentScale = 1f;

    [Header("吸附设置")]
    [Tooltip("吸附距离阈值")]
    public float snapDistance = 0.5f;

    [Tooltip("吸附动画时间")]
    public float snapDuration = 0.2f;

    [Header("完成后拾取")]
    [Tooltip("完成后可拾取的完整相片")]
    public ItemData completePhotoItem;

    [Tooltip("完成后显示的精灵图（可选）")]
    public Sprite completePhotoSprite;

    [Header("音效")]
    public string placeSound = "Audio/SFX/item_place";
    public string snapSound = "Audio/SFX/puzzle_snap";
    public string completeSound = "Audio/SFX/puzzle_complete";
    public string pickupSound = "Audio/SFX/item_pickup";

    [Header("事件")]
    public UnityEvent OnPuzzleCompleted;
    public UnityEvent OnPhotoPickedUp;

    [Header("存档")]
    public string puzzleID = "photo_frame_puzzle_01";

    // 内部状态
    private List<DraggableFragment> spawnedFragments = new List<DraggableFragment>();
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private bool isPuzzleComplete = false;
    private bool isPhotoPickedUp = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
    }

    private void OnEnable()
    {
        // 每次进入放大视图时恢复状态
        RestoreFromSave();
    }

    private void OnDisable()
    {
        // 离开时保存
        SaveState();
    }

    private void OnMouseDown()
    {
        // 如果已完成且未拾取，点击拾取
        if (isPuzzleComplete && !isPhotoPickedUp)
        {
            PickupCompletePhoto();
            return;
        }

        // 如果未完成，尝试放置碎片
        if (!isPuzzleComplete)
        {
            TryPlaceFragment();
        }
    }

    /// <summary>
    /// 尝试放置选中的碎片
    /// </summary>
    private void TryPlaceFragment()
    {
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log("[PhotoFramePuzzle] 没有选中物品");
            return;
        }

        // 检查是否是碎片物品
        int fragmentIndex = FindFragmentIndex(selectedItem.itemID);
        if (fragmentIndex < 0)
        {
            Debug.Log($"[PhotoFramePuzzle] '{selectedItem.itemID}' 不是碎片物品");
            return;
        }

        // 检查是否已放置
        if (fragments[fragmentIndex].isPlaced)
        {
            Debug.Log($"[PhotoFramePuzzle] 碎片 '{selectedItem.itemID}' 已经放置过了");
            return;
        }

        // 放置碎片
        PlaceFragment(fragmentIndex);

        // 消耗物品
        UIManager.Instance.ConsumeSelectedItem();

        // 播放音效
        AudioManager.Instance?.PlaySFX(placeSound);
    }

    /// <summary>
    /// 查找碎片索引
    /// </summary>
    private int FindFragmentIndex(string itemID)
    {
        for (int i = 0; i < fragments.Length; i++)
        {
            if (fragments[i].fragmentItemID == itemID)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 放置碎片到场景
    /// </summary>
    private void PlaceFragment(int index)
    {
        FragmentSlot slot = fragments[index];
        slot.isPlaced = true;

        // 计算初始位置（带随机偏移）
        Vector2 randomOffset = new Vector2(
            Random.Range(-randomRange, randomRange),
            Random.Range(-randomRange, randomRange)
        );
        Vector3 spawnPos = transform.position +
                          (Vector3)initialPlaceOffset +
                          (Vector3)randomOffset;

        // 创建碎片对象
        GameObject fragmentObj = new GameObject($"Fragment_{slot.fragmentItemID}");
        fragmentObj.transform.SetParent(transform);
        fragmentObj.transform.position = spawnPos;
        fragmentObj.transform.localScale = Vector3.one * fragmentScale;  // 设置缩放
        fragmentObj.layer = gameObject.layer;

        // 添加 SpriteRenderer
        SpriteRenderer sr = fragmentObj.AddComponent<SpriteRenderer>();
        sr.sprite = slot.fragmentSprite;
        sr.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        sr.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) + 1 + index;

        // 添加 Collider
        BoxCollider2D col = fragmentObj.AddComponent<BoxCollider2D>();
        col.size = sr.bounds.size / fragmentScale;  // 根据缩放调整碰撞体大小

        // 添加拖动组件
        DraggableFragment draggable = fragmentObj.AddComponent<DraggableFragment>();
        draggable.Initialize(this, index, (Vector2)transform.position + slot.targetPosition);

        spawnedFragments.Add(draggable);

        Debug.Log($"[PhotoFramePuzzle] 放置碎片: {slot.fragmentItemID}, Scale: {fragmentScale}");
    }

    /// <summary>
    /// 碎片吸附时调用（由 DraggableFragment 调用）
    /// </summary>
    public void OnFragmentSnapped(int fragmentIndex)
    {
        fragments[fragmentIndex].isSnapped = true;

        // 播放吸附音效
        AudioManager.Instance?.PlaySFX(snapSound);

        Debug.Log($"[PhotoFramePuzzle] 碎片 {fragmentIndex} 已吸附");

        // 检查是否全部完成
        CheckPuzzleComplete();
    }

    /// <summary>
    /// 检查是否全部完成
    /// </summary>
    private void CheckPuzzleComplete()
    {
        foreach (var slot in fragments)
        {
            if (!slot.isSnapped)
            {
                return; // 还有未完成的
            }
        }

        // 全部完成！
        isPuzzleComplete = true;
        Debug.Log("[PhotoFramePuzzle] ✓ 拼图完成！");

        // 播放完成音效
        AudioManager.Instance?.PlaySFX(completeSound);

        // 显示完整图片（可选）
        if (completePhotoSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = completePhotoSprite;
        }

        // 触发事件
        OnPuzzleCompleted?.Invoke();

        // 保存状态
        SaveState();
    }

    /// <summary>
    /// 拾取完整相片
    /// </summary>
    private void PickupCompletePhoto()
    {
        if (completePhotoItem == null)
        {
            Debug.LogWarning("[PhotoFramePuzzle] 未配置完整相片物品");
            return;
        }

        bool added = InventorySystem.Instance.AddItem(completePhotoItem);
        if (added)
        {
            isPhotoPickedUp = true;

            // 播放音效
            AudioManager.Instance?.PlaySFX(pickupSound);

            // 隐藏所有碎片
            foreach (var frag in spawnedFragments)
            {
                if (frag != null)
                {
                    frag.gameObject.SetActive(false);
                }
            }

            Debug.Log($"[PhotoFramePuzzle] 拾取完整相片: {completePhotoItem.displayName}");

            OnPhotoPickedUp?.Invoke();
            SaveState();
        }
    }

    // ============ 存档相关 ============

    private void SaveState()
    {
        string key = $"PhotoPuzzle_{puzzleID}";

        // 格式: placed0,snapped0,placed1,snapped1,...,complete,pickedup
        List<string> parts = new List<string>();
        foreach (var slot in fragments)
        {
            parts.Add(slot.isPlaced ? "1" : "0");
            parts.Add(slot.isSnapped ? "1" : "0");
        }
        parts.Add(isPuzzleComplete ? "1" : "0");
        parts.Add(isPhotoPickedUp ? "1" : "0");

        PlayerPrefs.SetString(key, string.Join(",", parts));
        PlayerPrefs.Save();

        Debug.Log($"[PhotoFramePuzzle] 状态已保存: {string.Join(",", parts)}");
    }

    private void RestoreFromSave()
    {
        string key = $"PhotoPuzzle_{puzzleID}";
        if (!PlayerPrefs.HasKey(key)) return;

        string data = PlayerPrefs.GetString(key);
        string[] parts = data.Split(',');

        if (parts.Length < fragments.Length * 2 + 2) return;

        // 清除之前的碎片对象
        foreach (var frag in spawnedFragments)
        {
            if (frag != null)
            {
                Destroy(frag.gameObject);
            }
        }
        spawnedFragments.Clear();

        // 恢复碎片状态
        for (int i = 0; i < fragments.Length; i++)
        {
            fragments[i].isPlaced = parts[i * 2] == "1";
            fragments[i].isSnapped = parts[i * 2 + 1] == "1";

            // 如果已放置，重新生成碎片对象
            if (fragments[i].isPlaced)
            {
                RestoreFragment(i, fragments[i].isSnapped);
            }
        }

        int baseIndex = fragments.Length * 2;
        isPuzzleComplete = parts[baseIndex] == "1";
        isPhotoPickedUp = parts[baseIndex + 1] == "1";

        // 如果已拾取，隐藏所有碎片
        if (isPhotoPickedUp)
        {
            foreach (var frag in spawnedFragments)
            {
                if (frag != null) frag.gameObject.SetActive(false);
            }
        }

        Debug.Log($"[PhotoFramePuzzle] 状态已恢复: complete={isPuzzleComplete}, pickedUp={isPhotoPickedUp}");
    }

    private void RestoreFragment(int index, bool snapped)
    {
        FragmentSlot slot = fragments[index];

        // 计算位置
        Vector3 pos = snapped
            ? transform.position + (Vector3)slot.targetPosition
            : transform.position + (Vector3)initialPlaceOffset;

        // 创建碎片对象
        GameObject fragmentObj = new GameObject($"Fragment_{slot.fragmentItemID}");
        fragmentObj.transform.SetParent(transform);
        fragmentObj.transform.position = pos;
        fragmentObj.transform.localScale = Vector3.one * fragmentScale;  // 设置缩放
        fragmentObj.layer = gameObject.layer;

        SpriteRenderer sr = fragmentObj.AddComponent<SpriteRenderer>();
        sr.sprite = slot.fragmentSprite;
        sr.sortingLayerID = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
        sr.sortingOrder = (spriteRenderer != null ? spriteRenderer.sortingOrder : 0) + 1 + index;

        BoxCollider2D col = fragmentObj.AddComponent<BoxCollider2D>();
        col.size = sr.bounds.size / fragmentScale;  // 根据缩放调整碰撞体大小

        DraggableFragment draggable = fragmentObj.AddComponent<DraggableFragment>();
        draggable.Initialize(this, index, (Vector2)transform.position + slot.targetPosition);

        if (snapped)
        {
            draggable.ForceSnap(); // 直接锁定
        }

        spawnedFragments.Add(draggable);
    }

    /// <summary>
    /// 重置谜题（用于调试）
    /// </summary>
    [ContextMenu("Reset Puzzle")]
    public void ResetPuzzle()
    {
        // 清除存档
        string key = $"PhotoPuzzle_{puzzleID}";
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();

        // 清除碎片对象
        foreach (var frag in spawnedFragments)
        {
            if (frag != null)
            {
                DestroyImmediate(frag.gameObject);
            }
        }
        spawnedFragments.Clear();

        // 重置状态
        foreach (var slot in fragments)
        {
            slot.isPlaced = false;
            slot.isSnapped = false;
        }
        isPuzzleComplete = false;
        isPhotoPickedUp = false;

        Debug.Log("[PhotoFramePuzzle] 谜题已重置");
    }

    // ============ 编辑器辅助 ============

    private void OnDrawGizmosSelected()
    {
        // 绘制目标位置
        Gizmos.color = Color.green;
        foreach (var slot in fragments)
        {
            if (slot != null)
            {
                Vector3 targetPos = transform.position + (Vector3)slot.targetPosition;
                Gizmos.DrawWireSphere(targetPos, snapDistance);
                Gizmos.DrawLine(transform.position, targetPos);
            }
        }

        // 绘制初始放置区域
        Gizmos.color = Color.yellow;
        Vector3 placeCenter = transform.position + (Vector3)initialPlaceOffset;
        Gizmos.DrawWireCube(placeCenter, new Vector3(randomRange * 2, randomRange * 2, 0));
    }
}