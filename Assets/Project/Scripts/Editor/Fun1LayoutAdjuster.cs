using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Fun1LayoutAdjuster
{
    static readonly Vector2 StatusPosition = new Vector2(-218f, 102f);
    static readonly Vector2 StatusSize = new Vector2(188f, 216f);
    static readonly Vector2 InfoPosition = new Vector2(205f, 110f);
    static readonly Vector2 InfoSize = new Vector2(206f, 136f);
    static readonly Vector2 ResultPosition = new Vector2(0f, 44f);
    static readonly Vector2 ResultSize = new Vector2(360f, 510f);

    [MenuItem("Tools/Healthy China/Fun1/Adjust HUD Layout")]
    public static void AdjustHudLayout()
    {
        GameObject hudRoot = GameObject.Find("Game1_EditableHUD");
        if (hudRoot == null) hudRoot = GameObject.Find("Game1_AutoHUD");

        if (hudRoot == null)
        {
            EditorUtility.DisplayDialog("Fun1 HUD Layout", "Open Func1 scene first, then create or bake the HUD.", "OK");
            return;
        }

        GameObject statusPanel = FindChild(hudRoot.transform, "Status_Panel");
        GameObject infoPanel = FindChild(hudRoot.transform, "Info_Panel");
        GameObject hintPanel = FindChild(hudRoot.transform, "Hint_Panel");
        GameObject resultPanel = FindChild(hudRoot.transform, "Result_Panel");

        if (statusPanel != null)
        {
            LayoutPanel(statusPanel, StatusPosition, StatusSize);
            LayoutText(statusPanel.transform, "Timer_Text", new Vector2(0f, 84f), new Vector2(160f, 30f), 21, TextAlignmentOptions.Left);
            LayoutText(statusPanel.transform, "Score_Text", new Vector2(0f, 45f), new Vector2(160f, 30f), 21, TextAlignmentOptions.Left);
            LayoutText(statusPanel.transform, "TotalScore_Text", new Vector2(0f, 6f), new Vector2(160f, 30f), 19, TextAlignmentOptions.Left);
            LayoutText(statusPanel.transform, "Reps_Text", new Vector2(0f, -33f), new Vector2(160f, 30f), 21, TextAlignmentOptions.Left);
            LayoutText(statusPanel.transform, "Combo_Text", new Vector2(0f, -73f), new Vector2(160f, 30f), 19, TextAlignmentOptions.Left);
        }

        if (infoPanel != null)
        {
            LayoutPanel(infoPanel, InfoPosition, InfoSize);
            LayoutText(infoPanel.transform, "Hand_Text", new Vector2(0f, 34f), new Vector2(178f, 32f), 22, TextAlignmentOptions.Center);
            LayoutText(infoPanel.transform, "TargetAngle_Text", new Vector2(0f, -6f), new Vector2(178f, 32f), 20, TextAlignmentOptions.Center);
            LayoutText(infoPanel.transform, "Grade_Text", new Vector2(0f, -48f), new Vector2(178f, 36f), 25, TextAlignmentOptions.Center);
        }

        if (hintPanel != null)
        {
            LayoutPanel(hintPanel, new Vector2(0f, -122f), new Vector2(430f, 58f));
        }

        if (resultPanel != null)
        {
            LayoutPanel(resultPanel, ResultPosition, ResultSize);
            LayoutText(resultPanel.transform, "Result_Title_Text", new Vector2(0f, 72f), new Vector2(255f, 36f), 26, TextAlignmentOptions.Center);
            LayoutText(resultPanel.transform, "Result_Score_Text", new Vector2(0f, 19f), new Vector2(255f, 34f), 20, TextAlignmentOptions.Center);
            LayoutText(resultPanel.transform, "Result_Details_Text", new Vector2(0f, -34f), new Vector2(255f, 34f), 20, TextAlignmentOptions.Center);
            LayoutText(resultPanel.transform, "Result_Extra_Text", new Vector2(0f, -87f), new Vector2(270f, 34f), 18, TextAlignmentOptions.Center);
        }

        EditorSceneManager.MarkSceneDirty(hudRoot.scene);
        Selection.activeGameObject = hudRoot;
        EditorUtility.DisplayDialog("Fun1 HUD Layout", "Func1 HUD layout has been adjusted.", "OK");
    }

    static void LayoutPanel(GameObject panel, Vector2 position, Vector2 size)
    {
        RectTransform rect = panel.GetComponent<RectTransform>();
        if (rect == null) return;

        Undo.RecordObject(rect, "Adjust Fun1 HUD Layout");
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        EditorUtility.SetDirty(rect);
    }

    static void LayoutText(Transform root, string name, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = FindChild(root, name);
        if (textObject == null) return;

        RectTransform rect = textObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            Undo.RecordObject(rect, "Adjust Fun1 HUD Text");
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            EditorUtility.SetDirty(rect);
        }

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            Undo.RecordObject(text, "Adjust Fun1 HUD Text");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.enableWordWrapping = false;
            text.raycastTarget = false;
            EditorUtility.SetDirty(text);
        }
    }

    static GameObject FindChild(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root.gameObject;

        for (int i = 0; i < root.childCount; i++)
        {
            GameObject found = FindChild(root.GetChild(i), name);
            if (found != null) return found;
        }

        return null;
    }
}
