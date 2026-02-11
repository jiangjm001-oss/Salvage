#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;

/// <summary>
/// Unity 音频剪辑编辑器工具
/// 功能：加载音频、波形显示、剪辑操作、音量控制、预览播放、保存剪辑
/// 使用方法：菜单栏 → Tools → Audio Clip Editor
/// </summary>
public class AudioClipEditorWindow : EditorWindow
{
    #region 字段定义

    // 音频相关
    private AudioClip sourceClip;
    private AudioClip previewClip;
    private float[] audioSamples;
    private Texture2D waveformTexture;

    // 剪辑参数
    private float startTime = 0f;
    private float endTime = 1f;
    private float volume = 1f;
    private bool isPlaying = false;
    private double playStartTime;
    private float playStartPosition;

    // UI 相关
    private Vector2 scrollPosition;
    private bool isDraggingStart = false;
    private bool isDraggingEnd = false;
    private bool isDraggingPlayhead = false;
    private float currentPlaybackTime = 0f;

    // 波形显示设置
    private int waveformWidth = 800;
    private int waveformHeight = 150;
    private Color waveformColor = new Color(0.2f, 0.7f, 1f, 1f);
    private Color waveformBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    private Color selectionColor = new Color(0.3f, 0.8f, 0.3f, 0.3f);
    private Color handleColor = new Color(1f, 0.8f, 0.2f, 1f);
    private Color playheadColor = new Color(1f, 0.3f, 0.3f, 1f);

    // 动画相关
    private float animationTime = 0f;
    private float pulseIntensity = 0f;

    // 保存路径
    private string lastSavePath = "";

    // ===== 音频播放系统 =====
    private static GameObject audioPreviewObject;
    private static AudioSource audioPreviewSource;

    // AudioUtil 反射备用
    private static Type audioUtilType;
    private static MethodInfo playClipMethod;
    private static MethodInfo stopAllClipsMethod;
    private static bool useAudioSource = true; // 默认使用 AudioSource 方式

    #endregion

    #region 菜单入口

    [MenuItem("Tools/Audio Clip Editor %#a")] // Ctrl+Shift+A 快捷键
    public static void ShowWindow()
    {
        AudioClipEditorWindow window = GetWindow<AudioClipEditorWindow>("音频剪辑工具");
        window.minSize = new Vector2(600, 500);
        window.Show();
    }

    #endregion

    #region Unity 生命周期

    private void OnEnable()
    {
        InitializeAudioSystem();
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        StopPreview();
        CleanupTextures();
        CleanupAudioPreview();
    }

    private void OnDestroy()
    {
        CleanupAudioPreview();
    }

    private void OnEditorUpdate()
    {
        // 更新播放头位置
        if (isPlaying && previewClip != null)
        {
            // 检查是否还在播放
            if (useAudioSource && audioPreviewSource != null)
            {
                if (!audioPreviewSource.isPlaying)
                {
                    StopPreview();
                    currentPlaybackTime = 0f;
                    Repaint();
                    return;
                }

                // 使用 AudioSource 的时间
                currentPlaybackTime = audioPreviewSource.time;
            }
            else
            {
                // 使用计算的时间
                float elapsed = (float)(EditorApplication.timeSinceStartup - playStartTime);
                currentPlaybackTime = playStartPosition + elapsed;

                float clipDuration = endTime - startTime;
                if (currentPlaybackTime >= clipDuration)
                {
                    StopPreview();
                    currentPlaybackTime = 0f;
                }
            }

            // 动画效果
            animationTime += 0.016f;
            pulseIntensity = Mathf.Sin(animationTime * 10f) * 0.5f + 0.5f;

            Repaint();
        }
    }

    #endregion

    #region 音频系统初始化

    private void InitializeAudioSystem()
    {
        // 尝试初始化 AudioUtil（备用方案）
        try
        {
            audioUtilType = typeof(Editor).Assembly.GetType("UnityEditor.AudioUtil");
            if (audioUtilType != null)
            {
                // Unity 2020+ 的方法签名
                playClipMethod = audioUtilType.GetMethod("PlayPreviewClip",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                    null);

                // 如果上面失败，尝试旧版签名
                if (playClipMethod == null)
                {
                    playClipMethod = audioUtilType.GetMethod("PlayPreviewClip",
                        BindingFlags.Static | BindingFlags.Public);
                }

                stopAllClipsMethod = audioUtilType.GetMethod("StopAllPreviewClips",
                    BindingFlags.Static | BindingFlags.Public);
            }
        }
        catch (Exception e)
        {
            Debug.Log($"[AudioClipEditor] AudioUtil 初始化跳过: {e.Message}");
        }

        // 默认使用 AudioSource 方式（更可靠）
        useAudioSource = true;
    }

    private static void EnsureAudioPreviewObject()
    {
        if (audioPreviewObject == null)
        {
            audioPreviewObject = new GameObject("_AudioClipEditorPreview_");
            audioPreviewObject.hideFlags = HideFlags.HideAndDontSave;
            audioPreviewSource = audioPreviewObject.AddComponent<AudioSource>();
            audioPreviewSource.playOnAwake = false;
        }

        if (audioPreviewSource == null && audioPreviewObject != null)
        {
            audioPreviewSource = audioPreviewObject.GetComponent<AudioSource>();
            if (audioPreviewSource == null)
            {
                audioPreviewSource = audioPreviewObject.AddComponent<AudioSource>();
            }
        }
    }

    private static void CleanupAudioPreview()
    {
        if (audioPreviewSource != null)
        {
            audioPreviewSource.Stop();
        }

        if (audioPreviewObject != null)
        {
            DestroyImmediate(audioPreviewObject);
            audioPreviewObject = null;
            audioPreviewSource = null;
        }
    }

    #endregion

    #region GUI 绘制

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawAudioLoadSection();

        if (sourceClip != null)
        {
            DrawWaveformSection();
            DrawTimelineControls();
            DrawVolumeControl();
            DrawPlaybackControls();
            DrawSaveSection();
        }
        else
        {
            DrawEmptyState();
        }

        EditorGUILayout.EndScrollView();

        // 处理键盘快捷键
        HandleKeyboardShortcuts();
    }

    private void DrawHeader()
    {
        EditorGUILayout.Space(10);

        // 标题带动画效果
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 20,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.8f, 0.9f, 1f) }
        };

        EditorGUILayout.LabelField("🎵 音频剪辑工具", titleStyle, GUILayout.Height(30));

        // 分隔线
        Rect lineRect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.6f, 1f, 0.5f));

        EditorGUILayout.Space(10);
    }

    private void DrawAudioLoadSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("📂 加载音频", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUI.BeginChangeCheck();
        AudioClip newClip = (AudioClip)EditorGUILayout.ObjectField(
            "音频片段",
            sourceClip,
            typeof(AudioClip),
            false
        );

        if (EditorGUI.EndChangeCheck() && newClip != sourceClip)
        {
            LoadAudioClip(newClip);
        }

        // 显示音频信息
        if (sourceClip != null)
        {
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            DrawInfoBox("时长", $"{sourceClip.length:F2} 秒");
            DrawInfoBox("采样率", $"{sourceClip.frequency} Hz");
            DrawInfoBox("声道", $"{sourceClip.channels}");
            DrawInfoBox("采样数", $"{sourceClip.samples}");
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawInfoBox(string label, string value)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.MinWidth(80));

        GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.gray }
        };
        EditorGUILayout.LabelField(label, labelStyle);

        GUIStyle valueStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 11
        };
        EditorGUILayout.LabelField(value, valueStyle);

        EditorGUILayout.EndVertical();
    }

    private void DrawWaveformSection()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("📊 波形显示", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 波形区域
        Rect waveformRect = GUILayoutUtility.GetRect(
            waveformWidth,
            waveformHeight,
            GUILayout.ExpandWidth(true),
            GUILayout.MinHeight(waveformHeight)
        );

        // 更新波形宽度
        if (Event.current.type == EventType.Repaint)
        {
            waveformWidth = (int)waveformRect.width;
        }

        DrawWaveformWithSelection(waveformRect);
        HandleWaveformInteraction(waveformRect);

        EditorGUILayout.EndVertical();
    }

    private void DrawWaveformWithSelection(Rect rect)
    {
        // 背景
        EditorGUI.DrawRect(rect, waveformBackgroundColor);

        // 绘制波形纹理
        if (waveformTexture != null)
        {
            GUI.DrawTexture(rect, waveformTexture);
        }
        else if (sourceClip != null)
        {
            GenerateWaveformTexture();
        }

        // 绘制选区
        float startX = rect.x + (startTime / sourceClip.length) * rect.width;
        float endX = rect.x + (endTime / sourceClip.length) * rect.width;

        // 选区高亮
        Rect selectionRect = new Rect(startX, rect.y, endX - startX, rect.height);
        EditorGUI.DrawRect(selectionRect, selectionColor);

        // 非选区变暗
        if (startX > rect.x)
        {
            Rect leftDark = new Rect(rect.x, rect.y, startX - rect.x, rect.height);
            EditorGUI.DrawRect(leftDark, new Color(0, 0, 0, 0.5f));
        }
        if (endX < rect.xMax)
        {
            Rect rightDark = new Rect(endX, rect.y, rect.xMax - endX, rect.height);
            EditorGUI.DrawRect(rightDark, new Color(0, 0, 0, 0.5f));
        }

        // 开始手柄
        DrawHandle(new Rect(startX - 4, rect.y, 8, rect.height), handleColor, "▶");

        // 结束手柄
        DrawHandle(new Rect(endX - 4, rect.y, 8, rect.height), handleColor, "◀");

        // 播放头
        if (isPlaying || currentPlaybackTime > 0)
        {
            float playheadTime = startTime + currentPlaybackTime;
            float playheadX = rect.x + (playheadTime / sourceClip.length) * rect.width;

            Color headColor = isPlaying
                ? Color.Lerp(playheadColor, Color.white, pulseIntensity * 0.3f)
                : playheadColor;

            EditorGUI.DrawRect(new Rect(playheadX - 1, rect.y, 2, rect.height), headColor);

            // 播放头三角形指示
            DrawPlayheadTriangle(playheadX, rect.y, headColor);
        }

        // 时间刻度
        DrawTimeScale(rect);
    }

    private void DrawHandle(Rect rect, Color color, string symbol)
    {
        EditorGUI.DrawRect(rect, color);

        // 手柄指示符号
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.black },
            fontSize = 10
        };

        Rect labelRect = new Rect(rect.x - 4, rect.y - 15, 16, 15);
        GUI.Label(labelRect, symbol, style);
    }

    private void DrawPlayheadTriangle(float x, float y, Color color)
    {
        Rect triangleRect = new Rect(x - 6, y - 8, 12, 8);
        EditorGUI.DrawRect(triangleRect, color);
    }

    private void DrawTimeScale(Rect rect)
    {
        if (sourceClip == null) return;

        GUIStyle timeStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = new Color(0.7f, 0.7f, 0.7f) },
            fontSize = 9
        };

        float duration = sourceClip.length;
        int numMarkers = Mathf.Max(5, (int)(rect.width / 100));

        for (int i = 0; i <= numMarkers; i++)
        {
            float t = (float)i / numMarkers;
            float x = rect.x + t * rect.width;
            float time = t * duration;

            // 刻度线
            EditorGUI.DrawRect(new Rect(x, rect.yMax - 10, 1, 10), new Color(0.5f, 0.5f, 0.5f, 0.5f));

            // 时间标签
            Rect labelRect = new Rect(x - 20, rect.yMax + 2, 40, 15);
            GUI.Label(labelRect, FormatTime(time), timeStyle);
        }
    }

    private void HandleWaveformInteraction(Rect rect)
    {
        Event e = Event.current;

        if (!rect.Contains(e.mousePosition) && !isDraggingStart && !isDraggingEnd && !isDraggingPlayhead)
            return;

        float startX = rect.x + (startTime / sourceClip.length) * rect.width;
        float endX = rect.x + (endTime / sourceClip.length) * rect.width;

        Rect startHandleRect = new Rect(startX - 10, rect.y, 20, rect.height);
        Rect endHandleRect = new Rect(endX - 10, rect.y, 20, rect.height);

        // 鼠标按下
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            if (startHandleRect.Contains(e.mousePosition))
            {
                isDraggingStart = true;
                e.Use();
            }
            else if (endHandleRect.Contains(e.mousePosition))
            {
                isDraggingEnd = true;
                e.Use();
            }
            else if (rect.Contains(e.mousePosition))
            {
                // 点击波形区域设置播放头位置
                float clickTime = ((e.mousePosition.x - rect.x) / rect.width) * sourceClip.length;
                if (clickTime >= startTime && clickTime <= endTime)
                {
                    currentPlaybackTime = clickTime - startTime;
                    isDraggingPlayhead = true;
                    e.Use();
                }
            }
        }

        // 鼠标拖动
        if (e.type == EventType.MouseDrag)
        {
            float normalizedX = Mathf.Clamp01((e.mousePosition.x - rect.x) / rect.width);
            float time = normalizedX * sourceClip.length;

            if (isDraggingStart)
            {
                startTime = Mathf.Clamp(time, 0, endTime - 0.01f);
                RegenerateWaveform();
                e.Use();
            }
            else if (isDraggingEnd)
            {
                endTime = Mathf.Clamp(time, startTime + 0.01f, sourceClip.length);
                RegenerateWaveform();
                e.Use();
            }
            else if (isDraggingPlayhead)
            {
                if (time >= startTime && time <= endTime)
                {
                    currentPlaybackTime = time - startTime;
                }
                e.Use();
            }
        }

        // 鼠标释放
        if (e.type == EventType.MouseUp)
        {
            isDraggingStart = false;
            isDraggingEnd = false;
            isDraggingPlayhead = false;
        }

        // 更新鼠标指针
        if (startHandleRect.Contains(e.mousePosition) || endHandleRect.Contains(e.mousePosition))
        {
            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        }
    }

    private void DrawTimelineControls()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("✂️ 剪辑控制", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        // 起始时间
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 20));
        EditorGUILayout.LabelField("起始时间", EditorStyles.miniLabel);
        EditorGUI.BeginChangeCheck();
        float newStartTime = EditorGUILayout.Slider(startTime, 0, sourceClip.length);
        if (EditorGUI.EndChangeCheck())
        {
            startTime = Mathf.Min(newStartTime, endTime - 0.01f);
            RegenerateWaveform();
        }
        EditorGUILayout.LabelField(FormatTime(startTime), EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();

        // 结束时间
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width / 2 - 20));
        EditorGUILayout.LabelField("结束时间", EditorStyles.miniLabel);
        EditorGUI.BeginChangeCheck();
        float newEndTime = EditorGUILayout.Slider(endTime, 0, sourceClip.length);
        if (EditorGUI.EndChangeCheck())
        {
            endTime = Mathf.Max(newEndTime, startTime + 0.01f);
            RegenerateWaveform();
        }
        EditorGUILayout.LabelField(FormatTime(endTime), EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        // 剪辑时长显示
        EditorGUILayout.Space(5);
        float clipDuration = endTime - startTime;

        GUIStyle durationStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            normal = { textColor = new Color(0.3f, 0.8f, 0.3f) }
        };
        EditorGUILayout.LabelField($"剪辑时长: {FormatTime(clipDuration)}", durationStyle);

        // 快捷按钮
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("🔄 重置选区", GUILayout.Height(25)))
        {
            startTime = 0;
            endTime = sourceClip.length;
            RegenerateWaveform();
        }

        if (GUILayout.Button("⏮ 选前半", GUILayout.Height(25)))
        {
            endTime = sourceClip.length / 2;
            startTime = 0;
            RegenerateWaveform();
        }

        if (GUILayout.Button("⏭ 选后半", GUILayout.Height(25)))
        {
            startTime = sourceClip.length / 2;
            endTime = sourceClip.length;
            RegenerateWaveform();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void DrawVolumeControl()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("🔊 音量控制", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        // 音量图标
        string volumeIcon = volume > 0.7f ? "🔊" : (volume > 0.3f ? "🔉" : (volume > 0 ? "🔈" : "🔇"));
        GUIStyle iconStyle = new GUIStyle(EditorStyles.label) { fontSize = 20 };
        EditorGUILayout.LabelField(volumeIcon, iconStyle, GUILayout.Width(30));

        // 音量滑块
        EditorGUI.BeginChangeCheck();
        float newVolume = EditorGUILayout.Slider(volume, 0f, 1f);
        if (EditorGUI.EndChangeCheck())
        {
            volume = newVolume;
            // 实时更新播放音量
            if (audioPreviewSource != null)
            {
                audioPreviewSource.volume = volume;
            }
        }

        // 音量百分比
        EditorGUILayout.LabelField($"{(int)(volume * 100)}%", GUILayout.Width(45));

        EditorGUILayout.EndHorizontal();

        // 预设按钮
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("静音", GUILayout.Height(22))) SetVolume(0f);
        if (GUILayout.Button("25%", GUILayout.Height(22))) SetVolume(0.25f);
        if (GUILayout.Button("50%", GUILayout.Height(22))) SetVolume(0.5f);
        if (GUILayout.Button("75%", GUILayout.Height(22))) SetVolume(0.75f);
        if (GUILayout.Button("100%", GUILayout.Height(22))) SetVolume(1f);

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    private void SetVolume(float newVolume)
    {
        volume = newVolume;
        if (audioPreviewSource != null)
        {
            audioPreviewSource.volume = volume;
        }
    }

    private void DrawPlaybackControls()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("▶️ 播放控制", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 播放进度条
        float clipDuration = endTime - startTime;
        float progress = clipDuration > 0 ? currentPlaybackTime / clipDuration : 0;

        EditorGUI.ProgressBar(
            EditorGUILayout.GetControlRect(false, 20),
            progress,
            $"播放进度: {FormatTime(currentPlaybackTime)} / {FormatTime(clipDuration)}"
        );

        EditorGUILayout.Space(10);

        // 播放控制按钮
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        // 从头播放按钮
        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("⏮", GUILayout.Width(50), GUILayout.Height(40)))
        {
            currentPlaybackTime = 0f;
            if (isPlaying)
            {
                StopPreview();
                PlayPreview();
            }
        }

        // 播放/暂停按钮
        GUI.backgroundColor = isPlaying ? new Color(1f, 0.5f, 0.3f) : new Color(0.3f, 1f, 0.5f);
        string playButtonText = isPlaying ? "⏸ 暂停" : "▶ 播放";
        if (GUILayout.Button(playButtonText, GUILayout.Width(100), GUILayout.Height(40)))
        {
            if (isPlaying)
            {
                StopPreview();
            }
            else
            {
                PlayPreview();
            }
        }

        // 停止按钮
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("⏹ 停止", GUILayout.Width(80), GUILayout.Height(40)))
        {
            StopPreview();
            currentPlaybackTime = 0f;
        }

        GUI.backgroundColor = Color.white;

        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // 快捷键提示
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("快捷键: 空格 = 播放/暂停, S = 停止, Home = 从头开始", EditorStyles.centeredGreyMiniLabel);

        // 播放状态指示
        if (isPlaying)
        {
            EditorGUILayout.Space(3);
            GUIStyle playingStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                normal = { textColor = new Color(0.3f, 1f, 0.3f) }
            };
            EditorGUILayout.LabelField("● 正在播放...", playingStyle);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSaveSection()
    {
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("💾 保存剪辑", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 剪辑信息摘要
        float clipDuration = endTime - startTime;
        int sampleCount = Mathf.RoundToInt(clipDuration * sourceClip.frequency * sourceClip.channels);

        EditorGUILayout.BeginHorizontal();
        DrawInfoBox("剪辑时长", FormatTime(clipDuration));
        DrawInfoBox("预计采样数", sampleCount.ToString("N0"));
        DrawInfoBox("输出音量", $"{(int)(volume * 100)}%");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // 保存按钮
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.5f);
        if (GUILayout.Button("💾 保存剪辑 (覆盖原文件)", GUILayout.Height(35)))
        {
            SaveClip(false);
        }

        GUI.backgroundColor = new Color(0.4f, 0.6f, 1f);
        if (GUILayout.Button("📄 另存为新文件...", GUILayout.Height(35)))
        {
            SaveClip(true);
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // 警告提示
        if (!string.IsNullOrEmpty(lastSavePath))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox($"上次保存: {lastSavePath}", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawEmptyState()
    {
        EditorGUILayout.Space(50);

        GUIStyle centerStyle = new GUIStyle(EditorStyles.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 48
        };
        EditorGUILayout.LabelField("🎧", centerStyle, GUILayout.Height(60));

        GUIStyle hintStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            fontSize = 14
        };
        EditorGUILayout.LabelField("请拖入音频文件开始编辑", hintStyle);
        EditorGUILayout.Space(20);

        // 拖放区域
        Rect dropRect = GUILayoutUtility.GetRect(200, 100, GUILayout.ExpandWidth(true));

        GUIStyle boxStyle = new GUIStyle(EditorStyles.helpBox)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12
        };
        GUI.Box(dropRect, "将音频文件拖放到此处\n\n支持格式: WAV, MP3, OGG, AIFF", boxStyle);

        // 处理拖放
        HandleDragAndDrop(dropRect);
    }

    private void HandleDragAndDrop(Rect dropRect)
    {
        Event e = Event.current;

        if (dropRect.Contains(e.mousePosition))
        {
            if (e.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                e.Use();
            }
            else if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is AudioClip clip)
                    {
                        LoadAudioClip(clip);
                        break;
                    }
                }

                e.Use();
            }
        }
    }

    private void HandleKeyboardShortcuts()
    {
        Event e = Event.current;

        if (e.type != EventType.KeyDown || sourceClip == null)
            return;

        switch (e.keyCode)
        {
            case KeyCode.Space:
                if (isPlaying) StopPreview();
                else PlayPreview();
                e.Use();
                break;

            case KeyCode.S:
                StopPreview();
                currentPlaybackTime = 0f;
                e.Use();
                break;

            case KeyCode.Home:
                currentPlaybackTime = 0f;
                if (isPlaying)
                {
                    StopPreview();
                    PlayPreview();
                }
                e.Use();
                break;
        }
    }

    #endregion

    #region 音频处理

    private void LoadAudioClip(AudioClip clip)
    {
        StopPreview();
        CleanupTextures();

        sourceClip = clip;

        if (clip != null)
        {
            startTime = 0f;
            endTime = clip.length;
            currentPlaybackTime = 0f;

            // 获取音频采样数据
            audioSamples = new float[clip.samples * clip.channels];
            clip.GetData(audioSamples, 0);

            GenerateWaveformTexture();

            Debug.Log($"[AudioClipEditor] 已加载音频: {clip.name}, 时长: {clip.length:F2}秒");
        }
    }

    private void GenerateWaveformTexture()
    {
        if (sourceClip == null || audioSamples == null || audioSamples.Length == 0)
            return;

        // 清理旧纹理
        if (waveformTexture != null)
        {
            DestroyImmediate(waveformTexture);
        }

        int width = Mathf.Max(waveformWidth, 400);
        int height = waveformHeight;

        waveformTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        waveformTexture.filterMode = FilterMode.Bilinear;

        Color[] colors = new Color[width * height];

        // 填充背景
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.clear;
        }

        // 计算每像素对应的采样数
        int samplesPerPixel = Mathf.Max(1, audioSamples.Length / width);
        int channels = sourceClip.channels;

        // 绘制波形
        for (int x = 0; x < width; x++)
        {
            int startSample = x * samplesPerPixel;
            int endSample = Mathf.Min(startSample + samplesPerPixel, audioSamples.Length);

            float min = 0f;
            float max = 0f;

            for (int i = startSample; i < endSample; i++)
            {
                float sample = audioSamples[i];
                if (sample < min) min = sample;
                if (sample > max) max = sample;
            }

            // 转换为像素坐标
            int yMin = Mathf.Clamp((int)((min + 1f) * 0.5f * height), 0, height - 1);
            int yMax = Mathf.Clamp((int)((max + 1f) * 0.5f * height), 0, height - 1);

            // 确保至少绘制一个像素
            if (yMin == yMax)
            {
                yMax = Mathf.Min(yMax + 1, height - 1);
            }

            // 绘制垂直线
            for (int y = yMin; y <= yMax; y++)
            {
                // 渐变色：中心亮，边缘暗
                float centerDist = Mathf.Abs(y - height * 0.5f) / (height * 0.5f);
                Color color = Color.Lerp(waveformColor, waveformColor * 0.6f, centerDist);
                colors[y * width + x] = color;
            }

            // 绘制中心线
            int centerY = height / 2;
            colors[centerY * width + x] = new Color(0.4f, 0.4f, 0.4f, 0.5f);
        }

        waveformTexture.SetPixels(colors);
        waveformTexture.Apply();
    }

    private void RegenerateWaveform()
    {
        // 标记需要重新绘制
        Repaint();
    }

    private void PlayPreview()
    {
        if (sourceClip == null) return;

        StopPreview();

        // 创建剪辑后的预览音频
        CreatePreviewClip();

        if (previewClip == null) return;

        // 使用 AudioSource 方式播放（更可靠）
        if (useAudioSource)
        {
            EnsureAudioPreviewObject();

            if (audioPreviewSource != null)
            {
                audioPreviewSource.clip = previewClip;
                audioPreviewSource.volume = volume;
                audioPreviewSource.time = currentPlaybackTime;
                audioPreviewSource.Play();

                isPlaying = true;
                playStartTime = EditorApplication.timeSinceStartup;
                playStartPosition = currentPlaybackTime;

                Debug.Log($"[AudioClipEditor] 开始播放 (AudioSource), 起始位置: {FormatTime(currentPlaybackTime)}");
            }
        }
        else
        {
            // 备用：使用 AudioUtil 反射方式
            if (playClipMethod != null)
            {
                try
                {
                    int startSample = Mathf.RoundToInt(currentPlaybackTime * previewClip.frequency);
                    playClipMethod.Invoke(null, new object[] { previewClip, startSample, false });

                    isPlaying = true;
                    playStartTime = EditorApplication.timeSinceStartup;
                    playStartPosition = currentPlaybackTime;

                    Debug.Log($"[AudioClipEditor] 开始播放 (AudioUtil), 起始位置: {FormatTime(currentPlaybackTime)}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[AudioClipEditor] AudioUtil 播放失败: {e.Message}");
                    // 回退到 AudioSource 方式
                    useAudioSource = true;
                    PlayPreview();
                }
            }
        }
    }

    private void CreatePreviewClip()
    {
        if (sourceClip == null) return;

        // 清理旧的预览剪辑
        if (previewClip != null)
        {
            DestroyImmediate(previewClip);
        }

        int startSample = Mathf.RoundToInt(startTime * sourceClip.frequency);
        int endSample = Mathf.RoundToInt(endTime * sourceClip.frequency);
        int sampleCount = endSample - startSample;

        if (sampleCount <= 0) return;

        // 创建新的 AudioClip
        previewClip = AudioClip.Create(
            "PreviewClip",
            sampleCount,
            sourceClip.channels,
            sourceClip.frequency,
            false
        );

        // 复制采样数据
        float[] samples = new float[sampleCount * sourceClip.channels];
        sourceClip.GetData(samples, startSample);

        previewClip.SetData(samples, 0);
    }

    private void StopPreview()
    {
        // 停止 AudioSource
        if (audioPreviewSource != null)
        {
            audioPreviewSource.Stop();
        }

        // 停止 AudioUtil
        if (stopAllClipsMethod != null)
        {
            try
            {
                stopAllClipsMethod.Invoke(null, null);
            }
            catch { }
        }

        isPlaying = false;
    }

    private void SaveClip(bool saveAsNew)
    {
        if (sourceClip == null) return;

        string sourcePath = AssetDatabase.GetAssetPath(sourceClip);
        string savePath;

        if (saveAsNew)
        {
            string directory = string.IsNullOrEmpty(sourcePath)
                ? "Assets"
                : Path.GetDirectoryName(sourcePath);
            string defaultName = string.IsNullOrEmpty(sourcePath)
                ? "ClippedAudio.wav"
                : Path.GetFileNameWithoutExtension(sourcePath) + "_clipped.wav";

            savePath = EditorUtility.SaveFilePanel(
                "保存音频剪辑",
                directory,
                defaultName,
                "wav"
            );

            if (string.IsNullOrEmpty(savePath))
                return;
        }
        else
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                EditorUtility.DisplayDialog("错误", "无法确定原始文件路径，请使用另存为功能", "确定");
                return;
            }

            // 确认覆盖
            if (!EditorUtility.DisplayDialog(
                "确认覆盖",
                $"确定要覆盖原文件吗？\n{sourcePath}\n\n此操作不可撤销！",
                "确定",
                "取消"))
            {
                return;
            }

            savePath = Path.GetFullPath(sourcePath);
        }

        try
        {
            SaveWavFile(savePath);
            lastSavePath = savePath;

            // 刷新资源
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("成功", $"音频剪辑已保存到:\n{savePath}", "确定");

            Debug.Log($"[AudioClipEditor] 音频已保存: {savePath}");
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("保存失败", e.Message, "确定");
            Debug.LogError($"[AudioClipEditor] 保存失败: {e.Message}");
        }
    }

    private void SaveWavFile(string path)
    {
        int startSample = Mathf.RoundToInt(startTime * sourceClip.frequency);
        int endSample = Mathf.RoundToInt(endTime * sourceClip.frequency);
        int sampleCount = endSample - startSample;

        float[] samples = new float[sampleCount * sourceClip.channels];
        sourceClip.GetData(samples, startSample);

        // 应用音量
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] *= volume;
        }

        // 转换为 16 位 PCM
        short[] intData = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            intData[i] = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
        }

        // 写入 WAV 文件
        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            int channels = sourceClip.channels;
            int frequency = sourceClip.frequency;
            int byteRate = frequency * channels * 2;
            int blockAlign = channels * 2;
            int dataSize = intData.Length * 2;

            // RIFF header
            writer.Write(new char[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new char[] { 'W', 'A', 'V', 'E' });

            // fmt chunk
            writer.Write(new char[] { 'f', 'm', 't', ' ' });
            writer.Write(16); // chunk size
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)16); // bits per sample

            // data chunk
            writer.Write(new char[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);

            foreach (short sample in intData)
            {
                writer.Write(sample);
            }
        }
    }

    #endregion

    #region 辅助方法

    private string FormatTime(float seconds)
    {
        int minutes = (int)(seconds / 60);
        float secs = seconds % 60;
        return $"{minutes:00}:{secs:00.00}";
    }

    private void CleanupTextures()
    {
        if (waveformTexture != null)
        {
            DestroyImmediate(waveformTexture);
            waveformTexture = null;
        }

        if (previewClip != null)
        {
            DestroyImmediate(previewClip);
            previewClip = null;
        }
    }

    #endregion
}
#endif