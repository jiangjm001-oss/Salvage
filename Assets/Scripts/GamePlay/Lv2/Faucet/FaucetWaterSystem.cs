// Assets/Scripts/GamePlay/FaucetWaterSystem.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 水龙头接水系统 - 主控制器
/// 
/// 交互流程：
/// 1. 水龙头：点击切换开/关状态（控制水流显示）
/// 2. 台面：选中空烧杯后点击，放置烧杯到台面上
/// 3. 放置的烧杯：
///    - 水龙头关闭时点击 → 拾取回空烧杯
///    - 水龙头打开时点击 → 拾取有水的烧杯
/// 
/// 场景结构：
/// 📁 FaucetSystem
///    ├─ Faucet（水龙头）      ← FaucetHandle 组件
///    ├─ WaterFlow（水流）     ← 初始隐藏
///    ├─ Counter（台面）       ← CounterTop 组件
///    └─ PlacedBeaker（放置的烧杯）← PlacedBeaker 组件，初始隐藏
/// </summary>
public class FaucetWaterSystem : MonoBehaviour
{
    public static FaucetWaterSystem Instance { get; private set; }

    [Header("基本信息")]
    public string systemID = "faucet_system_001";

    [Header("状态（只读）")]
    [SerializeField] private bool isFaucetOn = false;
    [SerializeField] private bool isBeakerPlaced = false;

    [Header("场景物体引用")]
    [Tooltip("水流效果物体")]
    public GameObject waterFlowObject;

    [Tooltip("放置在台面上的烧杯物体")]
    public GameObject placedBeakerObject;

    [Header("水流动画设置")]
    [Tooltip("水流的 SpriteRenderer（用于动画）")]
    public SpriteRenderer waterFlowRenderer;

    [Tooltip("水流淡入时间")]
    public float waterFadeInDuration = 0.3f;

    [Tooltip("水流淡出时间")]
    public float waterFadeOutDuration = 0.2f;

    [Header("物品设置")]
    [Tooltip("空烧杯物品数据")]
    public ItemData emptyBeakerItem;

    [Tooltip("有水烧杯物品数据")]
    public ItemData filledBeakerItem;

    [Header("烧杯精灵图")]
    [Tooltip("放置的空烧杯精灵图")]
    public Sprite placedEmptySprite;

    [Tooltip("放置的有水烧杯精灵图")]
    public Sprite placedFilledSprite;

    [Header("音效设置")]
    [Tooltip("水龙头打开音效")]
    public string faucetOnSound = "";

    [Tooltip("水龙头关闭音效")]
    public string faucetOffSound = "";

    [Tooltip("水流持续音效")]
    public string waterFlowLoopSound = "";

    [Tooltip("放置烧杯音效")]
    public string placeBeakerSound = "";

    [Tooltip("拾取烧杯音效")]
    public string pickupBeakerSound = "Audio/SFX/item_pickup";

    [Tooltip("水倒入烧杯音效")]
    public string waterFillSound = "";

    [Header("提示信息")]
    public string noBeakerSelectedHint = "需要选中烧杯才能放置";
    public string wrongItemHint = "这个放不上去";
    public string beakerAlreadyPlacedHint = "台面上已经有烧杯了";

    [Header("事件")]
    public UnityEvent OnFaucetTurnOn;
    public UnityEvent OnFaucetTurnOff;
    public UnityEvent OnBeakerPlaced;
    public UnityEvent OnBeakerPickedUp;
    public UnityEvent OnFilledBeakerPickedUp;

    // 私有变量
    private SpriteRenderer placedBeakerRenderer;
    private Coroutine waterAnimationCoroutine;
    private AudioSource loopingWaterAudio;

    // ============ 属性访问器 ============
    public bool IsFaucetOn => isFaucetOn;
    public bool IsBeakerPlaced => isBeakerPlaced;

    private void Awake()
    {
        // 简单的单例（场景内）
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[FaucetWaterSystem] 场景中存在多个实例！");
        }
        Instance = this;

        // 获取放置烧杯的渲染器
        if (placedBeakerObject != null)
        {
            placedBeakerRenderer = placedBeakerObject.GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        // 初始化状态
        InitializeState();
    }

    /// <summary>
    /// 初始化所有物体状态
    /// </summary>
    private void InitializeState()
    {
        // 水流初始隐藏
        if (waterFlowObject != null)
        {
            waterFlowObject.SetActive(false);
        }

        // 如果水流有渲染器，初始化透明度
        if (waterFlowRenderer != null)
        {
            Color c = waterFlowRenderer.color;
            c.a = 0f;
            waterFlowRenderer.color = c;
        }

        // 放置的烧杯初始隐藏（除非已放置）
        if (placedBeakerObject != null && !isBeakerPlaced)
        {
            placedBeakerObject.SetActive(false);
        }

        // 如果水龙头是开的，显示水流
        if (isFaucetOn)
        {
            ShowWaterFlow(false); // 不播放动画，直接显示
        }

        // 更新烧杯外观
        UpdatePlacedBeakerAppearance();
    }

    // ============ 水龙头控制 ============

    /// <summary>
    /// 切换水龙头开关状态
    /// </summary>
    public void ToggleFaucet()
    {
        if (isFaucetOn)
        {
            TurnOffFaucet();
        }
        else
        {
            TurnOnFaucet();
        }
    }

    /// <summary>
    /// 打开水龙头
    /// </summary>
    public void TurnOnFaucet()
    {
        if (isFaucetOn) return;

        Debug.Log("[FaucetWaterSystem] 打开水龙头");
        isFaucetOn = true;

        // 显示水流（带动画）
        ShowWaterFlow(true);

        // 播放音效
        PlaySound(faucetOnSound);
        StartWaterLoopSound();

        // 如果烧杯已放置，更新外观（变成有水的）
        if (isBeakerPlaced)
        {
            StartCoroutine(FillBeakerWithWater());
        }

        // 触发事件
        OnFaucetTurnOn?.Invoke();

        // 保存
        SaveProgress();
    }

    /// <summary>
    /// 关闭水龙头
    /// </summary>
    public void TurnOffFaucet()
    {
        if (!isFaucetOn) return;

        Debug.Log("[FaucetWaterSystem] 关闭水龙头");
        isFaucetOn = false;

        // 隐藏水流（带动画）
        HideWaterFlow(true);

        // 停止水流音效
        StopWaterLoopSound();

        // 播放关闭音效
        PlaySound(faucetOffSound);

        // 触发事件
        OnFaucetTurnOff?.Invoke();

        // 保存
        SaveProgress();
    }

    /// <summary>
    /// 显示水流
    /// </summary>
    private void ShowWaterFlow(bool animate)
    {
        if (waterFlowObject == null) return;

        // 停止之前的动画
        if (waterAnimationCoroutine != null)
        {
            StopCoroutine(waterAnimationCoroutine);
        }

        waterFlowObject.SetActive(true);

        if (animate && waterFlowRenderer != null)
        {
            waterAnimationCoroutine = StartCoroutine(AnimateWaterFlow(true));
        }
        else if (waterFlowRenderer != null)
        {
            // 直接设置为完全可见
            Color c = waterFlowRenderer.color;
            c.a = 1f;
            waterFlowRenderer.color = c;
        }
    }

    /// <summary>
    /// 隐藏水流
    /// </summary>
    private void HideWaterFlow(bool animate)
    {
        if (waterFlowObject == null) return;

        // 停止之前的动画
        if (waterAnimationCoroutine != null)
        {
            StopCoroutine(waterAnimationCoroutine);
        }

        if (animate && waterFlowRenderer != null)
        {
            waterAnimationCoroutine = StartCoroutine(AnimateWaterFlow(false));
        }
        else
        {
            waterFlowObject.SetActive(false);
            if (waterFlowRenderer != null)
            {
                Color c = waterFlowRenderer.color;
                c.a = 0f;
                waterFlowRenderer.color = c;
            }
        }
    }

    /// <summary>
    /// 水流淡入淡出动画
    /// </summary>
    private IEnumerator AnimateWaterFlow(bool fadeIn)
    {
        if (waterFlowRenderer == null) yield break;

        float duration = fadeIn ? waterFadeInDuration : waterFadeOutDuration;
        float startAlpha = waterFlowRenderer.color.a;
        float endAlpha = fadeIn ? 1f : 0f;

        float elapsed = 0f;
        Color color = waterFlowRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 使用平滑曲线
            t = fadeIn ? Mathf.SmoothStep(0f, 1f, t) : Mathf.SmoothStep(1f, 0f, 1f - t);

            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            waterFlowRenderer.color = color;

            yield return null;
        }

        // 确保最终值
        color.a = endAlpha;
        waterFlowRenderer.color = color;

        // 如果是淡出，隐藏物体
        if (!fadeIn)
        {
            waterFlowObject.SetActive(false);
        }

        waterAnimationCoroutine = null;
    }

    // ============ 烧杯放置 ============

    /// <summary>
    /// 尝试在台面上放置烧杯
    /// </summary>
    /// <returns>是否成功放置</returns>
    public bool TryPlaceBeaker()
    {
        Debug.Log("[FaucetWaterSystem] 尝试放置烧杯");

        // 检查是否已经放置了烧杯
        if (isBeakerPlaced)
        {
            ShowHint(beakerAlreadyPlacedHint);
            return false;
        }

        // 检查 UIManager
        if (UIManager.Instance == null)
        {
            Debug.LogError("[FaucetWaterSystem] UIManager 未找到！");
            return false;
        }

        // 检查是否选中了物品
        if (!UIManager.Instance.HasSelectedItem())
        {
            ShowHint(noBeakerSelectedHint);
            return false;
        }

        // 获取选中的物品
        ItemData selectedItem = UIManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            ShowHint(noBeakerSelectedHint);
            return false;
        }

        // 检查是否是空烧杯
        if (emptyBeakerItem == null)
        {
            Debug.LogError("[FaucetWaterSystem] 未设置 emptyBeakerItem！");
            return false;
        }

        if (selectedItem.itemID != emptyBeakerItem.itemID)
        {
            ShowHint(wrongItemHint);
            return false;
        }

        // 执行放置
        PlaceBeaker();
        return true;
    }

    /// <summary>
    /// 放置烧杯
    /// </summary>
    private void PlaceBeaker()
    {
        Debug.Log("[FaucetWaterSystem] 放置烧杯到台面");

        // 从背包移除空烧杯
        UIManager.Instance.ConsumeSelectedItem();

        // 设置状态
        isBeakerPlaced = true;

        // 显示放置的烧杯
        if (placedBeakerObject != null)
        {
            placedBeakerObject.SetActive(true);
        }

        // 播放放置音效
        PlaySound(placeBeakerSound);

        // 如果水龙头是开的，播放接水动画
        if (isFaucetOn)
        {
            StartCoroutine(FillBeakerWithWater());
        }
        else
        {
            // 显示空烧杯外观
            UpdatePlacedBeakerAppearance();
        }

        // 触发事件
        OnBeakerPlaced?.Invoke();

        // 保存
        SaveProgress();
    }

    /// <summary>
    /// 烧杯接水动画
    /// </summary>
    private IEnumerator FillBeakerWithWater()
    {
        // 播放接水音效
        PlaySound(waterFillSound);

        // 等待一小段时间模拟接水过程
        yield return new WaitForSeconds(0.5f);

        // 更新烧杯外观为有水状态
        UpdatePlacedBeakerAppearance();

        Debug.Log("[FaucetWaterSystem] 烧杯已接满水");
    }

    /// <summary>
    /// 更新放置烧杯的外观
    /// </summary>
    private void UpdatePlacedBeakerAppearance()
    {
        if (placedBeakerRenderer == null) return;
        if (!isBeakerPlaced) return;

        // 根据水龙头状态决定显示哪个精灵图
        Sprite targetSprite = isFaucetOn ? placedFilledSprite : placedEmptySprite;

        if (targetSprite != null)
        {
            placedBeakerRenderer.sprite = targetSprite;
        }
    }

    // ============ 烧杯拾取 ============

    /// <summary>
    /// 尝试拾取放置的烧杯
    /// </summary>
    /// <returns>是否成功拾取</returns>
    public bool TryPickupBeaker()
    {
        Debug.Log("[FaucetWaterSystem] 尝试拾取烧杯");

        // 检查是否有烧杯可拾取
        if (!isBeakerPlaced)
        {
            Debug.Log("[FaucetWaterSystem] 没有烧杯可拾取");
            return false;
        }

        // 检查背包系统
        if (InventorySystem.Instance == null)
        {
            Debug.LogError("[FaucetWaterSystem] InventorySystem 未找到！");
            return false;
        }

        // 根据水龙头状态决定拾取哪种烧杯
        ItemData itemToPickup = isFaucetOn ? filledBeakerItem : emptyBeakerItem;

        if (itemToPickup == null)
        {
            Debug.LogError("[FaucetWaterSystem] 未设置拾取物品！");
            return false;
        }

        // 尝试添加到背包
        bool added = InventorySystem.Instance.AddItem(itemToPickup);
        if (!added)
        {
            ShowHint("背包已满");
            return false;
        }

        // 执行拾取
        PickupBeaker(isFaucetOn);
        return true;
    }

    /// <summary>
    /// 拾取烧杯
    /// </summary>
    private void PickupBeaker(bool wasFilled)
    {
        string itemName = wasFilled ? "有水的烧杯" : "空烧杯";
        Debug.Log($"[FaucetWaterSystem] 拾取: {itemName}");

        // 隐藏放置的烧杯
        if (placedBeakerObject != null)
        {
            placedBeakerObject.SetActive(false);
        }

        // 更新状态
        isBeakerPlaced = false;

        // 播放拾取音效
        PlaySound(pickupBeakerSound);

        // 触发事件
        OnBeakerPickedUp?.Invoke();
        if (wasFilled)
        {
            OnFilledBeakerPickedUp?.Invoke();
        }

        // 保存
        SaveProgress();
    }

    // ============ 音效相关 ============

    private void PlaySound(string soundName)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundName))
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }

    private void StartWaterLoopSound()
    {
        if (string.IsNullOrEmpty(waterFlowLoopSound)) return;
        if (AudioManager.Instance == null) return;

        // 播放水流音效
        AudioManager.Instance.PlaySFX(waterFlowLoopSound);
    }

    private void StopWaterLoopSound()
    {
        // 如果 AudioManager 支持停止特定音效，在这里调用
    }

    // ============ 提示 ============

    private void ShowHint(string message)
    {
        Debug.Log($"[FaucetWaterSystem] 提示: {message}");
        // 可以接入 UI 提示系统
    }

    // ============ 存档相关 ============

    private void SaveProgress()
    {
        SaveLoadSystem.Instance?.SaveGame();
    }

    /// <summary>
    /// 获取存档数据
    /// </summary>
    public FaucetSystemSaveData GetSaveData()
    {
        return new FaucetSystemSaveData
        {
            systemID = this.systemID,
            isFaucetOn = this.isFaucetOn,
            isBeakerPlaced = this.isBeakerPlaced
        };
    }

    /// <summary>
    /// 从存档恢复状态
    /// </summary>
    public void RestoreFromSaveData(FaucetSystemSaveData data)
    {
        if (data == null) return;

        this.isFaucetOn = data.isFaucetOn;
        this.isBeakerPlaced = data.isBeakerPlaced;

        // 重新初始化状态
        InitializeState();

        Debug.Log($"[FaucetWaterSystem] 状态已恢复 - 水龙头: {(isFaucetOn ? "开" : "关")}, 烧杯: {(isBeakerPlaced ? "已放置" : "未放置")}");
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(systemID))
        {
            systemID = $"faucet_system_{GetInstanceID()}";
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制关联物体的连线
        Vector3 center = transform.position;

        if (waterFlowObject != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(center, waterFlowObject.transform.position);
            Gizmos.DrawWireSphere(waterFlowObject.transform.position, 0.15f);
        }

        if (placedBeakerObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(center, placedBeakerObject.transform.position);
            Gizmos.DrawWireSphere(placedBeakerObject.transform.position, 0.15f);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

/// <summary>
/// 水龙头系统存档数据
/// </summary>
[System.Serializable]
public class FaucetSystemSaveData
{
    public string systemID;
    public bool isFaucetOn;
    public bool isBeakerPlaced;
}