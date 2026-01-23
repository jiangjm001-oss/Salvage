// Assets/Scripts/Puzzles/Typewriter/TypewriterController.cs
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

/// <summary>
/// 打字机主控制器
/// 管理信纸放置、文字输入、答案验证、光标显示
/// 
/// 使用流程：
/// 1. 玩家选中空白信纸，点击打字机 → 信纸出现
/// 2. 点击按键输入字母 → 文字显示，光标移动
/// 3. 点击回车键 → 验证答案
/// 4. 正确答案 → 文字变色，可拾取结果信纸
/// </summary>
public class TypewriterController : MonoBehaviour
{
    // ============ 道具配置 ============
    [Header("道具配置")]
    [Tooltip("需要的空白信纸道具")]
    public ItemData requiredPaper;

    [Tooltip("正确答案后获得的信纸道具")]
    public ItemData resultPaper;

    // ============ 信纸显示 ============
    [Header("信纸显示")]
    [Tooltip("信纸 GameObject（初始隐藏）")]
    public GameObject paperObject;

    [Tooltip("信纸上的文字显示组件")]
    public TextMeshPro paperText;

    // ============ 答案配置 ============
    [Header("答案配置")]
    [Tooltip("正确答案（不分大小写）")]
    public string correctAnswer = "BlackHat";

    [Tooltip("最大输入字符数")]
    public int maxCharacters = 10;

    // ============ 视觉效果 ============
    [Header("视觉效果")]
    [Tooltip("普通输入时的文字颜色")]
    public Color normalColor = Color.black;

    [Tooltip("正确答案时的文字颜色")]
    public Color correctColor = Color.red;

    // ============ 字符指引器（光标） ============
    [Header("字符指引器（光标）")]
    [Tooltip("光标 Sprite 物体")]
    public GameObject cursorObject;

    [Tooltip("光标起始位置（第一个字符的位置）")]
    public Transform cursorStartPosition;

    [Tooltip("每个字符的宽度（光标移动间距）")]
    public float characterWidth = 0.3f;

    [Tooltip("光标是否闪烁")]
    public bool cursorBlink = true;

    [Tooltip("闪烁间隔（秒）")]
    public float blinkInterval = 0.5f;

    // ============ 音效 ============
    [Header("音效")]
    [Tooltip("按键音效")]
    public string keyPressSFX = "typewriter_key";

    [Tooltip("回车键音效")]
    public string enterSFX = "typewriter_enter";

    [Tooltip("答案正确音效")]
    public string successSFX = "typewriter_success";

    [Tooltip("答案错误音效")]
    public string errorSFX = "typewriter_error";

    [Tooltip("拾取信纸音效")]
    public string pickupSFX = "item_pickup";

    // ============ 事件 ============
    [Header("事件")]
    [Tooltip("信纸放置时触发")]
    public UnityEvent OnPaperPlaced;

    [Tooltip("答案正确时触发")]
    public UnityEvent OnAnswerCorrect;

    [Tooltip("答案错误时触发")]
    public UnityEvent OnAnswerWrong;

    [Tooltip("拾取结果信纸时触发")]
    public UnityEvent OnResultPickedUp;

    // ============ 内部状态 ============
    private string currentInput = "";
    private bool isPaperPlaced = false;
    private bool isPuzzleSolved = false;
    private bool canPickupResult = false;
    private Coroutine blinkCoroutine;
    private SpriteRenderer cursorRenderer;

    // ============ 属性访问器 ============

    /// <summary>
    /// 信纸是否已放置
    /// </summary>
    public bool IsPaperPlaced => isPaperPlaced;

    /// <summary>
    /// 谜题是否已解决
    /// </summary>
    public bool IsPuzzleSolved => isPuzzleSolved;

    /// <summary>
    /// 当前输入的文字
    /// </summary>
    public string CurrentInput => currentInput;

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        // 缓存光标渲染器
        if (cursorObject != null)
        {
            cursorRenderer = cursorObject.GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        // 初始隐藏信纸
        if (paperObject != null)
        {
            paperObject.SetActive(false);
        }

        // 初始隐藏光标
        if (cursorObject != null)
        {
            cursorObject.SetActive(false);
        }

        UpdatePaperDisplay();

        Debug.Log("[TypewriterController] 初始化完成");
    }

    private void OnDestroy()
    {
        // 清理协程
        StopCursorBlink();
    }

    // ============ 信纸放置 ============

    /// <summary>
    /// 尝试放置信纸（由打字机点击区域调用）
    /// </summary>
    public void TryPlacePaper()
    {
        // 如果已放置或已解谜，不处理
        if (isPaperPlaced || isPuzzleSolved)
        {
            Debug.Log("[TypewriterController] 信纸已放置或谜题已解决，忽略放置请求");
            return;
        }

        // 检查是否有 UIManager
        if (UIManager.Instance == null)
        {
            Debug.LogError("[TypewriterController] UIManager.Instance 为空！");
            return;
        }

        // 检查玩家是否选中了物品
        ItemData selectedItem = UIManager.Instance.GetSelectedItem();

        if (selectedItem == null)
        {
            Debug.Log("[TypewriterController] 玩家没有选中任何物品");
            return;
        }

        // 检查是否配置了所需物品
        if (requiredPaper == null)
        {
            Debug.LogError("[TypewriterController] 未配置 requiredPaper！");
            return;
        }

        // 检查是否是正确的物品
        if (selectedItem.itemID != requiredPaper.itemID)
        {
            Debug.Log($"[TypewriterController] 选中的物品 '{selectedItem.itemID}' 不是所需的 '{requiredPaper.itemID}'");
            return;
        }

        // 放置信纸
        PlacePaper();
    }

    /// <summary>
    /// 执行放置信纸
    /// </summary>
    private void PlacePaper()
    {
        // 消耗背包中的信纸
        UIManager.Instance.ConsumeSelectedItem();

        // 更新状态
        isPaperPlaced = true;
        currentInput = "";

        // 显示信纸
        if (paperObject != null)
        {
            paperObject.SetActive(true);
        }

        // 显示光标并开始闪烁
        if (cursorObject != null)
        {
            cursorObject.SetActive(true);

            // 确保光标渲染器启用
            if (cursorRenderer != null)
            {
                cursorRenderer.enabled = true;
            }

            UpdateCursorPosition();

            if (cursorBlink)
            {
                StartCursorBlink();
            }
        }

        // 更新显示
        UpdatePaperDisplay();

        // 播放音效（可选）
        // AudioManager.Instance?.PlaySFX("paper_place");

        Debug.Log("[TypewriterController] ✓ 信纸已放置");

        // 触发事件
        OnPaperPlaced?.Invoke();

        // 保存进度
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ 字符输入 ============

    /// <summary>
    /// 输入字符（由 TypewriterKey 调用）
    /// </summary>
    /// <param name="character">要输入的字符</param>
    public void TypeCharacter(char character)
    {
        // 检查状态
        if (!isPaperPlaced)
        {
            Debug.Log("[TypewriterController] 信纸未放置，忽略输入");
            return;
        }

        if (isPuzzleSolved)
        {
            Debug.Log("[TypewriterController] 谜题已解决，忽略输入");
            return;
        }

        // 检查字符数限制
        if (currentInput.Length >= maxCharacters)
        {
            Debug.Log($"[TypewriterController] 已达到最大字符数 ({maxCharacters})");
            return;
        }

        // 添加字符
        currentInput += character;

        // 更新显示
        UpdatePaperDisplay();

        // 播放打字音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(keyPressSFX))
        {
            AudioManager.Instance.PlaySFX(keyPressSFX);
        }

        Debug.Log($"[TypewriterController] 输入: '{character}' | 当前: \"{currentInput}\"");
    }

    /// <summary>
    /// 退格删除最后一个字符
    /// </summary>
    public void Backspace()
    {
        // 检查状态
        if (!isPaperPlaced || isPuzzleSolved)
        {
            return;
        }

        // 检查是否有字符可删除
        if (currentInput.Length == 0)
        {
            Debug.Log("[TypewriterController] 没有字符可删除");
            return;
        }

        // 删除最后一个字符
        currentInput = currentInput.Substring(0, currentInput.Length - 1);

        // 更新显示
        UpdatePaperDisplay();

        // 播放音效（可选）
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(keyPressSFX))
        {
            AudioManager.Instance.PlaySFX(keyPressSFX);
        }

        Debug.Log($"[TypewriterController] 退格 | 当前: \"{currentInput}\"");
    }

    /// <summary>
    /// 按下回车键（提交答案）
    /// </summary>
    public void PressEnter()
    {
        // 检查状态
        if (!isPaperPlaced)
        {
            Debug.Log("[TypewriterController] 信纸未放置，忽略回车");
            return;
        }

        if (isPuzzleSolved)
        {
            Debug.Log("[TypewriterController] 谜题已解决，忽略回车");
            return;
        }

        // 播放回车音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(enterSFX))
        {
            AudioManager.Instance.PlaySFX(enterSFX);
        }

        Debug.Log($"[TypewriterController] 回车提交 | 输入: \"{currentInput}\" | 答案: \"{correctAnswer}\"");

        // 检查答案（不分大小写）
        if (currentInput.Equals(correctAnswer, System.StringComparison.OrdinalIgnoreCase))
        {
            OnCorrectAnswer();
        }
        else
        {
            OnWrongAnswer();
        }
    }

    // ============ 答案处理 ============

    /// <summary>
    /// 正确答案处理
    /// </summary>
    private void OnCorrectAnswer()
    {
        Debug.Log("[TypewriterController] ★ 答案正确！");

        // 更新状态
        isPuzzleSolved = true;
        canPickupResult = true;

        // 文字变色
        if (paperText != null)
        {
            paperText.color = correctColor;
        }

        // 停止并隐藏光标
        StopCursorBlink();
        if (cursorObject != null)
        {
            cursorObject.SetActive(false);
        }

        // 播放成功音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(successSFX))
        {
            AudioManager.Instance.PlaySFX(successSFX);
        }

        // 触发事件
        OnAnswerCorrect?.Invoke();

        // 保存进度
        SaveLoadSystem.Instance?.SaveGame();
    }

    /// <summary>
    /// 错误答案处理
    /// </summary>
    private void OnWrongAnswer()
    {
        Debug.Log("[TypewriterController] ✗ 答案错误，重置输入");

        // 清空输入
        currentInput = "";

        // 更新显示（光标会回到起始位置）
        UpdatePaperDisplay();

        // 播放错误音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(errorSFX))
        {
            AudioManager.Instance.PlaySFX(errorSFX);
        }

        // 触发事件
        OnAnswerWrong?.Invoke();
    }

    // ============ 拾取结果 ============

    /// <summary>
    /// 尝试拾取结果信纸（点击信纸时调用）
    /// </summary>
    public void TryPickupResultPaper()
    {
        // 检查是否可以拾取
        if (!canPickupResult)
        {
            Debug.Log("[TypewriterController] 当前不能拾取结果信纸");
            return;
        }

        // 检查是否配置了结果物品
        if (resultPaper == null)
        {
            Debug.LogError("[TypewriterController] 未配置 resultPaper！");
            return;
        }

        // 检查背包系统
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[TypewriterController] InventorySystem.Instance 为空！");
            return;
        }

        // 添加到背包
        bool added = InventorySystem.Instance.AddItem(resultPaper);

        if (added)
        {
            Debug.Log($"[TypewriterController] ✓ 拾取了: {resultPaper.displayName}");

            // 隐藏信纸
            if (paperObject != null)
            {
                paperObject.SetActive(false);
            }

            // 更新状态
            canPickupResult = false;

            // 播放拾取音效
            if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSFX))
            {
                AudioManager.Instance.PlaySFX(pickupSFX);
            }

            // 触发事件
            OnResultPickedUp?.Invoke();

            // 保存进度
            SaveLoadSystem.Instance?.SaveGame();
        }
        else
        {
            Debug.LogWarning("[TypewriterController] 背包已满，无法拾取结果信纸");
        }
    }

    // ============ 显示更新 ============

    /// <summary>
    /// 更新信纸显示
    /// </summary>
    private void UpdatePaperDisplay()
    {
        // 更新文字
        if (paperText != null)
        {
            paperText.text = currentInput;
            paperText.color = isPuzzleSolved ? correctColor : normalColor;
        }

        // 更新光标位置
        UpdateCursorPosition();
    }

    // ============ 光标系统 ============

    /// <summary>
    /// 更新光标位置
    /// </summary>
    private void UpdateCursorPosition()
    {
        if (cursorObject == null) return;
        if (cursorStartPosition == null) return;

        // 根据当前输入的字符数计算光标位置
        float offsetX = currentInput.Length * characterWidth;

        // 设置光标位置
        cursorObject.transform.position = cursorStartPosition.position
            + new Vector3(offsetX, 0, 0);
    }

    /// <summary>
    /// 开始光标闪烁
    /// </summary>
    private void StartCursorBlink()
    {
        // 先停止已有的闪烁
        StopCursorBlink();

        // 启动新的闪烁协程
        blinkCoroutine = StartCoroutine(CursorBlinkRoutine());
    }

    /// <summary>
    /// 停止光标闪烁
    /// </summary>
    private void StopCursorBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // 确保光标可见（如果存在）
        if (cursorRenderer != null)
        {
            cursorRenderer.enabled = true;
        }
    }

    /// <summary>
    /// 光标闪烁协程
    /// </summary>
    private IEnumerator CursorBlinkRoutine()
    {
        if (cursorRenderer == null) yield break;

        while (true)
        {
            // 切换可见性
            cursorRenderer.enabled = !cursorRenderer.enabled;

            // 等待间隔
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    // ============ 存档系统 ============

    /// <summary>
    /// 获取存档数据
    /// </summary>
    public TypewriterSaveData GetSaveData()
    {
        return new TypewriterSaveData
        {
            isPaperPlaced = this.isPaperPlaced,
            isPuzzleSolved = this.isPuzzleSolved,
            canPickupResult = this.canPickupResult,
            currentInput = this.currentInput
        };
    }

    /// <summary>
    /// 从存档恢复状态
    /// </summary>
    public void LoadSaveData(TypewriterSaveData data)
    {
        if (data == null)
        {
            Debug.LogWarning("[TypewriterController] 存档数据为空");
            return;
        }

        // 恢复状态
        isPaperPlaced = data.isPaperPlaced;
        isPuzzleSolved = data.isPuzzleSolved;
        canPickupResult = data.canPickupResult;
        currentInput = data.currentInput ?? "";

        // 更新视觉状态
        if (paperObject != null)
        {
            paperObject.SetActive(isPaperPlaced);
        }

        if (cursorObject != null)
        {
            // 只有在信纸已放置且谜题未解决时显示光标
            bool showCursor = isPaperPlaced && !isPuzzleSolved;
            cursorObject.SetActive(showCursor);

            if (showCursor && cursorBlink)
            {
                StartCursorBlink();
            }
        }

        UpdatePaperDisplay();

        Debug.Log($"[TypewriterController] 从存档恢复 | 信纸: {isPaperPlaced} | 已解决: {isPuzzleSolved} | 输入: \"{currentInput}\"");
    }

    // ============ 调试方法 ============

    /// <summary>
    /// 重置谜题状态（仅用于调试）
    /// </summary>
    [ContextMenu("Debug: 重置谜题")]
    public void DebugReset()
    {
        isPaperPlaced = false;
        isPuzzleSolved = false;
        canPickupResult = false;
        currentInput = "";

        StopCursorBlink();

        if (paperObject != null)
        {
            paperObject.SetActive(false);
        }

        if (cursorObject != null)
        {
            cursorObject.SetActive(false);
        }

        UpdatePaperDisplay();

        Debug.Log("[TypewriterController] 谜题已重置");
    }

    /// <summary>
    /// 直接解决谜题（仅用于调试）
    /// </summary>
    [ContextMenu("Debug: 直接解决谜题")]
    public void DebugSolve()
    {
        if (!isPaperPlaced)
        {
            // 先放置信纸
            isPaperPlaced = true;
            if (paperObject != null)
            {
                paperObject.SetActive(true);
            }
        }

        currentInput = correctAnswer;
        OnCorrectAnswer();

        Debug.Log("[TypewriterController] 谜题已直接解决");
    }
}

/// <summary>
/// 打字机存档数据
/// </summary>
[System.Serializable]
public class TypewriterSaveData
{
    public bool isPaperPlaced;
    public bool isPuzzleSolved;
    public bool canPickupResult;
    public string currentInput;
}