using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Game1_UIController : MonoBehaviour
{
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
        "\u6559\u5b66\u6f14\u793a\u8bf7\u89c2\u5bdf\u524d\u81c2\u5411\u5916\u65cb\u8f6c\u518d\u56de\u5230\u8d77\u70b9";

    [Header("Auto UI")]
    public bool autoCreateMissingUI = true;
    public bool attachAutoHudToCamera = true;
    public Vector3 autoHudCameraOffset = new Vector3(0f, -0.18f, 1.45f);
    public Vector3 autoHudPosition = new Vector3(0f, 1.55f, 1.6f);
    public Vector2 autoHudSize = new Vector2(560f, 360f);
    public float autoHudScale = 0.00135f;
    public bool preferRuntimeChineseFont = true;
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

    [Header("Panels")]
    public GameObject introPanel;
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultScoreText;
    public TextMeshProUGUI resultDetailsText;

    TMP_FontAsset runtimeFontOverride;
    Transform autoHudRoot;
    GameObject statusPanelObject;
    GameObject infoPanelObject;
    GameObject hintPanelObject;

    void Awake()
    {
        AutoFindFontOverride();

        if (autoCreateMissingUI)
        {
            CreateAutoUIIfNeeded();
        }

        ApplyFontOverride();
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
        if (hintText != null) hintText.text = message;
    }

    public void ShowGrade(Game1_ActionGrade grade, int actionScore)
    {
        if (gradeText == null) return;

        switch (grade)
        {
            case Game1_ActionGrade.Excellent:
                gradeText.text = $"{Game1_Text.GradeExcellent} +{actionScore}";
                gradeText.color = Color.white;
                break;
            case Game1_ActionGrade.Good:
                gradeText.text = $"{Game1_Text.GradeGood} +{actionScore}";
                gradeText.color = Color.white;
                break;
            case Game1_ActionGrade.NeedsImprovement:
                gradeText.text = $"{Game1_Text.GradeNeedsImprovement} +{actionScore}";
                gradeText.color = Color.white;
                break;
            default:
                gradeText.text = Game1_Text.GradeInvalid;
                gradeText.color = Color.white;
                break;
        }
    }

    public void ShowResult(int finalScore, int validReps, int targetReps, int bestCombo, float averageScore, bool success)
    {
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
            resultDetailsText.text =
                $"{Game1_Text.ResultReps} {validReps}/{targetReps}\n" +
                $"{Game1_Text.ResultBestCombo} x{bestCombo}\n" +
                $"{Game1_Text.ResultAverage} {averageScore:0}\n" +
                (success ? Game1_Text.ResultCoreCharged : Game1_Text.ResultTryAgain);
        }
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

        GameObject statusPanel = CreatePanel("Status_Panel", hudPanel.transform, new Vector2(-175f, 72f), new Vector2(185f, 205f), new Color(0.02f, 0.05f, 0.07f, 0.30f));
        GameObject infoPanel = CreatePanel("Info_Panel", hudPanel.transform, new Vector2(102f, 96f), new Vector2(235f, 110f), new Color(0.02f, 0.05f, 0.07f, 0.22f));
        GameObject hintPanel = CreatePanel("Hint_Panel", hudPanel.transform, new Vector2(0f, -122f), new Vector2(430f, 58f), new Color(0.02f, 0.05f, 0.07f, 0.28f));
        statusPanelObject = statusPanel;
        infoPanelObject = infoPanel;
        hintPanelObject = hintPanel;

        timerText = timerText != null ? timerText : CreateText("Timer_Text", statusPanel.transform, new Vector2(0f, 75f), 20, TextAlignmentOptions.Left, new Vector2(150f, 28f));
        scoreText = scoreText != null ? scoreText : CreateText("Score_Text", statusPanel.transform, new Vector2(0f, 40f), 20, TextAlignmentOptions.Left, new Vector2(150f, 28f));
        totalScoreText = totalScoreText != null ? totalScoreText : CreateText("TotalScore_Text", statusPanel.transform, new Vector2(0f, 5f), 18, TextAlignmentOptions.Left, new Vector2(150f, 28f));
        repsText = repsText != null ? repsText : CreateText("Reps_Text", statusPanel.transform, new Vector2(0f, -30f), 20, TextAlignmentOptions.Left, new Vector2(150f, 28f));
        comboText = comboText != null ? comboText : CreateText("Combo_Text", statusPanel.transform, new Vector2(0f, -66f), 18, TextAlignmentOptions.Left, new Vector2(150f, 28f));
        handText = handText != null ? handText : CreateText("Hand_Text", infoPanel.transform, new Vector2(0f, 28f), 21, TextAlignmentOptions.Center, new Vector2(205f, 30f));
        targetAngleText = targetAngleText != null ? targetAngleText : CreateText("TargetAngle_Text", infoPanel.transform, new Vector2(0f, -8f), 19, TextAlignmentOptions.Center, new Vector2(205f, 30f));
        gradeText = gradeText != null ? gradeText : CreateText("Grade_Text", infoPanel.transform, new Vector2(0f, -44f), 24, TextAlignmentOptions.Center, new Vector2(205f, 34f));
        hintText = hintText != null ? hintText : CreateText("Hint_Text", hintPanel.transform, new Vector2(0f, 8f), 20, TextAlignmentOptions.Center, new Vector2(390f, 34f));
        rotationProgress = rotationProgress != null ? rotationProgress : CreateSlider("Rotation_Progress", hintPanel.transform, new Vector2(0f, -20f), new Vector2(360f, 14f));

        introPanel = introPanel != null ? introPanel : CreatePanel("Intro_Panel", canvasObject.transform, new Vector2(0f, -80f), new Vector2(460f, 86f), new Color(0.06f, 0.14f, 0.16f, 0.20f));
        TextMeshProUGUI introText = CreateText("Intro_Text", introPanel.transform, new Vector2(0f, 0f), 22, TextAlignmentOptions.Center, new Vector2(420f, 58f));
        introText.text = "\u5750\u6b63\uff0c\u8098\u90e8\u5f2f\u66f2 90 \u5ea6\uff0c\u6309\u63d0\u793a\u8fdb\u884c\u80a9\u5916\u65cb\u8bad\u7ec3";

        resultPanel = resultPanel != null ? resultPanel : CreatePanel("Result_Panel", canvasObject.transform, new Vector2(0f, 0f), autoHudSize, new Color(0.03f, 0.06f, 0.08f, 0.78f));
        resultTitleText = resultTitleText != null ? resultTitleText : CreateText("Result_Title_Text", resultPanel.transform, new Vector2(0f, 92f), 34, TextAlignmentOptions.Center);
        resultScoreText = resultScoreText != null ? resultScoreText : CreateText("Result_Score_Text", resultPanel.transform, new Vector2(0f, 34f), 28, TextAlignmentOptions.Center);
        resultDetailsText = resultDetailsText != null ? resultDetailsText : CreateText("Result_Details_Text", resultPanel.transform, new Vector2(0f, -58f), 22, TextAlignmentOptions.Center);
        resultPanel.SetActive(false);
        ApplyFontOverride();
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
        if (fontOverride == null) return;

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
            resultDetailsText
        };

        foreach (TextMeshProUGUI text in texts)
        {
            if (text != null) text.font = fontOverride;
        }

        if (autoHudRoot != null)
        {
            TextMeshProUGUI[] childTexts = autoHudRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in childTexts)
            {
                text.font = fontOverride;
                text.color = Color.white;
            }
        }
    }

    void AutoFindFontOverride()
    {
        if (fontOverride != null && !preferRuntimeChineseFont) return;

#if UNITY_EDITOR
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Project/Fonts/msyh.ttc");
        if (sourceFont != null)
        {
            runtimeFontOverride = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic);
            runtimeFontOverride.name = "Runtime_Microsoft_YaHei_Game1";
            fontOverride = runtimeFontOverride;
            return;
        }

        fontOverride = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Fonts/msyh SDF.asset");
#endif
    }

    void PrepareFontOverride()
    {
        if (fontOverride == null) return;

        fontOverride.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontOverride.TryAddCharacters(RequiredChineseCharacters, out _);
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
        if (fontOverride != null) text.font = fontOverride;
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
