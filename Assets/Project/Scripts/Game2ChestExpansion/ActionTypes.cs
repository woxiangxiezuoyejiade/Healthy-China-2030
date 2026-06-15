public enum Game2_ActionState
{
    WaitingStart,
    Opening,
    Holding,
    Returning
}

public enum Game2_ActionGrade
{
    Invalid,
    NeedsImprovement,
    Good,
    Excellent
}

public struct Game2_ActionResult
{
    public bool isValid;
    public string feedback;
    public float peakExpansion;
    public float actionDuration;
    public float holdDuration;
    public float maxHandHeightDifference;
    public float maxAverageHeightChange;
    public float maxForwardBackChange;
    public bool postureStable;
    public Game2_ActionGrade grade;

    public static Game2_ActionResult Invalid(string reason, float peakExpansion)
    {
        return new Game2_ActionResult
        {
            isValid = false,
            feedback = reason,
            peakExpansion = peakExpansion,
            grade = Game2_ActionGrade.Invalid
        };
    }
}
