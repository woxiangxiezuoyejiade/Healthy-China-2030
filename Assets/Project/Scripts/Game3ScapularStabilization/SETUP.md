# Game3 深海珊瑚修复 - 场景搭建指南

## 概述

Game3 是一个基于肩胛稳定训练的深海珊瑚修复游戏。玩家通过做坐姿肩胛后缩动作为受损珊瑚输送修复能量。

## 一、创建 Game3Controller 对象

在 Func3 场景中创建一个空 GameObject，命名为 `Game3Controller`，添加以下组件：

- `Game3_Controller`
- `Game3_ActionDetector`
- `Game3_ScoreManager`
- `Game3_UIController`
- `Game3_CoralVisual`
- `Game3_AudioController`
- `Game3_TutorialController`
- `Game3_UnderwaterEnvironment`

> 注意：`Game3_Controller` 的 Awake 会自动添加缺失组件，但建议手动添加以便在 Inspector 中配置。

## 二、XR Rig 配置

1. 将项目中的 `XRRig` 预制体拖入场景
2. 在 `Game3_ActionDetector` 中：
   - `Right Controller` → 拖入 XR Rig 的右手控制器 Transform
   - `Left Controller` → 拖入 XR Rig 的左手控制器 Transform
   - `Head` → 拖入 XR Rig 的 Main Camera Transform
3. 在 `Game3_CoralVisual` 中：
   - `Right Controller` → 拖入右手控制器 Transform
   - `Left Controller` → 拖入左手控制器 Transform

## 三、珊瑚场景搭建

### 3.1 珊瑚主体

1. 创建一个空 GameObject 命名 `CoralGroup`，放在玩家正前方约 2 米处
2. 在 `CoralGroup` 下创建多个珊瑚形状的子对象（建议 4-6 个）：
   - 可以使用 Unity 基本几何体组合（Cylinder + Sphere）模拟珊瑚形态
   - 或导入免费珊瑚 3D 模型（推荐 Asset Store 搜索 "coral"）
3. 给每个珊瑚对象添加材质，初始颜色设为灰白色（模拟受损状态）
4. 将所有珊瑚的 Renderer 拖入 `Game3_CoralVisual.coralRenderers` 数组
5. 将 `CoralGroup` 的 Transform 拖入 `Game3_CoralVisual.coralCenter`

### 3.2 珊瑚灯光

1. 在珊瑚周围放置 2-4 个 Point Light
2. 初始颜色设为暗淡的蓝绿色
3. 将灯光拖入 `Game3_CoralVisual.coralLights` 数组

### 3.3 珊瑚粒子效果

1. 在 `CoralGroup` 下创建两个 ParticleSystem：
   - 命名 `HealingParticles`：向上飘散的绿色/蓝绿色粒子，模拟修复能量
   - 命名 `InvalidParticles`：向外扩散的红色粒子，模拟修复失败
2. 将它们拖入 `Game3_CoralVisual` 的对应字段
3. 初始状态设为不播放（Play On Awake = false）

## 四、深海环境搭建

### 4.1 海底地面

1. 创建一个 Plane（Scale: 5, 1, 5），放在玩家脚下
2. 使用沙色/深蓝色材质
3. 可选：添加一些岩石模型（可使用项目已有的 PureNature 资源）

### 4.2 环境光源

1. 保留场景中的 Directional Light（设为 `Game3_UnderwaterEnvironment.mainLight`）
   - 颜色设为深蓝绿色 (0.1, 0.3, 0.5)
   - 强度 0.7
2. 创建两个 Point Light 作为焦散光：
   - `CausticLight1`：放在珊瑚上方，颜色浅蓝 (0.3, 0.7, 0.9)
   - `CausticLight2`：放在珊瑚侧方，颜色浅蓝绿 (0.2, 0.6, 0.8)
3. 拖入 `Game3_UnderwaterEnvironment` 的对应字段

### 4.3 暗流粒子

1. 在场景中创建一个 ParticleSystem 命名 `CurrentParticles`
   - 形状：Box，覆盖玩家周围区域
   - 速度：水平方向 2-3 m/s
   - 颜色：半透明蓝白色
   - Play On Awake = false
2. 拖入 `Game3_UnderwaterEnvironment.currentParticles`

### 4.4 水下光柱

`Game3_UnderwaterEnvironment` 会自动创建光柱效果（使用 Cylinder）。
如需自定义，取消勾选 `createLightShafts`，手动创建光柱对象。

## 五、UI 搭建

### 5.1 创建 World Space Canvas

1. 创建 Canvas，Render Mode 设为 `World Space`
2. 放在玩家前方偏上位置（约 1.5 米高，2 米远）
3. Canvas Size: 800 x 400

### 5.2 HUD 元素

在 Canvas 下创建以下 TextMeshPro 文本：

| UI 元素 | 命名建议 | 拖入字段 |
|---------|---------|---------|
| 计时器 | TimerText | uiController.timerText |
| 本局分数 | ScoreText | uiController.scoreText |
| 总分 | TotalScoreText | uiController.totalScoreText |
| 完成次数 | RepsText | uiController.repsText |
| 连击 | ComboText | uiController.comboText |
| 评级 | GradeText | uiController.gradeText |
| 提示 | HintText | uiController.hintText |
| 稳定度 | StabilityText | uiController.stabilityText |
| 珊瑚健康 | CoralHealthText | uiController.coralHealthText |

### 5.3 进度条

创建三个 Slider：

| Slider | 拖入字段 |
|--------|---------|
| 收缩进度 | uiController.retractionProgress |
| 稳定度条 | uiController.stabilityBar |
| 珊瑚健康条 | uiController.coralHealthBar |

### 5.4 面板

1. **IntroPanel**：包含游戏标题和说明，初始 Active
2. **ResultPanel**：包含结果标题、分数、详情文本，初始 Inactive

| 元素 | 拖入字段 |
|------|---------|
| IntroPanel | uiController.introPanel |
| ResultPanel | uiController.resultPanel |
| ResultTitleText | uiController.resultTitleText |
| ResultScoreText | uiController.resultScoreText |
| ResultDetailsText | uiController.resultDetailsText |

## 六、音频配置

1. 在 `Game3Controller` 下创建 4 个 AudioSource：
   - `BGMSource`：循环播放，拖入 `audioController.bgmSource`
   - `SFXSource`：不循环，拖入 `audioController.sfxSource`
   - `ChargeLoopSource`：循环播放，拖入 `audioController.chargeLoopSource`
   - `CurrentSource`：不循环（代码会控制），拖入 `audioController.currentSource`

2. 准备音效文件并拖入对应字段：
   - `startClip`：游戏开始音效
   - `countdownClip`：倒计时音效
   - `successClip`：成功修复音效
   - `excellentClip`：优秀评级音效
   - `invalidClip`：失败音效
   - `comboClip`：连击音效
   - `finishClip`：完成音效
   - `incompleteClip`：未完成音效
   - `currentClip`：暗流环境音效
   - `bubbleClip`：气泡音效

> 推荐从 freesound.org 或 Asset Store 获取免费水下音效

## 七、教程模型（可选）

1. 在场景中创建简单的手臂模型（两个 Cylinder）
2. 放在珊瑚旁边，用于演示肩胛后缩动作
3. 拖入 `tutorialController.demoRightArm` 和 `tutorialController.demoLeftArm`

## 八、Game3_Controller Inspector 配置

在 `Game3_Controller` 组件中：

- `Difficulty`：选择 Simple / Normal / Advanced
- `MainMenu Scene Name`：设为 "MainMenu"
- `Auto Start On Scene Load`：勾选

确认所有模块引用都已正确拖入。

## 九、测试

1. 不连接 VR 头显时，使用 XR Device Simulator（项目已配置）
   - WASD：移动
   - 鼠标右键拖动：旋转视角
   - 模拟手柄移动
2. 运行场景，观察：
   - 深海雾效和焦散光是否正常
   - 气泡和浮游粒子是否生成
   - 教程动画是否播放
   - 校准是否成功
   - 手柄后移时能量光束和进度条是否响应

## 十、默认参数（Normal 难度）

- 90 秒每回合
- 10 次目标修复
- 8 次最低成功修复
- 8cm 目标肩胛后缩距离
- 4cm 最低有效后缩距离
- 0.5 秒保持时间
- 1500 最高回合分数
