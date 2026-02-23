// Assets/Scripts/Managers/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

    [Header("选中状态颜色设置")]
    [Tooltip("物品选中时的变暗颜色")]
    [SerializeField] private Color selectedItemColor = new Color(0.6f, 0.6f, 0.6f, 1f);

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
            Debug.Log("[UIManager] Instance has been set.");
        }
        else
        {
            Debug.LogWarning($"[UIManager] Duplicate UIManager detected on {gameObject.name}! Destroying this component only.");
            Destroy(this);
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

    private void Update()
    {
        // ⭐ 新增：检测点击背包外部区域，自动收起背包
        HandleClickOutsideInventory();
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

    // ============ 点击背包外部检测（新增） ============

    /// <summary>
    /// 检测鼠标是否点击了背包区域外部，如果是则自动收起背包
    /// </summary>
    private void HandleClickOutsideInventory()
    {
        // 只有在背包展开状态下才需要检测
        if (!isExpanded) return;

        // 动画进行中不处理
        if (isAnimating) return;

        // 检测鼠标左键点击
        if (!Input.GetMouseButtonDown(0)) return;

        // 检查是否点击在UI上
        if (EventSystem.current == null) return;

        // 使用 Raycast 检测点击的UI元素
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, raycastResults);

        // 检查点击是否在背包相关UI上
        bool clickedOnInventory = false;

        foreach (var result in raycastResults)
        {
            // 检查点击的对象是否属于背包面板
            if (IsPartOfInventory(result.gameObject))
            {
                clickedOnInventory = true;
                break;
            }
        }

        // 如果点击不在背包区域内，则收起背包
        if (!clickedOnInventory)
        {
            Debug.Log("[UIManager] Clicked outside inventory, collapsing...");
            CollapseInventory();
        }
    }

    /// <summary>
    /// 判断一个GameObject是否属于背包UI的一部分
    /// </summary>
    private bool IsPartOfInventory(GameObject obj)
    {
        if (obj == null) return false;

        // 检查是否是 InventoryPanel 或其子对象
        if (InventoryPanel != null)
        {
            if (obj == InventoryPanel || obj.transform.IsChildOf(InventoryPanel.transform))
            {
                return true;
            }
        }

        // 检查是否是 SecondColumnPanel 或其子对象
        if (SecondColumnPanel != null)
        {
            if (obj == SecondColumnPanel || obj.transform.IsChildOf(SecondColumnPanel.transform))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 收起背包（仅当展开时）
    /// </summary>
    public void CollapseInventory()
    {
        if (!isExpanded || isAnimating) return;

        isExpanded = false;
        Debug.Log("[UIManager] Collapsing inventory");
        StartCoroutine(AnimateInventorySlide());
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

                // ⭐ 修改：检查是否是选中状态，应用对应颜色
                if (i == selectedIndex)
                {
                    iconImage.color = selectedItemColor; // 选中态为变暗颜色
                }
                else
                {
                    iconImage.color = Color.white; // 未选中为正常颜色
                }

                // 强制设置图标尺寸和缩放
                iconImage.rectTransform.sizeDelta = new Vector2(64, 64);
                iconImage.rectTransform.localScale = Vector3.one;
                iconImage.SetNativeSize();
                iconImage.rectTransform.sizeDelta = new Vector2(128, 128);
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

    // ============ 槽位点击处理（已优化） ============

    private void OnSlotClicked(int clickedIndex)
    {
        List<InventorySlot> slots = InventorySystem.Instance.GetSlots();

        // 情况1：当前没有选中任何物品
        if (selectedIndex == -1)
        {
            // 如果点击的槽位有物品，则选中它
            if (!slots[clickedIndex].IsEmpty)
            {
                SelectItem(clickedIndex);
            }
        }
        // 情况2：当前已有选中的物品
        else
        {
            // 2a：点击的是已选中的槽位 → 取消选中
            if (clickedIndex == selectedIndex)
            {
                DeselectItem();
            }
            // 2b：点击的是其他槽位
            else
            {
                // ⭐ 修改：检查目标槽位是否为空
                if (slots[clickedIndex].IsEmpty)
                {
                    // 目标槽位为空 → 交换位置（实际上是移动物品）
                    InventorySystem.Instance.SwapItems(selectedIndex, clickedIndex);
                    DeselectItem();
                }
                else
                {
                    // 目标槽位有物品 → 切换选中状态
                    // 先取消当前选中（恢复颜色）
                    DeselectItem();
                    // 再选中新的物品
                    SelectItem(clickedIndex);
                }
            }
        }
    }

    // ============ 选中/取消选中 ============

    private void SelectItem(int index)
    {
        selectedIndex = index;
        if (index >= 0 && index < slotUIObjects.Count)
        {
            Transform iconTransform = slotUIObjects[index].transform.Find("ItemIcon");
            if (iconTransform != null)
            {
                Image selectedIcon = iconTransform.GetComponent<Image>();
                // ⭐ 修改：选中态改为变暗颜色而非黄色高亮
                selectedIcon.color = selectedItemColor;
            }
        }
        Debug.Log($"[UIManager] Selected item at index {index}");
    }

    public void DeselectItem()
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
                    deselectedIcon.color = Color.white; // 恢复正常颜色
                }
                else
                {
                    deselectedIcon.color = new Color(1, 1, 1, 0); // 空槽位透明
                }
            }
        }
        Debug.Log($"[UIManager] Deselected item at index {selectedIndex}");
        selectedIndex = -1;
    }

    // ============ 物品选中状态访问 ============

    /// <summary>
    /// 获取当前选中的物品（如果有）
    /// </summary>
    public ItemData GetSelectedItem()
    {
        if (selectedIndex < 0) return null;

        var slots = InventorySystem.Instance?.GetSlots();
        if (slots == null) return null;

        if (selectedIndex >= slots.Count) return null;

        return slots[selectedIndex].item;
    }

    /// <summary>
    /// 获取当前选中物品的槽位索引
    /// </summary>
    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    /// <summary>
    /// 检查是否有物品被选中
    /// </summary>
    public bool HasSelectedItem()
    {
        return selectedIndex >= 0 && GetSelectedItem() != null;
    }

    /// <summary>
    /// 使用（消耗）当前选中的物品
    /// </summary>
    public void ConsumeSelectedItem()
    {
        if (selectedIndex < 0) return;

        var slots = InventorySystem.Instance?.GetSlots();
        if (slots == null) return;

        if (selectedIndex < slots.Count && !slots[selectedIndex].IsEmpty)
        {
            string itemName = slots[selectedIndex].item.displayName;
            string itemID = slots[selectedIndex].item.itemID;

            InventorySystem.Instance.RemoveItemByID(itemID);

            Debug.Log($"[UIManager] 消耗了物品: {itemName}");
        }

        DeselectItem();
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
            // 展开: InventoryPanel 向左移, SecondColumnPanel 滑入屏幕
            inventoryTargetX = -200f;
            secondTargetX = 0f;
        }
        else
        {
            // 收起: 两者都向右移回原位
            inventoryTargetX = 0f;
            secondTargetX = 200f;
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