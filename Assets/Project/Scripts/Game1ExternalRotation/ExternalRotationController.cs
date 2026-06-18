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

    [Header("Module References")]
    public Game1_ActionDetector actionDetector;
    public Game1_ScoreManager scoreManager;
    public Game1_UIController uiController;
    public Game1_EnergyCoreVisual energyCoreVisual;
    public Game1_AudioController audioController;
    public Game1_TutorialController tutorialController;

    public Game1_GameState CurrentState { get; private set; } = Game1_GameState.Intro;

    float remainingTime;
    bool hasSubmittedScore;
    Coroutine gameRoutine;

    void Awake()
    {
        // 获取或自动添加所有组件
        if (scoreManager == null) scoreManager = GetOrAddComponent<Game1_ScoreManager>();
        if (actionDetector == null) actionDetector = GetOrAddComponent<Game1_ActionDetector>();
        if (uiController == null) uiController = GetOrAddComponent<Game1_UIController>();
        if (energyCoreVisual == null) energyCoreVisual = GetOrAddComponent<Game1_EnergyCoreVisual>();
        if (audioController == null) audioController = GetOrAddComponent<Game1_AudioController>();
        if (tutorialController == null) tutorialController = GetOrAddComponent<Game1_TutorialController>();
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
        ConfigureModule();
        if (scoreManager == null)
        {
            yield break;
        }

        scoreManager.ResetScore();
        remainingTime = config.roundTime;
        hasSubmittedScore = false;

        if (uiController != null) uiController.ShowIntro(trainingHand, config);
        if (tutorialController != null) tutorialController.PlayDemo();
        if (audioController != null) audioController.PlayStart();

        yield return new WaitForSeconds(6f);

        if (uiController != null) uiController.SetHint(Game1_Text.HintElbowNinety);
        yield return new WaitForSeconds(2f);

        if (uiController != null) uiController.SetHint(Game1_Text.HintCalibrating);
        yield return new WaitForSeconds(1f);

        bool calibrated = actionDetector != null && actionDetector.Calibrate();
        if (!calibrated)
        {
            if (uiController != null) uiController.SetHint(Game1_Text.HintCalibrationFailed);
            yield break;
        }

        CurrentState = Game1_GameState.Countdown;
        for (int i = 3; i > 0; i--)
        {
            if (uiController != null) uiController.SetHint(i.ToString());
            if (audioController != null) audioController.PlayCountdown();
            yield return new WaitForSeconds(1f);
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

    void ConfigureModule()
    {
        if (scoreManager != null) scoreManager.Configure(config);
        if (actionDetector != null) actionDetector.Configure(config, trainingHand);
        if (uiController != null) uiController.SetTrainingInfo(trainingHand, config);
        if (energyCoreVisual != null) energyCoreVisual.ResetCore();
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
            uiController.SetHint(result.isValid ? Game1_Text.HintReturnStart : result.feedback);
        }

        if (energyCoreVisual != null)
        {
            if (result.isValid) energyCoreVisual.PlaySuccessEffect(grade);
            else energyCoreVisual.PlayInvalidEffect(result.feedback);
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
    }

    void HandleActionInvalid(string reason)
    {
        if (uiController != null) uiController.SetHint(reason);
        if (energyCoreVisual != null) energyCoreVisual.PlayInvalidEffect(reason);
        if (audioController != null) audioController.PlayInvalid();
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
