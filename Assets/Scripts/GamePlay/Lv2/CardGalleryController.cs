// Assets/Scripts/GamePlay/CardGalleryController.cs
// 卡片轮播控制器 - 用于在ZoomView中浏览多张卡片
// 点击左侧切换上一张，点击右侧切换下一张，首尾循环

using UnityEngine;
using UnityEngine.Events;

public class CardGalleryController : MonoBehaviour
{
    [Header("=== 卡片配置 ===")]
    [Tooltip("所有卡片的Sprite数组")]
    [SerializeField] private Sprite[] cardSprites;

    [Tooltip("显示卡片的SpriteRenderer")]
    [SerializeField] private SpriteRenderer cardDisplay;

    [Header("=== 点击区域（可选）===")]
    [Tooltip("左侧点击区域的Collider（不设置则使用卡片左半边）")]
    [SerializeField] private Collider2D leftClickArea;

    [Tooltip("右侧点击区域的Collider（不设置则使用卡片右半边）")]
    [SerializeField] private Collider2D rightClickArea;

    [Header("=== 音效配置 ===")]
    [Tooltip("切换卡片时的音效")]
    [SerializeField] private string flipSoundName = "Audio/SFX/card_flip";

    [Header("=== 事件 ===")]
    [Tooltip("卡片切换时触发")]
    public UnityEvent<int> OnCardChanged;

    // 当前卡片索引
    private int currentIndex = 0;

    // 缓存卡片的边界
    private Bounds cardBounds;
    private bool useSeparateClickAreas = false;

    private void Start()
    {
        InitializeGallery();
    }

    private void OnEnable()
    {
        // 每次显示时重置到第一张卡片
        currentIndex = 0;
        UpdateCardDisplay();
    }

    /// <summary>
    /// 初始化卡片轮播
    /// </summary>
    private void InitializeGallery()
    {
        // 检查必要组件
        if (cardSprites == null || cardSprites.Length == 0)
        {
            Debug.LogError($"[CardGallery] {gameObject.name}: 没有配置卡片Sprite！");
            return;
        }

        if (cardDisplay == null)
        {
            // 尝试在子物体中查找
            cardDisplay = GetComponentInChildren<SpriteRenderer>();
            if (cardDisplay == null)
            {
                Debug.LogError($"[CardGallery] {gameObject.name}: 没有配置CardDisplay！");
                return;
            }
        }

        // 检查是否使用独立的点击区域
        useSeparateClickAreas = (leftClickArea != null && rightClickArea != null);

        // 显示第一张卡片
        UpdateCardDisplay();

        Debug.Log($"[CardGallery] 初始化完成，共 {cardSprites.Length} 张卡片，使用独立点击区域: {useSeparateClickAreas}");
    }

    private void Update()
    {
        // 仅当此ZoomView激活时处理点击
        if (!gameObject.activeInHierarchy) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    /// <summary>
    /// 处理鼠标点击
    /// </summary>
    private void HandleClick()
    {
        if (cardSprites == null || cardSprites.Length <= 1) return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (useSeparateClickAreas)
        {
            // 使用独立的点击区域
            if (leftClickArea.OverlapPoint(mouseWorldPos))
            {
                ShowPreviousCard();
            }
            else if (rightClickArea.OverlapPoint(mouseWorldPos))
            {
                ShowNextCard();
            }
        }
        else
        {
            // 使用卡片自身的左右半边
            if (cardDisplay == null) return;

            // 获取卡片边界
            cardBounds = cardDisplay.bounds;

            // 检查点击是否在卡片范围内
            if (!cardBounds.Contains(mouseWorldPos)) return;

            // 计算卡片中心X坐标
            float centerX = cardBounds.center.x;

            if (mouseWorldPos.x < centerX)
            {
                // 点击左半边 → 上一张
                ShowPreviousCard();
            }
            else
            {
                // 点击右半边 → 下一张
                ShowNextCard();
            }
        }
    }

    /// <summary>
    /// 显示上一张卡片（循环）
    /// </summary>
    public void ShowPreviousCard()
    {
        if (cardSprites == null || cardSprites.Length == 0) return;

        currentIndex--;
        if (currentIndex < 0)
        {
            currentIndex = cardSprites.Length - 1; // 循环到最后一张
        }

        UpdateCardDisplay();
        PlayFlipSound();

        Debug.Log($"[CardGallery] 切换到上一张: {currentIndex + 1}/{cardSprites.Length}");
    }

    /// <summary>
    /// 显示下一张卡片（循环）
    /// </summary>
    public void ShowNextCard()
    {
        if (cardSprites == null || cardSprites.Length == 0) return;

        currentIndex++;
        if (currentIndex >= cardSprites.Length)
        {
            currentIndex = 0; // 循环到第一张
        }

        UpdateCardDisplay();
        PlayFlipSound();

        Debug.Log($"[CardGallery] 切换到下一张: {currentIndex + 1}/{cardSprites.Length}");
    }

    /// <summary>
    /// 跳转到指定卡片
    /// </summary>
    public void GoToCard(int index)
    {
        if (cardSprites == null || cardSprites.Length == 0) return;

        // 确保索引在有效范围内
        currentIndex = Mathf.Clamp(index, 0, cardSprites.Length - 1);
        UpdateCardDisplay();

        Debug.Log($"[CardGallery] 跳转到: {currentIndex + 1}/{cardSprites.Length}");
    }

    /// <summary>
    /// 更新卡片显示
    /// </summary>
    private void UpdateCardDisplay()
    {
        if (cardDisplay == null || cardSprites == null || cardSprites.Length == 0) return;

        if (currentIndex >= 0 && currentIndex < cardSprites.Length)
        {
            cardDisplay.sprite = cardSprites[currentIndex];
            OnCardChanged?.Invoke(currentIndex);
        }
    }

    /// <summary>
    /// 播放翻页音效
    /// </summary>
    private void PlayFlipSound()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(flipSoundName))
        {
            AudioManager.Instance.PlaySFX(flipSoundName);
        }
    }

    /// <summary>
    /// 获取当前卡片索引
    /// </summary>
    public int GetCurrentIndex()
    {
        return currentIndex;
    }

    /// <summary>
    /// 获取卡片总数
    /// </summary>
    public int GetCardCount()
    {
        return cardSprites?.Length ?? 0;
    }

    /// <summary>
    /// 获取当前卡片Sprite
    /// </summary>
    public Sprite GetCurrentCard()
    {
        if (cardSprites == null || cardSprites.Length == 0) return null;
        return cardSprites[currentIndex];
    }
}