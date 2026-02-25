// Assets/Scripts/Editor/SpotTheDifferenceEditor.cs
// 找茬玩法编辑器 - 可视化配置差异点位置（世界空间版）

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SpotTheDifferenceManager))]
public class SpotTheDifferenceEditor : Editor
{
    private SpotTheDifferenceManager manager;

    private SerializedProperty leftImageProp;
    private SerializedProperty rightImageProp;
    private SerializedProperty differenceSpotsProp;
    private SerializedProperty clickRadiusProp;
    private SerializedProperty circleMarkerSpriteProp;
    private SerializedProperty circleMarkerScaleProp;
    private SerializedProperty circleMarkerColorProp;
    private SerializedProperty circleAnimationDurationProp;
    private SerializedProperty circleMarkerSortingOffsetProp;
    private SerializedProperty collectableItemProp;
    private SerializedProperty rewardItemDataProp;
    private SerializedProperty itemFadeDurationProp;
    private SerializedProperty backButtonProp;
    private SerializedProperty backButtonFadeDurationProp;
    private SerializedProperty correctSoundNameProp;
    private SerializedProperty wrongSoundNameProp;
    private SerializedProperty itemAppearSoundNameProp;
    private SerializedProperty pickupSoundNameProp;
    private SerializedProperty onSpotFoundProp;
    private SerializedProperty onAllSpotsFoundProp;
    private SerializedProperty onItemCollectedProp;

    private int selectedSpotIndex = -1;
    private bool isPositionEditMode = false;

    private void OnEnable()
    {
        manager = (SpotTheDifferenceManager)target;

        leftImageProp = serializedObject.FindProperty("leftImage");
        rightImageProp = serializedObject.FindProperty("rightImage");
        differenceSpotsProp = serializedObject.FindProperty("differenceSpots");
        clickRadiusProp = serializedObject.FindProperty("clickRadius");
        circleMarkerSpriteProp = serializedObject.FindProperty("circleMarkerSprite");
        circleMarkerScaleProp = serializedObject.FindProperty("circleMarkerScale");
        circleMarkerColorProp = serializedObject.FindProperty("circleMarkerColor");
        circleAnimationDurationProp = serializedObject.FindProperty("circleAnimationDuration");
        circleMarkerSortingOffsetProp = serializedObject.FindProperty("circleMarkerSortingOffset");
        collectableItemProp = serializedObject.FindProperty("collectableItem");
        rewardItemDataProp = serializedObject.FindProperty("rewardItemData");
        itemFadeDurationProp = serializedObject.FindProperty("itemFadeDuration");
        backButtonProp = serializedObject.FindProperty("backButton");
        backButtonFadeDurationProp = serializedObject.FindProperty("backButtonFadeDuration");
        correctSoundNameProp = serializedObject.FindProperty("correctSoundName");
        wrongSoundNameProp = serializedObject.FindProperty("wrongSoundName");
        itemAppearSoundNameProp = serializedObject.FindProperty("itemAppearSoundName");
        pickupSoundNameProp = serializedObject.FindProperty("pickupSoundName");
        onSpotFoundProp = serializedObject.FindProperty("OnSpotFound");
        onAllSpotsFoundProp = serializedObject.FindProperty("OnAllSpotsFound");
        onItemCollectedProp = serializedObject.FindProperty("OnItemCollected");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ============ 图片设置 ============
        EditorGUILayout.LabelField("图片设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(leftImageProp, new GUIContent("左侧图片 (SpriteRenderer)"));
        EditorGUILayout.PropertyField(rightImageProp, new GUIContent("右侧图片 (SpriteRenderer)"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ============ 差异点设置 ============
        EditorGUILayout.LabelField("差异点设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        // 快捷按钮
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("初始化10个差异点", GUILayout.Height(25)))
        {
            InitializeSpots();
        }
        if (GUILayout.Button("清空所有", GUILayout.Height(25)))
        {
            ClearAllSpots();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 位置编辑模式提示
        EditorGUILayout.BeginHorizontal();
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = isPositionEditMode ? Color.green : originalColor;
        if (GUILayout.Button(isPositionEditMode ? "● 拖拽编辑模式 ON" : "○ 拖拽编辑模式 OFF", GUILayout.Height(30)))
        {
            isPositionEditMode = !isPositionEditMode;
            if (isPositionEditMode && selectedSpotIndex < 0 && differenceSpotsProp.arraySize > 0)
            {
                selectedSpotIndex = 0;
            }
            SceneView.RepaintAll();
        }
        GUI.backgroundColor = originalColor;
        EditorGUILayout.EndHorizontal();

        if (isPositionEditMode)
        {
            EditorGUILayout.HelpBox("在 Scene 视图中拖拽黄色圆圈来调整差异点位置", MessageType.Info);
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(clickRadiusProp, new GUIContent("点击检测半径（世界单位）"));

        EditorGUILayout.Space(5);

        // 差异点列表
        EditorGUILayout.LabelField($"差异点列表 ({differenceSpotsProp.arraySize}/10)");

        for (int i = 0; i < differenceSpotsProp.arraySize; i++)
        {
            var spotProp = differenceSpotsProp.GetArrayElementAtIndex(i);
            var nameProp = spotProp.FindPropertyRelative("spotName");
            var posProp = spotProp.FindPropertyRelative("normalizedPosition");

            EditorGUILayout.BeginHorizontal();

            // 选中指示器
            bool isSelected = (selectedSpotIndex == i);
            GUI.backgroundColor = isSelected ? Color.cyan : originalColor;

            if (GUILayout.Button(isSelected ? "►" : " ", GUILayout.Width(25)))
            {
                selectedSpotIndex = i;
                isPositionEditMode = true;
                SceneView.RepaintAll();
            }

            GUI.backgroundColor = originalColor;

            // 序号
            EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(25));

            // 名称
            nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.Width(70));

            // 位置
            EditorGUILayout.LabelField("X:", GUILayout.Width(18));
            Vector2 pos = posProp.vector2Value;
            pos.x = EditorGUILayout.FloatField(pos.x, GUILayout.Width(45));
            EditorGUILayout.LabelField("Y:", GUILayout.Width(18));
            pos.y = EditorGUILayout.FloatField(pos.y, GUILayout.Width(45));
            posProp.vector2Value = pos;

            // 删除按钮
            if (GUILayout.Button("×", GUILayout.Width(22)))
            {
                differenceSpotsProp.DeleteArrayElementAtIndex(i);
                if (selectedSpotIndex >= differenceSpotsProp.arraySize)
                {
                    selectedSpotIndex = differenceSpotsProp.arraySize - 1;
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        // 添加按钮
        if (differenceSpotsProp.arraySize < 10)
        {
            if (GUILayout.Button("+ 添加差异点"))
            {
                differenceSpotsProp.InsertArrayElementAtIndex(differenceSpotsProp.arraySize);
                var newSpot = differenceSpotsProp.GetArrayElementAtIndex(differenceSpotsProp.arraySize - 1);
                newSpot.FindPropertyRelative("spotName").stringValue = $"Spot_{differenceSpotsProp.arraySize}";
                newSpot.FindPropertyRelative("normalizedPosition").vector2Value = new Vector2(0.5f, 0.5f);
                selectedSpotIndex = differenceSpotsProp.arraySize - 1;
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ============ 圆圈标记设置 ============
        EditorGUILayout.LabelField("圆圈标记设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(circleMarkerSpriteProp, new GUIContent("圆圈 Sprite（可选）"));
        EditorGUILayout.PropertyField(circleMarkerScaleProp, new GUIContent("圆圈大小"));
        EditorGUILayout.PropertyField(circleMarkerColorProp, new GUIContent("圆圈颜色"));
        EditorGUILayout.PropertyField(circleAnimationDurationProp, new GUIContent("出现动画时长"));
        EditorGUILayout.PropertyField(circleMarkerSortingOffsetProp, new GUIContent("Sorting Order 偏移"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ============ 奖励物品设置 ============
        EditorGUILayout.LabelField("奖励物品设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(collectableItemProp, new GUIContent("可拾取物品 GameObject"));
        EditorGUILayout.PropertyField(rewardItemDataProp, new GUIContent("物品数据 (ItemData)"));
        EditorGUILayout.PropertyField(itemFadeDurationProp, new GUIContent("物品渐显时长"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ============ 返回按钮设置 ============
        EditorGUILayout.LabelField("返回按钮设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(backButtonProp, new GUIContent("返回按钮 GameObject"));
        EditorGUILayout.PropertyField(backButtonFadeDurationProp, new GUIContent("按钮渐显时长"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ============ 音效设置 ============
        EditorGUILayout.LabelField("音效设置", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(correctSoundNameProp, new GUIContent("找到正确位置"));
        EditorGUILayout.PropertyField(wrongSoundNameProp, new GUIContent("点击错误（可选）"));
        EditorGUILayout.PropertyField(itemAppearSoundNameProp, new GUIContent("道具出现"));
        EditorGUILayout.PropertyField(pickupSoundNameProp, new GUIContent("拾取物品"));
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ============ 事件 ============
        EditorGUILayout.LabelField("事件", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(onSpotFoundProp);
        EditorGUILayout.PropertyField(onAllSpotsFoundProp);
        EditorGUILayout.PropertyField(onItemCollectedProp);
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);

        // ============ 调试工具 ============
        EditorGUILayout.LabelField("调试工具", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        if (Application.isPlaying)
        {
            if (GUILayout.Button("重置游戏"))
            {
                manager.ResetGame();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("运行游戏后可使用重置功能", MessageType.Info);
        }

        EditorGUI.indentLevel--;

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            SceneView.RepaintAll();
        }
    }

    private void InitializeSpots()
    {
        differenceSpotsProp.ClearArray();
        for (int i = 0; i < 10; i++)
        {
            differenceSpotsProp.InsertArrayElementAtIndex(i);
            var spot = differenceSpotsProp.GetArrayElementAtIndex(i);
            spot.FindPropertyRelative("spotName").stringValue = $"Spot_{i + 1}";
            spot.FindPropertyRelative("normalizedPosition").vector2Value = new Vector2(0.5f, 0.5f);
        }
        selectedSpotIndex = 0;
        isPositionEditMode = true;
        serializedObject.ApplyModifiedProperties();
    }

    private void ClearAllSpots()
    {
        if (EditorUtility.DisplayDialog("确认清空", "确定要清空所有差异点吗？", "确定", "取消"))
        {
            differenceSpotsProp.ClearArray();
            selectedSpotIndex = -1;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void OnSceneGUI()
    {
        serializedObject.Update();

        SpriteRenderer leftImage = leftImageProp.objectReferenceValue as SpriteRenderer;
        if (leftImage == null) return;

        Bounds bounds = leftImage.bounds;
        float clickRadius = clickRadiusProp.floatValue;

        // 绘制所有差异点
        for (int i = 0; i < differenceSpotsProp.arraySize; i++)
        {
            var spotProp = differenceSpotsProp.GetArrayElementAtIndex(i);
            var posProp = spotProp.FindPropertyRelative("normalizedPosition");
            Vector2 normalizedPos = posProp.vector2Value;

            // 计算世界坐标
            Vector3 worldPos = GetWorldPositionFromNormalized(bounds, normalizedPos);

            // 绘制样式
            bool isSelected = (selectedSpotIndex == i);
            Handles.color = isSelected ? Color.cyan : Color.yellow;

            // 绘制圆圈
            Handles.DrawWireDisc(worldPos, Vector3.forward, clickRadius);

            // 绘制序号标签
            Handles.Label(worldPos + Vector3.up * clickRadius * 1.3f,
                $"{i + 1}",
                new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = isSelected ? Color.cyan : Color.yellow }
                });

            // 如果在编辑模式，允许拖动
            if (isPositionEditMode)
            {
                EditorGUI.BeginChangeCheck();

                float handleSize = HandleUtility.GetHandleSize(worldPos) * 0.1f;
                Vector3 newPos = Handles.FreeMoveHandle(
                    worldPos,
                    handleSize,
                    Vector3.zero,
                    Handles.CircleHandleCap
                );

                if (EditorGUI.EndChangeCheck())
                {
                    selectedSpotIndex = i;
                    Vector2 newNormalized = GetNormalizedPositionFromWorld(bounds, newPos);
                    newNormalized.x = Mathf.Clamp01(newNormalized.x);
                    newNormalized.y = Mathf.Clamp01(newNormalized.y);
                    posProp.vector2Value = newNormalized;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        // 绘制图片边界
        Handles.color = new Color(0f, 1f, 1f, 0.3f);
        Vector3[] corners = new Vector3[4];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.center.z);
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.center.z);
        corners[2] = new Vector3(bounds.max.x, bounds.max.y, bounds.center.z);
        corners[3] = new Vector3(bounds.min.x, bounds.max.y, bounds.center.z);
        Handles.DrawSolidRectangleWithOutline(corners, new Color(0, 1, 1, 0.05f), Color.cyan);
    }

    private Vector3 GetWorldPositionFromNormalized(Bounds bounds, Vector2 normalizedPos)
    {
        float x = bounds.min.x + normalizedPos.x * bounds.size.x;
        float y = bounds.min.y + normalizedPos.y * bounds.size.y;
        return new Vector3(x, y, bounds.center.z - 0.01f);
    }

    private Vector2 GetNormalizedPositionFromWorld(Bounds bounds, Vector3 worldPos)
    {
        float x = (worldPos.x - bounds.min.x) / bounds.size.x;
        float y = (worldPos.y - bounds.min.y) / bounds.size.y;
        return new Vector2(x, y);
    }
}
#endif