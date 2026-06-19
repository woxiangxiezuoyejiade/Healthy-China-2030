using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Game2_UIController : MonoBehaviour
{
    const string TextReady = "\u51c6\u5907\u5f00\u59cb\uff1a\u7f13\u6162\u5411\u4e24\u4fa7\u6253\u5f00\u53cc\u81c2\uff0c\u6536\u96c6\u9633\u5149\u80fd\u91cf";
    const string TextPlaying = "\u8bf7\u6309\u4f4f\u624b\u67c4\u786e\u8ba4\u952e\uff0c\u53cc\u624b\u5411\u4e24\u4fa7\u6253\u5f00\uff0c\u677e\u5f00\u540e\u56de\u5230\u8d77\u70b9";
    const string TextTime = "\u65f6\u95f4";
    const string TextRoundScore = "\u672c\u5c40";
    const string TextTotalScore = "\u603b\u5206";
    const string TextReps = "\u5b8c\u6210";
    const string TextCombo = "\u8fde\u51fb";
    const string TextSunlight = "\u9633\u5149\u80fd\u91cf";
    const string TextExcellent = "\u4f18\u79c0";
    const string TextGood = "\u826f\u597d";
    const string TextKeepGoing = "\u7ee7\u7eed\u52a0\u6cb9";
    const string TextTryAgain = "\u518d\u8bd5\u4e00\u6b21";
    const string TextTrainingComplete = "\u8bad\u7ec3\u5b8c\u6210";
    const string TextKeepPracticing = "\u7ee7\u7eed\u7ec3\u4e60";
    const string TextFinalScore = "\u6700\u7ec8\u5f97\u5206";
    const string TextBestCombo = "\u6700\u9ad8\u8fde\u51fb";
    const string TextAverageQuality = "\u5e73\u5747\u8d28\u91cf";
    const string TextTutorial = "\u6559\u5b66\u6f14\u793a\uff1a\u8bf7\u89c2\u5bdf\u53cc\u624b\u5411\u4e24\u4fa7\u5c55\u5f00\uff0c\u518d\u56de\u5230\u8d77\u70b9";
    const string RequiredChineseCharacters =
        TextReady + TextPlaying + TextTime + TextRoundScore + TextTotalScore + TextReps +
        TextCombo + TextSunlight + TextExcellent + TextGood + TextKeepGoing + TextTryAgain +
        TextTrainingComplete + TextKeepPracticing + TextFinalScore + TextBestCombo +
        TextAverageQuality + TextTutorial +
        "\u8282\u594f\u5f88\u597d\u8fde\u51fb\u4fdd\u6301\u4f4f\u52a8\u4f5c\u826f\u597d\u4f18\u79c0" +
        "\u6162\u6162\u56de\u5230\u8d77\u70b9\u5df2\u7ecf\u5b8c\u6210\u4e00\u90e8\u5206" +
        "\u53ef\u4ee5\u518d\u6253\u5f00\u4e00\u4e9b\u5e76\u4fdd\u6301\u53cc\u624b\u540c\u9ad8" +
        "\u65e0\u6548\u540e\u6269\u80f8\u8fd0\u52a8\u8bad\u7ec3\u7f13\u6162\u5411\u4e24\u4fa7" +
        "\u6253\u5f00\u53cc\u81c2\u9a71\u6563\u8584\u96fe\u51c6\u5907\u9636\u6bb5\u6821\u51c6\u5931\u8d25\u68c0\u67e5\u63a7\u5236\u5668\u5f15\u7528" +
        "\u89c2\u770b\u6559\u5b66\u76f4\u63a5\u5f00\u59cb\u624b\u67c4\u786e\u8ba4\u952e\u677e\u5f00\u79d2\u6b21\u8eab\u4f53\u9762\u5411\u524d\u65b9\u624b\u62ac\u5f97\u8fc7\u9ad8\u624b\u653e\u5f97\u8fc7\u4f4e";

    [Header("Auto UI")]
    public bool autoCreateMissingUI = true;
    public bool attachAutoHudToCamera = true;
    public Vector3 autoHudCameraOffset = new Vector3(0f, -0.14f, 1.45f);
    public Vector3 autoHudPosition = new Vector3(0f, 1.55f, 1.6f);
    public Vector2 autoHudSize = new Vector2(720f, 440f);
    public float autoHudScale = 0.00125f;
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
    public TextMeshProUGUI energyText;
    public Slider actionProgress;
    public Slider energyProgress;

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

    Coroutine gradeRoutine;
    TMP_FontAsset runtimeFontOverride;
    Transform autoHudRoot;
    [SerializeField] GameObject statusPanelObject;
    [SerializeField] GameObject energyPanelObject;
    [SerializeField] GameObject hintPanelObject;

    void Awake()
    {
        AutoFindFontOverride();

        if (autoCreateMissingUI)
        {
            CreateAutoUIIfNeeded();
        }

        ApplyFontOverride();
        AutoResolvePanelObjects();
        BindIntroButtons();
        ApplyHudPanelTextStyle();
        ApplyHintTextStyle();
        ApplyResultTextStyle();
    }

    public void ShowIntro(Game2_DifficultyConfig config)
    {
        SetPanel(introPanel, true);
        SetPanel(resultPanel, false);
        SetPanel(statusPanelObject, false);
        SetPanel(energyPanelObject, false);
        SetPanel(hintPanelObject, false);
        SetHint(TextTutorial);
        if (gradeText != null) gradeText.text = "";
        SetProgress(0f);
        SetEnergy(0f);
        ShowIntroButtons();
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

    public void ShowPlaying()
    {
        SetPanel(introPanel, false);
        SetPanel(resultPanel, false);
        SetPanel(statusPanelObject, true);
        SetPanel(energyPanelObject, true);
        SetPanel(hintPanelObject, true);
        SetHint(TextPlaying);
    }

    public void UpdateHud(float remainingTime, int roundScore, int totalScore, int validReps, int targetReps, int combo, float energy)
    {
        if (timerText != null) timerText.text = $"{TextTime}  {Mathf.CeilToInt(remainingTime)}\u79d2";
        if (scoreText != null) scoreText.text = $"{TextRoundScore}  {roundScore}";
        if (totalScoreText != null) totalScoreText.text = $"{TextTotalScore}  {totalScore}";
        if (repsText != null) repsText.text = $"{TextReps}  {validReps}/{targetReps}";
        if (comboText != null) comboText.text = combo > 1 ? $"{TextCombo}  {combo}\u6b21" : $"{TextCombo}  -";
        SetEnergy(energy);
    }

    public void SetProgress(float progress)
    {
        if (actionProgress != null) actionProgress.value = Mathf.Clamp01(progress);
    }

    public void SetEnergy(float energy)
    {
        float value = Mathf.Clamp01(energy);
        if (energyProgress != null) energyProgress.value = value;
        if (energyText != null) energyText.text = $"{TextSunlight}  {Mathf.RoundToInt(value * 100f)}%";
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

    public void ShowActionResult(Game2_ActionGrade grade, int actionScore, int combo)
    {
        if (gradeText == null) return;

        if (gradeRoutine != null) StopCoroutine(gradeRoutine);

        switch (grade)
        {
            case Game2_ActionGrade.Excellent:
                gradeText.text = $"{TextExcellent}  +{actionScore}";
                gradeText.color = hudPanelTextColor;
                SetHint(combo >= 3
                    ? $"\u8282\u594f\u5f88\u597d\uff0c\u8fde\u51fb {combo}\u6b21\uff01"
                    : "\u52a8\u4f5c\u4f18\u79c0\uff0c\u6162\u6162\u56de\u5230\u8d77\u70b9");
                break;
            case Game2_ActionGrade.Good:
                gradeText.text = $"{TextGood}  +{actionScore}";
                gradeText.color = hudPanelTextColor;
                SetHint(combo >= 3
                    ? $"\u4fdd\u6301\u4f4f\uff0c\u8fde\u51fb {combo}\u6b21\uff01"
                    : "\u52a8\u4f5c\u826f\u597d\uff0c\u6162\u6162\u56de\u5230\u8d77\u70b9");
                break;
            case Game2_ActionGrade.NeedsImprovement:
                gradeText.text = $"{TextKeepGoing}  +{actionScore}";
                gradeText.color = hudPanelTextColor;
                SetHint("\u5df2\u7ecf\u5b8c\u6210\u4e00\u90e8\u5206\uff0c\u53ef\u4ee5\u518d\u6253\u5f00\u4e00\u4e9b\uff0c\u5e76\u4fdd\u6301\u53cc\u624b\u540c\u9ad8");
                break;
            default:
                gradeText.text = TextTryAgain;
                gradeText.color = hudPanelTextColor;
                SetHint("\u52a8\u4f5c\u65e0\u6548\uff0c\u56de\u5230\u8d77\u70b9\u540e\u518d\u8bd5\u4e00\u6b21");
                break;
        }

        gradeRoutine = StartCoroutine(GradePulseRoutine());
    }

    public void ShowResult(int finalScore, int validReps, int targetReps, int bestCombo, float averageScore, bool success)
    {
        SetPanel(resultPanel, true);

        if (resultTitleText != null)
        {
            resultTitleText.text = success ? TextTrainingComplete : TextKeepPracticing;
        }

        if (resultScoreText != null)
        {
            resultScoreText.text = $"{TextFinalScore}  {finalScore}";
        }

        if (resultDetailsText != null)
        {
            resultDetailsText.text =
                $"{TextReps}  {validReps}/{targetReps}\n" +
                $"{TextBestCombo}  {bestCombo}\u6b21\n" +
                $"{TextAverageQuality}  {averageScore:0}";
        }

        ApplyResultTextStyle();
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
        if (energyPanelObject == null && energyText != null) energyPanelObject = energyText.transform.parent.gameObject;
        if (hintPanelObject == null && hintText != null) hintPanelObject = hintText.transform.parent.gameObject;
    }

    IEnumerator GradePulseRoutine()
    {
        if (gradeText == null) yield break;

        Vector3 baseScale = gradeText.rectTransform.localScale;
        Color baseColor = gradeText.color;

        for (float t = 0f; t < 0.18f; t += Time.deltaTime)
        {
            float k = Mathf.Sin((t / 0.18f) * Mathf.PI);
            gradeText.rectTransform.localScale = baseScale * Mathf.Lerp(1f, 1.18f, k);
            yield return null;
        }

        gradeText.rectTransform.localScale = baseScale;
        gradeText.color = baseColor;
        yield return new WaitForSeconds(1.2f);

        if (gradeText != null)
        {
            gradeText.text = "";
        }
    }

    void CreateAutoUIIfNeeded()
    {
        bool needsHud =
            timerText == null ||
            scoreText == null ||
            repsText == null ||
            hintText == null ||
            gradeText == null ||
            actionProgress == null;

        if (!needsHud && resultPanel != null) return;

        GameObject canvasObject = new GameObject("Game2_AutoHUD");
        autoHudRoot = canvasObject.transform;
        Transform canvasParent = attachAutoHudToCamera && Camera.main != null ? Camera.main.transform : transform;
        canvasObject.transform.SetParent(canvasParent, false);
        canvasObject.transform.localPosition = canvasParent == transform ? autoHudPosition : autoHudCameraOffset;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * autoHudScale;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 25;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = autoHudSize;

        GameObject hudRoot = CreatePanel("HUD_Root", canvasObject.transform, Vector2.zero, autoHudSize, new Color(0f, 0f, 0f, 0f));
        Image rootImage = hudRoot.GetComponent<Image>();
        if (rootImage != null) rootImage.raycastTarget = false;

        GameObject statusPanel = CreatePanel("Status_Panel", hudRoot.transform, new Vector2(-270f, 205f), new Vector2(205f, 235f), new Color(1f, 1f, 1f, 0.70f));
        GameObject energyPanel = CreatePanel("Energy_Panel", hudRoot.transform, new Vector2(270f, 205f), new Vector2(235f, 160f), new Color(1f, 1f, 1f, 0.70f));
        GameObject hintPanel = CreatePanel("Hint_Panel", hudRoot.transform, new Vector2(0f, -154f), new Vector2(545f, 92f), new Color(1f, 1f, 1f, 0.70f));
        statusPanelObject = statusPanel;
        energyPanelObject = energyPanel;
        hintPanelObject = hintPanel;

        timerText = timerText != null ? timerText : CreateText("Timer_Text", statusPanel.transform, new Vector2(0f, 90f), 24, TextAlignmentOptions.Left, new Vector2(174f, 32f));
        scoreText = scoreText != null ? scoreText : CreateText("Score_Text", statusPanel.transform, new Vector2(0f, 47f), 24, TextAlignmentOptions.Left, new Vector2(174f, 32f));
        totalScoreText = totalScoreText != null ? totalScoreText : CreateText("TotalScore_Text", statusPanel.transform, new Vector2(0f, 4f), 22, TextAlignmentOptions.Left, new Vector2(174f, 32f));
        repsText = repsText != null ? repsText : CreateText("Reps_Text", statusPanel.transform, new Vector2(0f, -39f), 24, TextAlignmentOptions.Left, new Vector2(174f, 32f));
        comboText = comboText != null ? comboText : CreateText("Combo_Text", statusPanel.transform, new Vector2(0f, -82f), 22, TextAlignmentOptions.Left, new Vector2(174f, 32f));

        energyText = energyText != null ? energyText : CreateText("Energy_Text", energyPanel.transform, new Vector2(0f, 43f), 24, TextAlignmentOptions.Center, new Vector2(202f, 34f));
        gradeText = gradeText != null ? gradeText : CreateText("Grade_Text", energyPanel.transform, new Vector2(0f, -8f), 30, TextAlignmentOptions.Center, new Vector2(202f, 40f));
        energyProgress = energyProgress != null ? energyProgress : CreateSlider("Energy_Progress", energyPanel.transform, new Vector2(0f, -57f), new Vector2(184f, 16f), new Color(1f, 0.83f, 0.25f, 0.92f));

        hintText = hintText != null ? hintText : CreateText("Hint_Text", hintPanel.transform, new Vector2(0f, 14f), 23, TextAlignmentOptions.Center, new Vector2(498f, 34f));
        actionProgress = actionProgress != null ? actionProgress : CreateSlider("Action_Progress", hintPanel.transform, new Vector2(0f, -25f), new Vector2(462f, 16f), new Color(0.35f, 0.78f, 1f, 0.92f));

        introPanel = introPanel != null ? introPanel : CreatePanel("Intro_Panel", canvasObject.transform, new Vector2(0f, -98f), new Vector2(650f, 155f), new Color(1f, 1f, 1f, 0f));
        Image introImage = introPanel.GetComponent<Image>();
        if (introImage != null) introImage.raycastTarget = false;

        tutorialButton = tutorialButton != null ? tutorialButton : CreateButton("Tutorial_Button", introPanel.transform, new Vector2(-165f, 38f), new Vector2(305f, 84f), "\u89c2\u770b\u6559\u5b66", new Color(0.15f, 0.72f, 0.78f, 0.55f));
        skipTutorialButton = skipTutorialButton != null ? skipTutorialButton : CreateButton("SkipTutorial_Button", introPanel.transform, new Vector2(165f, 38f), new Vector2(305f, 84f), "\u76f4\u63a5\u5f00\u59cb", new Color(0.25f, 0.60f, 0.88f, 0.55f));

        BindIntroButtons();

        resultPanel = resultPanel != null ? resultPanel : CreatePanel("Result_Panel", canvasObject.transform, new Vector2(0f, 0f), new Vector2(620f, 455f), new Color(1f, 1f, 1f, 0.90f));
        resultTitleText = resultTitleText != null ? resultTitleText : CreateText("Result_Title_Text", resultPanel.transform, new Vector2(0f, 108f), 34, TextAlignmentOptions.Center, new Vector2(500f, 46f));
        resultScoreText = resultScoreText != null ? resultScoreText : CreateText("Result_Score_Text", resultPanel.transform, new Vector2(0f, 42f), 29, TextAlignmentOptions.Center, new Vector2(500f, 42f));
        resultDetailsText = resultDetailsText != null ? resultDetailsText : CreateText("Result_Details_Text", resultPanel.transform, new Vector2(0f, -68f), 24, TextAlignmentOptions.Center, new Vector2(500f, 120f));
        resultPanel.SetActive(false);

        ApplyFontOverride();
        ApplyHudPanelTextStyle();
        ApplyHintTextStyle();
        ApplyResultTextStyle();
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

    TextMeshProUGUI CreateText(string objectName, Transform parent, Vector2 anchoredPosition, int fontSize, TextAlignmentOptions alignment, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (fontOverride != null) text.font = fontOverride;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.text = "";

        return text;
    }

    Slider CreateSlider(string objectName, Transform parent, Vector2 anchoredPosition, Vector2 size, Color fillColor)
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
        background.color = new Color(1f, 1f, 1f, 0.17f);

        GameObject fillAreaObject = new GameObject("Fill Area");
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(2f, 2f);
        fillAreaRect.offsetMax = new Vector2(-2f, -2f);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(fillAreaObject.transform, false);
        RectTransform fillRect = fillObject.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        Image fill = fillObject.AddComponent<Image>();
        fill.color = fillColor;

        slider.fillRect = fillRect;
        slider.targetGraphic = fill;
        slider.interactable = false;

        return slider;
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
            energyText,
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
            }
        }

        if (forceSceneTextFont)
        {
            TextMeshProUGUI[] sceneTexts = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI text in sceneTexts)
            {
                text.font = fontOverride;
            }
        }
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
            gradeText,
            energyText
        };

        foreach (TextMeshProUGUI text in texts)
        {
            if (text == null) continue;

            text.color = hudPanelTextColor;
            text.fontStyle = FontStyles.Bold;
            if (fontOverride != null) text.font = fontOverride;
        }
    }

    void ApplyHintTextStyle()
    {
        if (hintText == null) return;

        hintText.color = hintTextColor;
        hintText.fontStyle = FontStyles.Bold;
        if (fontOverride != null) hintText.font = fontOverride;
    }

    void ApplyResultTextStyle()
    {
        ApplyResultTextStyle(resultTitleText);
        ApplyResultTextStyle(resultScoreText);
        ApplyResultTextStyle(resultDetailsText);
    }

    void ApplyResultTextStyle(TextMeshProUGUI text)
    {
        if (text == null) return;

        text.color = resultTextColor;
        text.fontStyle = FontStyles.Bold;
        if (fontOverride != null) text.font = fontOverride;
    }

    void AutoFindFontOverride()
    {
        if (fontOverride != null && !preferRuntimeChineseFont) return;

#if UNITY_EDITOR
        TMP_FontAsset hanyiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Fonts/HanyiHuaMulanW SDF.asset");
        if (hanyiFont != null)
        {
            fontOverride = hanyiFont;
            preferRuntimeChineseFont = false;
            return;
        }

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
            runtimeFontOverride.name = "Runtime_Microsoft_YaHei_Game2";
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

    void TryWarmCharacters(string message)
    {
        if (fontOverride == null || string.IsNullOrEmpty(message)) return;

        fontOverride.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        fontOverride.TryAddCharacters(message, out _);
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

        // ColorTint for hover feedback
        ColorBlock cb = button.colors;
        cb.normalColor = color;
        cb.highlightedColor = new Color(color.r * 1.25f, color.g * 1.25f, color.b * 1.25f, Mathf.Min(1f, color.a + 0.15f));
        cb.pressedColor = new Color(color.r * 0.85f, color.g * 0.85f, color.b * 0.85f, Mathf.Min(1f, color.a + 0.25f));
        cb.fadeDuration = 0.12f;
        button.colors = cb;

        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI labelText = labelObject.AddComponent<TextMeshProUGUI>();
        if (fontOverride != null) labelText.font = fontOverride;
        labelText.fontSize = 26;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.text = label;

        return button;
    }

    void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
