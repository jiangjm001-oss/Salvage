// Assets/Scripts/Editor/AutoBootstrapLoader.cs
// ⚠️ 此脚本必须放在 Editor 文件夹内
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 自动从 Bootstrap 场景启动游戏
/// 无论当前打开哪个场景，点击 Play 都会先切换到 Bootstrap
/// </summary>
[InitializeOnLoad]
public static class AutoBootstrapLoader
{
    // 配置：Bootstrap 场景路径
    private const string BOOTSTRAP_SCENE_PATH = "Assets/Scenes/Bootstrap.unity";

    // 是否启用自动加载（可以在菜单中切换）
    private const string ENABLED_KEY = "AutoBootstrapLoader_Enabled";

    // 记录播放前的场景（用于停止后恢复）
    private const string PREVIOUS_SCENE_KEY = "AutoBootstrapLoader_PreviousScene";

    static AutoBootstrapLoader()
    {
        // 订阅播放模式状态变化事件
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    /// <summary>
    /// 播放模式状态变化回调
    /// </summary>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // 检查是否启用
        if (!IsEnabled)
            return;

        switch (state)
        {
            case PlayModeStateChange.ExitingEditMode:
                // 即将进入播放模式 - 在这里切换场景
                OnExitingEditMode();
                break;

            case PlayModeStateChange.EnteredEditMode:
                // 刚退出播放模式 - 可选：恢复原场景
                OnEnteredEditMode();
                break;
        }
    }

    /// <summary>
    /// 即将进入播放模式
    /// </summary>
    private static void OnExitingEditMode()
    {
        // 获取当前场景
        var currentScene = EditorSceneManager.GetActiveScene();
        string currentScenePath = currentScene.path;

        // 如果已经在 Bootstrap 场景，不需要切换
        if (currentScenePath == BOOTSTRAP_SCENE_PATH)
        {
            Debug.Log("[AutoBootstrapLoader] 已在 Bootstrap 场景，直接运行");
            return;
        }

        // 检查 Bootstrap 场景是否存在
        if (!System.IO.File.Exists(BOOTSTRAP_SCENE_PATH))
        {
            Debug.LogError($"[AutoBootstrapLoader] Bootstrap 场景不存在: {BOOTSTRAP_SCENE_PATH}");
            return;
        }

        // 保存当前场景路径（用于之后恢复）
        EditorPrefs.SetString(PREVIOUS_SCENE_KEY, currentScenePath);

        // 如果当前场景有未保存的修改，提示保存
        if (currentScene.isDirty)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                // 用户选择保存或不保存，继续
            }
            else
            {
                // 用户取消，中止播放
                EditorApplication.isPlaying = false;
                return;
            }
        }

        // 切换到 Bootstrap 场景
        Debug.Log($"[AutoBootstrapLoader] 从 {currentScene.name} 切换到 Bootstrap 场景运行");
        EditorSceneManager.OpenScene(BOOTSTRAP_SCENE_PATH);
    }

    /// <summary>
    /// 刚退出播放模式（可选：恢复原场景）
    /// </summary>
    private static void OnEnteredEditMode()
    {
        // 获取之前的场景路径
        string previousScenePath = EditorPrefs.GetString(PREVIOUS_SCENE_KEY, "");

        // 如果有记录且不是 Bootstrap，恢复原场景
        if (!string.IsNullOrEmpty(previousScenePath) &&
            previousScenePath != BOOTSTRAP_SCENE_PATH &&
            System.IO.File.Exists(previousScenePath))
        {
            Debug.Log($"[AutoBootstrapLoader] 恢复到原场景: {previousScenePath}");
            EditorSceneManager.OpenScene(previousScenePath);

            // 清除记录
            EditorPrefs.DeleteKey(PREVIOUS_SCENE_KEY);
        }
    }

    // ============ 菜单控制 ============

    /// <summary>
    /// 是否启用自动加载
    /// </summary>
    private static bool IsEnabled
    {
        get => EditorPrefs.GetBool(ENABLED_KEY, true);  // 默认启用
        set => EditorPrefs.SetBool(ENABLED_KEY, value);
    }

    /// <summary>
    /// 菜单项：切换启用状态
    /// </summary>
    [MenuItem("Tools/Auto Bootstrap Loader/启用自动加载 _F5", false, 100)]
    private static void ToggleEnabled()
    {
        IsEnabled = !IsEnabled;
        Debug.Log($"[AutoBootstrapLoader] 自动加载已{(IsEnabled ? "启用 ✓" : "禁用 ✗")}");
    }

    /// <summary>
    /// 菜单项验证：显示勾选状态
    /// </summary>
    [MenuItem("Tools/Auto Bootstrap Loader/启用自动加载 _F5", true)]
    private static bool ToggleEnabledValidate()
    {
        Menu.SetChecked("Tools/Auto Bootstrap Loader/启用自动加载 _F5", IsEnabled);
        return true;
    }

    /// <summary>
    /// 菜单项：直接打开 Bootstrap 场景
    /// </summary>
    [MenuItem("Tools/Auto Bootstrap Loader/打开 Bootstrap 场景", false, 200)]
    private static void OpenBootstrapScene()
    {
        if (System.IO.File.Exists(BOOTSTRAP_SCENE_PATH))
        {
            // 保存当前场景
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            EditorSceneManager.OpenScene(BOOTSTRAP_SCENE_PATH);
        }
        else
        {
            EditorUtility.DisplayDialog("错误", $"Bootstrap 场景不存在:\n{BOOTSTRAP_SCENE_PATH}", "确定");
        }
    }

    /// <summary>
    /// 菜单项：显示当前状态
    /// </summary>
    [MenuItem("Tools/Auto Bootstrap Loader/显示状态", false, 300)]
    private static void ShowStatus()
    {
        string status = IsEnabled ? "✓ 已启用" : "✗ 已禁用";
        string previousScene = EditorPrefs.GetString(PREVIOUS_SCENE_KEY, "无");

        EditorUtility.DisplayDialog(
            "Auto Bootstrap Loader 状态",
            $"状态: {status}\n" +
            $"Bootstrap 路径: {BOOTSTRAP_SCENE_PATH}\n" +
            $"记录的原场景: {previousScene}",
            "确定"
        );
    }
}