using TMPro;
using UnityEditor;
using UnityEngine;

//Assets / Scripts / Editor / TypewriterKeyGenerator.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class TypewriterKeyGenerator : EditorWindow
{
    private GameObject keyPrefab;
    private Transform parentTransform;
    private string characters = "QWERTYUIOPASDFGHJKLZXCVBNM";
    private int columns = 10;
    private float spacingX = 0.7f;
    private float spacingY = 0.7f;
    private Vector3 startOffset = Vector3.zero;

    [MenuItem("Tools/Blank Salvager/打字机按键生成器")]
    public static void ShowWindow()
    {
        GetWindow<TypewriterKeyGenerator>("打字机按键生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("打字机按键批量生成", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        keyPrefab = (GameObject)EditorGUILayout.ObjectField(
            "按键预制体", keyPrefab, typeof(GameObject), false);

        parentTransform = (Transform)EditorGUILayout.ObjectField(
            "父物体 (Keys)", parentTransform, typeof(Transform), true);

        EditorGUILayout.Space(5);

        characters = EditorGUILayout.TextField("字符列表", characters);
        columns = EditorGUILayout.IntSlider("每行按键数", columns, 5, 15);
        spacingX = EditorGUILayout.FloatField("水平间距", spacingX);
        spacingY = EditorGUILayout.FloatField("垂直间距", spacingY);
        startOffset = EditorGUILayout.Vector3Field("起始偏移", startOffset);

        EditorGUILayout.Space(10);

        // 预览信息
        int rows = Mathf.CeilToInt((float)characters.Length / columns);
        EditorGUILayout.HelpBox(
            $"将生成 {characters.Length} 个按键\n" +
            $"排列: {columns} 列 × {rows} 行",
            MessageType.Info);

        EditorGUILayout.Space(5);

        GUI.enabled = (keyPrefab != null && parentTransform != null);

        if (GUILayout.Button("生成按键", GUILayout.Height(30)))
        {
            GenerateKeys();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(5);

        if (GUILayout.Button("清空父物体下所有子物体"))
        {
            ClearChildren();
        }
    }

    private void GenerateKeys()
    {
        if (keyPrefab == null || parentTransform == null)
        {
            EditorUtility.DisplayDialog("错误", "请先指定预制体和父物体！", "确定");
            return;
        }

        Undo.RecordObject(parentTransform, "Generate Typewriter Keys");

        for (int i = 0; i < characters.Length; i++)
        {
            int row = i / columns;
            int col = i % columns;

            // 计算位置（从左上角开始，向右下排列）
            Vector3 localPos = new Vector3(
                col * spacingX + startOffset.x,
                -row * spacingY + startOffset.y,
                startOffset.z
            );

            // 实例化预制体
            GameObject keyObj = (GameObject)PrefabUtility.InstantiatePrefab(keyPrefab, parentTransform);
            keyObj.transform.localPosition = localPos;
            keyObj.name = $"Key_{characters[i]}";

            // 设置 TypewriterKey 组件
            TypewriterKey key = keyObj.GetComponent<TypewriterKey>();
            if (key != null)
            {
                key.keyCharacter = characters[i];

                // 自动设置文字标签
                TextMeshPro label = keyObj.GetComponentInChildren<TextMeshPro>();
                if (label != null)
                {
                    label.text = characters[i].ToString();
                }
            }

            Undo.RegisterCreatedObjectUndo(keyObj, "Create Key");
        }

        Debug.Log($"[TypewriterKeyGenerator] 已生成 {characters.Length} 个按键");
        EditorUtility.DisplayDialog("完成", $"已生成 {characters.Length} 个按键！", "确定");
    }

    private void ClearChildren()
    {
        if (parentTransform == null) return;

        if (EditorUtility.DisplayDialog("确认",
            $"确定要删除 {parentTransform.name} 下的所有子物体吗？",
            "删除", "取消"))
        {
            while (parentTransform.childCount > 0)
            {
                Undo.DestroyObjectImmediate(parentTransform.GetChild(0).gameObject);
            }
            Debug.Log("[TypewriterKeyGenerator] 已清空子物体");
        }
    }
}
#endif