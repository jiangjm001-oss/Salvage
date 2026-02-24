// Assets/Scripts/Editor/InteractableObjectEditor.cs
// 自定义编辑器 - 根据交互类型动态显示对应设置
// 升级版 - 集成 SFX 字段拖拽功能
using UnityEngine;
using UnityEditor;
using System.Reflection;

[CustomEditor(typeof(InteractableObject))]
public class InteractableObjectEditor : Editor
{
    // ========== 基本信息 ==========
    SerializedProperty objectID;
    SerializedProperty displayName;
    SerializedProperty interactionType;

    // ========== Pickup ==========
    SerializedProperty item;
    SerializedProperty isPickupable;
    SerializedProperty pickupSoundName;

    // ========== ZoomView ==========
    SerializedProperty zoomViewTarget;
    SerializedProperty associatedZoomView;
    SerializedProperty zoomSoundName;

    // ========== Trigger ==========
    SerializedProperty disableAfterTrigger;
    SerializedProperty OnTrigger;
    SerializedProperty triggerSoundName;

    // ========== RequireItem ==========
    SerializedProperty requiredItem;
    SerializedProperty consumeItemOnUse;
    SerializedProperty noItemHint;
    SerializedProperty wrongItemHint;
    SerializedProperty OnItemUsedSuccess;
    SerializedProperty itemUsedSoundName;

    // ========== ItemCombine ==========
    SerializedProperty combineRequiredItem;
    SerializedProperty combineResultItem;
    SerializedProperty consumeCombineItem;
    SerializedProperty disableAfterCombine;
    SerializedProperty OnCombineSuccess;
    SerializedProperty combineSoundName;

    // ========== StateSwitch ==========
    SerializedProperty switchRequiredItem;
    SerializedProperty switchedSprite;
    SerializedProperty consumeSwitchItem;
    SerializedProperty hasStateSwitch;
    SerializedProperty OnStateSwitchSuccess;
    SerializedProperty stateSwitchSoundName;
    SerializedProperty waitForSoundBeforeSwitch;

    // ========== ObjectSwap ==========
    SerializedProperty swapTargetObject;
    SerializedProperty swapRequiredItem;
    SerializedProperty consumeSwapItem;
    SerializedProperty isSwapUnlocked;
    SerializedProperty OnSwapSuccess;
    SerializedProperty swapSoundName;

    // ========== Container ==========
    SerializedProperty containerClosedSprite;
    SerializedProperty containerOpenedSprite;
    SerializedProperty containedObjects;
    SerializedProperty containerRequiredItem;
    SerializedProperty consumeContainerItem;
    SerializedProperty isContainerUnlocked;
    SerializedProperty isContainerOpen;
    SerializedProperty containerOpenSound;
    SerializedProperty containerCloseSound;

    private void OnEnable()
    {
        // 基本信息
        objectID = serializedObject.FindProperty("objectID");
        displayName = serializedObject.FindProperty("displayName");
        interactionType = serializedObject.FindProperty("interactionType");

        // Pickup
        item = serializedObject.FindProperty("item");
        isPickupable = serializedObject.FindProperty("isPickupable");
        pickupSoundName = serializedObject.FindProperty("pickupSoundName");

        // ZoomView
        zoomViewTarget = serializedObject.FindProperty("zoomViewTarget");
        associatedZoomView = serializedObject.FindProperty("associatedZoomView");
        zoomSoundName = serializedObject.FindProperty("zoomSoundName");

        // Trigger
        disableAfterTrigger = serializedObject.FindProperty("disableAfterTrigger");
        OnTrigger = serializedObject.FindProperty("OnTrigger");
        triggerSoundName = serializedObject.FindProperty("triggerSoundName");

        // RequireItem
        requiredItem = serializedObject.FindProperty("requiredItem");
        consumeItemOnUse = serializedObject.FindProperty("consumeItemOnUse");
        noItemHint = serializedObject.FindProperty("noItemHint");
        wrongItemHint = serializedObject.FindProperty("wrongItemHint");
        OnItemUsedSuccess = serializedObject.FindProperty("OnItemUsedSuccess");
        itemUsedSoundName = serializedObject.FindProperty("itemUsedSoundName");

        // ItemCombine
        combineRequiredItem = serializedObject.FindProperty("combineRequiredItem");
        combineResultItem = serializedObject.FindProperty("combineResultItem");
        consumeCombineItem = serializedObject.FindProperty("consumeCombineItem");
        disableAfterCombine = serializedObject.FindProperty("disableAfterCombine");
        OnCombineSuccess = serializedObject.FindProperty("OnCombineSuccess");
        combineSoundName = serializedObject.FindProperty("combineSoundName");

        // StateSwitch
        switchRequiredItem = serializedObject.FindProperty("switchRequiredItem");
        switchedSprite = serializedObject.FindProperty("switchedSprite");
        consumeSwitchItem = serializedObject.FindProperty("consumeSwitchItem");
        hasStateSwitch = serializedObject.FindProperty("hasStateSwitch");
        OnStateSwitchSuccess = serializedObject.FindProperty("OnStateSwitchSuccess");
        stateSwitchSoundName = serializedObject.FindProperty("stateSwitchSoundName");
        waitForSoundBeforeSwitch = serializedObject.FindProperty("waitForSoundBeforeSwitch");

        // ObjectSwap
        swapTargetObject = serializedObject.FindProperty("swapTargetObject");
        swapRequiredItem = serializedObject.FindProperty("swapRequiredItem");
        consumeSwapItem = serializedObject.FindProperty("consumeSwapItem");
        isSwapUnlocked = serializedObject.FindProperty("isSwapUnlocked");
        OnSwapSuccess = serializedObject.FindProperty("OnSwapSuccess");
        swapSoundName = serializedObject.FindProperty("swapSoundName");

        // Container
        containerClosedSprite = serializedObject.FindProperty("containerClosedSprite");
        containerOpenedSprite = serializedObject.FindProperty("containerOpenedSprite");
        containedObjects = serializedObject.FindProperty("containedObjects");
        containerRequiredItem = serializedObject.FindProperty("containerRequiredItem");
        consumeContainerItem = serializedObject.FindProperty("consumeContainerItem");
        isContainerUnlocked = serializedObject.FindProperty("isContainerUnlocked");
        isContainerOpen = serializedObject.FindProperty("isContainerOpen");
        containerOpenSound = serializedObject.FindProperty("containerOpenSound");
        containerCloseSound = serializedObject.FindProperty("containerCloseSound");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ===== 基本信息 =====
        EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(objectID, new GUIContent("Object ID"));
        EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(10);

        // ===== 交互类型选择 =====
        EditorGUILayout.LabelField("交互设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(interactionType, new GUIContent("Interaction Type"));
        EditorGUI.indentLevel--;

        EditorGUILayout.Space(10);

        // ===== 根据类型显示对应设置 =====
        InteractableObject.InteractionType type =
            (InteractableObject.InteractionType)interactionType.enumValueIndex;

        switch (type)
        {
            case InteractableObject.InteractionType.Pickup:
                DrawPickupSettings();
                break;
            case InteractableObject.InteractionType.ZoomView:
                DrawZoomViewSettings();
                break;
            case InteractableObject.InteractionType.Trigger:
                DrawTriggerSettings();
                break;
            case InteractableObject.InteractionType.RequireItem:
                DrawRequireItemSettings();
                break;
            case InteractableObject.InteractionType.ItemCombine:
                DrawItemCombineSettings();
                break;
            case InteractableObject.InteractionType.StateSwitch:
                DrawStateSwitchSettings();
                break;
            case InteractableObject.InteractionType.ObjectSwap:
                DrawObjectSwapSettings();
                break;
            case InteractableObject.InteractionType.Container:
                DrawContainerSettings();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    // ========== 各类型绘制方法 ==========

    private void DrawPickupSettings()
    {
        EditorGUILayout.LabelField("拾取物品设置 (Pickup)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(item, new GUIContent("Item Data"));
        EditorGUILayout.PropertyField(isPickupable, new GUIContent("Is Pickupable"));
        EditorGUI.indentLevel--;

        DrawSoundSection(pickupSoundName, "Pickup Sound Name");
    }

    private void DrawZoomViewSettings()
    {
        EditorGUILayout.LabelField("放大视图设置 (ZoomView)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(zoomViewTarget, new GUIContent("Zoom View Target (推荐)"));
        EditorGUILayout.PropertyField(associatedZoomView, new GUIContent("Associated Zoom View (旧版兼容)"));
        EditorGUI.indentLevel--;

        DrawSoundSection(zoomSoundName, "Zoom Sound Name");
    }

    private void DrawTriggerSettings()
    {
        EditorGUILayout.LabelField("触发事件设置 (Trigger)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(disableAfterTrigger, new GUIContent("Disable After Trigger"));
        EditorGUILayout.PropertyField(OnTrigger, new GUIContent("On Trigger ()"));
        EditorGUI.indentLevel--;

        DrawSoundSection(triggerSoundName, "Trigger Sound Name");
    }

    private void DrawRequireItemSettings()
    {
        EditorGUILayout.LabelField("条件触发设置 (RequireItem)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("物品条件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(requiredItem, new GUIContent("Required Item"));
        EditorGUILayout.PropertyField(consumeItemOnUse, new GUIContent("Consume Item On Use"));
        EditorGUILayout.PropertyField(disableAfterTrigger, new GUIContent("Disable After Trigger"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("提示信息", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(noItemHint, new GUIContent("No Item Hint"));
        EditorGUILayout.PropertyField(wrongItemHint, new GUIContent("Wrong Item Hint"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("事件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(OnItemUsedSuccess, new GUIContent("On Item Used Success ()"));
        EditorGUI.indentLevel--;

        DrawSoundSection(itemUsedSoundName, "Item Used Sound Name");
    }

    private void DrawItemCombineSettings()
    {
        EditorGUILayout.LabelField("物品合成设置 (ItemCombine)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("合成配方", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(combineRequiredItem, new GUIContent("需要的物品 (手持)"));
        EditorGUILayout.PropertyField(combineResultItem, new GUIContent("产出的物品"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("合成选项", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(consumeCombineItem, new GUIContent("Consume Combine Item"));
        EditorGUILayout.PropertyField(disableAfterCombine, new GUIContent("Disable After Combine"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("事件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(OnCombineSuccess, new GUIContent("On Combine Success ()"));
        EditorGUI.indentLevel--;

        DrawSoundSection(combineSoundName, "Combine Sound Name");
    }

    private void DrawStateSwitchSettings()
    {
        EditorGUILayout.LabelField("状态切换设置 (StateSwitch)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("切换条件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(switchRequiredItem, new GUIContent("Switch Required Item"));
        EditorGUILayout.PropertyField(consumeSwitchItem, new GUIContent("Consume Switch Item"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("外观", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(switchedSprite, new GUIContent("Switched Sprite"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("状态", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(hasStateSwitch, new GUIContent("Has State Switch"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("事件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(OnStateSwitchSuccess, new GUIContent("On State Switch Success ()"));
        EditorGUI.indentLevel--;

        // ⭐ 音效设置区域
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("音效设置（可选）", EditorStyles.miniBoldLabel);
        EditorGUI.indentLevel++;

        DrawSoundSection(stateSwitchSoundName, "State Switch Sound Name");

        // ⭐ 新增：等待音效选项
        EditorGUILayout.PropertyField(waitForSoundBeforeSwitch,
            new GUIContent("Wait For Sound", "勾选后，将等待音效播放完毕再切换图片"));

        EditorGUI.indentLevel--;
    }

    private void DrawObjectSwapSettings()
    {
        EditorGUILayout.LabelField("物体切换设置 (ObjectSwap)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("切换目标", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(swapTargetObject, new GUIContent("Swap Target Object"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("解锁条件（可选）", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(swapRequiredItem, new GUIContent("Swap Required Item"));
        EditorGUILayout.PropertyField(consumeSwapItem, new GUIContent("Consume Swap Item"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("状态", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(isSwapUnlocked, new GUIContent("Is Swap Unlocked"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("事件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(OnSwapSuccess, new GUIContent("On Swap Success ()"));
        EditorGUI.indentLevel--;

        DrawSoundSection(swapSoundName, "Swap Sound Name");
    }

    private void DrawContainerSettings()
    {
        EditorGUILayout.LabelField("容器设置 (Container)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("解锁条件（可选）", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(containerRequiredItem, new GUIContent("Container Required Item"));
        EditorGUILayout.PropertyField(consumeContainerItem, new GUIContent("Consume Container Item"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("状态", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(isContainerUnlocked, new GUIContent("Is Container Unlocked"));
        EditorGUILayout.PropertyField(isContainerOpen, new GUIContent("Is Container Open"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("外观", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(containerClosedSprite, new GUIContent("Closed Sprite"));
        EditorGUILayout.PropertyField(containerOpenedSprite, new GUIContent("Opened Sprite"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("内容物", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(containedObjects, new GUIContent("Contained Objects"), true);
        EditorGUI.indentLevel--;

        // 容器有两个音效字段
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("音效设置（可选）", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawSFXField(containerOpenSound, "Container Open Sound");
        DrawSFXField(containerCloseSound, "Container Close Sound");
        EditorGUI.indentLevel--;
    }

    // ========== SFX 字段绘制方法 ==========

    /// <summary>
    /// 绘制音效配置区域（单个音效）
    /// </summary>
    private void DrawSoundSection(SerializedProperty soundProperty, string label)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("音效设置（可选）", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        DrawSFXField(soundProperty, label);
        EditorGUI.indentLevel--;
    }

    /// <summary>
    /// 绘制单个 SFX 字段 - 支持 AudioClip 拖拽
    /// </summary>
    private void DrawSFXField(SerializedProperty property, string label)
    {
        if (property == null) return;

        string currentValue = property.stringValue;
        AudioClip currentClip = SFXFieldAutoDrawer.FindClip(currentValue);
        bool hasValue = !string.IsNullOrEmpty(currentValue);
        bool clipFound = currentClip != null;

        // 设置背景色反馈
        Color originalBg = GUI.backgroundColor;
        if (hasValue && !clipFound)
        {
            GUI.backgroundColor = new Color(1f, 0.8f, 0.6f); // 橙色警告
        }
        else if (clipFound)
        {
            GUI.backgroundColor = new Color(0.75f, 1f, 0.75f); // 绿色成功
        }

        EditorGUILayout.BeginHorizontal();

        // AudioClip 拖拽字段
        EditorGUI.BeginChangeCheck();
        AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField(
            new GUIContent(label, property.tooltip),
            currentClip,
            typeof(AudioClip),
            false
        );

        GUI.backgroundColor = originalBg;

        if (EditorGUI.EndChangeCheck())
        {
            if (newClip != null)
            {
                property.stringValue = SFXFieldAutoDrawer.GetResourcesPath(newClip);
            }
            else
            {
                property.stringValue = "";
            }
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
        if (GUILayout.Button(new GUIContent("↻", "刷新音效缓存"), GUILayout.Width(25), GUILayout.Height(18)))
        {
            SFXFieldAutoDrawer.RefreshCache();
        }

        EditorGUILayout.EndHorizontal();

        // 警告提示
        if (hasValue && !clipFound)
        {
            EditorGUILayout.HelpBox($"找不到音效文件: {currentValue}", MessageType.Warning);
        }
    }

    /// <summary>
    /// 在编辑器中预览播放音效
    /// </summary>
    private void PlayClipPreview(AudioClip clip)
    {
        if (clip == null) return;

        // 使用反射调用 Unity 内部的音频预览 API
        System.Type audioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        if (audioUtilType != null)
        {
            // 尝试新版 API (Unity 2020+)
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

            // 尝试旧版 API
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