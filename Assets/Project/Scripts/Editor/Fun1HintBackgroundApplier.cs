using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Fun1HintBackgroundApplier
{
    const string TexturePath = "Assets/Project/UITextures/Fun1/ActionHintBackground.jpg";

    [MenuItem("Tools/Healthy China/Fun1/Apply Hint Background")]
    public static void ApplyHintBackground()
    {
        ConfigureTextureAsSprite();

        Sprite backgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
        if (backgroundSprite == null)
        {
            EditorUtility.DisplayDialog("Fun1 Hint Background", "没有成功导入动作提示背景图，请先右键 Project 面板刷新或重新打开 Unity。", "OK");
            return;
        }

        GameObject hintPanel = FindHintPanel();
        if (hintPanel == null)
        {
            EditorUtility.DisplayDialog("Fun1 Hint Background", "没有找到 Hint_Panel。请先打开 Func1 场景，或者先生成 Game1_EditableHUD。", "OK");
            return;
        }

        Undo.RecordObject(hintPanel, "Apply Fun1 Hint Background");

        Image image = hintPanel.GetComponent<Image>();
        if (image == null)
        {
            image = Undo.AddComponent<Image>(hintPanel);
        }

        image.sprite = backgroundSprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.color = new Color(1f, 1f, 1f, 0.92f);
        image.raycastTarget = false;

        RectTransform rect = hintPanel.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(455f, 76f);
        }

        MoveHintTextForward(hintPanel.transform);

        EditorUtility.SetDirty(hintPanel);
        EditorSceneManager.MarkSceneDirty(hintPanel.scene);
        Selection.activeGameObject = hintPanel;

        EditorUtility.DisplayDialog("Fun1 Hint Background", "已经把动作提示背景图贴到 Hint_Panel 上了。现在可以直接在 Inspector 里继续调 Image 颜色、透明度和 RectTransform 大小。", "OK");
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

    static GameObject FindHintPanel()
    {
        GameObject editableHud = GameObject.Find("Game1_EditableHUD");
        if (editableHud != null)
        {
            Transform panel = FindChildRecursive(editableHud.transform, "Hint_Panel");
            if (panel != null) return panel.gameObject;
        }

        GameObject autoHud = GameObject.Find("Game1_AutoHUD");
        if (autoHud != null)
        {
            Transform panel = FindChildRecursive(autoHud.transform, "Hint_Panel");
            if (panel != null) return panel.gameObject;
        }

        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>(true);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == "Hint_Panel") return obj;
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

    static void MoveHintTextForward(Transform hintPanel)
    {
        TextMeshProUGUI[] texts = hintPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI text in texts)
        {
            text.color = new Color(0.05f, 0.07f, 0.08f, 1f);
            text.raycastTarget = false;
            text.transform.SetAsLastSibling();
        }
    }
}

