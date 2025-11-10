#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>
/// Unity 编辑器工具 - 自动化设置系统的部分设置过程
/// 使用方法：菜单栏 → Tools → Setup Settings System
/// </summary>
public class SettingsSetupHelper : EditorWindow
{
    [MenuItem("Tools/Setup Settings System")]
    public static void ShowWindow()
    {
        GetWindow<SettingsSetupHelper>("Settings Setup Helper");
    }

    private void OnGUI()
    {
        GUILayout.Label("Settings System Setup Helper", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("此工具帮助你设置游戏的设置系统", EditorStyles.helpBox);
        GUILayout.Space(10);

        if (GUILayout.Button("打开设置指南文档", GUILayout.Height(40)))
        {
            string guidePath = "Assets/../SETTINGS_SETUP_GUIDE.md";
            if (System.IO.File.Exists(guidePath))
            {
                System.Diagnostics.Process.Start(guidePath);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "找不到 SETTINGS_SETUP_GUIDE.md 文件", "确定");
            }
        }

        GUILayout.Space(10);
        GUILayout.Label("快捷操作：", EditorStyles.boldLabel);

        if (GUILayout.Button("1. 选中 _Managers_Prefab", GUILayout.Height(30)))
        {
            string prefabPath = "Assets/Prefabs/_Managers_Prefab.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "找不到 _Managers_Prefab.prefab", "确定");
            }
        }

        if (GUILayout.Button("2. 打开 LandingPage 场景", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("打开场景", "是否保存当前场景并打开 LandingPage?", "是", "否"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/LandingPage.unity");
            }
        }

        if (GUILayout.Button("3. 打开 Level1_Room 场景", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("打开场景", "是否保存当前场景并打开 Level1_Room?", "是", "否"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level1_Room.unity");
            }
        }

        if (GUILayout.Button("4. 打开 Level2_Room 场景", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("打开场景", "是否保存当前场景并打开 Level2_Room?", "是", "否"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Level2_Room.unity");
            }
        }

        GUILayout.Space(20);
        GUILayout.Label("验证检查：", EditorStyles.boldLabel);

        if (GUILayout.Button("检查 SettingsManager 脚本", GUILayout.Height(30)))
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/Managers/SettingsManager.cs");
            if (script != null)
            {
                EditorUtility.DisplayDialog("成功", "SettingsManager.cs 脚本存在", "确定");
                Selection.activeObject = script;
                EditorGUIUtility.PingObject(script);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "找不到 SettingsManager.cs 脚本", "确定");
            }
        }

        if (GUILayout.Button("检查 SettingsButton 脚本", GUILayout.Height(30)))
        {
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>("Assets/Scripts/UI/SettingsButton.cs");
            if (script != null)
            {
                EditorUtility.DisplayDialog("成功", "SettingsButton.cs 脚本存在", "确定");
                Selection.activeObject = script;
                EditorGUIUtility.PingObject(script);
            }
            else
            {
                EditorUtility.DisplayDialog("错误", "找不到 SettingsButton.cs 脚本", "确定");
            }
        }

        GUILayout.Space(20);
        GUILayout.Label("请按照 SETTINGS_SETUP_GUIDE.md 文档完成剩余设置步骤", EditorStyles.helpBox);
    }
}
#endif
