// Assets/Scripts/Editor/SFXLibraryEditor.cs
// SFXLibrary 编辑器增强 - 提供更好的配置体验
// 功能：自动填充名称、批量导入、验证检查

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(SFXLibrary))]
public class SFXLibraryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SFXLibrary library = (SFXLibrary)target;

        // 工具栏
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("📦 音效库工具", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🔄 自动填充名称", GUILayout.Height(25)))
        {
            AutoFillNames(library);
        }

        if (GUILayout.Button("✅ 验证配置", GUILayout.Height(25)))
        {
            ValidateLibrary(library);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("📋 导出名称列表", GUILayout.Height(25)))
        {
            ExportNameList(library);
        }

        if (GUILayout.Button("🧹 清除空项", GUILayout.Height(25)))
        {
            CleanEmptyEntries(library);
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "使用方法：\n" +
            "1. 将音效文件拖入对应分类的数组\n" +
            "2. 点击「自动填充名称」自动设置 sfxName\n" +
            "3. 现有代码中的路径会自动匹配（如 'Audio/SFX/shadow_appear' 会匹配 'shadow_appear'）",
            MessageType.Info);

        EditorGUILayout.Space();

        // 绘制默认 Inspector
        DrawDefaultInspector();
    }

    /// <summary>
    /// 自动从 AudioClip 名称填充 sfxName
    /// </summary>
    private void AutoFillNames(SFXLibrary library)
    {
        int filledCount = 0;

        filledCount += AutoFillArray(library.uiSounds);
        filledCount += AutoFillArray(library.itemSounds);
        filledCount += AutoFillArray(library.puzzleSounds);
        filledCount += AutoFillArray(library.ambientSounds);
        filledCount += AutoFillArray(library.miscSounds);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SFXLibrary] 自动填充完成，共填充 {filledCount} 个名称");
        EditorUtility.DisplayDialog("完成", $"已自动填充 {filledCount} 个音效名称", "确定");
    }

    private int AutoFillArray(SFXLibrary.SFXEntry[] entries)
    {
        if (entries == null) return 0;

        int count = 0;
        foreach (var entry in entries)
        {
            if (entry != null && entry.clip != null && string.IsNullOrEmpty(entry.sfxName))
            {
                entry.sfxName = entry.clip.name.ToLower();
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 验证库配置
    /// </summary>
    private void ValidateLibrary(SFXLibrary library)
    {
        List<string> issues = new List<string>();
        HashSet<string> names = new HashSet<string>();
        int totalCount = 0;

        ValidateArray(library.uiSounds, "UI音效", issues, names, ref totalCount);
        ValidateArray(library.itemSounds, "物品音效", issues, names, ref totalCount);
        ValidateArray(library.puzzleSounds, "谜题音效", issues, names, ref totalCount);
        ValidateArray(library.ambientSounds, "环境音效", issues, names, ref totalCount);
        ValidateArray(library.miscSounds, "其他音效", issues, names, ref totalCount);

        if (issues.Count == 0)
        {
            EditorUtility.DisplayDialog("验证通过", $"✅ 音效库配置正确\n共 {totalCount} 个音效", "确定");
        }
        else
        {
            string message = $"发现 {issues.Count} 个问题：\n\n" + string.Join("\n", issues.GetRange(0, Mathf.Min(10, issues.Count)));
            if (issues.Count > 10)
            {
                message += $"\n\n... 还有 {issues.Count - 10} 个问题，请查看 Console";
            }

            foreach (var issue in issues)
            {
                Debug.LogWarning($"[SFXLibrary] {issue}");
            }

            EditorUtility.DisplayDialog("验证结果", message, "确定");
        }
    }

    private void ValidateArray(SFXLibrary.SFXEntry[] entries, string category, List<string> issues, HashSet<string> names, ref int totalCount)
    {
        if (entries == null) return;

        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry == null) continue;

            totalCount++;

            if (entry.clip == null)
            {
                issues.Add($"[{category}][{i}] AudioClip 为空");
            }

            if (string.IsNullOrEmpty(entry.sfxName))
            {
                issues.Add($"[{category}][{i}] sfxName 为空");
            }
            else
            {
                string lowerName = entry.sfxName.ToLower();
                if (names.Contains(lowerName))
                {
                    issues.Add($"[{category}][{i}] 名称重复: {entry.sfxName}");
                }
                else
                {
                    names.Add(lowerName);
                }
            }
        }
    }

    /// <summary>
    /// 导出名称列表到剪贴板
    /// </summary>
    private void ExportNameList(SFXLibrary library)
    {
        List<string> names = new List<string>();

        CollectNames(library.uiSounds, "UI", names);
        CollectNames(library.itemSounds, "物品", names);
        CollectNames(library.puzzleSounds, "谜题", names);
        CollectNames(library.ambientSounds, "环境", names);
        CollectNames(library.miscSounds, "其他", names);

        string result = string.Join("\n", names);
        GUIUtility.systemCopyBuffer = result;

        Debug.Log($"[SFXLibrary] 名称列表已复制到剪贴板:\n{result}");
        EditorUtility.DisplayDialog("完成", $"已复制 {names.Count} 个音效名称到剪贴板", "确定");
    }

    private void CollectNames(SFXLibrary.SFXEntry[] entries, string category, List<string> names)
    {
        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.sfxName))
            {
                names.Add($"[{category}] {entry.sfxName}");
            }
        }
    }

    /// <summary>
    /// 清除空项
    /// </summary>
    private void CleanEmptyEntries(SFXLibrary library)
    {
        int removed = 0;

        library.uiSounds = CleanArray(library.uiSounds, ref removed);
        library.itemSounds = CleanArray(library.itemSounds, ref removed);
        library.puzzleSounds = CleanArray(library.puzzleSounds, ref removed);
        library.ambientSounds = CleanArray(library.ambientSounds, ref removed);
        library.miscSounds = CleanArray(library.miscSounds, ref removed);

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        Debug.Log($"[SFXLibrary] 已清除 {removed} 个空项");
        EditorUtility.DisplayDialog("完成", $"已清除 {removed} 个空项", "确定");
    }

    private SFXLibrary.SFXEntry[] CleanArray(SFXLibrary.SFXEntry[] entries, ref int removed)
    {
        if (entries == null) return new SFXLibrary.SFXEntry[0];

        List<SFXLibrary.SFXEntry> valid = new List<SFXLibrary.SFXEntry>();
        foreach (var entry in entries)
        {
            if (entry != null && entry.clip != null)
            {
                valid.Add(entry);
            }
            else
            {
                removed++;
            }
        }
        return valid.ToArray();
    }
}

/// <summary>
/// 快捷创建 SFXLibrary 资产
/// </summary>
public class SFXLibraryCreator
{
    [MenuItem("Assets/Create/Audio/SFX Library", false, 1)]
    public static void CreateSFXLibrary()
    {
        SFXLibrary asset = ScriptableObject.CreateInstance<SFXLibrary>();

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
        {
            path = "Assets";
        }
        else if (Path.GetExtension(path) != "")
        {
            path = Path.GetDirectoryName(path);
        }

        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{path}/SFXLibrary.asset");
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();

        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        Debug.Log($"[SFXLibrary] Created at: {assetPath}");
    }
}
#endif