# Salvage 项目架构文档

> **最后更新：** 2025-11-06
> **版本：** v1.0
> **状态：** 核心架构已完成，进入内容开发阶段

---

## 📋 目录

- [项目概述](#项目概述)
- [技术栈](#技术栈)
- [核心架构](#核心架构)
  - [启动流程](#启动流程)
  - [Manager 系统](#manager-系统)
  - [场景系统](#场景系统)
  - [UI 系统](#ui-系统)
  - [交互系统](#交互系统)
  - [视图系统](#视图系统)
- [已完成功能](#已完成功能)
- [待开发功能](#待开发功能)
- [关键设计决策](#关键设计决策)
- [常见问题与解决方案](#常见问题与解决方案)

---

## 项目概述

**Salvage** 是一个 2D 密室逃脱解谜游戏，玩家需要在不同房间中探索、收集物品、解开谜题以推进游戏进程。

**核心玩法：**
- 🔍 点击交互探索场景
- 🎒 收集和管理物品（12格背包系统）
- 🧩 解开谜题推进剧情
- 🚪 在多个房间间移动
- 💾 存档/读档系统

---

## 技术栈

- **引擎：** Unity 2D
- **语言：** C#
- **UI 系统：** Unity UI (Canvas + EventSystem)
- **场景管理：** 异步场景加载
- **数据持久化：** JSON 存档系统
- **音频：** Unity AudioSource

---

## 核心架构

### 启动流程

```
Bootstrap Scene (入口场景)
    ↓
BootstrapLoader (实例化管理器)
    ↓
_Managers_Prefab (DontDestroyOnLoad)
    ├── GameManager
    ├── UIManager (包含 UICanvas)
    ├── SceneController
    ├── InventorySystem
    ├── SaveLoadSystem
    ├── AudioManager
    └── EventSystem + StandaloneInputModule
    ↓
BootstrapInitializer (延迟0.5s)
    ↓
LandingPage Scene (主菜单)
    ↓
Level1_Room / Level2_Room (游戏场景)
```

**关键文件：**
- `Assets/Scenes/Bootstrap.unity`
- `Assets/Scripts/Bootstrap/BootstrapLoader.cs`
- `Assets/Scripts/Bootstrap/BootstrapInitializer.cs`
- `Assets/Prefabs/_Managers_Prefab.prefab`

---

### Manager 系统

所有 Manager 都是单例模式，附加在 `_Managers_Prefab` 上，通过 `DontDestroyOnLoad` 持久化。

#### 1. GameManager
**职责：** 游戏状态与视图状态管理

**文件：** `Assets/Scripts/Managers/GameManager.cs`

**游戏状态（GameState）：**
```csharp
public enum GameState {
    MainMenu,   // 主菜单
    Level1,     // 第一关
    Level2,     // 第二关
    Paused,     // 暂停
    Ending      // 结局
}
```

**视图状态（ViewState）：**
```csharp
public enum ViewState {
    // 四面墙视图
    Wall_A, Wall_B, Wall_C, Wall_D,

    // Level 1 放大视图
    Level1_Zoom_Mirror,
    Level1_Zoom_LowCabinet,
    Level1_Zoom_GrandfatherClock,
    Level1_Zoom_CoalHeater,

    // Level 2 放大视图
    Level2_Zoom_Mirror,
    Level2_Zoom_Painting,
    Level2_Zoom_Safe,
}
```

**核心方法：**
- `ChangeGameState(GameState)` - 切换游戏状态
- `SwitchToView(ViewState)` - 切换视图状态
- `RegisterWallManager(WallManager)` - 注册场景墙壁管理器
- `RegisterZoomController(FurnitureZoomController)` - 注册放大视图控制器
- `StartNewGame()` - 开始新游戏
- `ContinueGame()` - 继续游戏
- `QuitGame()` - 退出游戏

**事件：**
- `OnGameStateChanged` - 游戏状态改变事件
- `OnViewStateChanged` - 视图状态改变事件

---

#### 2. UIManager
**职责：** UI 显示控制、背包UI管理

**文件：** `Assets/Scripts/Managers/UIManager.cs`

**管理的UI元素：**
- InventoryPanel（背包面板，6个槽位）
- SecondColumnPanel（扩展面板，6个槽位）
- PauseMenuPanel（暂停菜单，未实现）

**核心方法：**
- `ShowInventoryUI()` - 显示背包
- `HideInventoryUI()` - 隐藏背包
- `UpdateInventoryUI()` - 更新背包显示
- `ToggleInventoryExpansion()` - 展开/收起扩展背包

**背包扩展动画：**
- 使用协程实现平滑滑动动画
- 可配置动画时长和缓动曲线
- 按钮文字自动切换（"<" ↔ ">"）

**重要：** UICanvas 必须保持激活状态（`m_IsActive: 1`），否则背包不会显示！

---

#### 3. SceneController
**职责：** 场景加载与切换

**文件：** `Assets/Scripts/Managers/SceneController.cs`

**核心方法：**
- `LoadScene(string sceneName)` - 异步加载场景

**加载流程：**
```csharp
LoadScene("Level1_Room")
    ↓
清理旧场景管理器引用
    ↓
异步加载新场景
    ↓
新场景的 WallManager/FurnitureZoomController 自动注册
    ↓
显示/隐藏背包UI（根据场景类型）
    ↓
更新 GameState
    ↓
重置视图到 Wall_A（如果是关卡场景）
```

---

#### 4. InventorySystem
**职责：** 背包数据管理

**文件：** `Assets/Scripts/Inventory/InventorySystem.cs`

**配置：**
- 背包大小：12个槽位（前6个 + 扩展6个）
- 支持物品拾取、交换、使用

**核心方法：**
- `AddItem(Item item)` - 添加物品
- `RemoveItem(int slotIndex)` - 移除物品
- `SwapItems(int index1, int index2)` - 交换物品位置
- `GetSlots()` - 获取所有槽位

**事件：**
- `OnInventoryChanged` - 背包改变事件（UI会监听此事件更新显示）

**数据结构：**
```csharp
public class InventorySlot {
    public Item item;        // 物品数据
    public bool IsEmpty => item == null;
}
```

---

#### 5. SaveLoadSystem
**职责：** 存档/读档管理

**文件：** `Assets/Scripts/SaveLoad/SaveLoadSystem.cs`

**存档数据：**
```csharp
public class SaveData {
    public string currentSceneName;           // 当前场景
    public int currentViewState;              // 当前视图状态
    public List<string> inventoryItemIDs;     // 背包物品ID列表
    public List<string> collectedObjectIDs;   // 已收集物品ID
    public List<string> triggeredObjectIDs;   // 已触发物品ID
    public string saveTime;                   // 存档时间
}
```

**存档位置：** `Application.persistentDataPath + "/savegame.json"`

**核心方法：**
- `SaveGame()` - 保存游戏
- `LoadGame()` - 加载游戏
- `DeleteSaveData()` - 删除存档

---

#### 6. AudioManager
**职责：** 音效与背景音乐管理

**文件：** `Assets/Scripts/Managers/AudioManager.cs`

**核心方法：**
- `PlaySound(string soundName)` - 播放音效
- `PlayMusic(string musicName)` - 播放背景音乐
- `StopMusic()` - 停止背景音乐

---

#### 7. EventSystem
**职责：** UI 事件处理

**重要：**
- ✅ 已在 `_Managers_Prefab` 中添加 `EventSystem` 组件
- ✅ 已添加 `StandaloneInputModule` 组件
- 这两个组件**必须同时存在**才能正常处理UI点击事件！

---

### 场景系统

#### 场景列表

| 场景名称 | 类型 | 状态 | 说明 |
|---------|------|------|------|
| Bootstrap | 启动场景 | ✅ 已完成 | 游戏入口，负责初始化管理器 |
| LandingPage | 主菜单 | ✅ 已完成 | 开始游戏、继续游戏、退出 |
| Level1_Room | 关卡场景 | ✅ 架构完成 | 第一关，需要添加内容 |
| Level2_Room | 关卡场景 | ⏳ 待开发 | 第二关 |
| EndingScene | 结局场景 | ⏳ 待开发 | 游戏结局 |

---

#### LandingPage 场景

**文件：** `Assets/Scenes/LandingPage.unity`

**组成：**
- Canvas（场景内UI）
  - MainMenuPanel
    - Button New Game（开始新游戏）
    - Button Continue Game（继续游戏）
    - Button Exit Game（退出游戏）
    - Button Setting（设置，未实现）
- _LandingPageUI（脚本对象）

**脚本：** `Assets/Scripts/SceneSpecific/LandingPageUI.cs`

**按钮绑定逻辑：**
```csharp
// 使用协程延迟初始化，确保管理器已准备就绪
private IEnumerator InitializeButtonsCoroutine() {
    yield return null;

    // 检查管理器是否存在
    if (GameManager.Instance == null || UIManager.Instance == null) {
        Debug.LogError("Manager instances missing!");
        yield break;
    }

    // 绑定按钮事件
    startNewGameButton.onClick.AddListener(() => {
        GameManager.Instance.StartNewGame();
    });
    // ...其他按钮
}
```

---

#### Level1_Room 场景

**文件：** `Assets/Scenes/Level1_Room.unity`

**场景结构：**
```
Level1_Room
├── Main Camera
├── _InteractionSystem (DontDestroyOnLoad)
├── _SceneManagers
│   ├── WallManager (切换墙壁)
│   └── FurnitureZoomController (管理放大视图)
├── WallSystem
│   ├── Wall_A
│   ├── Wall_B
│   ├── Wall_C
│   └── Wall_D
├── FurnitureZoomViews
│   ├── Mirror_ZoomView
│   ├── LowCabinet_ZoomView
│   ├── GrandfatherClock_ZoomView
│   └── CoalHeater_ZoomView
└── test_interactable_obj (测试用可交互物体)
```

**场景管理器：**

1. **WallManager**
   - **文件：** `Assets/Scripts/SceneSpecific/WallManager.cs`
   - **职责：** 控制四面墙的显示/隐藏
   - **在 Awake 时自动注册到 GameManager**

2. **FurnitureZoomController**
   - **文件：** `Assets/Scripts/Managers/FurnitureZoomController.cs`
   - **职责：** 管理家具放大视图
   - **配置（Inspector）：**
     ```
     Zoom Views:
     - viewState: 4 (Level1_Zoom_Mirror) → Mirror_ZoomView
     - viewState: 5 (Level1_Zoom_LowCabinet) → LowCabinet_ZoomView
     - viewState: 6 (Level1_Zoom_GrandfatherClock) → GrandfatherClock_ZoomView
     - viewState: 7 (Level1_Zoom_CoalHeater) → CoalHeater_ZoomView
     ```
   - **在 Awake 时自动注册到 GameManager**

---

### UI 系统

#### UICanvas 层级结构

```
_Managers_Prefab
└── UICanvas (Canvas, m_IsActive: 1) ⚠️ 必须激活！
    ├── InventoryPanel (右侧背包面板)
    │   ├── SlotContainer (GridLayoutGroup, 6槽位)
    │   └── ExpandButton (展开按钮 "<")
    └── SecondColumnPanel (扩展背包面板)
        └── SecondSlotContainer (GridLayoutGroup, 6槽位)
```

**重要配置：**
- **InventoryPanel** 初始位置：`anchoredPosition = (0, 0)`
- **SecondColumnPanel** 初始位置：`anchoredPosition = (200, 0)` （屏幕外右侧）
- **展开动画：** 平滑滑动，InventoryPanel 向左移，SecondColumnPanel 滑入

**背包槽位 Prefab：**
- **路径：** `Resources/Prefabs/UI/ItemSlot`
- **结构：**
  ```
  ItemSlot (Button)
  └── ItemIcon (Image)
  ```

---

### 交互系统

#### InteractionSystem
**文件：** `Assets/Scripts/Player/InteractionSystem.cs`

**职责：**
- 检测鼠标点击
- 发射射线检测可交互物体
- 调用 `InteractableObject.Interact()`

**关键逻辑：**
```csharp
private void Update() {
    if (Input.GetMouseButtonDown(0)) {
        PerformInteractionCheck();
    }
}

private void PerformInteractionCheck() {
    // 检查是否点击在UI上
    if (IsPointerOverUI()) {
        return; // 忽略场景交互
    }

    // 射线检测
    Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
    RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero, Mathf.Infinity, interactableLayer);

    if (hit.collider != null) {
        InteractableObject interactable = hit.collider.GetComponent<InteractableObject>();
        if (interactable != null) {
            interactable.Interact();
        }
    }
}
```

**配置：**
- **Interactable Layer：** Layer 3
- **所有可交互物体必须：**
  1. Layer 设置为 3 (Interactable)
  2. 添加 Collider2D（IsTrigger = true）
  3. 添加 InteractableObject 组件

---

#### InteractableObject
**文件：** `Assets/Scripts/Interactable/InteractableObject.cs`

**交互类型：**
```csharp
public enum InteractionType {
    PickUp,        // 拾取物品
    ZoomView,      // 进入放大视图
    TriggerEvent,  // 触发事件
    UseItem        // 使用物品
}
```

**重要属性：**
```csharp
[Header("基础信息")]
public string objectID;              // 唯一ID
public string displayName;           // 显示名称
public InteractionType interactionType;

[Header("拾取相关")]
public Item item;                    // 物品数据
public bool isPickupable = true;

[Header("放大视图相关")]
public GameManager.ViewState associatedZoomView;

[Header("音效")]
public string pickupSoundName;
public string zoomSoundName;
public string triggerSoundName;

[Header("触发后行为")]
public bool disableAfterTrigger = false;
```

**Interact() 方法流程：**
```csharp
public void Interact() {
    switch (interactionType) {
        case InteractionType.PickUp:
            // 1. 添加物品到背包
            // 2. 播放拾取音效
            // 3. 销毁物体或禁用
            // 4. 记录到 collectedObjectIDs
            break;

        case InteractionType.ZoomView:
            // 1. 切换到放大视图
            // 2. 播放放大音效
            break;

        case InteractionType.TriggerEvent:
            // 1. 播放触发音效
            // 2. 触发自定义事件
            // 3. 记录到 triggeredObjectIDs
            break;
    }
}
```

---

### 视图系统

#### 视图切换流程

```
用户点击墙壁切换按钮
    ↓
WallManager.SwitchToWall(ViewState)
    ↓
GameManager.SwitchToView(ViewState)
    ↓
触发 OnViewStateChanged 事件
    ↓
    ├── WallManager 监听 → 显示/隐藏墙壁
    ├── FurnitureZoomController 监听 → 显示/隐藏放大视图
    └── UIManager 监听 → 显示/隐藏背包
```

#### 视图显示规则

**背包显示规则（在 UIManager 中）：**
```csharp
private void OnViewStateChanged(GameManager.ViewState newState) {
    bool isInGameplayView = newState == ViewState.Wall_A ||
                            newState == ViewState.Wall_B ||
                            newState == ViewState.Wall_C ||
                            newState == ViewState.Wall_D;

    if (isInGameplayView) {
        InventoryPanel.SetActive(true);
        SecondColumnPanel.SetActive(true);
    } else {
        InventoryPanel.SetActive(false);
        SecondColumnPanel.SetActive(false);
    }
}
```

**放大视图显示规则（在 FurnitureZoomController 中）：**
```csharp
private void OnViewStateChanged(GameManager.ViewState newState) {
    // 隐藏所有放大视图
    HideAllZoomViews();

    // 显示匹配的放大视图
    var activeView = zoomViews.Find(m => m.viewState == newState);
    if (activeView != null && activeView.zoomViewObject != null) {
        activeView.zoomViewObject.SetActive(true);
    }
}
```

---

## 已完成功能

### ✅ 核心架构
- [x] Bootstrap 启动系统
- [x] Manager 单例系统（DontDestroyOnLoad）
- [x] 场景加载系统
- [x] 游戏状态管理
- [x] 视图状态管理

### ✅ UI 系统
- [x] UICanvas 设置
- [x] EventSystem + StandaloneInputModule（修复按钮点击）
- [x] 背包系统（12格，可展开/收起）
- [x] 主菜单UI
- [x] 按钮点击事件绑定

### ✅ 交互系统
- [x] InteractionSystem（鼠标点击检测）
- [x] InteractableObject（可交互物体基类）
- [x] 拾取物品功能
- [x] 放大视图切换

### ✅ 场景
- [x] Bootstrap 场景
- [x] LandingPage 场景
- [x] Level1_Room 场景架构
  - [x] 四面墙系统
  - [x] 放大视图系统
  - [x] FurnitureZoomController 配置（修复）

### ✅ 存档系统
- [x] SaveLoadSystem 基础架构
- [x] JSON 序列化/反序列化
- [x] 存档数据结构定义

### ✅ 已修复的问题
- [x] **按钮点击无反应** - 添加 EventSystem 组件
- [x] **FurnitureZoomController 报错** - 修复 zoom view 配置
- [x] **背包不显示** - 激活 UICanvas

---

## 待开发功能

### 🔨 Level1_Room 内容开发（优先级：高）

#### 1. 可交互物体设计
需要在 Level1_Room 中添加实际的游戏内容：

**拾取物品清单：**
- [ ] 钥匙（开锁）
- [ ] 手电筒（照亮暗处）
- [ ] 笔记/日记（提供线索）
- [ ] 工具（螺丝刀、撬棍等）
- [ ] 谜题组件（拼图碎片、密码纸条等）

**放大视图内容：**
- [ ] **Mirror_ZoomView**
  - 添加背景图
  - 添加可交互细节（裂纹、隐藏物品等）
- [ ] **LowCabinet_ZoomView**
  - 抽屉系统（可打开/关闭）
  - 柜子内物品
  - 锁定机制（需要钥匙）
- [ ] **GrandfatherClock_ZoomView**
  - 时钟谜题（调整时间）
  - 隐藏密码
- [ ] **CoalHeater_ZoomView**
  - 炉火状态（点燃/熄灭）
  - 烧过的纸条（线索）

**物品数据创建：**
```
1. 创建 ScriptableObject: Assets/Resources/Items/
   - RustyKey.asset (生锈的钥匙)
   - Flashlight.asset (手电筒)
   - OldNote.asset (旧笔记)
   等等...

2. 配置物品属性：
   - icon (图标精灵)
   - itemName (物品名称)
   - description (描述)
   - isUsable (是否可使用)
   - usageEffect (使用效果)
```

**推荐开发顺序：**
1. 先实现简单的拾取物品（钥匙、笔记）
2. 测试背包系统是否正常工作
3. 实现第一个放大视图（推荐从 LowCabinet 开始）
4. 添加简单的锁/钥匙机制
5. 逐步添加其他放大视图和谜题

---

#### 2. 物品使用系统
当前只有拾取功能，需要实现使用功能：

**待实现：**
- [ ] **物品使用接口**
  ```csharp
  public abstract class UsableItem : Item {
      public abstract void Use(InteractableObject target);
  }
  ```

- [ ] **使用场景示例：**
  - 钥匙 → 开锁
  - 手电筒 → 照亮
  - 工具 → 修理/撬开
  - 拼图 → 组合

- [ ] **UI 交互流程：**
  1. 在背包中选中物品
  2. 点击场景中的目标
  3. 检查物品是否可用于该目标
  4. 执行使用效果

---

#### 3. 谜题系统设计
需要设计一个通用的谜题框架：

**谜题类型建议：**
- [ ] **密码锁谜题**
  - 4位数字密码
  - 提示来自场景中的线索

- [ ] **组合谜题**
  - 收集多个物品组合成新物品

- [ ] **序列谜题**
  - 按特定顺序操作物体

- [ ] **观察谜题**
  - 放大视图中寻找隐藏信息

**谜题系统架构：**
```csharp
// 基类
public abstract class Puzzle : MonoBehaviour {
    public string puzzleID;
    public bool isSolved = false;

    public abstract void AttemptSolve(object input);
    public abstract void OnSolved();
}

// 示例：密码锁谜题
public class CodeLockPuzzle : Puzzle {
    public string correctCode = "1234";

    public override void AttemptSolve(object input) {
        string code = input as string;
        if (code == correctCode) {
            isSolved = true;
            OnSolved();
        }
    }

    public override void OnSolved() {
        // 解锁抽屉、播放音效等
    }
}
```

---

### 🔨 Level2_Room 开发（优先级：中）

完全参照 Level1_Room 的架构创建：

**步骤：**
1. [ ] 复制 Level1_Room 场景 → 重命名为 Level2_Room
2. [ ] 创建 WallManager（或复用）
3. [ ] 创建 FurnitureZoomController
4. [ ] 配置放大视图：
   ```
   - viewState: 8 (Level2_Zoom_Mirror) → Mirror_ZoomView
   - viewState: 9 (Level2_Zoom_Painting) → Painting_ZoomView
   - viewState: 10 (Level2_Zoom_Safe) → Safe_ZoomView
   ```
5. [ ] 设计新的谜题和物品
6. [ ] 添加场景切换机制（Level1 → Level2）

---

### 🔨 存档/读档完善（优先级：中）

当前存档系统只有基础架构，需要完善：

**待实现：**
- [ ] **存档 UI 界面**
  - 存档槽位选择
  - 显示存档时间/场景信息
  - 覆盖存档确认

- [ ] **读档功能测试**
  - 测试背包物品恢复
  - 测试场景状态恢复
  - 测试视图状态恢复

- [ ] **自动存档**
  - 场景切换时自动存档
  - 重要事件后自动存档

---

### 🔨 音效系统（优先级：中）

AudioManager 已存在但未实际使用：

**待实现：**
- [ ] 准备音效资源
  - item_pickup.mp3（拾取音效）
  - zoom_in.mp3（放大音效）
  - trigger.mp3（触发音效）
  - button_click.mp3（按钮点击）
  - door_open.mp3（开门）
  - lock_unlock.mp3（解锁）

- [ ] 在 InteractableObject 中集成音效播放
- [ ] 添加背景音乐（循环播放）
- [ ] 音量控制设置

---

### 🔨 结局场景（优先级：低）

**待实现：**
- [ ] 创建 EndingScene 场景
- [ ] 结局动画/文字
- [ ] 返回主菜单按钮
- [ ] 成就统计（可选）

---

### 🔨 功能增强（优先级：低）

**可选功能：**
- [ ] **暂停菜单**
  - 继续游戏
  - 保存游戏
  - 返回主菜单
  - 设置选项

- [ ] **提示系统**
  - 玩家可请求提示
  - 提示消耗代价（限制次数）

- [ ] **成就系统**
  - 完成特定谜题
  - 收集所有物品
  - 无提示通关

- [ ] **多语言支持**
  - 中文/英文切换

---

## 关键设计决策

### 1. 为什么使用 DontDestroyOnLoad？
**优点：**
- 管理器在场景切换时不会被销毁
- 游戏状态持久化
- 避免重复初始化

**注意事项：**
- 必须实现单例模式防止重复
- 场景特定的管理器（如 WallManager）不应使用 DontDestroyOnLoad

---

### 2. 为什么分离场景管理器和全局管理器？
**设计原则：**
- **全局管理器（_Managers_Prefab）：** 跨场景共享的系统（UI、存档、音频等）
- **场景管理器（_SceneManagers）：** 场景特定逻辑（墙壁切换、放大视图等）

**好处：**
- 清晰的职责分离
- 避免耦合
- 便于扩展新场景

---

### 3. 为什么使用 ViewState 枚举而不是字符串？
**优点：**
- 编译时类型检查
- 自动补全
- 避免拼写错误
- 性能更好（整数比较）

---

### 4. 为什么背包槽位用 Prefab 而不是手动布局？
**优点：**
- 动态生成，易于扩展
- 统一样式
- 便于更新（修改一个 Prefab 即可）

---

## 常见问题与解决方案

### ❓ 按钮点击没反应
**原因：** EventSystem 组件缺失

**解决方案：**
确保 `_Managers_Prefab` 上同时有：
- EventSystem 组件
- StandaloneInputModule 组件

**检查方法：**
```
在 Unity 中选中 _Managers_Prefab
查看 Inspector 是否有这两个组件
```

---

### ❓ 背包不显示
**可能原因：**
1. UICanvas 未激活（`m_IsActive: 0`）
2. InventoryPanel 未激活
3. ViewState 不在 Wall_A/B/C/D

**解决方案：**
1. 检查 `_Managers_Prefab.prefab` → UICanvas → `m_IsActive: 1`
2. 在 SceneController 加载场景时会自动调用 `ShowInventoryUI()`
3. 确保场景加载后 ViewState 切换到 Wall_A

---

### ❓ FurnitureZoomController 报错
**错误信息：**
```
[FurnitureZoom] Zoom view [0] (Wall_A) object is missing!
```

**原因：** FurnitureZoomController 的 Zoom Views 列表配置错误

**解决方案：**
在 Level1_Room 场景中，选中 `_SceneManagers` → FurnitureZoomController，检查配置：
```
Zoom Views:
✅ Element 0: viewState = 4, zoomViewObject = Mirror_ZoomView
✅ Element 1: viewState = 5, zoomViewObject = LowCabinet_ZoomView
✅ Element 2: viewState = 6, zoomViewObject = GrandfatherClock_ZoomView
✅ Element 3: viewState = 7, zoomViewObject = CoalHeater_ZoomView

❌ 错误示例：
   Element 0: viewState = 0, zoomViewObject = None
```

---

### ❓ 可交互物体点击没反应
**可能原因：**
1. Layer 设置错误（不是 Layer 3 Interactable）
2. 没有 Collider2D 或 IsTrigger = false
3. InteractionSystem 的 interactableLayer 配置错误

**解决方案：**
检查可交互物体：
1. Layer = 3 (Interactable)
2. 有 BoxCollider2D 或 CircleCollider2D
3. IsTrigger = true
4. 有 InteractableObject 组件

检查 InteractionSystem：
1. interactableLayer Mask 包含 Layer 3

---

### ❓ 场景加载后管理器为 null
**原因：** Bootstrap 场景未正确加载

**解决方案：**
确保：
1. Build Settings 中第一个场景是 Bootstrap
2. BootstrapLoader 正确实例化了 _Managers_Prefab
3. BootstrapInitializer 延迟足够（默认 0.5s）

---

### ❓ 新场景中墙壁切换不工作
**原因：** WallManager 未注册到 GameManager

**解决方案：**
确保场景中的 WallManager 在 Awake 时调用：
```csharp
private void Awake() {
    if (GameManager.Instance != null) {
        GameManager.Instance.RegisterWallManager(this);
    }
}
```

---

## 开发建议

### 📝 添加新场景的步骤

1. **创建场景文件**
   ```
   Assets/Scenes/NewScene.unity
   ```

2. **添加场景管理器**
   ```
   创建空物体 _SceneManagers
   添加 WallManager (如果需要)
   添加 FurnitureZoomController (如果需要)
   ```

3. **配置 GameManager**
   ```csharp
   // 在 GameManager.cs 中添加新的 GameState
   public enum GameState {
       // ...
       NewLevel,
   }

   // 在 UpdateGameStateBasedOnScene 中添加映射
   "NewScene" => GameState.NewLevel,
   ```

4. **配置场景加载**
   ```csharp
   // 在合适的地方调用
   SceneController.Instance.LoadScene("NewScene");
   ```

---

### 📝 添加新物品的步骤

1. **创建物品数据（ScriptableObject）**
   ```
   右键 → Create → Inventory → Item
   保存到 Assets/Resources/Items/ItemName.asset
   ```

2. **配置物品属性**
   - icon: 分配精灵图
   - itemName: 显示名称
   - description: 物品描述

3. **创建场景中的可交互物体**
   ```
   创建 GameObject
   Layer = 3 (Interactable)
   添加 SpriteRenderer
   添加 BoxCollider2D (IsTrigger = true)
   添加 InteractableObject 组件
   ```

4. **配置 InteractableObject**
   - objectID: 唯一ID（如 "key_level1"）
   - displayName: 显示名称
   - interactionType: PickUp
   - item: 拖入第1步创建的物品数据
   - isPickupable: true

---

### 📝 添加新放大视图的步骤

1. **在 GameManager.cs 中添加 ViewState**
   ```csharp
   public enum ViewState {
       // ...
       Level1_Zoom_NewFurniture,
   }
   ```

2. **在场景中创建放大视图 GameObject**
   ```
   FurnitureZoomViews/NewFurniture_ZoomView
   添加背景图、可交互物体等
   默认设置为 Active = false
   ```

3. **在 FurnitureZoomController 中配置**
   ```
   在 Inspector 的 Zoom Views 列表中添加新条目：
   - viewState: Level1_Zoom_NewFurniture
   - zoomViewObject: NewFurniture_ZoomView
   ```

4. **创建触发器**
   ```
   在墙壁视图中创建可点击的家具物体
   InteractionType = ZoomView
   associatedZoomView = Level1_Zoom_NewFurniture
   ```

---

## 总结

### 当前项目状态
✅ **架构完成度：90%**
- 核心系统全部搭建完毕
- 已修复所有已知 bug
- 可以开始内容开发

⏳ **内容完成度：10%**
- 只有测试用的 test_interactable_obj
- 需要大量游戏内容填充

### 下一步建议优先级

**Priority 1 - 立即开始：**
1. ✅ 设计 Level1_Room 的谜题和故事线
2. ✅ 创建 3-5 个可拾取物品
3. ✅ 实现一个完整的放大视图（推荐 LowCabinet）
4. ✅ 测试拾取和背包系统

**Priority 2 - 短期目标：**
1. 完成 Level1_Room 所有放大视图
2. 实现物品使用系统
3. 设计并实现第一个谜题
4. 测试存档/读档功能

**Priority 3 - 中期目标：**
1. 开发 Level2_Room
2. 完善音效系统
3. 添加暂停菜单
4. 优化 UI/UX

**Priority 4 - 长期目标：**
1. 创建结局场景
2. 添加成就系统
3. 多语言支持
4. 最终测试和打包

---

## 附录

### 文件路径快速参考

**核心脚本：**
```
Assets/Scripts/
├── Bootstrap/
│   ├── BootstrapLoader.cs
│   └── BootstrapInitializer.cs
├── Managers/
│   ├── GameManager.cs
│   ├── UIManager.cs
│   ├── SceneController.cs
│   ├── AudioManager.cs
│   └── FurnitureZoomController.cs
├── Inventory/
│   └── InventorySystem.cs
├── SaveLoad/
│   └── SaveLoadSystem.cs
├── Player/
│   └── InteractionSystem.cs
├── Interactable/
│   └── InteractableObject.cs
└── SceneSpecific/
    ├── LandingPageUI.cs
    └── WallManager.cs
```

**场景：**
```
Assets/Scenes/
├── Bootstrap.unity
├── LandingPage.unity
├── Level1_Room.unity
└── (待添加更多场景)
```

**Prefabs：**
```
Assets/Prefabs/
├── _Managers_Prefab.prefab
└── (待添加物品 prefabs)

Assets/Resources/Prefabs/UI/
└── ItemSlot.prefab
```

**资源：**
```
Assets/Resources/
├── Items/ (ScriptableObjects)
├── Prefabs/
│   └── UI/
└── (待添加音效、图片等)
```

---

**文档版本历史：**
- v1.0 (2025-11-06): 初始版本，总结当前架构和待开发功能

**维护者：** AI Assistant
**联系方式：** GitHub Issues

---

祝开发顺利！🎮✨
