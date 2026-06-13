public enum Game1_GameState
{
    Intro,
    Countdown,
    Playing,
    Result,
    Finished
}

public enum Game1_ActionState
{
    WaitingStart,
    RotatingOut,
    Holding,
    Returning
}

public enum Game1_ActionGrade
{
    Invalid,
    NeedsImprovement,
    Good,
    Excellent
}

public enum Game1_TrainingHand
{
    Right,
    Left
}

public enum Game1_Difficulty
{
    Simple,
    Normal,
    Advanced
}

public struct Game1_ActionResult
{
    public bool isValid;
    public string feedback;
    public float peakAngle;
    public float actionDuration;
    public float holdDuration;
    public float maxLinearSpeed;
    public float maxAngularSpeed;
    public float maxHorizontalDisplacement;
    public float maxVerticalDisplacement;
    public float maxForwardDisplacement;
    public bool postureStable;
    public Game1_ActionGrade grade;

    public static Game1_ActionResult Invalid(string reason, float peakAngle)
    {
        return new Game1_ActionResult
        {
            isValid = false,
            feedback = reason,
            peakAngle = peakAngle,
            grade = Game1_ActionGrade.Invalid
        };
    }
}
