# PROJECT_CONTEXT.md

## 项目简介

### 项目名称
**Healthy China 2030（健康中国2030）** — VR 肩颈舒缓训练系统

### 项目目标
本项目是虚拟现实课程的大作业，旨在开发一套基于 VR 的肩颈疲劳缓解动作训练小游戏。通过 PICO VR 头显和手柄控制器，引导用户在坐姿下完成标准的康复训练动作，并实时提供视觉、听觉反馈和评分，帮助用户养成正确的运动习惯。

项目共包含 5 个功能模块（func1 ~ func5），当前主要开发 func1 和 func2。

### 课程要求
- 虚拟现实课程大作业
- 需要在 PICO VR 设备上运行
- 动作识别需基于手柄/控制器位置追踪
- 支持编辑器键盘模拟进行无设备测试
- 中文界面 + 中文语音提示

---

## 技术栈

| 层级 | 技术 |
|------|------|
| 游戏引擎 | Unity 2022.3 LTS |
| 渲染管线 | Universal Render Pipeline (URP) 14.0.8 |
| 脚本语言 | C# (.NET Standard 2.1) |
| XR 平台 | PICO XR (PICO Unity Integration SDK v3.4.0) |
| XR 框架 | Unity XR Interaction Toolkit 3.5.0 + Unity XR Management 4.5.4 + Unity OpenXR 1.14.3 |
| UI 系统 | Unity uGUI + TextMeshPro 3.0.9 |
| 中文字体 | Microsoft YaHei（微软雅黑）.ttc，通过 TextMeshPro SDF 动态加载 |
| 音频 | Unity AudioSource + 程序化合成回退音频 |
| 第三方资产 | PureNature 环境资源包（植被、岩石、水面、天空） |
| 目标平台 | Android (PICO VR 头显, ARM64, min SDK 29) |
| 版本控制 | Git |
| IDE | Visual Studio / JetBrains Rider |

### 依赖包
- `com.unity.xr.picoxr` — PICO XR 插件
- `com.unity.xr.interaction.toolkit` — XR 交互工具包
- `com.unity.xr.management` — XR 管理器
- `com.unity.xr.openxr` — OpenXR 支持
- `com.unity.textmeshpro` — 文字渲染
- `com.unity.render-pipelines.universal` — URP

---

## 项目结构

```
Healthy-China-2030-master/
├── Assets/
│   ├── Project/
│   │   ├── Scenes/
│   │   │   ├── MainMenu.unity              # 主菜单场景
│   │   │   ├── Func1.unity                 # func1 场景：坐姿单臂外旋
│   │   │   ├── Func2.unity                 # func2 场景：坐姿屈臂扩胸
│   │   │   ├── Func3.unity                 # func3 场景（预留）
│   │   │   └── SampleScene.unity           # Unity 默认模板场景
│   │   ├── Scripts/
│   │   │   ├── Game1ExternalRotation/      # func1 脚本（11 个文件）
│   │   │   ├── Game2ChestExpansion/        # func2 脚本（8 个文件）
│   │   │   ├── GameManager.cs              # 全局分数管理（单例，跨场景持久化）
│   │   │   ├── TrainingAudioController.cs  # 音频基类（BGM + SFX + 程序化回退）
│   │   │   ├── UI/UIManager.cs             # 主菜单 UI 管理
│   │   │   ├── MotionDetection/            # 动作检测（预留，空）
│   │   │   └── NPC/                        # NPC（预留，空）
│   │   ├── Fonts/                          # 中文字体（msyh.ttc 等）
│   │   ├── Materials/Game2ChestExpansion/  # func2 材质
│   │   ├── Prefabs/Game2ChestExpansion/    # func2 预制体
│   │   └── Settings/                       # 设置（预留，空）
│   ├── PureNature/                         # 第三方环境资产
│   ├── Samples/                            # XR Interaction Toolkit 示例
│   ├── XR/                                 # XR Loader 和 Settings
│   ├── XRI/                                # XR Interaction 设置
│   ├── TextMesh Pro/                       # TextMeshPro 资源
│   ├── Perfabs/                            # XRRig 预制体
│   ├── Models/                             # 模型目录（空，预留）
│   └── Resources/                          # 资源目录（空）
├── Packages/
│   └── manifest.json                       # Unity Package Manager 清单
├── ProjectSettings/                        # Unity 项目设置（21 个 .asset 文件）
├── UserSettings/                           # 用户级编辑器设置
├── Library/                                # Unity 生成缓存
├── Logs/                                   # Unity 日志
├── .gitignore
└── *.sln / *.csproj                        # Visual Studio 解决方案和项目文件
```

---

## 功能模块

### func1 — 坐姿单臂外旋（Seated Single-Arm External Rotation）

**功能：**
用户坐姿，一侧上臂贴近身体、肘部弯曲 90°，以前臂为轴向外旋转（类似开门动作），达到目标角度后保持片刻，再缓慢回到起始位置。训练双手交替进行（先右手、再左手循环）。用于锻炼肩关节外旋能力，缓解肩颈疲劳。

**核心玩法：**
- 面向"能量核心"，按提示交替旋转左右前臂
- **用户可选**：Intro 界面顶部左右各有一个按钮——"观看教学"（青色半透明）和"直接开始"（浅蓝半透明），用户可自选是否观看教学演示；编辑器下也可用 T/Enter 快捷键选择
- **全息小人**：右上角半透明全息投影小人实时展示动作——教学时演示标准动画，训练时跟随用户实际手臂角度；支持左右手交替高亮
- 实时动作检测：角度计算（四元数分解法）、姿态验证（肘部位移、头部偏转、速度限制）
- 三种难度：Simple（35°/8次）、Normal（45°/12次）、Advanced（60°/15次）
- 评分维度：角度完成度 + 动作速度 + 保持时长 + 姿态稳定性
- 连击奖励：2/3/5/8/12 combo 触发额外加分和音效
- 等级评定：Excellent(≥95)、Good(≥75)、NeedsImprovement(≥25)、Invalid(<25)

**完成度：** ~90%（核心逻辑完整，需联调测试和优化）

**涉及脚本：** `Assets/Project/Scripts/Game1ExternalRotation/`
| 文件 | 职责 |
|------|------|
| `ExternalRotationController.cs` | 顶层状态机（Intro→Countdown→Playing→Result），流程编排 |
| `ActionDetector.cs` | 动作检测：外旋角度计算、4 状态机、姿态验证、键盘模拟 |
| `ActionTypes.cs` | 枚举和结构体定义（GameState, ActionState, ActionGrade 等） |
| `DifficultyConfig.cs` | 难度配置（含 Simple/Normal/Advanced 工厂方法） |
| `ScoreManager.cs` | 评分系统：角度/速度/保持/姿态分项 + 连击奖励 |
| `Game1_UIController.cs` | 世界空间 HUD：自动创建 Canvas、计时、分数、评级、结果面板 |
| `EnergyCoreVisual.cs` | 能量核心特效：发光、缩放、完成度粒子（25/50/75/100%）、无效闪烁 |
| `Game1_HandGuideVisual.cs` | 手部引导球 + 弧形运动轨迹箭头（LineRenderer）+ 教程演示 |
| `AudioController.cs` | 音频控制器（继承 TrainingAudioController） |
| `Text.cs` | 静态中文字符串常量 |
| `TutorialController.cs` | 3D 前臂模型教程演示 |
| `Game1_AvatarVisual.cs` | 全息小人：右上角半透明人体可视化，教学自动演示/训练实时跟随 |

**待办：**
- [ ] 在 PICO 设备上实机测试外旋角度检测精度
- [ ] 调优姿态验证阈值（水平/垂直/前后位移容差）
- [ ] 确保左右手交替切换时校准逻辑正确
- [ ] 验证连击奖励和最高连击记录
- [ ] 补齐场景中的 EnergyCore、Pillar 等视觉组件绑定

---

### func2 — 坐姿屈臂扩胸（Seated Bent-Arm Chest Expansion）

**功能：**
用户坐姿，双手持控制器，以肩部为轴同时向两侧打开双臂（类似扩胸运动），达到目标扩展距离后保持片刻，再缓慢回到起始位置。主题隐喻为"驱散迷雾、收集阳光能量"——正确完成动作可逐步清除场景中的雾气，为太阳能量核心充能。

**核心玩法：**
- 双手同时运动，测量两手柄间距相对于起始距离的扩展量
- 实时姿态验证：双手高度差、平均高度变化、前后偏移、头部偏转
- 默认配置：90 秒/轮、10 次目标、8 次最低成功标准、0.50 目标扩展比
- 雾效清除：Excellent 14%、Good 10%、NeedsImprovement 6%（combo≥3 额外+2%）
- 评分维度：扩展完成度 + 动作时间 + 保持时长 + 姿态稳定性
- 连击奖励：3/5/8/10 combo
- 等级评定：Excellent(≥95)、Good(≥75)、NeedsImprovement(≥25)、Invalid(<25)

**完成度：** ~90%（核心逻辑完整，需联调测试和优化）

**涉及脚本：** `Assets/Project/Scripts/Game2ChestExpansion/`
| 文件 | 职责 |
|------|------|
| `Game2_ChestExpansionController.cs` | 顶层状态机：流程编排、雾效清除、能量存储 |
| `Game2_ActionDetector.cs` | 动作检测（4 状态机、姿态验证、键盘模拟） |
| `ActionTypes.cs` | 枚举和结构体定义（ActionState, ActionGrade, ActionResult） |
| `DifficultyConfig.cs` | 难度配置 |
| `Game2_ScoreManager.cs` | 评分系统 + 雾清除量计算 + 连击奖励 |
| `Game2_UIController.cs` | 世界空间 HUD：计时/分数/能量进度条/评级/结果面板 |
| `Game2_FogVisual.cs` | 雾效控制：透明度、粒子密度随清除进度变化 |
| `Game2_SunEnergyVisual.cs` | 太阳能量核心：发光/缩放/光环脉冲 + 彩带雨特效（ConfettiRain） |
| `Game2_IslandSceneVisual.cs` | 程序化场景构建：海洋/岛屿/太阳光环/柔雾粒子 |
| `Game2_HandGuideVisual.cs` | 双手引导球 + 弧形双向运动轨迹箭头 + 教程演示 |
| `Game2_AudioController.cs` | 音频控制器（继承 TrainingAudioController） |

**待办：**
- [ ] 在 PICO 设备上实机测试双手扩展距离检测
- [ ] 调优姿态验证阈值（手部高度差、平均高度变化）
- [ ] 确保雾效材质透明度在 URP 下行为正确
- [ ] 验证彩带雨特效（ConfettiRain）性能和视觉效果
- [ ] 确保 SunEnergy 和 FogVisual 组件在场景中正确绑定
- [ ] 补齐 Game2 的中文提示文本（部分英文提示需汉化）

---

## 当前问题

1. **命名不一致**：func1 的部分类名带 `Game1_` 前缀（如 `Game1_ExternalRotationController`、`Game1_DifficultyConfig`），func2 同理（`Game2_` 前缀）。这是 C# 中避免命名冲突的做法，但需确保 Unity 场景中引用的脚本名称与代码一致。

2. ~~**func2 单文件包含多类**~~：✅ 已拆分。`Game2_ActionDetector.cs` 已拆为 `ActionDetector.cs` + `ActionTypes.cs` + `DifficultyConfig.cs`，与 func1 结构一致。

3. **中文提示不完整**：func2 的 `Game2_ActionDetector.IsPostureStable()` 返回的 `reason` 字符串目前是英文（如 "Keep both hands at a similar height."），需要汉化为中文以保持用户体验一致。

4. **场景绑定未验证**：两个场景（`Func1.unity`、`Func2.unity`）需要在 Unity Editor 中确认所有组件引用（Controller、Head、UI 元素、Visual 模块）已正确挂载到对应的 GameObject 上。

5. **实机测试缺失**：目前仅在 Editor 键盘模拟模式下测试过，PICO VR 实机的控制器追踪精度、延迟和姿态验证阈值需要实际验证和调优。

6. **func3 未开发**：`Func3.unity` 场景和 `MotionDetection/`、`NPC/` 目录预留但内容为空，尚未开始开发。

7. **GameManager 遗留代码**：`GameManager.cs` 中包含 `healthPoints`、`sessionTarget`、`isGameActive` 等遗留字段和 `AddPoints()`、`StartGame()` 等老接口，当前仅 `addScore()`/`totalScore` 被 func1/func2 实际使用。

---

## 下一步计划

1. **联调测试**
   - 在 Unity Editor 中打开 Func1/Func2 场景，验证所有模块自动绑定和 UI 生成
   - 使用键盘模拟器完整跑通游戏流程（教学→倒计时→训练→结果）

2. **中文化完善**
   - 将 func2 的英文提示字符串替换为中文
   - 统一两个游戏的中文术语和提示风格

3. **代码规范化**
   - 将 func2 的 `Game2_ActionDetector.cs` 拆分为符合 func1 结构的多个文件
   - 清理 `GameManager.cs` 中的遗留无用代码

4. **PICO 实机部署**
   - 配置 Android Build Settings（IL2CPP, ARM64, min SDK 29）
   - 构建 APK 并在 PICO 设备上测试
   - 根据实机表现调优姿态检测参数

5. **func3 及后续开发**
   - 开发第三组训练动作
   - 完善主菜单场景的场景跳转逻辑
   - 考虑添加 NPC 引导或教学角色
   - 添加数据持久化（本地分数排行、训练记录）

6. **交付准备**
   - 完善项目文档（SETUP.md 或 README）
   - 准备课程答辩材料（演示视频、PPT）

---

## 变更记录

| 日期 | 变更内容 |
|------|---------|
| 2026-06-15 | **func2 代码拆分**：`Game2_ActionDetector.cs` 拆为 `ActionDetector.cs` + `ActionTypes.cs` + `DifficultyConfig.cs`，与 func1 结构对齐 |
| 2026-06-15 | **func1 新增教学/跳过选择**：Intro 面板顶部增加"观看教学"（青色半透明）和"直接开始"（浅蓝半透明）两个按钮，用户可自选是否观看教学演示；支持键盘快捷键 T/Enter |
| 2026-06-15 | **func1 UI 视觉升级**：面板增加投影+彩色 accent 装饰条；文字加阴影；按钮 ColorTint 悬停效果；进度条外发光；等级弹窗颜色+弹跳动画；连击文字变色+emoji；结果面板大号分数+星级评定 |
| 2026-06-15 | **func1 全息小人**：新增 `Game1_AvatarVisual.cs`，右上角半透明全息小人——教学时自动演示标准动作，训练时实时跟随用户手臂角度，左右手交替时高亮切换 |
