// Assets/Scripts/Editor/SFXFieldDrawer.cs
// SFX 字段自动识别系统 - 核心工具类
// 提供 AudioClip 缓存和路径转换功能
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// SFX 字段自动识别工具
/// 提供 AudioClip 缓存、命名规则识别、路径转换等核心功能
/// </summary>
[InitializeOnLoad]
public static class SFXFieldAutoDrawer
{
    // 识别规则：字段名包含这些后缀的 string 字段会被自动处理
    private static readonly string[] SFX_FIELD_SUFFIXES = new string[]
    {
        "SoundName",    // pickupSoundName, triggerSoundName
        "Sound",        // containerOpenSound, containerCloseSound  
        "SFX",          // addItemSFX, errorSFX
        "AudioPath",    // 备用命名
        "SfxPath"       // 备用命名
    };

    // AudioClip 缓存
    private static Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private static Dictionary<string, string> pathCache = new Dictionary<string, string>();
    private static bool cacheInitialized = false;

    static SFXFieldAutoDrawer()
    {
        EditorApplication.delayCall += InitializeCacheIfNeeded;
    }

    /// <summary>
    /// 检查字段名是否符合 SFX 命名规则
    /// </summary>
    public static bool IsSFXField(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName)) return false;

        foreach (var suffix in SFX_FIELD_SUFFIXES)
        {
            if (fieldName.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 初始化 AudioClip 缓存
    /// </summary>
    public static void InitializeCacheIfNeeded()
    {
        if (cacheInitialized) return;
        RefreshCache();
    }

    /// <summary>
    /// 刷新 AudioClip 缓存
    /// </summary>
    public static void RefreshCache()
    {
        clipCache.Clear();
        pathCache.Clear();

        // 搜索 Audio 文件夹中的所有 AudioClip
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });

        // 如果 Audio 文件夹不存在，搜索整个 Assets
        if (guids.Length == 0)
        {
            guids = AssetDatabase.FindAssets("t:AudioClip");
        }

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);

            if (clip != null)
            {
                // 使用 clip 名称作为 key
                if (!clipCache.ContainsKey(clip.name))
                {
                    clipCache[clip.name] = clip;
                    pathCache[clip.name] = path;
                }

                // 同时用标准化路径作为备用 key
                string normalizedPath = NormalizePath(path);
                if (!clipCache.ContainsKey(normalizedPath))
                {
                    clipCache[normalizedPath] = clip;
                    pathCache[normalizedPath] = path;
                }
            }
        }

        cacheInitialized = true;
        Debug.Log($"[SFXFieldDrawer] 缓存已刷新，共 {clipCache.Count / 2} 个音效文件");
    }

    /// <summary>
    /// 标准化路径（去掉 Assets/ 和扩展名）
    /// </summary>
    private static string NormalizePath(string assetPath)
    {
        string result = assetPath;

        if (result.StartsWith("Assets/"))
        {
            result = result.Substring(7);
        }

        int lastDot = result.LastIndexOf('.');
        if (lastDot > 0)
        {
            result = result.Substring(0, lastDot);
        }

        return result;
    }

    /// <summary>
    /// 根据名称或路径查找 AudioClip
    /// </summary>
    public static AudioClip FindClip(string nameOrPath)
    {
        if (string.IsNullOrEmpty(nameOrPath)) return null;

        InitializeCacheIfNeeded();

        // 1. 直接查找
        if (clipCache.TryGetValue(nameOrPath, out AudioClip clip))
        {
            return clip;
        }

        // 2. 标准化后查找
        string normalized = NormalizePath(nameOrPath);
        if (clipCache.TryGetValue(normalized, out clip))
        {
            return clip;
        }

        // 3. 只用文件名查找
        string fileName = System.IO.Path.GetFileNameWithoutExtension(nameOrPath);
        if (!string.IsNullOrEmpty(fileName) && clipCache.TryGetValue(fileName, out clip))
        {
            return clip;
        }

        return null;
    }

    /// <summary>
    /// 获取 AudioClip 的 Resources 路径（用于运行时加载）
    /// </summary>
    public static string GetResourcesPath(AudioClip clip)
    {
        if (clip == null) return "";

        string assetPath = AssetDatabase.GetAssetPath(clip);

        // 检查是否在 Resources 文件夹中
        int resourcesIndex = assetPath.IndexOf("/Resources/");
        if (resourcesIndex >= 0)
        {
            string resourcePath = assetPath.Substring(resourcesIndex + 11);
            int lastDot = resourcePath.LastIndexOf('.');
            if (lastDot > 0)
            {
                resourcePath = resourcePath.Substring(0, lastDot);
            }
            return resourcePath;
        }

        // 不在 Resources 中，返回标准化路径
        return NormalizePath(assetPath);
    }
}