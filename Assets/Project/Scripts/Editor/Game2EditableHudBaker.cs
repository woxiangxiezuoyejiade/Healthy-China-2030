using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class Game2EditableHudBaker
{
    const string RootName = "Game2_EditableHUD";
    const float DefaultBakeScale = 0.00115f;

    const string StatusTexturePath = "Assets/Project/UITextures/Fun1/StatusPanelBackground.png";
    const string EnergyTexturePath = "Assets/Project/UITextures/Fun1/InfoPanelBackground.png";
    const string HintTexturePath = "Assets/Project/UITextures/Fun1/ActionHintBackground.jpg";
    const string ResultTexturePath = "Assets/Project/UITextures/Fun1/FinalPanelBackground.png";
    const string TutorialButtonTexturePath = "Assets/Project/UITextures/Fun1/TutorialButton.png";
    const string StartButtonTexturePath = "Assets/Project/UITextures/Fun1/StartTrainingButton.png";

    static readonly Color HudPanelTextColor = new Color(0.04f, 0.16f, 0.15f, 1f);
    static readonly Color HintTextColor = new Color(0.05f, 0.07f, 0.08f, 1f);
    static readonly Color ResultTextColor = new Color(0.06f, 0.17f, 0.18f, 1f);
    static readonly Vector2 BakedHudSize = new Vector2(720f, 440f);

    [MenuItem("Tools/Healthy China/Fun2/Bake Editable HUD")]
    public static void BakeEditableHud()
    {
        Game2_UIController controller = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<Game2_UIController>()
            : null;

        if (controller == null)
        {
            controller = Object.FindObjectOfType<Game2_UIController>(true);
        }

        if (controller == null)
        {
            EditorUtility.DisplayDialog("Bake Game2 HUD", "当前场景里没有找到 Game2_UIController。请先打开 Func2 场景。", "OK");
            return;
        }

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Bake Game2 HUD",
                "场景里已经有 Game2_EditableHUD。要删除旧的并重新生成吗？",
                "重新生成",
                "取消");

            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        ConfigureSprites();

        Sprite statusSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StatusTexturePath);
        Sprite energySprite = AssetDatabase.LoadAssetAtPath<Sprite>(EnergyTexturePath);
        Sprite hintSprite = AssetDatabase.LoadAssetAtPath<Sprite>(HintTexturePath);
        Sprite resultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ResultTexturePath);
        Sprite tutorialButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TutorialButtonTexturePath);
        Sprite startButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StartButtonTexturePath);

        Undo.RecordObject(controller, "Bake Game2 Editable HUD");

        Transform parent = controller.attachAutoHudToCamera && Camera.main != null
            ? Camera.main.transform
            : controller.transform;

        float hudScale = GetReasonableScale(controller.autoHudScale);
        controller.autoHudScale = hudScale;
        controller.autoHudSize = BakedHudSize;

        GameObject canvasObject = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Bake Game2 Editable HUD");
        canvasObject.transform.SetParent(parent, false);
        canvasObject.transform.localPosition = parent == controller.transform ? controller.autoHudPosition : controller.autoHudCameraOffset;
        canvasObject.transform.localRotation = Quaternion.identity;
        canvasObject.transform.localScale = Vector3.one * hudScale;

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 25;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = controller.autoHudSize;

        TMP_FontAsset font = GetFont(controller);

        GameObject hudRoot = CreatePanel("HUD_Root", canvasObject.transform, Vector2.zero, controller.autoHudSize, null, new Color(0f, 0f, 0f, 0f));
        Image rootImage = hudRoot.GetComponent<Image>();
        if (rootImage != null) rootImage.raycastTarget = false;

        GameObject statusPanel = CreatePanel("Status_Panel", hudRoot.transform, new Vector2(-270f, 112f), new Vector2(205f, 235f), statusSprite, Color.white);
        GameObject energyPanel = CreatePanel("Energy_Panel", hudRoot.transform, new Vector2(270f, 122f), new Vector2(235f, 160f), energySprite, Color.white);
        GameObject hintPanel = CreatePanel("Hint_Panel", hudRoot.transform, new Vector2(0f, -154f), new Vector2(545f, 92f), hintSprite, new Color(1f, 1f, 1f, 0.92f));

        controller.timerText = CreateText("Timer_Text", statusPanel.transform, new Vector2(0f, 90f), new Vector2(174f, 32f), 24, TextAlignmentOptions.Left, font, "时间 90s", HudPanelTextColor);
        controller.scoreText = CreateText("Score_Text", statusPanel.transform, new Vector2(0f, 47f), new Vector2(174f, 32f), 24, TextAlignmentOptions.Left, font, "本局 0", HudPanelTextColor);
        controller.totalScoreText = CreateText("TotalScore_Text", statusPanel.transform, new Vector2(0f, 4f), new Vector2(174f, 32f), 22, TextAlignmentOptions.Left, font, "总分 0", HudPanelTextColor);
        controller.repsText = CreateText("Reps_Text", statusPanel.transform, new Vector2(0f, -39f), new Vector2(174f, 32f), 24, TextAlignmentOptions.Left, font, "完成 0/10", HudPanelTextColor);
        controller.comboText = CreateText("Combo_Text", statusPanel.transform, new Vector2(0f, -82f), new Vector2(174f, 32f), 22, TextAlignmentOptions.Left, font, "连击 -", HudPanelTextColor);

        controller.energyText = CreateText("Energy_Text", energyPanel.transform, new Vector2(0f, 43f), new Vector2(202f, 34f), 24, TextAlignmentOptions.Center, font, "阳光能量 0%", HudPanelTextColor);
        controller.gradeText = CreateText("Grade_Text", energyPanel.transform, new Vector2(0f, -8f), new Vector2(202f, 40f), 30, TextAlignmentOptions.Center, font, "", HudPanelTextColor);
        controller.energyProgress = CreateSlider("Energy_Progress", energyPanel.transform, new Vector2(0f, -57f), new Vector2(184f, 16f), new Color(1f, 0.83f, 0.25f, 0.92f));

        controller.hintText = CreateText("Hint_Text", hintPanel.transform, new Vector2(0f, 14f), new Vector2(498f, 34f), 23, TextAlignmentOptions.Center, font, "按住 Space 模拟扩胸，松开后回到起点", HintTextColor);
        controller.actionProgress = CreateSlider("Action_Progress", hintPanel.transform, new Vector2(0f, -25f), new Vector2(462f, 16f), new Color(0.35f, 0.78f, 1f, 0.92f));

        controller.introPanel = CreatePanel("Intro_Panel", canvasObject.transform, new Vector2(0f, -98f), new Vector2(650f, 155f), null, new Color(1f, 1f, 1f, 0f));
        Image introImage = controller.introPanel.GetComponent<Image>();
        if (introImage != null) introImage.raycastTarget = false;

        controller.tutorialButton = CreateButton("Tutorial_Button", controller.introPanel.transform, new Vector2(-165f, 38f), new Vector2(305f, 84f), "观看教学", tutorialButtonSprite, font);
        controller.skipTutorialButton = CreateButton("SkipTutorial_Button", controller.introPanel.transform, new Vector2(165f, 38f), new Vector2(305f, 84f), "直接开始", startButtonSprite, font);

        controller.resultPanel = CreatePanel("Result_Panel", canvasObject.transform, Vector2.zero, new Vector2(620f, 455f), resultSprite, new Color(1f, 1f, 1f, 0.98f));
        controller.resultTitleText = CreateText("Result_Title_Text", controller.resultPanel.transform, new Vector2(0f, 108f), new Vector2(500f, 46f), 34, TextAlignmentOptions.Center, font, "训练完成", ResultTextColor);
        controller.resultScoreText = CreateText("Result_Score_Text", controller.resultPanel.transform, new Vector2(0f, 42f), new Vector2(500f, 42f), 29, TextAlignmentOptions.Center, font, "最终得分 0", ResultTextColor);
        controller.resultDetailsText = CreateText("Result_Details_Text", controller.resultPanel.transform, new Vector2(0f, -68f), new Vector2(500f, 120f), 24, TextAlignmentOptions.Center, font, "完成 0/10\n最高连击 x0\n平均质量 0", ResultTextColor);
        controller.resultPanel.SetActive(false);

        controller.autoCreateMissingUI = false;
        controller.fontOverride = font;
        controller.hudPanelTextColor = HudPanelTextColor;
        controller.hintTextColor = HintTextColor;
        controller.resultTextColor = ResultTextColor;

        SetPrivatePanelReferences(controller, statusPanel, energyPanel, hintPanel);
        EnsureEventSystem();
        OfferToDisableOldHudCanvases(canvasObject);

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Selection.activeGameObject = canvasObject;

        EditorUtility.DisplayDialog(
            "Bake Game2 HUD",
            "已经生成放大版 Game2_EditableHUD，并自动套用 Func1 的背景图和按钮图。之后可以在 Hierarchy 里继续手动微调。",
            "OK");
    }

    static float GetReasonableScale(float currentScale)
    {
        if (currentScale >= 0.001f && currentScale <= 0.0016f) return currentScale;
        return DefaultBakeScale;
    }

    static TMP_FontAsset GetFont(Game2_UIController controller)
    {
        if (controller.fontOverride != null) return controller.fontOverride;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Project/Fonts/msyh SDF.asset");
        if (font != null) return font;

        return TMP_Settings.defaultFontAsset;
    }

    static GameObject CreatePanel(string name, Transform parent, Vector2 position, Vector2 size, Sprite sprite, Color color)
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
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = color;
        image.raycastTarget = false;

        return panel;
    }

    static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment, TMP_FontAsset font, string text, Color color)
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
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = alignment;
        label.enableWordWrapping = false;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    static Button CreateButton(string name, Transform parent, Vector2 position, Vector2 size, string label, Sprite sprite, TMP_FontAsset font)
    {
        GameObject buttonObject = CreatePanel(name, parent, position, size, sprite, Color.white);
        Image image = buttonObject.GetComponent<Image>();
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        cb.pressedColor = new Color(0.86f, 0.92f, 0.92f, 1f);
        cb.fadeDuration = 0.12f;
        button.colors = cb;

        RectTransform labelRect = CreateText("Label", buttonObject.transform, Vector2.zero, size, 26, TextAlignmentOptions.Center, font, label, new Color(0.04f, 0.16f, 0.15f, 1f)).rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.sizeDelta = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;

        return button;
    }

    static Slider CreateSlider(string name, Transform parent, Vector2 position, Vector2 size, Color fillColor)
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

        GameObject backgroundObject = CreatePanel("Background", sliderObject.transform, Vector2.zero, size, null, new Color(0.30f, 0.47f, 0.47f, 0.22f));
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

        GameObject fillObject = CreatePanel("Fill", fillAreaObject.transform, Vector2.zero, size, null, fillColor);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        slider.fillRect = fillRect;
        slider.targetGraphic = fillObject.GetComponent<Image>();
        slider.interactable = false;
        return slider;
    }

    static void SetPrivatePanelReferences(Game2_UIController controller, GameObject statusPanel, GameObject energyPanel, GameObject hintPanel)
    {
        SerializedObject serialized = new SerializedObject(controller);
        SerializedProperty statusProperty = serialized.FindProperty("statusPanelObject");
        SerializedProperty energyProperty = serialized.FindProperty("energyPanelObject");
        SerializedProperty hintProperty = serialized.FindProperty("hintPanelObject");

        if (statusProperty != null) statusProperty.objectReferenceValue = statusPanel;
        if (energyProperty != null) energyProperty.objectReferenceValue = energyPanel;
        if (hintProperty != null) hintProperty.objectReferenceValue = hintPanel;

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

            bool looksLikeGame2Hud =
                root.transform.Find("HUD_Root") != null &&
                root.transform.Find("Intro_Panel") != null;

            if (looksLikeGame2Hud)
            {
                candidates.Add(root);
            }
        }

        if (candidates.Count == 0) return;

        bool disable = EditorUtility.DisplayDialog(
            "Bake Game2 HUD",
            $"检测到 {candidates.Count} 个旧的 Fun2 HUD Canvas。要先禁用它们，避免和新的 Game2_EditableHUD 重叠吗？",
            "禁用旧的",
            "先保留");

        if (!disable) return;

        foreach (GameObject candidate in candidates)
        {
            Undo.RecordObject(candidate, "Disable old Game2 HUD");
            candidate.SetActive(false);
            EditorUtility.SetDirty(candidate);
        }
    }

    static void ConfigureSprites()
    {
        ConfigureTextureAsSprite(StatusTexturePath);
        ConfigureTextureAsSprite(EnergyTexturePath);
        ConfigureTextureAsSprite(HintTexturePath);
        ConfigureTextureAsSprite(ResultTexturePath);
        ConfigureTextureAsSprite(TutorialButtonTexturePath);
        ConfigureTextureAsSprite(StartButtonTexturePath);
    }

    static void ConfigureTextureAsSprite(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.spritePixelsPerUnit = 100f;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteGenerateFallbackPhysicsShape = false;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
    }
}
