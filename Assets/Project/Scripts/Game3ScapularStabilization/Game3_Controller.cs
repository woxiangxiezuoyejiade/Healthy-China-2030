using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// 射击行动 - 游戏主控制器
/// 完整游戏流程：主菜单 → 倒计时 → 游戏中 → 结算 → 返回
/// </summary>
public class Game3_Controller : MonoBehaviour
{
    [Header("游戏设置")]
    public string mainMenuSceneName = "MainMenu";
    public float roundTime = 90f;

    [Header("模块引用")]
    public Game3_ActionDetector actionDetector;
    public Game3_ScoreManager scoreManager;
    public Game3_ShootingSceneBuilder sceneBuilder;
    public Game3_ProjectileSystem projectileSystem;
    public Game3_HUD hud;
    public Game3_AudioSystem audioSystem;

    public Game3_GameState CurrentState { get; private set; } = Game3_GameState.Intro;

    float remainingTime;
    bool hasSubmittedScore;
    bool gameEnded;  // 防止 EndGame() 多次触发
    Coroutine gameRoutine;
    Game3_DifficultyConfig difficultyConfig;

    // 射击统计
    int totalHits;
    int currentCombo;
    int bestCombo;
    int totalScore;

    // 单例检查
    static Game3_Controller _instance;

    void Awake()
    {
        // 单例检查：防止多个 Controller 实例
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[Game3_Controller] 检测到重复实例！销毁多余的: {gameObject.name}");
            Destroy(this);
            return;
        }
        _instance = this;

        // 初始化难度配置
        difficultyConfig = Game3_DifficultyConfig.Normal();

        // 获取或添加模块
        scoreManager = GetOrAddComponent<Game3_ScoreManager>();
        actionDetector = GetOrAddComponent<Game3_ActionDetector>();
        sceneBuilder = GetOrAddComponent<Game3_ShootingSceneBuilder>();
        projectileSystem = GetOrAddComponent<Game3_ProjectileSystem>();
        hud = GetOrAddComponent<Game3_HUD>();
        audioSystem = GetOrAddComponent<Game3_AudioSystem>();

        // 配置模块
        scoreManager.Configure(difficultyConfig);
        actionDetector.Configure(difficultyConfig);

        // 构建场景
        if (sceneBuilder != null) sceneBuilder.AutoBuild();

        // 创建HUD
        hud.CreateHUD();

        // 绑定弹射系统
        projectileSystem.sceneBuilder = sceneBuilder;

        // 绑定目标命中回调
        if (sceneBuilder != null && sceneBuilder.AllTargets != null)
        {
            foreach (Game3_Target target in sceneBuilder.AllTargets)
            {
                if (target != null) target.OnHit += HandleTargetHit;
            }
        }

        // 绑定按钮事件
        hud.GetRestartButton()?.onClick.AddListener(RestartGame);
        hud.GetMenuButton()?.onClick.AddListener(ReturnToMainMenu);

        // 查找XR Rig（延迟执行，等PICO SDK初始化完成）
        // 立即+延迟双调用：先尝试立刻修复（解决 Unity 在 Awake 时报 LineRenderable 错误的问题）
        AutoFindXRRig();
        Invoke(nameof(AutoFindXRRig), 0.5f);
        // 再延迟 1.5s 兜底（PICO SDK 可能很晚才创建手柄）
        Invoke(nameof(AutoFindXRRig), 1.5f);

        // 设置主菜单按钮点击
        SetupMenuButtons();

        Debug.Log("[Game3_Controller] Initialization complete.");
    }

    void SetupMenuButtons()
    {
        GameObject startBtn = GameObject.Find("StartButton");
        if (startBtn != null)
        {
            GameObject startBg = startBtn.transform.Find("StartButton_BG")?.gameObject;
            if (startBg == null) startBg = startBtn;
            if (startBg.GetComponent<Game3_Button3D>() == null)
            {
                Game3_Button3D btn3d = startBg.AddComponent<Game3_Button3D>();
                btn3d.onClick = () => StartGame();
            }
        }

        GameObject exitBtn = GameObject.Find("ExitButton");
        if (exitBtn != null)
        {
            GameObject exitBg = exitBtn.transform.Find("ExitButton_BG")?.gameObject;
            if (exitBg == null) exitBg = exitBtn;
            if (exitBg.GetComponent<Game3_Button3D>() == null)
            {
                Game3_Button3D btn3d = exitBg.AddComponent<Game3_Button3D>();
                btn3d.onClick = () => ReturnToMainMenu();
            }
        }
    }

    void OnEnable()
    {
        if (actionDetector != null)
        {
            actionDetector.OnProgressChanged += HandleProgressChanged;
            actionDetector.OnActionCompleted += HandleActionCompleted;
            actionDetector.OnActionInvalid += HandleActionInvalid;
        }
    }

    void OnDisable()
    {
        if (actionDetector != null)
        {
            actionDetector.OnProgressChanged -= HandleProgressChanged;
            actionDetector.OnActionCompleted -= HandleActionCompleted;
            actionDetector.OnActionInvalid -= HandleActionInvalid;
        }
    }

    void Update()
    {
        // 结算状态下只处理UI按钮，屏蔽所有其他输入
        if (CurrentState == Game3_GameState.Result || CurrentState == Game3_GameState.Finished)
        {
            return;
        }

        if (CurrentState == Game3_GameState.Playing)
        {
            remainingTime -= Time.deltaTime;
            hud.UpdateTimer(remainingTime);
            hud.UpdateScore(totalScore);

            if (actionDetector != null)
            {
                actionDetector.Tick(Time.deltaTime);
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                SimulateTestAction();
            }
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                {
                }
                else
                {
                    SimulateTestAction();
                }
            }

            if (remainingTime <= 0f)
            {
                EndGame();
            }
        }

        // 主菜单模式下检测鼠标点击3D按钮
        if (CurrentState == Game3_GameState.Intro)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Game3_Button3D btn = hit.collider.GetComponent<Game3_Button3D>();
                    if (btn != null) btn.onClick?.Invoke();
                }
            }
        }
    }

    #region 游戏流程

    public void StartGame()
    {
        if (CurrentState != Game3_GameState.Intro) return;
        BeginGameFlow();
    }

    public void BeginGameFlow()
    {
        if (gameRoutine != null) StopCoroutine(gameRoutine);
        gameRoutine = StartCoroutine(GameFlowRoutine());
    }

    public void RestartGame()
    {
        Debug.Log("[Game3] RestartGame called");

        // 先停止正在运行的协程
        if (gameRoutine != null)
        {
            StopCoroutine(gameRoutine);
            gameRoutine = null;
        }

        // 停止所有飞行中的弹丸
        if (projectileSystem != null) projectileSystem.StopAllProjectiles();

        // 立即重置状态，防止 Update() 中残留状态触发逻辑
        CurrentState = Game3_GameState.Intro;
        remainingTime = roundTime;
        gameEnded = false;

        // 重置所有统计
        hasSubmittedScore = false;
        totalHits = 0;
        currentCombo = 0;
        bestCombo = 0;
        totalScore = 0;

        hud.ClearResult();
        BeginGameFlow();
    }

    public void ReturnToMainMenu()
    {
        Debug.Log("[Game3] ReturnToMainMenu called");

        // 先停止正在运行的协程
        if (gameRoutine != null)
        {
            StopCoroutine(gameRoutine);
            gameRoutine = null;
        }

        // 停止所有飞行中的弹丸
        if (projectileSystem != null) projectileSystem.StopAllProjectiles();

        // 立即重置状态
        CurrentState = Game3_GameState.Intro;
        remainingTime = roundTime;
        gameEnded = false;

        hud.SetGameHUDVisible(false);
        hud.ClearResult();
        hud.SetCountdownVisible(false);
        if (sceneBuilder != null) sceneBuilder.EnterMenuMode();

        // 重置所有统计数据
        totalHits = 0;
        currentCombo = 0;
        bestCombo = 0;
        totalScore = 0;
        hasSubmittedScore = false;

        // 重置目标
        if (sceneBuilder != null && sceneBuilder.AllTargets != null)
        {
            foreach (Game3_Target target in sceneBuilder.AllTargets)
            {
                if (target != null && !target.IsAlive)
                {
                    target.gameObject.SetActive(true);
                    target.IsAlive = true;
                }
            }
        }
    }

    IEnumerator GameFlowRoutine()
    {
        // 立即设置安全状态，防止 Update() 中残留状态触发逻辑
        CurrentState = Game3_GameState.Countdown;

        // 重置状态
        totalHits = 0;
        currentCombo = 0;
        bestCombo = 0;
        totalScore = 0;
        remainingTime = roundTime;
        hasSubmittedScore = false;
        gameEnded = false;

        scoreManager.ResetScore();

        // 切换到游戏模式
        if (sceneBuilder != null) sceneBuilder.EnterGameMode();
        hud.SetGameHUDVisible(true);
        hud.SetResultVisible(false);
        hud.UpdateScore(0);
        hud.UpdateTimer(remainingTime);
        hud.UpdateCombo(0);

        // 优先等待 VR 控制器就绪；若未就绪则继续等待而不显示测试提示
        bool calibrated = false;
        if (actionDetector != null)
        {
            calibrated = actionDetector.Calibrate();
        }
        if (!calibrated)
        {
            Debug.LogWarning("[Game3_Controller] VR控制器未就绪，等待控制器连接后再继续。");
            float waitTime = 0f;
            while (!calibrated && waitTime < 20f)
            {
                yield return new WaitForSeconds(0.5f);
                waitTime += 0.5f;
                AutoFindXRRig();
                calibrated = actionDetector != null && actionDetector.Calibrate();
            }

            if (!calibrated)
            {
                Debug.LogWarning("[Game3_Controller] VR控制器仍未就绪，继续使用测试输入模式。");
            }
        }

        // 倒计时（CurrentState 已在协程开头设置为 Countdown）
        hud.SetCountdownVisible(true);

        for (int i = 3; i > 0; i--)
        {
            hud.ShowCountdown(i);
            if (audioSystem != null) audioSystem.PlayCountdown();
            yield return new WaitForSeconds(1f);
        }
        hud.ShowCountdown(0); // "开始!"
        if (audioSystem != null) audioSystem.PlayStart();
        yield return new WaitForSeconds(0.5f);
        hud.SetCountdownVisible(false);

        // 开始游戏
        CurrentState = Game3_GameState.Playing;
        hud.ShowFeedback("射击开始!", new Color(1f, 0.85f, 0f));
        Debug.Log("[Game3_Controller] Game started! Press Space or Mouse to shoot.");
    }

    void EndGame()
    {
        // 双重保护：只有 Playing 状态且未结束过才能触发
        if (CurrentState != Game3_GameState.Playing) return;
        if (gameEnded) return;
        gameEnded = true;
        CurrentState = Game3_GameState.Result;

        // 计算最终得分
        int finalScore = totalScore;
        string grade = GetGrade(finalScore);

        Debug.Log($"[Game3] EndGame: score={finalScore}, hits={totalHits}, combo={bestCombo}, grade={grade}");
        Debug.Log($"[Game3] EndGame call stack:\n{System.Environment.StackTrace}");

        // 提交分数
        SubmitScore(finalScore);

        // 显示结算
        hud.ShowResult(finalScore, totalHits, bestCombo, grade);
        hud.SetGameHUDVisible(false);

        if (audioSystem != null) audioSystem.PlayEnd();
    }

    string GetGrade(int score)
    {
        if (score >= 300) return "S级 - 神射手!";
        if (score >= 200) return "A级 - 精准射击!";
        if (score >= 100) return "B级 - 继续加油!";
        if (score >= 50) return "C级 - 需要练习";
        return "D级 - 再接再厉";
    }

    void SubmitScore(int finalScore)
    {
        if (hasSubmittedScore) return;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.addScore(finalScore);
        }
        hasSubmittedScore = true;
    }

    #endregion

    #region 动作检测回调

    void HandleProgressChanged(float progress)
    {
        // 蓄力进度反馈（可用于准星大小变化等）
    }

    void SimulateTestAction()
    {
        if (CurrentState != Game3_GameState.Playing) return;

        if (projectileSystem == null)
        {
            Debug.LogError("[Game3_Controller] projectileSystem is null!");
            return;
        }

        bool fired = projectileSystem.Fire();
        Debug.Log($"[Game3_Controller] Fire: fired={fired}");

        if (fired)
        {
            hud.ShowFeedback("你的姿势真标准", new Color(0.2f, 1f, 0.8f));
            if (audioSystem != null) audioSystem.PlayShoot();

            if (scoreManager != null)
            {
                Game3_ActionResult testResult = new Game3_ActionResult
                {
                    isValid = true,
                    grade = Game3_ActionGrade.Good,
                    retractionDepth = 0.8f,
                    holdDuration = 0.5f,
                    actionDuration = 1.5f,
                    stabilityScore = 0.9f,
                    symmetryScore = 0.85f,
                    postureStable = true
                };
                scoreManager.AddActionResult(testResult, out _);
            }
        }
    }

    void HandleActionCompleted(Game3_ActionResult result)
    {
        if (CurrentState != Game3_GameState.Playing) return;

        if (result.isValid)
        {
            // 动作有效 → 发射弹丸
            bool fired = projectileSystem.Fire();
            if (fired)
            {
                // 根据动作质量显示反馈
                string feedback = "";
                Color color = Color.white;
                switch (result.grade)
                {
                    case Game3_ActionGrade.Excellent:
                        feedback = "完美射击!";
                        color = new Color(1f, 0.85f, 0f);
                        break;
                    case Game3_ActionGrade.Good:
                        feedback = "精准射击!";
                        color = new Color(0.3f, 1f, 0.3f);
                        break;
                    case Game3_ActionGrade.NeedsImprovement:
                        feedback = "勉强命中";
                        color = new Color(1f, 0.6f, 0.2f);
                        break;
                    default:
                        feedback = "射击!";
                        color = Color.white;
                        break;
                }
                hud.ShowFeedback(feedback, color);
                hud.ShowFeedback("你的姿势真标准", new Color(0.2f, 1f, 0.8f));
            }
        }
        else
        {
            // 动作无效 → 未命中
            currentCombo = 0;
            hud.UpdateCombo(0);
            hud.ShowFeedback("动作不标准 - 未发射", new Color(1f, 0.3f, 0.3f));
            if (audioSystem != null) audioSystem.PlayMiss();
        }

        // 同步到ScoreManager
        scoreManager.AddActionResult(result, out Game3_ActionGrade grade);
    }

    void HandleActionInvalid(string reason)
    {
        if (CurrentState != Game3_GameState.Playing) return;
        currentCombo = 0;
        hud.UpdateCombo(0);
    }

    #endregion

    #region 目标命中回调

    void HandleTargetHit(Game3_Target target, int score)
    {
        // 只有 Playing 状态才处理命中，防止游戏结束后残留弹丸污染新游戏数据
        if (CurrentState != Game3_GameState.Playing) return;

        totalHits++;
        totalScore += score;
        currentCombo++;
        bestCombo = Mathf.Max(bestCombo, currentCombo);

        // 连击加分
        int comboBonus = 0;
        if (currentCombo >= 5) comboBonus = 10;
        else if (currentCombo >= 3) comboBonus = 5;
        totalScore += comboBonus;

        hud.UpdateScore(totalScore);
        hud.UpdateCombo(currentCombo);

        // 连击反馈
        if (currentCombo >= 5)
        {
            hud.ShowFeedback($"{currentCombo}连击! +{score + comboBonus}", new Color(1f, 0.3f, 0.3f));
            if (audioSystem != null) audioSystem.PlayCombo();
        }
        else if (currentCombo >= 3)
        {
            hud.ShowFeedback($"{currentCombo}连击! +{score + comboBonus}", new Color(1f, 0.85f, 0f));
            if (audioSystem != null) audioSystem.PlayHit();
        }
        else
        {
            if (audioSystem != null) audioSystem.PlayHit();
        }

        Debug.Log($"[Game3] 命中{target.targetType}目标! +{score}分 (距离加分已含), 连击{currentCombo}");
    }

    #endregion

    #region XR Rig

    void AutoFindXRRig()
    {
        GameObject xrRig = GameObject.Find("XR Origin");
        if (xrRig == null) xrRig = GameObject.Find("XRRig");
        if (xrRig == null) xrRig = GameObject.Find("XR Origin (XR Rig)");
        if (xrRig == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null) xrRig = mainCam.transform.root.gameObject;
        }
        if (xrRig == null)
        {
            Debug.LogWarning("[Game3_Controller] XR Rig not found.");
            return;
        }

        Transform rightCtrl = FindDeepChildByName(xrRig.transform, "Right");
        if (rightCtrl == null) rightCtrl = FindDeepChildByName(xrRig.transform, "RightHand");
        if (rightCtrl == null) rightCtrl = FindDeepChildByName(xrRig.transform, "RightHand Controller");
        if (rightCtrl == null) rightCtrl = FindDeepChildByName(xrRig.transform, "Right Controller");

        Transform leftCtrl = FindDeepChildByName(xrRig.transform, "Left");
        if (leftCtrl == null) leftCtrl = FindDeepChildByName(xrRig.transform, "LeftHand");
        if (leftCtrl == null) leftCtrl = FindDeepChildByName(xrRig.transform, "LeftHand Controller");
        if (leftCtrl == null) leftCtrl = FindDeepChildByName(xrRig.transform, "Left Controller");

        Transform head = FindDeepChildByName(xrRig.transform, "Main Camera");

        if (actionDetector != null)
        {
            actionDetector.rightController = rightCtrl;
            actionDetector.leftController = leftCtrl != null ? leftCtrl : rightCtrl;
            actionDetector.head = head;
        }
        if (sceneBuilder != null)
        {
            sceneBuilder.SetControllers(rightCtrl, leftCtrl != null ? leftCtrl : rightCtrl);
        }

        // 自动修复：给缺少 XR Ray Interactor 的控制器添加组件
        if (rightCtrl != null) FixXRController(rightCtrl.gameObject);
        if (leftCtrl != null) FixXRController(leftCtrl.gameObject);

        Debug.Log("[Game3_Controller] XR Rig references configured.");
    }

    void FixXRController(GameObject controllerObj)
    {
        if (controllerObj == null) return;

        // 第一步：先删除所有孤儿的 XRInteractorLineVisual（这是报错的根源）
        RemoveOrphanedLineVisual(controllerObj);

        // 通过反射查找 XR Ray Interactor 类型
        Type rayInteractorType = FindXRType(
            "UnityEngine.XR.Interaction.Toolkit.XRRayInteractor",
            "Unity.XR.Interaction.Toolkit.XRRayInteractor"
        );

        if (rayInteractorType == null)
        {
            Debug.LogWarning("[Game3_Controller] XR Ray Interactor type not found. Make sure XR Interaction Toolkit is installed.");
            return;
        }

        // 检查是否已经有 Ray Interactor
        Component existingRay = controllerObj.GetComponent(rayInteractorType);
        if (existingRay == null)
        {
            // 动态添加 Ray Interactor
            existingRay = controllerObj.AddComponent(rayInteractorType);
            Debug.Log($"[Game3_Controller] Auto-added {rayInteractorType.Name} to {controllerObj.name}.");
        }
        else
        {
            Debug.Log($"[Game3_Controller] {controllerObj.name} already has {rayInteractorType.Name}.");
        }

        // 添加 XR Interactor Line Visual（可见激光线）
        Type lineVisualType = FindXRType(
            "UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual",
            "Unity.XR.Interaction.Toolkit.XRInteractorLineVisual"
        );

        if (lineVisualType != null)
        {
            Component existingLine = controllerObj.GetComponent(lineVisualType);
            if (existingLine == null)
            {
                controllerObj.AddComponent(lineVisualType);
                Debug.Log($"[Game3_Controller] Auto-added {lineVisualType.Name} to {controllerObj.name}.");
            }
            else
            {
                // 如果已有 LineVisual，确保它绑定了 Ray Interactor
                FieldInfo interactorField = lineVisualType.GetField("m_Interactor",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
                if (interactorField == null)
                    interactorField = lineVisualType.GetField("interactor",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                if (interactorField != null)
                {
                    object interactorValue = interactorField.GetValue(existingLine);
                    if (interactorValue == null)
                    {
                        // 绑定 Ray Interactor
                        interactorField.SetValue(existingLine, existingRay);
                        Debug.Log($"[Game3_Controller] Bound {rayInteractorType.Name} to existing {lineVisualType.Name} on {controllerObj.name}.");
                    }
                }
            }
        }
    }

    void RemoveOrphanedLineVisual(GameObject controllerObj)
    {
        Type lineVisualType = FindXRType(
            "UnityEngine.XR.Interaction.Toolkit.XRInteractorLineVisual",
            "Unity.XR.Interaction.Toolkit.XRInteractorLineVisual"
        );
        if (lineVisualType == null) return;

        // 激进策略：删除所有旧的 LineVisual（不管是否孤儿）
        // 因为后面 FixXRController 会重新添加一个正确绑定的
        Component[] lineVisuals = controllerObj.GetComponents(lineVisualType);
        foreach (Component lv in lineVisuals)
        {
            Debug.Log($"[Game3_Controller] Removing old LineVisual on {controllerObj.name} (will re-add clean one).");
            Destroy(lv);
        }
    }

    Type FindXRType(params string[] typeNames)
    {
        foreach (string name in typeNames)
        {
            Type t = Type.GetType(name);
            if (t != null) return t;
            foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(name);
                if (t != null) return t;
            }
        }
        return null;
    }

    Transform FindDeepChildByName(Transform parent, string namePart)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name != null && child.name.Contains(namePart)) return child;
            Transform found = FindDeepChildByName(child, namePart);
            if (found != null) return found;
        }
        return null;
    }

    #endregion

    T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
            Debug.Log($"[Game3_Controller] Auto-added: {typeof(T).Name}");
        }
        return component;
    }
}

/// <summary>
/// 3D按钮组件 - 用于场景中的物理按钮点击
/// </summary>
public class Game3_Button3D : MonoBehaviour
{
    public System.Action onClick;

    void Start()
    {
        // 确保有碰撞器
        if (GetComponent<Collider>() == null)
        {
            BoxCollider bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.5f, 0.5f, 0.2f);
        }
    }

    void OnMouseDown()
    {
        // 只在主菜单状态下响应3D按钮点击
        Game3_Controller ctrl = FindObjectOfType<Game3_Controller>();
        if (ctrl != null && ctrl.CurrentState != Game3_GameState.Intro) return;

        onClick?.Invoke();
    }
}
