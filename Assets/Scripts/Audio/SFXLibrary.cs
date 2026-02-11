// Assets/Scripts/Audio/SFXLibrary.cs
// 音效库 - 通过名称或拖拽管理所有游戏音效

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音效条目 - 将名称映射到AudioClip
/// </summary>
[System.Serializable]
public class SFXEntry
{
    [Tooltip("音效名称（用于代码中调用）")]
    public string name;

    [Tooltip("音效文件（直接拖拽）")]
    public AudioClip clip;

    [Tooltip("音量（0-1）")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("备注说明")]
    public string description;
}

/// <summary>
/// 音效库 - ScriptableObject
/// 在Project窗口右键 -> Create -> Audio -> SFX Library 创建
/// </summary>
[CreateAssetMenu(fileName = "SFXLibrary", menuName = "Audio/SFX Library", order = 1)]
public class SFXLibrary : ScriptableObject
{
    [Header("音效列表")]
    [Tooltip("所有音效条目，名称用于代码调用")]
    public List<SFXEntry> sfxEntries = new List<SFXEntry>();

    // 运行时缓存（名称 -> 条目）
    private Dictionary<string, SFXEntry> _cache;

    /// <summary>
    /// 获取缓存字典（懒加载）
    /// </summary>
    private Dictionary<string, SFXEntry> Cache
    {
        get
        {
            if (_cache == null)
            {
                BuildCache();
            }
            return _cache;
        }
    }

    /// <summary>
    /// 构建查找缓存
    /// </summary>
    public void BuildCache()
    {
        _cache = new Dictionary<string, SFXEntry>();

        foreach (var entry in sfxEntries)
        {
            if (entry == null || entry.clip == null) continue;

            string key = entry.name;

            // 如果名称为空，使用AudioClip的名称
            if (string.IsNullOrEmpty(key))
            {
                key = entry.clip.name;
                entry.name = key; // 自动填充名称
            }

            // 转为小写以支持大小写不敏感查找
            string lowerKey = key.ToLower();

            if (!_cache.ContainsKey(lowerKey))
            {
                _cache[lowerKey] = entry;
            }
            else
            {
                Debug.LogWarning($"[SFXLibrary] 重复的音效名称: {key}");
            }
        }

        Debug.Log($"[SFXLibrary] 缓存已构建，共 {_cache.Count} 个音效");
    }

    /// <summary>
    /// 通过名称获取音效条目
    /// </summary>
    /// <param name="sfxName">音效名称（大小写不敏感）</param>
    /// <returns>音效条目，未找到返回null</returns>
    public SFXEntry GetEntry(string sfxName)
    {
        if (string.IsNullOrEmpty(sfxName)) return null;

        // 处理可能的路径格式（兼容旧代码）
        // 例如 "Audio/SFX/lv1_wallA/Burn" -> "Burn"
        string cleanName = ExtractClipName(sfxName);
        string lowerName = cleanName.ToLower();

        if (Cache.TryGetValue(lowerName, out SFXEntry entry))
        {
            return entry;
        }

        return null;
    }

    /// <summary>
    /// 通过名称获取AudioClip
    /// </summary>
    /// <param name="sfxName">音效名称</param>
    /// <returns>AudioClip，未找到返回null</returns>
    public AudioClip GetClip(string sfxName)
    {
        var entry = GetEntry(sfxName);
        return entry?.clip;
    }

    /// <summary>
    /// 获取音效音量
    /// </summary>
    public float GetVolume(string sfxName)
    {
        var entry = GetEntry(sfxName);
        return entry?.volume ?? 1f;
    }

    /// <summary>
    /// 检查是否存在指定音效
    /// </summary>
    public bool HasSFX(string sfxName)
    {
        return GetEntry(sfxName) != null;
    }

    /// <summary>
    /// 从路径中提取音效名称
    /// 支持格式：
    /// - "Burn" (直接名称)
    /// - "Audio/SFX/lv1_wallA/Burn" (完整路径)
    /// - "lv1_wallA/Burn" (部分路径)
    /// </summary>
    private string ExtractClipName(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // 移除可能的文件扩展名
        int extIndex = input.LastIndexOf('.');
        if (extIndex > 0)
        {
            input = input.Substring(0, extIndex);
        }

        // 获取最后一个路径段
        int lastSlash = input.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < input.Length - 1)
        {
            return input.Substring(lastSlash + 1);
        }

        int lastBackslash = input.LastIndexOf('\\');
        if (lastBackslash >= 0 && lastBackslash < input.Length - 1)
        {
            return input.Substring(lastBackslash + 1);
        }

        return input;
    }

    /// <summary>
    /// 清除缓存（编辑器中修改后调用）
    /// </summary>
    public void ClearCache()
    {
        _cache = null;
    }

    /// <summary>
    /// 在编辑器中验证时自动构建缓存
    /// </summary>
    private void OnValidate()
    {
        // 编辑器中修改时清除缓存
        _cache = null;
    }

    /// <summary>
    /// 获取所有音效名称（用于调试或编辑器）
    /// </summary>
    public List<string> GetAllSFXNames()
    {
        List<string> names = new List<string>();
        foreach (var entry in sfxEntries)
        {
            if (entry != null && entry.clip != null)
            {
                names.Add(string.IsNullOrEmpty(entry.name) ? entry.clip.name : entry.name);
            }
        }
        return names;
    }
}