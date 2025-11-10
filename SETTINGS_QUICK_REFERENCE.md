# 设置系统快速参考

## 🎯 已完成的工作

### 新建文件
1. ✅ `Assets/Scripts/UI/SettingsButton.cs` - 设置按钮组件
2. ✅ `Assets/Scripts/Editor/SettingsSetupHelper.cs` - Unity 编辑器辅助工具
3. ✅ `Assets/Resources/TutorialText.txt` - 游戏教学文本
4. ✅ `SETTINGS_SETUP_GUIDE.md` - 详细设置指南
5. ✅ `SETTINGS_QUICK_REFERENCE.md` - 本文档

### 更新文件
1. ✅ `Assets/Scripts/Managers/SettingsManager.cs` - 添加调试日志

## 📋 待在 Unity 编辑器中完成的操作

### 必须完成（核心功能）

#### 1. 在 _Managers_Prefab 中添加 SettingsManager
```
打开: Assets/Prefabs/_Managers_Prefab.prefab
操作: 添加 SettingsManager 组件到根对象
```

#### 2. 创建设置面板 UI
在 `_Managers_Prefab` 中创建：
- `SettingsCanvas` (Canvas)
  - `SettingsPanel` (Panel)
    - 按钮：继续、音乐、音效、主菜单、说明
  - `TutorialPanel` (Panel)
    - 说明文本和关闭按钮

#### 3. 连接 SettingsManager 引用
在 SettingsManager 组件中连接所有 UI 引用：
- Settings Panel
- Tutorial Panel
- 所有按钮 (Continue, Music, SFX, Main Menu, Tutorial, Close Tutorial)
- 文本组件 (Music Button Text, SFX Button Text, Tutorial Text)

#### 4. 在场景中添加设置按钮
在以下场景添加设置按钮（右上角）：
- `LandingPage.unity`
- `Level1_Room.unity`
- `Level2_Room.unity`

每个按钮都需要：
- Button 组件
- SettingsButton 脚本组件

## 🛠️ Unity 编辑器工具使用

### 打开设置辅助工具
```
菜单: Tools → Setup Settings System
```

这个工具提供：
- 快速打开相关场景
- 快速定位预制件
- 验证脚本存在性
- 打开设置指南文档

## 🎮 功能说明

### SettingsManager.cs
**功能**：
- 管理设置面板的显示/隐藏
- 处理所有设置按钮的点击事件
- 与 AudioManager 集成实现音乐/音效切换
- 与 SceneController 集成实现场景跳转
- 管理游戏说明弹窗

**关键方法**：
- `OpenSettings()` - 打开设置面板
- `CloseSettings()` - 关闭设置面板
- `SetTutorialText(string text)` - 设置教学文本

### SettingsButton.cs
**功能**：
- 附加到场景中的设置图标按钮
- 点击时调用 `SettingsManager.Instance.OpenSettings()`

**使用方法**：
1. 在场景中创建 Button
2. 添加 SettingsButton 组件
3. 脚本会自动绑定点击事件

## 📐 UI 布局建议

### 设置面板尺寸
- Canvas: Screen Space - Overlay
- SettingsPanel: 全屏 (Stretch)
- 主面板: 600 x 700 (居中)

### 按钮位置（从上到下）
```
标题 "设置"        (y: 280)
继续              (y: 180)
音乐: 开          (y: 80)
音效: 开          (y: -20)
主菜单            (y: -120)
说明              (y: -220)
```

### 设置按钮位置（场景中）
- 位置: 屏幕右上角
- Anchor: Top Right
- Position: (-30, -30)
- 尺寸: 80 x 80

## 🔍 测试检查清单

运行游戏后测试：

- [ ] 控制台显示 "[SettingsManager] Instance has been set."
- [ ] 点击设置按钮显示设置面板
- [ ] 点击"继续"关闭设置面板
- [ ] 点击"音乐"切换音乐开关，文字更新
- [ ] 点击"音效"切换音效开关，文字更新
- [ ] 点击"主菜单"返回 LandingPage
- [ ] 点击"说明"显示教学弹窗
- [ ] 点击"关闭"关闭教学弹窗
- [ ] 设置在场景切换后保持

## 🐛 常见问题排查

### 问题：点击设置按钮没反应
**检查**：
1. Console 是否有错误？
2. SettingsButton 脚本是否已添加？
3. SettingsManager.Instance 是否为 null？
4. _Managers_Prefab 是否在 Bootstrap 中实例化？

### 问题：按钮点击无效
**检查**：
1. SettingsManager 所有引用是否已连接？
2. Button 组件是否存在？
3. EventSystem 是否存在于场景中？

### 问题：音乐/音效切换无效
**检查**：
1. AudioManager.Instance 是否存在？
2. AudioManager 的 musicSource 和 sfxSource 是否已分配？
3. 音频文件是否正确导入？

## 📚 相关文件

### 脚本
- `Assets/Scripts/Managers/SettingsManager.cs` - 设置管理器
- `Assets/Scripts/Managers/AudioManager.cs` - 音频管理器
- `Assets/Scripts/Managers/SceneController.cs` - 场景控制器
- `Assets/Scripts/UI/SettingsButton.cs` - 设置按钮组件

### 资源
- `Assets/Resources/TutorialText.txt` - 教学文本
- `Assets/Prefabs/_Managers_Prefab.prefab` - 管理器预制件

### 文档
- `SETTINGS_SETUP_GUIDE.md` - 详细设置指南
- `SETTINGS_QUICK_REFERENCE.md` - 本快速参考

## 💡 提示

1. **优先完成 _Managers_Prefab 设置**
   - 这是核心，其他功能都依赖它

2. **使用编辑器工具**
   - Tools → Setup Settings System 可以加快设置速度

3. **参考现有 UI**
   - 可以参考 UIManager 中的 InventoryPanel 布局

4. **逐步测试**
   - 完成一个场景的设置按钮后先测试
   - 确认可用后再添加到其他场景

5. **备份场景**
   - 修改场景前建议先备份
   - 或者使用版本控制

---

如有疑问，请查看详细的 `SETTINGS_SETUP_GUIDE.md` 文档！
