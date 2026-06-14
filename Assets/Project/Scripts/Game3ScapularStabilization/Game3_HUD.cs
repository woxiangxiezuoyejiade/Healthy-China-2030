using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 射击行动 - HUD系统
/// 显示分数、倒计时、连击、动作反馈、准星
/// </summary>
public class Game3_HUD : MonoBehaviour
{
    [Header("HUD设置")]
    public float feedbackDisplayTime = 1.5f;

    // HUD元素
    GameObject hudCanvas;
    UnityEngine.UI.Text scoreText;
    UnityEngine.UI.Text timerText;
    UnityEngine.UI.Text comboText;
    UnityEngine.UI.Text feedbackText;
    UnityEngine.UI.Text countdownText;
    GameObject crosshair;
    GameObject resultPanel;
    UnityEngine.UI.Text resultScoreText;
    UnityEngine.UI.Text resultHitsText;
    UnityEngine.UI.Text resultComboText;
    UnityEngine.UI.Text resultGradeText;
    UnityEngine.UI.Button resultRestartBtn;
    UnityEngine.UI.Button resultMenuBtn;

    Coroutine feedbackRoutine;

    public void CreateHUD()
    {
        // 创建Canvas
        hudCanvas = new GameObject("HUD_Canvas");
        hudCanvas.transform.SetParent(transform);
        Canvas canvas = hudCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        hudCanvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        hudCanvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ===== 准星 =====
        CreateCrosshair();

        // ===== 顶部信息栏 =====
        CreateTopBar();

        // ===== 动作反馈 =====
        CreateFeedbackText();

        // ===== 倒计时 =====
        CreateCountdownText();

        // ===== 结算面板 =====
        CreateResultPanel();

        // 初始隐藏
        SetGameHUDVisible(false);
        SetResultVisible(false);
        SetCountdownVisible(false);
    }

    void CreateCrosshair()
    {
        crosshair = new GameObject("Crosshair");
        crosshair.transform.SetParent(hudCanvas.transform);

        // 中心点
        GameObject center = new GameObject("Center");
        center.transform.SetParent(crosshair.transform);
        UnityEngine.UI.Image img = center.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(1f, 1f, 1f, 0.8f);
        center.GetComponent<RectTransform>().sizeDelta = new Vector2(6f, 6f);
        center.GetComponent<RectTransform>().anchorMin = new Vector2(0.5f, 0.5f);
        center.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0.5f);
        center.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        // 四条线
        float gap = 8f, len = 12f, width = 2f;
        CreateCrosshairLine("Top", new Vector2(0, gap), new Vector2(width, len));
        CreateCrosshairLine("Bottom", new Vector2(0, -gap - len), new Vector2(width, len));
        CreateCrosshairLine("Left", new Vector2(-gap - len, 0), new Vector2(len, width));
        CreateCrosshairLine("Right", new Vector2(gap, 0), new Vector2(len, width));
    }

    void CreateCrosshairLine(string name, Vector2 pos, Vector2 size)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(crosshair.transform);
        UnityEngine.UI.Image img = line.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(1f, 1f, 1f, 0.6f);
        RectTransform rt = line.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
    }

    void CreateTopBar()
    {
        // 分数 - 左上
        scoreText = CreateTextElement("ScoreText", new Vector2(0f, 1f), new Vector2(200f, 50f),
            new Vector2(20f, -25f), 28, Color.white, TextAnchor.MiddleLeft);
        scoreText.text = "得分: 0";

        // 倒计时 - 右上
        timerText = CreateTextElement("TimerText", new Vector2(1f, 1f), new Vector2(200f, 50f),
            new Vector2(-20f, -25f), 28, Color.white, TextAnchor.MiddleRight);
        timerText.text = "90";

        // 连击 - 顶部中央
        comboText = CreateTextElement("ComboText", new Vector2(0.5f, 1f), new Vector2(300f, 50f),
            new Vector2(0f, -25f), 26, new Color(1f, 0.85f, 0f), TextAnchor.MiddleCenter);
        comboText.text = "";
    }

    void CreateFeedbackText()
    {
        feedbackText = CreateTextElement("FeedbackText", new Vector2(0.5f, 0.5f), new Vector2(500f, 80f),
            new Vector2(0f, 80f), 32, Color.white, TextAnchor.MiddleCenter);
        feedbackText.text = "";
    }

    void CreateCountdownText()
    {
        countdownText = CreateTextElement("CountdownText", new Vector2(0.5f, 0.5f), new Vector2(300f, 200f),
            new Vector2(0f, 0f), 72, new Color(1f, 0.85f, 0f), TextAnchor.MiddleCenter);
        countdownText.text = "";
    }

    void CreateResultPanel()
    {
        resultPanel = new GameObject("ResultPanel");
        resultPanel.transform.SetParent(hudCanvas.transform);
        RectTransform rpRt = resultPanel.AddComponent<RectTransform>();
        rpRt.anchorMin = new Vector2(0.5f, 0.5f);
        rpRt.anchorMax = new Vector2(0.5f, 0.5f);
        rpRt.sizeDelta = new Vector2(500f, 400f);
        rpRt.anchoredPosition = Vector2.zero;

        // 半透明黑色背景
        UnityEngine.UI.Image bg = resultPanel.AddComponent<UnityEngine.UI.Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // 标题
        CreateChildText("ResultTitle", resultPanel.transform, new Vector2(0.5f, 1f), new Vector2(400f, 50f),
            new Vector2(0f, -40f), 32, new Color(1f, 0.85f, 0f), TextAnchor.MiddleCenter).text = "射击完成";

        // 得分
        resultScoreText = CreateChildText("ResultScore", resultPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(400f, 50f),
            new Vector2(0f, 80f), 40, Color.white, TextAnchor.MiddleCenter);

        // 命中次数
        resultHitsText = CreateChildText("ResultHits", resultPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(400f, 40f),
            new Vector2(0f, 20f), 24, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);

        // 最高连击
        resultComboText = CreateChildText("ResultCombo", resultPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(400f, 40f),
            new Vector2(0f, -20f), 24, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);

        // 评级
        resultGradeText = CreateChildText("ResultGrade", resultPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(400f, 60f),
            new Vector2(0f, -80f), 36, new Color(1f, 0.85f, 0f), TextAnchor.MiddleCenter);

        // 重新开始按钮
        resultRestartBtn = CreateButtonElement("RestartBtn", resultPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(180f, 45f), new Vector2(-110f, 40f), "再来一局");

        // 返回菜单按钮
        resultMenuBtn = CreateButtonElement("MenuBtn", resultPanel.transform,
            new Vector2(0.5f, 0f), new Vector2(180f, 45f), new Vector2(110f, 40f), "返回菜单");
    }

    UnityEngine.UI.Text CreateTextElement(string name, Vector2 anchor, Vector2 size, Vector2 pos, int fontSize, Color color, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(hudCanvas.transform);
        UnityEngine.UI.Text text = obj.AddComponent<UnityEngine.UI.Text>();
        text.font = GetSafeFont();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        return text;
    }

    UnityEngine.UI.Text CreateChildText(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos, int fontSize, Color color, TextAnchor alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        UnityEngine.UI.Text text = obj.AddComponent<UnityEngine.UI.Text>();
        text.font = GetSafeFont();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.fontStyle = FontStyle.Bold;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        return text;
    }

    UnityEngine.UI.Button CreateButtonElement(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos, string label)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);

        UnityEngine.UI.Image img = obj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.white;

        UnityEngine.UI.Button btn = obj.AddComponent<UnityEngine.UI.Button>();
        btn.targetGraphic = img;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        // 按钮文字
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(obj.transform);
        UnityEngine.UI.Text txt = labelObj.AddComponent<UnityEngine.UI.Text>();
        txt.font = GetSafeFont();
        txt.text = label;
        txt.fontSize = 20;
        txt.color = Color.black;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontStyle = FontStyle.Bold;

        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;
        labelRt.anchoredPosition = Vector2.zero;

        return btn;
    }

    // ===== 公共接口 =====

    public void SetGameHUDVisible(bool visible)
    {
        if (scoreText != null) scoreText.gameObject.SetActive(visible);
        if (timerText != null) timerText.gameObject.SetActive(visible);
        if (comboText != null) comboText.gameObject.SetActive(visible);
        if (crosshair != null) crosshair.SetActive(visible);
    }

    public void SetResultVisible(bool visible)
    {
        if (resultPanel != null) resultPanel.SetActive(visible);
    }

    public void SetCountdownVisible(bool visible)
    {
        if (countdownText != null) countdownText.gameObject.SetActive(visible);
    }

    public void UpdateScore(int score)
    {
        if (scoreText != null) scoreText.text = $"得分: {score}";
    }

    public void UpdateTimer(float time)
    {
        if (timerText == null) return;
        int seconds = Mathf.CeilToInt(Mathf.Max(0, time));
        timerText.text = seconds.ToString();
        timerText.color = seconds <= 10 ? Color.red : Color.white;
    }

    public void UpdateCombo(int combo)
    {
        if (comboText == null) return;
        if (combo >= 2)
        {
            comboText.text = $"{combo}连击!";
            comboText.color = combo >= 5 ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.85f, 0f);
        }
        else
        {
            comboText.text = "";
        }
    }

    public void ShowFeedback(string text, Color color)
    {
        if (feedbackText == null) return;
        if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
        feedbackRoutine = StartCoroutine(FeedbackRoutine(text, color));
    }

    IEnumerator FeedbackRoutine(string text, Color color)
    {
        feedbackText.text = text;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);

        yield return new WaitForSeconds(feedbackDisplayTime);

        feedbackText.gameObject.SetActive(false);
        feedbackRoutine = null;
    }

    public void ShowCountdown(int number)
    {
        if (countdownText == null) return;
        countdownText.text = number > 0 ? number.ToString() : "开始!";
        countdownText.gameObject.SetActive(true);
    }

    public void ShowResult(int score, int hits, int bestCombo, string grade)
    {
        if (resultScoreText != null) resultScoreText.text = $"最终得分: {score}";
        if (resultHitsText != null) resultHitsText.text = $"命中次数: {hits}";
        if (resultComboText != null) resultComboText.text = $"最高连击: {bestCombo}";
        if (resultGradeText != null) resultGradeText.text = grade;
        SetResultVisible(true);
    }

    public UnityEngine.UI.Button GetRestartButton() => resultRestartBtn;
    public UnityEngine.UI.Button GetMenuButton() => resultMenuBtn;

    static Font cachedFont;
    static Font GetSafeFont()
    {
        if (cachedFont != null) return cachedFont;
        // Unity 2021+ 使用 LegacyRuntime.ttf 替代 Arial.ttf
        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (cachedFont == null) cachedFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        if (cachedFont == null) cachedFont = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 14);
        return cachedFont;
    }
}
