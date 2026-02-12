// Assets/Scripts/UI/LandingPage/FogEffectController.cs
// 雾气效果控制器 - 使用多层UI图像实现柔和的雾气动效
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class FogEffectController : MonoBehaviour
{
    [System.Serializable]
    public class FogLayer
    {
        [Header("基础设置")]
        public RectTransform fogTransform;
        public Image fogImage;

        [Header("移动设置")]
        [Tooltip("水平移动速度")]
        public float horizontalSpeed = 10f;
        [Tooltip("垂直漂浮速度")]
        public float verticalFloatSpeed = 5f;
        [Tooltip("垂直漂浮幅度")]
        public float verticalFloatAmplitude = 10f;

        [Header("缩放呼吸效果")]
        public bool enableBreathing = true;
        [Tooltip("呼吸速度")]
        public float breathingSpeed = 0.3f;
        [Tooltip("呼吸幅度 (0.05 = 5%)")]
        public float breathingAmplitude = 0.05f;

        [Header("透明度脉动")]
        public bool enableAlphaPulse = true;
        [Tooltip("透明度脉动速度")]
        public float alphaPulseSpeed = 0.5f;
        [Tooltip("最小透明度")]
        [Range(0f, 1f)] public float minAlpha = 0.3f;
        [Tooltip("最大透明度")]
        [Range(0f, 1f)] public float maxAlpha = 0.7f;

        // 运行时数据
        [HideInInspector] public Vector2 originalPosition;
        [HideInInspector] public Vector3 originalScale;
        [HideInInspector] public float timeOffset;
        [HideInInspector] public Color originalColor;
    }

    [Header("=== 雾气层配置 ===")]
    [SerializeField] private List<FogLayer> fogLayers = new List<FogLayer>();

    [Header("=== 全局设置 ===")]
    [SerializeField] private float globalFadeInDuration = 2.0f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("=== 路径尽头雾气（背景内）===")]
    [SerializeField] private RectTransform pathFogTransform;
    [SerializeField] private Image pathFogImage;
    [SerializeField] private float pathFogPulseSpeed = 0.4f;
    [SerializeField][Range(0f, 1f)] private float pathFogMinAlpha = 0.5f;
    [SerializeField][Range(0f, 1f)] private float pathFogMaxAlpha = 0.9f;
    [SerializeField] private float pathFogScaleSpeed = 0.2f;
    [SerializeField] private float pathFogScaleAmplitude = 0.03f;

    [Header("=== 标题雾气层 ===")]
    [SerializeField] private List<FogLayer> titleFogLayers = new List<FogLayer>();

    // 运行状态
    private bool isRunning = false;
    private Coroutine fadeInCoroutine;
    private Vector3 pathFogOriginalScale;
    private Color pathFogOriginalColor;

    private void Awake()
    {
        // 初始化所有雾气层
        InitializeFogLayers(fogLayers);
        InitializeFogLayers(titleFogLayers);
        InitializePathFog();

        // 初始隐藏所有雾气
        SetAllFogAlpha(0f);
    }

    private void InitializeFogLayers(List<FogLayer> layers)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (layer.fogTransform != null)
            {
                layer.originalPosition = layer.fogTransform.anchoredPosition;
                layer.originalScale = layer.fogTransform.localScale;
                layer.timeOffset = Random.Range(0f, Mathf.PI * 2f); // 随机相位
            }
            if (layer.fogImage != null)
            {
                layer.originalColor = layer.fogImage.color;
            }
        }
    }

    private void InitializePathFog()
    {
        if (pathFogTransform != null)
        {
            pathFogOriginalScale = pathFogTransform.localScale;
        }
        if (pathFogImage != null)
        {
            pathFogOriginalColor = pathFogImage.color;
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        float time = Time.time;

        // 更新所有雾气层
        UpdateFogLayers(fogLayers, time);
        UpdateFogLayers(titleFogLayers, time);
        UpdatePathFog(time);
    }

    private void UpdateFogLayers(List<FogLayer> layers, float time)
    {
        foreach (var layer in layers)
        {
            if (layer.fogTransform == null) continue;

            float layerTime = time + layer.timeOffset;

            // 1. 水平移动（缓慢漂移）
            float horizontalOffset = Mathf.Sin(layerTime * layer.horizontalSpeed * 0.1f) * 20f;

            // 2. 垂直漂浮
            float verticalOffset = Mathf.Sin(layerTime * layer.verticalFloatSpeed * 0.1f) * layer.verticalFloatAmplitude;

            // 应用位置
            layer.fogTransform.anchoredPosition = layer.originalPosition + new Vector2(horizontalOffset, verticalOffset);

            // 3. 呼吸缩放效果
            if (layer.enableBreathing)
            {
                float breathingScale = 1f + Mathf.Sin(layerTime * layer.breathingSpeed) * layer.breathingAmplitude;
                layer.fogTransform.localScale = layer.originalScale * breathingScale;
            }

            // 4. 透明度脉动
            if (layer.enableAlphaPulse && layer.fogImage != null)
            {
                float alphaNormalized = (Mathf.Sin(layerTime * layer.alphaPulseSpeed) + 1f) * 0.5f;
                float alpha = Mathf.Lerp(layer.minAlpha, layer.maxAlpha, alphaNormalized);

                Color c = layer.fogImage.color;
                c.a = alpha;
                layer.fogImage.color = c;
            }
        }
    }

    private void UpdatePathFog(float time)
    {
        if (pathFogImage == null) return;

        // 透明度脉动
        float alphaNormalized = (Mathf.Sin(time * pathFogPulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(pathFogMinAlpha, pathFogMaxAlpha, alphaNormalized);

        Color c = pathFogImage.color;
        c.a = alpha;
        pathFogImage.color = c;

        // 轻微缩放脉动
        if (pathFogTransform != null)
        {
            float scale = 1f + Mathf.Sin(time * pathFogScaleSpeed) * pathFogScaleAmplitude;
            pathFogTransform.localScale = pathFogOriginalScale * scale;
        }
    }

    /// <summary>
    /// 启动雾气效果（带渐入）
    /// </summary>
    public void StartFog()
    {
        if (fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
        }
        fadeInCoroutine = StartCoroutine(FadeInFog());
    }

    /// <summary>
    /// 停止雾气效果
    /// </summary>
    public void StopFog()
    {
        isRunning = false;
        if (fadeInCoroutine != null)
        {
            StopCoroutine(fadeInCoroutine);
            fadeInCoroutine = null;
        }
        SetAllFogAlpha(0f);
    }

    /// <summary>
    /// 雾气渐入动画
    /// </summary>
    private IEnumerator FadeInFog()
    {
        isRunning = true;
        float elapsed = 0f;

        while (elapsed < globalFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / globalFadeInDuration);
            float easedT = fadeInCurve.Evaluate(t);

            // 渐入所有雾气层（作为基础乘数）
            SetAllFogBaseAlpha(easedT);

            yield return null;
        }

        SetAllFogBaseAlpha(1f);
        Debug.Log("[FogEffectController] 雾气渐入完成");
    }

    /// <summary>
    /// 设置所有雾气的基础透明度（与脉动效果叠加）
    /// </summary>
    private void SetAllFogBaseAlpha(float baseAlpha)
    {
        foreach (var layer in fogLayers)
        {
            if (layer.fogImage != null)
            {
                Color c = layer.fogImage.color;
                // 基础透明度 * 脉动范围的中间值
                c.a = baseAlpha * ((layer.minAlpha + layer.maxAlpha) * 0.5f);
                layer.fogImage.color = c;
            }
        }

        foreach (var layer in titleFogLayers)
        {
            if (layer.fogImage != null)
            {
                Color c = layer.fogImage.color;
                c.a = baseAlpha * ((layer.minAlpha + layer.maxAlpha) * 0.5f);
                layer.fogImage.color = c;
            }
        }

        if (pathFogImage != null)
        {
            Color c = pathFogImage.color;
            c.a = baseAlpha * ((pathFogMinAlpha + pathFogMaxAlpha) * 0.5f);
            pathFogImage.color = c;
        }
    }

    /// <summary>
    /// 直接设置所有雾气透明度
    /// </summary>
    private void SetAllFogAlpha(float alpha)
    {
        foreach (var layer in fogLayers)
        {
            if (layer.fogImage != null)
            {
                Color c = layer.fogImage.color;
                c.a = alpha;
                layer.fogImage.color = c;
            }
        }

        foreach (var layer in titleFogLayers)
        {
            if (layer.fogImage != null)
            {
                Color c = layer.fogImage.color;
                c.a = alpha;
                layer.fogImage.color = c;
            }
        }

        if (pathFogImage != null)
        {
            Color c = pathFogImage.color;
            c.a = alpha;
            pathFogImage.color = c;
        }
    }

    /// <summary>
    /// 编辑器中添加雾气层的便捷方法
    /// </summary>
    [ContextMenu("Add New Fog Layer")]
    private void AddNewFogLayer()
    {
        fogLayers.Add(new FogLayer
        {
            horizontalSpeed = Random.Range(5f, 15f),
            verticalFloatSpeed = Random.Range(3f, 8f),
            verticalFloatAmplitude = Random.Range(5f, 15f),
            breathingSpeed = Random.Range(0.2f, 0.5f),
            breathingAmplitude = Random.Range(0.03f, 0.08f),
            alphaPulseSpeed = Random.Range(0.3f, 0.7f),
            minAlpha = 0.3f,
            maxAlpha = 0.7f
        });
    }
}