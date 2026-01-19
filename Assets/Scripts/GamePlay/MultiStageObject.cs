// Assets/Scripts/GamePlay/MultiStageObject.cs
// 多阶段交互物体 - 支持按顺序使用多个物品进行状态切换
using UnityEngine;
using UnityEngine.Events;

public class MultiStageObject : MonoBehaviour
{
    [System.Serializable]
    public class Stage
    {
        [Tooltip("此阶段需要的物品")]
        public ItemData requiredItem;

        [Tooltip("完成此阶段后显示的精灵")]
        public Sprite resultSprite;

        [Tooltip("是否消耗物品")]
        public bool consumeItem = true;

        [Tooltip("完成此阶段后触发的事件")]
        public UnityEvent OnStageComplete;
    }

    [Header("基本设置")]
    [Tooltip("物体唯一ID（用于存档）")]
    public string objectID;

    [Tooltip("显示名称")]
    public string displayName;

    [Header("阶段配置")]
    [Tooltip("所有阶段（按顺序配置）")]
    public Stage[] stages;

    [Tooltip("当前阶段索引")]
    [HideInInspector]
    public int currentStage = 0;

    [Header("完成后设置")]
    [Tooltip("全部阶段完成后可拾取的物品")]
    public ItemData finalPickupItem;

    [Tooltip("全部完成后触发的事件")]
    public UnityEvent OnAllStagesComplete;

    [Header("音效")]
    public string stageSoundName = "item_used";
    public string pickupSoundName = "item_pickup";

    // 内部状态
    private SpriteRenderer spriteRenderer;
    private bool allStagesComplete = false;
    private bool hasBeenPickedUp = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {
        // 检查是否点击在UI上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        OnClick();
    }

    /// <summary>
    /// 点击交互（由 InteractionSystem 调用，或挂载 Collider + 自己检测）
    /// </summary>
    public void OnClick()
    {
        // 已拾取则忽略
        if (hasBeenPickedUp) return;

        // 全部完成 → 执行拾取
        if (allStagesComplete)
        {
            TryPickup();
            return;
        }

        // 尝试进入下一阶段
        TryAdvanceStage();
    }

    private void TryAdvanceStage()
    {
        if (currentStage >= stages.Length) return;

        // 获取当前阶段配置
        Stage stage = stages[currentStage];

        // 检查是否选中了正确物品
        if (UIManager.Instance == null) return;

        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            Debug.Log($"[MultiStageObject] '{displayName}' 需要物品: {stage.requiredItem?.displayName}");
            return;
        }

        if (selectedItem.itemID != stage.requiredItem.itemID)
        {
            Debug.Log($"[MultiStageObject] 物品不匹配，需要: {stage.requiredItem?.displayName}");
            return;
        }

        // ✓ 物品匹配，执行阶段切换
        Debug.Log($"[MultiStageObject] '{displayName}' 完成阶段 {currentStage}");

        // 消耗或取消选中物品
        if (stage.consumeItem)
            UIManager.Instance.ConsumeSelectedItem();
        else
            UIManager.Instance.DeselectItem();

        // 切换精灵
        if (spriteRenderer != null && stage.resultSprite != null)
        {
            spriteRenderer.sprite = stage.resultSprite;
        }

        // 播放音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(stageSoundName))
        {
            AudioManager.Instance.PlaySFX(stageSoundName);
        }

        // 触发阶段完成事件
        stage.OnStageComplete?.Invoke();

        // 进入下一阶段
        currentStage++;

        // 检查是否全部完成
        if (currentStage >= stages.Length)
        {
            allStagesComplete = true;
            Debug.Log($"[MultiStageObject] '{displayName}' 全部阶段完成！");
            OnAllStagesComplete?.Invoke();
        }

        // 保存进度
        SaveLoadSystem.Instance?.SaveGame();
    }

    private void TryPickup()
    {
        if (finalPickupItem == null)
        {
            Debug.LogWarning($"[MultiStageObject] '{displayName}' 没有配置 finalPickupItem");
            return;
        }

        bool added = InventorySystem.Instance.AddItem(finalPickupItem);
        if (added)
        {
            Debug.Log($"[MultiStageObject] 拾取: {finalPickupItem.displayName}");

            if (AudioManager.Instance != null && !string.IsNullOrEmpty(pickupSoundName))
            {
                AudioManager.Instance.PlaySFX(pickupSoundName);
            }

            hasBeenPickedUp = true;
            gameObject.SetActive(false);

            SaveLoadSystem.Instance?.OnItemPickedUp(objectID);
        }
    }

    // ============ 存档/读档支持 ============

    public int GetCurrentStage() => currentStage;
    public bool IsComplete() => allStagesComplete;
    public bool IsPickedUp() => hasBeenPickedUp;

    public void RestoreState(int stage, bool complete, bool pickedUp)
    {
        currentStage = stage;
        allStagesComplete = complete;
        hasBeenPickedUp = pickedUp;

        // 恢复精灵到对应阶段
        if (spriteRenderer != null && stage > 0 && stage <= stages.Length)
        {
            spriteRenderer.sprite = stages[stage - 1].resultSprite;
        }

        if (pickedUp)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"MultiStage_{gameObject.name}_{GetInstanceID()}";
        }
    }
}