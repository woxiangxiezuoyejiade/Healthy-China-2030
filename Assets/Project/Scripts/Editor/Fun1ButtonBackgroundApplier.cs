using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Fun1ButtonBackgroundApplier
{
    const string StartButtonPath = "Assets/Project/UITextures/Fun1/StartTrainingButton.png";
    const string TutorialButtonPath = "Assets/Project/UITextures/Fun1/TutorialButton.png";

    static readonly Color StartTextColor = new Color(0.03f, 0.17f, 0.17f, 1f);
    static readonly Color TutorialTextColor = new Color(0.24f, 0.14f, 0.02f, 1f);

    [MenuItem("Tools/Healthy China/Fun1/Apply Button Backgrounds")]
    public static void ApplyButtonBackgrounds()
    {
        ConfigureTextureAsSprite(StartButtonPath);
        ConfigureTextureAsSprite(TutorialButtonPath);

        Sprite startSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StartButtonPath);
        Sprite tutorialSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TutorialButtonPath);

        if (startSprite == null || tutorialSprite == null)
        {
            EditorUtility.DisplayDialog("Fun1 Button Backgrounds", "按钮背景图没有成功导入，请先右键 Project 面板刷新或重新打开 Unity。", "OK");
            return;
        }

        Button startButton = FindButton("SkipTutorial_Button");
        Button tutorialButton = FindButton("Tutorial_Button");

        if (startButton == null || tutorialButton == null)
        {
            EditorUtility.DisplayDialog("Fun1 Button Backgrounds", "没有找到 Tutorial_Button 或 SkipTutorial_Button。请先打开 Func1 场景，或者先生成 Game1_EditableHUD。", "OK");
            return;
        }

        ApplyButton(startButton, startSprite, "直接开始", StartTextColor);
        ApplyButton(tutorialButton, tutorialSprite, "观看教学", TutorialTextColor);
        CleanIntroPanel(startButton.transform.parent);
        UpdateControllerReferences(startButton, tutorialButton);

        EditorSceneManager.MarkSceneDirty(startButton.gameObject.scene);
        Selection.activeGameObject = startButton.gameObject;

        EditorUtility.DisplayDialog("Fun1 Button Backgrounds", "已经替换两个按钮背景：蓝绿色是“直接开始”，金黄色是“观看教学”。如果大小不合适，可以选中按钮调 RectTransform。", "OK");
    }

    [MenuItem("Tools/Healthy China/Fun1/Clean Intro Button Panel")]
    public static void CleanIntroButtonPanel()
    {
        Transform introPanel = FindIntroPanel();
        if (introPanel == null)
        {
            EditorUtility.DisplayDialog("Fun1 Intro Panel", "没有找到 Intro_Panel。请先打开 Func1 场景。", "OK");
            return;
        }

        CleanIntroPanel(introPanel);
        EditorSceneManager.MarkSceneDirty(introPanel.gameObject.scene);
        Selection.activeGameObject = introPanel.gameObject;

        EditorUtility.DisplayDialog("Fun1 Intro Panel", "已经隐藏 Intro_Panel 背景，并删除/隐藏 Intro_Text。两个按钮会保留。", "OK");
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

    static void ApplyButton(Button button, Sprite sprite, string label, Color textColor)
    {
        Undo.RecordObject(button.gameObject, "Apply Fun1 Button Background");

        Image image = button.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(button.gameObject);
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = true;
        button.targetGraphic = image;

        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(230f, 62f);
        }

        TextMeshProUGUI labelText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (labelText != null)
        {
            Undo.RecordObject(labelText, "Apply Fun1 Button Text Style");
            labelText.text = label;
            labelText.color = textColor;
            labelText.fontSize = 21f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.raycastTarget = false;
            labelText.transform.SetAsLastSibling();
            EditorUtility.SetDirty(labelText);
        }

        EditorUtility.SetDirty(button.gameObject);
    }

    static Button FindButton(string buttonName)
    {
        GameObject editableHud = GameObject.Find("Game1_EditableHUD");
        if (editableHud != null)
        {
            Transform button = FindChildRecursive(editableHud.transform, buttonName);
            if (button != null) return button.GetComponent<Button>();
        }

        GameObject autoHud = GameObject.Find("Game1_AutoHUD");
        if (autoHud != null)
        {
            Transform button = FindChildRecursive(autoHud.transform, buttonName);
            if (button != null) return button.GetComponent<Button>();
        }

        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == buttonName) return obj.GetComponent<Button>();
        }

        return null;
    }

    static Transform FindIntroPanel()
    {
        GameObject editableHud = GameObject.Find("Game1_EditableHUD");
        if (editableHud != null)
        {
            Transform panel = FindChildRecursive(editableHud.transform, "Intro_Panel");
            if (panel != null) return panel;
        }

        GameObject autoHud = GameObject.Find("Game1_AutoHUD");
        if (autoHud != null)
        {
            Transform panel = FindChildRecursive(autoHud.transform, "Intro_Panel");
            if (panel != null) return panel;
        }

        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Intro_Panel") return obj.transform;
        }

        return null;
    }

    static void CleanIntroPanel(Transform introPanel)
    {
        if (introPanel == null) return;

        Image panelImage = introPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            Undo.RecordObject(panelImage, "Hide Fun1 Intro Panel Background");
            panelImage.color = new Color(1f, 1f, 1f, 0f);
            panelImage.raycastTarget = false;
            EditorUtility.SetDirty(panelImage);
        }

        Transform introText = FindChildRecursive(introPanel, "Intro_Text");
        if (introText != null)
        {
            Undo.DestroyObjectImmediate(introText.gameObject);
        }
    }

    static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

    static void UpdateControllerReferences(Button startButton, Button tutorialButton)
    {
        Game1_UIController controller = Object.FindObjectOfType<Game1_UIController>(true);
        if (controller == null) return;

        Undo.RecordObject(controller, "Apply Fun1 Button References");
        controller.skipTutorialButton = startButton;
        controller.tutorialButton = tutorialButton;
        EditorUtility.SetDirty(controller);
    }
}
