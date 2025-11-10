# 设置系统集成指南

本指南将帮助你在 Unity 编辑器中完成设置系统的集成。

## 📋 前置条件

- ✅ `SettingsManager.cs` - 已存在
- ✅ `AudioManager.cs` - 已存在
- ✅ `SettingsButton.cs` - 已创建
- ✅ `SceneController.cs` - 已存在

## 🎯 实现步骤

### 第一步：在 _Managers_Prefab 中添加 SettingsManager

1. **打开预制件**
   - 在 Project 窗口中找到 `Assets/Prefabs/_Managers_Prefab.prefab`
   - 双击打开预制件编辑模式

2. **添加 SettingsManager 组件**
   - 选中 `_Managers_Prefab` 根对象
   - 在 Inspector 中点击 "Add Component"
   - 搜索并添加 `SettingsManager` 组件

3. **保存预制件**
   - 点击 Prefab 窗口的 "Save" 按钮
   - 退出预制件编辑模式

---

### 第二步：创建设置面板 UI

#### 2.1 创建设置面板结构

在 `_Managers_Prefab` 中创建以下 UI 层级结构：

```
_Managers_Prefab
└── SettingsCanvas (新建)
    ├── SettingsPanel (新建)
    │   ├── Background (Image - 半透明黑色背景)
    │   ├── Panel (Image - 白色/浅色面板)
    │   │   ├── Title (Text: "设置")
    │   │   ├── ContinueButton (Button: "继续")
    │   │   ├── MusicToggleButton (Button)
    │   │   │   └── Text (Text: "音乐: 开")
    │   │   ├── SFXToggleButton (Button)
    │   │   │   └── Text (Text: "音效: 开")
    │   │   ├── MainMenuButton (Button: "主菜单")
    │   │   └── TutorialButton (Button: "说明")
    │   └── TutorialPanel (新建)
    │       ├── Background (Image - 半透明背景)
    │       ├── Panel (Image)
    │       │   ├── Title (Text: "游戏说明")
    │       │   ├── TutorialText (Text - 多行)
    │       │   └── CloseButton (Button: "关闭")
```

#### 2.2 详细创建步骤

**A. 创建 SettingsCanvas**
1. 右键点击 `_Managers_Prefab` → UI → Canvas
2. 重命名为 `SettingsCanvas`
3. 设置 Canvas 组件：
   - Render Mode: `Screen Space - Overlay`
   - Pixel Perfect: ✓
   - Sort Order: `100` (确保显示在最上层)
4. 添加 `Canvas Scaler` 组件：
   - UI Scale Mode: `Scale With Screen Size`
   - Reference Resolution: `1920 x 1080`
   - Match: `0.5`

**B. 创建 SettingsPanel**
1. 右键 SettingsCanvas → UI → Panel
2. 重命名为 `SettingsPanel`
3. 设置 RectTransform:
   - Anchor: Stretch (覆盖全屏)
   - Left/Right/Top/Bottom: `0`
4. Image 组件：
   - Color: `黑色 (0, 0, 0, 180)` - 半透明背景

**C. 创建 Panel (主面板)**
1. 右键 SettingsPanel → UI → Image
2. 重命名为 `Panel`
3. RectTransform:
   - Anchor: Middle Center
   - Width: `600`
   - Height: `700`
   - Pos X: `0`, Pos Y: `0`
4. Image:
   - Color: `白色或浅灰色`
   - Sprite: 使用圆角矩形 sprite（可选）

**D. 添加按钮**

在 `Panel` 中创建以下按钮（右键 Panel → UI → Button）：

1. **Title (标题文本)**
   - GameObject Type: Text
   - 文本内容: `"设置"`
   - 字体大小: `48`
   - 对齐: Center
   - Position: 面板顶部

2. **ContinueButton**
   - 文本: `"继续"`
   - Position: 面板中上部
   - Size: Width `400`, Height `80`

3. **MusicToggleButton**
   - 包含子对象 Text
   - Text 初始内容: `"音乐: 开"`
   - Position: 面板中部偏上

4. **SFXToggleButton**
   - 包含子对象 Text
   - Text 初始内容: `"音效: 开"`
   - Position: MusicToggleButton 下方

5. **MainMenuButton**
   - 文本: `"主菜单"`
   - Position: SFXToggleButton 下方

6. **TutorialButton**
   - 文本: `"说明"`
   - Position: MainMenuButton 下方

**E. 创建 TutorialPanel (说明弹窗)**
1. 右键 SettingsPanel → UI → Panel
2. 重命名为 `TutorialPanel`
3. 设置为覆盖全屏（同 SettingsPanel）
4. 在其中创建：
   - Background (半透明黑色)
   - Panel (白色面板，包含标题、说明文本、关闭按钮)
   - TutorialText (Text 组件，支持多行)
   - CloseButton (关闭按钮)

#### 2.3 连接 SettingsManager 组件

1. 选中 `_Managers_Prefab` 根对象
2. 在 Inspector 中找到 `SettingsManager` 组件
3. 拖拽连接以下引用：
   - **Settings Panel**: 拖入 `SettingsPanel` GameObject
   - **Tutorial Panel**: 拖入 `TutorialPanel` GameObject
   - **Continue Button**: 拖入 `ContinueButton` 的 Button 组件
   - **Music Toggle Button**: 拖入 `MusicToggleButton` 的 Button 组件
   - **Sfx Toggle Button**: 拖入 `SFXToggleButton` 的 Button 组件
   - **Main Menu Button**: 拖入 `MainMenuButton` 的 Button 组件
   - **Tutorial Button**: 拖入 `TutorialButton` 的 Button 组件
   - **Close Tutorial Button**: 拖入 `CloseButton` 的 Button 组件
   - **Music Button Text**: 拖入 MusicToggleButton 的子 Text 组件
   - **Sfx Button Text**: 拖入 SFXToggleButton 的子 Text 组件
   - **Tutorial Text**: 拖入 TutorialPanel 中的 TutorialText 组件

4. **设置教学文本内容**（可选）
   - 选中 TutorialText
   - 在 Text 组件中输入游戏说明：
   ```
   游戏说明

   1. 点击屏幕探索场景
   2. 收集物品并放入背包
   3. 使用物品解开谜题
   4. 左右箭头切换视角
   5. 点击物品进入放大视图

   祝你游戏愉快！
   ```

5. **初始化面板状态**
   - 确保 `SettingsPanel` 在 Inspector 中未勾选 Active（默认隐藏）
   - 确保 `TutorialPanel` 也未勾选 Active（默认隐藏）

---

### 第三步：在场景中添加设置图标按钮

对以下场景重复此步骤：
- `LandingPage.unity`
- `Level1_Room.unity`
- `Level2_Room.unity`

#### 3.1 创建设置按钮

1. **打开场景**
   - 双击打开场景文件

2. **找到或创建 UI Canvas**
   - 如果场景中已有 Canvas，使用现有的
   - 如果没有，创建新的：右键 Hierarchy → UI → Canvas

3. **创建设置按钮**
   - 右键 Canvas → UI → Button
   - 重命名为 `SettingsButton`

4. **设置按钮位置**（右上角）
   - RectTransform:
     - Anchor: Top Right
     - Pivot: (1, 1)
     - Pos X: `-30`
     - Pos Y: `-30`
     - Width: `80`
     - Height: `80`

5. **设置按钮外观**
   - 删除默认的 Text 子对象（或修改为设置图标）
   - 可选：添加一个设置齿轮图标
     - 右键 SettingsButton → UI → Image
     - 重命名为 `Icon`
     - 分配设置图标 Sprite
     - 设置为填充整个按钮

6. **添加 SettingsButton 脚本**
   - 选中 SettingsButton GameObject
   - 在 Inspector 中点击 "Add Component"
   - 搜索并添加 `SettingsButton` 脚本

7. **保存场景**
   - Ctrl+S 或 File → Save

---

### 第四步：创建设置图标（可选但推荐）

如果你想使用图标而不是文字：

1. **准备图标资源**
   - 在 `Assets/Sprites/UI/` 文件夹中添加设置齿轮图标（PNG）
   - 导入设置：
     - Texture Type: `Sprite (2D and UI)`
     - Sprite Mode: `Single`
     - Pixels Per Unit: `100`

2. **应用图标**
   - 在各场景的 SettingsButton 中
   - 选中 Icon Image 组件
   - 拖入设置图标 Sprite

---

### 第五步：测试设置功能

1. **运行 Bootstrap 场景**
   - 打开 `Bootstrap.unity`
   - 点击 Play

2. **测试功能清单**：
   - [ ] SettingsManager 是否成功初始化？（查看 Console）
   - [ ] 在 LandingPage 中点击设置按钮，是否显示设置面板？
   - [ ] 点击"继续"按钮，设置面板是否关闭？
   - [ ] 点击"音乐"按钮，音乐是否切换开/关，按钮文字是否更新？
   - [ ] 点击"音效"按钮，音效是否切换开/关，按钮文字是否更新？
   - [ ] 点击"主菜单"按钮，是否返回 LandingPage？
   - [ ] 点击"说明"按钮，是否显示教学弹窗？
   - [ ] 在教学弹窗中点击"关闭"，弹窗是否关闭？
   - [ ] 设置是否在场景切换后保持（进入 Level1 后测试）？

3. **调试**
   - 检查 Console 中的日志输出
   - 如果有 null reference 错误，检查 SettingsManager 的引用是否都正确连接

---

## 🎨 UI 样式建议

### 颜色方案
- **背景遮罩**: RGBA(0, 0, 0, 180) - 半透明黑色
- **面板**: RGBA(240, 240, 240, 255) - 浅灰色
- **按钮**:
  - Normal: RGBA(200, 200, 200, 255)
  - Highlighted: RGBA(220, 220, 220, 255)
  - Pressed: RGBA(180, 180, 180, 255)
- **文字**: RGBA(50, 50, 50, 255) - 深灰色

### 字体大小
- 标题: `48`
- 按钮文字: `32`
- 说明文字: `24`

### 布局建议
```
Panel (600 x 700)
├── Title (y: 280)
├── ContinueButton (y: 180)
├── MusicToggleButton (y: 80)
├── SFXToggleButton (y: -20)
├── MainMenuButton (y: -120)
└── TutorialButton (y: -220)
```

---

## ⚠️ 常见问题

### Q: 点击设置按钮没有反应
**A**: 检查以下几点：
1. `_Managers_Prefab` 是否已在 Bootstrap 场景中实例化？
2. SettingsManager 组件是否已添加到 `_Managers_Prefab`？
3. SettingsButton 脚本是否已添加到按钮上？
4. Console 中是否有错误信息？

### Q: 设置面板显示但按钮不工作
**A**:
1. 检查 SettingsManager 组件中所有按钮引用是否正确连接
2. 查看 Console 日志，确认按钮点击事件是否被触发

### Q: 音乐/音效切换不工作
**A**:
1. 确认 AudioManager 在 `_Managers_Prefab` 中存在
2. 检查 AudioManager 的 musicSource 和 sfxSource 是否已分配
3. 确认音频文件已正确导入

### Q: 主菜单按钮点击后没有切换场景
**A**:
1. 确认 SceneController 在 `_Managers_Prefab` 中存在
2. 检查 Build Settings 中是否包含 LandingPage 场景
3. 确认场景名称拼写正确

---

## 📝 代码架构说明

### 组件关系
```
_Managers_Prefab (DontDestroyOnLoad)
├── GameManager (单例)
├── AudioManager (单例)
├── UIManager (单例)
├── SettingsManager (单例) ← 新添加
└── SceneController (单例)
```

### 调用流程
```
场景中的 SettingsButton
    ↓ (点击)
SettingsButton.OnSettingsButtonClicked()
    ↓
SettingsManager.Instance.OpenSettings()
    ↓
显示 SettingsPanel
    ↓ (用户点击按钮)
SettingsManager 的按钮回调
    ↓
AudioManager.ToggleMusic() / SceneController.LoadScene() 等
```

---

## ✅ 完成检查清单

- [ ] SettingsManager 组件已添加到 _Managers_Prefab
- [ ] SettingsPanel UI 已创建并正确配置
- [ ] TutorialPanel UI 已创建并正确配置
- [ ] SettingsManager 所有引用已正确连接
- [ ] SettingsButton 已添加到 LandingPage 场景
- [ ] SettingsButton 已添加到 Level1_Room 场景
- [ ] SettingsButton 已添加到 Level2_Room 场景
- [ ] 所有功能已测试通过

---

祝你集成顺利！如有问题，请查看 Console 日志进行调试。
