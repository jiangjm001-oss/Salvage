// Assets/Scripts/GamePlay/Cutscene/ShadowAnimationEffect.cs
// 高级黑影动画效果组件
// 提供多种扭曲、脉动、液化效果，可独立使用或配合Shader
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 黑影动画效果组件
/// 提供多种视觉效果：脉动、扭曲、抖动、分裂等
/// 可在没有自定义Shader时提供精美的动画效果
/// </summary>
[RequireComponent(typeof(Image))]
public class ShadowAnimationEffect : MonoBehaviour
{
    // ============ 效果类型 ============
    [System.Flags]
    public enum EffectType
    {
        None = 0,
        Pulse = 1 << 0,           // 脉动（缩放）
        Breathe = 1 << 1,         // 呼吸（平滑缩放）
        Shake = 1 << 2,           // 抖动
        Float = 1 << 3,           // 漂浮
        Rotate = 1 << 4,          // 旋转摆动
        ColorPulse = 1 << 5,      // 颜色脉动
        Glitch = 1 << 6,          // 故障效果
        Wave = 1 << 7,            // 波浪变形
        Shadow = 1 << 8,          // 投影效果
        Distortion = 1 << 9,      // UV扭曲（需要Shader）
    }

    [Header("效果选择")]
    [Tooltip("启用的效果类型（可多选）")]
    public EffectType enabledEffects = EffectType.Pulse | EffectType.Breathe | EffectType.Shake;

    // ============ 脉动效果 ============
    [Header("脉动效果 (Pulse)")]
    [Tooltip("脉动强度")]
    [Range(0f, 0.5f)]
    public float pulseStrength = 0.1f;

    [Tooltip("脉动速度")]
    [Range(0.1f, 10f)]
    public float pulseSpeed = 3f;

    [Tooltip("脉动曲线")]
    public AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    // ============ 呼吸效果 ============
    [Header("呼吸效果 (Breathe)")]
    [Range(0f, 0.3f)]
    public float breatheStrength = 0.05f;

    [Range(0.1f, 5f)]
    public float breatheSpeed = 1f;

    // ============ 抖动效果 ============
    [Header("抖动效果 (Shake)")]
    [Tooltip("抖动强度")]
    [Range(0f, 20f)]
    public float shakeStrength = 3f;

    [Tooltip("抖动速度")]
    [Range(0.1f, 50f)]
    public float shakeSpeed = 20f;

    [Tooltip("是否使用Perlin噪声（更自然）")]
    public bool usePerlinShake = true;

    // ============ 漂浮效果 ============
    [Header("漂浮效果 (Float)")]
    [Range(0f, 50f)]
    public float floatAmplitude = 10f;

    [Range(0.1f, 5f)]
    public float floatSpeed = 1f;

    // ============ 旋转效果 ============
    [Header("旋转效果 (Rotate)")]
    [Range(0f, 30f)]
    public float rotateAngle = 5f;

    [Range(0.1f, 5f)]
    public float rotateSpeed = 2f;

    // ============ 颜色脉动 ============
    [Header("颜色脉动 (ColorPulse)")]
    [Range(0f, 1f)]
    public float colorPulseStrength = 0.2f;

    [Range(0.1f, 10f)]
    public float colorPulseSpeed = 4f;

    public Color pulseColor = new Color(0.3f, 0f, 0.5f, 1f);

    // ============ 故障效果 ============
    [Header("故障效果 (Glitch)")]
    [Range(0f, 1f)]
    public float glitchProbability = 0.1f;

    [Range(0f, 50f)]
    public float glitchOffset = 10f;

    [Range(0.01f, 0.2f)]
    public float glitchDuration = 0.05f;

    // ============ 波浪效果 ============
    [Header("波浪效果 (Wave)")]
    [Range(0f, 20f)]
    public float waveAmplitude = 5f;

    [Range(0.1f, 10f)]
    public float waveSpeed = 3f;

    [Range(1, 10)]
    public int waveCount = 3;

    // ============ 投影效果 ============
    [Header("投影效果 (Shadow)")]
    public Vector2 shadowOffset = new Vector2(5f, -5f);
    public Color shadowColor = new Color(0, 0, 0, 0.5f);
    public bool animateShadow = true;

    // ============ UV扭曲（需要Shader） ============
    [Header("UV扭曲 (Distortion - 需要Shader)")]
    public Material distortionMaterial;
    [Range(0f, 0.1f)]
    public float distortionStrength = 0.02f;

    // ============ 状态 ============
    [Header("运行时状态")]
    [SerializeField] private bool isPlaying = false;
    [SerializeField] private float elapsedTime = 0f;

    // ============ 内部变量 ============
    private Image targetImage;
    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Vector2 originalPosition;
    private Quaternion originalRotation;
    private Color originalColor;
    private Material originalMaterial;

    // 投影相关
    private GameObject shadowObject;
    private Image shadowImage;

    // 故障效果
    private float glitchTimer = 0f;
    private bool isGlitching = false;

    // ============ Unity生命周期 ============

    private void Awake()
    {
        targetImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // 保存原始状态
        originalScale = rectTransform.localScale;
        originalPosition = rectTransform.anchoredPosition;
        originalRotation = rectTransform.localRotation;
        originalColor = targetImage.color;
        originalMaterial = targetImage.material;
    }

    private void OnEnable()
    {
        // 创建投影
        if ((enabledEffects & EffectType.Shadow) != 0)
        {
            CreateShadow();
        }
    }

    private void OnDisable()
    {
        // 销毁投影
        if (shadowObject != null)
        {
            Destroy(shadowObject);
            shadowObject = null;
        }

        // 恢复原始状态
        RestoreOriginalState();
    }

    private void Update()
    {
        if (!isPlaying) return;

        elapsedTime += Time.deltaTime;
        ApplyEffects();
    }

    // ============ 公共方法 ============

    /// <summary>
    /// 开始播放动画
    /// </summary>
    public void Play()
    {
        if (isPlaying) return;

        isPlaying = true;
        elapsedTime = 0f;

        // 应用扭曲材质
        if ((enabledEffects & EffectType.Distortion) != 0 && distortionMaterial != null)
        {
            targetImage.material = distortionMaterial;
        }

        Debug.Log("[ShadowAnimation] 开始播放");
    }

    /// <summary>
    /// 停止播放动画
    /// </summary>
    public void Stop()
    {
        if (!isPlaying) return;

        isPlaying = false;
        RestoreOriginalState();

        Debug.Log("[ShadowAnimation] 停止播放");
    }

    /// <summary>
    /// 播放指定时长后自动停止
    /// </summary>
    public void PlayForDuration(float duration)
    {
        StartCoroutine(PlayForDurationCoroutine(duration));
    }

    private IEnumerator PlayForDurationCoroutine(float duration)
    {
        Play();
        yield return new WaitForSeconds(duration);
        Stop();
    }

    /// <summary>
    /// 设置效果类型
    /// </summary>
    public void SetEffects(EffectType effects)
    {
        enabledEffects = effects;
    }

    /// <summary>
    /// 添加效果类型
    /// </summary>
    public void AddEffect(EffectType effect)
    {
        enabledEffects |= effect;
    }

    /// <summary>
    /// 移除效果类型
    /// </summary>
    public void RemoveEffect(EffectType effect)
    {
        enabledEffects &= ~effect;
    }

    // ============ 效果应用 ============

    private void ApplyEffects()
    {
        float time = elapsedTime;
        Vector3 scale = originalScale;
        Vector2 position = originalPosition;
        Quaternion rotation = originalRotation;
        Color color = originalColor;

        // 脉动效果
        if ((enabledEffects & EffectType.Pulse) != 0)
        {
            float pulse = Mathf.Sin(time * pulseSpeed * Mathf.PI * 2f);
            pulse = pulseCurve.Evaluate((pulse + 1f) * 0.5f);
            float pulseScale = 1f + pulse * pulseStrength;
            scale *= pulseScale;
        }

        // 呼吸效果
        if ((enabledEffects & EffectType.Breathe) != 0)
        {
            float breathe = Mathf.Sin(time * breatheSpeed * Mathf.PI);
            breathe = (breathe + 1f) * 0.5f; // 0-1
            float breatheScale = 1f + breathe * breatheStrength;
            scale *= breatheScale;
        }

        // 抖动效果
        if ((enabledEffects & EffectType.Shake) != 0)
        {
            Vector2 shake;
            if (usePerlinShake)
            {
                shake.x = (Mathf.PerlinNoise(time * shakeSpeed, 0f) - 0.5f) * 2f * shakeStrength;
                shake.y = (Mathf.PerlinNoise(0f, time * shakeSpeed) - 0.5f) * 2f * shakeStrength;
            }
            else
            {
                shake.x = Mathf.Sin(time * shakeSpeed * 1.1f) * shakeStrength;
                shake.y = Mathf.Cos(time * shakeSpeed * 0.9f) * shakeStrength;
            }
            position += shake;
        }

        // 漂浮效果
        if ((enabledEffects & EffectType.Float) != 0)
        {
            float floatOffset = Mathf.Sin(time * floatSpeed * Mathf.PI * 2f) * floatAmplitude;
            position.y += floatOffset;
        }

        // 旋转效果
        if ((enabledEffects & EffectType.Rotate) != 0)
        {
            float rotAngle = Mathf.Sin(time * rotateSpeed * Mathf.PI * 2f) * rotateAngle;
            rotation = Quaternion.Euler(0, 0, rotAngle);
        }

        // 颜色脉动
        if ((enabledEffects & EffectType.ColorPulse) != 0)
        {
            float colorT = (Mathf.Sin(time * colorPulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
            color = Color.Lerp(originalColor, pulseColor, colorT * colorPulseStrength);
        }

        // 故障效果
        if ((enabledEffects & EffectType.Glitch) != 0)
        {
            ApplyGlitchEffect(ref position, ref color);
        }

        // 波浪效果（需要多个子对象或使用Shader）
        // 这里简化为轻微的波动
        if ((enabledEffects & EffectType.Wave) != 0)
        {
            float waveOffset = 0f;
            for (int i = 0; i < waveCount; i++)
            {
                waveOffset += Mathf.Sin(time * waveSpeed + i * Mathf.PI / waveCount) * waveAmplitude / (i + 1);
            }
            position.x += waveOffset * 0.3f;
        }

        // UV扭曲（更新材质参数）
        if ((enabledEffects & EffectType.Distortion) != 0 && targetImage.material != null)
        {
            targetImage.material.SetFloat("_TimeOffset", time);
            targetImage.material.SetFloat("_DistortionStrength", distortionStrength);
        }

        // 应用变换
        rectTransform.localScale = scale;
        rectTransform.anchoredPosition = position;
        rectTransform.localRotation = rotation;
        targetImage.color = color;

        // 更新投影
        if ((enabledEffects & EffectType.Shadow) != 0 && shadowObject != null)
        {
            UpdateShadow(position, scale, rotation, color.a);
        }
    }

    private void ApplyGlitchEffect(ref Vector2 position, ref Color color)
    {
        glitchTimer -= Time.deltaTime;

        if (!isGlitching && glitchTimer <= 0)
        {
            // 随机触发故障
            if (Random.value < glitchProbability * Time.deltaTime * 10f)
            {
                isGlitching = true;
                glitchTimer = glitchDuration;
            }
        }

        if (isGlitching)
        {
            // 水平位移
            position.x += Random.Range(-glitchOffset, glitchOffset);

            // 颜色分离效果
            if (Random.value < 0.5f)
            {
                color.r *= 1.2f;
                color.b *= 0.8f;
            }

            if (glitchTimer <= 0)
            {
                isGlitching = false;
                glitchTimer = Random.Range(0.1f, 0.5f); // 随机间隔
            }
        }
    }

    // ============ 投影效果 ============

    private void CreateShadow()
    {
        if (shadowObject != null) return;

        // 创建投影对象
        shadowObject = new GameObject("Shadow");
        shadowObject.transform.SetParent(transform.parent);
        shadowObject.transform.SetSiblingIndex(transform.GetSiblingIndex()); // 放在原图下面

        // 复制RectTransform
        RectTransform shadowRect = shadowObject.AddComponent<RectTransform>();
        shadowRect.anchoredPosition = rectTransform.anchoredPosition + shadowOffset;
        shadowRect.sizeDelta = rectTransform.sizeDelta;
        shadowRect.localScale = rectTransform.localScale;
        shadowRect.localRotation = rectTransform.localRotation;
        shadowRect.anchorMin = rectTransform.anchorMin;
        shadowRect.anchorMax = rectTransform.anchorMax;
        shadowRect.pivot = rectTransform.pivot;

        // 添加Image
        shadowImage = shadowObject.AddComponent<Image>();
        shadowImage.sprite = targetImage.sprite;
        shadowImage.color = shadowColor;
        shadowImage.raycastTarget = false;
    }

    private void UpdateShadow(Vector2 position, Vector3 scale, Quaternion rotation, float alpha)
    {
        if (shadowObject == null || shadowImage == null) return;

        RectTransform shadowRect = shadowObject.GetComponent<RectTransform>();

        if (animateShadow)
        {
            // 投影跟随动画，但有延迟感
            float lagFactor = 0.8f;
            Vector2 targetPos = position + shadowOffset;
            shadowRect.anchoredPosition = Vector2.Lerp(shadowRect.anchoredPosition, targetPos, Time.deltaTime * 10f);
            shadowRect.localScale = Vector3.Lerp(shadowRect.localScale, scale * lagFactor, Time.deltaTime * 10f);
            shadowRect.localRotation = Quaternion.Lerp(shadowRect.localRotation, rotation, Time.deltaTime * 10f);
        }
        else
        {
            shadowRect.anchoredPosition = position + shadowOffset;
            shadowRect.localScale = scale;
            shadowRect.localRotation = rotation;
        }

        // 投影透明度跟随主图
        Color sc = shadowColor;
        sc.a = shadowColor.a * alpha;
        shadowImage.color = sc;
    }

    // ============ 状态恢复 ============

    private void RestoreOriginalState()
    {
        if (rectTransform != null)
        {
            rectTransform.localScale = originalScale;
            rectTransform.anchoredPosition = originalPosition;
            rectTransform.localRotation = originalRotation;
        }

        if (targetImage != null)
        {
            targetImage.color = originalColor;
            targetImage.material = originalMaterial;
        }

        isGlitching = false;
        glitchTimer = 0f;
    }

    // ============ 编辑器预览 ============

#if UNITY_EDITOR
    [Header("编辑器调试")]
    public bool previewInEditor = false;

    private void OnValidate()
    {
        if (previewInEditor && !Application.isPlaying)
        {
            // 编辑器中预览（简化版）
            if (targetImage == null) targetImage = GetComponent<Image>();
            if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        }
    }
#endif
}