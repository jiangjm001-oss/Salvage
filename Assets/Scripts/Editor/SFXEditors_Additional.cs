// Assets/Scripts/Editor/SFXEditors_Additional.cs
// 为项目中其他包含 SFX 字段的类创建自定义 Editor
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

// ============================================================================
// 为其他类添加 SFX 拖拽支持
// 注意：InteractableObject 使用独立的 InteractableObjectEditor.cs
// ============================================================================

/// <summary>
/// PotController 自定义 Editor
/// </summary>
[CustomEditor(typeof(PotController))]
[CanEditMultipleObjects]
public class PotControllerSFXEditor : GenericSFXEditor { }

/// <summary>
/// OrganCollectionPuzzle 自定义 Editor
/// </summary>
[CustomEditor(typeof(OrganCollectionPuzzle))]
[CanEditMultipleObjects]
public class OrganCollectionPuzzleSFXEditor : GenericSFXEditor { }

/// <summary>
/// PhotoFramePuzzle 自定义 Editor
/// </summary>
[CustomEditor(typeof(PhotoFramePuzzle))]
[CanEditMultipleObjects]
public class PhotoFramePuzzleSFXEditor : GenericSFXEditor { }

// ============================================================================
// 如果有其他类也需要 SFX 字段支持，按照以下模板添加即可：
// 
// [CustomEditor(typeof(YourClassName))]
// [CanEditMultipleObjects]
// public class YourClassNameSFXEditor : GenericSFXEditor { }
// ============================================================================

/// <summary>
/// SFX 工具窗口 - 扫描项目中的 SFX 字段
/// 优化版：缓存扫描结果，避免每帧反射导致卡顿
/// </summary>
public class SFXEditorToolWindow : EditorWindow
{
    // 缓存的扫描结果
    private class TypeInfo
    {
        public System.Type Type;
        public string Name;
        public int SFXFieldCount;
        public bool HasEditor;
    }

    private Vector2 scrollPosition;
    private List<TypeInfo> cachedTypeInfos = new List<TypeInfo>();
    private bool isScanning = false;
    private string statusMessage = "点击'扫描项目'开始";

    [MenuItem("Tools/SFX字段工具/扫描SFX字段")]
    public static void ShowWindow()
    {
        var window = GetWindow<SFXEditorToolWindow>("SFX字段扫描器");
        window.minSize = new Vector2(450, 350);
    }

    [MenuItem("Tools/SFX字段工具/刷新音效缓存")]
    public static void RefreshSFXCache()
    {
        SFXFieldAutoDrawer.RefreshCache();
        Debug.Log("[SFX工具] 音效缓存已刷新");
    }

    private void OnEnable()
    {
        // 窗口打开时不自动扫描，让用户手动触发
    }

    /// <summary>
    /// 执行扫描（只在点击按钮时调用）
    /// </summary>
    private void ScanForSFXFields()
    {
        isScanning = true;
        statusMessage = "正在扫描...";
        cachedTypeInfos.Clear();

        // 缓存已有的 CustomEditor 映射
        HashSet<System.Type> typesWithEditors = new HashSet<System.Type>();
        CacheCustomEditors(typesWithEditors);

        int totalTypesScanned = 0;

        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            // 跳过系统程序集
            string assemblyName = assembly.GetName().Name;
            if (assemblyName.StartsWith("Unity") ||
                assemblyName.StartsWith("System") ||
                assemblyName.StartsWith("mscorlib") ||
                assemblyName.StartsWith("Mono") ||
                assemblyName.StartsWith("netstandard"))
            {
                continue;
            }

            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (!type.IsSubclassOf(typeof(MonoBehaviour))) continue;
                    if (type.IsAbstract) continue;

                    totalTypesScanned++;

                    // 计算 SFX 字段数量
                    int sfxCount = CountSFXFieldsCached(type);

                    if (sfxCount > 0)
                    {
                        cachedTypeInfos.Add(new TypeInfo
                        {
                            Type = type,
                            Name = type.Name,
                            SFXFieldCount = sfxCount,
                            HasEditor = typesWithEditors.Contains(type)
                        });
                    }
                }
            }
            catch { }
        }

        // 按名称排序
        cachedTypeInfos.Sort((a, b) => a.Name.CompareTo(b.Name));

        isScanning = false;
        statusMessage = $"扫描完成：{cachedTypeInfos.Count} 个类包含 SFX 字段（共扫描 {totalTypesScanned} 个类）";
    }

    /// <summary>
    /// 缓存所有 CustomEditor 的目标类型
    /// </summary>
    private void CacheCustomEditors(HashSet<System.Type> result)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var editorType in assembly.GetTypes())
                {
                    if (!editorType.IsSubclassOf(typeof(Editor))) continue;

                    var attrs = editorType.GetCustomAttributes(typeof(CustomEditor), true);
                    foreach (CustomEditor attr in attrs)
                    {
                        var field = typeof(CustomEditor).GetField(
                            "m_InspectedType",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );

                        if (field != null)
                        {
                            System.Type inspectedType = field.GetValue(attr) as System.Type;
                            if (inspectedType != null)
                            {
                                result.Add(inspectedType);
                            }
                        }
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// 计算类型的 SFX 字段数量
    /// </summary>
    private int CountSFXFieldsCached(System.Type type)
    {
        int count = 0;
        System.Type currentType = type;

        while (currentType != null && currentType != typeof(MonoBehaviour))
        {
            try
            {
                foreach (var field in currentType.GetFields(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly))
                {
                    if (field.FieldType == typeof(string) && SFXFieldAutoDrawer.IsSFXField(field.Name))
                    {
                        count++;
                    }
                }
            }
            catch { }

            currentType = currentType.BaseType;
        }

        return count;
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // 标题
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 14;
        EditorGUILayout.LabelField("🔊 SFX 字段扫描器", titleStyle);

        EditorGUILayout.Space(5);

        // 状态信息
        EditorGUILayout.HelpBox(statusMessage, MessageType.Info);

        EditorGUILayout.Space(5);

        // 按钮区
        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(isScanning))
        {
            if (GUILayout.Button("扫描项目", GUILayout.Height(28)))
            {
                ScanForSFXFields();
            }
        }

        if (GUILayout.Button("刷新音效缓存", GUILayout.Height(28)))
        {
            SFXFieldAutoDrawer.RefreshCache();
            ShowNotification(new GUIContent("缓存已刷新"));
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 结果列表（使用缓存数据，不做任何反射）
        if (cachedTypeInfos.Count > 0)
        {
            EditorGUILayout.LabelField($"包含 SFX 字段的类 ({cachedTypeInfos.Count})", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var info in cachedTypeInfos)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // 类名
                EditorGUILayout.LabelField(info.Name, EditorStyles.boldLabel, GUILayout.Width(200));

                // SFX 字段数量
                EditorGUILayout.LabelField($"{info.SFXFieldCount} 个字段", GUILayout.Width(80));

                // 是否已有 Editor
                if (info.HasEditor)
                {
                    GUIStyle greenStyle = new GUIStyle(EditorStyles.label);
                    greenStyle.normal.textColor = new Color(0.2f, 0.8f, 0.2f);
                    EditorGUILayout.LabelField("✓ 已配置", greenStyle, GUILayout.Width(70));
                }
                else
                {
                    GUIStyle orangeStyle = new GUIStyle(EditorStyles.label);
                    orangeStyle.normal.textColor = new Color(1f, 0.6f, 0.2f);
                    EditorGUILayout.LabelField("○ 未配置", orangeStyle, GUILayout.Width(70));
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(10);

        // 帮助信息
        EditorGUILayout.HelpBox(
            "为新类添加 SFX 拖拽支持：\n\n" +
            "[CustomEditor(typeof(类名))]\n" +
            "public class 类名SFXEditor : GenericSFXEditor { }\n\n" +
            "添加到 SFXEditors_Additional.cs 文件中即可",
            MessageType.None
        );
    }
}