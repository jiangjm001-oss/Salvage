// Assets/Scripts/GamePlay/PhotoPuzzleController.cs
// 照片拼图谜题控制器 - 管理碎片放置、拖动、完成检测
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 照片拼图谜题控制器
/// 放在相框放大视图中，管理4个碎片的放置和拼合
/// </summary>
public class PhotoPuzzleController : MonoBehaviour
{
    [Header("谜题设置")]
    [Tooltip("唯一标识符（用于存档）")]
    public string puzzleID = "photo_puzzle_01";

    [Tooltip("需要的碎片物品（按顺序：note1, note2, note3, note4）")]
    public ItemData[] requiredPieces = new ItemData[4];

    [Tooltip("碎片对应的精灵图（放置后显示）")]
    public Sprite[] pieceSprites = new Sprite[4];

    [Tooltip("碎片的正确位置（相对于此物体）")]
    public Vector2[] correctPositions = new Vector2[4];

    [Tooltip("吸附判定距离")]
    public float snapDistance = 0.5f;

    [Header("完成设置")]
    [Tooltip("完成后显示的完整照片精灵")]
    public Sprite completedPhotoSprite;

    [Tooltip("完成后可拾取的物品")]
    public ItemData completedPhotoItem;

    [Tooltip("完成后是否自动返回")]
    public bool autoReturnOnComplete = false;

    [Tooltip("自动返回延迟时间")]
    public float returnDelay = 1.0f;

    [Header("音效")]
    public string placePieceSoundPath = "Audio/SFX/piece_place";
    public string snapSoundPath = "Audio/SFX/piece_snap";
    public string completeSoundPath = "Audio/SFX/puzzle_complete";

    [Header("事件")]
    public UnityEvent OnPuzzleCompleted;
    public UnityEvent OnPhotoPickedUp;

    [Header("视觉设置")]
    [Tooltip("碎片的缩放比例（1 = 原始大小）")]
    [Range(0.1f, 2f)]
    public float pieceScale = 0.5f;

    [Tooltip("碎片的排序层级")]
    public int pieceSortingOrder = 10;

    [Tooltip("正在拖动的碎片层级（更高）")]
    public int draggingSortingOrder = 20;

    // 内部状态
    private List<DraggablePiece> placedPieces = new List<DraggablePiece>();
    private bool[] pieceCompleted = new bool[4];
    private bool isPuzzleCompleted = false;
    private bool isPhotoPickedUp = false;

    private SpriteRenderer completedPhotoRenderer;
    private BoxCollider2D photoCollider;

    private void OnEnable()
    {
        // 每次进入放大视图时恢复状态
        TryRestoreFromSave();

        if (isPuzzleCompleted && !isPhotoPickedUp)
        {
            ShowCompletedPhoto();
        }
    }

    private void Start()
    {
        // 创建完成照片的显示对象（初始隐藏）
        SetupCompletedPhotoObject();
    }

    private void Update()
    {
        // 检测点击放置碎片
        if (Input.GetMouseButtonDown(0) && !isPuzzleCompleted)
        {
            TryPlacePiece();
        }

        // 检测点击拾取完成的照片
        if (Input.GetMouseButtonDown(0) && isPuzzleCompleted && !isPhotoPickedUp)
        {
            TryPickupPhoto();
        }
    }

    /// <summary>
    /// 设置完成照片的显示对象
    /// </summary>
    private void SetupCompletedPhotoObject()
    {
        GameObject photoObj = new GameObject("CompletedPhoto");
        photoObj.transform.SetParent(transform);
        photoObj.transform.localPosition = Vector3.zero;

        completedPhotoRenderer = photoObj.AddComponent<SpriteRenderer>();
        completedPhotoRenderer.sprite = completedPhotoSprite;
        completedPhotoRenderer.sortingOrder = pieceSortingOrder + 5;
        completedPhotoRenderer.enabled = false;

        // 添加碰撞器用于拾取检测
        photoCollider = photoObj.AddComponent<BoxCollider2D>();
        photoCollider.enabled = false;

        // 根据精灵大小设置碰撞器
        if (completedPhotoSprite != null)
        {
            photoCollider.size = completedPhotoSprite.bounds.size;
        }
    }

    /// <summary>
    /// 尝试放置碎片
    /// </summary>
    private void TryPlacePiece()
    {
        // 检查是否有选中的物品
        if (UIManager.Instance == null || !UIManager.Instance.HasSelectedItem())
            return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
            return;

        // 检查是否是需要的碎片
        int pieceIndex = GetPieceIndex(selectedItem);
        if (pieceIndex < 0)
            return;

        // 检查这个碎片是否已经放置并完成
        if (pieceCompleted[pieceIndex])
        {
            Debug.Log($"[PhotoPuzzle] 碎片 {pieceIndex + 1} 已经放置完成");
            return;
        }

        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 检查点击是否在拼图区域内（可选：添加边界检查）

        // 消耗物品
        UIManager.Instance.ConsumeSelectedItem();

        // 创建可拖动的碎片
        CreateDraggablePiece(pieceIndex, mouseWorldPos);

        // 播放放置音效
        PlaySound(placePieceSoundPath);

        Debug.Log($"[PhotoPuzzle] 放置碎片 {pieceIndex + 1} 在位置 {mouseWorldPos}");
    }

    /// <summary>
    /// 获取物品对应的碎片索引
    /// </summary>
    private int GetPieceIndex(ItemData item)
    {
        for (int i = 0; i < requiredPieces.Length; i++)
        {
            if (requiredPieces[i] != null && requiredPieces[i].itemID == item.itemID)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 创建可拖动的碎片
    /// </summary>
    private void CreateDraggablePiece(int pieceIndex, Vector3 position)
    {
        // 检查是否已存在这个碎片（未完成的）
        DraggablePiece existingPiece = placedPieces.Find(p => p != null && p.PieceIndex == pieceIndex);
        if (existingPiece != null)
        {
            // 移除旧的
            placedPieces.Remove(existingPiece);
            Destroy(existingPiece.gameObject);
        }

        GameObject pieceObj = new GameObject($"Piece_{pieceIndex + 1}");
        pieceObj.transform.SetParent(transform);
        pieceObj.transform.position = position;
        pieceObj.transform.localScale = Vector3.one * pieceScale;  // 应用缩放

        // 添加 SpriteRenderer
        SpriteRenderer sr = pieceObj.AddComponent<SpriteRenderer>();
        if (pieceIndex < pieceSprites.Length && pieceSprites[pieceIndex] != null)
        {
            sr.sprite = pieceSprites[pieceIndex];
        }
        sr.sortingOrder = pieceSortingOrder;

        // 添加碰撞器
        BoxCollider2D collider = pieceObj.AddComponent<BoxCollider2D>();
        if (sr.sprite != null)
        {
            collider.size = sr.sprite.bounds.size;
        }

        // 添加拖动组件
        DraggablePiece piece = pieceObj.AddComponent<DraggablePiece>();
        piece.Initialize(this, pieceIndex, correctPositions[pieceIndex], snapDistance);
        piece.SetSortingOrders(pieceSortingOrder, draggingSortingOrder);

        placedPieces.Add(piece);
    }

    /// <summary>
    /// 碎片吸附到正确位置时调用
    /// </summary>
    public void OnPieceSnapped(int pieceIndex)
    {
        pieceCompleted[pieceIndex] = true;

        // 播放吸附音效
        PlaySound(snapSoundPath);

        Debug.Log($"[PhotoPuzzle] 碎片 {pieceIndex + 1} 已吸附到正确位置");

        // 检查是否全部完成
        CheckPuzzleComplete();

        // 保存状态
        SaveState();
    }

    /// <summary>
    /// 检查谜题是否完成
    /// </summary>
    private void CheckPuzzleComplete()
    {
        for (int i = 0; i < pieceCompleted.Length; i++)
        {
            if (!pieceCompleted[i])
                return;
        }

        // 全部完成！
        isPuzzleCompleted = true;
        Debug.Log("[PhotoPuzzle] ✓ 拼图完成！");

        // 播放完成音效
        PlaySound(completeSoundPath);

        // 隐藏所有碎片，显示完整照片
        HideAllPieces();
        ShowCompletedPhoto();

        // 触发事件
        OnPuzzleCompleted?.Invoke();

        // 保存状态
        SaveState();

        // 自动返回（可选）
        if (autoReturnOnComplete)
        {
            Invoke(nameof(AutoReturn), returnDelay);
        }
    }

    /// <summary>
    /// 隐藏所有碎片
    /// </summary>
    private void HideAllPieces()
    {
        foreach (var piece in placedPieces)
        {
            if (piece != null)
            {
                piece.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 显示完成的照片
    /// </summary>
    private void ShowCompletedPhoto()
    {
        if (completedPhotoRenderer != null)
        {
            completedPhotoRenderer.enabled = true;
            photoCollider.enabled = true;
        }
    }

    /// <summary>
    /// 尝试拾取完成的照片
    /// </summary>
    private void TryPickupPhoto()
    {
        if (completedPhotoItem == null)
            return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 检测点击是否在照片上
        Collider2D hit = Physics2D.OverlapPoint(mouseWorldPos);
        if (hit != null && hit == photoCollider)
        {
            // 添加到背包
            if (InventorySystem.Instance != null)
            {
                bool added = InventorySystem.Instance.AddItem(completedPhotoItem);
                if (added)
                {
                    isPhotoPickedUp = true;
                    completedPhotoRenderer.enabled = false;
                    photoCollider.enabled = false;

                    Debug.Log($"[PhotoPuzzle] 拾取完整照片: {completedPhotoItem.displayName}");

                    // 播放拾取音效
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySFX("Audio/SFX/item_pickup");
                    }

                    // 触发事件
                    OnPhotoPickedUp?.Invoke();

                    // 保存状态
                    SaveState();
                }
            }
        }
    }

    /// <summary>
    /// 自动返回
    /// </summary>
    private void AutoReturn()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExitZoomView();
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    private void PlaySound(string soundPath)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundPath))
        {
            AudioManager.Instance.PlaySFX(soundPath);
        }
    }

    // ============ 存档相关 ============

    private void SaveState()
    {
        string key = $"PhotoPuzzle_{puzzleID}";
        string data = GetSaveData();
        PlayerPrefs.SetString(key, data);
        PlayerPrefs.Save();

        // 同时触发主存档系统
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    /// <summary>
    /// 获取存档数据
    /// </summary>
    public string GetSaveData()
    {
        // 格式: completed0,completed1,completed2,completed3,puzzleCompleted,photoPickedUp,pos0x,pos0y,...
        List<string> parts = new List<string>();

        // 碎片完成状态
        for (int i = 0; i < 4; i++)
        {
            parts.Add(pieceCompleted[i] ? "1" : "0");
        }

        // 谜题完成和照片拾取状态
        parts.Add(isPuzzleCompleted ? "1" : "0");
        parts.Add(isPhotoPickedUp ? "1" : "0");

        // 未完成碎片的位置
        for (int i = 0; i < 4; i++)
        {
            if (!pieceCompleted[i])
            {
                DraggablePiece piece = placedPieces.Find(p => p != null && p.PieceIndex == i);
                if (piece != null)
                {
                    parts.Add($"{i}:{piece.transform.localPosition.x:F2}:{piece.transform.localPosition.y:F2}");
                }
            }
        }

        return string.Join(",", parts);
    }

    /// <summary>
    /// 从存档恢复
    /// </summary>
    private bool TryRestoreFromSave()
    {
        string key = $"PhotoPuzzle_{puzzleID}";

        if (!PlayerPrefs.HasKey(key))
            return false;

        string data = PlayerPrefs.GetString(key);
        string[] parts = data.Split(',');

        if (parts.Length < 6)
            return false;

        // 恢复碎片完成状态
        for (int i = 0; i < 4; i++)
        {
            pieceCompleted[i] = parts[i] == "1";
        }

        // 恢复谜题完成和照片拾取状态
        isPuzzleCompleted = parts[4] == "1";
        isPhotoPickedUp = parts[5] == "1";

        // 恢复未完成碎片的位置
        for (int i = 6; i < parts.Length; i++)
        {
            string[] posParts = parts[i].Split(':');
            if (posParts.Length == 3)
            {
                int pieceIndex = int.Parse(posParts[0]);
                float x = float.Parse(posParts[1]);
                float y = float.Parse(posParts[2]);

                if (!pieceCompleted[pieceIndex])
                {
                    CreateDraggablePiece(pieceIndex, transform.position + new Vector3(x, y, 0));
                }
            }
        }

        // 恢复已完成碎片（显示在正确位置）
        for (int i = 0; i < 4; i++)
        {
            if (pieceCompleted[i] && !isPuzzleCompleted)
            {
                CreateCompletedPiece(i);
            }
        }

        Debug.Log($"[PhotoPuzzle] 从存档恢复: completed={isPuzzleCompleted}, pickedUp={isPhotoPickedUp}");
        return true;
    }

    /// <summary>
    /// 创建已完成的碎片（不可拖动）
    /// </summary>
    private void CreateCompletedPiece(int pieceIndex)
    {
        GameObject pieceObj = new GameObject($"CompletedPiece_{pieceIndex + 1}");
        pieceObj.transform.SetParent(transform);
        pieceObj.transform.localPosition = new Vector3(correctPositions[pieceIndex].x, correctPositions[pieceIndex].y, 0);
        pieceObj.transform.localScale = Vector3.one * pieceScale;  // 应用缩放

        SpriteRenderer sr = pieceObj.AddComponent<SpriteRenderer>();
        if (pieceIndex < pieceSprites.Length && pieceSprites[pieceIndex] != null)
        {
            sr.sprite = pieceSprites[pieceIndex];
        }
        sr.sortingOrder = pieceSortingOrder;
    }

    private void OnDisable()
    {
        // 离开放大视图时保存
        SaveState();
    }

    // ============ 编辑器辅助 ============

    private void OnDrawGizmosSelected()
    {
        // 绘制正确位置
        Gizmos.color = Color.green;
        for (int i = 0; i < correctPositions.Length; i++)
        {
            Vector3 worldPos = transform.position + new Vector3(correctPositions[i].x, correctPositions[i].y, 0);
            Gizmos.DrawWireSphere(worldPos, snapDistance);
            Gizmos.DrawWireCube(worldPos, new Vector3(1f, 1f, 0.1f));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(worldPos + Vector3.up * 0.5f, $"Piece {i + 1}");
#endif
        }
    }
}