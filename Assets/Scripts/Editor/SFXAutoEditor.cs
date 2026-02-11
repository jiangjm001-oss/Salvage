// Assets/Scripts/Editor/SFXAutoEditor.cs
// 通用 SFX Editor 基类
// 用于其他包含 SFX 字段的类
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// 通用 SFX Editor 基类
/// 自动识别并渲染 SFX 相关的 string 字段为 AudioClip 拖拽框
/// </summary>
public abstract class GenericSFXEditor : Editor
{
    private HashSet<string> sfxFieldNames;

    protected virtual void OnEnable()
    {
        sfxFieldNames = new HashSet<string>();

        if (target == null) return;

        // 扫描所有符合命名规则的 string 字段
        System.Type type = target.GetType();
        while (type != null && type != typeof(MonoBehaviour))
        {
            FieldInfo[] fields = type.GetFields(
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly
            );

            foreach (var field in fields)
            {
                if (field.FieldType == typeof(string) && SFXFieldAutoDrawer.IsSFXField(field.Name))
                {
                    sfxFieldNames.Add(field.Name);
                }
            }

            type = type.BaseType;
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 绘制脚本引用（只读）
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
        }

        // 遍历所有属性
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.propertyPath == "m_Script") continue;

            // 检查是否是 SFX 字段
            if (sfxFieldNames != null && sfxFieldNames.Contains(iterator.name) &&
                iterator.propertyType == SerializedPropertyType.String)
            {
                DrawSFXField(iterator);
            }
            else
            {
                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 绘制 SFX 字段
    /// </summary>
    protected void DrawSFXField(SerializedProperty property)
    {
        string currentValue = property.stringValue;
        AudioClip currentClip = SFXFieldAutoDrawer.FindClip(currentValue);
        bool hasValue = !string.IsNullOrEmpty(currentValue);
        bool clipFound = currentClip != null;

        // 背景色反馈
        Color originalBg = GUI.backgroundColor;
        if (hasValue && !clipFound)
        {
            GUI.backgroundColor = new Color(1f, 0.8f, 0.6f);
        }
        else if (clipFound)
        {
            GUI.backgroundColor = new Color(0.75f, 1f, 0.75f);
        }

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();
        AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField(
            new GUIContent(property.displayName, property.tooltip),
            currentClip,
            typeof(AudioClip),
            false
        );

        GUI.backgroundColor = originalBg;

        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = newClip != null ? SFXFieldAutoDrawer.GetResourcesPath(newClip) : "";
        }

        // 试听按钮
        if (clipFound)
        {
            if (GUILayout.Button(new GUIContent("▶", "试听"), GUILayout.Width(25), GUILayout.Height(18)))
            {
                PlayClipPreview(currentClip);
            }
        }

        // 刷新按钮
        if (GUILayout.Button(new GUIContent("↻", "刷新"), GUILayout.Width(25), GUILayout.Height(18)))
        {
            SFXFieldAutoDrawer.RefreshCache();
        }

        EditorGUILayout.EndHorizontal();

        if (hasValue && !clipFound)
        {
            EditorGUILayout.HelpBox($"找不到: {currentValue}", MessageType.Warning);
        }
    }

    /// <summary>
    /// 预览播放音效
    /// </summary>
    protected void PlayClipPreview(AudioClip clip)
    {
        if (clip == null) return;

        System.Type audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtilType != null)
        {
            MethodInfo playMethod = audioUtilType.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );

            if (playMethod != null)
            {
                playMethod.Invoke(null, new object[] { clip, 0, false });
                return;
            }

            playMethod = audioUtilType.GetMethod(
                "PlayClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new System.Type[] { typeof(AudioClip) },
                null
            );

            if (playMethod != null)
            {
                playMethod.Invoke(null, new object[] { clip });
                return;
            }
        }

        Debug.Log($"[SFX Preview] {clip.name} ({clip.length:F2}s)");
    }
}