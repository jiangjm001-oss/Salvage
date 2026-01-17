// Assets/Scripts/GamePlay/SlidingPuzzleBox.cs
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 华容道机关盒 - 3x3滑动谜题 + 容器功能
/// 解开谜题后可以打开盒子，露出内部物品
/// </summary>
public class SlidingPuzzleBox : MonoBehaviour
{
    [Header("基本信息")]
    [Tooltip("物体唯一标识符（用于存档）")]
    public string objectID;

    [Header("谜题配置")]
    [Tooltip("数字方块的精灵图（按顺序：1-8，共8张）")]
    public Sprite[] tileSprites;

    [Tooltip("方块大小")]
    public float tileSize = 1f;

    [Tooltip("方块间距")]
    public float tileSpacing = 0.05f;

    [Tooltip("谜题相对于盒子的偏移位置")]
    public Vector2 puzzleOffset = Vector2.zero;

    [Header("盒子外观")]
    [Tooltip("盒子关闭时的精灵图")]
    public Sprite boxClosedSprite;

    [Tooltip("盒子打开时的精灵图")]
    public Sprite boxOpenedSprite;

    [Header("内部物品")]
    [Tooltip("盒子内的物品（解锁后可见）")]
    public GameObject[] containedObjects;

    [Header("音效")]
    [Tooltip("方块滑动音效")]
    public string slideSoundPath = "Audio/SFX/slide";

    [Tooltip("谜题完成音效")]
    public string completeSoundPath = "Audio/SFX/puzzle_complete";

    [Tooltip("盒子打开音效")]
    public string openSoundPath = "Audio/SFX/box_open";

    [Tooltip("盒子关闭音效")]
    public string closeSoundPath = "Audio/SFX/box_close";

    [Header("事件")]
    public UnityEvent OnPuzzleSolved;
    public UnityEvent OnBoxOpened;
    public UnityEvent OnBoxClosed;

    [Header("运行时状态（只读）")]
    [SerializeField] private bool isPuzzleSolved = false;
    [SerializeField] private bool isBoxOpen = false;

    // 内部数据
    private int[] board = new int[9]; // 0=空位, 1-8=数字
    private int emptyIndex = 8; // 空位索引
    private GameObject[] tileObjects = new GameObject[8];
    private SpriteRenderer boxRenderer;
    private bool isInitialized = false;

    // 目标状态：1,2,3,4,5,6,7,8,0
    private readonly int[] solvedState = { 1, 2, 3, 4, 5, 6, 7, 8, 0 };

    /// <summary>
    /// 谜题是否已解开
    /// </summary>
    public bool IsPuzzleSolved => isPuzzleSolved;

    /// <summary>
    /// 盒子是否打开
    /// </summary>
    public bool IsBoxOpen => isBoxOpen;

    private void Awake()
    {
        boxRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        if (!isInitialized)
        {
            InitializePuzzle();
        }
        UpdateBoxVisual();
        UpdateContainedObjects();
    }

    /// <summary>
    /// 初始化谜题
    /// </summary>
    public void InitializePuzzle()
    {
        if (isInitialized) return;

        // 生成可解的随机布局
        GenerateSolvableBoard();

        // 创建方块
        CreateTiles();

        isInitialized = true;

        Debug.Log($"[SlidingPuzzleBox] 谜题初始化完成，初始布局: {string.Join(",", board)}");
    }

    /// <summary>
    /// 生成可解的棋盘布局
    /// </summary>
    private void GenerateSolvableBoard()
    {
        // 从已解决状态开始，随机滑动若干次
        board = (int[])solvedState.Clone();
        emptyIndex = 8;

        // 随机滑动 100 次打乱
        int shuffleCount = 100;
        int lastMove = -1;

        for (int i = 0; i < shuffleCount; i++)
        {
            int[] neighbors = GetNeighbors(emptyIndex);

            // 过滤掉上一次移动的方块（避免来回移动）
            int moveIndex;
            do
            {
                moveIndex = neighbors[Random.Range(0, neighbors.Length)];
            } while (neighbors.Length > 1 && moveIndex == lastMove);

            // 执行移动
            board[emptyIndex] = board[moveIndex];
            board[moveIndex] = 0;
            lastMove = emptyIndex;
            emptyIndex = moveIndex;
        }
    }

    /// <summary>
    /// 获取指定位置的相邻位置
    /// </summary>
    private int[] GetNeighbors(int index)
    {
        int row = index / 3;
        int col = index % 3;
        var neighbors = new System.Collections.Generic.List<int>();

        if (row > 0) neighbors.Add(index - 3); // 上
        if (row < 2) neighbors.Add(index + 3); // 下
        if (col > 0) neighbors.Add(index - 1); // 左
        if (col < 2) neighbors.Add(index + 1); // 右

        return neighbors.ToArray();
    }

    /// <summary>
    /// 创建方块物体
    /// </summary>
    private void CreateTiles()
    {
        if (tileSprites == null || tileSprites.Length < 8)
        {
            Debug.LogError("[SlidingPuzzleBox] 需要配置8张数字精灵图！");
            return;
        }

        float totalSize = tileSize + tileSpacing;

        for (int i = 0; i < 9; i++)
        {
            int value = board[i];
            if (value == 0) continue; // 跳过空位

            // 创建方块物体
            GameObject tile = new GameObject($"Tile_{value}");
            tile.transform.SetParent(transform);

            // 计算位置
            int row = i / 3;
            int col = i % 3;
            float x = (col - 1) * totalSize + puzzleOffset.x;
            float y = (1 - row) * totalSize + puzzleOffset.y;
            tile.transform.localPosition = new Vector3(x, y, -0.1f);

            // 添加精灵
            SpriteRenderer sr = tile.AddComponent<SpriteRenderer>();
            sr.sprite = tileSprites[value - 1];
            sr.sortingOrder = boxRenderer != null ? boxRenderer.sortingOrder + 1 : 1;

            // 添加碰撞体
            BoxCollider2D collider = tile.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(tileSize, tileSize);

            // 添加方块组件
            PuzzleTile puzzleTile = tile.AddComponent<PuzzleTile>();
            puzzleTile.Initialize(this, value);

            // 保存引用
            tileObjects[value - 1] = tile;
        }
    }

    /// <summary>
    /// 尝试移动方块（由 PuzzleTile 调用）
    /// </summary>
    public bool TryMoveTile(int tileValue)
    {
        if (isPuzzleSolved) return false;

        // 找到方块当前位置
        int tileIndex = -1;
        for (int i = 0; i < 9; i++)
        {
            if (board[i] == tileValue)
            {
                tileIndex = i;
                break;
            }
        }

        if (tileIndex == -1) return false;

        // 检查是否与空位相邻
        int[] neighbors = GetNeighbors(emptyIndex);
        bool isAdjacent = false;
        foreach (int n in neighbors)
        {
            if (n == tileIndex)
            {
                isAdjacent = true;
                break;
            }
        }

        if (!isAdjacent) return false;

        // 执行移动
        board[emptyIndex] = tileValue;
        board[tileIndex] = 0;

        // 更新方块位置
        UpdateTilePosition(tileValue, emptyIndex);

        emptyIndex = tileIndex;

        // 播放音效
        PlaySound(slideSoundPath);

        Debug.Log($"[SlidingPuzzleBox] 移动方块 {tileValue}，当前布局: {string.Join(",", board)}");

        // 检查是否完成
        CheckSolved();

        // 保存游戏
        SaveLoadSystem.Instance?.SaveGame();

        return true;
    }

    /// <summary>
    /// 更新方块的显示位置
    /// </summary>
    private void UpdateTilePosition(int tileValue, int boardIndex)
    {
        if (tileValue < 1 || tileValue > 8) return;

        GameObject tile = tileObjects[tileValue - 1];
        if (tile == null) return;

        float totalSize = tileSize + tileSpacing;
        int row = boardIndex / 3;
        int col = boardIndex % 3;
        float x = (col - 1) * totalSize + puzzleOffset.x;
        float y = (1 - row) * totalSize + puzzleOffset.y;

        tile.transform.localPosition = new Vector3(x, y, -0.1f);
    }

    /// <summary>
    /// 检查是否已解开
    /// </summary>
    private void CheckSolved()
    {
        for (int i = 0; i < 9; i++)
        {
            if (board[i] != solvedState[i])
                return;
        }

        // 谜题解开！
        isPuzzleSolved = true;

        PlaySound(completeSoundPath);
        OnPuzzleSolved?.Invoke();

        Debug.Log("[SlidingPuzzleBox] ★ 谜题解开！盒子已解锁");
    }

    /// <summary>
    /// 点击盒子（谜题解开后可以开关）
    /// </summary>
    public void OnBoxClicked()
    {
        if (!isPuzzleSolved)
        {
            Debug.Log("[SlidingPuzzleBox] 谜题未解开，无法打开盒子");
            return;
        }

        // 切换开关状态
        isBoxOpen = !isBoxOpen;

        if (isBoxOpen)
        {
            OpenBox();
        }
        else
        {
            CloseBox();
        }

        SaveLoadSystem.Instance?.SaveGame();
    }

    private void OpenBox()
    {
        Debug.Log("[SlidingPuzzleBox] 打开盒子");

        // 隐藏谜题方块
        SetTilesVisible(false);

        // 更新盒子外观
        UpdateBoxVisual();

        // 显示内部物品
        UpdateContainedObjects();

        PlaySound(openSoundPath);
        OnBoxOpened?.Invoke();
    }

    private void CloseBox()
    {
        Debug.Log("[SlidingPuzzleBox] 关闭盒子");

        // 显示谜题方块
        SetTilesVisible(true);

        // 更新盒子外观
        UpdateBoxVisual();

        // 隐藏内部物品
        UpdateContainedObjects();

        PlaySound(closeSoundPath);
        OnBoxClosed?.Invoke();
    }

    private void SetTilesVisible(bool visible)
    {
        foreach (var tile in tileObjects)
        {
            if (tile != null)
                tile.SetActive(visible);
        }
    }

    private void UpdateBoxVisual()
    {
        if (boxRenderer == null) return;

        if (isBoxOpen && boxOpenedSprite != null)
        {
            boxRenderer.sprite = boxOpenedSprite;
        }
        else if (!isBoxOpen && boxClosedSprite != null)
        {
            boxRenderer.sprite = boxClosedSprite;
        }
    }

    private void UpdateContainedObjects()
    {
        if (containedObjects == null) return;

        foreach (var obj in containedObjects)
        {
            if (obj == null) continue;

            // 检查是否已被拾取
            InteractableObject interactable = obj.GetComponent<InteractableObject>();
            if (interactable != null && interactable.hasBeenPickedUp)
            {
                obj.SetActive(false);
                continue;
            }

            obj.SetActive(isBoxOpen);
        }
    }

    private void PlaySound(string path)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(path))
        {
            AudioManager.Instance.PlaySFX(path);
        }
    }

    // ============ 点击检测（盒子本体）============

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 只有谜题解开后，点击盒子才能开关
        if (isPuzzleSolved)
        {
            OnBoxClicked();
        }
    }

    // ============ 存档相关 ============

    /// <summary>
    /// 获取当前棋盘状态（用于存档）
    /// </summary>
    public int[] GetBoardState()
    {
        return (int[])board.Clone();
    }

    /// <summary>
    /// 恢复状态（用于读档）
    /// </summary>
    public void RestoreState(int[] savedBoard, bool solved, bool open)
    {
        if (savedBoard != null && savedBoard.Length == 9)
        {
            board = (int[])savedBoard.Clone();

            // 找到空位
            for (int i = 0; i < 9; i++)
            {
                if (board[i] == 0)
                {
                    emptyIndex = i;
                    break;
                }
            }

            // 重新创建方块
            DestroyTiles();
            CreateTiles();
        }

        isPuzzleSolved = solved;
        isBoxOpen = open;

        if (isPuzzleSolved && isBoxOpen)
        {
            SetTilesVisible(false);
        }

        UpdateBoxVisual();
        UpdateContainedObjects();

        isInitialized = true;
    }

    private void DestroyTiles()
    {
        foreach (var tile in tileObjects)
        {
            if (tile != null)
                Destroy(tile);
        }
        tileObjects = new GameObject[8];
    }
}