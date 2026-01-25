// Assets/Scripts/GamePlay/Letter/TypewriterController.cs
// 打字机控制器 - 放在打字机 ZoomView 中
// 功能：打出 "BlackHat" 后，信纸获得收件人
using UnityEngine;
using TMPro;
using UnityEngine.Events;

/// <summary>
/// 打字机控制器
/// 玩家在打字机 ZoomView 中输入指定文字，完成后信纸获得收件人
/// </summary>
public class TypewriterController : MonoBehaviour
{
    [Header("打字设置")]
    [Tooltip("需要打出的目标文字")]
    public string targetText = "BlackHat";

    [Tooltip("显示已打字符的 TMP 文本")]
    public TextMeshPro displayText;

    [Tooltip("是否区分大小写")]
    public bool caseSensitive = false;

    [Tooltip("最大输入字符数（防止无限输入）")]
    public int maxInputLength = 20;

    [Header("信纸显示")]
    [Tooltip("打字机中的信纸物体（需要先把信纸放入背包才显示）")]
    public GameObject letterInTypewriter;

    [Tooltip("信纸的 SpriteRenderer（用于更新精灵）")]
    public SpriteRenderer letterSpriteRenderer;

    [Header("完成后设置")]
    [Tooltip("完成打字后显示的信纸物体（带收件人版本）")]
    public GameObject completedLetter;

    [Tooltip("完成后是否自动消耗背包中的信纸")]
    public bool consumeLetterOnComplete = true;

    [Header("提示设置")]
    [Tooltip("没有信纸时的提示文字")]
    public string noLetterHint = "需要一张信纸...";

    [Header("音效")]
    public string typeSoundName = "typewriter_key";
    public string deleteSoundName = "typewriter_key";
    public string completeSoundName = "typewriter_ding";
    public string errorSoundName = "";

    [Header("事件")]
    public UnityEvent OnTypingStarted;
    public UnityEvent OnTypingComplete;

    // 内部状态
    private string currentInput = "";
    private bool isComplete = false;
    private bool isActive = false;
    private bool hasLetterPlaced = false;

    private void OnEnable()
    {
        isActive = true;
        currentInput = "";

        // 检查是否已完成收件人
        if (LetterManager.Instance != null && LetterManager.Instance.hasRecipient)
        {
            isComplete = true;
            currentInput = targetText;
            UpdateDisplay();
            ShowCompletedState();
            return;
        }

        // 检查玩家是否持有信纸
        CheckForLetter();
    }

    private void OnDisable()
    {
        isActive = false;
    }

    private void Update()
    {
        if (!isActive || isComplete || !hasLetterPlaced) return;

        // 检测键盘输入
        if (!string.IsNullOrEmpty(Input.inputString))
        {
            foreach (char c in Input.inputString)
            {
                ProcessInput(c);
            }
        }
    }

    /// <summary>
    /// 检查玩家是否持有信纸
    /// </summary>
    private void CheckForLetter()
    {
        if (LetterManager.Instance == null)
        {
            Debug.LogWarning("[TypewriterController] LetterManager.Instance 为空！");
            return;
        }

        bool hasLetter = LetterManager.Instance.HasLetterInInventory();
        hasLetterPlaced = hasLetter;

        // 显示/隐藏信纸
        if (letterInTypewriter != null)
        {
            letterInTypewriter.SetActive(hasLetter);
        }

        // 更新信纸精灵
        if (hasLetter && letterSpriteRenderer != null)
        {
            letterSpriteRenderer.sprite = LetterManager.Instance.GetCurrentSprite();
        }

        // 隐藏已完成的信纸
        if (completedLetter != null)
        {
            completedLetter.SetActive(false);
        }

        Debug.Log($"[TypewriterController] 信纸检查: {(hasLetter ? "有" : "无")}");
    }

    /// <summary>
    /// 处理输入字符
    /// </summary>
    private void ProcessInput(char c)
    {
        // 退格处理
        if (c == '\b')
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
                UpdateDisplay();
                PlaySound(deleteSoundName);
            }
            return;
        }

        // 回车忽略
        if (c == '\n' || c == '\r') return;

        // 忽略非可打印字符
        if (char.IsControl(c)) return;

        // 检查长度限制
        if (currentInput.Length >= maxInputLength)
        {
            PlaySound(errorSoundName);
            return;
        }

        // 添加字符
        currentInput += c;
        UpdateDisplay();

        // 播放打字音效
        PlaySound(typeSoundName);

        // 首次输入触发事件
        if (currentInput.Length == 1)
        {
            OnTypingStarted?.Invoke();
        }

        // 检查是否完成
        CheckCompletion();
    }

    private void UpdateDisplay()
    {
        if (displayText != null)
        {
            displayText.text = currentInput;
        }
    }

    private void CheckCompletion()
    {
        string target = caseSensitive ? targetText : targetText.ToLower();
        string input = caseSensitive ? currentInput : currentInput.ToLower();

        if (input == target)
        {
            OnTypingCompleteHandler();
        }
    }

    private void OnTypingCompleteHandler()
    {
        if (isComplete) return;

        isComplete = true;
        Debug.Log("[TypewriterController] ✓ 打字完成！");

        // 播放完成音效
        PlaySound(completeSoundName);

        // 消耗背包中的信纸（如果配置了）
        if (consumeLetterOnComplete && LetterManager.Instance != null)
        {
            LetterManager.Instance.RemoveLetterFromInventory();
        }

        // 通知 LetterManager
        if (LetterManager.Instance != null)
        {
            LetterManager.Instance.SetRecipientComplete();
        }

        // 显示完成状态
        ShowCompletedState();

        // 触发事件
        OnTypingComplete?.Invoke();
    }

    private void ShowCompletedState()
    {
        // 隐藏原来的信纸
        if (letterInTypewriter != null)
        {
            letterInTypewriter.SetActive(false);
        }

        // 显示完成版本的信纸（可拾取）
        if (completedLetter != null)
        {
            completedLetter.SetActive(true);

            // 更新精灵
            SpriteRenderer sr = completedLetter.GetComponent<SpriteRenderer>();
            if (sr != null && LetterManager.Instance != null)
            {
                sr.sprite = LetterManager.Instance.GetCurrentSprite();
            }
        }
    }

    private void PlaySound(string soundName)
    {
        if (string.IsNullOrEmpty(soundName)) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    /// <summary>
    /// 手动放置信纸（如果玩家选中信纸点击打字机区域）
    /// </summary>
    public void TryPlaceLetter()
    {
        if (isComplete) return;
        if (hasLetterPlaced) return;

        // 检查是否选中了信纸
        if (UIManager.Instance == null || LetterManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null) return;

        if (selectedItem.itemID != LetterManager.Instance.letterItemData.itemID) return;

        // 取消选中（不消耗，只是取消选中）
        UIManager.Instance.DeselectItem();

        // 显示信纸
        hasLetterPlaced = true;
        if (letterInTypewriter != null)
        {
            letterInTypewriter.SetActive(true);

            if (letterSpriteRenderer != null)
            {
                letterSpriteRenderer.sprite = LetterManager.Instance.GetCurrentSprite();
            }
        }

        Debug.Log("[TypewriterController] 信纸已放入打字机");
    }

    /// <summary>
    /// 重置状态（用于测试）
    /// </summary>
    public void ResetState()
    {
        currentInput = "";
        isComplete = false;
        hasLetterPlaced = false;
        UpdateDisplay();
        CheckForLetter();
    }
}