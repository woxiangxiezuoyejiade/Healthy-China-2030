using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Game1_UIController : MonoBehaviour
{
    const string StatusPanelResourcePath = "UI/Fun1_StatusPanel";
    const string InfoPanelResourcePath = "UI/Fun1_InfoPanel";
    static readonly Vector2 StatusPanelPosition = new Vector2(-232f, 112f);
    static readonly Vector2 StatusPanelSize = new Vector2(205f, 250f);
    static readonly Vector2 InfoPanelPosition = new Vector2(232f, 112f);
    static readonly Vector2 InfoPanelSize = new Vector2(205f, 238f);

    const string RequiredChineseCharacters =
        Game1_Text.HintSitFacingCore + Game1_Text.HintElbowNinety + Game1_Text.HintCalibrating +
        Game1_Text.HintCalibrationFailed + Game1_Text.HintRotateOut + Game1_Text.HintKeepRotating +
        Game1_Text.HintReturnStart + Game1_Text.HintAdjustPosture + Game1_Text.HintControllerMissing +
        Game1_Text.HintAngleTooLarge + Game1_Text.HintActionTooLong + Game1_Text.HintKeepElbowClose +
        Game1_Text.HintForearmStable + Game1_Text.HintDoNotSwingForward + Game1_Text.HintSlowDown +
        Game1_Text.HintFaceCore + Game1_Text.LabelTime + Game1_Text.LabelRoundScore +
        Game1_Text.LabelTotalScore + Game1_Text.LabelCompleted + Game1_Text.LabelCombo +
        Game1_Text.LabelComboEmpty + Game1_Text.LabelRightHand + Game1_Text.LabelLeftHand +
        Game1_Text.LabelTargetAngle + Game1_Text.GradeExcellent + Game1_Text.GradeGood +
        Game1_Text.GradeNeedsImprovement + Game1_Text.GradeInvalid + Game1_Text.ResultSuccess +
        Game1_Text.ResultKeepGoing + Game1_Text.ResultRoundScore + Game1_Text.ResultReps +
        Game1_Text.ResultBestCombo + Game1_Text.ResultAverage + Game1_Text.ResultCoreCharged +
        Game1_Text.ResultTryAgain +
        "\u5750\u6b63\u8098\u90e8\u5f2f\u66f290\u5ea6\u6309\u63d0\u793a\u8fdb\u884c\u80a9\u5916\u65cb\u8bad\u7ec3" +
        "\u6559\u5b66\u6f14\u793a\u8bf7\u89c2\u5bdf\u524d\u81c2\u5411\u5916\u65cb\u8f6c\u518d\u56de\u5230\u8d77\u70b9" +
        "\u89c2\u770b\u6559\u5b66\u76f4\u63a5\u5f00\u59cb" +
        "0123456789:%+-/x SpaceABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz：，。；、！？（）/\\°";

    [Header("Auto UI")]
    public bool autoCreateMissingUI = true;
    public bool attachAutoHudToCamera = true;
    public Vector3 autoHudCameraOffset = new Vector3(0f, -0.18f, 1.45f);
    public Vector3 autoHudPosition = new Vector3(0f, 1.55f, 1.6f);
    public Vector2 autoHudSize = new Vector2(560f, 360f);
    public float autoHudScale = 0.00135f;
    public bool preferRuntimeChineseFont = true;
    public bool forceSceneTextFont = true;
    public Color hudPanelTextColor = new Color(0.04f, 0.16f, 0.15f, 1f);
    public Color hintTextColor = new Color(0.05f, 0.07f, 0.08f, 1f);
    public Color resultTextColor = new Color(0.06f, 0.17f, 0.18f, 1f);
    public TMP_FontAsset fontOverride;

    [Header("HUD")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI repsText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI handText;
    public TextMeshProUGUI targetAngleText;
    public Slider rotationProgress;

    [Header("Intro Buttons")]
    public Button tutorialButton;
    public Button skipTutorialButton;

    public System.Action OnTutorialChosen;
    public System.Action OnSkipTutorial;

    [Header("Panels")]
    public GameObject introPanel;
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultScoreText;
    public TextMeshProUGUI resultDetailsText;
    public TextMeshProUGUI resultExtraText;

    TMP_FontAsset runtimeFontOverride;
    Transform autoHudRoot;
    [SerializeField] GameObject statusPanelObject;
    [SerializeField] GameObject infoPanelObject;
    [SerializeField] GameObject hintPanelObject;

    void Awake()
    {
        AutoFindFontOverride();

        if (autoCreateMissingUI)
        {
            CreateAutoUIIfNeeded();
        }

        AutoResolvePanelObjects();
        ApplyHudPanelBackgrounds();
        EnsureResultTextLayout();
        BindIntroButtons();
        ApplyFontOverride();
        ApplySceneFontOverride();
        ApplyHudPanelTextStyle();
        ApplyHintTextStyle();
        ApplyResultTextStyle();
    }

    public void ShowIntro(Game1_TrainingHand hand, Game1_DifficultyConfig config)
    {
        SetPanel(introPanel, true);
        SetPanel(resultPanel, false);
        SetPanel(statusPanelObject, false);
        SetPanel(infoPanelObject, false);
        SetPanel(hintPanelObject, false);
        SetTrainingInfo(hand, config);
        SetHint(Game1_Text.HintSitFacingCore);
        ShowIntroButtons();
    }

    public void ShowPlaying()
    {
        SetPanel(introPanel, false);
        SetPanel(resultPanel, false);
        SetPanel(statusPanelObject, true);
        SetPanel(infoPanelObject, true);
        SetPanel(hintPanelObject, true);
    }

    public void UpdateHud(float remainingTime, int roundScore, int totalScore, int validReps, int targetReps, int combo)
    {
        if (timerText != null) timerText.text = $"{Game1_Text.LabelTime} {Mathf.CeilToInt(remainingTime)}s";
        if (scoreText != null) scoreText.text = $"{Game1_Text.LabelRoundScore} {roundScore}";
        if (totalScoreText != null) totalScoreText.text = $"{Game1_Text.LabelTotalScore} {totalScore}";
        if (repsText != null) repsText.text = $"{Game1_Text.LabelCompleted} {validReps}/{targetReps}";
        if (comboText != null) comboText.text = combo > 1 ? $"{Game1_Text.LabelCombo} x{combo}" : Game1_Text.LabelComboEmpty;
    }

    public void SetTrainingInfo(Game1_TrainingHand hand, Game1_DifficultyConfig config)
    {
        if (handText != null) handText.text = hand == Game1_TrainingHand.Right ? Game1_Text.LabelRightHand : Game1_Text.LabelLeftHand;
        if (targetAngleText != null) targetAngleText.text = $"{Game1_Text.LabelTargetAngle}: {config.targetAngle:0}\u00b0";
    }

    public void SetProgress(float progress)
    {
        if (rotationProgress != null) rotationProgress.value = Mathf.Clamp01(progress);
    }

    public void SetHint(string message)
    {
        TryWarmCharacters(message);
        if (hintText != null)
        {
            hintText.text = message;
            hintText.color = hintTextColor;
        }
    }

    public void ShowGrade(Game1_ActionGrade grade, int actionScore)
    {
        if (gradeText == null) return;

        switch (grade)
        {
            case Game1_ActionGrade.Excellent:
                gradeText.text = $"{Game1_Text.GradeExcellent} +{actionScore}";
                gradeText.color = hudPanelTextColor;
                break;
            case Game1_ActionGrade.Good:
                gradeText.text = $"{Game1_Text.GradeGood} +{actionScore}";
                gradeText.color = hudPanelTextColor;
                break;
            case Game1_ActionGrade.NeedsImprovement:
                gradeText.text = $"{Game1_Text.GradeNeedsImprovement} +{actionScore}";
                gradeText.color = hudPanelTextColor;
                break;
            default:
                gradeText.text = Game1_Text.GradeInvalid;
                gradeText.color = hudPanelTextColor;
                break;
        }
    }

    public void ShowResult(int finalScore, int validReps, int targetReps, int bestCombo, float averageScore, bool success)
    {
        SetPanel(introPanel, false);
        SetPanel(statusPanelObject, false);
        SetPanel(infoPanelObject, false);
        SetPanel(hintPanelObject, false);
        SetPanel(resultPanel, true);

        if (resultTitleText != null)
        {
            resultTitleText.text = success ? Game1_Text.ResultSuccess : Game1_Text.ResultKeepGoing;
        }

        if (resultScoreText != null)
        {
            resultScoreText.text = $"{Game1_Text.ResultRoundScore} {finalScore}";
        }

        if (resultDetailsText != null)
        {
            resultDetailsText.text = $"{Game1_Text.ResultReps} {validReps}/{targetReps}";
        }

        if (resultExtraText != null)
        {
            resultExtraText.text = $"{Game1_Text.ResultBestCombo} x{bestCombo}    {Game1_Text.ResultAverage} {averageScore:0}";
        }

        ApplyResultTextStyle();
    }

    public void ShowIntroButtons()
    {
        if (tutorialButton != null) tutorialButton.gameObject.SetActive(true);
        if (skipTutorialButton != null) skipTutorialButton.gameObject.SetActive(true);
    }

    public void HideIntroButtons()
    {
        if (tutorialButton != null) tutorialButton.gameObject.SetActive(false);
        if (skipTutorialButton != null) skipTutorialButton.gameObject.SetActive(false);
    }

    public void ChooseTutorial()
    {
        OnTutorialChosen?.Invoke();
    }

    public void SkipTutorial()
    {
        OnSkipTutorial?.Invoke();
    }

    void BindIntroButtons()
    {
        if (tutorialButton != null)
        {
            tutorialButton.onClick.RemoveListener(ChooseTutorial);
            tutorialButton.onClick.AddListener(ChooseTutorial);
        }

        if (skipTutorialButton != null)
        {
            skipTutorialButton.onClick.RemoveListener(SkipTutorial);
            skipTutorialButton.onClick.AddListener(SkipTutorial);
        }
    }

    void AutoResolvePanelObjects()
    {
        if (statusPanelObject == null && timerText != null) statusPanelObject = timerText.transform.parent.gameObject;
        if (infoPanelObject == null && handText != null) infoPanelObject = handText.transform.parent.gameObject;
        if (hintPanelObject == null && hintText != null) hintPanelObject = hintText.transform.parent.gameObject;
    }

    Button CreateButton(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, string label, Color color)
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
        if (IsUsableFontAsset(fontOverride)) labelText.font = fontOverride;
        labelText.fontSize = 18;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.text = label;

        return button;
    }

    void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }

    void CreateAutoUIIfNeeded()
    {
        bool needsHud =
            timerText == null ||
            scoreText == null ||
            repsText == null ||
            hintText == null ||
            handText == null ||
            targetAngleText == null;

        if (!needsHud && resultPanel != null) return;

        GameObject canvasObject = new GameObject("Game1_AutoHUD");
        autoHudRoot = canvasObject.transform;
        Transform canvasParent = attachAutoHudToCamera && Camera.main != null ? Camera.main.transform : transform;
        canvasObject.transform.SetParent(canvasParent, false);
        canvasObject.transform.localPosition = canvasParent == transform ? autoHudPosition : autoHudCameraOffset;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * autoHudScale;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = autoHudSize;

        GameObject hudPanel = CreatePanel("HUD_Panel", canvasObject.transform, new Vector2(0f, 0f), autoHudSize, new Color(0.02f, 0.05f, 0.07f, 0.42f));
        Image hudImage = hudPanel.GetComponent<Image>();
        if (hudImage != null)
        {
            hudImage.color = new Color(0f, 0f, 0f, 0f);
            hudImage.raycastTarget = false;
        }

        GameObject statusPanel = CreatePanel("Status_Panel", hudPanel.transform, StatusPanelPosition, StatusPanelSize, Color.white);
        GameObject infoPanel = CreatePanel("Info_Panel", hudPanel.transform, InfoPanelPosition, InfoPanelSize, Color.white);
        GameObject hintPanel = CreatePanel("Hint_Panel", hudPanel.transform, new Vector2(0f, -122f), new Vector2(430f, 58f), new Color(0.02f, 0.05f, 0.07f, 0.28f));
        statusPanelObject = statusPanel;
        infoPanelObject = infoPanel;
        hintPanelObject = hintPanel;

        timerText = timerText != null ? timerText : CreateText("Timer_Text", statusPanel.transform, new Vector2(0f, 80f), 21, TextAlignmentOptions.Center, new Vector2(156f, 30f));
        scoreText = scoreText != null ? scoreText : CreateText("Score_Text", statusPanel.transform, new Vector2(0f, 40f), 21, TextAlignmentOptions.Center, new Vector2(156f, 30f));
        totalScoreText = totalScoreText != null ? totalScoreText : CreateText("TotalScore_Text", statusPanel.transform, new Vector2(0f, 0f), 19, TextAlignmentOptions.Center, new Vector2(156f, 30f));
        repsText = repsText != null ? repsText : CreateText("Reps_Text", statusPanel.transform, new Vector2(0f, -40f), 21, TextAlignmentOptions.Center, new Vector2(156f, 30f));
        comboText = comboText != null ? comboText : CreateText("Combo_Text", statusPanel.transform, new Vector2(0f, -80f), 19, TextAlignmentOptions.Center, new Vector2(156f, 30f));
        handText = handText != null ? handText : CreateText("Hand_Text", infoPanel.transform, new Vector2(0f, 62f), 22, TextAlignmentOptions.Center, new Vector2(158f, 34f));
        targetAngleText = targetAngleText != null ? targetAngleText : CreateText("TargetAngle_Text", infoPanel.transform, new Vector2(0f, 0f), 20, TextAlignmentOptions.Center, new Vector2(158f, 34f));
        gradeText = gradeText != null ? gradeText : CreateText("Grade_Text", infoPanel.transform, new Vector2(0f, -62f), 24, TextAlignmentOptions.Center, new Vector2(158f, 38f));
        hintText = hintText != null ? hintText : CreateText("Hint_Text", hintPanel.transform, new Vector2(0f, 8f), 20, TextAlignmentOptions.Center, new Vector2(390f, 34f));
        rotationProgress = rotationProgress != null ? rotationProgress : CreateSlider("Rotation_Progress", hintPanel.transform, new Vector2(0f, -20f), new Vector2(360f, 14f));

        introPanel = introPanel != null ? introPanel : CreatePanel("Intro_Panel", canvasObject.transform, new Vector2(0f, -80f), new Vector2(460f, 130f), new Color(1f, 1f, 1f, 0f));
        Image introImage = introPanel.GetComponent<Image>();
        if (introImage != null) introImage.raycastTarget = false;

        tutorialButton = tutorialButton != null ? tutorialButton : CreateButton("Tutorial_Button", introPanel.transform, new Vector2(-112f, 34f), new Vector2(210f, 50f), "\u89c2\u770b\u6559\u5b66", new Color(0.15f, 0.72f, 0.78f, 0.55f));
        skipTutorialButton = skipTutorialButton != null ? skipTutorialButton : CreateButton("SkipTutorial_Button", introPanel.transform, new Vector2(112f, 34f), new Vector2(210f, 50f), "\u76f4\u63a5\u5f00\u59cb", new Color(0.25f, 0.60f, 0.88f, 0.55f));

        BindIntroButtons();

        resultPanel = resultPanel != null ? resultPanel : CreatePanel("Result_Panel", canvasObject.transform, new Vector2(0f, 44f), new Vector2(360f, 510f), new Color(1f, 1f, 1f, 0.92f));
        resultTitleText = resultTitleText != null ? resultTitleText : CreateText("Result_Title_Text", resultPanel.transform, new Vector2(0f, 72f), 26, TextAlignmentOptions.Center, new Vector2(255f, 36f));
        resultScoreText = resultScoreText != null ? resultScoreText : CreateText("Result_Score_Text", resultPanel.transform, new Vector2(0f, 19f), 20, TextAlignmentOptions.Center, new Vector2(255f, 34f));
        resultDetailsText = resultDetailsText != null ? resultDetailsText : CreateText("Result_Details_Text", resultPanel.transform, new Vector2(0f, -34f), 20, TextAlignmentOptions.Center, new Vector2(255f, 34f));
        resultExtraText = resultExtraText != null ? resultExtraText : CreateText("Result_Extra_Text", resultPanel.transform, new Vector2(0f, -87f), 18, TextAlignmentOptions.Center, new Vector2(270f, 34f));
        if (resultDetailsText != null)
        {
            resultDetailsText.enableWordWrapping = false;
            resultDetailsText.lineSpacing = 0f;
        }
        if (resultExtraText != null)
        {
            resultExtraText.enableWordWrapping = false;
            resultExtraText.lineSpacing = 0f;
        }
        resultPanel.SetActive(false);
        ApplyFontOverride();
        ApplySceneFontOverride();
        ApplyHudPanelBackgrounds();
        ApplyHudPanelTextStyle();
        ApplyHintTextStyle();
    }

    void ApplyHudPanelBackgrounds()
    {
        ApplyHudPanelBackground(statusPanelObject, StatusPanelResourcePath, StatusPanelPosition, StatusPanelSize);
        ApplyHudPanelBackground(infoPanelObject, InfoPanelResourcePath, InfoPanelPosition, InfoPanelSize);
        LayoutHudPanelTexts();
    }

    void ApplyHudPanelBackground(GameObject panel, string resourcePath, Vector2 position, Vector2 size)
    {
        if (panel == null) return;

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        Image image = panel.GetComponent<Image>();
        if (image == null) image = panel.AddComponent<Image>();

        Sprite sprite = LoadSprite(resourcePath);
        if (sprite != null)
        {
            image.sprite = sprite;
        }

        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    void LayoutHudPanelTexts()
    {
        ConfigureHudText(timerText, new Vector2(0f, 80f), new Vector2(156f, 30f), 21, TextAlignmentOptions.Center);
        ConfigureHudText(scoreText, new Vector2(0f, 40f), new Vector2(156f, 30f), 21, TextAlignmentOptions.Center);
        ConfigureHudText(totalScoreText, new Vector2(0f, 0f), new Vector2(156f, 30f), 19, TextAlignmentOptions.Center);
        ConfigureHudText(repsText, new Vector2(0f, -40f), new Vector2(156f, 30f), 21, TextAlignmentOptions.Center);
        ConfigureHudText(comboText, new Vector2(0f, -80f), new Vector2(156f, 30f), 19, TextAlignmentOptions.Center);

        ConfigureHudText(handText, new Vector2(0f, 62f), new Vector2(158f, 34f), 22, TextAlignmentOptions.Center);
        ConfigureHudText(targetAngleText, new Vector2(0f, 0f), new Vector2(158f, 34f), 20, TextAlignmentOptions.Center);
        ConfigureHudText(gradeText, new Vector2(0f, -62f), new Vector2(158f, 38f), 24, TextAlignmentOptions.Center);
    }

    void ConfigureHudText(TextMeshProUGUI text, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
    {
        if (text == null) return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        text.fontSize = fontSize;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.transform.SetAsLastSibling();
    }

    static Sprite LoadSprite(string resourcePath)
    {
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null) return null;

        Rect rect = new Rect(0f, 0f, texture.width, texture.height);
        return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
    }

    void EnsureResultTextLayout()
    {
        if (resultPanel == null) return;

        RectTransform resultRect = resultPanel.GetComponent<RectTransform>();
        if (resultRect != null)
        {
            resultRect.anchoredPosition = new Vector2(resultRect.anchoredPosition.x, 44f);
            resultRect.sizeDelta = new Vector2(360f, 510f);
        }

        Transform resultRoot = resultPanel.transform;
        resultTitleText = resultTitleText != null ? resultTitleText : FindChildText(resultRoot, "Result_Title_Text");
        resultScoreText = resultScoreText != null ? resultScoreText : FindChildText(resultRoot, "Result_Score_Text");
        resultDetailsText = resultDetailsText != null ? resultDetailsText : FindChildText(resultRoot, "Result_Details_Text");
        resultExtraText = resultExtraText != null ? resultExtraText : FindChildText(resultRoot, "Result_Extra_Text");

        if (resultExtraText == null)
        {
            resultExtraText = CreateText("Result_Extra_Text", resultRoot, new Vector2(0f, -87f), 18, TextAlignmentOptions.Center, new Vector2(270f, 34f));
            if (resultDetailsText != null)
            {
                resultExtraText.font = resultDetailsText.font;
                resultExtraText.material = resultDetailsText.material;
            }
            resultExtraText.text = $"{Game1_Text.ResultBestCombo} x0    {Game1_Text.ResultAverage} 0";
        }

        ConfigureResultText(resultTitleText, new Vector2(0f, 72f), new Vector2(255f, 36f), 26);
        ConfigureResultText(resultScoreText, new Vector2(0f, 19f), new Vector2(255f, 34f), 20);
        ConfigureResultText(resultDetailsText, new Vector2(0f, -34f), new Vector2(255f, 34f), 20);
        ConfigureResultText(resultExtraText, new Vector2(0f, -87f), new Vector2(270f, 34f), 18);

        if (resultDetailsText != null && resultDetailsText.text.Contains("\n"))
        {
            resultDetailsText.text = $"{Game1_Text.ResultReps} 0/12";
        }
    }

    TextMeshProUGUI FindChildText(Transform root, string childName)
    {
        if (root == null) return null;

        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.name == childName) return text;
        }

        return null;
    }

    void ConfigureResultText(TextMeshProUGUI text, Vector2 position, Vector2 size, int fontSize)
    {
        if (text == null) return;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.lineSpacing = 0f;
        text.raycastTarget = false;
        text.color = resultTextColor;
    }

    GameObject CreatePanel(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(objectName);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;

        return panel;
    }

    void ApplyFontOverride()
    {
        if (!IsUsableFontAsset(fontOverride)) return;

        PrepareFontOverride();

        TextMeshProUGUI[] texts =
        {
            timerText,
            scoreText,
            totalScoreText,
            repsText,
            comboText,
            gradeText,
            hintText,
            handText,
            targetAngleText,
            resultTitleText,
            resultScoreText,
            resultDetailsText,
            resultExtraText
        };

        foreach (TextMeshProUGUI text in texts)
        {
            ApplyFontToText(text);
        }

        if (autoHudRoot != null)
        {
            TextMeshProUGUI[] childTexts = autoHudRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in childTexts)
            {
                ApplyFontToText(text);
            }
        }
    }

    void ApplySceneFontOverride()
    {
        if (!forceSceneTextFont || fontOverride == null) return;

        TextMeshProUGUI[] sceneTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in sceneTexts)
        {
            ApplyFontToText(text);
        }
    }

    void TryWarmCharacters(string message)
    {
        if (!IsUsableFontAsset(fontOverride) || string.IsNullOrEmpty(message)) return;

        fontOverride.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontOverride.TryAddCharacters(message, out _);
    }

    void ApplyHintTextStyle()
    {
        if (hintText == null) return;

        hintText.color = hintTextColor;
        if (fontOverride != null) ApplyFontToText(hintText);
    }

    void ApplyHudPanelTextStyle()
    {
        TextMeshProUGUI[] texts =
        {
            timerText,
            scoreText,
            totalScoreText,
            repsText,
            comboText,
            handText,
            targetAngleText,
            gradeText
        };

        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null) continue;

            text.color = hudPanelTextColor;
            text.fontStyle = FontStyles.Bold;
            if (fontOverride != null) ApplyFontToText(text);
        }
    }

    void ApplyResultTextStyle()
    {
        ApplyResultTextStyle(resultTitleText);
        ApplyResultTextStyle(resultScoreText);
        ApplyResultTextStyle(resultDetailsText);
        ApplyResultTextStyle(resultExtraText);
    }

    void ApplyResultTextStyle(TextMeshProUGUI text)
    {
        if (text == null) return;

        text.color = resultTextColor;
        text.fontStyle = FontStyles.Bold;
        if (fontOverride != null) ApplyFontToText(text);
    }
    void AutoFindFontOverride()
    {
        if (fontOverride != null && !preferRuntimeChineseFont && fontOverride.name.Contains("HanyiHuaMulanW SDF"))
        {
            if (IsUsableFontAsset(fontOverride)) return;
            fontOverride = null;
        }

#if UNITY_EDITOR
        fontOverride = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Fonts/HanyiHuaMulanW SDF.asset");
        if (IsUsableFontAsset(fontOverride)) return;
        fontOverride = null;

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Project/Fonts/HanyiHuaMulanW.ttf");
        if (sourceFont != null)
        {
            runtimeFontOverride = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                4096,
                4096,
                AtlasPopulationMode.Dynamic);
            runtimeFontOverride.name = "Runtime_HanyiHuaMulan_Game1";
            fontOverride = runtimeFontOverride;
            return;
        }

#endif
    }

    void PrepareFontOverride()
    {
        if (!IsUsableFontAsset(fontOverride)) return;

        fontOverride.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontOverride.TryAddCharacters(RequiredChineseCharacters, out _);

        if (fontOverride.fallbackFontAssetTable != null)
        {
            fontOverride.fallbackFontAssetTable.Clear();
        }
    }

    void ApplyFontToText(TextMeshProUGUI text)
    {
        if (text == null || !IsUsableFontAsset(fontOverride)) return;

        text.font = fontOverride;
        text.fontSharedMaterial = fontOverride.material;
        text.enableWordWrapping = false;
    }

    static bool IsUsableFontAsset(TMP_FontAsset fontAsset)
    {
        try
        {
            return fontAsset != null &&
                   fontAsset.material != null &&
                   fontAsset.atlasTexture != null;
        }
        catch
        {
            return false;
        }
    }

    TextMeshProUGUI CreateText(string objectName, Transform parent, Vector2 anchoredPosition, int fontSize, TextAlignmentOptions alignment, Vector2? size = null)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size ?? new Vector2(470f, 42f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (IsUsableFontAsset(fontOverride)) text.font = fontOverride;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.text = "";

        return text;
    }

    Slider CreateSlider(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject sliderObject = new GameObject(objectName);
        sliderObject.transform.SetParent(parent, false);

        RectTransform rect = sliderObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(sliderObject.transform, false);
        RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;
        Image background = backgroundObject.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.18f);

        GameObject fillAreaObject = new GameObject("Fill Area");
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fill = fillObject.AddComponent<Image>();
        fill.color = new Color(1f, 0.82f, 0.25f, 0.92f);

        slider.fillRect = fillRect;
        slider.targetGraphic = fill;
        slider.interactable = false;

        return slider;
    }
}
