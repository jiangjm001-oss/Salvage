// Assets/Scripts/GamePlay/BirdCrossController.cs
// 鸟与十字控制器 - 控制鸟的朝向和对话显示
// 支持三种状态：中立、向左看、向右看
// ⭐ 十字围绕自身中心旋转，对话文字根据朝向出现在左/右侧
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using System.Collections;

public class BirdCrossController : MonoBehaviour
{
    // ============ 状态枚举 ============
    public enum BirdState
    {
        Neutral = 0,    // 中立态
        Left = -1,      // 向左看
        Right = 1       // 向右看
    }

    // ============ 基本设置 ============
    [Header("基本设置")]
    [Tooltip("物体唯一ID（用于存档）")]
    public string objectID = "bird_cross_puzzle";

    [Tooltip("显示名称")]
    public string displayName = "机械鸟";

    // ============ 鸟的设置 ============
    [Header("鸟的精灵设置")]
    [Tooltip("鸟的 SpriteRenderer")]
    public SpriteRenderer birdRenderer;

    [Tooltip("中立状态精灵")]
    public Sprite birdNeutralSprite;

    [Tooltip("向左看精灵")]
    public Sprite birdLeftSprite;

    [Tooltip("向右看精灵")]
    public Sprite birdRightSprite;

    // ============ 十字设置 ============
    [Header("十字控制器设置")]
    [Tooltip("十字控制器整体（包含中心点，用于旋转）")]
    public Transform crossTransform;

    [Tooltip("横杆的 CrossHandle 组件")]
    public CrossHandle horizontalBar;

    [Tooltip("竖杆的 CrossHandle 组件")]
    public CrossHandle verticalBar;

    // ============ 对话设置 ============
    [Header("对话设置")]
    [Tooltip("左侧对话文字组件 (TextMeshPro)")]
    public TextMeshPro leftDialogueText;

    [Tooltip("右侧对话文字组件 (TextMeshPro)")]
    public TextMeshPro rightDialogueText;

    [Tooltip("向左看时说的话")]
    [TextArea(2, 4)]
    public string leftDialogue = "我看见了过去的影子...";

    [Tooltip("向右看时说的话")]
    [TextArea(2, 4)]
    public string rightDialogue = "未来正在召唤我...";

    [Tooltip("对话显示持续时间")]
    public float dialogueDisplayDuration = 3f;

    [Tooltip("对话淡出时间")]
    public float dialogueFadeDuration = 0.5f;

    // ============ 动画设置 ============
    [Header("动画设置")]
    [Tooltip("精灵切换淡入淡出时间")]
    public float spriteFadeDuration = 0.3f;

    [Tooltip("十字旋转动画时间")]
    public float crossRotateDuration = 0.4f;

    [Tooltip("旋转动画曲线")]
    public AnimationCurve rotateCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("打字机效果每字符间隔")]
    public float typewriterInterval = 0.05f;

    // ============ 音效设置 ============
    [Header("音效设置")]
    [Tooltip("状态切换音效")]
    public string stateChangeSoundName = "Audio/SFX/mechanical_click";

    [Tooltip("对话出现音效")]
    public string dialogueSoundName = "Audio/SFX/bird_speak";

    [Tooltip("无效点击音效")]
    public string invalidClickSoundName = "Audio/SFX/click_invalid";

    // ============ 事件 ============
    [Header("事件")]
    [Tooltip("状态变化时触发")]
    public UnityEvent<BirdState> OnStateChanged;

    [Tooltip("变为向左看时触发")]
    public UnityEvent OnLookLeft;

    [Tooltip("变为向右看时触发")]
    public UnityEvent OnLookRight;

    [Tooltip("回到中立时触发")]
    public UnityEvent OnReturnNeutral;

    // ============ 内部状态 ============
    [HideInInspector]
    public BirdState currentState = BirdState.Neutral;

    private bool isAnimating = false;
    private Coroutine dialogueCoroutine;
    private Coroutine spriteCoroutine;
    private Coroutine rotateCoroutine;

    // 十字初始旋转角度
    private float neutralRotation = 0f;

    // ============ 生命周期 ============

    private void Awake()
    {
        // 初始化对话文字
        InitializeDialogueText(leftDialogueText);
        InitializeDialogueText(rightDialogueText);

        // 记录十字初始旋转
        if (crossTransform != null)
        {
            neutralRotation = crossTransform.localEulerAngles.z;
        }

        // 自动获取引用
        if (birdRenderer == null)
        {
            birdRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void InitializeDialogueText(TextMeshPro text)
    {
        if (text != null)
        {
            text.text = "";
            SetTextAlpha(text, 0f);
        }
    }

    private void Start()
    {
        // 注册十字手柄的点击事件
        if (horizontalBar != null)
        {
            horizontalBar.Initialize(this, CrossHandle.HandleType.Horizontal);
        }

        if (verticalBar != null)
        {
            verticalBar.Initialize(this, CrossHandle.HandleType.Vertical);
        }

        // 恢复保存的状态
        LoadState();
    }

    // ============ 核心交互逻辑 ============

    /// <summary>
    /// 处理十字手柄点击
    /// </summary>
    /// <param name="handleType">点击的手柄类型</param>
    public void OnHandleClicked(CrossHandle.HandleType handleType)
    {
        if (isAnimating)
        {
            Debug.Log("[BirdCross] 动画进行中，忽略点击");
            return;
        }

        BirdState targetState = DetermineTargetState(handleType);

        if (targetState == currentState)
        {
            // 无效点击
            Debug.Log($"[BirdCross] 无效点击: 当前状态={currentState}, 点击={handleType}");
            PlayInvalidClickFeedback(handleType);
            return;
        }

        Debug.Log($"[BirdCross] 状态切换: {currentState} → {targetState}");
        StartCoroutine(TransitionToState(targetState));
    }

    /// <summary>
    /// 根据当前状态和点击类型确定目标状态
    /// </summary>
    private BirdState DetermineTargetState(CrossHandle.HandleType handleType)
    {
        switch (currentState)
        {
            case BirdState.Neutral:
                // 中立态：横杆→左，竖杆→右
                return handleType == CrossHandle.HandleType.Horizontal
                    ? BirdState.Left
                    : BirdState.Right;

            case BirdState.Left:
                // 向左看：只有竖杆能回到中立
                return handleType == CrossHandle.HandleType.Vertical
                    ? BirdState.Neutral
                    : BirdState.Left;

            case BirdState.Right:
                // 向右看：只有横杆能回到中立
                return handleType == CrossHandle.HandleType.Horizontal
                    ? BirdState.Neutral
                    : BirdState.Right;

            default:
                return currentState;
        }
    }

    /// <summary>
    /// 无效点击的视觉反馈
    /// </summary>
    private void PlayInvalidClickFeedback(CrossHandle.HandleType handleType)
    {
        // 播放无效音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(invalidClickSoundName))
        {
            AudioManager.Instance.PlaySFX(invalidClickSoundName);
        }

        // 让对应的手柄抖动
        CrossHandle handle = handleType == CrossHandle.HandleType.Horizontal
            ? horizontalBar
            : verticalBar;

        if (handle != null)
        {
            handle.PlayShakeAnimation();
        }
    }

    // ============ 状态转换动画 ============

    /// <summary>
    /// 执行状态转换的完整动画序列
    /// </summary>
    private IEnumerator TransitionToState(BirdState targetState)
    {
        isAnimating = true;

        // 1. 播放状态切换音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(stateChangeSoundName))
        {
            AudioManager.Instance.PlaySFX(stateChangeSoundName);
        }

        // 2. 隐藏当前对话（如果有）
        if (dialogueCoroutine != null)
        {
            StopCoroutine(dialogueCoroutine);
            dialogueCoroutine = null;
        }
        yield return StartCoroutine(FadeOutAllDialogues(0.15f));

        // 3. 同时执行：鸟的精灵切换 + 十字旋转
        Sprite targetSprite = GetSpriteForState(targetState);
        float targetRotation = GetRotationForState(targetState);

        spriteCoroutine = StartCoroutine(TransitionBirdSprite(targetSprite));
        rotateCoroutine = StartCoroutine(RotateCross(targetRotation));

        // 等待两个动画完成
        yield return spriteCoroutine;
        yield return rotateCoroutine;

        // 4. 更新状态
        BirdState previousState = currentState;
        currentState = targetState;

        // 5. 触发事件
        OnStateChanged?.Invoke(currentState);

        switch (currentState)
        {
            case BirdState.Left:
                OnLookLeft?.Invoke();
                break;
            case BirdState.Right:
                OnLookRight?.Invoke();
                break;
            case BirdState.Neutral:
                OnReturnNeutral?.Invoke();
                break;
        }

        // 6. 显示对话（如果不是中立态）
        if (currentState == BirdState.Left)
        {
            dialogueCoroutine = StartCoroutine(ShowDialogue(leftDialogueText, leftDialogue));
        }
        else if (currentState == BirdState.Right)
        {
            dialogueCoroutine = StartCoroutine(ShowDialogue(rightDialogueText, rightDialogue));
        }

        // 7. 保存状态
        SaveState();

        isAnimating = false;
    }

    /// <summary>
    /// 获取指定状态对应的精灵
    /// </summary>
    private Sprite GetSpriteForState(BirdState state)
    {
        switch (state)
        {
            case BirdState.Left:
                return birdLeftSprite;
            case BirdState.Right:
                return birdRightSprite;
            default:
                return birdNeutralSprite;
        }
    }

    /// <summary>
    /// 获取指定状态对应的十字旋转角度
    /// ⭐ 十字围绕自身中心旋转
    /// </summary>
    private float GetRotationForState(BirdState state)
    {
        switch (state)
        {
            case BirdState.Left:
                return neutralRotation + 90f;  // 逆时针90度
            case BirdState.Right:
                return neutralRotation - 90f;  // 顺时针90度
            default:
                return neutralRotation;
        }
    }

    // ============ 鸟的精灵动画 ============

    /// <summary>
    /// 鸟的精灵淡入淡出切换
    /// </summary>
    private IEnumerator TransitionBirdSprite(Sprite newSprite)
    {
        if (birdRenderer == null || newSprite == null) yield break;

        float halfDuration = spriteFadeDuration / 2f;

        // 淡出
        Color startColor = birdRenderer.color;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            birdRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
            yield return null;
        }

        // 切换精灵
        birdRenderer.sprite = newSprite;

        // 淡入
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            birdRenderer.color = new Color(startColor.r, startColor.g, startColor.b, t);
            yield return null;
        }

        birdRenderer.color = startColor;
    }

    // ============ 十字旋转动画 ============

    /// <summary>
    /// 平滑旋转十字控制器（围绕自身中心）
    /// ⭐ crossTransform 的 pivot 应设置在十字中心点
    /// </summary>
    private IEnumerator RotateCross(float targetAngle)
    {
        if (crossTransform == null)
        {
            Debug.LogError("[BirdCross] crossTransform 为空！");
            yield break;
        }

        // ⭐ 调试日志
        Debug.Log($"[BirdCross] === 旋转调试 ===");
        Debug.Log($"[BirdCross] crossTransform 名称: {crossTransform.name}");
        Debug.Log($"[BirdCross] crossTransform 世界位置: {crossTransform.position}");
        Debug.Log($"[BirdCross] crossTransform 本地位置: {crossTransform.localPosition}");
        Debug.Log($"[BirdCross] crossTransform 父物体: {(crossTransform.parent != null ? crossTransform.parent.name : "无")}");
        Debug.Log($"[BirdCross] crossTransform 子物体数量: {crossTransform.childCount}");

        float startAngle = crossTransform.localEulerAngles.z;

        // 处理角度跨越问题（如从350到10）
        float delta = Mathf.DeltaAngle(startAngle, targetAngle);
        float endAngle = startAngle + delta;

        Debug.Log($"[BirdCross] 旋转: {startAngle}° → {targetAngle}°");

        float elapsed = 0f;

        while (elapsed < crossRotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = rotateCurve.Evaluate(elapsed / crossRotateDuration);
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
            crossTransform.localEulerAngles = new Vector3(0, 0, currentAngle);
            yield return null;
        }

        crossTransform.localEulerAngles = new Vector3(0, 0, targetAngle);
        Debug.Log($"[BirdCross] 旋转完成，最终角度: {crossTransform.localEulerAngles.z}°");
    }

    // ============ 对话显示动画 ============

    /// <summary>
    /// 淡出所有对话
    /// </summary>
    private IEnumerator FadeOutAllDialogues(float duration)
    {
        Coroutine left = null;
        Coroutine right = null;

        if (leftDialogueText != null && leftDialogueText.color.a > 0)
        {
            left = StartCoroutine(FadeOutDialogue(leftDialogueText, duration));
        }

        if (rightDialogueText != null && rightDialogueText.color.a > 0)
        {
            right = StartCoroutine(FadeOutDialogue(rightDialogueText, duration));
        }

        if (left != null) yield return left;
        if (right != null) yield return right;
    }

    /// <summary>
    /// 显示对话（打字机效果 + 自动淡出）
    /// </summary>
    private IEnumerator ShowDialogue(TextMeshPro targetText, string content)
    {
        if (targetText == null) yield break;

        // 播放对话音效
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(dialogueSoundName))
        {
            AudioManager.Instance.PlaySFX(dialogueSoundName);
        }

        // 重置
        targetText.text = "";
        SetTextAlpha(targetText, 1f);

        // 打字机效果
        for (int i = 0; i < content.Length; i++)
        {
            targetText.text += content[i];
            yield return new WaitForSeconds(typewriterInterval);
        }

        // 等待显示
        yield return new WaitForSeconds(dialogueDisplayDuration);

        // 淡出
        yield return StartCoroutine(FadeOutDialogue(targetText, dialogueFadeDuration));
    }

    /// <summary>
    /// 对话淡出
    /// </summary>
    private IEnumerator FadeOutDialogue(TextMeshPro targetText, float duration)
    {
        if (targetText == null) yield break;

        float startAlpha = targetText.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            SetTextAlpha(targetText, Mathf.Lerp(startAlpha, 0f, t));
            yield return null;
        }

        SetTextAlpha(targetText, 0f);
        targetText.text = "";
    }

    /// <summary>
    /// 设置文字透明度
    /// </summary>
    private void SetTextAlpha(TextMeshPro text, float alpha)
    {
        if (text == null) return;
        Color c = text.color;
        text.color = new Color(c.r, c.g, c.b, alpha);
    }

    // ============ 存档系统 ============

    /// <summary>
    /// 保存当前状态
    /// </summary>
    private void SaveState()
    {
        string key = $"BirdCross_{objectID}_State";
        PlayerPrefs.SetInt(key, (int)currentState);
        PlayerPrefs.Save();
        Debug.Log($"[BirdCross] 状态已保存: {currentState}");
    }

    /// <summary>
    /// 加载保存的状态
    /// </summary>
    private void LoadState()
    {
        string key = $"BirdCross_{objectID}_State";

        if (PlayerPrefs.HasKey(key))
        {
            int savedState = PlayerPrefs.GetInt(key);
            currentState = (BirdState)savedState;
            Debug.Log($"[BirdCross] 加载状态: {currentState}");

            // 直接设置到目标状态（无动画）
            ApplyStateImmediate(currentState);
        }
    }

    /// <summary>
    /// 立即应用状态（无动画，用于加载）
    /// </summary>
    private void ApplyStateImmediate(BirdState state)
    {
        // 设置鸟的精灵
        if (birdRenderer != null)
        {
            birdRenderer.sprite = GetSpriteForState(state);
        }

        // 设置十字旋转
        if (crossTransform != null)
        {
            float rotation = GetRotationForState(state);
            crossTransform.localEulerAngles = new Vector3(0, 0, rotation);
        }

        // 隐藏所有对话
        if (leftDialogueText != null)
        {
            leftDialogueText.text = "";
            SetTextAlpha(leftDialogueText, 0f);
        }

        if (rightDialogueText != null)
        {
            rightDialogueText.text = "";
            SetTextAlpha(rightDialogueText, 0f);
        }
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 强制设置到指定状态（用于外部调用）
    /// </summary>
    public void SetState(BirdState state, bool animate = true)
    {
        if (animate && !isAnimating)
        {
            StartCoroutine(TransitionToState(state));
        }
        else if (!animate)
        {
            currentState = state;
            ApplyStateImmediate(state);
            SaveState();
        }
    }

    /// <summary>
    /// 重置到中立状态
    /// </summary>
    [ContextMenu("重置到中立态")]
    public void ResetToNeutral()
    {
        SetState(BirdState.Neutral, false);
    }

    /// <summary>
    /// 清除存档
    /// </summary>
    [ContextMenu("清除存档")]
    public void ClearSave()
    {
        string key = $"BirdCross_{objectID}_State";
        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
        Debug.Log("[BirdCross] 存档已清除");
    }

    // ============ 编辑器辅助 ============

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(objectID))
        {
            objectID = $"bird_cross_{GetInstanceID()}";
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 显示十字旋转中心
        if (crossTransform != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(crossTransform.position, 0.1f);

            // 显示旋转范围
            Gizmos.color = Color.yellow;
            Vector3 center = crossTransform.position;
            float radius = 0.5f;

            // 中立位置
            Gizmos.DrawLine(center, center + Vector3.up * radius);

            // 左转位置（+90度）
            Gizmos.color = Color.cyan;
            Vector3 leftDir = Quaternion.Euler(0, 0, 90) * Vector3.up;
            Gizmos.DrawLine(center, center + leftDir * radius);

            // 右转位置（-90度）
            Gizmos.color = Color.magenta;
            Vector3 rightDir = Quaternion.Euler(0, 0, -90) * Vector3.up;
            Gizmos.DrawLine(center, center + rightDir * radius);
        }

        // 显示对话文字位置
        if (leftDialogueText != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(leftDialogueText.transform.position, new Vector3(1f, 0.3f, 0f));
        }

        if (rightDialogueText != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(rightDialogueText.transform.position, new Vector3(1f, 0.3f, 0f));
        }
    }
}