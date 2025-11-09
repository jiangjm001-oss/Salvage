using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Inventory UI (可选,如果为空则自动查找)")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject secondColumnPanel;
    [SerializeField] private Button expandButton;
    [SerializeField] private Text expandButtonText;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Inventory Settings")]
    [SerializeField] private float expandAnimationDuration = 0.3f;
    [SerializeField] private AnimationCurve expandCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Navigation Buttons")]
    [SerializeField] private GameObject leftArrowButton;
    [SerializeField] private GameObject rightArrowButton;
    [SerializeField] private GameObject backButton;

    [Header("Other UI")]
    [SerializeField] private GameObject pauseMenuPanel;

    private bool isExpanded = false;
    private bool isAnimating = false;
    private List<GameObject> slotUIObjects = new List<GameObject>();
    private int selectedIndex = -1;

    // ============ 属性访问器,自动查找UI元素 ============

    private GameObject InventoryPanel
    {
        get
        {
            if (inventoryPanel == null)
            {
                Transform found = transform.Find("UICanvas/InventoryPanel");
                if (found != null)
                {
                    inventoryPanel = found.gameObject;
                    Debug.Log("[UIManager] Auto-found InventoryPanel");
                }
            }
            return inventoryPanel;
        }
    }

    private GameObject SecondColumnPanel
    {
        get
        {
            if (secondColumnPanel == null)
            {
                Transform found = transform.Find("UICanvas/SecondColumnPanel");
                if (found != null)
                {
                    secondColumnPanel = found.gameObject;
                    Debug.Log("[UIManager] Auto-found SecondColumnPanel");
                }
            }
            return secondColumnPanel;
        }
    }

    private Transform SlotContainer
    {
        get
        {
            if (InventoryPanel == null) return null;
            return InventoryPanel.transform.Find("SlotContainer");
        }
    }

    private Transform SecondSlotContainer
    {
        get
        {
            if (SecondColumnPanel == null) return null;
            return SecondColumnPanel.transform.Find("SlotContainer");
        }
    }

    private Button ExpandButton
    {
        get
        {
            if (expandButton == null && InventoryPanel != null)
            {
                Transform found = InventoryPanel.transform.Find("ExpandButton");
                if (found != null)
                {
                    expandButton = found.GetComponent<Button>();
                    Debug.Log("[UIManager] Auto-found ExpandButton");
                }
            }
            return expandButton;
        }
    }

    private GameObject ItemSlotPrefab
    {
        get
        {
            if (itemSlotPrefab == null)
            {
                itemSlotPrefab = Resources.Load<GameObject>("Prefabs/UI/ItemSlot");
                if (itemSlotPrefab != null)
                {
                    Debug.Log("[UIManager] Auto-loaded ItemSlot prefab from Resources");
                }
            }
            return itemSlotPrefab;
        }
    }

    // ============ Unity生命周期 ============

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        Debug.Log("[UIManager] Starting initialization...");

        // 订阅InventorySystem的事件
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged.AddListener(UpdateInventoryUI);
            Debug.Log("[UIManager] Subscribed to InventorySystem.OnInventoryChanged");
        }
        else
        {
            Debug.LogWarning("[UIManager] InventorySystem.Instance is null!");
        }

        // 订阅GameManager的事件
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.AddListener(OnViewStateChanged);
            GameManager.Instance.OnGameStateChanged.AddListener(OnGameStateChanged);
            Debug.Log("[UIManager] Subscribed to GameManager events");

            // 立即更新一次
            UpdateNavigationButtons();
            UpdateInventoryVisibility();
        }
        else
        {
            Debug.LogWarning("[UIManager] GameManager.Instance is null!");
        }

        // 绑定展开按钮
        var expButton = ExpandButton;
        if (expButton != null)
        {
            expButton.onClick.AddListener(ToggleInventoryExpansion);
            Debug.Log("[UIManager] ExpandButton click listener added");

            // 初始化按钮文字
            var textComp = expButton.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                expandButtonText = textComp;
                expandButtonText.text = ">";
            }
        }
        else
        {
            Debug.LogError("[UIManager] ExpandButton not found!");
        }

        // 初始化背包UI
        UpdateInventoryUI();

        Debug.Log("[UIManager] Initialization complete");
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged.RemoveListener(UpdateInventoryUI);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.RemoveListener(OnViewStateChanged);
            GameManager.Instance.OnGameStateChanged.RemoveListener(OnGameStateChanged);
        }

        var expButton = ExpandButton;
        if (expButton != null)
        {
            expButton.onClick.RemoveListener(ToggleInventoryExpansion);
        }
    }

    // ============ 游戏状态响应 ============

    private void OnGameStateChanged(GameManager.GameState newState)
    {
        Debug.Log($"[UIManager] OnGameStateChanged: {newState}");
        UpdateNavigationButtons();
        UpdateInventoryVisibility();
    }

    private void OnViewStateChanged(GameManager.ViewState newState)
    {
        Debug.Log($"[UIManager] OnViewStateChanged: {newState}");
        UpdateNavigationButtons();
    }

    // ============ 导航按钮控制 ============

    private void UpdateNavigationButtons()
    {
        if (GameManager.Instance == null) return;

        var gameState = GameManager.Instance.CurrentGameState;
        bool shouldShowNavigation = (gameState == GameManager.GameState.Level1 ||
                                      gameState == GameManager.GameState.Level2);

        if (!shouldShowNavigation)
        {
            if (leftArrowButton != null) leftArrowButton.SetActive(false);
            if (rightArrowButton != null) rightArrowButton.SetActive(false);
            if (backButton != null) backButton.SetActive(false);
            return;
        }

        bool isInWallView = GameManager.Instance.IsInWallView();

        if (isInWallView)
        {
            if (leftArrowButton != null) leftArrowButton.SetActive(true);
            if (rightArrowButton != null) rightArrowButton.SetActive(true);
            if (backButton != null) backButton.SetActive(false);
        }
        else
        {
            if (leftArrowButton != null) leftArrowButton.SetActive(false);
            if (rightArrowButton != null) rightArrowButton.SetActive(false);
            if (backButton != null) backButton.SetActive(true);
        }
    }

    // ============ 背包显示控制 ============

    private void UpdateInventoryVisibility()
    {
        if (GameManager.Instance == null) return;

        var gameState = GameManager.Instance.CurrentGameState;
        bool shouldShow = (gameState == GameManager.GameState.Level1 ||
                           gameState == GameManager.GameState.Level2);

        if (InventoryPanel != null)
        {
            InventoryPanel.SetActive(shouldShow);
            Debug.Log($"[UIManager] InventoryPanel.SetActive({shouldShow})");
        }

        if (SecondColumnPanel != null)
        {
            SecondColumnPanel.SetActive(shouldShow);
            Debug.Log($"[UIManager] SecondColumnPanel.SetActive({shouldShow})");
        }
    }

    public void ShowInventoryUI()
    {
        if (InventoryPanel != null) InventoryPanel.SetActive(true);
        if (SecondColumnPanel != null) SecondColumnPanel.SetActive(true);
    }

    public void HideInventoryUI()
    {
        if (InventoryPanel != null) InventoryPanel.SetActive(false);
        if (SecondColumnPanel != null) SecondColumnPanel.SetActive(false);
    }

    // ============ 背包UI更新 ============

    private void UpdateInventoryUI()
    {
        Debug.Log("[UIManager] UpdateInventoryUI called");

        var slotContainer = SlotContainer;
        var secondSlotContainer = SecondSlotContainer;
        var prefab = ItemSlotPrefab;

        if (slotContainer == null)
        {
            Debug.LogError("[UIManager] SlotContainer not found!");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError("[UIManager] ItemSlot prefab not found!");
            return;
        }

        // 清除旧的槽位
        foreach (var obj in slotUIObjects)
        {
            if (obj != null) Destroy(obj);
        }
        slotUIObjects.Clear();

        // 获取背包数据
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[UIManager] InventorySystem.Instance is null!");
            return;
        }

        List<InventorySlot> slots = InventorySystem.Instance.GetSlots();
        Debug.Log($"[UIManager] Creating {slots.Count} slots");

        // 创建新的槽位
        for (int i = 0; i < slots.Count; i++)
        {
            Transform targetContainer = (i < 6) ? slotContainer : secondSlotContainer;

            if (targetContainer == null && i >= 6)
            {
                Debug.LogWarning($"[UIManager] SecondSlotContainer not found, skipping slot {i}");
                continue;
            }

            GameObject slotGO = Instantiate(prefab, targetContainer);
            slotUIObjects.Add(slotGO);

            // 获取图标
            Transform iconTransform = slotGO.transform.Find("ItemIcon");
            if (iconTransform == null)
            {
                Debug.LogError($"[UIManager] ItemIcon not found in slot {i}");
                continue;
            }

            Image iconImage = iconTransform.GetComponent<Image>();
            Button slotButton = slotGO.GetComponent<Button>();

            // 设置图标
            if (!slots[i].IsEmpty)
            {
                iconImage.sprite = slots[i].item.icon;
                iconImage.color = Color.white;
            }
            else
            {
                iconImage.sprite = null;
                iconImage.color = new Color(1, 1, 1, 0);
            }

            // 绑定点击事件
            int currentIndex = i;
            slotButton.onClick.AddListener(() => OnSlotClicked(currentIndex));
        }

        Debug.Log($"[UIManager] Created {slotUIObjects.Count} slot UI objects");
    }

    private void OnSlotClicked(int clickedIndex)
    {
        List<InventorySlot> slots = InventorySystem.Instance.GetSlots();

        if (selectedIndex == -1)
        {
            if (!slots[clickedIndex].IsEmpty)
            {
                SelectItem(clickedIndex);
            }
        }
        else
        {
            if (clickedIndex == selectedIndex)
            {
                DeselectItem();
            }
            else
            {
                InventorySystem.Instance.SwapItems(selectedIndex, clickedIndex);
                DeselectItem();
            }
        }
    }

    private void SelectItem(int index)
    {
        selectedIndex = index;
        if (index >= 0 && index < slotUIObjects.Count)
        {
            Transform iconTransform = slotUIObjects[index].transform.Find("ItemIcon");
            if (iconTransform != null)
            {
                Image selectedIcon = iconTransform.GetComponent<Image>();
                selectedIcon.color = Color.yellow; // 高亮显示
            }
        }
    }

    private void DeselectItem()
    {
        if (selectedIndex >= 0 && selectedIndex < slotUIObjects.Count)
        {
            List<InventorySlot> slots = InventorySystem.Instance.GetSlots();
            Transform iconTransform = slotUIObjects[selectedIndex].transform.Find("ItemIcon");
            if (iconTransform != null)
            {
                Image deselectedIcon = iconTransform.GetComponent<Image>();
                if (!slots[selectedIndex].IsEmpty)
                {
                    deselectedIcon.color = Color.white;
                }
                else
                {
                    deselectedIcon.color = new Color(1, 1, 1, 0);
                }
            }
        }
        selectedIndex = -1;
    }

    // ============ 背包展开/收起 ============

    private void ToggleInventoryExpansion()
    {
        if (isAnimating)
        {
            Debug.Log("[UIManager] Animation in progress, ignoring click");
            return;
        }

        isExpanded = !isExpanded;
        Debug.Log($"[UIManager] Toggling inventory expansion. isExpanded: {isExpanded}");
        StartCoroutine(AnimateInventorySlide());
    }

    private IEnumerator AnimateInventorySlide()
    {
        isAnimating = true;

        RectTransform inventoryRect = InventoryPanel?.GetComponent<RectTransform>();
        RectTransform secondRect = SecondColumnPanel?.GetComponent<RectTransform>();

        if (inventoryRect == null || secondRect == null)
        {
            Debug.LogError("[UIManager] InventoryPanel or SecondColumnPanel RectTransform not found!");
            isAnimating = false;
            yield break;
        }

        // 记录起始位置
        float inventoryStartX = inventoryRect.anchoredPosition.x;
        float secondStartX = secondRect.anchoredPosition.x;

        // 定义目标位置
        float inventoryTargetX, secondTargetX;

        if (isExpanded)
        {
            // 展开:InventoryPanel 向左移,SecondColumnPanel 滑入屏幕
            inventoryTargetX = -200f; // InventoryPanel 向左移动一列的宽度
            secondTargetX = 0f;       // SecondColumnPanel 移到右侧
        }
        else
        {
            // 收起:两者都向右移回原位
            inventoryTargetX = 0f;    // InventoryPanel 回到右侧
            secondTargetX = 200f;     // SecondColumnPanel 移出屏幕
        }

        float elapsedTime = 0f;

        // 平滑插值动画
        while (elapsedTime < expandAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / expandAnimationDuration);
            float easedT = expandCurve.Evaluate(t);

            // 同时移动两个面板
            inventoryRect.anchoredPosition = new Vector2(
                Mathf.Lerp(inventoryStartX, inventoryTargetX, easedT),
                inventoryRect.anchoredPosition.y
            );

            secondRect.anchoredPosition = new Vector2(
                Mathf.Lerp(secondStartX, secondTargetX, easedT),
                secondRect.anchoredPosition.y
            );

            yield return null;
        }

        // 确保最终位置准确
        inventoryRect.anchoredPosition = new Vector2(inventoryTargetX, inventoryRect.anchoredPosition.y);
        secondRect.anchoredPosition = new Vector2(secondTargetX, secondRect.anchoredPosition.y);

        // 更新按钮文字
        if (expandButtonText != null)
        {
            expandButtonText.text = isExpanded ? ">" : "<";
        }

        isAnimating = false;
        Debug.Log($"[UIManager] Inventory {(isExpanded ? "Expanded" : "Collapsed")}");
    }

    // ============ 其他UI方法 ============

    public void ShowPauseMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void HidePauseMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }
}