// Assets/Scripts/Audio/SFXLibrary.cs
// 音效库 - 集中管理所有音效，支持拖拽配置
// 【重要】此系统完全兼容现有代码，无需修改任何谜题脚本

using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SFXLibrary", menuName = "Audio/SFX Library", order = 1)]
public class SFXLibrary : ScriptableObject
{
    [System.Serializable]
    public class SFXEntry
    {
        [Tooltip("音效名称（用于代码查找，如 'shadow_appear'）")]
        public string sfxName;

        [Tooltip("拖入音效文件")]
        public AudioClip clip;

        [Tooltip("备注（仅用于说明）")]
        public string note;
    }

    [Header("音效分类")]
    [Tooltip("通用UI音效")]
    public SFXEntry[] uiSounds;

    [Tooltip("物品相关音效")]
    public SFXEntry[] itemSounds;

    [Tooltip("谜题相关音效")]
    public SFXEntry[] puzzleSounds;

    [Tooltip("环境/氛围音效")]
    public SFXEntry[] ambientSounds;

    [Tooltip("其他音效")]
    public SFXEntry[] miscSounds;

    // 运行时查找缓存
    private Dictionary<string, AudioClip> _lookupCache;

    /// <summary>
    /// 根据名称查找音效
    /// </summary>
    public AudioClip GetClip(string sfxName)
    {
        if (string.IsNullOrEmpty(sfxName)) return null;

        // 初始化缓存
        if (_lookupCache == null)
        {
            BuildCache();
        }

        // 直接查找
        if (_lookupCache.TryGetValue(sfxName, out AudioClip clip))
        {
            return clip;
        }

        // 尝试从路径中提取名称 (如 "Audio/SFX/shadow_appear" → "shadow_appear")
        string extractedName = ExtractNameFromPath(sfxName);
        if (!string.IsNullOrEmpty(extractedName) && _lookupCache.TryGetValue(extractedName, out clip))
        {
            return clip;
        }

        return null;
    }

    /// <summary>
    /// 检查是否存在指定音效
    /// </summary>
    public bool HasClip(string sfxName)
    {
        return GetClip(sfxName) != null;
    }

    /// <summary>
    /// 构建查找缓存
    /// </summary>
    private void BuildCache()
    {
        _lookupCache = new Dictionary<string, AudioClip>();

        AddToCache(uiSounds);
        AddToCache(itemSounds);
        AddToCache(puzzleSounds);
        AddToCache(ambientSounds);
        AddToCache(miscSounds);

        Debug.Log($"[SFXLibrary] Cache built with {_lookupCache.Count} entries");
    }

    private void AddToCache(SFXEntry[] entries)
    {
        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (entry == null || string.IsNullOrEmpty(entry.sfxName) || entry.clip == null)
                continue;

            string key = entry.sfxName.ToLower().Trim();

            if (!_lookupCache.ContainsKey(key))
            {
                _lookupCache[key] = entry.clip;
            }
            else
            {
                Debug.LogWarning($"[SFXLibrary] Duplicate SFX name: {entry.sfxName}");
            }
        }
    }

    /// <summary>
    /// 从路径中提取文件名
    /// "Audio/SFX/shadow_appear" → "shadow_appear"
    /// </summary>
    private string ExtractNameFromPath(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // 移除扩展名
        int dotIndex = path.LastIndexOf('.');
        if (dotIndex > 0)
        {
            path = path.Substring(0, dotIndex);
        }

        // 获取最后一个 / 或 \ 之后的内容
        int slashIndex = Mathf.Max(path.LastIndexOf('/'), path.LastIndexOf('\\'));
        if (slashIndex >= 0 && slashIndex < path.Length - 1)
        {
            return path.Substring(slashIndex + 1).ToLower().Trim();
        }

        return path.ToLower().Trim();
    }

    /// <summary>
    /// 清除缓存（编辑器修改后调用）
    /// </summary>
    public void ClearCache()
    {
        _lookupCache = null;
    }

    private void OnValidate()
    {
        // 编辑器中修改后清除缓存
        _lookupCache = null;
    }

    /// <summary>
    /// 获取所有已配置的音效名称（用于编辑器工具）
    /// </summary>
    public List<string> GetAllSFXNames()
    {
        if (_lookupCache == null)
        {
            BuildCache();
        }

        return new List<string>(_lookupCache.Keys);
    }
}