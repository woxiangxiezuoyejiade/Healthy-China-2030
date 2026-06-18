using System;
using UnityEngine;

[Serializable]
public class Game1_DifficultyConfig
{
    [Header("Round")]
    public float roundTime = 90f;
    public int targetReps = 12;
    public int minimumSuccessReps = 10;
    public int maxRoundScore = 1500;

    [Header("External Rotation Angles")]
    public float startThresholdAngle = 8f;
    public float returnThresholdAngle = 10f;
    public float minValidAngle = 30f;
    public float targetAngle = 45f;
    public float excellentMinAngle = 45f;
    public float excellentMaxAngle = 60f;
    public float goodMinAngle = 40f;
    public float goodMaxAngle = 65f;
    public float improveMinAngle = 30f;
    public float improveMaxAngle = 75f;
    public float overRotationWarningAngle = 75f;
    public float maxSafeAngle = 80f;
    public float holdDropTolerance = 8f;

    [Header("Timing")]
    public float holdTime = 0.5f;
    public float excellentHoldTime = 0.8f;
    public float improveHoldTime = 0.3f;
    public float minActionTime = 0.8f;
    public float excellentMinActionTime = 1.8f;
    public float excellentMaxActionTime = 3.0f;
    public float goodMinActionTime = 1.2f;
    public float goodMaxActionTime = 4.0f;
    public float maxActionTime = 5.0f;

    [Header("Posture Guard")]
    public float maxHorizontalDisplacement = 0.25f;
    public float maxVerticalDisplacement = 0.20f;
    public float maxForwardDisplacement = 0.20f;
    public float maxLinearSpeed = 0.8f;
    public float maxAngularSpeed = 220f;
    public float maxHeadYawChange = 20f;

    public static Game1_DifficultyConfig Simple()
    {
        return new Game1_DifficultyConfig
        {
            targetAngle = 35f,
            minValidAngle = 30f,
            excellentMinAngle = 35f,
            excellentMaxAngle = 45f,
            goodMinAngle = 30f,
            goodMaxAngle = 50f,
            improveMinAngle = 25f,
            improveMaxAngle = 60f,
            holdTime = 0.3f,
            targetReps = 8,
            minimumSuccessReps = 6
        };
    }

    public static Game1_DifficultyConfig Normal()
    {
        return new Game1_DifficultyConfig();
    }

    public static Game1_DifficultyConfig Advanced()
    {
        return new Game1_DifficultyConfig
        {
            roundTime = 100f,
            targetReps = 15,
            minimumSuccessReps = 12,
            targetAngle = 60f,
            minValidAngle = 45f,
            excellentMinAngle = 60f,
            excellentMaxAngle = 70f,
            goodMinAngle = 55f,
            goodMaxAngle = 75f,
            improveMinAngle = 45f,
            improveMaxAngle = 80f,
            holdTime = 0.8f
        };
    }
}
