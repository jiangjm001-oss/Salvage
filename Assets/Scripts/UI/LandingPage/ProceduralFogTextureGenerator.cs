// Assets/Scripts/UI/LandingPage/ProceduralFogTextureGenerator.cs
// 程序化雾气纹理生成器 - 创建柔和的云雾纹理
using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class ProceduralFogTextureGenerator : MonoBehaviour
{
    [Header("=== 纹理设置 ===")]
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 256;

    [Header("=== 噪声设置 ===")]
    [SerializeField] private float noiseScale = 4f;
    [SerializeField] private int octaves = 4;
    [SerializeField][Range(0f, 1f)] private float persistence = 0.5f;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private Vector2 noiseOffset = Vector2.zero;

    [Header("=== 颜色设置 ===")]
    [SerializeField] private Color fogColor = new Color(0.8f, 0.8f, 0.85f, 0.6f);
    [SerializeField][Range(0f, 1f)] private float centerAlpha = 0.8f;
    [SerializeField][Range(0f, 1f)] private float edgeAlpha = 0f;

    [Header("=== 形状设置 ===")]
    [SerializeField] private bool useRadialFalloff = true;
    [SerializeField] private float radialFalloffPower = 2f;
    [SerializeField] private bool useHorizontalGradient = false;
    [SerializeField] private AnimationCurve horizontalFalloff = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] private bool useVerticalGradient = true;
    [SerializeField] private AnimationCurve verticalFalloff = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("=== 目标组件 ===")]
    [SerializeField] private Image targetImage;
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    [Header("=== 生成控制 ===")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool autoApplyToTarget = true;

    private Texture2D generatedTexture;
    private Sprite generatedSprite;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateAndApply();
        }
    }

    /// <summary>
    /// 生成纹理并应用到目标组件
    /// </summary>
    [ContextMenu("Generate Fog Texture")]
    public void GenerateAndApply()
    {
        GenerateTexture();

        if (autoApplyToTarget)
        {
            ApplyToTarget();
        }
    }

    /// <summary>
    /// 生成雾气纹理
    /// </summary>
    public Texture2D GenerateTexture()
    {
        // 创建纹理
        generatedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        generatedTexture.wrapMode = TextureWrapMode.Clamp;
        generatedTexture.filterMode = FilterMode.Bilinear;

        Color[] pixels = new Color[textureWidth * textureHeight];

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                // 归一化坐标 (0-1)
                float normalizedX = (float)x / textureWidth;
                float normalizedY = (float)y / textureHeight;

                // 计算噪声值
                float noiseValue = CalculateFBMNoise(normalizedX, normalizedY);

                // 计算衰减
                float falloff = CalculateFalloff(normalizedX, normalizedY);

                // 最终Alpha
                float alpha = noiseValue * falloff * Mathf.Lerp(edgeAlpha, centerAlpha, falloff);
                alpha = Mathf.Clamp01(alpha);

                // 设置像素颜色
                Color pixelColor = new Color(fogColor.r, fogColor.g, fogColor.b, alpha * fogColor.a);
                pixels[y * textureWidth + x] = pixelColor;
            }
        }

        generatedTexture.SetPixels(pixels);
        generatedTexture.Apply();

        // 创建Sprite
        generatedSprite = Sprite.Create(
            generatedTexture,
            new Rect(0, 0, textureWidth, textureHeight),
            new Vector2(0.5f, 0.5f),
            100f
        );

        Debug.Log($"[ProceduralFogTextureGenerator] 纹理生成完成: {textureWidth}x{textureHeight}");

        return generatedTexture;
    }

    /// <summary>
    /// 计算FBM噪声
    /// </summary>
    private float CalculateFBMNoise(float x, float y)
    {
        float total = 0f;
        float frequency = noiseScale;
        float amplitude = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float sampleX = (x + noiseOffset.x) * frequency;
            float sampleY = (y + noiseOffset.y) * frequency;

            float noiseValue = Mathf.PerlinNoise(sampleX, sampleY);
            total += noiseValue * amplitude;

            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }

    /// <summary>
    /// 计算衰减值
    /// </summary>
    private float CalculateFalloff(float x, float y)
    {
        float falloff = 1f;

        // 径向衰减
        if (useRadialFalloff)
        {
            float distFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f)) * 2f;
            float radialFalloff = 1f - Mathf.Pow(Mathf.Clamp01(distFromCenter), radialFalloffPower);
            falloff *= radialFalloff;
        }

        // 水平渐变
        if (useHorizontalGradient)
        {
            falloff *= horizontalFalloff.Evaluate(x);
        }

        // 垂直渐变（通常用于路尽头的雾，底部浓顶部淡）
        if (useVerticalGradient)
        {
            falloff *= verticalFalloff.Evaluate(y);
        }

        return Mathf.Clamp01(falloff);
    }

    /// <summary>
    /// 应用到目标组件
    /// </summary>
    public void ApplyToTarget()
    {
        if (generatedTexture == null)
        {
            Debug.LogWarning("[ProceduralFogTextureGenerator] 请先生成纹理");
            return;
        }

        // 应用到Image
        if (targetImage != null && generatedSprite != null)
        {
            targetImage.sprite = generatedSprite;
            Debug.Log("[ProceduralFogTextureGenerator] 已应用到Image组件");
        }

        // 应用到RawImage
        if (targetRawImage != null)
        {
            targetRawImage.texture = generatedTexture;
            Debug.Log("[ProceduralFogTextureGenerator] 已应用到RawImage组件");
        }

        // 应用到SpriteRenderer
        if (targetSpriteRenderer != null && generatedSprite != null)
        {
            targetSpriteRenderer.sprite = generatedSprite;
            Debug.Log("[ProceduralFogTextureGenerator] 已应用到SpriteRenderer组件");
        }
    }

    /// <summary>
    /// 获取生成的纹理
    /// </summary>
    public Texture2D GetGeneratedTexture()
    {
        return generatedTexture;
    }

    /// <summary>
    /// 获取生成的Sprite
    /// </summary>
    public Sprite GetGeneratedSprite()
    {
        return generatedSprite;
    }

    /// <summary>
    /// 设置噪声偏移（用于动画）
    /// </summary>
    public void SetNoiseOffset(Vector2 offset)
    {
        noiseOffset = offset;
    }

    /// <summary>
    /// 在编辑器中预览
    /// </summary>
    [ContextMenu("Preview In Editor")]
    private void PreviewInEditor()
    {
        GenerateAndApply();
    }

    /// <summary>
    /// 清理生成的资源
    /// </summary>
    private void OnDestroy()
    {
        if (generatedTexture != null)
        {
            if (Application.isPlaying)
            {
                Destroy(generatedTexture);
            }
            else
            {
                DestroyImmediate(generatedTexture);
            }
        }

        if (generatedSprite != null)
        {
            if (Application.isPlaying)
            {
                Destroy(generatedSprite);
            }
            else
            {
                DestroyImmediate(generatedSprite);
            }
        }
    }

    // ============ 预设配置 ============

    /// <summary>
    /// 应用"路尽头雾气"预设
    /// </summary>
    [ContextMenu("Apply Preset: Path End Fog")]
    public void ApplyPresetPathEndFog()
    {
        noiseScale = 3f;
        octaves = 4;
        persistence = 0.5f;
        fogColor = new Color(0.75f, 0.75f, 0.8f, 0.7f);
        useRadialFalloff = false;
        useHorizontalGradient = false;
        useVerticalGradient = true;
        verticalFalloff = new AnimationCurve(
            new Keyframe(0f, 0.2f),
            new Keyframe(0.3f, 0.8f),
            new Keyframe(0.6f, 1f),
            new Keyframe(1f, 0.6f)
        );
        centerAlpha = 0.9f;
        edgeAlpha = 0.1f;

        GenerateAndApply();
    }

    /// <summary>
    /// 应用"标题萦绕雾气"预设
    /// </summary>
    [ContextMenu("Apply Preset: Title Fog")]
    public void ApplyPresetTitleFog()
    {
        noiseScale = 2f;
        octaves = 3;
        persistence = 0.6f;
        fogColor = new Color(0.85f, 0.85f, 0.9f, 0.5f);
        useRadialFalloff = true;
        radialFalloffPower = 1.5f;
        useHorizontalGradient = false;
        useVerticalGradient = false;
        centerAlpha = 0.6f;
        edgeAlpha = 0f;

        GenerateAndApply();
    }

    /// <summary>
    /// 应用"全屏薄雾"预设
    /// </summary>
    [ContextMenu("Apply Preset: Full Screen Mist")]
    public void ApplyPresetFullScreenMist()
    {
        noiseScale = 5f;
        octaves = 5;
        persistence = 0.45f;
        fogColor = new Color(0.9f, 0.9f, 0.92f, 0.3f);
        useRadialFalloff = false;
        useHorizontalGradient = false;
        useVerticalGradient = false;
        centerAlpha = 0.4f;
        edgeAlpha = 0.2f;

        GenerateAndApply();
    }
}