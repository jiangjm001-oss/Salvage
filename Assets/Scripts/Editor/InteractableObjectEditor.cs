// Assets/Editor/InteractableObjectEditor.cs
// 自定义编辑器 - 根据交互类型动态显示对应设置
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(InteractableObject))]
public class InteractableObjectEditor : Editor
{
    // 基本信息
    SerializedProperty objectID;
    SerializedProperty displayName;
    SerializedProperty interactionType;

    // Pickup
    SerializedProperty item;
    SerializedProperty isPickupable;

    // ZoomView
    SerializedProperty zoomViewTarget;
    SerializedProperty associatedZoomView;

    // 音效
    SerializedProperty pickupSoundName;
    SerializedProperty zoomSoundName;
    SerializedProperty triggerSoundName;

    // Trigger
    SerializedProperty disableAfterTrigger;
    SerializedProperty OnTrigger;

    // RequireItem
    SerializedProperty requiredItem;
    SerializedProperty consumeItemOnUse;
    SerializedProperty noItemHint;
    SerializedProperty wrongItemHint;
    SerializedProperty OnItemUsedSuccess;
    SerializedProperty itemUsedSoundName;

    // ItemCombine
    SerializedProperty combineRequiredItem;
    SerializedProperty resultItem;
    SerializedProperty consumeCombineItem;
    SerializedProperty combineHint;
    SerializedProperty wrongCombineHint;
    SerializedProperty OnCombineSuccess;
    SerializedProperty combineSoundName;

    // StateSwitch
    SerializedProperty switchRequiredItem;
    SerializedProperty consumeSwitchItem;
    SerializedProperty switchedSprite;
    SerializedProperty hasStateSwitch;
    SerializedProperty OnStateSwitchSuccess;
    SerializedProperty stateSwitchSoundName;

    // ObjectSwap
    SerializedProperty swapTargetObject;
    SerializedProperty swapRequiredItem;
    SerializedProperty consumeSwapItem;
    SerializedProperty isSwapUnlocked;
    SerializedProperty OnSwapSuccess;
    SerializedProperty swapSoundName;

    // Container
    SerializedProperty containerRequiredItem;
    SerializedProperty consumeContainerItem;
    SerializedProperty isContainerUnlocked;
    SerializedProperty isContainerOpen;
    SerializedProperty containerClosedSprite;
    SerializedProperty containerOpenedSprite;
    SerializedProperty containedObjects;
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

        // ZoomView
        zoomViewTarget = serializedObject.FindProperty("zoomViewTarget");
        associatedZoomView = serializedObject.FindProperty("associatedZoomView");

        // 音效
        pickupSoundName = serializedObject.FindProperty("pickupSoundName");
        zoomSoundName = serializedObject.FindProperty("zoomSoundName");
        triggerSoundName = serializedObject.FindProperty("triggerSoundName");

        // Trigger
        disableAfterTrigger = serializedObject.FindProperty("disableAfterTrigger");
        OnTrigger = serializedObject.FindProperty("OnTrigger");

        // RequireItem
        requiredItem = serializedObject.FindProperty("requiredItem");
        consumeItemOnUse = serializedObject.FindProperty("consumeItemOnUse");
        noItemHint = serializedObject.FindProperty("noItemHint");
        wrongItemHint = serializedObject.FindProperty("wrongItemHint");
        OnItemUsedSuccess = serializedObject.FindProperty("OnItemUsedSuccess");
        itemUsedSoundName = serializedObject.FindProperty("itemUsedSoundName");

        // ItemCombine
        combineRequiredItem = serializedObject.FindProperty("combineRequiredItem");
        resultItem = serializedObject.FindProperty("resultItem");
        consumeCombineItem = serializedObject.FindProperty("consumeCombineItem");
        combineHint = serializedObject.FindProperty("combineHint");
        wrongCombineHint = serializedObject.FindProperty("wrongCombineHint");
        OnCombineSuccess = serializedObject.FindProperty("OnCombineSuccess");
        combineSoundName = serializedObject.FindProperty("combineSoundName");

        // StateSwitch
        switchRequiredItem = serializedObject.FindProperty("switchRequiredItem");
        consumeSwitchItem = serializedObject.FindProperty("consumeSwitchItem");
        switchedSprite = serializedObject.FindProperty("switchedSprite");
        hasStateSwitch = serializedObject.FindProperty("hasStateSwitch");
        OnStateSwitchSuccess = serializedObject.FindProperty("OnStateSwitchSuccess");
        stateSwitchSoundName = serializedObject.FindProperty("stateSwitchSoundName");

        // ObjectSwap
        swapTargetObject = serializedObject.FindProperty("swapTargetObject");
        swapRequiredItem = serializedObject.FindProperty("swapRequiredItem");
        consumeSwapItem = serializedObject.FindProperty("consumeSwapItem");
        isSwapUnlocked = serializedObject.FindProperty("isSwapUnlocked");
        OnSwapSuccess = serializedObject.FindProperty("OnSwapSuccess");
        swapSoundName = serializedObject.FindProperty("swapSoundName");

        // Container
        containerRequiredItem = serializedObject.FindProperty("containerRequiredItem");
        consumeContainerItem = serializedObject.FindProperty("consumeContainerItem");
        isContainerUnlocked = serializedObject.FindProperty("isContainerUnlocked");
        isContainerOpen = serializedObject.FindProperty("isContainerOpen");
        containerClosedSprite = serializedObject.FindProperty("containerClosedSprite");
        containerOpenedSprite = serializedObject.FindProperty("containerOpenedSprite");
        containedObjects = serializedObject.FindProperty("containedObjects");
        containerOpenSound = serializedObject.FindProperty("containerOpenSound");
        containerCloseSound = serializedObject.FindProperty("containerCloseSound");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ========== 基本信息（始终显示）==========
        EditorGUILayout.LabelField("基本信息", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(objectID);
        EditorGUILayout.PropertyField(displayName);

        EditorGUILayout.Space(10);

        // ========== 交互设置 ==========
        EditorGUILayout.LabelField("交互设置", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(interactionType);

        EditorGUILayout.Space(10);

        // 获取当前选择的交互类型
        InteractableObject.InteractionType currentType =
            (InteractableObject.InteractionType)interactionType.enumValueIndex;

        // ========== 根据类型显示对应设置 ==========
        switch (currentType)
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

    // ========== 各类型的绘制方法 ==========

    private void DrawPickupSettings()
    {
        EditorGUILayout.LabelField("拾取物品设置 (Pickup)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(item, new GUIContent("Item"));
        EditorGUILayout.PropertyField(isPickupable, new GUIContent("Is Pickupable"));
        EditorGUI.indentLevel--;

        DrawSoundSection(pickupSoundName, "Pickup Sound Name");
    }

    private void DrawZoomViewSettings()
    {
        EditorGUILayout.LabelField("放大视图设置 (ZoomView)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(zoomViewTarget, new GUIContent("Zoom View Target"));
        EditorGUILayout.PropertyField(associatedZoomView, new GUIContent("Associated Zoom View"));
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
        EditorGUILayout.PropertyField(requiredItem, new GUIContent("Required Item"));
        EditorGUILayout.PropertyField(consumeItemOnUse, new GUIContent("Consume Item On Use"));
        EditorGUILayout.PropertyField(noItemHint, new GUIContent("No Item Hint"));
        EditorGUILayout.PropertyField(wrongItemHint, new GUIContent("Wrong Item Hint"));
        EditorGUILayout.PropertyField(OnItemUsedSuccess, new GUIContent("On Item Used Success ()"));
        EditorGUI.indentLevel--;

        DrawSoundSection(itemUsedSoundName, "Item Used Sound Name");
    }

    private void DrawItemCombineSettings()
    {
        EditorGUILayout.LabelField("物品合成设置 (ItemCombine)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(combineRequiredItem, new GUIContent("Combine Required Item"));
        EditorGUILayout.PropertyField(resultItem, new GUIContent("Result Item"));
        EditorGUILayout.PropertyField(consumeCombineItem, new GUIContent("Consume Combine Item"));
        EditorGUILayout.PropertyField(combineHint, new GUIContent("Combine Hint"));
        EditorGUILayout.PropertyField(wrongCombineHint, new GUIContent("Wrong Combine Hint"));
        EditorGUILayout.PropertyField(OnCombineSuccess, new GUIContent("On Combine Success ()"));
        EditorGUI.indentLevel--;

        DrawSoundSection(combineSoundName, "Combine Sound Name");
    }

    private void DrawStateSwitchSettings()
    {
        EditorGUILayout.LabelField("状态切换设置 (StateSwitch)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(switchRequiredItem, new GUIContent("Switch Required Item"));
        EditorGUILayout.PropertyField(consumeSwitchItem, new GUIContent("Consume Switch Item"));
        EditorGUILayout.PropertyField(switchedSprite, new GUIContent("Switched Sprite"));
        EditorGUILayout.PropertyField(hasStateSwitch, new GUIContent("Has State Switch"));
        EditorGUILayout.PropertyField(OnStateSwitchSuccess, new GUIContent("On State Switch Success ()"));
        EditorGUI.indentLevel--;

        DrawSoundSection(stateSwitchSoundName, "State Switch Sound Name");
    }

    private void DrawObjectSwapSettings()
    {
        EditorGUILayout.LabelField("物体切换设置 (ObjectSwap)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(swapTargetObject, new GUIContent("Swap Target Object"));
        EditorGUILayout.PropertyField(swapRequiredItem, new GUIContent("Swap Required Item"));
        EditorGUILayout.PropertyField(consumeSwapItem, new GUIContent("Consume Swap Item"));
        EditorGUILayout.PropertyField(isSwapUnlocked, new GUIContent("Is Swap Unlocked"));
        EditorGUILayout.PropertyField(OnSwapSuccess, new GUIContent("On Swap Success ()"));
        EditorGUI.indentLevel--;

        DrawSoundSection(swapSoundName, "Swap Sound Name");
    }

    private void DrawContainerSettings()
    {
        EditorGUILayout.LabelField("容器设置 (Container)", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("解锁条件", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(containerRequiredItem, new GUIContent("Container Required Item"));
        EditorGUILayout.PropertyField(consumeContainerItem, new GUIContent("Consume Container Item"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("状态", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(isContainerUnlocked, new GUIContent("Is Container Unlocked"));
        EditorGUILayout.PropertyField(isContainerOpen, new GUIContent("Is Container Open"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("外观", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(containerClosedSprite, new GUIContent("Container Closed Sprite"));
        EditorGUILayout.PropertyField(containerOpenedSprite, new GUIContent("Container Opened Sprite"));

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("内容物", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(containedObjects, new GUIContent("Contained Objects"));

        EditorGUI.indentLevel--;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("音效设置（可选）", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(containerOpenSound, new GUIContent("Container Open Sound"));
        EditorGUILayout.PropertyField(containerCloseSound, new GUIContent("Container Close Sound"));
        EditorGUI.indentLevel--;
    }

    // ========== 辅助方法 ==========

    private void DrawSoundSection(SerializedProperty soundProperty, string label)
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("音效设置（可选）", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(soundProperty, new GUIContent(label));
        EditorGUI.indentLevel--;
    }
}