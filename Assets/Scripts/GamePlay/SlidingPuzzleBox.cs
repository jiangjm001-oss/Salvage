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

    [Tooltip("方块大小")]
    public float tileSize = 1.5f;

    [Tooltip("方块间距")]
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
        // 每次进入放大视图时检查
        if (!isInitialized)
        {
            InitializePuzzle();
        }

        // 如果已经完成，直接返回
        if (isSolved)
        {
            Debug.Log("[SlidingPuzzle] 谜题已完成，自动返回");
            // 延迟一帧返回，避免冲突
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

        // 尝试从存档恢复
        if (!TryRestoreFromSave())
        {
            // 没有存档，生成随机可解的布局
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
        // 从已解决状态开始，随机移动来打乱
        board = (int[])solvedState.Clone();

        System.Random rng = new System.Random();
        int emptyIndex = 8; // 空格初始在位置8

        // 随机移动100次
        for (int i = 0; i < 100; i++)
        {
            List<int> neighbors = GetNeighbors(emptyIndex);
            int randomNeighbor = neighbors[rng.Next(neighbors.Count)];

            // 交换
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

        float totalSize = tileSize + tileSpacing;

        for (int i = 0; i < 9; i++)
        {
            int value = board[i];
            if (value == 0) continue; // 空格不创建

            int row = i / 3;
            int col = i % 3;

            // 计算位置（左上角为原点）
            float x = (col - 1) * totalSize + puzzleOffset.x;
            float y = (1 - row) * totalSize + puzzleOffset.y;

            GameObject tile = new GameObject($"Tile_{value}");
            tile.transform.SetParent(transform);
            tile.transform.localPosition = new Vector3(x, y, 0);

            // 添加 SpriteRenderer
            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            if (value >= 1 && value <= 8 && tileSprites[value - 1] != null)
            {
                sr.sprite = tileSprites[value - 1];
            }
            sr.sortingOrder = 10;

            // 添加碰撞器
            BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(tileSize, tileSize);

            // 添加点击组件
            PuzzleTile pt = tile.AddComponent<PuzzleTile>();
            pt.puzzle = this;
            pt.boardIndex = i;

            tileObjects[value - 1] = tile;
        }
    }

    /// <summary>
    /// 尝试移动方块
    /// </summary>
    public void TryMoveTile(int boardIndex)
    {
        if (isSolved) return;

        int value = board[boardIndex];
        if (value == 0) return; // 点击的是空格

        // 找到空格位置
        int emptyIndex = System.Array.IndexOf(board, 0);

        // 检查是否相邻
        List<int> neighbors = GetNeighbors(emptyIndex);
        if (!neighbors.Contains(boardIndex)) return;

        // 交换
        board[emptyIndex] = value;
        board[boardIndex] = 0;

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
    /// 更新所有方块位置
    /// </summary>
    private void UpdateTilePositions()
    {
        float totalSize = tileSize + tileSpacing;

        for (int i = 0; i < 9; i++)
        {
            int value = board[i];
            if (value == 0) continue;

            int row = i / 3;
            int col = i % 3;

            float x = (col - 1) * totalSize + puzzleOffset.x;
            float y = (1 - row) * totalSize + puzzleOffset.y;

            GameObject tile = tileObjects[value - 1];
            if (tile != null)
            {
                tile.transform.localPosition = new Vector3(x, y, 0);

                // 更新 PuzzleTile 的 boardIndex
                PuzzleTile pt = tile.GetComponent<PuzzleTile>();
                if (pt != null) pt.boardIndex = i;
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

        // 完成！
        isSolved = true;
        Debug.Log("[SlidingPuzzle] ✓ 谜题完成！");

        // 播放完成音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(completeSoundPath))
        {
            AudioManager.Instance.PlaySFX(completeSoundPath);
        }

        // 触发事件
        OnPuzzleCompleted?.Invoke();

        // 切换盒子状态
        SwitchBoxToOpened();

        // 保存状态
        SaveState();

        // 延迟返回上一界面
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

        // 显示内部物品
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
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    private bool TryRestoreFromSave()
    {
        // 这里需要从 SaveLoadSystem 获取存档数据
        // 简化实现：使用 PlayerPrefs
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
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 获取存档数据
    /// </summary>
    public string GetSaveData()
    {
        string boardStr = string.Join(",", board);
        return $"{boardStr},{(isSolved ? "1" : "0")}";
    }

    /// <summary>
    /// 恢复存档数据
    /// </summary>
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

            // 如果已完成，切换盒子状态
            if (isSolved)
            {
                SwitchBoxToOpened();
            }
        }
    }

    /// <summary>
    /// 保存到 PlayerPrefs（简化存档）
    /// </summary>
    public void SaveToPlayerPrefs()
    {
        string key = $"Puzzle_{puzzleID}";
        PlayerPrefs.SetString(key, GetSaveData());
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        // 离开放大视图时保存
        SaveToPlayerPrefs();
    }

    // ============ 编辑器辅助 ============

    private void OnDrawGizmosSelected()
    {
        float totalSize = tileSize + tileSpacing;

        Gizmos.color = Color.yellow;

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                float x = (col - 1) * totalSize + puzzleOffset.x;
                float y = (1 - row) * totalSize + puzzleOffset.y;

                Vector3 pos = transform.position + new Vector3(x, y, 0);
                Gizmos.DrawWireCube(pos, new Vector3(tileSize, tileSize, 0.1f));
            }
        }
    }
}