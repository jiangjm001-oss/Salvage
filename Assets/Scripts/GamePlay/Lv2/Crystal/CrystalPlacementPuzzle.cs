// Assets/Scripts/GamePlay/CrystalPlacementPuzzle.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 水晶碎片放置谜题 - 主控制器
/// 管理4个放置槽位，全部放置后浮现第5个可拾取的水晶
/// </summary>
public class CrystalPlacementPuzzle : MonoBehaviour
{
    [Header("谜题标识")]
    [Tooltip("谜题唯一ID（用于存档）")]
    public string puzzleID = "crystal_placement";

    [Header("放置槽位")]
    [Tooltip("拖入4个CrystalSlot子物体")]
    public CrystalSlot[] crystalSlots = new CrystalSlot[4];

    [Header("第五水晶设置")]
    [Tooltip("第五水晶的显示物体")]
    public GameObject fifthCrystalObject;

    [Tooltip("第五水晶的物品数据（用于拾取）")]
    public ItemData fifthCrystalItem;

    [Tooltip("第五水晶的SpriteRenderer（用于动画）")]
    public SpriteRenderer fifthCrystalRenderer;

    [Header("第五水晶浮现动画")]
    [Tooltip("浮现动画持续时间")]
    public float appearDuration = 1.5f;

    [Tooltip("初始缩放")]
    public float startScale = 0f;

    [Tooltip("最终缩放")]
    public float endScale = 1f;

    [Tooltip("缩放动画曲线")]
    public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("浮现时的发光颜色")]
    public Color glowColor = new Color(0.5f, 0.8f, 1f, 1f);

    [Tooltip("发光强度（HDR亮度倍数）")]
    [Range(1f, 5f)]
    public float glowIntensity = 2f;

    [Header("第五水晶发光循环")]
    [Tooltip("可拾取时是否持续发光")]
    public bool pulseWhenPickable = true;

    [Tooltip("发光脉冲速度")]
    public float pulseSpeed = 2f;

    [Tooltip("脉冲最小亮度")]
    [Range(0f, 1f)]
    public float pulseMinBrightness = 0.6f;

    [Header("音效")]
    [Tooltip("放置水晶音效")]
    public string placeSoundPath = "Audio/SFX/crystal_place";

    [Tooltip("第五水晶浮现音效")]
    public string appearSoundPath = "Audio/SFX/crystal_appear";

    [Tooltip("拾取第五水晶音效")]
    public string pickupSoundPath = "Audio/SFX/item_pickup";

    [Header("事件")]
    public UnityEvent OnAllPlaced;
    public UnityEvent OnFifthCrystalAppear;
    public UnityEvent OnFifthCrystalPickup;

    [Header("状态（只读）")]
    [SerializeField] private int placedCount = 0;
    [SerializeField] private bool allPlaced = false;
    [SerializeField] private bool fifthCrystalAvailable = false;
    [SerializeField] private bool fifthCrystalPickedUp = false;

    // 私有变量
    private Color originalFifthColor;
    private Coroutine pulseCoroutine;
    private Collider2D fifthCrystalCollider;
    private bool isInitialized = false;

    private void Start()
    {
        Initialize();
        LoadState();
    }

    /// <summary>
    /// 初始化谜题
    /// </summary>
    private void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        // 初始化所有槽位
        for (int i = 0; i < crystalSlots.Length; i++)
        {
            if (crystalSlots[i] != null)
            {
                crystalSlots[i].Initialize(this, i);
            }
        }

        // 初始化第五水晶
        if (fifthCrystalObject != null)
        {
            fifthCrystalObject.SetActive(false);

            // 获取或添加碰撞器
            fifthCrystalCollider = fifthCrystalObject.GetComponent<Collider2D>();
            if (fifthCrystalCollider == null)
            {
                fifthCrystalCollider = fifthCrystalObject.AddComponent<BoxCollider2D>();
            }
            fifthCrystalCollider.enabled = false;

            // 保存原始颜色
            if (fifthCrystalRenderer != null)
            {
                originalFifthColor = fifthCrystalRenderer.color;
            }
        }

        Debug.Log($"[CrystalPlacementPuzzle] 初始化完成，共 {crystalSlots.Length} 个槽位");
    }

    /// <summary>
    /// 槽位放置回调（由CrystalSlot调用）
    /// </summary>
    public void OnSlotPlaced(int slotIndex)
    {
        placedCount++;
        Debug.Log($"[CrystalPlacementPuzzle] 槽位 {slotIndex} 已放置，当前进度: {placedCount}/{crystalSlots.Length}");

        // 播放放置音效
        PlaySound(placeSoundPath);

        // 保存状态
        SaveState();

        // 检查是否全部放置
        CheckAllPlaced();
    }

    /// <summary>
    /// 检查是否全部放置完成
    /// </summary>
    private void CheckAllPlaced()
    {
        if (allPlaced) return;

        int actualPlaced = 0;
        foreach (var slot in crystalSlots)
        {
            if (slot != null && slot.IsPlaced)
            {
                actualPlaced++;
            }
        }

        placedCount = actualPlaced;

        if (placedCount >= crystalSlots.Length)
        {
            allPlaced = true;
            Debug.Log("[CrystalPlacementPuzzle] ✓ 全部水晶已放置！");

            OnAllPlaced?.Invoke();

            // 浮现第五水晶
            StartCoroutine(AppearFifthCrystalCoroutine());
        }
    }

    /// <summary>
    /// 第五水晶浮现动画协程
    /// </summary>
    private IEnumerator AppearFifthCrystalCoroutine()
    {
        if (fifthCrystalObject == null)
        {
            Debug.LogWarning("[CrystalPlacementPuzzle] 未设置第五水晶物体！");
            yield break;
        }

        // 播放浮现音效
        PlaySound(appearSoundPath);

        // 显示物体，初始缩放为0
        fifthCrystalObject.SetActive(true);
        fifthCrystalObject.transform.localScale = Vector3.one * startScale;

        // 设置初始发光颜色
        if (fifthCrystalRenderer != null)
        {
            fifthCrystalRenderer.color = glowColor * glowIntensity;
        }

        float elapsed = 0f;

        while (elapsed < appearDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / appearDuration;

            // 缩放动画
            float scale = Mathf.Lerp(startScale, endScale, scaleCurve.Evaluate(t));
            fifthCrystalObject.transform.localScale = Vector3.one * scale;

            // 发光渐变回正常颜色（后半段）
            if (fifthCrystalRenderer != null && t > 0.5f)
            {
                float colorT = (t - 0.5f) * 2f; // 0~1
                fifthCrystalRenderer.color = Color.Lerp(glowColor * glowIntensity, originalFifthColor, colorT);
            }

            yield return null;
        }

        // 确保最终状态
        fifthCrystalObject.transform.localScale = Vector3.one * endScale;
        if (fifthCrystalRenderer != null)
        {
            fifthCrystalRenderer.color = originalFifthColor;
        }

        // 启用可拾取
        fifthCrystalAvailable = true;
        if (fifthCrystalCollider != null)
        {
            fifthCrystalCollider.enabled = true;
        }

        OnFifthCrystalAppear?.Invoke();

        Debug.Log("[CrystalPlacementPuzzle] ✓ 第五水晶已浮现，可以拾取");

        // 开始发光脉冲
        if (pulseWhenPickable)
        {
            pulseCoroutine = StartCoroutine(PulseGlowCoroutine());
        }

        // 保存状态
        SaveState();
    }

    /// <summary>
    /// 发光脉冲协程（可拾取提示）
    /// </summary>
    private IEnumerator PulseGlowCoroutine()
    {
        if (fifthCrystalRenderer == null) yield break;

        while (fifthCrystalAvailable && !fifthCrystalPickedUp)
        {
            float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0~1
            float brightness = Mathf.Lerp(pulseMinBrightness, 1f, t);

            Color pulseColor = new Color(
                originalFifthColor.r * brightness + glowColor.r * (1f - brightness) * 0.3f,
                originalFifthColor.g * brightness + glowColor.g * (1f - brightness) * 0.3f,
                originalFifthColor.b * brightness + glowColor.b * (1f - brightness) * 0.3f,
                originalFifthColor.a
            );

            fifthCrystalRenderer.color = pulseColor;

            yield return null;
        }

        // 恢复原始颜色
        if (fifthCrystalRenderer != null)
        {
            fifthCrystalRenderer.color = originalFifthColor;
        }
    }

    /// <summary>
    /// 尝试拾取第五水晶（由点击检测调用）
    /// </summary>
    public void TryPickupFifthCrystal()
    {
        if (!fifthCrystalAvailable || fifthCrystalPickedUp)
        {
            Debug.Log("[CrystalPlacementPuzzle] 第五水晶不可拾取");
            return;
        }

        if (fifthCrystalItem == null)
        {
            Debug.LogError("[CrystalPlacementPuzzle] 未设置第五水晶的ItemData！");
            return;
        }

        // 添加到背包
        bool added = InventorySystem.Instance.AddItem(fifthCrystalItem);
        if (added)
        {
            fifthCrystalPickedUp = true;
            fifthCrystalAvailable = false;

            // 停止发光脉冲
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
                pulseCoroutine = null;
            }

            // 播放拾取音效
            PlaySound(pickupSoundPath);

            // 隐藏水晶
            StartCoroutine(PickupFadeOutCoroutine());

            OnFifthCrystalPickup?.Invoke();

            Debug.Log($"[CrystalPlacementPuzzle] ✓ 拾取第五水晶: {fifthCrystalItem.displayName}");

            // 保存状态
            SaveState();
            SaveLoadSystem.Instance?.SaveGame();
        }
    }

    /// <summary>
    /// 拾取时淡出动画
    /// </summary>
    private IEnumerator PickupFadeOutCoroutine()
    {
        if (fifthCrystalRenderer == null)
        {
            fifthCrystalObject.SetActive(false);
            yield break;
        }

        float duration = 0.3f;
        float elapsed = 0f;
        Color startColor = fifthCrystalRenderer.color;

        // 禁用碰撞
        if (fifthCrystalCollider != null)
        {
            fifthCrystalCollider.enabled = false;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // 淡出 + 缩小
            fifthCrystalRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            fifthCrystalObject.transform.localScale = Vector3.one * endScale * (1f - t * 0.5f);

            yield return null;
        }

        fifthCrystalObject.SetActive(false);
    }

    /// <summary>
    /// 检查是否可以在指定槽位放置指定物品
    /// </summary>
    public bool CanPlaceItem(string itemID, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= crystalSlots.Length) return false;

        var slot = crystalSlots[slotIndex];
        if (slot == null || slot.IsPlaced) return false;

        return slot.RequiredItemID == itemID;
    }

    // ============ 存档功能 ============

    private void SaveState()
    {
        string key = $"Puzzle_{puzzleID}";

        // 构建槽位状态字符串
        List<string> slotStates = new List<string>();
        foreach (var slot in crystalSlots)
        {
            slotStates.Add(slot != null && slot.IsPlaced ? "1" : "0");
        }

        string data = string.Join(",", slotStates) +
                      $"|{(allPlaced ? 1 : 0)}" +
                      $"|{(fifthCrystalAvailable ? 1 : 0)}" +
                      $"|{(fifthCrystalPickedUp ? 1 : 0)}";

        PlayerPrefs.SetString(key, data);
        PlayerPrefs.Save();

        Debug.Log($"[CrystalPlacementPuzzle] 状态已保存: {data}");
    }

    private void LoadState()
    {
        string key = $"Puzzle_{puzzleID}";
        if (!PlayerPrefs.HasKey(key)) return;

        string data = PlayerPrefs.GetString(key);
        string[] parts = data.Split('|');

        if (parts.Length >= 4)
        {
            // 恢复槽位状态
            string[] slotStates = parts[0].Split(',');
            for (int i = 0; i < slotStates.Length && i < crystalSlots.Length; i++)
            {
                if (slotStates[i] == "1" && crystalSlots[i] != null)
                {
                    crystalSlots[i].RestorePlaced();
                    placedCount++;
                }
            }

            // 恢复整体状态
            allPlaced = parts[1] == "1";
            fifthCrystalAvailable = parts[2] == "1";
            fifthCrystalPickedUp = parts[3] == "1";

            // 恢复第五水晶显示状态
            if (fifthCrystalObject != null)
            {
                if (fifthCrystalPickedUp)
                {
                    fifthCrystalObject.SetActive(false);
                }
                else if (fifthCrystalAvailable)
                {
                    fifthCrystalObject.SetActive(true);
                    fifthCrystalObject.transform.localScale = Vector3.one * endScale;
                    if (fifthCrystalCollider != null)
                    {
                        fifthCrystalCollider.enabled = true;
                    }

                    // 开始发光脉冲
                    if (pulseWhenPickable)
                    {
                        pulseCoroutine = StartCoroutine(PulseGlowCoroutine());
                    }
                }
            }

            Debug.Log($"[CrystalPlacementPuzzle] 状态已恢复: placed={placedCount}, allPlaced={allPlaced}, available={fifthCrystalAvailable}, pickedUp={fifthCrystalPickedUp}");
        }
    }

    /// <summary>
    /// 重置谜题（用于调试）
    /// </summary>
    [ContextMenu("重置谜题")]
    public void ResetPuzzle()
    {
        // 停止协程
        StopAllCoroutines();
        pulseCoroutine = null;

        // 重置状态
        placedCount = 0;
        allPlaced = false;
        fifthCrystalAvailable = false;
        fifthCrystalPickedUp = false;

        // 重置所有槽位
        foreach (var slot in crystalSlots)
        {
            if (slot != null)
            {
                slot.ResetSlot();
            }
        }

        // 重置第五水晶
        if (fifthCrystalObject != null)
        {
            fifthCrystalObject.SetActive(false);
            fifthCrystalObject.transform.localScale = Vector3.one * startScale;

            if (fifthCrystalRenderer != null)
            {
                fifthCrystalRenderer.color = originalFifthColor;
            }
        }

        // 删除存档
        PlayerPrefs.DeleteKey($"Puzzle_{puzzleID}");
        PlayerPrefs.Save();

        Debug.Log("[CrystalPlacementPuzzle] 谜题已重置");
    }

    // ============ 辅助方法 ============

    private void PlaySound(string soundPath)
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(soundPath))
        {
            AudioManager.Instance.PlaySFX(soundPath);
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(puzzleID))
        {
            puzzleID = $"crystal_placement_{GetInstanceID()}";
        }
    }

    // ============ 编辑器辅助绘制 ============

    private void OnDrawGizmosSelected()
    {
        // 绘制槽位位置
        Gizmos.color = Color.cyan;
        foreach (var slot in crystalSlots)
        {
            if (slot != null)
            {
                Gizmos.DrawWireSphere(slot.transform.position, 0.3f);
            }
        }

        // 绘制第五水晶位置
        if (fifthCrystalObject != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(fifthCrystalObject.transform.position, 0.4f);
        }
    }
}