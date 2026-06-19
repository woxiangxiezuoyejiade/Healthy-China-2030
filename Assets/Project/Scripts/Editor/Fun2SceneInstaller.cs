using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class Fun2SceneInstaller
{
    [MenuItem("Tools/Healthy China/Fun2/Install Avatar And Enlarge UI")]
    public static void InstallAvatarAndEnlargeUI()
    {
        Game2_ChestExpansionController controller = Object.FindObjectOfType<Game2_ChestExpansionController>(true);
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Fun2 Setup", "没有找到 Game2_ChestExpansionController。请先打开 Func2 场景。", "OK");
            return;
        }

        Undo.RecordObject(controller, "Install Fun2 Avatar And Enlarge UI");

        Game2_AvatarVisual avatar = controller.GetComponent<Game2_AvatarVisual>();
        if (avatar == null)
        {
            avatar = Undo.AddComponent<Game2_AvatarVisual>(controller.gameObject);
        }

        Undo.RecordObject(avatar, "Configure Fun2 Avatar");
        avatar.avatarOffset = new Vector3(0.62f, -0.45f, 1.36f);
        avatar.avatarScale = 0.24f;
        avatar.followCamera = true;
        avatar.tutorialCycleTime = 3.2f;
        avatar.smoothSpeed = 12f;
        avatar.bodyAlpha = 0.95f;
        avatar.activeArmAlpha = 1f;
        controller.avatarVisual = avatar;

        GameObject generatedAvatar = GameObject.Find("Game2_BlockAvatarRoot");
        if (generatedAvatar != null)
        {
            Undo.DestroyObjectImmediate(generatedAvatar);
        }

        Game2_UIController ui = controller.uiController != null
            ? controller.uiController
            : controller.GetComponent<Game2_UIController>();

        if (ui != null)
        {
            Undo.RecordObject(ui, "Configure Fun2 UI Size");
            ui.autoHudSize = new Vector2(720f, 440f);
            ui.autoHudScale = 0.00125f;
            ui.autoHudCameraOffset = new Vector3(0f, -0.14f, 1.45f);
            LayoutExistingHud(ui);
            EditorUtility.SetDirty(ui);
        }

        EditorUtility.SetDirty(avatar);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Selection.activeGameObject = controller.gameObject;

        EditorUtility.DisplayDialog(
            "Fun2 Setup",
            "已经给 Func2 安装扩胸小人，并放大现有 UI。运行后小人会跟随教学和 Space/手柄扩胸进度打开双臂。",
            "OK");
    }

    static void LayoutExistingHud(Game2_UIController ui)
    {
        GameObject hud = GameObject.Find("Game2_EditableHUD");
        if (hud == null) hud = GameObject.Find("Game2_AutoHUD");
        if (hud == null) return;

        Undo.RecordObject(hud.transform, "Resize Fun2 HUD Root");
        hud.transform.localScale = Vector3.one * 0.00115f;

        RectTransform hudRect = hud.GetComponent<RectTransform>();
        if (hudRect != null)
        {
            Undo.RecordObject(hudRect, "Resize Fun2 HUD Rect");
            hudRect.sizeDelta = new Vector2(720f, 440f);
            EditorUtility.SetDirty(hudRect);
        }

        LayoutPanel(hud.transform, "HUD_Root", Vector2.zero, new Vector2(720f, 440f));
        LayoutPanel(hud.transform, "Status_Panel", new Vector2(-270f, 112f), new Vector2(205f, 235f));
        LayoutPanel(hud.transform, "Energy_Panel", new Vector2(270f, 122f), new Vector2(235f, 160f));
        LayoutPanel(hud.transform, "Hint_Panel", new Vector2(0f, -154f), new Vector2(545f, 92f));
        LayoutPanel(hud.transform, "Intro_Panel", new Vector2(0f, -98f), new Vector2(650f, 155f));
        LayoutPanel(hud.transform, "Result_Panel", Vector2.zero, new Vector2(620f, 455f));

        LayoutText(hud.transform, "Timer_Text", new Vector2(0f, 90f), new Vector2(174f, 32f), 24);
        LayoutText(hud.transform, "Score_Text", new Vector2(0f, 47f), new Vector2(174f, 32f), 24);
        LayoutText(hud.transform, "TotalScore_Text", new Vector2(0f, 4f), new Vector2(174f, 32f), 22);
        LayoutText(hud.transform, "Reps_Text", new Vector2(0f, -39f), new Vector2(174f, 32f), 24);
        LayoutText(hud.transform, "Combo_Text", new Vector2(0f, -82f), new Vector2(174f, 32f), 22);
        LayoutText(hud.transform, "Energy_Text", new Vector2(0f, 43f), new Vector2(202f, 34f), 24);
        LayoutText(hud.transform, "Grade_Text", new Vector2(0f, -8f), new Vector2(202f, 40f), 30);
        LayoutText(hud.transform, "Hint_Text", new Vector2(0f, 14f), new Vector2(498f, 34f), 23);
        LayoutText(hud.transform, "Result_Title_Text", new Vector2(0f, 108f), new Vector2(500f, 46f), 34);
        LayoutText(hud.transform, "Result_Score_Text", new Vector2(0f, 42f), new Vector2(500f, 42f), 29);
        LayoutText(hud.transform, "Result_Details_Text", new Vector2(0f, -68f), new Vector2(500f, 120f), 24);

        LayoutPanel(hud.transform, "Energy_Progress", new Vector2(0f, -57f), new Vector2(184f, 16f));
        LayoutPanel(hud.transform, "Action_Progress", new Vector2(0f, -25f), new Vector2(462f, 16f));
        LayoutPanel(hud.transform, "Tutorial_Button", new Vector2(-165f, 38f), new Vector2(305f, 84f));
        LayoutPanel(hud.transform, "SkipTutorial_Button", new Vector2(165f, 38f), new Vector2(305f, 84f));

        LayoutButtonLabel(hud.transform, "Tutorial_Button");
        LayoutButtonLabel(hud.transform, "SkipTutorial_Button");
    }

    static void LayoutPanel(Transform root, string name, Vector2 position, Vector2 size)
    {
        Transform found = FindChild(root, name);
        if (found == null) return;

        RectTransform rect = found.GetComponent<RectTransform>();
        if (rect == null) return;

        Undo.RecordObject(rect, "Layout Fun2 UI");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        EditorUtility.SetDirty(rect);
    }

    static void LayoutText(Transform root, string name, Vector2 position, Vector2 size, int fontSize)
    {
        Transform found = FindChild(root, name);
        if (found == null) return;

        TextMeshProUGUI text = found.GetComponent<TextMeshProUGUI>();
        RectTransform rect = found.GetComponent<RectTransform>();
        if (text == null || rect == null) return;

        Undo.RecordObject(text, "Layout Fun2 Text");
        Undo.RecordObject(rect, "Layout Fun2 Text");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.enableWordWrapping = false;
        EditorUtility.SetDirty(text);
        EditorUtility.SetDirty(rect);
    }

    static void LayoutButtonLabel(Transform root, string buttonName)
    {
        Transform button = FindChild(root, buttonName);
        if (button == null) return;

        Transform label = FindChild(button, "Label");
        if (label == null) return;

        RectTransform labelRect = label.GetComponent<RectTransform>();
        TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();
        if (labelRect == null || labelText == null) return;

        Undo.RecordObject(labelRect, "Layout Fun2 Button Label");
        Undo.RecordObject(labelText, "Layout Fun2 Button Label");
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelText.fontSize = 22;
        labelText.fontStyle = FontStyles.Bold;
        labelText.alignment = TextAlignmentOptions.Center;
        EditorUtility.SetDirty(labelRect);
        EditorUtility.SetDirty(labelText);
    }

    static Transform FindChild(Transform root, string name)
    {
        if (root.name == name) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChild(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }
}
