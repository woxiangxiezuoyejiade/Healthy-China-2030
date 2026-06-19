using TMPro;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Game1EditableHudBaker
{
    const string RootName = "Game1_EditableHUD";
    const float DefaultBakeScale = 0.0007f;
    const string StatusTexturePath = "Assets/Project/UITextures/Fun1/StatusPanelBackground.png";
    const string InfoTexturePath = "Assets/Project/UITextures/Fun1/InfoPanelBackground.png";
    static readonly Color HudPanelTextColor = new Color(0.04f, 0.16f, 0.15f, 1f);
    static readonly Vector2 StatusPanelPosition = new Vector2(-232f, 112f);
    static readonly Vector2 StatusPanelSize = new Vector2(205f, 250f);
    static readonly Vector2 InfoPanelPosition = new Vector2(232f, 112f);
    static readonly Vector2 InfoPanelSize = new Vector2(205f, 238f);

    [MenuItem("Tools/Healthy China/Fun1/Bake Editable HUD")]
    public static void BakeEditableHud()
    {
        Game1_UIController controller = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<Game1_UIController>()
            : null;

        if (controller == null)
        {
            controller = Object.FindObjectOfType<Game1_UIController>(true);
        }

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Bake Game1 HUD", "当前场景里没有找到 Game1_UIController。请先打开 Func1 场景。", "OK");
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Bake Game1 HUD",
                "场景里已经有 Game1_EditableHUD。要删除旧的并重新生成吗？",
                "重新生成",
                "取消");

            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        Undo.RecordObject(controller, "Bake Game1 Editable HUD");

        Transform parent = controller.attachAutoHudToCamera && Camera.main != null
            ? Camera.main.transform
            : controller.transform;

        float hudScale = GetReasonableScale(controller.autoHudScale);
        controller.autoHudScale = hudScale;

        GameObject canvasObject = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Bake Game1 Editable HUD");
        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.localPosition = parent == controller.transform ? controller.autoHudPosition : controller.autoHudCameraOffset;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * hudScale;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = controller.autoHudSize;

        TMP_FontAsset font = GetFont(controller);

        GameObject hudPanel = CreatePanel("HUD_Panel", canvasObject.transform, Vector2.zero, controller.autoHudSize, new Color(0f, 0f, 0f, 0f));
        Image hudImage = hudPanel.GetComponent<Image>();
        if (hudImage != null) hudImage.raycastTarget = false;

        GameObject statusPanel = CreatePanel("Status_Panel", hudPanel.transform, StatusPanelPosition, StatusPanelSize, Color.white);
        GameObject infoPanel = CreatePanel("Info_Panel", hudPanel.transform, InfoPanelPosition, InfoPanelSize, Color.white);
        GameObject hintPanel = CreatePanel("Hint_Panel", hudPanel.transform, new Vector2(0f, -122f), new Vector2(430f, 58f), new Color(0.02f, 0.05f, 0.07f, 0.28f));

        ApplyPanelSprite(statusPanel, StatusTexturePath);
        ApplyPanelSprite(infoPanel, InfoTexturePath);

        controller.timerText = CreateText("Timer_Text", statusPanel.transform, new Vector2(0f, 80f), new Vector2(156f, 30f), 21, TextAlignmentOptions.Center, font, "时间 90s");
        controller.scoreText = CreateText("Score_Text", statusPanel.transform, new Vector2(0f, 40f), new Vector2(156f, 30f), 21, TextAlignmentOptions.Center, font, "本局 0");
        controller.totalScoreText = CreateText("TotalScore_Text", statusPanel.transform, new Vector2(0f, 0f), new Vector2(156f, 30f), 19, TextAlignmentOptions.Center, font, "总分 0");
        controller.repsText = CreateText("Reps_Text", statusPanel.transform, new Vector2(0f, -40f), new Vector2(156f, 30f), 21, TextAlignmentOptions.Center, font, "完成 0/12");
        controller.comboText = CreateText("Combo_Text", statusPanel.transform, new Vector2(0f, -80f), new Vector2(156f, 30f), 19, TextAlignmentOptions.Center, font, "连击 -");
        StyleHudPanelText(statusPanel.transform, TextAlignmentOptions.Center);

        controller.handText = CreateText("Hand_Text", infoPanel.transform, new Vector2(0f, 62f), new Vector2(158f, 34f), 22, TextAlignmentOptions.Center, font, "训练手：右手");
        controller.targetAngleText = CreateText("TargetAngle_Text", infoPanel.transform, new Vector2(0f, 0f), new Vector2(158f, 34f), 20, TextAlignmentOptions.Center, font, "目标角度: 45°");
        controller.gradeText = CreateText("Grade_Text", infoPanel.transform, new Vector2(0f, -62f), new Vector2(158f, 38f), 24, TextAlignmentOptions.Center, font, "");
        StyleHudPanelText(infoPanel.transform, TextAlignmentOptions.Center);

        controller.hintText = CreateText("Hint_Text", hintPanel.transform, new Vector2(0f, 8f), new Vector2(390f, 34f), 20, TextAlignmentOptions.Center, font, "请按提示进行肩外旋训练");
        controller.hintText.color = new Color(0.05f, 0.07f, 0.08f, 1f);
        controller.rotationProgress = CreateSlider("Rotation_Progress", hintPanel.transform, new Vector2(0f, -20f), new Vector2(360f, 14f));

        controller.introPanel = CreatePanel("Intro_Panel", canvasObject.transform, new Vector2(0f, -80f), new Vector2(460f, 130f), new Color(1f, 1f, 1f, 0f));
        Image introImage = controller.introPanel.GetComponent<Image>();
        if (introImage != null) introImage.raycastTarget = false;
        controller.tutorialButton = CreateButton("Tutorial_Button", controller.introPanel.transform, new Vector2(-112f, 34f), new Vector2(210f, 50f), "观看教学", new Color(0.15f, 0.72f, 0.78f, 0.55f), font);
        controller.skipTutorialButton = CreateButton("SkipTutorial_Button", controller.introPanel.transform, new Vector2(112f, 34f), new Vector2(210f, 50f), "直接开始", new Color(0.25f, 0.60f, 0.88f, 0.55f), font);

        controller.resultPanel = CreatePanel("Result_Panel", canvasObject.transform, new Vector2(0f, 44f), new Vector2(360f, 510f), new Color(1f, 1f, 1f, 0.92f));
        controller.resultTitleText = CreateText("Result_Title_Text", controller.resultPanel.transform, new Vector2(0f, 72f), new Vector2(255f, 36f), 26, TextAlignmentOptions.Center, font, "训练完成");
        controller.resultScoreText = CreateText("Result_Score_Text", controller.resultPanel.transform, new Vector2(0f, 19f), new Vector2(255f, 34f), 20, TextAlignmentOptions.Center, font, "本局得分 0");
        controller.resultDetailsText = CreateText("Result_Details_Text", controller.resultPanel.transform, new Vector2(0f, -34f), new Vector2(255f, 34f), 20, TextAlignmentOptions.Center, font, "完成次数 0/12");
        controller.resultExtraText = CreateText("Result_Extra_Text", controller.resultPanel.transform, new Vector2(0f, -87f), new Vector2(270f, 34f), 18, TextAlignmentOptions.Center, font, "最高连击 x0    平均质量 0");
        controller.resultDetailsText.enableWordWrapping = false;
        controller.resultDetailsText.lineSpacing = 0f;
        controller.resultExtraText.enableWordWrapping = false;
        controller.resultExtraText.lineSpacing = 0f;
        controller.resultPanel.SetActive(false);

        controller.autoCreateMissingUI = false;
        controller.fontOverride = font;
        controller.hudPanelTextColor = HudPanelTextColor;
        SetPrivatePanelReferences(controller, statusPanel, infoPanel, hintPanel);
        EnsureEventSystem();
        OfferToDisableOldHudCanvases(canvasObject);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Selection.activeGameObject = canvasObject;

        EditorUtility.DisplayDialog(
            "Bake Game1 HUD",
            "已经生成 Game1_EditableHUD，并自动连接到 Game1_UIController。\n\n如果觉得大小不合适，选中 Game1_EditableHUD 调 Transform Scale，建议先在 0.0005 - 0.0010 之间微调。",
            "OK");
    }

    static float GetReasonableScale(float currentScale)
    {
        if (currentScale > 0f && currentScale <= 0.001f) return currentScale;
        return DefaultBakeScale;
    }

    static TMP_FontAsset GetFont(Game1_UIController controller)
    {
        if (controller.fontOverride != null) return controller.fontOverride;

        TMP_FontAsset hanyiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Fonts/HanyiHuaMulanW SDF.asset");
        if (hanyiFont != null) return hanyiFont;

        return TMP_Settings.defaultFontAsset;
    }

    static GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    static void ApplyPanelSprite(GameObject panel, string texturePath)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        if (sprite == null) return;

        Image image = panel.GetComponent<Image>();
        if (image == null) image = panel.AddComponent<Image>();

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment, TMP_FontAsset font, string text)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = alignment;
        label.enableWordWrapping = true;
        label.text = text;
        return label;
    }

    static Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size, string label, Color color, TMP_FontAsset font)
    {
        GameObject buttonObject = CreatePanel(name, parent, position, size, color);
        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();

        RectTransform labelRect = CreateText("Label", buttonObject.transform, Vector2.zero, size, 18, TextAlignmentOptions.Center, font, label).rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;

        return button;
    }

    static void StyleHudPanelText(Transform panel, TextAlignmentOptions alignment)
    {
        TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            text.color = HudPanelTextColor;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            if (text.name != "Grade_Text") text.alignment = alignment;
        }
    }

    static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject sliderObject = new GameObject(name);
        sliderObject.transform.SetParent(parent, false);

        RectTransform rect = sliderObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;

        GameObject backgroundObject = CreatePanel("Background", sliderObject.transform, Vector2.zero, size, new Color(1f, 1f, 1f, 0.18f));
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;

        GameObject fillAreaObject = new GameObject("Fill Area");
        fillAreaObject.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(3f, 3f);
        fillAreaRect.offsetMax = new Vector2(-3f, -3f);

        GameObject fillObject = CreatePanel("Fill", fillAreaObject.transform, Vector2.zero, size, new Color(1f, 0.82f, 0.25f, 0.92f));
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillObject.GetComponent<Image>();
        slider.interactable = false;
        return slider;
    }

    static void SetPrivatePanelReferences(Game1_UIController controller, GameObject statusPanel, GameObject infoPanel, GameObject hintPanel)
    {
        SerializedObject serialized = new SerializedObject(controller);
        serialized.FindProperty("statusPanelObject").objectReferenceValue = statusPanel;
        serialized.FindProperty("infoPanelObject").objectReferenceValue = infoPanel;
        serialized.FindProperty("hintPanelObject").objectReferenceValue = hintPanel;
        serialized.ApplyModifiedProperties();
    }

    static void EnsureEventSystem()
    {
        if (Object.FindObjectOfType<EventSystem>(true) != null) return;

        GameObject eventSystemObject = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    static void OfferToDisableOldHudCanvases(GameObject newRoot)
    {
        List<GameObject> candidates = new List<GameObject>();
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);

        foreach (Canvas canvas in canvases)
        {
            GameObject root = canvas.gameObject;
            if (root == newRoot || root.name == RootName || !root.activeSelf) continue;

            bool looksLikeGame1Hud =
                root.transform.Find("HUD_Panel") != null &&
                root.transform.Find("Intro_Panel") != null;

            if (looksLikeGame1Hud)
            {
                candidates.Add(root);
            }
        }

        if (candidates.Count == 0) return;

        bool disable = EditorUtility.DisplayDialog(
            "Bake Game1 HUD",
            $"检测到 {candidates.Count} 个旧的 Fun1 HUD Canvas。要先禁用它们，避免和新的 Game1_EditableHUD 重叠吗？",
            "禁用旧的",
            "先保留");

        if (!disable) return;

        foreach (GameObject candidate in candidates)
        {
            Undo.RecordObject(candidate, "Disable old Game1 HUD");
            candidate.SetActive(false);
            EditorUtility.SetDirty(candidate);
        }
    }
}

