using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Game2_ChestExpansionController : MonoBehaviour
{
    [Header("Game Setup")]
    public Game2_DifficultyConfig config = new Game2_DifficultyConfig();
    public string mainMenuSceneName = "MainMenu";
    public bool autoStartOnSceneLoad = true;
    public KeyCode restartKey = KeyCode.R;
    public float tutorialDuration = 15f;
    public string tutorialVideoFileName = "func2_tutorial.mp4";
    [Tooltip("Keep on: Game2 fog still clears after each valid rep. Game2_FogVisual smooths this on Pico/Android.")]
    public bool updateFogOnEachAction = true;
    [Tooltip("Keep on: valid reps still show confetti. Game2_SunEnergyVisual uses Pico-safe confetti settings.")]
    public bool playSunResultEffects = true;

    [Header("Module References")]
    public Game2_ActionDetector actionDetector;
    public Game2_ScoreManager scoreManager;
    public Game2_FogVisual fogVisual;
    public Game2_SunEnergyVisual sunEnergyVisual;
    public Game2_IslandSceneVisual islandSceneVisual;
    public Game2_HandGuideVisual handGuideVisual;
    public Game2_UIController uiController;
    public Game2_AudioController audioController;
    public Game2_AvatarVisual avatarVisual;
    public TutorialVideoDisplay tutorialVideoDisplay;
    public XRRuntimeRigFixer xrRuntimeRigFixer;
    public ReturnToMainMenuInput returnInput;

    public bool IsPlaying { get; private set; }

    float remainingTime;
    float storedEnergy;
    bool hasSubmittedScore;
    Coroutine gameRoutine;

    void Awake()
    {
        if (actionDetector == null) actionDetector = GetOrAddComponent<Game2_ActionDetector>();
        if (scoreManager == null) scoreManager = GetOrAddComponent<Game2_ScoreManager>();
        if (fogVisual == null) fogVisual = GetOrAddComponent<Game2_FogVisual>();
        if (sunEnergyVisual == null) sunEnergyVisual = GetComponent<Game2_SunEnergyVisual>();
        if (islandSceneVisual == null) islandSceneVisual = GetOrAddComponent<Game2_IslandSceneVisual>();
        if (handGuideVisual == null) handGuideVisual = GetOrAddComponent<Game2_HandGuideVisual>();
        if (uiController == null) uiController = GetOrAddComponent<Game2_UIController>();
        if (audioController == null) audioController = GetOrAddComponent<Game2_AudioController>();
        if (avatarVisual == null) avatarVisual = GetOrAddComponent<Game2_AvatarVisual>();
        if (tutorialVideoDisplay == null) tutorialVideoDisplay = GetOrAddComponent<TutorialVideoDisplay>();
        if (xrRuntimeRigFixer == null) xrRuntimeRigFixer = GetOrAddComponent<XRRuntimeRigFixer>();
        if (returnInput == null) returnInput = GetOrAddComponent<ReturnToMainMenuInput>();
        if (xrRuntimeRigFixer != null) xrRuntimeRigFixer.Configure(actionDetector, handGuideVisual);
        if (returnInput != null)
        {
            returnInput.Configure(mainMenuSceneName, true, HandleReturnRequested);
            returnInput.SetReturnButtonText("\u8fd4\u56de\u4e3b\u83dc\u5355");
        }
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
        if (KeyboardInputCompat.GetKeyDown(restartKey))
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

    public void ReturnToMainMenu()
    {
        HandleReturnRequested();
    }

    IEnumerator GameRoutine()
    {
        IsPlaying = false;
        ConfigureModules();
        hasSubmittedScore = false;
        if (tutorialVideoDisplay != null) tutorialVideoDisplay.Stop();
        if (handGuideVisual != null) handGuideVisual.StopTutorialDemo();
        if (avatarVisual != null) avatarVisual.SetMode(Game2_AvatarMode.Idle);

        if (islandSceneVisual != null) islandSceneVisual.ApplySceneStyle();
        if (scoreManager != null) scoreManager.ResetScore();
        if (fogVisual != null) fogVisual.SetClearProgress(0f);
        if (uiController != null) uiController.ShowIntro(config);
        if (returnInput != null) returnInput.SetReturnButtonText("\u8fd4\u56de\u4e3b\u83dc\u5355");

        storedEnergy = 0f;
        if (sunEnergyVisual != null)
        {
            sunEnergyVisual.SetStoredEnergy(storedEnergy);
            sunEnergyVisual.SetActionProgress(0f);
        }

        remainingTime = config.roundTime;

        // 等待用户选择：观看教学 或 直接开始
        bool? watchTutorial = null;
        if (uiController != null)
        {
            uiController.OnTutorialChosen = () => watchTutorial = true;
            uiController.OnSkipTutorial = () => watchTutorial = false;

            while (!watchTutorial.HasValue)
            {
                if (KeyboardInputCompat.GetKeyDown(KeyCode.T)) watchTutorial = true;
                if (KeyboardInputCompat.GetKeyDown(KeyCode.Return)) watchTutorial = false;
                yield return null;
            }

            uiController.OnTutorialChosen = null;
            uiController.OnSkipTutorial = null;
            uiController.HideIntroButtons();
            if (returnInput != null) returnInput.SetReturnButtonText("\u8fd4\u56de\u9009\u62e9\u9875");
        }

        if (watchTutorial == true)
        {
            if (avatarVisual != null)
            {
                avatarVisual.SetTutorialMotion(3.2f);
                avatarVisual.SetMode(Game2_AvatarMode.Tutorial);
            }
            if (handGuideVisual != null) handGuideVisual.PlayTutorialDemo(3600f);
            if (audioController != null) audioController.PlayStart();

            if (tutorialVideoDisplay != null)
            {
                yield return tutorialVideoDisplay.PlayUntilFinished(tutorialVideoFileName);
            }
            else
            {
                yield return new WaitForSeconds(tutorialDuration);
            }

            if (tutorialVideoDisplay != null) tutorialVideoDisplay.Stop();
            if (handGuideVisual != null) handGuideVisual.StopTutorialDemo();
        }

        yield return WaitForStartAlignmentRoutine();

        if (avatarVisual != null)
        {
            avatarVisual.SetMode(Game2_AvatarMode.Tracking);
        }

        bool calibrated = actionDetector != null && actionDetector.Calibrate();
        if (!calibrated)
        {
            Debug.LogWarning("Func2 \u6821\u51c6\u5931\u8d25\uff0c\u8bf7\u68c0\u67e5\u5de6\u53f3\u624b\u67c4\u5f15\u7528\u3002");
            if (uiController != null) uiController.SetHint("\u6821\u51c6\u5931\u8d25\uff0c\u8bf7\u68c0\u67e5\u5de6\u53f3\u624b\u63a7\u5236\u5668\u5f15\u7528");
            if (audioController != null) audioController.PlayInvalid();
            yield break;
        }

        if (actionDetector != null && actionDetector.IsUsingKeyboardSimulator)
        {
            Debug.Log("Func2 \u5df2\u4ee5\u7535\u8111\u6a21\u62df\u6a21\u5f0f\u5f00\u59cb\uff1a\u6309\u4f4f\u7a7a\u683c\u952e\u6253\u5f00\u53cc\u81c2\uff0c\u677e\u5f00\u540e\u56de\u5230\u8d77\u70b9\u3002");
            Debug.Log("\u6309 R \u53ef\u4ee5\u91cd\u65b0\u5f00\u59cb Func2\u3002");
        }
        else
        {
            Debug.Log("Func2 \u5df2\u5f00\u59cb\uff0c\u8bf7\u5411\u4e24\u4fa7\u6253\u5f00\u53cc\u81c2\u3002");
        }

        IsPlaying = true;
        if (uiController != null) uiController.ShowPlaying();
        if (returnInput != null) returnInput.SetReturnButtonText("\u8fd4\u56de\u9009\u62e9\u9875");
        if (audioController != null) audioController.PlayCountdown();
        UpdateHud();
    }

    void ConfigureModules()
    {
        if (actionDetector != null) actionDetector.Configure(config);
        if (scoreManager != null) scoreManager.Configure(config);
        if (handGuideVisual != null) handGuideVisual.Configure(actionDetector);
        if (xrRuntimeRigFixer != null) xrRuntimeRigFixer.Configure(actionDetector, handGuideVisual);
    }

    IEnumerator WaitForStartAlignmentRoutine()
    {
        if (xrRuntimeRigFixer == null || actionDetector == null || actionDetector.IsUsingKeyboardSimulator)
        {
            yield break;
        }

        xrRuntimeRigFixer.SetStartAlignmentVisible(true);
        if (uiController != null)
        {
            uiController.ShowPlaying();
            uiController.SetHint("\u8bf7\u5c06\u5de6\u53f3\u624b\u653e\u5230\u5149\u5708\u9644\u8fd1\uff0c\u6309\u4f4f\u786e\u8ba4\u952e\u540e\u5f00\u59cb\u8bad\u7ec3");
            uiController.SetProgress(0f);
        }

        while (true)
        {
            bool ready = xrRuntimeRigFixer.TickStartAlignment(Time.deltaTime, out float progress);
            if (uiController != null) uiController.SetProgress(progress);
            if (ready) break;
            yield return null;
        }

        xrRuntimeRigFixer.SetStartAlignmentVisible(false);
        if (uiController != null)
        {
            uiController.SetProgress(0f);
            uiController.SetHint("\u5df2\u5bf9\u9f50\uff0c\u8bf7\u7ee7\u7eed\u6309\u4f4f\u786e\u8ba4\u952e\u5b8c\u6210\u52a8\u4f5c");
        }

        yield return new WaitForSeconds(0.25f);
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

        if (avatarVisual != null)
        {
            avatarVisual.SetProgress(progress);
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

            if (updateFogOnEachAction && fogVisual != null)
            {
                fogVisual.AddClearProgress(clearAmount);
            }

            storedEnergy = Mathf.Clamp01(storedEnergy + clearAmount);
            if (sunEnergyVisual != null) sunEnergyVisual.SetStoredEnergy(storedEnergy);
        }

        if (playSunResultEffects && sunEnergyVisual != null)
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

    void HandleReturnRequested()
    {
        bool onIntroPage = uiController != null && uiController.introPanel != null && uiController.introPanel.activeSelf;

        if (!onIntroPage)
        {
            ReturnToIntroPage();
            return;
        }

        SceneManager.LoadScene(mainMenuSceneName);
    }

    void ReturnToIntroPage()
    {
        if (gameRoutine != null)
        {
            StopCoroutine(gameRoutine);
            gameRoutine = null;
        }

        IsPlaying = false;
        hasSubmittedScore = false;
        remainingTime = 0f;
        storedEnergy = 0f;

        if (tutorialVideoDisplay != null) tutorialVideoDisplay.Stop();
        if (handGuideVisual != null) handGuideVisual.StopTutorialDemo();
        if (avatarVisual != null) avatarVisual.SetMode(Game2_AvatarMode.Idle);
        if (fogVisual != null) fogVisual.SetClearProgress(0f);
        if (sunEnergyVisual != null)
        {
            sunEnergyVisual.SetStoredEnergy(0f);
            sunEnergyVisual.SetActionProgress(0f);
        }
        if (scoreManager != null) scoreManager.ResetScore();
        if (uiController != null)
        {
            uiController.OnTutorialChosen = null;
            uiController.OnSkipTutorial = null;
        }

        BeginGame();
    }

    void EndGame()
    {
        if (!IsPlaying) return;

        IsPlaying = false;
        bool success = scoreManager != null && scoreManager.IsRoundSuccess();
        int finalScore = scoreManager != null ? scoreManager.RoundScore : 0;

        if (success)
        {
            if (updateFogOnEachAction && fogVisual != null)
            {
                fogVisual.SetClearProgress(1f);
            }
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
        if (returnInput != null) returnInput.SetReturnButtonText("\u8fd4\u56de\u9009\u62e9\u9875");

        if (audioController != null)
        {
            audioController.SetChargeProgress(0f);
            audioController.PlayFinish(success);
        }

        Debug.Log(success
            ? "Func2 \u8bad\u7ec3\u5b8c\u6210\u3002"
            : "Func2 \u8bad\u7ec3\u7ed3\u675f\uff0c\u53ef\u4ee5\u5c1d\u8bd5\u5b8c\u6210\u66f4\u591a\u6807\u51c6\u52a8\u4f5c\u3002");
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
