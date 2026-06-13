using System.Collections;
using UnityEngine;

public class Game2_ChestExpansionController : MonoBehaviour
{
    [Header("Game Setup")]
    public Game2_DifficultyConfig config = new Game2_DifficultyConfig();
    public bool autoStartOnSceneLoad = true;
    public KeyCode restartKey = KeyCode.R;
    public float tutorialDuration = 15f;

    [Header("Module References")]
    public Game2_ActionDetector actionDetector;
    public Game2_ScoreManager scoreManager;
    public Game2_FogVisual fogVisual;
    public Game2_SunEnergyVisual sunEnergyVisual;
    public Game2_IslandSceneVisual islandSceneVisual;
    public Game2_HandGuideVisual handGuideVisual;
    public Game2_UIController uiController;
    public Game2_AudioController audioController;

    public bool IsPlaying { get; private set; }

    float remainingTime;
    float storedEnergy;
    bool hasSubmittedScore;
    Coroutine gameRoutine;

    void Awake()
    {
        if (actionDetector == null) actionDetector = GetOrAddComponent<Game2_ActionDetector>();
        if (scoreManager == null) scoreManager = GetOrAddComponent<Game2_ScoreManager>();
        if (fogVisual == null) fogVisual = GetComponent<Game2_FogVisual>();
        if (sunEnergyVisual == null) sunEnergyVisual = GetComponent<Game2_SunEnergyVisual>();
        if (islandSceneVisual == null) islandSceneVisual = GetOrAddComponent<Game2_IslandSceneVisual>();
        if (handGuideVisual == null) handGuideVisual = GetOrAddComponent<Game2_HandGuideVisual>();
        if (uiController == null) uiController = GetOrAddComponent<Game2_UIController>();
        if (audioController == null) audioController = GetOrAddComponent<Game2_AudioController>();
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
        ConfigureModules();

        if (autoStartOnSceneLoad)
        {
            BeginGame();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
            return;
        }

        if (!IsPlaying) return;

        remainingTime -= Time.deltaTime;
        if (actionDetector != null)
        {
            actionDetector.Tick(Time.deltaTime);
        }

        if (remainingTime <= 0f || (scoreManager != null && scoreManager.IsRoundComplete()))
        {
            EndGame();
        }

        UpdateHud();
    }

    public void BeginGame()
    {
        if (gameRoutine != null) StopCoroutine(gameRoutine);
        gameRoutine = StartCoroutine(GameRoutine());
    }

    public void RestartGame()
    {
        BeginGame();
    }

    IEnumerator GameRoutine()
    {
        IsPlaying = false;
        ConfigureModules();
        hasSubmittedScore = false;
        if (handGuideVisual != null) handGuideVisual.StopTutorialDemo();

        if (islandSceneVisual != null) islandSceneVisual.ApplySceneStyle();
        if (scoreManager != null) scoreManager.ResetScore();
        if (fogVisual != null) fogVisual.SetClearProgress(0f);
        if (uiController != null) uiController.ShowIntro(config);
        if (handGuideVisual != null) handGuideVisual.PlayTutorialDemo(tutorialDuration);
        if (audioController != null) audioController.PlayStart();

        storedEnergy = 0f;
        if (sunEnergyVisual != null)
        {
            sunEnergyVisual.SetStoredEnergy(storedEnergy);
            sunEnergyVisual.SetActionProgress(0f);
        }

        remainingTime = config.roundTime;

        yield return new WaitForSeconds(tutorialDuration);
        if (handGuideVisual != null) handGuideVisual.StopTutorialDemo();

        bool calibrated = actionDetector != null && actionDetector.Calibrate();
        if (!calibrated)
        {
            Debug.LogWarning("Game2 calibration failed. Check left/right controller references.");
            if (uiController != null) uiController.SetHint("\u6821\u51c6\u5931\u8d25\uff0c\u8bf7\u68c0\u67e5\u5de6\u53f3\u624b\u63a7\u5236\u5668\u5f15\u7528");
            if (audioController != null) audioController.PlayInvalid();
            yield break;
        }

        if (actionDetector != null && actionDetector.IsUsingKeyboardSimulator)
        {
            Debug.Log("Game2 started in keyboard simulator mode. Hold Space to open arms, release Space to return. Hold Left Shift + Space for an excellent rep.");
            Debug.Log("Press R to restart Game2 and reset the sun/fog.");
        }
        else
        {
            Debug.Log("Game2 started. Open both arms to clear fog.");
        }

        IsPlaying = true;
        if (uiController != null) uiController.ShowPlaying();
        if (audioController != null) audioController.PlayCountdown();
        UpdateHud();
    }

    void ConfigureModules()
    {
        if (actionDetector != null) actionDetector.Configure(config);
        if (scoreManager != null) scoreManager.Configure(config);
        if (handGuideVisual != null) handGuideVisual.Configure(actionDetector);
    }

    void HandleProgressChanged(float progress)
    {
        if (sunEnergyVisual != null)
        {
            sunEnergyVisual.SetActionProgress(progress);
        }

        if (uiController != null)
        {
            uiController.SetProgress(progress);
        }

        if (audioController != null)
        {
            audioController.SetChargeProgress(progress);
        }
    }

    void HandleActionCompleted(Game2_ActionResult result)
    {
        if (!IsPlaying || scoreManager == null) return;

        int scoreBeforeAction = scoreManager.RoundScore;
        int actionScore = scoreManager.AddActionResult(result, out Game2_ActionGrade grade);
        int gainedScore = scoreManager.RoundScore - scoreBeforeAction;
        result.grade = grade;

        if (grade != Game2_ActionGrade.Invalid)
        {
            float clearAmount = scoreManager.GetFogClearAmount(grade);
            if (scoreManager.CurrentCombo >= 3) clearAmount += 0.02f;

            if (fogVisual != null) fogVisual.AddClearProgress(clearAmount);

            storedEnergy = Mathf.Clamp01(storedEnergy + clearAmount);
            if (sunEnergyVisual != null) sunEnergyVisual.SetStoredEnergy(storedEnergy);
        }

        if (sunEnergyVisual != null)
        {
            sunEnergyVisual.PlayActionResult(grade, actionScore);
        }

        if (audioController != null)
        {
            audioController.PlayActionResult(grade);
            if (scoreManager.CurrentCombo == 3 || scoreManager.CurrentCombo == 5 || scoreManager.CurrentCombo == 8 || scoreManager.CurrentCombo == 10)
            {
                audioController.PlayCombo();
            }
        }

        if (uiController != null)
        {
            uiController.ShowActionResult(grade, gainedScore, scoreManager.CurrentCombo);
            if (grade == Game2_ActionGrade.Invalid && !string.IsNullOrEmpty(result.feedback))
            {
                uiController.SetHint(result.feedback);
            }
            UpdateHud();
        }

        Debug.Log($"Game2 action: {grade}, +{gainedScore}, reps {scoreManager.ValidReps}/{config.targetReps}, combo {scoreManager.CurrentCombo}");
    }

    void HandleActionInvalid(string reason)
    {
        if (uiController != null) uiController.SetHint(reason);
        Debug.Log($"Game2 action invalid: {reason}");
    }

    void EndGame()
    {
        if (!IsPlaying) return;

        IsPlaying = false;
        bool success = scoreManager != null && scoreManager.IsRoundSuccess();
        int finalScore = scoreManager != null ? scoreManager.RoundScore : 0;

        if (success)
        {
            if (fogVisual != null) fogVisual.SetClearProgress(1f);
            if (sunEnergyVisual != null) sunEnergyVisual.SetStoredEnergy(1f);
        }

        SubmitScore(finalScore);

        if (uiController != null && scoreManager != null)
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
            audioController.SetChargeProgress(0f);
            audioController.PlayFinish(success);
        }

        Debug.Log(success
            ? "Game2 complete. Fog cleared."
            : "Game2 ended. Try to complete more standard reps.");
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
            Debug.LogWarning("GameManager.Instance is missing; Game2 score was not submitted.");
        }

        hasSubmittedScore = true;
    }

    void UpdateHud()
    {
        if (uiController == null || scoreManager == null) return;

        int total = GameManager.Instance != null ? GameManager.Instance.totalScore : 0;
        uiController.UpdateHud(
            Mathf.Max(0f, remainingTime),
            scoreManager.RoundScore,
            total,
            scoreManager.ValidReps,
            config.targetReps,
            scoreManager.CurrentCombo,
            storedEnergy);
    }

    T GetOrAddComponent<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }
}
