// Assets/Editor/CutsceneSetupWizard.cs
// 过场动画快速设置向导
// 编辑器工具，一键创建过场动画所需的UI结构
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 过场动画设置向导
/// 自动创建所需的UI结构和组件
/// </summary>
public class CutsceneSetupWizard : EditorWindow
{
    // 设置参数
    private Sprite backgroundSprite;
    private Sprite letterSprite;
    private Sprite shadowSprite;
    private int canvasSortOrder = 100;
    private Vector2 referenceResolution = new Vector2(1920, 1080);

    [MenuItem("Tools/Blank Salvager/创建过场动画 UI")]
    public static void ShowWindow()
    {
        var window = GetWindow<CutsceneSetupWizard>("过场动画设置");
        window.minSize = new Vector2(400, 500);
    }

    private void OnGUI()
    {
        GUILayout.Label("LV1 信纸完成过场动画设置", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "此工具将自动创建过场动画所需的UI结构：\n" +
            "• CutsceneCanvas (渐黑遮罩 + 内容容器)\n" +
            "• 背景图片、信纸图片、黑影图片\n" +
            "• 点击提示文字\n" +
            "• LetterCompleteCutsceneController",
            MessageType.Info);

        EditorGUILayout.Space(10);

        // 图片资源
        GUILayout.Label("图片资源", EditorStyles.boldLabel);
        backgroundSprite = (Sprite)EditorGUILayout.ObjectField("背景图片", backgroundSprite, typeof(Sprite), false);
        letterSprite = (Sprite)EditorGUILayout.ObjectField("信纸图片", letterSprite, typeof(Sprite), false);
        shadowSprite = (Sprite)EditorGUILayout.ObjectField("黑影图片", shadowSprite, typeof(Sprite), false);

        EditorGUILayout.Space(10);

        // Canvas设置
        GUILayout.Label("Canvas 设置", EditorStyles.boldLabel);
        canvasSortOrder = EditorGUILayout.IntField("Sort Order", canvasSortOrder);
        referenceResolution = EditorGUILayout.Vector2Field("参考分辨率", referenceResolution);

        EditorGUILayout.Space(20);

        // 创建按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("创建过场动画 UI", GUILayout.Height(40)))
        {
            CreateCutsceneUI();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(10);

        // 单独创建按钮
        GUILayout.Label("或单独创建组件", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("仅创建 Canvas"))
        {
            CreateCutsceneCanvas();
        }
        if (GUILayout.Button("仅创建控制器"))
        {
            CreateController();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(20);

        // 帮助信息
        EditorGUILayout.HelpBox(
            "创建后需要手动操作：\n" +
            "1. 将控制器的引用拖拽关联到 UI 组件\n" +
            "2. 调整位置和大小参数\n" +
            "3. 配置音效名称",
            MessageType.Warning);
    }

    /// <summary>
    /// 创建完整的过场动画UI
    /// </summary>
    private void CreateCutsceneUI()
    {
        // 1. 创建 Canvas
        GameObject canvasObj = CreateCutsceneCanvas();
        Canvas canvas = canvasObj.GetComponent<Canvas>();

        // 2. 创建渐黑面板
        GameObject fadePanel = CreateFadePanel(canvasObj.transform);

        // 3. 创建内容容器
        GameObject container = CreateContainer(canvasObj.transform);

        // 4. 创建背景
        GameObject bgObj = CreateImage(container.transform, "BackgroundImage", backgroundSprite);
        SetStretchAnchors(bgObj.GetComponent<RectTransform>());
        bgObj.GetComponent<Image>().preserveAspect = true;

        // 5. 创建信纸
        GameObject letterObj = CreateImage(container.transform, "LetterImage", letterSprite);
        RectTransform letterRect = letterObj.GetComponent<RectTransform>();
        letterRect.anchoredPosition = new Vector2(0, 500);
        letterRect.sizeDelta = new Vector2(400, 500);
        letterObj.GetComponent<Image>().preserveAspect = true;
        letterObj.SetActive(false);

        // 6. 创建黑影
        GameObject shadowObj = CreateImage(container.transform, "ShadowImage", shadowSprite);
        RectTransform shadowRect = shadowObj.GetComponent<RectTransform>();
        shadowRect.sizeDelta = new Vector2(800, 600);
        shadowObj.GetComponent<Image>().preserveAspect = true;
        shadowObj.SetActive(false);

        // 添加黑影动画效果组件
        ShadowAnimationEffect shadowEffect = shadowObj.AddComponent<ShadowAnimationEffect>();
        shadowEffect.enabledEffects = ShadowAnimationEffect.EffectType.Pulse |
                                       ShadowAnimationEffect.EffectType.Breathe |
                                       ShadowAnimationEffect.EffectType.Shake;

        // 7. 创建点击提示
        GameObject hintObj = CreateClickHint(container.transform);
        hintObj.SetActive(false);

        // 8. 隐藏容器
        container.SetActive(false);

        // 9. 创建控制器
        GameObject controllerObj = CreateController();
        LetterCompleteCutsceneController controller = controllerObj.GetComponent<LetterCompleteCutsceneController>();

        // 10. 关联引用
        controller.fadePanel = fadePanel.GetComponent<CanvasGroup>();
        controller.cutsceneContainer = container;
        controller.backgroundImage = bgObj.GetComponent<Image>();
        controller.letterImage = letterObj.GetComponent<Image>();
        controller.shadowImage = shadowObj.GetComponent<Image>();
        controller.clickHintText = hintObj.GetComponent<TextMeshProUGUI>();

        // 选中控制器
        Selection.activeGameObject = controllerObj;

        Debug.Log("[CutsceneWizard] 过场动画UI创建完成！");
        EditorUtility.DisplayDialog("完成", "过场动画UI已创建完成！\n请检查并调整参数。", "确定");
    }

    /// <summary>
    /// 创建 Canvas
    /// </summary>
    private GameObject CreateCutsceneCanvas()
    {
        // 创建Canvas
        GameObject canvasObj = new GameObject("CutsceneCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortOrder;

        // Canvas Scaler
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.matchWidthOrHeight = 0.5f;

        // Graphic Raycaster
        canvasObj.AddComponent<GraphicRaycaster>();

        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Cutscene Canvas");
        return canvasObj;
    }

    /// <summary>
    /// 创建渐黑面板
    /// </summary>
    private GameObject CreateFadePanel(Transform parent)
    {
        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(parent, false);

        // RectTransform - 全屏
        RectTransform rect = panelObj.AddComponent<RectTransform>();
        SetStretchAnchors(rect);

        // Image - 黑色
        Image image = panelObj.AddComponent<Image>();
        image.color = Color.black;

        // Canvas Group
        CanvasGroup canvasGroup = panelObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        return panelObj;
    }

    /// <summary>
    /// 创建内容容器
    /// </summary>
    private GameObject CreateContainer(Transform parent)
    {
        GameObject containerObj = new GameObject("CutsceneContainer");
        containerObj.transform.SetParent(parent, false);

        RectTransform rect = containerObj.AddComponent<RectTransform>();
        SetStretchAnchors(rect);

        return containerObj;
    }

    /// <summary>
    /// 创建Image
    /// </summary>
    private GameObject CreateImage(Transform parent, string name, Sprite sprite)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400, 400);

        Image image = obj.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;

        return obj;
    }

    /// <summary>
    /// 创建点击提示文字
    /// </summary>
    private GameObject CreateClickHint(Transform parent)
    {
        GameObject obj = new GameObject("ClickHintText");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0, 100);
        rect.sizeDelta = new Vector2(400, 60);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = "点击继续";
        text.fontSize = 36;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        return obj;
    }

    /// <summary>
    /// 创建控制器
    /// </summary>
    private GameObject CreateController()
    {
        // 检查是否已存在
        LetterCompleteCutsceneController existing = FindObjectOfType<LetterCompleteCutsceneController>();
        if (existing != null)
        {
            Debug.LogWarning("[CutsceneWizard] 控制器已存在，返回现有控制器");
            return existing.gameObject;
        }

        GameObject controllerObj = new GameObject("LetterCutsceneController");
        controllerObj.AddComponent<LetterCompleteCutsceneController>();

        Undo.RegisterCreatedObjectUndo(controllerObj, "Create Cutscene Controller");
        return controllerObj;
    }

    /// <summary>
    /// 设置为拉伸锚点
    /// </summary>
    private void SetStretchAnchors(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
#endif