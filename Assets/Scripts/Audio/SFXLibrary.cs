// Assets/Scripts/Audio/SFXLibrary.cs
// 音效库 - ScriptableObject
// 支持两种调用方式：
// 1. 完整路径: PlaySFX("Audio/SFX/lv1_wallA/Towel_Water")
// 2. 纯文件名: PlaySFX("Towel_Water") 或 PlaySFX("item_pickup")

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 音效条目
/// </summary>
[System.Serializable]
public class SFXEntry
{
    [Tooltip("音效名称（用于代码中调用）")]
    public string name;

    [Tooltip("音频文件")]
    public AudioClip clip;

    [Tooltip("音量倍数")]
    [Range(0f, 2f)]
    public float volume = 1f;

    [Tooltip("备注")]
    public string note;
}

/// <summary>
/// 音效库 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "SFXLibrary", menuName = "Audio/SFX Library", order = 1)]
public class SFXLibrary : ScriptableObject
{
    [Header("音效列表")]
    [Tooltip("在这里配置所有游戏音效")]
    public List<SFXEntry> sfxEntries = new List<SFXEntry>();

    // 运行时缓存 - 同时存储完整路径和纯文件名
    private Dictionary<string, SFXEntry> sfxCache = new Dictionary<string, SFXEntry>();
    private bool isCacheBuilt = false;

    /// <summary>
    /// 构建缓存
    /// 【关键改进】每个音效同时注册：完整路径 + 纯文件名 + clip名称
    /// </summary>
    public void BuildCache()
    {
        sfxCache.Clear();

        foreach (var entry in sfxEntries)
        {
            if (string.IsNullOrEmpty(entry.name))
            {
                Debug.LogWarning("[SFXLibrary] 发现空名称的音效条目，已跳过");
                continue;
            }

            // 1. 注册完整路径（小写）
            string fullPathKey = entry.name.ToLower();
            if (!sfxCache.ContainsKey(fullPathKey))
            {
                sfxCache[fullPathKey] = entry;
            }

            // 2. 同时注册纯文件名（小写）
            string fileNameKey = ExtractFileName(entry.name).ToLower();
            if (fileNameKey != fullPathKey && !sfxCache.ContainsKey(fileNameKey))
            {
                sfxCache[fileNameKey] = entry;
            }

            // 3. 如果有 AudioClip，也用 clip 的名称注册（兜底）
            if (entry.clip != null)
            {
                string clipNameKey = entry.clip.name.ToLower();
                if (!sfxCache.ContainsKey(clipNameKey))
                {
                    sfxCache[clipNameKey] = entry;
                }
            }
        }

        isCacheBuilt = true;
        Debug.Log($"[SFXLibrary] 缓存构建完成，共 {sfxEntries.Count} 个音效，{sfxCache.Count} 个查找键");
    }

    public void ClearCache()
    {
        sfxCache.Clear();
        isCacheBuilt = false;
    }

    /// <summary>
    /// 获取音效 Clip
    /// 支持多种查找方式：完整路径、纯文件名、clip名称
    /// </summary>
    public AudioClip GetClip(string sfxName)
    {
        if (!isCacheBuilt) BuildCache();
        if (string.IsNullOrEmpty(sfxName)) return null;

        // 尝试直接查找（小写）
        string key = sfxName.ToLower();
        if (sfxCache.TryGetValue(key, out SFXEntry entry))
        {
            return entry.clip;
        }

        // 尝试提取文件名后查找
        string fileName = ExtractFileName(sfxName).ToLower();
        if (fileName != key && sfxCache.TryGetValue(fileName, out entry))
        {
            return entry.clip;
        }

        // 尝试去掉下划线和连字符后模糊匹配
        string normalizedKey = NormalizeKey(sfxName);
        foreach (var kvp in sfxCache)
        {
            if (NormalizeKey(kvp.Key) == normalizedKey)
            {
                return kvp.Value.clip;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取音效音量
    /// </summary>
    public float GetVolume(string sfxName)
    {
        if (!isCacheBuilt) BuildCache();
        if (string.IsNullOrEmpty(sfxName)) return 1f;

        string key = sfxName.ToLower();
        if (sfxCache.TryGetValue(key, out SFXEntry entry))
        {
            return entry.volume;
        }

        string fileName = ExtractFileName(sfxName).ToLower();
        if (fileName != key && sfxCache.TryGetValue(fileName, out entry))
        {
            return entry.volume;
        }

        return 1f;
    }

    /// <summary>
    /// 检查音效是否存在
    /// </summary>
    public bool HasSFX(string sfxName)
    {
        if (!isCacheBuilt) BuildCache();
        if (string.IsNullOrEmpty(sfxName)) return false;

        string key = sfxName.ToLower();
        if (sfxCache.ContainsKey(key)) return true;

        string fileName = ExtractFileName(sfxName).ToLower();
        return fileName != key && sfxCache.ContainsKey(fileName);
    }

    /// <summary>
    /// 获取所有音效名称
    /// </summary>
    public List<string> GetAllSFXNames()
    {
        List<string> names = new List<string>();
        foreach (var entry in sfxEntries)
        {
            if (!string.IsNullOrEmpty(entry.name))
            {
                names.Add(entry.name);
            }
        }
        return names;
    }

    /// <summary>
    /// 调试：打印所有注册的查找键
    /// </summary>
    public void DebugPrintAllKeys()
    {
        if (!isCacheBuilt) BuildCache();

        Debug.Log("=== [SFXLibrary] 所有注册的查找键 ===");
        foreach (var kvp in sfxCache)
        {
            string clipName = kvp.Value.clip != null ? kvp.Value.clip.name : "NULL";
            Debug.Log($"  [{kvp.Key}] → {clipName}");
        }
        Debug.Log($"=== 共 {sfxCache.Count} 个查找键 ===");
    }

    /// <summary>
    /// 从路径中提取文件名
    /// </summary>
    private string ExtractFileName(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        int lastSlash = path.LastIndexOf('/');
        if (lastSlash >= 0 && lastSlash < path.Length - 1)
        {
            return path.Substring(lastSlash + 1);
        }

        int lastBackSlash = path.LastIndexOf('\\');
        if (lastBackSlash >= 0 && lastBackSlash < path.Length - 1)
        {
            return path.Substring(lastBackSlash + 1);
        }

        return path;
    }

    /// <summary>
    /// 标准化键（用于模糊匹配）
    /// </summary>
    private string NormalizeKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        return key.ToLower().Replace("_", "").Replace("-", "").Replace(" ", "");
    }
}