using UnityEngine;

public class Game2_ScoreManager : MonoBehaviour
{
    public int RoundScore { get; private set; }
    public int CurrentCombo { get; private set; }
    public int BestCombo { get; private set; }
    public int CompletedReps { get; private set; }
    public int ValidReps { get; private set; }
    public int TotalActionScore { get; private set; }

    Game2_DifficultyConfig config;

    public void Configure(Game2_DifficultyConfig difficultyConfig)
    {
        config = difficultyConfig;
        ResetScore();
    }

    public void ResetScore()
    {
        RoundScore = 0;
        CurrentCombo = 0;
        BestCombo = 0;
        CompletedReps = 0;
        ValidReps = 0;
        TotalActionScore = 0;
    }

    public int AddActionResult(Game2_ActionResult result, out Game2_ActionGrade grade)
    {
        CompletedReps++;

        if (!result.isValid)
        {
            CurrentCombo = 0;
            grade = Game2_ActionGrade.Invalid;
            return 0;
        }

        int actionScore = CalculateActionScore(result, out grade);
        if (grade == Game2_ActionGrade.Invalid || actionScore <= 0)
        {
            CurrentCombo = 0;
            return 0;
        }

        ValidReps++;
        TotalActionScore += actionScore;
        RoundScore += actionScore;

        if (grade == Game2_ActionGrade.Good || grade == Game2_ActionGrade.Excellent)
        {
            CurrentCombo++;
            BestCombo = Mathf.Max(BestCombo, CurrentCombo);
            RoundScore += CalculateComboBonus(CurrentCombo);
        }
        else
        {
            CurrentCombo = 0;
        }

        result.grade = grade;
        return actionScore;
    }

    public bool IsRoundComplete()
    {
        return config != null && ValidReps >= config.targetReps;
    }

    public bool IsRoundSuccess()
    {
        return config != null && ValidReps >= config.minimumSuccessReps;
    }

    public float GetFogClearAmount(Game2_ActionGrade grade)
    {
        switch (grade)
        {
            case Game2_ActionGrade.Excellent:
                return 0.14f;
            case Game2_ActionGrade.Good:
                return 0.10f;
            case Game2_ActionGrade.NeedsImprovement:
                return 0.06f;
            default:
                return 0f;
        }
    }

    public float GetAverageActionScore()
    {
        if (ValidReps == 0) return 0f;
        return (float)TotalActionScore / ValidReps;
    }

    int CalculateActionScore(Game2_ActionResult result, out Game2_ActionGrade grade)
    {
        float completion = Mathf.Clamp01(result.peakExpansion / Mathf.Max(0.01f, config.targetExpansion));
        int total = Mathf.RoundToInt(completion * 100f);

        if (!result.postureStable)
        {
            total = Mathf.Min(total, 20);
        }
        else if (result.actionDuration < 0.25f)
        {
            total = Mathf.Min(total, 35);
        }

        if (total >= 95) grade = Game2_ActionGrade.Excellent;
        else if (total >= 75) grade = Game2_ActionGrade.Good;
        else if (total >= 25) grade = Game2_ActionGrade.NeedsImprovement;
        else grade = Game2_ActionGrade.Invalid;

        return total;
    }

    int CalculateExpansionScore(float expansion)
    {
        if (expansion >= config.excellentExpansion && expansion <= config.maxSafeExpansion) return 40;
        if (expansion >= config.targetExpansion) return 32;
        if (expansion >= config.minValidExpansion) return 18;
        return 0;
    }

    int CalculateTimingScore(float duration)
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
        return 0;
    }

    int CalculatePostureScore(Game2_ActionResult result)
    {
        if (!result.postureStable) return 0;

        bool excellent =
            result.maxHandHeightDifference <= config.maxHandHeightDifference * 0.55f &&
            result.maxAverageHeightChange <= config.maxAverageHeightChange * 0.55f &&
            result.maxForwardBackChange <= config.maxForwardBackChange * 0.55f;

        if (excellent) return 15;

        bool good =
            result.maxHandHeightDifference <= config.maxHandHeightDifference * 0.8f &&
            result.maxAverageHeightChange <= config.maxAverageHeightChange * 0.8f &&
            result.maxForwardBackChange <= config.maxForwardBackChange * 0.8f;

        return good ? 10 : 5;
    }

    int CalculateComboBonus(int combo)
    {
        if (combo == 3) return 20;
        if (combo == 5) return 50;
        if (combo == 8) return 100;
        if (combo == 10) return 150;
        return 0;
    }
}
