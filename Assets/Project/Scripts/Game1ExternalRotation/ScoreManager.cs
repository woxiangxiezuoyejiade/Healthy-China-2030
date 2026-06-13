using UnityEngine;

public class Game1_ScoreManager : MonoBehaviour
{
    public int RoundScore { get; private set; }
    public int ComboBonus { get; private set; }
    public int FinishBonus { get; private set; }
    public int CurrentCombo { get; private set; }
    public int BestCombo { get; private set; }
    public int CompletedReps { get; private set; }
    public int ValidReps { get; private set; }
    public int ScoredReps { get; private set; }
    public int TotalActionScore { get; private set; }

    Game1_DifficultyConfig config;

    public void Configure(Game1_DifficultyConfig difficultyConfig)
    {
        config = difficultyConfig;
        ResetScore();
    }

    public void ResetScore()
    {
        RoundScore = 0;
        ComboBonus = 0;
        FinishBonus = 0;
        CurrentCombo = 0;
        BestCombo = 0;
        CompletedReps = 0;
        ValidReps = 0;
        ScoredReps = 0;
        TotalActionScore = 0;
    }

    public int AddActionResult(Game1_ActionResult result, out Game1_ActionGrade grade)
    {
        CompletedReps++;

        int actionScore = CalculateActionScore(result, out grade);
        if (actionScore > 0)
        {
            ScoredReps++;
            TotalActionScore += actionScore;
            RoundScore += actionScore;
        }

        if (actionScore >= 25)
        {
            ValidReps++;
        }

        if (grade == Game1_ActionGrade.Good || grade == Game1_ActionGrade.Excellent)
        {
            CurrentCombo++;
            BestCombo = Mathf.Max(BestCombo, CurrentCombo);
            ComboBonus += CalculateComboBonus(CurrentCombo);
        }
        else
        {
            CurrentCombo = 0;
        }

        if (actionScore >= 95)
        {
            ComboBonus += 20;
        }

        return actionScore;
    }

    public void ApplyFinishBonus()
    {
        FinishBonus = ValidReps >= config.targetReps ? 100 : 0;
    }

    public int GetFinalScore()
    {
        ApplyFinishBonus();
        return Mathf.Clamp(RoundScore + ComboBonus + FinishBonus, 0, config.maxRoundScore);
    }

    public float GetAverageActionScore()
    {
        if (ScoredReps == 0) return 0f;
        return (float)TotalActionScore / ScoredReps;
    }

    int CalculateActionScore(Game1_ActionResult result, out Game1_ActionGrade grade)
    {
        float completion = Mathf.Clamp01(result.peakAngle / Mathf.Max(0.01f, config.targetAngle));
        int total = Mathf.RoundToInt(completion * 100f);

        if (!result.isValid || !result.postureStable)
        {
            total = Mathf.Min(total, 20);
        }

        if (total >= 95) grade = Game1_ActionGrade.Excellent;
        else if (total >= 75) grade = Game1_ActionGrade.Good;
        else if (total >= 25) grade = Game1_ActionGrade.NeedsImprovement;
        else grade = Game1_ActionGrade.Invalid;

        return total;
    }

    int CalculateAngleScore(float angle)
    {
        if (angle >= config.excellentMinAngle && angle <= config.excellentMaxAngle) return 40;
        if (angle >= config.goodMinAngle && angle <= config.goodMaxAngle) return 30;
        if (angle >= config.improveMinAngle && angle <= config.improveMaxAngle) return 15;
        return 0;
    }

    int CalculateSpeedScore(float duration)
    {
        if (duration >= config.excellentMinActionTime && duration <= config.excellentMaxActionTime) return 25;
        if (duration >= config.goodMinActionTime && duration <= config.goodMaxActionTime) return 18;
        if (duration >= config.minActionTime && duration <= config.maxActionTime) return 10;
        return 0;
    }

    int CalculateHoldScore(float holdDuration)
    {
        if (holdDuration >= config.excellentHoldTime) return 20;
        if (holdDuration >= config.holdTime) return 15;
        if (holdDuration >= config.improveHoldTime) return 8;
        return 0;
    }

    int CalculatePostureScore(Game1_ActionResult result)
    {
        if (!result.postureStable) return 0;

        bool excellent =
            result.maxHorizontalDisplacement <= config.maxHorizontalDisplacement * 0.6f &&
            result.maxVerticalDisplacement <= config.maxVerticalDisplacement * 0.6f &&
            result.maxForwardDisplacement <= config.maxForwardDisplacement * 0.6f &&
            result.maxLinearSpeed <= config.maxLinearSpeed * 0.75f &&
            result.maxAngularSpeed <= config.maxAngularSpeed * 0.75f;

        if (excellent) return 15;

        bool good =
            result.maxHorizontalDisplacement <= config.maxHorizontalDisplacement * 0.85f &&
            result.maxVerticalDisplacement <= config.maxVerticalDisplacement * 0.85f &&
            result.maxForwardDisplacement <= config.maxForwardDisplacement * 0.85f;

        return good ? 10 : 5;
    }

    int CalculateComboBonus(int combo)
    {
        if (combo == 2) return 10;
        if (combo == 3) return 20;
        if (combo == 5) return 50;
        if (combo == 8) return 100;
        if (combo == 12) return 150;
        return 0;
    }
}
