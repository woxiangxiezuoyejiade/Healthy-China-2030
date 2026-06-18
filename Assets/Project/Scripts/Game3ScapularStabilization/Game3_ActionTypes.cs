public enum Game3_GameState
{
    Intro,
    Countdown,
    Playing,
    Result,
    Finished
}

public enum Game3_ActionState
{
    WaitingStart,
    Retracting,
    Holding,
    Returning
}

public enum Game3_ActionGrade
{
    Invalid,
    NeedsImprovement,
    Good,
    Excellent
}

public enum Game3_Difficulty
{
    Simple,
    Normal,
    Advanced
}

public struct Game3_ActionResult
{
    public bool isValid;
    public string feedback;
    public float retractionDepth;
    public float holdDuration;
    public float actionDuration;
    public float stabilityScore;
    public float symmetryScore;
    public bool postureStable;
    public float currentIntensity;
    public float maxHorizontalDisplacement;
    public float maxVerticalDisplacement;
    public float maxForwardDisplacement;
    public float maxLinearSpeed;
    public Game3_ActionGrade grade;

    public static Game3_ActionResult Invalid(string reason, float retractionDepth, float currentIntensity)
    {
        return new Game3_ActionResult
        {
            isValid = false,
            feedback = reason,
            retractionDepth = retractionDepth,
            grade = Game3_ActionGrade.Invalid,
            currentIntensity = currentIntensity
        };
    }
}
