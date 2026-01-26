// Assets/Scripts/GamePlay/Typewriter/TypewriterController.cs
// 打字机主控制器 - 配合分层显示系统
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
/// 4. 正确答案 → 通知 LetterManager，LetterDisplay 自动显示收件人
/// </summary>
public class TypewriterController : MonoBehaviour
{
    // ============ 道具配置 ============
    [Header("道具配置")]
    [Tooltip("需要的空白信纸道具（留空则使用 LetterManager 的 letterItemData）")]
    public ItemData requiredPaper;

    // ============ 信纸显示 ============
    [Header("信纸显示")]
    [Tooltip("信纸 GameObject（包含 LetterDisplay 组件，初始隐藏）")]
    public GameObject paperObject;

    [Tooltip("信纸的 LetterDisplay 组件（用于分层显示）")]
    public LetterDisplay letterDisplay;

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
    public bool IsPaperPlaced => isPaperPlaced;
    public bool IsPuzzleSolved => isPuzzleSolved;
    public string CurrentInput => currentInput;

    // ============ Unity 生命周期 ============

    private void Awake()
    {
        if (cursorObject != null)
        {
            cursorRenderer = cursorObject.GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        if (paperObject != null) paperObject.SetActive(false);
        if (cursorObject != null) cursorObject.SetActive(false);
        UpdatePaperDisplay();
        Debug.Log("[TypewriterController] 初始化完成");
    }

    private void OnEnable()
    {
        CheckAndRestoreState();
    }

    private void OnDestroy()
    {
        StopCursorBlink();
    }

    // ============ 状态检查与恢复 ============

    private void CheckAndRestoreState()
    {
        // 如果收件人已完成，直接显示完成状态
        if (LetterManager.Instance != null && LetterManager.Instance.hasRecipient)
        {
            isPuzzleSolved = true;
            canPickupResult = false;
            isPaperPlaced = false;

            if (paperObject != null) paperObject.SetActive(false);
            if (cursorObject != null) cursorObject.SetActive(false);

            Debug.Log("[TypewriterController] 收件人已完成，跳过谜题");
            return;
        }
    }

    // ============ 信纸放置 ============

    public void TryPlacePaper()
    {
        if (isPaperPlaced || isPuzzleSolved)
        {
            Debug.Log("[TypewriterController] 信纸已放置或谜题已解决");
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogError("[TypewriterController] UIManager.Instance 为空！");
            return;
        }

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log("[TypewriterController] 玩家没有选中任何物品");
            return;
        }

        // 获取所需的信纸 ItemData
        ItemData paperToCheck = requiredPaper;
        if (paperToCheck == null && LetterManager.Instance != null)
        {
            paperToCheck = LetterManager.Instance.letterItemData;
        }

        if (paperToCheck == null)
        {
            Debug.LogError("[TypewriterController] 未配置 requiredPaper！");
            return;
        }

        if (selectedItem.itemID != paperToCheck.itemID)
        {
            Debug.Log($"[TypewriterController] 选中的不是信纸");
            return;
        }

        PlacePaper();
    }

    private void PlacePaper()
    {
        // 消耗背包中的信纸
        UIManager.Instance.ConsumeSelectedItem();

        isPaperPlaced = true;
        currentInput = "";

        // 显示信纸
        if (paperObject != null)
        {
            paperObject.SetActive(true);
        }

        // 刷新 LetterDisplay 显示当前状态
        if (letterDisplay != null)
        {
            letterDisplay.RefreshDisplay();
        }

        // 显示光标
        if (cursorObject != null)
        {
            cursorObject.SetActive(true);
            if (cursorRenderer != null) cursorRenderer.enabled = true;
            UpdateCursorPosition();
            if (cursorBlink) StartCursorBlink();
        }

        UpdatePaperDisplay();
        Debug.Log("[TypewriterController] ✓ 信纸已放置");

        OnPaperPlaced?.Invoke();
        SaveLoadSystem.Instance?.SaveGame();
    }

    // ============ 字符输入 ============

    public void TypeCharacter(char character)
    {
        if (!isPaperPlaced || isPuzzleSolved) return;

        if (currentInput.Length >= maxCharacters)
        {
            Debug.Log($"[TypewriterController] 已达到最大字符数");
            return;
        }

        currentInput += character;
        UpdatePaperDisplay();

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(keyPressSFX))
        {
            AudioManager.Instance.PlaySFX(keyPressSFX);
        }

        Debug.Log($"[TypewriterController] 输入: '{character}' | 当前: \"{currentInput}\"");
    }

    public void Backspace()
    {
        if (!isPaperPlaced || isPuzzleSolved) return;
        if (currentInput.Length == 0) return;

        currentInput = currentInput.Substring(0, currentInput.Length - 1);
        UpdatePaperDisplay();

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(keyPressSFX))
        {
            AudioManager.Instance.PlaySFX(keyPressSFX);
        }
    }

    public void PressEnter()
    {
        if (!isPaperPlaced || isPuzzleSolved) return;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(enterSFX))
        {
            AudioManager.Instance.PlaySFX(enterSFX);
        }

        Debug.Log($"[TypewriterController] 提交: \"{currentInput}\"");

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

    private void OnCorrectAnswer()
    {
        Debug.Log("[TypewriterController] ★ 答案正确！");

        isPuzzleSolved = true;
        canPickupResult = true;

        if (paperText != null) paperText.color = correctColor;

        StopCursorBlink();
        if (cursorObject != null) cursorObject.SetActive(false);

        // 通知 LetterManager 收件人完成
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.SetRecipientComplete();
        }

        // LetterDisplay 会自动刷新显示收件人
        // 如果没有自动刷新，手动调用
        if (letterDisplay != null)
        {
            letterDisplay.RefreshDisplay();
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(successSFX))
        {
            AudioManager.Instance.PlaySFX(successSFX);
        }

        OnAnswerCorrect?.Invoke();
        SaveLoadSystem.Instance?.SaveGame();
    }

    private void OnWrongAnswer()
    {
        Debug.Log("[TypewriterController] ✗ 答案错误");

        currentInput = "";
        UpdatePaperDisplay();

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(errorSFX))
        {
            AudioManager.Instance.PlaySFX(errorSFX);
        }

        OnAnswerWrong?.Invoke();
    }

    // ============ 拾取结果 ============

    public void TryPickupResultPaper()
    {
        if (!canPickupResult)
        {
            Debug.Log("[TypewriterController] 当前不能拾取");
            return;
        }

        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[TypewriterController] InventorySystem 为空！");
            return;
        }

        // 通过 LetterManager 添加信纸到背包
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.AddLetterToInventory();
        }

        if (paperObject != null) paperObject.SetActive(false);

        canPickupResult = false;
        isPaperPlaced = false;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSFX))
        {
            AudioManager.Instance.PlaySFX(pickupSFX);
        }

        OnResultPickedUp?.Invoke();
        SaveLoadSystem.Instance?.SaveGame();

        Debug.Log("[TypewriterController] ✓ 信纸已拾取");
    }

    // ============ 显示更新 ============

    private void UpdatePaperDisplay()
    {
        if (paperText != null)
        {
            paperText.text = currentInput;
            paperText.color = isPuzzleSolved ? correctColor : normalColor;
        }
        UpdateCursorPosition();
    }

    // ============ 光标系统 ============

    private void UpdateCursorPosition()
    {
        if (cursorObject == null || cursorStartPosition == null) return;
        float offsetX = currentInput.Length * characterWidth;
        cursorObject.transform.position = cursorStartPosition.position + new Vector3(offsetX, 0, 0);
    }

    private void StartCursorBlink()
    {
        StopCursorBlink();
        blinkCoroutine = StartCoroutine(CursorBlinkRoutine());
    }

    private void StopCursorBlink()
    {
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }
        if (cursorRenderer != null) cursorRenderer.enabled = true;
    }

    private IEnumerator CursorBlinkRoutine()
    {
        if (cursorRenderer == null) yield break;
        while (true)
        {
            cursorRenderer.enabled = !cursorRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    // ============ 存档系统 ============

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

    public void LoadSaveData(TypewriterSaveData data)
    {
        if (data == null) return;

        isPaperPlaced = data.isPaperPlaced;
        isPuzzleSolved = data.isPuzzleSolved;
        canPickupResult = data.canPickupResult;
        currentInput = data.currentInput ?? "";

        if (paperObject != null) paperObject.SetActive(isPaperPlaced);

        if (cursorObject != null)
        {
            bool showCursor = isPaperPlaced && !isPuzzleSolved;
            cursorObject.SetActive(showCursor);
            if (showCursor && cursorBlink) StartCursorBlink();
        }

        if (letterDisplay != null) letterDisplay.RefreshDisplay();
        UpdatePaperDisplay();
    }

    // ============ 调试 ============

    [ContextMenu("Debug: 重置谜题")]
    public void DebugReset()
    {
        isPaperPlaced = false;
        isPuzzleSolved = false;
        canPickupResult = false;
        currentInput = "";
        StopCursorBlink();
        if (paperObject != null) paperObject.SetActive(false);
        if (cursorObject != null) cursorObject.SetActive(false);
        UpdatePaperDisplay();
    }

    [ContextMenu("Debug: 直接解决")]
    public void DebugSolve()
    {
        if (!isPaperPlaced)
        {
            isPaperPlaced = true;
            if (paperObject != null) paperObject.SetActive(true);
        }
        currentInput = correctAnswer;
        OnCorrectAnswer();
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