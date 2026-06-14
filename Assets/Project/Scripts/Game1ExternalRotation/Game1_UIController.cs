using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Game1_UIController : MonoBehaviour
{
    [Header("HUD")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI repsText;
    public TextMeshProUGUI comboText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI handText;
    public TextMeshProUGUI targetAngleText;
    public Slider rotationProgress;

    [Header("Panels")]
    public GameObject introPanel;
    public GameObject resultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultScoreText;
    public TextMeshProUGUI resultDetailsText;

    public void ShowIntro(Game1_TrainingHand hand, Game1_DifficultyConfig config)
    {
        SetPanel(introPanel, true);
        SetPanel(resultPanel, false);
        SetTrainingInfo(hand, config);
        SetHint(Game1_Text.HintSitFacingCore);
    }

    public void ShowPlaying()
    {
        SetPanel(introPanel, false);
        SetPanel(resultPanel, false);
    }

    public void UpdateHud(float remainingTime, int roundScore, int totalScore, int validReps, int targetReps, int combo)
    {
        if (timerText != null) timerText.text = $"{Game1_Text.LabelTime} {Mathf.CeilToInt(remainingTime)}s";
        if (scoreText != null) scoreText.text = $"{Game1_Text.LabelRoundScore} {roundScore}";
        if (totalScoreText != null) totalScoreText.text = $"{Game1_Text.LabelTotalScore} {totalScore}";
        if (repsText != null) repsText.text = $"{Game1_Text.LabelCompleted} {validReps}/{targetReps}";
        if (comboText != null) comboText.text = combo > 1 ? $"{Game1_Text.LabelCombo} x{combo}" : Game1_Text.LabelComboEmpty;
    }

    public void SetTrainingInfo(Game1_TrainingHand hand, Game1_DifficultyConfig config)
    {
        if (handText != null) handText.text = hand == Game1_TrainingHand.Right ? Game1_Text.LabelRightHand : Game1_Text.LabelLeftHand;
        if (targetAngleText != null) targetAngleText.text = $"{Game1_Text.LabelTargetAngle}: {config.targetAngle:0}\u00b0";
    }

    public void SetProgress(float progress)
    {
        if (rotationProgress != null) rotationProgress.value = Mathf.Clamp01(progress);
    }

    public void SetHint(string message)
    {
        if (hintText != null) hintText.text = message;
    }

    public void ShowGrade(Game1_ActionGrade grade, int actionScore)
    {
        if (gradeText == null) return;

        switch (grade)
        {
            case Game1_ActionGrade.Excellent:
                gradeText.text = $"{Game1_Text.GradeExcellent} +{actionScore}";
                gradeText.color = new Color(1f, 0.85f, 0.35f);
                break;
            case Game1_ActionGrade.Good:
                gradeText.text = $"{Game1_Text.GradeGood} +{actionScore}";
                gradeText.color = new Color(0.36f, 0.75f, 1f);
                break;
            case Game1_ActionGrade.NeedsImprovement:
                gradeText.text = $"{Game1_Text.GradeNeedsImprovement} +{actionScore}";
                gradeText.color = new Color(0.5f, 0.84f, 0.76f);
                break;
            default:
                gradeText.text = Game1_Text.GradeInvalid;
                gradeText.color = new Color(1f, 0.54f, 0.40f);
                break;
        }
    }

    public void ShowResult(int finalScore, int validReps, int targetReps, int bestCombo, float averageScore, bool success)
    {
        SetPanel(resultPanel, true);

        if (resultTitleText != null)
        {
            resultTitleText.text = success ? Game1_Text.ResultSuccess : Game1_Text.ResultKeepGoing;
        }

        if (resultScoreText != null)
        {
            resultScoreText.text = $"{Game1_Text.ResultRoundScore} {finalScore}";
        }

        if (resultDetailsText != null)
        {
            resultDetailsText.text =
                $"{Game1_Text.ResultReps} {validReps}/{targetReps}\n" +
                $"{Game1_Text.ResultBestCombo} x{bestCombo}\n" +
                $"{Game1_Text.ResultAverage} {averageScore:0}\n" +
                (success ? Game1_Text.ResultCoreCharged : Game1_Text.ResultTryAgain);
        }
    }

    void SetPanel(GameObject panel, bool active)
    {
        if (panel != null) panel.SetActive(active);
    }
}
