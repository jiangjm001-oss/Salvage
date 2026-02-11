// Assets/Scripts/GamePlay/MultiStageObject.cs
// 多阶段交互物体 - 支持按顺序使用多个物品进行状态切换
// ⭐ 扩展版：支持每阶段独立音效 + 视觉动效
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class MultiStageObject : MonoBehaviour
{
    [System.Serializable]
    public class Stage
    {
        [Tooltip("此阶段需要的物品（留空 = 直接点击推进）")]
        public ItemData requiredItem;

        [Tooltip("完成此阶段后显示的精灵")]
        public Sprite resultSprite;

        [Tooltip("是否消耗物品")]
        public bool consumeItem = true;

        [Tooltip("完成此阶段后触发的事件")]
        public UnityEvent OnStageComplete;

        [Header("阶段专属音效")]
        [Tooltip("完成此阶段时播放的音效（直接拖入AudioClip，优先于全局音效）")]
        public AudioClip stageCompleteSound;

        [Tooltip("音效音量（0-1）")]
        [Range(0f, 1f)]
        public float soundVolume = 1f;
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
    [Tooltip("全部阶段完成后可拾取的物品（留空则不拾取）")]
    public ItemData finalPickupItem;

    [Tooltip("全部完成后触发的事件")]
    public UnityEvent OnAllStagesComplete;

    [Header("全局音效（回退用）")]
    [Tooltip("阶段切换音效路径（当阶段未配置专属音效时使用）")]
    public string stageSoundName = "Audio/SFX/item_used";

    [Tooltip("拾取物品音效路径")]
    public string pickupSoundName = "Audio/SFX/item_pickup";

    [Header("动效设置")]
    [Tooltip("启用精灵切换动效")]
    public bool enableSpriteTransition = true;

    [Tooltip("精灵淡入淡出时间")]
    [Range(0.1f, 1f)]
    public float transitionDuration = 0.3f;

    [Tooltip("启用缩放弹跳效果")]
    public bool enableScaleBounce = true;

    [Tooltip("弹跳缩放倍率")]
    [Range(1f, 1.5f)]
    public float bounceScale = 1.15f;

    [Tooltip("弹跳动画时间")]
    [Range(0.1f, 0.5f)]
    public float bounceDuration = 0.2f;

    [Tooltip("启用完成时闪光效果")]
    public bool enableCompletionFlash = true;

    [Tooltip("闪光颜色")]
    public Color flashColor = new Color(1f, 1f, 0.8f, 1f);

    // 内部状态
    private SpriteRenderer spriteRenderer;
    private bool allStagesComplete = false;
    private bool hasBeenPickedUp = false;
    private bool isAnimating = false;
    private Vector3 originalScale;
    private Color originalColor;
    private AudioSource localAudioSource;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalScale = transform.localScale;

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }

        // 创建本地AudioSource用于播放阶段专属音效
        localAudioSource = GetComponent<AudioSource>();
        if (localAudioSource == null)
        {
            localAudioSource = gameObject.AddComponent<AudioSource>();
            localAudioSource.playOnAwake = false;
            localAudioSource.spatialBlend = 0f; // 2D音效
        }
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
        // 动画中忽略点击
        if (isAnimating) return;

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

        // ⭐ 关键修改：检查是否需要物品
        if (stage.requiredItem != null)
        {
            // 需要物品 → 检查是否选中了正确物品
            if (UIManager.Instance == null) return;

            ItemData selectedItem = UIManager.Instance.GetSelectedItem();
            if (selectedItem == null)
            {
                Debug.Log($"[MultiStageObject] '{displayName}' 阶段{currentStage} 需要物品: {stage.requiredItem.displayName}");
                return;
            }

            if (selectedItem.itemID != stage.requiredItem.itemID)
            {
                Debug.Log($"[MultiStageObject] 物品不匹配，需要: {stage.requiredItem.displayName}");
                return;
            }

            // 消耗或取消选中物品
            if (stage.consumeItem)
                UIManager.Instance.ConsumeSelectedItem();
            else
                UIManager.Instance.DeselectItem();
        }
        // ⭐ 如果 requiredItem == null，直接通过（无需物品）

        // ✓ 条件满足，执行阶段切换
        Debug.Log($"[MultiStageObject] '{displayName}' 完成阶段 {currentStage}");

        // 播放阶段专属音效（优先）或全局音效（回退）
        PlayStageSound(stage);

        // 执行精灵切换（带动效）
        if (spriteRenderer != null && stage.resultSprite != null)
        {
            if (enableSpriteTransition || enableScaleBounce)
            {
                StartCoroutine(AnimatedSpriteChange(stage.resultSprite));
            }
            else
            {
                spriteRenderer.sprite = stage.resultSprite;
            }
        }
        else
        {
            // 没有精灵切换，但可能有弹跳效果
            if (enableScaleBounce)
            {
                StartCoroutine(ScaleBounceAnimation());
            }
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

            // 完成闪光效果
            if (enableCompletionFlash && spriteRenderer != null)
            {
                StartCoroutine(CompletionFlashAnimation());
            }

            OnAllStagesComplete?.Invoke();
        }

        // 保存进度
        SaveLoadSystem.Instance?.SaveGame();
    }

    /// <summary>
    /// 播放阶段音效 - 优先使用阶段专属AudioClip，否则回退到全局路径
    /// </summary>
    private void PlayStageSound(Stage stage)
    {
        // ⭐ 优先：阶段专属AudioClip
        if (stage.stageCompleteSound != null)
        {
            if (localAudioSource != null)
            {
                localAudioSource.clip = stage.stageCompleteSound;
                localAudioSource.volume = stage.soundVolume;
                localAudioSource.Play();
                Debug.Log($"[MultiStageObject] 播放阶段专属音效: {stage.stageCompleteSound.name}");
            }
            return;
        }

        // ⭐ 回退：全局音效路径
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(stageSoundName))
        {
            AudioManager.Instance.PlaySFX(stageSoundName);
        }
    }

    /// <summary>
    /// 带动效的精灵切换
    /// </summary>
    private IEnumerator AnimatedSpriteChange(Sprite newSprite)
    {
        isAnimating = true;

        float halfDuration = transitionDuration / 2f;

        // 第一阶段：淡出 + 缩小
        if (enableSpriteTransition)
        {
            float elapsed = 0f;
            Color startColor = spriteRenderer.color;
            Vector3 startScale = transform.localScale;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // 淡出
                spriteRenderer.color = new Color(
                    startColor.r,
                    startColor.g,
                    startColor.b,
                    Mathf.Lerp(startColor.a, 0f, smoothT)
                );

                // 缩小
                if (enableScaleBounce)
                {
                    transform.localScale = Vector3.Lerp(startScale, originalScale * 0.9f, smoothT);
                }

                yield return null;
            }
        }

        // 切换精灵
        spriteRenderer.sprite = newSprite;

        // 第二阶段：淡入 + 弹跳放大
        if (enableSpriteTransition)
        {
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / halfDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // 淡入
                spriteRenderer.color = new Color(
                    originalColor.r,
                    originalColor.g,
                    originalColor.b,
                    Mathf.Lerp(0f, originalColor.a, smoothT)
                );

                // 弹跳放大
                if (enableScaleBounce)
                {
                    float scaleT = Mathf.Sin(smoothT * Mathf.PI);
                    float currentScale = 1f + (bounceScale - 1f) * scaleT;
                    transform.localScale = originalScale * currentScale;
                }

                yield return null;
            }
        }

        // 确保最终状态正确
        spriteRenderer.color = originalColor;
        transform.localScale = originalScale;

        isAnimating = false;
    }

    /// <summary>
    /// 单独的缩放弹跳动画（无精灵切换时使用）
    /// </summary>
    private IEnumerator ScaleBounceAnimation()
    {
        isAnimating = true;

        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / bounceDuration;

            // 使用正弦曲线产生弹跳效果
            float scaleMultiplier = 1f + (bounceScale - 1f) * Mathf.Sin(t * Mathf.PI);
            transform.localScale = originalScale * scaleMultiplier;

            yield return null;
        }

        transform.localScale = originalScale;
        isAnimating = false;
    }

    /// <summary>
    /// 完成时的闪光效果
    /// </summary>
    private IEnumerator CompletionFlashAnimation()
    {
        if (spriteRenderer == null) yield break;

        int flashCount = 2;
        float flashDuration = 0.15f;

        for (int i = 0; i < flashCount; i++)
        {
            // 闪光
            float elapsed = 0f;
            while (elapsed < flashDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / flashDuration;
                spriteRenderer.color = Color.Lerp(originalColor, flashColor, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }
        }

        spriteRenderer.color = originalColor;
    }

    private void TryPickup()
    {
        // ⭐ 允许无拾取物品（纯状态切换谜题）
        if (finalPickupItem == null)
        {
            Debug.Log($"[MultiStageObject] '{displayName}' 已完成，无需拾取物品");
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