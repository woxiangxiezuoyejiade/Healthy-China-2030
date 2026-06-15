using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game1_ExternalRotationController : MonoBehaviour
{
    [Header("Game Setup")]
    public Game1_TrainingHand trainingHand = Game1_TrainingHand.Right;
    public Game1_Difficulty difficulty = Game1_Difficulty.Normal;
    public Game1_DifficultyConfig config = new Game1_DifficultyConfig();
    public string mainMenuSceneName = "MainMenu";
    public bool autoStartOnSceneLoad = true;
    public bool fastStartInEditor = true;
    public bool alternateHandsEachRep = true;
    public float tutorialDuration = 15f;

    [Header("Module References")]
    public Game1_ActionDetector actionDetector;
    public Game1_ScoreManager scoreManager;
    public Game1_UIController uiController;
    public Game1_EnergyCoreVisual energyCoreVisual;
    public Game1_AudioController audioController;
    public Game1_TutorialController tutorialController;
    public Game1_HandGuideVisual handGuideVisual;

    public Game1_GameState CurrentState { get; private set; } = Game1_GameState.Intro;

    float remainingTime;
    bool hasSubmittedScore;
    Coroutine gameRoutine;
    Coroutine handSwitchRoutine;

    void Awake()
    {
        // 获取或自动添加所有组件
        if (scoreManager == null) scoreManager = GetOrAddComponent<Game1_ScoreManager>();
        if (scoreManager == null) scoreManager = GetOrAddComponent<Game1_ScoreManager>();
        if (actionDetector == null) actionDetector = GetOrAddComponent<Game1_ActionDetector>();
        if (uiController == null) uiController = GetOrAddComponent<Game1_UIController>();
        if (energyCoreVisual == null) energyCoreVisual = GetOrAddComponent<Game1_EnergyCoreVisual>();
        if (audioController == null) audioController = GetOrAddComponent<Game1_AudioController>();
        if (tutorialController == null) tutorialController = GetOrAddComponent<Game1_TutorialController>();
        if (handGuideVisual == null) handGuideVisual = GetOrAddComponent<Game1_HandGuideVisual>();
    }


    T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
            Debug.Log($"自动添加组件: {typeof(T).Name}");
        }
        return component;
    }

    void OnEnable()
    {
        if (actionDetector == null) return;

        actionDetector.OnProgressChanged += HandleProgressChanged;
        actionDetector.OnActionCompleted += HandleActionCompleted;
        actionDetector.OnActionInvalid += HandleActionInvalid;
    }

    void OnDisable()
    {
        if (actionDetector == null) return;

        actionDetector.OnProgressChanged -= HandleProgressChanged;
        actionDetector.OnActionCompleted -= HandleActionCompleted;
        actionDetector.OnActionInvalid -= HandleActionInvalid;
    }

    void Start()
    {
        ApplyDifficultyDefaults();
        ConfigureModule();

        if (scoreManager == null || actionDetector == null)
        {
            Debug.LogError("Game1 requires Game1_ScoreManager and Game1_ActionDetector on the controller object.");
            enabled = false;
            return;
        }

        if (autoStartOnSceneLoad)
        {
            BeginGameFlow();
        }
    }

    void Update()
    {
        if (CurrentState != Game1_GameState.Playing) return;

        remainingTime -= Time.deltaTime;

        if (actionDetector != null)
        {
            actionDetector.Tick(Time.deltaTime);
        }

        UpdateHud();

        if (scoreManager != null && (remainingTime <= 0f || scoreManager.ValidReps >= config.targetReps))
        {
            EndGame();
        }
    }

    public void BeginGameFlow()
    {
        if (gameRoutine != null) StopCoroutine(gameRoutine);
        gameRoutine = StartCoroutine(GameFlowRoutine());
    }

    public void RestartGame()
    {
        hasSubmittedScore = false;
        if (energyCoreVisual != null) energyCoreVisual.ResetCore();
        BeginGameFlow();
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }

    IEnumerator GameFlowRoutine()
    {
        CurrentState = Game1_GameState.Intro;
        if (alternateHandsEachRep)
        {
            trainingHand = Game1_TrainingHand.Right;
        }

        ConfigureModule();
        if (scoreManager == null)
        {
            yield break;
        }

        scoreManager.ResetScore();
        remainingTime = config.roundTime;
        hasSubmittedScore = false;

        if (uiController != null) uiController.ShowIntro(trainingHand, config);

        // \u7b49\u5f85\u7528\u6237\u9009\u62e9\uff1a\u89c2\u770b\u6559\u5b66 \u6216 \u76f4\u63a5\u5f00\u59cb
        bool? watchTutorial = null;
        if (uiController != null)
        {
            uiController.OnTutorialChosen = () => watchTutorial = true;
            uiController.OnSkipTutorial = () => watchTutorial = false;

            while (!watchTutorial.HasValue)
            {
                // \u952e\u76d8\u5feb\u6377\u952e\uff0c\u65b9\u4fbf\u7f16\u8f91\u5668\u6d4b\u8bd5
                if (Input.GetKeyDown(KeyCode.T)) watchTutorial = true;
                if (Input.GetKeyDown(KeyCode.Return)) watchTutorial = false;
                yield return null;
            }

            uiController.OnTutorialChosen = null;
            uiController.OnSkipTutorial = null;
            uiController.HideIntroButtons();
        }

        if (watchTutorial == true)
        {
            if (uiController != null) uiController.SetHint("\u6559\u5b66\u6f14\u793a\uff1a\u8bf7\u89c2\u5bdf\u524d\u81c2\u5411\u5916\u65cb\u8f6c\uff0c\u518d\u56de\u5230\u8d77\u70b9");
            if (handGuideVisual != null) handGuideVisual.PlayTutorialDemo(tutorialDuration);
            if (audioController != null) audioController.PlayStart();

            yield return new WaitForSeconds(tutorialDuration);
            if (handGuideVisual != null) handGuideVisual.StopTutorialDemo();
        }

        if (uiController != null) uiController.SetHint(Game1_Text.HintElbowNinety);
        yield return new WaitForSeconds(UseFastStart() ? 0.2f : 2f);

        if (uiController != null) uiController.SetHint(Game1_Text.HintCalibrating);
        yield return new WaitForSeconds(UseFastStart() ? 0.2f : 1f);

        bool calibrated = actionDetector != null && actionDetector.Calibrate();
        if (!calibrated)
        {
            if (uiController != null) uiController.SetHint(Game1_Text.HintCalibrationFailed);
            yield break;
        }

        CurrentState = Game1_GameState.Countdown;
        int countdownStart = UseFastStart() ? 1 : 3;
        for (int i = countdownStart; i > 0; i--)
        {
            if (uiController != null) uiController.SetHint(i.ToString());
            if (audioController != null) audioController.PlayCountdown();
            yield return new WaitForSeconds(UseFastStart() ? 0.2f : 1f);
        }

        CurrentState = Game1_GameState.Playing;
        if (uiController != null)
        {
            uiController.ShowPlaying();
            uiController.SetHint(Game1_Text.HintRotateOut);
        }
    }

    void EndGame()
    {
        if (CurrentState == Game1_GameState.Result || CurrentState == Game1_GameState.Finished) return;
        if (scoreManager == null) return;

        if (handSwitchRoutine != null)
        {
            StopCoroutine(handSwitchRoutine);
            handSwitchRoutine = null;
        }

        CurrentState = Game1_GameState.Result;
        int finalScore = scoreManager.GetFinalScore();
        bool success = scoreManager.ValidReps >= config.minimumSuccessReps;

        SubmitScore(finalScore);

        if (uiController != null)
        {
            uiController.ShowResult(
                finalScore,
                scoreManager.ValidReps,
                config.targetReps,
                scoreManager.BestCombo,
                scoreManager.GetAverageActionScore(),
                success);
        }

        if (audioController != null)
        {
            audioController.PlayFinish(success);
        }
    }

    void SubmitScore(int finalScore)
    {
        if (hasSubmittedScore) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.addScore(finalScore);
        }
        else
        {
            Debug.LogWarning("GameManager.Instance is missing; Game1 score was not submitted.");
        }

        hasSubmittedScore = true;
    }

    bool UseFastStart()
    {
        return Application.isEditor && fastStartInEditor;
    }

    void ConfigureModule()
    {
        if (scoreManager != null) scoreManager.Configure(config);
        if (actionDetector != null) actionDetector.Configure(config, trainingHand);
        if (uiController != null) uiController.SetTrainingInfo(trainingHand, config);
        if (energyCoreVisual != null) energyCoreVisual.ResetCore();
        if (handGuideVisual != null) handGuideVisual.Configure(actionDetector, trainingHand, config);
    }

    void ApplyDifficultyDefaults()
    {
        switch (difficulty)
        {
            case Game1_Difficulty.Simple:
                config = Game1_DifficultyConfig.Simple();
                break;
            case Game1_Difficulty.Advanced:
                config = Game1_DifficultyConfig.Advanced();
                break;
            default:
                config = Game1_DifficultyConfig.Normal();
                break;
        }
    }

    void HandleProgressChanged(float progress)
    {
        if (energyCoreVisual != null) energyCoreVisual.SetChargeProgress(progress);
        if (audioController != null) audioController.SetChargeProgress(progress);
        if (uiController != null) uiController.SetProgress(progress);

        if (CurrentState == Game1_GameState.Playing && progress > 0.05f && progress < 1f && uiController != null)
        {
            uiController.SetHint(Game1_Text.HintKeepRotating);
        }
    }

    void HandleActionCompleted(Game1_ActionResult result)
    {
        if (CurrentState != Game1_GameState.Playing) return;

        int actionScore = scoreManager.AddActionResult(result, out Game1_ActionGrade grade);
        result.grade = grade;

        if (uiController != null)
        {
            uiController.ShowGrade(grade, actionScore);
            uiController.SetHint(actionScore > 0 ? Game1_Text.HintReturnStart : result.feedback);
        }

        if (energyCoreVisual != null)
        {
            float completion = Mathf.Clamp01(result.peakAngle / Mathf.Max(0.01f, config.targetAngle));
            energyCoreVisual.PlayCompletionEffect(completion, grade);
        }

        if (audioController != null)
        {
            audioController.PlayActionResult(grade);
            if (scoreManager.CurrentCombo == 3 || scoreManager.CurrentCombo == 5 || scoreManager.CurrentCombo == 8 || scoreManager.CurrentCombo == 12)
            {
                audioController.PlayCombo();
            }
        }

        UpdateHud();

        if (alternateHandsEachRep && actionDetector != null)
        {
            if (handSwitchRoutine != null) StopCoroutine(handSwitchRoutine);
            handSwitchRoutine = StartCoroutine(SwitchHandAfterReturnRoutine());
        }
    }

    void HandleActionInvalid(string reason)
    {
        if (uiController != null) uiController.SetHint(reason);
        if (energyCoreVisual != null) energyCoreVisual.PlayInvalidEffect(reason);
        if (audioController != null) audioController.PlayInvalid();
    }

    IEnumerator SwitchHandAfterReturnRoutine()
    {
        while (CurrentState == Game1_GameState.Playing &&
               actionDetector != null &&
               actionDetector.CurrentState != Game1_ActionState.WaitingStart)
        {
            yield return null;
        }

        if (CurrentState != Game1_GameState.Playing || actionDetector == null)
        {
            handSwitchRoutine = null;
            yield break;
        }

        trainingHand = trainingHand == Game1_TrainingHand.Right
            ? Game1_TrainingHand.Left
            : Game1_TrainingHand.Right;

        actionDetector.Configure(config, trainingHand);
        bool calibrated = actionDetector.Calibrate();

        if (uiController != null)
        {
            uiController.SetTrainingInfo(trainingHand, config);
            uiController.SetHint(calibrated ? Game1_Text.HintRotateOut : Game1_Text.HintCalibrationFailed);
        }

        if (handGuideVisual != null)
        {
            handGuideVisual.Configure(actionDetector, trainingHand, config);
        }

        handSwitchRoutine = null;
    }

    void UpdateHud()
    {
        if (uiController == null || scoreManager == null) return;

        int total = GameManager.Instance != null ? GameManager.Instance.totalScore : 0;
        uiController.UpdateHud(
            Mathf.Max(0f, remainingTime),
            scoreManager.RoundScore + scoreManager.ComboBonus,
            total,
            scoreManager.ValidReps,
            config.targetReps,
            scoreManager.CurrentCombo);
    }
}
