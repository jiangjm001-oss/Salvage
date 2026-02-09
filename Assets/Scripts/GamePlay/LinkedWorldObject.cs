// Assets/Scripts/GamePlay/LinkedWorldObject.cs
// 链接世界物体 - 用于全景视图中的装饰物体，与放大视图中的可拾取物品同步状态
// v1.1 - 修复：处理 GameObject 已经 inactive 的情况
using UnityEngine;

/// <summary>
/// 链接世界物体组件
/// 
/// 用途：挂载在全景视图中的"装饰性"物体上（如桌上的陶罐），
/// 当放大视图中对应的可拾取物品被拾取后，此物体会自动隐藏。
/// 
/// 使用方法：
/// 1. 在全景视图的装饰物体上添加此组件
/// 2. 在 linkedObjectID 中填入对应可拾取物品的 objectID
/// 3. 当该物品被拾取时，此装饰物体会自动消失
/// </summary>
public class LinkedWorldObject : MonoBehaviour
{
    [Header("链接设置")]
    [Tooltip("关联的可交互物品ID（与 InteractableObject.objectID 对应）")]
    public string linkedObjectID;

    [Header("消失动画设置")]
    [Tooltip("是否启用消失动画")]
    public bool useDisappearAnimation = true;

    [Tooltip("消失动画持续时间")]
    [Range(0.1f, 2f)]
    public float disappearDuration = 0.3f;

    [Tooltip("消失动画类型")]
    public DisappearType disappearType = DisappearType.FadeAndShrink;

    [Header("音效设置（可选）")]
    [Tooltip("消失时播放的音效")]
    public string disappearSoundName;

    [Header("调试")]
    [Tooltip("在控制台显示调试信息")]
    public bool debugMode = false;

    // 私有变量
    private SpriteRenderer spriteRenderer;
    private bool isDisappearing = false;
    private bool hasBeenHidden = false;  // ★ 新增：标记是否已被隐藏
    private Vector3 originalScale;
    private Color originalColor;

    /// <summary>
    /// 消失动画类型
    /// </summary>
    public enum DisappearType
    {
        Instant,        // 立即消失
        FadeOut,        // 淡出
        Shrink,         // 缩小
        FadeAndShrink,  // 淡出+缩小
        PopOut          // 弹出消失（先放大再缩小）
    }

    // ============ 生命周期 ============

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        originalScale = transform.localScale;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(linkedObjectID))
        {
            if (debugMode)
            {
                Debug.LogWarning($"[LinkedWorldObject] '{gameObject.name}' 未设置 linkedObjectID！");
            }
            return;
        }

        // 订阅状态改变事件
        WorldObjectStateManager.OnObjectStateChanged += OnObjectStateChanged;
        WorldObjectStateManager.OnObjectPickedUp += OnObjectPickedUp;

        // 检查初始状态（处理存档读取的情况）
        CheckInitialState();

        if (debugMode)
        {
            Debug.Log($"[LinkedWorldObject] '{gameObject.name}' 已链接到物品: {linkedObjectID}");
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        WorldObjectStateManager.OnObjectStateChanged -= OnObjectStateChanged;
        WorldObjectStateManager.OnObjectPickedUp -= OnObjectPickedUp;
    }

    /// <summary>
    /// ★ 关键：当物体被激活时检查是否应该隐藏
    /// 这处理了物体在 inactive 时收到拾取事件的情况
    /// </summary>
    private void OnEnable()
    {
        // 如果已被标记为隐藏，立即隐藏自己
        if (hasBeenHidden)
        {
            if (debugMode)
            {
                Debug.Log($"[LinkedWorldObject] '{gameObject.name}' OnEnable 检测到已被标记隐藏");
            }
            gameObject.SetActive(false);
            return;
        }

        // 额外检查：从 WorldObjectStateManager 获取最新状态
        if (!string.IsNullOrEmpty(linkedObjectID) && WorldObjectStateManager.Instance != null)
        {
            if (WorldObjectStateManager.Instance.IsObjectPickedUp(linkedObjectID))
            {
                if (debugMode)
                {
                    Debug.Log($"[LinkedWorldObject] '{gameObject.name}' OnEnable 从状态管理器检测到已被拾取，隐藏自己");
                }
                hasBeenHidden = true;
                gameObject.SetActive(false);
            }
        }
    }

    // ============ 事件处理 ============

    /// <summary>
    /// 检查初始状态（用于处理存档读取）
    /// </summary>
    private void CheckInitialState()
    {
        if (WorldObjectStateManager.Instance == null) return;

        // 如果关联的物品已被拾取，立即隐藏自己
        if (WorldObjectStateManager.Instance.IsObjectPickedUp(linkedObjectID))
        {
            if (debugMode)
            {
                Debug.Log($"[LinkedWorldObject] '{gameObject.name}' 检测到关联物品已被拾取，立即隐藏");
            }

            // 标记并直接隐藏，不播放动画
            hasBeenHidden = true;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 响应物品状态改变事件
    /// </summary>
    private void OnObjectStateChanged(string objectID, bool isActive)
    {
        if (objectID != linkedObjectID) return;

        // ★ 如果已经被处理过，直接返回
        if (hasBeenHidden) return;
        if (isDisappearing) return;

        if (debugMode)
        {
            Debug.Log($"[LinkedWorldObject] '{gameObject.name}' 收到状态改变: {objectID} → {(isActive ? "激活" : "隐藏")}");
        }

        if (!isActive)
        {
            // 物品被隐藏，执行消失逻辑
            StartDisappear();
        }
        else
        {
            // 物品被激活，显示自己（用于特殊情况）
            hasBeenHidden = false;
            gameObject.SetActive(true);
            ResetAppearance();
        }
    }

    /// <summary>
    /// 响应物品拾取事件
    /// </summary>
    private void OnObjectPickedUp(string objectID)
    {
        if (objectID != linkedObjectID) return;

        // ★ 如果已经被处理过，直接返回
        if (hasBeenHidden) return;
        if (isDisappearing) return;

        if (debugMode)
        {
            Debug.Log($"[LinkedWorldObject] '{gameObject.name}' 收到拾取事件: {objectID}");
        }

        StartDisappear();
    }

    // ============ 消失逻辑 ============

    /// <summary>
    /// 开始消失
    /// </summary>
    private void StartDisappear()
    {
        if (isDisappearing) return;
        if (hasBeenHidden) return;

        // ★ 关键修复：检查 GameObject 是否处于激活状态
        // 如果已经是 inactive，直接标记并返回，不尝试启动协程
        if (!gameObject.activeInHierarchy)
        {
            if (debugMode)
            {
                Debug.Log($"[LinkedWorldObject] '{gameObject.name}' 已经是 inactive 状态，跳过动画");
            }
            hasBeenHidden = true;
            return;
        }

        isDisappearing = true;
        hasBeenHidden = true;

        // 播放音效
        if (!string.IsNullOrEmpty(disappearSoundName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(disappearSoundName);
        }

        // 根据类型执行消失动画
        if (useDisappearAnimation && disappearType != DisappearType.Instant)
        {
            StartCoroutine(DisappearAnimation());
        }
        else
        {
            // 立即消失
            gameObject.SetActive(false);
            isDisappearing = false;
        }
    }

    /// <summary>
    /// 消失动画协程
    /// </summary>
    private System.Collections.IEnumerator DisappearAnimation()
    {
        float elapsed = 0f;

        // PopOut 类型先放大
        if (disappearType == DisappearType.PopOut)
        {
            float popDuration = disappearDuration * 0.3f;
            Vector3 popScale = originalScale * 1.2f;

            while (elapsed < popDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / popDuration;

                // 使用缓出曲线
                float easeT = 1f - Mathf.Pow(1f - t, 2f);
                transform.localScale = Vector3.Lerp(originalScale, popScale, easeT);

                yield return null;
            }

            elapsed = 0f;
            originalScale = popScale; // 更新起始缩放
        }

        // 主消失动画
        while (elapsed < disappearDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / disappearDuration);

            // 使用缓入曲线让消失更自然
            float easeT = t * t;

            switch (disappearType)
            {
                case DisappearType.FadeOut:
                    ApplyFade(1f - easeT);
                    break;

                case DisappearType.Shrink:
                case DisappearType.PopOut:
                    transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, easeT);
                    break;

                case DisappearType.FadeAndShrink:
                    ApplyFade(1f - easeT);
                    transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, easeT);
                    break;
            }

            yield return null;
        }

        // 动画结束，隐藏物体
        gameObject.SetActive(false);

        // 重置外观（为了可能的重新显示）
        ResetAppearance();
        isDisappearing = false;
    }

    /// <summary>
    /// 应用透明度
    /// </summary>
    private void ApplyFade(float alpha)
    {
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = originalColor.a * alpha;
            spriteRenderer.color = c;
        }
    }

    /// <summary>
    /// 重置外观
    /// </summary>
    private void ResetAppearance()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        transform.localScale = originalScale;
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 手动设置链接ID（用于运行时动态创建）
    /// </summary>
    public void SetLinkedObjectID(string objectID)
    {
        linkedObjectID = objectID;

        if (debugMode)
        {
            Debug.Log($"[LinkedWorldObject] '{gameObject.name}' 动态链接到: {objectID}");
        }

        // 检查状态
        CheckInitialState();
    }

    /// <summary>
    /// 强制立即隐藏
    /// </summary>
    public void ForceHide()
    {
        isDisappearing = false;
        hasBeenHidden = true;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 强制显示
    /// </summary>
    public void ForceShow()
    {
        hasBeenHidden = false;
        isDisappearing = false;
        gameObject.SetActive(true);
        ResetAppearance();
    }

    /// <summary>
    /// 重置状态（用于新游戏）
    /// </summary>
    public void ResetState()
    {
        hasBeenHidden = false;
        isDisappearing = false;
        ResetAppearance();
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        // 自动生成提示名称
        if (string.IsNullOrEmpty(linkedObjectID))
        {
            // 可以基于物体名称给个建议
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 在Scene视图中显示链接标识
        if (!string.IsNullOrEmpty(linkedObjectID))
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.2f);

            // 画一个小标签
#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 0.3f,
                $"链接: {linkedObjectID}",
                new GUIStyle { normal = { textColor = Color.cyan } }
            );
#endif
        }
    }
}