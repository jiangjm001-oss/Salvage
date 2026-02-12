// Assets/Editor/FogTextureCreator.cs
// 编辑器工具 - 一键生成雾气纹理
using UnityEngine;
using UnityEditor;
using System.IO;

public class FogTextureCreator : EditorWindow
{
    private int textureWidth = 512;
    private int textureHeight = 256;
    private float noiseScale = 4f;
    private int octaves = 4;
    private float persistence = 0.5f;
    private Color fogColor = new Color(0.9f, 0.9f, 0.95f, 1f);
    private bool useRadialFalloff = true;
    private float falloffPower = 2f;
    private string savePath = "Assets/Textures/Fog";
    private string fileName = "FogTexture";

    private Texture2D previewTexture;

    [MenuItem("Tools/Fog Texture Creator")]
    public static void ShowWindow()
    {
        GetWindow<FogTextureCreator>("Fog Texture Creator");
    }

    private void OnGUI()
    {
        GUILayout.Label("雾气纹理生成器", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // 尺寸设置
        GUILayout.Label("纹理尺寸", EditorStyles.boldLabel);
        textureWidth = EditorGUILayout.IntSlider("宽度", textureWidth, 128, 1024);
        textureHeight = EditorGUILayout.IntSlider("高度", textureHeight, 128, 1024);

        EditorGUILayout.Space(10);

        // 噪声设置
        GUILayout.Label("噪声设置", EditorStyles.boldLabel);
        noiseScale = EditorGUILayout.Slider("噪声缩放", noiseScale, 1f, 10f);
        octaves = EditorGUILayout.IntSlider("叠加层数", octaves, 1, 8);
        persistence = EditorGUILayout.Slider("持续度", persistence, 0.1f, 1f);

        EditorGUILayout.Space(10);

        // 外观设置
        GUILayout.Label("外观设置", EditorStyles.boldLabel);
        fogColor = EditorGUILayout.ColorField("雾气颜色", fogColor);
        useRadialFalloff = EditorGUILayout.Toggle("径向衰减", useRadialFalloff);
        if (useRadialFalloff)
        {
            falloffPower = EditorGUILayout.Slider("衰减强度", falloffPower, 0.5f, 5f);
        }

        EditorGUILayout.Space(10);

        // 保存设置
        GUILayout.Label("保存设置", EditorStyles.boldLabel);
        savePath = EditorGUILayout.TextField("保存路径", savePath);
        fileName = EditorGUILayout.TextField("文件名", fileName);

        EditorGUILayout.Space(20);

        // 按钮
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("预览", GUILayout.Height(30)))
        {
            GeneratePreview();
        }

        if (GUILayout.Button("生成并保存", GUILayout.Height(30)))
        {
            GenerateAndSave();
        }

        EditorGUILayout.EndHorizontal();

        // 预设按钮
        EditorGUILayout.Space(10);
        GUILayout.Label("快速预设", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("路尽头雾气"))
        {
            ApplyPathEndPreset();
        }

        if (GUILayout.Button("标题萦绕雾气"))
        {
            ApplyTitleFogPreset();
        }

        if (GUILayout.Button("薄雾层"))
        {
            ApplyMistPreset();
        }

        EditorGUILayout.EndHorizontal();

        // 显示预览
        EditorGUILayout.Space(20);
        if (previewTexture != null)
        {
            GUILayout.Label("预览:", EditorStyles.boldLabel);

            float previewHeight = 150f;
            float previewWidth = previewHeight * ((float)textureWidth / textureHeight);

            Rect previewRect = GUILayoutUtility.GetRect(previewWidth, previewHeight);
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture);
        }
    }

    private void ApplyPathEndPreset()
    {
        textureWidth = 512;
        textureHeight = 256;
        noiseScale = 3f;
        octaves = 4;
        persistence = 0.5f;
        fogColor = new Color(0.85f, 0.85f, 0.9f, 1f);
        useRadialFalloff = false;
        fileName = "Fog_PathEnd";
        GeneratePreview();
    }

    private void ApplyTitleFogPreset()
    {
        textureWidth = 512;
        textureHeight = 256;
        noiseScale = 2.5f;
        octaves = 3;
        persistence = 0.6f;
        fogColor = new Color(0.9f, 0.9f, 0.95f, 1f);
        useRadialFalloff = true;
        falloffPower = 1.5f;
        fileName = "Fog_Title";
        GeneratePreview();
    }

    private void ApplyMistPreset()
    {
        textureWidth = 512;
        textureHeight = 512;
        noiseScale = 5f;
        octaves = 5;
        persistence = 0.45f;
        fogColor = new Color(0.95f, 0.95f, 0.97f, 1f);
        useRadialFalloff = true;
        falloffPower = 2.5f;
        fileName = "Fog_Mist";
        GeneratePreview();
    }

    private void GeneratePreview()
    {
        previewTexture = GenerateTexture();
        Repaint();
    }

    private void GenerateAndSave()
    {
        Texture2D texture = GenerateTexture();

        // 确保目录存在
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        // 保存为 PNG
        string fullPath = $"{savePath}/{fileName}.png";
        byte[] pngData = texture.EncodeToPNG();
        File.WriteAllBytes(fullPath, pngData);

        // 刷新资源
        AssetDatabase.Refresh();

        // 设置纹理导入设置
        TextureImporter importer = AssetImporter.GetAtPath(fullPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        Debug.Log($"[FogTextureCreator] 雾气纹理已保存到: {fullPath}");
        EditorUtility.DisplayDialog("完成", $"纹理已保存到:\n{fullPath}", "确定");

        // 选中生成的文件
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
    }

    private Texture2D GenerateTexture()
    {
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[textureWidth * textureHeight];

        // 随机偏移，每次生成不同的纹理
        float offsetX = Random.Range(0f, 1000f);
        float offsetY = Random.Range(0f, 1000f);

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float normalizedX = (float)x / textureWidth;
                float normalizedY = (float)y / textureHeight;

                // FBM 噪声
                float noiseValue = CalculateFBM(normalizedX + offsetX, normalizedY + offsetY);

                // 径向衰减
                float falloff = 1f;
                if (useRadialFalloff)
                {
                    float distFromCenter = Vector2.Distance(
                        new Vector2(normalizedX, normalizedY),
                        new Vector2(0.5f, 0.5f)
                    ) * 2f;
                    falloff = 1f - Mathf.Pow(Mathf.Clamp01(distFromCenter), falloffPower);
                }

                // 最终 alpha
                float alpha = noiseValue * falloff;
                alpha = Mathf.Clamp01(alpha);

                // 设置像素
                pixels[y * textureWidth + x] = new Color(fogColor.r, fogColor.g, fogColor.b, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return texture;
    }

    private float CalculateFBM(float x, float y)
    {
        float total = 0f;
        float frequency = noiseScale;
        float amplitude = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }

        return total / maxValue;
    }

    private void OnDestroy()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
        }
    }
}