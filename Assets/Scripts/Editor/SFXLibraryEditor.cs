// Assets/Scripts/Editor/SFXLibraryEditor.cs
// 编辑器工具：自动扫描音效文件夹并填充 SFXLibrary
// 注意：菜单项 "Create → Audio → SFX Library" 已在 SFXLibrary.cs 中通过 CreateAssetMenu 定义

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

[CustomEditor(typeof(SFXLibrary))]
public class SFXLibraryEditor : Editor
{
    private string scanFolder = "Assets/Audio/SFX";
    private bool usePathAsName = true;
    private bool includeSubfolders = true;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SFXLibrary library = (SFXLibrary)target;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("自动扫描工具", EditorStyles.boldLabel);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // 扫描文件夹设置
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("扫描文件夹:", GUILayout.Width(80));
        scanFolder = EditorGUILayout.TextField(scanFolder);
        if (GUILayout.Button("选择", GUILayout.Width(50)))
        {
            string selected = EditorUtility.OpenFolderPanel("选择音效文件夹", "Assets", "");
            if (!string.IsNullOrEmpty(selected))
            {
                if (selected.StartsWith(Application.dataPath))
                {
                    scanFolder = "Assets" + selected.Substring(Application.dataPath.Length);
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        includeSubfolders = EditorGUILayout.Toggle("包含子文件夹", includeSubfolders);
        usePathAsName = EditorGUILayout.Toggle("使用路径作为名称", usePathAsName);

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🔍 扫描并添加新音效", GUILayout.Height(30)))
        {
            ScanAndAddNewSFX(library);
        }

        if (GUILayout.Button("🔄 清空并重新扫描", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("确认", "这将清空现有列表并重新扫描，确定吗？", "确定", "取消"))
            {
                library.sfxEntries.Clear();
                ScanAndAddNewSFX(library);
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("✓ 验证所有条目"))
        {
            ValidateEntries(library);
        }

        if (GUILayout.Button("📋 打印所有查找键"))
        {
            library.ClearCache();
            library.BuildCache();
            library.DebugPrintAllKeys();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox($"当前共有 {library.sfxEntries.Count} 个音效条目", MessageType.Info);
    }

    private void ScanAndAddNewSFX(SFXLibrary library)
    {
        if (!Directory.Exists(scanFolder))
        {
            EditorUtility.DisplayDialog("错误", $"文件夹不存在: {scanFolder}", "确定");
            return;
        }

        HashSet<string> existingNames = new HashSet<string>();
        foreach (var entry in library.sfxEntries)
        {
            if (!string.IsNullOrEmpty(entry.name))
            {
                existingNames.Add(entry.name.ToLower());
            }
        }

        SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] audioExtensions = { "*.wav", "*.mp3", "*.ogg", "*.aiff", "*.aif" };

        List<string> audioFiles = new List<string>();
        foreach (string ext in audioExtensions)
        {
            audioFiles.AddRange(Directory.GetFiles(scanFolder, ext, searchOption));
        }

        int addedCount = 0;
        int skippedCount = 0;

        foreach (string filePath in audioFiles)
        {
            string assetPath = filePath.Replace("\\", "/");

            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null)
            {
                Debug.LogWarning($"[SFXLibraryEditor] 无法加载: {assetPath}");
                continue;
            }

            string entryName;
            if (usePathAsName)
            {
                entryName = assetPath;
                if (entryName.StartsWith("Assets/"))
                {
                    entryName = entryName.Substring(7);
                }
                int dotIndex = entryName.LastIndexOf('.');
                if (dotIndex > 0)
                {
                    entryName = entryName.Substring(0, dotIndex);
                }
            }
            else
            {
                entryName = Path.GetFileNameWithoutExtension(filePath);
            }

            if (existingNames.Contains(entryName.ToLower()))
            {
                skippedCount++;
                continue;
            }

            // 创建新条目
            SFXEntry newEntry = new SFXEntry();
            newEntry.name = entryName;
            newEntry.clip = clip;
            newEntry.volume = 1f;
            newEntry.note = $"自动导入";
            library.sfxEntries.Add(newEntry);

            existingNames.Add(entryName.ToLower());
            addedCount++;
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();

        // 重建缓存
        library.ClearCache();
        library.BuildCache();

        EditorUtility.DisplayDialog("扫描完成",
            $"扫描完成！\n\n" +
            $"• 新增: {addedCount} 个音效\n" +
            $"• 跳过（已存在）: {skippedCount} 个\n" +
            $"• 总计: {library.sfxEntries.Count} 个条目",
            "确定");

        Debug.Log($"[SFXLibraryEditor] 扫描完成 - 新增: {addedCount}, 跳过: {skippedCount}");
    }

    private void ValidateEntries(SFXLibrary library)
    {
        int errorCount = 0;
        int warningCount = 0;

        for (int i = 0; i < library.sfxEntries.Count; i++)
        {
            var entry = library.sfxEntries[i];

            if (string.IsNullOrEmpty(entry.name))
            {
                Debug.LogWarning($"[SFXLibrary] 条目 {i}: 名称为空");
                warningCount++;
            }

            if (entry.clip == null)
            {
                Debug.LogError($"[SFXLibrary] 条目 {i} ({entry.name}): AudioClip 丢失！");
                errorCount++;
            }
        }

        HashSet<string> names = new HashSet<string>();
        foreach (var entry in library.sfxEntries)
        {
            if (!string.IsNullOrEmpty(entry.name))
            {
                string key = entry.name.ToLower();
                if (names.Contains(key))
                {
                    Debug.LogWarning($"[SFXLibrary] 发现重复名称: {entry.name}");
                    warningCount++;
                }
                else
                {
                    names.Add(key);
                }
            }
        }

        EditorUtility.DisplayDialog("验证结果",
            $"验证完成！\n\n" +
            $"• 总条目: {library.sfxEntries.Count}\n" +
            $"• 错误: {errorCount}\n" +
            $"• 警告: {warningCount}",
            "确定");
    }
}