// Assets/Scripts/Editor/TextPromptUICreator.cs
// 编辑器工具 - 一键创建提示文字 UI
// 放在 Editor 文件夹中
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// 提示文字 UI 创建工具
/// 在 Unity 编辑器顶部菜单：Tools > 提示文字系统 > 创建提示文字 UI
/// </summary>
public class TextPromptUICreator
{
    [MenuItem("Tools/提示文字系统/创建提示文字 UI", false, 100)]
    public static void CreateTextPromptUI()
    {
        // 查找或创建 Canvas
        Canvas canvas = FindOrCreateCanvas();
        if (canvas == null)
        {
            Debug.LogError("[TextPromptUICreator] 无法创建 Canvas！");
            return;
        }

        // 检查是否已存在 TextPromptPanel
        Transform existingPanel = canvas.transform.Find("TextPromptPanel");
        if (existingPanel != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "提示文字 UI 已存在",
                "场景中已存在 TextPromptPanel，是否替换？",
                "替换", "取消"
            );

            if (!replace) return;

            Undo.DestroyObjectImmediate(existingPanel.gameObject);
        }

        // 创建主面板
        GameObject panelObj = CreatePanel(canvas.transform);

        // 创建背景
        GameObject bgObj = CreateBackground(panelObj.transform);

        // 创建文字
        GameObject textObj = CreatePromptText(panelObj.transform);

        // 创建继续指示器
        GameObject indicatorObj = CreateContinueIndicator(panelObj.transform);

        // 创建页码显示
        GameObject pageObj = CreatePageIndicator(panelObj.transform);

        // 添加 TextPromptManager 组件
        TextPromptManager manager = panelObj.AddComponent<TextPromptManager>();

        // 配置引用（通过 SerializedObject）
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("promptPanel").objectReferenceValue = panelObj;
        so.FindProperty("promptText").objectReferenceValue = textObj.GetComponent<TextMeshProUGUI>();
        so.FindProperty("backgroundImage").objectReferenceValue = bgObj.GetComponent<Image>();
        so.FindProperty("continueIndicator").objectReferenceValue = indicatorObj;
        so.FindProperty("pageIndicatorText").objectReferenceValue = pageObj.GetComponent<TextMeshProUGUI>();
        so.ApplyModifiedProperties();

        // 注册 Undo
        Undo.RegisterCreatedObjectUndo(panelObj, "Create Text Prompt UI");

        // 选中创建的对象
        Selection.activeGameObject = panelObj;

        Debug.Log("[TextPromptUICreator] ✅ 提示文字 UI 创建成功！");
        Debug.Log("  - 位置：Canvas/TextPromptPanel");
        Debug.Log("  - 组件：TextPromptManager 已自动配置");
    }

    [MenuItem("Tools/提示文字系统/添加触发器到选中物体", false, 101)]
    public static void AddTriggerToSelected()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选中一个 GameObject", "确定");
            return;
        }

        // 检查是否已有触发器
        if (selected.GetComponent<TextPromptTrigger>() != null)
        {
            EditorUtility.DisplayDialog("提示", "该物体已有 TextPromptTrigger 组件", "确定");
            return;
        }

        // 添加 Collider2D（如果没有）
        if (selected.GetComponent<Collider2D>() == null)
        {
            bool addCollider = EditorUtility.DisplayDialog(
                "添加碰撞体",
                "该物体没有 Collider2D，是否添加 BoxCollider2D？",
                "添加", "取消"
            );

            if (addCollider)
            {
                Undo.AddComponent<BoxCollider2D>(selected);
            }
            else
            {
                return;
            }
        }

        // 添加触发器
        TextPromptTrigger trigger = Undo.AddComponent<TextPromptTrigger>(selected);

        // 设置默认值
        SerializedObject so = new SerializedObject(trigger);
        SerializedProperty messagesProp = so.FindProperty("messages");
        messagesProp.arraySize = 1;
        messagesProp.GetArrayElementAtIndex(0).stringValue = "点击这里显示的提示文字...";
        so.ApplyModifiedProperties();

        Debug.Log($"[TextPromptUICreator] ✅ 已为 {selected.name} 添加 TextPromptTrigger");
    }

    [MenuItem("Tools/提示文字系统/快速配置 InteractableObject", false, 102)]
    public static void SetupInteractableForPrompt()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog("提示", "请先选中一个 GameObject", "确定");
            return;
        }

        // 获取或添加 InteractableObject
        InteractableObject interactable = selected.GetComponent<InteractableObject>();
        if (interactable == null)
        {
            interactable = Undo.AddComponent<InteractableObject>(selected);
        }

        // 获取或添加 TextPromptInteraction
        TextPromptInteraction promptInteraction = selected.GetComponent<TextPromptInteraction>();
        if (promptInteraction == null)
        {
            promptInteraction = Undo.AddComponent<TextPromptInteraction>(selected);
        }

        // 配置 InteractableObject
        SerializedObject soInteractable = new SerializedObject(interactable);
        soInteractable.FindProperty("interactionType").enumValueIndex = (int)InteractableObject.InteractionType.Trigger;

        // 绑定事件（需要手动在 Inspector 中完成）
        soInteractable.ApplyModifiedProperties();

        Debug.Log($"[TextPromptUICreator] ✅ {selected.name} 已配置为 Trigger 类型");
        Debug.Log("  ⚠️ 请在 Inspector 中手动将 InteractableObject 的 OnTrigger 事件绑定到 TextPromptInteraction.ShowPrompt()");

        Selection.activeGameObject = selected;
    }

    // ============ 创建 UI 元素 ============

    private static Canvas FindOrCreateCanvas()
    {
        // 尝试找到现有的 UICanvas
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.name.Contains("UICanvas") || c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }

        // 创建新 Canvas
        GameObject canvasObj = new GameObject("UICanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");

        return canvas;
    }

    private static GameObject CreatePanel(Transform parent)
    {
        GameObject panel = new GameObject("TextPromptPanel");
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();

        // 锚点：顶部中央
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);

        // 位置和大小
        rect.anchoredPosition = new Vector2(0, -50);
        rect.sizeDelta = new Vector2(800, 120);

        // 添加 CanvasGroup 用于透明度控制
        CanvasGroup cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 0; // 初始隐藏

        return panel;
    }

    private static GameObject CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);

        RectTransform rect = bg.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image img = bg.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.8f);

        // 圆角效果（需要圆角 Sprite，这里使用默认）
        img.type = Image.Type.Sliced;

        return bg;
    }

    private static GameObject CreatePromptText(Transform parent)
    {
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(30, 20);
        rect.offsetMax = new Vector2(-50, -20); // 右边留空给指示器

        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 28;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;

        return textObj;
    }

    private static GameObject CreateContinueIndicator(Transform parent)
    {
        GameObject indicator = new GameObject("ContinueIndicator");
        indicator.transform.SetParent(parent, false);

        RectTransform rect = indicator.AddComponent<RectTransform>();

        // 右下角
        rect.anchorMin = new Vector2(1, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(1, 0);
        rect.anchoredPosition = new Vector2(-15, 15);
        rect.sizeDelta = new Vector2(20, 20);

        Image img = indicator.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.7f);

        // 使用内置的三角形或创建简单形状
        // 这里暂时用颜色块表示，实际项目中可替换为箭头图片

        indicator.SetActive(false); // 初始隐藏

        return indicator;
    }

    private static GameObject CreatePageIndicator(Transform parent)
    {
        GameObject pageObj = new GameObject("PageIndicator");
        pageObj.transform.SetParent(parent, false);

        RectTransform rect = pageObj.AddComponent<RectTransform>();

        // 右上角
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-15, -10);
        rect.sizeDelta = new Vector2(60, 25);

        TextMeshProUGUI tmp = pageObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "1/1";
        tmp.fontSize = 16;
        tmp.color = new Color(1, 1, 1, 0.5f);
        tmp.alignment = TextAlignmentOptions.Right;

        pageObj.SetActive(false); // 单条消息时隐藏

        return pageObj;
    }
}
#endif
