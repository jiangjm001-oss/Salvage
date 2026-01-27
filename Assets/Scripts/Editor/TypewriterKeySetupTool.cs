// Assets/Scripts/Editor/TypewriterKeySetupTool.cs
// 打字机按键批量配置工具
// 放在 Editor 文件夹中
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 打字机按键批量配置工具
/// 用于快速配置所有按键的 Controller 和 ZoomViewRoot 引用
/// </summary>
public class TypewriterKeySetupTool : EditorWindow
{
    private TypewriterController controller;
    private GameObject zoomViewRoot;
    private Transform keysParent;

    [MenuItem("Tools/Typewriter/批量配置按键")]
    public static void ShowWindow()
    {
        GetWindow<TypewriterKeySetupTool>("打字机按键配置");
    }

    private void OnGUI()
    {
        GUILayout.Label("打字机按键批量配置工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "此工具用于批量配置所有 TypewriterKey 组件的引用。\n" +
            "1. 拖入 TypewriterController\n" +
            "2. 拖入放大视图根物体\n" +
            "3. 拖入按键父物体（Keys）\n" +
            "4. 点击\"应用配置\"",
            MessageType.Info);

        EditorGUILayout.Space();

        controller = (TypewriterController)EditorGUILayout.ObjectField(
            "TypewriterController",
            controller,
            typeof(TypewriterController),
            true);

        zoomViewRoot = (GameObject)EditorGUILayout.ObjectField(
            "放大视图根物体",
            zoomViewRoot,
            typeof(GameObject),
            true);

        keysParent = (Transform)EditorGUILayout.ObjectField(
            "按键父物体 (Keys)",
            keysParent,
            typeof(Transform),
            true);

        EditorGUILayout.Space();

        // 应用按钮
        EditorGUI.BeginDisabledGroup(controller == null || keysParent == null);
        if (GUILayout.Button("应用配置到所有按键", GUILayout.Height(30)))
        {
            ApplyToAllKeys();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        // 快速查找按钮
        if (GUILayout.Button("自动查找场景中的组件"))
        {
            AutoFind();
        }

        EditorGUILayout.Space();

        // 显示统计
        if (keysParent != null)
        {
            int keyCount = keysParent.GetComponentsInChildren<TypewriterKey>(true).Length;
            EditorGUILayout.LabelField($"找到 {keyCount} 个按键组件");
        }
    }

    /// <summary>
    /// 应用配置到所有按键
    /// </summary>
    private void ApplyToAllKeys()
    {
        if (keysParent == null)
        {
            Debug.LogError("[TypewriterKeySetupTool] 请先指定按键父物体！");
            return;
        }

        TypewriterKey[] keys = keysParent.GetComponentsInChildren<TypewriterKey>(true);

        if (keys.Length == 0)
        {
            Debug.LogWarning("[TypewriterKeySetupTool] 没有找到任何 TypewriterKey 组件！");
            return;
        }

        int modifiedCount = 0;

        foreach (TypewriterKey key in keys)
        {
            bool modified = false;

            // 设置 Controller
            if (controller != null && key.controller != controller)
            {
                Undo.RecordObject(key, "Set TypewriterKey Controller");
                key.controller = controller;
                modified = true;
            }

            // 设置 ZoomViewRoot
            if (zoomViewRoot != null && key.zoomViewRoot != zoomViewRoot)
            {
                Undo.RecordObject(key, "Set TypewriterKey ZoomViewRoot");
                key.zoomViewRoot = zoomViewRoot;
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(key);
                modifiedCount++;
            }
        }

        Debug.Log($"[TypewriterKeySetupTool] ✓ 已配置 {modifiedCount}/{keys.Length} 个按键");

        // 如果是 Prefab，标记为已修改
        if (keysParent != null)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(keysParent);
        }
    }

    /// <summary>
    /// 自动查找场景中的组件
    /// </summary>
    private void AutoFind()
    {
        // 查找 TypewriterController
        if (controller == null)
        {
            controller = FindObjectOfType<TypewriterController>();
            if (controller != null)
            {
                Debug.Log($"[TypewriterKeySetupTool] 找到 TypewriterController: {controller.name}");
            }
        }

        // 查找放大视图（名称包含 zoom 和 typewriter）
        if (zoomViewRoot == null)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                string nameLower = obj.name.ToLower();
                if (nameLower.Contains("zoom") && nameLower.Contains("typewriter"))
                {
                    zoomViewRoot = obj;
                    Debug.Log($"[TypewriterKeySetupTool] 找到放大视图: {zoomViewRoot.name}");
                    break;
                }
            }
        }

        // 查找按键父物体（名称为 Keys）
        if (keysParent == null)
        {
            GameObject keysObj = GameObject.Find("Keys");
            if (keysObj != null)
            {
                keysParent = keysObj.transform;
                Debug.Log($"[TypewriterKeySetupTool] 找到按键父物体: {keysParent.name}");
            }
        }

        Repaint();
    }
}

/// <summary>
/// TypewriterKey 的自定义 Inspector 扩展
/// 添加快速配置按钮
/// </summary>
[CustomEditor(typeof(TypewriterKey))]
public class TypewriterKeyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();

        TypewriterKey key = (TypewriterKey)target;

        // 快速配置按钮
        if (GUILayout.Button("自动查找引用"))
        {
            AutoFindReferences(key);
        }

        // 批量工具按钮
        if (GUILayout.Button("打开批量配置工具"))
        {
            TypewriterKeySetupTool.ShowWindow();
        }
    }

    private void AutoFindReferences(TypewriterKey key)
    {
        bool modified = false;

        // 查找 Controller
        if (key.controller == null)
        {
            key.controller = FindObjectOfType<TypewriterController>();
            if (key.controller != null)
            {
                Debug.Log($"[TypewriterKey] 自动找到 Controller: {key.controller.name}");
                modified = true;
            }
        }

        // 查找 ZoomViewRoot（向上查找）
        if (key.zoomViewRoot == null)
        {
            Transform parent = key.transform.parent;
            while (parent != null)
            {
                if (parent.name.ToLower().Contains("zoom"))
                {
                    key.zoomViewRoot = parent.gameObject;
                    Debug.Log($"[TypewriterKey] 自动找到 ZoomViewRoot: {key.zoomViewRoot.name}");
                    modified = true;
                    break;
                }
                parent = parent.parent;
            }
        }

        // 查找 KeyLabel（子物体）
        if (key.keyLabel == null)
        {
            key.keyLabel = key.GetComponentInChildren<TMPro.TextMeshPro>();
            if (key.keyLabel != null)
            {
                Debug.Log($"[TypewriterKey] 自动找到 KeyLabel");
                modified = true;
            }
        }

        if (modified)
        {
            EditorUtility.SetDirty(key);
        }
    }
}
#endif