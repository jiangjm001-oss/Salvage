# 设置功能配置指南

本指南将帮助您在Unity编辑器中配置设置功能。

## 已创建的脚本

1. **SettingsManager.cs** - 设置管理器（管理音乐和音效状态）
2. **SettingsUI.cs** - 设置界面UI
3. **SettingsButton.cs** - 设置按钮脚本
4. **AudioManager.cs** - 音频管理器（已更新）

## Unity编辑器配置步骤

### 第一步：在Bootstrap场景中添加SettingsManager

1. 打开 **Bootstrap** 场景
2. 找到或创建 **GameManagers** GameObject（存放所有持久化管理器）
3. 在 GameManagers 上添加 **SettingsManager** 组件
   - 右键点击 GameManagers → Add Component → Settings Manager

### 第二步：创建设置UI界面

#### 2.1 在每个场景创建设置UI面板

为 **LandingPage**、**Level1_Room** 和 **Level2_Room** 每个场景执行以下步骤：

1. 在Canvas下创建设置面板：
   - 右键点击 Canvas → UI → Panel
   - 命名为 **SettingsPanel**
   - 设置为全屏遮罩（可选添加半透明背景）

2. 在SettingsPanel下创建按钮容器：
   - 右键点击 SettingsPanel → UI → Vertical Layout Group（或手动放置）
   - 命名为 **ButtonContainer**

3. 创建以下按钮（在ButtonContainer下）：

   **a. 继续按钮**
   - 创建 UI → Button
   - 命名为 **ContinueButton**
   - 按钮文字：继续

   **b. 音乐开关按钮**
   - 创建 UI → Button
   - 命名为 **MusicToggleButton**
   - 按钮文字：音乐: 开

   **c. 音效开关按钮**
   - 创建 UI → Button
   - 命名为 **SFXToggleButton**
   - 按钮文字：音效: 开

   **d. 主菜单按钮**
   - 创建 UI → Button
   - 命名为 **MainMenuButton**
   - 按钮文字：主菜单

   **e. 说明按钮**
   - 创建 UI → Button
   - 命名为 **InstructionsButton**
   - 按钮文字：说明

#### 2.2 创建游戏说明弹窗

在SettingsPanel下创建说明弹窗：

1. 创建 UI → Panel
   - 命名为 **InstructionsPopup**
   - 设置位置在屏幕中央

2. 在InstructionsPopup下添加：
   - **标题**: UI → Text，内容：游戏说明
   - **内容**: UI → Text，添加游戏教学文字（多行）
     ```
     游戏说明：

     1. 点击物品可以拾取到背包
     2. 点击背包中的物品可以查看详情
     3. 拖拽背包物品可以交换位置
     4. 点击家具可以放大查看
     5. 使用箭头按钮切换不同墙面
     6. 解开谜题推进游戏进度
     ```
   - **关闭按钮**: UI → Button
     - 命名为 **CloseInstructionsButton**
     - 文字：关闭

#### 2.3 添加SettingsUI组件

1. 在Canvas上或创建一个新的GameObject（命名为SettingsUIManager）
2. 添加 **SettingsUI** 组件
3. 在Inspector中配置引用：
   - Settings Panel: 拖入 SettingsPanel
   - Continue Button: 拖入 ContinueButton
   - Music Toggle Button: 拖入 MusicToggleButton
   - SFX Toggle Button: 拖入 SFXToggleButton
   - Main Menu Button: 拖入 MainMenuButton
   - Instructions Button: 拖入 InstructionsButton
   - Music Button Text: 拖入 MusicToggleButton 下的 Text 组件
   - SFX Button Text: 拖入 SFXToggleButton 下的 Text 组件
   - Instructions Popup: 拖入 InstructionsPopup
   - Close Instructions Button: 拖入 CloseInstructionsButton

### 第三步：在三个场景添加设置图标按钮

为 **LandingPage**、**Level1_Room** 和 **Level2_Room** 每个场景执行以下步骤：

1. 在Canvas右上角创建设置图标按钮：
   - 右键点击 Canvas → UI → Button
   - 命名为 **SettingsIconButton**

2. 设置按钮位置：
   - RectTransform → Anchor: 右上角
   - Position: X=-50, Y=-50（根据实际调整）
   - Size: Width=60, Height=60

3. 设置按钮图标：
   - 可以使用齿轮图标（⚙️）或文字"设置"
   - 如果有设置图标图片，拖入Image组件

4. 添加 **SettingsButton** 组件：
   - 选中 SettingsIconButton
   - Add Component → Settings Button

### 第四步：测试功能

1. 运行游戏（Play模式）
2. 测试以下功能：
   - ✅ 点击设置图标打开设置面板
   - ✅ 点击"继续"关闭设置面板
   - ✅ 点击"音乐"切换音乐开关状态
   - ✅ 点击"音效"切换音效开关状态
   - ✅ 点击"主菜单"返回LandingPage
   - ✅ 点击"说明"打开游戏说明弹窗
   - ✅ 点击"关闭"按钮关闭说明弹窗

## 可选优化

### 添加设置图标
如果您想使用图标而不是文字：

1. 准备一个齿轮图标（PNG格式，透明背景）
2. 导入到 `Assets/Sprites/UI/` 文件夹
3. 设置图片类型为 Sprite (2D and UI)
4. 将图标拖到 SettingsIconButton 的 Image 组件

### 美化设置面板
可以添加以下元素：

- 背景图片（半透明黑色遮罩）
- 按钮悬停效果
- 过渡动画
- 音量滑块（未来扩展）

### 保存设置
设置会自动保存到 PlayerPrefs：
- `MusicEnabled`: 音乐开关状态
- `SFXEnabled`: 音效开关状态

## 场景结构示例

```
Canvas
├── SettingsPanel [默认隐藏]
│   ├── ButtonContainer
│   │   ├── ContinueButton
│   │   ├── MusicToggleButton
│   │   ├── SFXToggleButton
│   │   ├── MainMenuButton
│   │   └── InstructionsButton
│   └── InstructionsPopup [默认隐藏]
│       ├── Title (Text)
│       ├── Content (Text)
│       └── CloseInstructionsButton
├── SettingsIconButton (右上角)
└── SettingsUIManager [SettingsUI组件]
```

## 注意事项

1. **SettingsPanel 和 InstructionsPopup 默认应该是隐藏的**（SetActive = false）
2. **SettingsUI组件应该只有一个实例**（单例模式）
3. **确保所有按钮都正确引用**到SettingsUI组件
4. **如果某个场景没有设置按钮**，设置面板不会显示

## 问题排查

如果设置功能不工作，请检查：

1. Bootstrap场景中是否有SettingsManager
2. 每个场景中是否有SettingsUI组件
3. 所有按钮引用是否正确配置
4. Console中是否有错误日志
5. SettingsPanel和InstructionsPopup初始状态是否为隐藏

## 完成！

配置完成后，你的游戏将拥有完整的设置功能！
