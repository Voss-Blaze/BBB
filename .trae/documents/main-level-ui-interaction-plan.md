# Main / Level 页面交互实施方案

## Summary

本轮只做非玩法页面的交互：

- Main 四张区域卷轴按钮 → 进入 Level，并记录所选区域。
- Level 根据所选区域只显示一张关卡图，其余隐藏。
- Level 工具栏的「画卷 / 图鉴 / 设置」可用；「残页」保持无响应。
- 画卷界面可在 Level 内切换区域，联动关卡图、区域高亮和飘屏提示。
- 图鉴、设置复用 Main 的 UI：提取为共享预制体，Main 与 Level 各放一份实例。
- Level 的设置面板显示「返回主页」按钮，Main 的设置面板不显示。
- 残页、写生、异兽辨名的面板与按钮全部不动。

## Current State Analysis

### Main.unity

- `Canvas/Main/画卷` 下是 4 个区域预制体实例：山卷、海内卷、海外卷、大荒卷。
- 4 个区域预制体 `Assets/Art/*.prefab` 根节点已带 `Image + Button + Text`，可点击。
- `Canvas/Codex` 初始未激活，内部已有：
  - 4 个区域签按钮：山卷按钮 / 海内卷按钮 / 海外卷按钮 / 大荒卷按钮
  - 上一个Button (Legacy) / 下一个Button (Legacy)
  - 异兽名底图/异兽名、异兽立绘
  - 残页内容Title/Text (Legacy)、内容
  - 没有关闭按钮
- `Canvas/SettingPanel` 初始未激活，内部只有 3 个 Slider（总音量 / 音乐 / 音效）与标题，没有关闭/返回按钮。
- `图鉴按钮`、`设置按钮` 已有 Button。
- `重新绘卷`、`退出绘卷` 本轮不动。

### Level.unity

- 根节点存在 `山关卡 / 海内关卡 / 海外关卡 / 大荒关卡` 四张 SpriteRenderer，目前没有运行时显隐控制。
- `Canvas/GameUI` 下有 `画卷按钮 / 图鉴按钮 / 残页按钮 / 设置按钮`，且有 `文字提示飘屏`。
- `Canvas/画卷界面` 内有 `Image`、4 个区域预制体实例、`返回`；`返回` 没有 Button 组件。
- 场景内只有 4 个 Button 组件，全部是工具栏四个按钮；画卷界面的「返回」、残页/写生/辨名内部按钮均未挂 Button。
- Level 内没有 Codex 面板、没有 SettingPanel、没有返回主页入口。
- `Canvas` 渲染模式为 Screen Space - Overlay，EventSystem 已存在。

### Build Settings

`ProjectSettings/EditorBuildSettings.asset` 目前只注册了 `Assets/Scenes/Main.unity`，`Level.unity` 未注册，必须补上，否则 `SceneManager.LoadScene("Level")` 会失败。

### 可复用资源

- 现有预制体只有 `Assets/Art/山卷.prefab`、`海内卷.prefab`、`海外卷.prefab`、`大荒卷.prefab`。
- 没有 Codex / SettingPanel 预制体，需要从 Main 场景提取。

### 数据层

- `GameDatabase` 已可按 regionId / beastId 查询。
- `RegionDefinition` 提供 `levelSprite`、`displayName`、`beasts`。
- `BeastDefinition` 提供 `nameSprite`、`codexSprite`、`portraitSprite`、`fragments`。
- `NamePool`、`GameConfig` 已存在；本轮不使用辨名与写生配置。

## Decisions & Assumptions

1. **共享方式**：把 Main 的 Codex、SettingPanel 提取为共享预制体，Main 与 Level 共用；Main 原场景对象替换为预制体实例。
2. **残页入口**：Level 的「残页」按钮不加任何监听，点击无反馈；残页面板保持未激活。
3. **Level 图鉴区域签**：仅浏览图鉴内容，不改变当前关卡区域与关卡图。
4. **区域解锁**：暂不做进度门槛，4 个区域全部可点、可切换；收集状态不参与本轮逻辑。
5. **图鉴内容**：`内容` 只有一个 Text，把该异兽的 3 张残页按「残页一/二/三 + 正文」拼接显示。
6. **设置**：总音量写入 `AudioListener.volume`，三个滑块都写入 PlayerPrefs；音乐/音效暂无音频系统，只存值。
7. **返回主页**：只存在于 Level 的设置面板；点击直接 `SceneManager.LoadScene("Main")`，不做二次确认。
8. **面板互斥**：画卷 / 图鉴 / 设置同一时间只显示一个；点面板空白处关闭。
9. **场景名**：`Main`、`Level`，与 Build Settings 中的文件名一致。
10. **无存档**：不读取/写入游戏进度；区域与异兽内容全部展示。

## Proposed Changes

### 1. 新增运行时脚本

目录：`Assets/Scripts/UI/`，命名空间 `Lingjuan.UI`。

| 文件 | 职责 |
|---|---|
| `GameSession.cs` | 静态会话：保存 `SelectedRegionId`，PlayerPrefs 持久化，读取时回退到 `shanjuan` |
| `GameFlow.cs` | `EnterRegion(RegionDefinition)`：写会话并 `LoadScene("Level")`；`ReturnToMain()` |
| `RegionButton.cs` | 挂在 4 个区域卷轴预制体上，持有 `RegionDefinition`、`Button`、`Image`；暴露 `Clicked` 事件和 `SetSelected(bool)` 高亮 |
| `PanelGroup.cs` | 面板互斥：`Open(GameObject)`、`Toggle(GameObject)`、`CloseAll()` |
| `MainMenuController.cs` | Main 的 4 个区域按钮、图鉴按钮、设置按钮绑定；初始关闭面板 |
| `LevelController.cs` | Level 启动、区域切换、四张关卡图显隐、工具栏绑定、返回主页 |
| `ScrollPanelView.cs` | Level 画卷界面：4 个区域按钮切换区域，返回/空白关闭，当前区域高亮 |
| `CodexPanelView.cs` | 图鉴：区域签、上一个/下一个、异兽名、立绘、3 张残页拼接内容；只浏览 |
| `SettingsPanelView.cs` | 设置：三个滑块、可选「返回主页」按钮、空白关闭 |
| `ToastView.cs` | 复用 `文字提示飘屏`，显示「已进入山卷」等提示并淡出 |

关键字段与规则：

- `GameSession.SelectedRegionId` 为 string；`SetRegion` 同时写 `PlayerPrefs["LastRegion"]`。
- `RegionButton.SetSelected(true)` 时标签 Image 用白色，未选中用 `(1,1,1,0.55)`；不改变布局。
- `PanelGroup` 只管理 `画卷界面 / CodexPanel / SettingPanel` 三个对象，不触碰残页、写生、辨名。
- `LevelController.SwitchRegion(string id)` 统一执行：会话赋值 → 关卡图只激活一张 → 画卷区域按钮高亮 → 关闭面板 → 飘屏「已进入X」。
- `CodexPanelView.Open(regionId)` 每次打开都重置到指定区域第 1 只异兽；区域签只在图鉴内部切换。
- `SettingsPanelView.Open(bool showHome)`：Level 传 `true`，Main 传 `false`。
- `ToastView.Show(text)` 使用 `GameConfig.toastDuration`，无配置时默认 3 秒。

### 2. 共享预制体

目录：`Assets/Prefabs/UI/`。

| 预制体 | 来源 | 新增内容 |
|---|---|---|
| `CodexPanel.prefab` | Main 的 `Canvas/Codex` | 根节点挂 `CodexPanelView`；空白关闭层；按子物体名绑定 4 个区域按钮、上/下按钮、名字/立绘 Image、标题/内容 Text |
| `SettingPanel.prefab` | Main 的 `Canvas/SettingPanel` | 根节点挂 `SettingsPanelView`；空白关闭层；新增 `返回主页` 按钮（默认未激活）；绑定 3 个 Slider |

绑定用的子物体路径：

- Codex：`山卷按钮`、`海内卷按钮`、`海外卷按钮`、`大荒卷按钮`、`上一个Button (Legacy)`、`下一个Button (Legacy)`、`异兽名底图/异兽名`、`异兽立绘`、`残页内容Title/Text (Legacy)`、`内容`。
- Setting：`总音量Slider`、`音乐Slider`、`音效Slider`。
- 空白关闭层：在背景之上、内容之下插入全屏透明 Image + Button，点击关闭当前面板。

### 3. 场景改造

#### Main.unity

- 用 `CodexPanel.prefab`、`SettingPanel.prefab` 的实例替换原 `Codex`、`SettingPanel`，保持未激活与原位置。
- 新增 `MainMenuController`，绑定：
  - `RegionButton[]`：画卷下 4 个区域实例
  - `codexButton`：图鉴按钮
  - `settingsButton`：设置按钮
  - `codexView`、`settingsView`、`panelGroup`
- 初始状态：Codex / SettingPanel 均关闭。

#### Level.unity

- 在 Canvas 下实例化 `CodexPanel`、`SettingPanel` 各一份，未激活。
- `Canvas/画卷界面` 挂 `ScrollPanelView`：
  - 4 个区域实例通过 `RegionButton.Clicked` 调 `LevelController.SwitchRegion`
  - 给 `返回` 补 Button，点击关闭画卷
  - 当前区域标签高亮
- 新增 `LevelController`，绑定：
  - `GameDatabase`
  - 4 张关卡图（山关卡/海内关卡/海外关卡/大荒关卡）
  - 工具栏 4 个按钮（残页按钮只赋值、不监听）
  - `ScrollPanelView`、`CodexPanelView`、`SettingsPanelView`、`PanelGroup`、`ToastView`
- 工具栏行为：
  - 画卷按钮：开关画卷界面
  - 图鉴按钮：以当前区域打开 Codex，关闭其他面板
  - 设置按钮：打开 SettingPanel，并显示「返回主页」
  - 残页按钮：无监听
- 给 `文字提示飘屏` 增加 `CanvasGroup` 与 `ToastView`。
- 关卡启动：读会话区域（无则 PlayerPrefs，仍无则 `shanjuan`），只激活对应关卡图，飘屏「已进入X」。

### 4. 区域预制体

给 `Assets/Art/山卷.prefab`、`海内卷.prefab`、`海外卷.prefab`、`大荒卷.prefab` 根节点添加 `RegionButton`，分别绑定：

| 预制体 | regionId |
|---|---|
| 山卷 | `shanjuan` |
| 海内卷 | `hainei` |
| 海外卷 | `haiwai` |
| 大荒卷 | `dahuang` |

因为 Main 画卷与 Level 画卷共用这 4 个预制体，组件只加一次。

### 5. Build Settings

在 `ProjectSettings/EditorBuildSettings.asset` 的 `m_Scenes` 中追加：

```yaml
  - enabled: 1
    path: Assets/Scenes/Level.unity
    guid: cece0449aee100e4792ef219c47889c1
```

`Main` 保持 index 0。

### 6. 编辑器初始化工具

新增 `Assets/Editor/SharedPanelSetup.cs`，菜单：`灵卷绘形/初始化共享面板与场景绑定`。

执行顺序（可重复运行，已有预制体时复用）：

1. 创建 `Assets/Prefabs/UI/`。
2. 打开 Main 场景，给 `Canvas/Codex` 挂 `CodexPanelView`、空白关闭层，按上表绑定。
3. 给 `Canvas/SettingPanel` 挂 `SettingsPanelView`、空白关闭层，创建并绑定未激活的「返回主页」按钮。
4. 分别保存为 `CodexPanel.prefab`、`SettingPanel.prefab`，并把 Main 原对象替换为预制体实例（保持未激活与层级位置）。
5. 在 Main 添加 `MainMenuController` 与 `PanelGroup`，完成绑定。
6. 给 4 个区域预制体添加 `RegionButton` 并设置 regionId。
7. 打开 Level 场景，实例化两个面板（未激活），添加 `LevelController`、`PanelGroup`、`ScrollPanelView`、`ToastView`，完成工具栏、画卷按钮、关卡图绑定。
8. 给 Level 的画卷 `返回` 补 Button；给面板空白层补关闭监听。
9. 把 Level 加入 Build Settings（若尚未存在）。
10. 保存所有场景与预制体。

说明：预制体必须在 Unity 编辑器内生成，因此首次实现后需要执行一次该菜单；若编辑器已关闭，可用同一方法通过批处理执行。

### 7. 交互规则汇总

#### Main

| 操作 | 结果 |
|---|---|
| 点击山卷/海内卷/海外卷/大荒卷 | 记录区域，加载 Level |
| 点击图鉴按钮 | 打开 Codex，默认上次区域或山卷 |
| 点击设置按钮 | 打开 SettingPanel，不显示返回主页 |
| 图鉴区域签 | 只切图鉴内容 |
| 图鉴上/下 | 区域内 2 只循环 |
| 点击面板空白 | 关闭当前面板 |

#### Level

| 操作 | 结果 |
|---|---|
| 进入场景 | 只显示对应关卡图，飘屏「已进入X」 |
| 画卷按钮 | 开关画卷界面 |
| 图鉴按钮 | 打开 Codex（当前区域，仅浏览） |
| 设置按钮 | 打开设置（显示返回主页） |
| 残页按钮 | 无响应 |
| 画卷区域标签 | 切换区域 + 换关卡图 + 高亮 + 飘屏 |
| 画卷返回/空白 | 关闭画卷 |
| 设置返回主页 | 加载 Main |
| 面板互斥 | 同一时间只显示一个 |

## Implementation Steps

1. 新增 `Assets/Scripts/UI/` 下 10 个运行时脚本。
2. 新增 `Assets/Editor/SharedPanelSetup.cs`。
3. 修改 `ProjectSettings/EditorBuildSettings.asset` 加入 Level。
4. 在 Unity 中执行 `灵卷绘形/初始化共享面板与场景绑定`。
5. 编译并进入 Play 模式验证。

## Verification

1. 编译无错误；Build Settings 中 Main 为 0、Level 为 1。
2. Main 点击四个区域分别进入 Level，且只显示对应关卡图，另外三张未激活。
3. Level 工具栏：画卷/图鉴/设置可开可关，三者互斥；残页按钮无任何反馈。
4. 画卷切换区域后：关卡图变化、当前标签高亮、飘屏正确、面板关闭。
5. Level 图鉴：默认当前区域；切区域签只影响图鉴，关卡图不变；上/下只在 2 只异兽间循环。
6. Level 设置显示「返回主页」，点击回到 Main；Main 设置不显示该按钮。
7. 点 Codex/SettingPanel/画卷的空白区域能关闭面板。
8. 从 Main 重新进入其他区域，Level 显示正确关卡图。

## Risks

- 共享面板依赖编辑器工具生成预制体；工具需幂等，避免重复实例。
- 子物体按名称绑定，后续改名会断引用；绑定集中在两个 View 脚本中，便于排查。
- `GameSession` 静态字段在编辑器域重载后会重置，已用 PlayerPrefs 回退。
- 新建「返回主页」按钮需要 Legacy Text 与内置 UISprite；字体使用项目现有 Text 的字体或 `LegacyRuntime.ttf`。
- 关卡图四张同时存在，必须确保启动时只激活一张，避免视觉重叠。
