// Assets/Scripts/Managers/UIManager.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI 面板")]
    public GameObject pauseMenuPanel;

    [Header("背包动画设置")]
    [SerializeField] private float slideAnimationDuration = 0.3f; // 动画持续时间
    [SerializeField] private AnimationCurve slideEaseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 缓动曲线

    private List<GameObject> slotUIObjects = new List<GameObject>();
    private int selectedIndex = -1;
    private bool isExpanded = false;
    private bool isAnimating = false; // 防止动画期间重复点击

    // ✅ 修改：需要获取 RectTransform 来做动画
    private GameObject InventoryPanel
    {
        get
        {
            if (Instance == null) return null;
            Transform panelTransform = Instance.transform.Find("UICanvas/InventoryPanel");
            return panelTransform?.gameObject;
        }
    }

    private RectTransform InventoryPanelRect => InventoryPanel?.GetComponent<RectTransform>();

    private Transform SlotContainer
    {
        get
        {
            var panel = InventoryPanel;
            if (panel == null) return null;
            return panel.transform.Find("SlotContainer");
        }
    }

    // ✅ 修改：SecondColumnPanel 现在是 UICanvas 的子对象
    private GameObject SecondColumnPanel
    {
        get
        {
            if (Instance == null) return null;
            Transform panelTransform = Instance.transform.Find("UICanvas/SecondColumnPanel");
            return panelTransform?.gameObject;
        }
    }

    private RectTransform SecondColumnPanelRect => SecondColumnPanel?.GetComponent<RectTransform>();

    private Transform SecondSlotContainer
    {
        get
        {
            var panel = SecondColumnPanel;
            if (panel == null) return null;
            return panel.transform.Find("SecondSlotContainer");
        }
    }

    private GameObject ItemSlotPrefab => Resources.Load<GameObject>("Prefabs/UI/ItemSlot");

    private Button ExpandButton
    {
        get
        {
            var panel = InventoryPanel;
            if (panel == null) return null;
            Transform buttonTransform = panel.transform.Find("ExpandButton");
            return buttonTransform?.GetComponent<Button>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else
        {
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged.AddListener(UpdateInventoryUI);
        }
        if (ExpandButton != null)
        {
            ExpandButton.onClick.AddListener(ToggleInventoryExpansion);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.AddListener(OnViewStateChanged);
        }
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged.RemoveListener(UpdateInventoryUI);
        }
        if (ExpandButton != null)
        {
            ExpandButton.onClick.RemoveListener(ToggleInventoryExpansion);
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnViewStateChanged.RemoveListener(OnViewStateChanged);
        }
    }

    private void Start()
    {
        if (InventorySystem.Instance != null)
        {
            InventorySystem.Instance.OnInventoryChanged.AddListener(UpdateInventoryUI);
        }

        if (InventoryPanel != null)
        {
            InventoryPanel.SetActive(false);

            // ✅ 设置初始位置
            if (SecondColumnPanel != null)
            {
                SecondColumnPanel.SetActive(true); // 必须激活才能设置位置
                RectTransform secondRect = SecondColumnPanelRect;
                if (secondRect != null)
                {
                    // 初始位置：屏幕外右侧
                    secondRect.anchoredPosition = new Vector2(200, secondRect.anchoredPosition.y);
                }
            }

            UpdateInventoryUI();
        }
        else
        {
            Debug.LogError("UIManager: Could not find InventoryPanel dynamically!");
        }

        if (GameManager.Instance != null)
        {
            OnViewStateChanged(GameManager.Instance.CurrentViewState);
        }
    }

    // 新增：响应视图状态变化
    private void OnViewStateChanged(GameManager.ViewState newState)
    {
        // 根据视图状态决定是否显示背包
        bool isInGameplayView = newState == GameManager.ViewState.Wall_A ||
                               newState == GameManager.ViewState.Wall_B ||
                               newState == GameManager.ViewState.Wall_C ||
                               newState == GameManager.ViewState.Wall_D;

        if (InventoryPanel != null)
        {
            InventoryPanel.SetActive(isInGameplayView);
            if (SecondColumnPanel != null)
            {
                SecondColumnPanel.SetActive(isInGameplayView);
            }
        }

        // 如果是放大视图，可以在这里添加特定UI处理
        if (!isInGameplayView)
        {
            // 可能需要隐藏某些UI元素或显示返回按钮
            Debug.Log($"UIManager: In zoom view {newState}, adjusting UI accordingly");
        }
    }

    #region 背包UI逻辑
    private void UpdateInventoryUI()
    {
        var inventoryPanel = InventoryPanel;
        var slotContainer = SlotContainer;
        var secondSlotContainer = SecondSlotContainer;
        var itemSlotPrefab = ItemSlotPrefab;

        if (inventoryPanel == null || slotContainer == null || itemSlotPrefab == null)
        {
            Debug.LogError($"UIManager.UpdateInventoryUI: Could not find required UI elements.");
            return;
        }

        foreach (var obj in slotUIObjects)
        {
            if (obj != null) Destroy(obj);
        }
        slotUIObjects.Clear();

        List<InventorySlot> slots = InventorySystem.Instance.GetSlots();
        for (int i = 0; i < slots.Count; i++)
        {
            Transform targetContainer = (i < 6) ? slotContainer : secondSlotContainer;

            if (targetContainer == null && i >= 6)
            {
                Debug.LogWarning("UIManager: SecondSlotContainer not found, skipping slots 6-11.");
                continue;
            }

            GameObject slotGO = Instantiate(itemSlotPrefab, targetContainer);
            slotUIObjects.Add(slotGO);

            Transform iconTransform = slotGO.transform.Find("ItemIcon");
            if (iconTransform == null)
            {
                Debug.LogError($"UIManager: Could not find 'ItemIcon' child on ItemSlot prefab.", slotGO);
                continue;
            }
            Image iconImage = iconTransform.GetComponent<Image>();
            Button slotButton = slotGO.GetComponent<Button>();

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

            int currentIndex = i;
            slotButton.onClick.AddListener(() => OnSlotClicked(currentIndex));
        }
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
                selectedIcon.color = Color.black;
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

    // ✅ 修改：添加滑动动画
    private void ToggleInventoryExpansion()
    {
        if (isAnimating) return; // 动画期间不响应点击

        isExpanded = !isExpanded;
        StartCoroutine(AnimateInventorySlide());
    }

    // ✅ 新增：滑动动画协程
    private IEnumerator AnimateInventorySlide()
    {
        isAnimating = true;

        RectTransform inventoryRect = InventoryPanelRect;
        RectTransform secondRect = SecondColumnPanelRect;

        if (inventoryRect == null || secondRect == null)
        {
            Debug.LogError("UIManager: Cannot animate - RectTransform not found.");
            isAnimating = false;
            yield break;
        }

        // 定义起始和目标位置
        float inventoryStartX = inventoryRect.anchoredPosition.x;
        float secondStartX = secondRect.anchoredPosition.x;

        float inventoryTargetX, secondTargetX;

        if (isExpanded)
        {
            // 展开：InventoryPanel 向左移，SecondColumnPanel 滑入屏幕
            inventoryTargetX = -200f; // InventoryPanel 向左移动一列的宽度
            secondTargetX = 0f;       // SecondColumnPanel 移到右侧
        }
        else
        {
            // 收起：两者都向右移回原位
            inventoryTargetX = 0f;    // InventoryPanel 回到右侧
            secondTargetX = 200f;     // SecondColumnPanel 移出屏幕
        }

        float elapsedTime = 0f;

        // 平滑插值动画
        while (elapsedTime < slideAnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / slideAnimationDuration);
            float easedT = slideEaseCurve.Evaluate(t);

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

        // 更新箭头方向
        var expandButton = ExpandButton;
        if (expandButton != null)
        {
            var textComponent = expandButton.GetComponentInChildren<TMPro.TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = isExpanded ? ">" : "<";
            }
        }

        isAnimating = false;
        Debug.Log($"UIManager: Inventory {(isExpanded ? "Expanded" : "Collapsed")}");
    }
    #endregion

    #region 通用UI方法
    public void ShowPauseMenu()
    {
        Debug.Log("UIManager: Showing Pause Menu.");
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);
    }

    public void HidePauseMenu()
    {
        Debug.Log("UIManager: Hiding Pause Menu.");
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
    }

    public void HideAllUI()
    {
        Debug.Log("UIManager: Hiding all UI elements.");
        HidePauseMenu();
    }

    public void ShowNotification(string message)
    {
        Debug.Log($"UIManager Notification: {message}");
    }
    #endregion
}