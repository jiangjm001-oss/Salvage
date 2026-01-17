// Assets/Scripts/GamePlay/CycleStateObject.cs
using UnityEngine;

/// <summary>
/// 循环状态物体 - 点击循环切换多个状态
/// 适用场景：天线、开关、旋钮等需要循环切换的物体
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Collider2D))]
public class CycleStateObject : MonoBehaviour
{
    [Header("基本信息")]
    [Tooltip("物体唯一标识符（用于存档）")]
    public string objectID;

    [Header("状态配置")]
    [Tooltip("所有状态的精灵图（按顺序：A → B → C → ...）")]
    public Sprite[] stateSprites;

    [Tooltip("切换状态时播放的音效")]
    public string switchSoundPath = "Audio/SFX/switch";

    [Header("运行时状态（只读）")]
    [SerializeField]
    private int currentStateIndex = 0;

    // 组件缓存
    private SpriteRenderer spriteRenderer;

    /// <summary>
    /// 获取当前状态索引（供其他组件查询）
    /// </summary>
    public int CurrentStateIndex => currentStateIndex;

    /// <summary>
    /// 获取状态总数
    /// </summary>
    public int StateCount => stateSprites != null ? stateSprites.Length : 0;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 确保初始状态正确显示
        UpdateSprite();
    }

    /// <summary>
    /// 切换到下一个状态（由 InteractionSystem 或其他方式调用）
    /// </summary>
    public void CycleToNextState()
    {
        if (stateSprites == null || stateSprites.Length == 0)
        {
            Debug.LogWarning($"[CycleStateObject] '{gameObject.name}' 没有配置状态精灵图！");
            return;
        }

        // 循环切换
        currentStateIndex = (currentStateIndex + 1) % stateSprites.Length;

        UpdateSprite();
        PlaySwitchSound();

        Debug.Log($"[CycleStateObject] '{gameObject.name}' 切换到状态 {currentStateIndex}");

        // 保存游戏
        if (SaveLoadSystem.Instance != null)
        {
            SaveLoadSystem.Instance.SaveGame();
        }
    }

    /// <summary>
    /// 设置到指定状态（用于存档恢复）
    /// </summary>
    public void SetState(int stateIndex)
    {
        if (stateSprites == null || stateSprites.Length == 0) return;

        currentStateIndex = Mathf.Clamp(stateIndex, 0, stateSprites.Length - 1);
        UpdateSprite();
    }

    /// <summary>
    /// 更新精灵图显示
    /// </summary>
    private void UpdateSprite()
    {
        if (spriteRenderer != null && stateSprites != null && currentStateIndex < stateSprites.Length)
        {
            spriteRenderer.sprite = stateSprites[currentStateIndex];
        }
    }

    /// <summary>
    /// 播放切换音效
    /// </summary>
    private void PlaySwitchSound()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(switchSoundPath))
        {
            AudioManager.Instance.PlaySFX(switchSoundPath);
        }
    }

    // ============ 点击检测（独立处理，不依赖 InteractableObject）============

    private void OnMouseDown()
    {
        // 检查是否点击在 UI 上
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        CycleToNextState();
    }
}