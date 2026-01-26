// Assets/Scripts/GamePlay/SlidingPuzzle.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// 华容道滑动谜题组件
/// 放在放大视图中，完成后自动返回并切换盒子状态
/// </summary>
public class SlidingPuzzle : MonoBehaviour
{
    [Header("谜题设置")]
    [Tooltip("数字方块精灵 (1-8)，按顺序放入")]
    public Sprite[] tileSprites = new Sprite[8];

    [Tooltip("方块缩放比例")]
    public float tileScale = 0.3f;

    [Tooltip("方块间距（缩放后的实际间距）")]
    public float tileSpacing = 0.1f;

    [Tooltip("谜题相对于此物体的偏移")]
    public Vector2 puzzleOffset = Vector2.zero;

    [Header("关联的盒子")]
    [Tooltip("盒子的 SpriteRenderer（用于切换打开状态）")]
    public SpriteRenderer boxRenderer;

    [Tooltip("盒子打开后的精灵图")]
    public Sprite boxOpenedSprite;

    [Tooltip("盒子内的物品（完成后显示）")]
    public GameObject[] containedItems;

    [Header("音效")]
    [Tooltip("移动方块音效")]
    public string moveSoundPath = "Audio/SFX/tile_move";

    [Tooltip("完成谜题音效")]
    public string completeSoundPath = "Audio/SFX/puzzle_complete";

    [Header("事件")]
    public UnityEvent OnPuzzleCompleted;

    [Header("存档")]
    [Tooltip("唯一标识符")]
    public string puzzleID = "sliding_puzzle_01";

    // 内部状态
    private int[] board = new int[9]; // 0 = 空格, 1-8 = 数字
    private GameObject[] tileObjects = new GameObject[8];
    private bool isSolved = false;
    private bool isInitialized = false;

    // 目标状态：1,2,3,4,5,6,7,8,0
    private readonly int[] solvedState = { 1, 2, 3, 4, 5, 6, 7, 8, 0 };

    private void OnEnable()
    {
        if (!isInitialized)
        {
            InitializePuzzle();
        }

        if (isSolved)
        {
            Debug.Log("[SlidingPuzzle] 谜题已完成，自动返回");
            Invoke(nameof(AutoReturn), 0.1f);
        }
    }

    private void Start()
    {
        if (!isInitialized)
        {
            InitializePuzzle();
        }
    }

    /// <summary>
    /// 初始化谜题
    /// </summary>
    private void InitializePuzzle()
    {
        if (isInitialized) return;

        if (!TryRestoreFromSave())
        {
            GenerateSolvableBoard();
        }

        CreateTiles();
        isInitialized = true;

        Debug.Log($"[SlidingPuzzle] 初始化完成，棋盘: [{string.Join(",", board)}]");
    }

    /// <summary>
    /// 生成可解的随机棋盘
    /// </summary>
    private void GenerateSolvableBoard()
    {
        board = (int[])solvedState.Clone();

        System.Random rng = new System.Random();
        int emptyIndex = 8;

        for (int i = 0; i < 100; i++)
        {
            List<int> neighbors = GetNeighbors(emptyIndex);
            int randomNeighbor = neighbors[rng.Next(neighbors.Count)];

            board[emptyIndex] = board[randomNeighbor];
            board[randomNeighbor] = 0;
            emptyIndex = randomNeighbor;
        }
    }

    /// <summary>
    /// 获取相邻位置
    /// </summary>
    private List<int> GetNeighbors(int index)
    {
        List<int> neighbors = new List<int>();
        int row = index / 3;
        int col = index % 3;

        if (row > 0) neighbors.Add(index - 3); // 上
        if (row < 2) neighbors.Add(index + 3); // 下
        if (col > 0) neighbors.Add(index - 1); // 左
        if (col < 2) neighbors.Add(index + 1); // 右

        return neighbors;
    }

    /// <summary>
    /// 创建方块 GameObject
    /// </summary>
    private void CreateTiles()
    {
        // 清除旧的方块
        foreach (var tile in tileObjects)
        {
            if (tile != null) Destroy(tile);
        }

        // 计算方块实际大小
        float actualTileSize = 1f;
        Vector2 spriteSize = Vector2.one;
        if (tileSprites.Length > 0 && tileSprites[0] != null)
        {
            spriteSize = tileSprites[0].bounds.size;
            actualTileSize = spriteSize.x * tileScale;
        }

        float totalSize = actualTileSize + tileSpacing;

        Debug.Log($"[SlidingPuzzle] CreateTiles: scale={tileScale}, spriteSize={spriteSize}, totalSize={totalSize}");

        for (int i = 0; i < 9; i++)
        {
            int value = board[i];
            if (value == 0) continue;

            int row = i / 3;
            int col = i % 3;

            float x = (col - 1) * totalSize + puzzleOffset.x;
            float y = (1 - row) * totalSize + puzzleOffset.y;

            GameObject tile = new GameObject($"Tile_{value}");
            tile.transform.SetParent(transform);
            tile.transform.localPosition = new Vector3(x, y, 0);
            tile.transform.localScale = new Vector3(tileScale, tileScale, 1f);

            // SpriteRenderer
            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            Vector2 thisSpriteSize = spriteSize;
            if (value >= 1 && value <= 8 && tileSprites[value - 1] != null)
            {
                sr.sprite = tileSprites[value - 1];
                thisSpriteSize = tileSprites[value - 1].bounds.size;
            }
            sr.sortingOrder = 10;

            // 碰撞器 - 使用更可靠的方式获取尺寸
            BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
            Sprite currentSprite = sr.sprite;
            if (currentSprite != null)
            {
                // 使用 rect 和 pixelsPerUnit 计算正确的尺寸
                Vector2 spriteRectSize = new Vector2(
                    currentSprite.rect.width / currentSprite.pixelsPerUnit,
                    currentSprite.rect.height / currentSprite.pixelsPerUnit
                );
                collider.size = spriteRectSize;
                Debug.Log($"[SlidingPuzzle] Tile_{value} 碰撞器大小: {spriteRectSize}");
            }
            else
            {
                // 后备方案：使用默认大小
                collider.size = Vector2.one;
                Debug.LogWarning($"[SlidingPuzzle] Tile_{value} 精灵为空，使用默认碰撞器大小");
            }

            // PuzzleTile 组件
            PuzzleTile pt = tile.AddComponent<PuzzleTile>();
            pt.puzzle = this;
            pt.boardIndex = i;
            pt.tileValue = value; // ⭐ 新增：记录方块的值

            tileObjects[value - 1] = tile;

            Debug.Log($"[SlidingPuzzle] 创建 Tile_{value}: boardIndex={i}, pos=({x:F2},{y:F2})");
        }
    }

    /// <summary>
    /// 尝试移动方块（通过方块值来找位置，更可靠）
    /// </summary>
    public void TryMoveTileByValue(int tileValue)
    {
        if (isSolved)
        {
            Debug.Log("[SlidingPuzzle] 谜题已完成，忽略点击");
            return;
        }

        // 根据值找到当前在棋盘中的位置
        int boardIndex = System.Array.IndexOf(board, tileValue);
        Debug.Log($"[SlidingPuzzle] TryMoveTileByValue: tileValue={tileValue}, 当前位置={boardIndex}");

        if (boardIndex < 0)
        {
            Debug.LogError($"[SlidingPuzzle] 找不到值 {tileValue} 在棋盘中的位置！");
            return;
        }

        // 找到空格位置
        int emptyIndex = System.Array.IndexOf(board, 0);
        Debug.Log($"[SlidingPuzzle] 空格位置: {emptyIndex}");

        // 检查是否相邻
        List<int> neighbors = GetNeighbors(emptyIndex);
        Debug.Log($"[SlidingPuzzle] 空格的相邻位置: [{string.Join(",", neighbors)}]");

        if (!neighbors.Contains(boardIndex))
        {
            Debug.Log($"[SlidingPuzzle] 位置 {boardIndex} 不与空格相邻，无法移动");
            return;
        }

        // 交换
        board[emptyIndex] = tileValue;
        board[boardIndex] = 0;
        Debug.Log($"[SlidingPuzzle] 移动成功！新棋盘: [{string.Join(",", board)}]");

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(moveSoundPath))
        {
            AudioManager.Instance.PlaySFX(moveSoundPath);
        }

        // 更新方块位置
        UpdateTilePositions();

        // 检查是否完成
        CheckSolved();

        // 保存状态
        SaveState();
    }

    /// <summary>
    /// 旧方法保留兼容
    /// </summary>
    public void TryMoveTile(int boardIndex)
    {
        if (isSolved) return;

        int value = board[boardIndex];
        if (value == 0) return;

        TryMoveTileByValue(value);
    }

    /// <summary>
    /// 更新所有方块位置
    /// </summary>
    private void UpdateTilePositions()
    {
        float actualTileSize = 1f;
        if (tileSprites.Length > 0 && tileSprites[0] != null)
        {
            actualTileSize = tileSprites[0].bounds.size.x * tileScale;
        }

        float totalSize = actualTileSize + tileSpacing;

        // 遍历所有 8 个方块
        for (int tileValue = 1; tileValue <= 8; tileValue++)
        {
            // 找到这个值在棋盘中的当前位置
            int boardPos = System.Array.IndexOf(board, tileValue);
            if (boardPos < 0) continue;

            int row = boardPos / 3;
            int col = boardPos % 3;

            float x = (col - 1) * totalSize + puzzleOffset.x;
            float y = (1 - row) * totalSize + puzzleOffset.y;

            GameObject tile = tileObjects[tileValue - 1];
            if (tile != null)
            {
                tile.transform.localPosition = new Vector3(x, y, 0);

                // 更新 PuzzleTile 的 boardIndex
                PuzzleTile pt = tile.GetComponent<PuzzleTile>();
                if (pt != null)
                {
                    pt.boardIndex = boardPos;
                }
            }
        }
    }

    /// <summary>
    /// 检查是否完成
    /// </summary>
    private void CheckSolved()
    {
        for (int i = 0; i < 9; i++)
        {
            if (board[i] != solvedState[i]) return;
        }

        isSolved = true;
        Debug.Log("[SlidingPuzzle] ✓ 谜题完成！");

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(completeSoundPath))
        {
            AudioManager.Instance.PlaySFX(completeSoundPath);
        }

        OnPuzzleCompleted?.Invoke();
        SwitchBoxToOpened();
        SaveState();
        Invoke(nameof(AutoReturn), 0.8f);
    }

    /// <summary>
    /// 切换盒子为打开状态
    /// </summary>
    private void SwitchBoxToOpened()
    {
        if (boxRenderer != null && boxOpenedSprite != null)
        {
            boxRenderer.sprite = boxOpenedSprite;
            Debug.Log("[SlidingPuzzle] 盒子已切换为打开状态");
        }

        if (containedItems != null)
        {
            foreach (var item in containedItems)
            {
                if (item != null)
                {
                    item.SetActive(true);
                }
            }
        }
    }

    /// <summary>
    /// 自动返回上一界面
    /// </summary>
    private void AutoReturn()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExitZoomView();
        }
    }

    // ============ 存档相关 ============

    private void SaveState()
    {
        SaveToPlayerPrefs();
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    private bool TryRestoreFromSave()
    {
        string key = $"Puzzle_{puzzleID}";

        if (PlayerPrefs.HasKey(key))
        {
            string data = PlayerPrefs.GetString(key);
            string[] parts = data.Split(',');

            if (parts.Length >= 10)
            {
                for (int i = 0; i < 9; i++)
                {
                    board[i] = int.Parse(parts[i]);
                }
                isSolved = parts[9] == "1";

                Debug.Log($"[SlidingPuzzle] 从存档恢复: [{string.Join(",", board)}], solved={isSolved}");

                if (isSolved)
                {
                    SwitchBoxToOpened();
                }

                return true;
            }
        }

        return false;
    }

    public string GetSaveData()
    {
        string boardStr = string.Join(",", board);
        return $"{boardStr},{(isSolved ? "1" : "0")}";
    }

    public void RestoreSaveData(string data)
    {
        if (string.IsNullOrEmpty(data)) return;

        string[] parts = data.Split(',');
        if (parts.Length >= 10)
        {
            for (int i = 0; i < 9; i++)
            {
                board[i] = int.Parse(parts[i]);
            }
            isSolved = parts[9] == "1";

            if (isInitialized)
            {
                UpdateTilePositions();
            }

            if (isSolved)
            {
                SwitchBoxToOpened();
            }
        }
    }

    public void SaveToPlayerPrefs()
    {
        string key = $"Puzzle_{puzzleID}";
        PlayerPrefs.SetString(key, GetSaveData());
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        if (isInitialized)
        {
            SaveToPlayerPrefs();
        }
    }

    // ============ 编辑器辅助 ============

    private void OnDrawGizmosSelected()
    {
        float actualTileSize = 1f;
        if (tileSprites != null && tileSprites.Length > 0 && tileSprites[0] != null)
        {
            actualTileSize = tileSprites[0].bounds.size.x * tileScale;
        }

        float totalSize = actualTileSize + tileSpacing;

        Gizmos.color = Color.yellow;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = (col - 1) * totalSize + puzzleOffset.x;
                float y = (1 - row) * totalSize + puzzleOffset.y;

                Vector3 pos = transform.position + new Vector3(x, y, 0);
                Gizmos.DrawWireCube(pos, new Vector3(actualTileSize, actualTileSize, 0.1f));
            }
        }
    }
}