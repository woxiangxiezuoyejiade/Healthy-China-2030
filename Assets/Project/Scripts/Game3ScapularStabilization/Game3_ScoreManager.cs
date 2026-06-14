using UnityEngine;

public class Game3_ScoreManager : MonoBehaviour
{
    public int RoundScore { get; private set; }
    public int ComboBonus { get; private set; }
    public int FinishBonus { get; private set; }
    public int CurrentCombo { get; private set; }
    public int BestCombo { get; private set; }
    public int CompletedReps { get; private set; }
    public int ValidReps { get; private set; }
    public int TotalActionScore { get; private set; }

    Game3_DifficultyConfig config;

    public void Configure(Game3_DifficultyConfig difficultyConfig)
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
        TotalActionScore = 0;
    }

    public int AddActionResult(Game3_ActionResult result, out Game3_ActionGrade grade)
    {
        CompletedReps++;

        if (!result.isValid)
        {
            CurrentCombo = 0;
            grade = Game3_ActionGrade.Invalid;
            return 0;
        }

        int actionScore = CalculateActionScore(result, out grade);
        ValidReps++;
        TotalActionScore += actionScore;
        RoundScore += actionScore;

        if (grade == Game3_ActionGrade.Good || grade == Game3_ActionGrade.Excellent)
        {
            CurrentCombo++;
            BestCombo = Mathf.Max(BestCombo, CurrentCombo);
            ComboBonus += CalculateComboBonus(CurrentCombo);
        }
        else
        {
            CurrentCombo = 0;
        }

        if (actionScore >= 90)
        {
            ComboBonus += 15;
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
        if (ValidReps == 0) return 0f;
        return (float)TotalActionScore / ValidReps;
    }

    int CalculateActionScore(Game3_ActionResult result, out Game3_ActionGrade grade)
    {
        int total = CalculateRetractionScore(result.retractionDepth) +
                    CalculateHoldScore(result.holdDuration) +
                    CalculateStabilityScore(result.stabilityScore, result.currentIntensity) +
                    CalculateSymmetryScore(result.symmetryScore) +
                    CalculatePostureScore(result);

        if (total >= 85) grade = Game3_ActionGrade.Excellent;
        else if (total >= 65) grade = Game3_ActionGrade.Good;
        else if (total >= 40) grade = Game3_ActionGrade.NeedsImprovement;
        else grade = Game3_ActionGrade.Invalid;

        return total;
    }

    int CalculateRetractionScore(float depth)
    {
        if (depth >= config.excellentMinRetraction && depth <= config.excellentMaxRetraction) return 30;
        if (depth >= config.goodMinRetraction && depth <= config.goodMaxRetraction) return 22;
        if (depth >= config.improveMinRetraction && depth <= config.improveMaxRetraction) return 12;
        return 0;
    }

    int CalculateHoldScore(float holdDuration)
    {
        if (holdDuration >= config.excellentHoldTime) return 20;
        if (holdDuration >= config.holdTime) return 15;
        if (holdDuration >= config.improveHoldTime) return 8;
        return 0;
    }

    int CalculateStabilityScore(float stability, float currentIntensity)
    {
        if (currentIntensity > 0.5f) return 0;
        if (stability >= 0.9f && currentIntensity < 0.1f) return 20;
        if (stability >= 0.7f && currentIntensity < 0.3f) return 14;
        if (stability >= 0.5f) return 8;
        return 3;
    }

    int CalculateSymmetryScore(float symmetry)
    {
        if (symmetry >= 0.9f) return 15;
        if (symmetry >= 0.7f) return 10;
        if (symmetry >= 0.5f) return 5;
        return 0;
    }

    int CalculatePostureScore(Game3_ActionResult result)
    {
        if (!result.postureStable) return 0;

        bool excellent =
            result.maxHorizontalDisplacement <= config.maxHorizontalDisplacement * 0.6f &&
            result.maxVerticalDisplacement <= config.maxVerticalDisplacement * 0.6f &&
            result.maxForwardDisplacement <= config.maxForwardDisplacement * 0.6f &&
            result.maxLinearSpeed <= config.maxLinearSpeed * 0.75f;

        if (excellent) return 15;
        return 8;
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
