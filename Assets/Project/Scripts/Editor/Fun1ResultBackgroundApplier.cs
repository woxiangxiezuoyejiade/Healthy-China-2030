using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Fun1ResultBackgroundApplier
{
    const string TexturePath = "Assets/Project/UITextures/Fun1/ResultPanelShellBackground.png";
    static readonly Color ResultTextColor = new Color(0.04f, 0.18f, 0.24f, 1f);

    [MenuItem("Tools/Healthy China/Fun1/Apply Result Background")]
    public static void ApplyResultBackground()
    {
        ConfigureTextureAsSprite();

        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
        if (backgroundSprite == null)
        {
            EditorUtility.DisplayDialog("Fun1 Result Background", "没有成功导入最终面板背景图，请先右键 Project 面板刷新或重新打开 Unity。", "OK");
            return;
        }

        GameObject resultPanel = FindPanel("Result_Panel");
        if (resultPanel == null)
        {
            EditorUtility.DisplayDialog("Fun1 Result Background", "没有找到 Result_Panel。请先打开 Func1 场景，或者先生成 Game1_EditableHUD。", "OK");
            return;
        }

        Undo.RecordObject(resultPanel, "Apply Fun1 Result Background");

        Image image = resultPanel.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(resultPanel);
        }

        image.sprite = backgroundSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;

        RectTransform rect = resultPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 44f);
            rect.sizeDelta = new Vector2(360f, 510f);
        }

        LayoutResultTexts(resultPanel.transform);
        UpdateControllerColors(resultPanel);

        EditorUtility.SetDirty(resultPanel);
        EditorSceneManager.MarkSceneDirty(resultPanel.scene);
        Selection.activeGameObject = resultPanel;

        EditorUtility.DisplayDialog("Fun1 Result Background", "已经把新的贝壳结算背景贴到 Result_Panel 上，并把结算文字排成四行。现在可以直接运行 Func1 看最终结算效果。", "OK");
    }

    static void ConfigureTextureAsSprite()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
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

    static GameObject FindPanel(string panelName)
    {
        GameObject editableHud = GameObject.Find("Game1_EditableHUD");
        if (editableHud != null)
        {
            Transform panel = FindChildRecursive(editableHud.transform, panelName);
            if (panel != null) return panel.gameObject;
        }

        GameObject autoHud = GameObject.Find("Game1_AutoHUD");
        if (autoHud != null)
        {
            Transform panel = FindChildRecursive(autoHud.transform, panelName);
            if (panel != null) return panel.gameObject;
        }

        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == panelName) return obj;
        }

        return null;
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

    static void LayoutResultTexts(Transform resultPanel)
    {
        TextMeshProUGUI title = FindText(resultPanel, "Result_Title_Text");
        TextMeshProUGUI score = FindText(resultPanel, "Result_Score_Text");
        TextMeshProUGUI details = FindText(resultPanel, "Result_Details_Text");
        TextMeshProUGUI extra = EnsureText(resultPanel, "Result_Extra_Text", details != null ? details : score);

        if (title != null) title.text = string.IsNullOrEmpty(title.text) ? "训练完成" : title.text;
        if (score != null) score.text = string.IsNullOrEmpty(score.text) ? "本局得分 0" : score.text;
        if (details != null && details.text.Contains("\n")) details.text = "完成次数 0/12";
        if (extra != null && string.IsNullOrEmpty(extra.text)) extra.text = "最高连击 x0    平均质量 0";

        ConfigureText(title, new Vector2(0f, 72f), new Vector2(255f, 36f), 26, 0f);
        ConfigureText(score, new Vector2(0f, 19f), new Vector2(255f, 34f), 20, 0f);
        ConfigureText(details, new Vector2(0f, -34f), new Vector2(255f, 34f), 20, 0f);
        ConfigureText(extra, new Vector2(0f, -87f), new Vector2(270f, 34f), 18, 0f);
    }

    static TextMeshProUGUI FindText(Transform root, string textName)
    {
        Transform found = FindChildRecursive(root, textName);
        return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
    }

    static TextMeshProUGUI EnsureText(Transform parent, string textName, TextMeshProUGUI template)
    {
        TextMeshProUGUI existing = FindText(parent, textName);
        if (existing != null) return existing;

        GameObject textObject = new GameObject(textName);
        Undo.RegisterCreatedObjectUndo(textObject, "Create Fun1 Result Text");
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (template != null)
        {
            text.font = template.font;
            text.material = template.material;
        }

        text.text = "最高连击 x0    平均质量 0";
        text.raycastTarget = false;
        return text;
    }

    static void ConfigureText(TextMeshProUGUI text, Vector2 position, Vector2 size, int fontSize, float lineSpacing)
    {
        if (text == null) return;

        Undo.RecordObject(text, "Apply Fun1 Result Text Style");
        text.color = ResultTextColor;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.lineSpacing = lineSpacing;
        text.raycastTarget = false;
        text.transform.SetAsLastSibling();

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        EditorUtility.SetDirty(text);
    }

    static void UpdateControllerColors(GameObject resultPanel)
    {
        Game1_UIController controller = Object.FindObjectOfType<Game1_UIController>(true);
        if (controller == null) return;

        Undo.RecordObject(controller, "Apply Fun1 Result Text Color");
        controller.resultTextColor = ResultTextColor;

        if (controller.resultPanel == null) controller.resultPanel = resultPanel;
        if (controller.resultTitleText == null) controller.resultTitleText = FindText(resultPanel.transform, "Result_Title_Text");
        if (controller.resultScoreText == null) controller.resultScoreText = FindText(resultPanel.transform, "Result_Score_Text");
        if (controller.resultDetailsText == null) controller.resultDetailsText = FindText(resultPanel.transform, "Result_Details_Text");
        if (controller.resultExtraText == null) controller.resultExtraText = FindText(resultPanel.transform, "Result_Extra_Text");

        EditorUtility.SetDirty(controller);
    }
}
