using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Fun1HudPanelBackgroundApplier
{
    const string StatusTexturePath = "Assets/Project/UITextures/Fun1/StatusPanelBackground.png";
    const string InfoTexturePath = "Assets/Project/UITextures/Fun1/InfoPanelBackground.png";

    static readonly Color PanelTextColor = new Color(0.04f, 0.16f, 0.15f, 1f);
    static readonly Vector2 StatusPosition = new Vector2(-232f, 112f);
    static readonly Vector2 StatusSize = new Vector2(205f, 250f);
    static readonly Vector2 InfoPosition = new Vector2(232f, 112f);
    static readonly Vector2 InfoSize = new Vector2(205f, 238f);

    [MenuItem("Tools/Healthy China/Fun1/Apply HUD Panel Backgrounds")]
    public static void ApplyHudPanelBackgrounds()
    {
        ConfigureTextureAsSprite(StatusTexturePath);
        ConfigureTextureAsSprite(InfoTexturePath);

        Sprite statusSprite = AssetDatabase.LoadAssetAtPath<Sprite>(StatusTexturePath);
        Sprite infoSprite = AssetDatabase.LoadAssetAtPath<Sprite>(InfoTexturePath);

        if (statusSprite == null || infoSprite == null)
        {
            EditorUtility.DisplayDialog("Fun1 HUD Panels", "Panel sprites are not ready yet. Please wait for Unity to refresh, then click this menu again.", "OK");
            return;
        }

        GameObject statusPanel = FindPanel("Status_Panel");
        GameObject infoPanel = FindPanel("Info_Panel");

        if (statusPanel == null || infoPanel == null)
        {
            EditorUtility.DisplayDialog("Fun1 HUD Panels", "Cannot find Status_Panel or Info_Panel. Please open Func1 or bake Game1_EditableHUD first.", "OK");
            return;
        }

        ApplyPanel(statusPanel, statusSprite, StatusPosition, StatusSize);
        ApplyPanel(infoPanel, infoSprite, InfoPosition, InfoSize);

        StylePanelText(statusPanel.transform);
        StylePanelText(infoPanel.transform);
        LayoutStatusTexts(statusPanel.transform);
        LayoutInfoTexts(infoPanel.transform);

        Game1_UIController controller = Object.FindObjectOfType<Game1_UIController>(true);
        if (controller != null)
        {
            Undo.RecordObject(controller, "Apply Fun1 HUD Panel Backgrounds");
            controller.hudPanelTextColor = PanelTextColor;
            EditorUtility.SetDirty(controller);
        }

        EditorSceneManager.MarkSceneDirty(statusPanel.scene);
        Selection.objects = new Object[] { statusPanel, infoPanel };

        EditorUtility.DisplayDialog("Fun1 HUD Panels", "Func1 left and right HUD panels now use the new row backgrounds.", "OK");
    }

    static void ApplyPanel(GameObject panel, Sprite sprite, Vector2 position, Vector2 size)
    {
        Undo.RecordObject(panel, "Apply Fun1 HUD Panel Background");

        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect != null)
        {
            Undo.RecordObject(rect, "Move Fun1 HUD Panel");
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        Image image = panel.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(panel);
        }

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = Color.white;
        image.raycastTarget = false;

        EditorUtility.SetDirty(panel);
    }

    static void StylePanelText(Transform panel)
    {
        TextMeshProUGUI[] texts = panel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            Undo.RecordObject(text, "Style Fun1 HUD Panel Text");
            text.color = PanelTextColor;
            text.fontStyle = FontStyles.Bold;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Center;
            text.transform.SetAsLastSibling();
            EditorUtility.SetDirty(text);
        }
    }

    static void LayoutStatusTexts(Transform panel)
    {
        ConfigureText(panel, "Timer_Text", new Vector2(0f, 80f), new Vector2(156f, 30f), 21);
        ConfigureText(panel, "Score_Text", new Vector2(0f, 40f), new Vector2(156f, 30f), 21);
        ConfigureText(panel, "TotalScore_Text", new Vector2(0f, 0f), new Vector2(156f, 30f), 19);
        ConfigureText(panel, "Reps_Text", new Vector2(0f, -40f), new Vector2(156f, 30f), 21);
        ConfigureText(panel, "Combo_Text", new Vector2(0f, -80f), new Vector2(156f, 30f), 19);
    }

    static void LayoutInfoTexts(Transform panel)
    {
        ConfigureText(panel, "Hand_Text", new Vector2(0f, 62f), new Vector2(158f, 34f), 22);
        ConfigureText(panel, "TargetAngle_Text", new Vector2(0f, 0f), new Vector2(158f, 34f), 20);
        ConfigureText(panel, "Grade_Text", new Vector2(0f, -62f), new Vector2(158f, 38f), 24);
    }

    static void ConfigureText(Transform panel, string childName, Vector2 position, Vector2 size, int fontSize)
    {
        Transform child = FindChildRecursive(panel, childName);
        if (child == null) return;

        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        RectTransform rect = child.GetComponent<RectTransform>();
        if (text == null || rect == null) return;

        Undo.RecordObject(rect, "Layout Fun1 HUD Panel Text");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Undo.RecordObject(text, "Layout Fun1 HUD Panel Text");
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.transform.SetAsLastSibling();

        EditorUtility.SetDirty(rect);
        EditorUtility.SetDirty(text);
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
