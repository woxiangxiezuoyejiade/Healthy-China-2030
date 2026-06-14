using System;
using UnityEngine;

[Serializable]
public class Game3_DifficultyConfig
{
    [Header("回合设置")]
    public float roundTime = 90f;
    public int targetReps = 10;
    public int minimumSuccessReps = 8;
    public int maxRoundScore = 1500;

    [Header("肩胛后缩距离")]
    public float startThresholdDistance = 0.02f;
    public float returnThresholdDistance = 0.025f;
    public float minValidRetraction = 0.04f;
    public float targetRetraction = 0.08f;
    public float excellentMinRetraction = 0.08f;
    public float excellentMaxRetraction = 0.13f;
    public float goodMinRetraction = 0.065f;
    public float goodMaxRetraction = 0.14f;
    public float improveMinRetraction = 0.04f;
    public float improveMaxRetraction = 0.15f;
    public float maxSafeRetraction = 0.16f;
    public float holdDropTolerance = 0.02f;

    [Header("时间参数")]
    public float holdTime = 0.5f;
    public float excellentHoldTime = 0.8f;
    public float improveHoldTime = 0.3f;
    public float minActionTime = 1.0f;
    public float excellentMinActionTime = 2.0f;
    public float excellentMaxActionTime = 4.0f;
    public float goodMinActionTime = 1.5f;
    public float goodMaxActionTime = 5.0f;
    public float maxActionTime = 6.0f;

    [Header("稳定性检测")]
    public float maxPositionVariance = 0.008f;
    public float currentTriggerVariance = 0.005f;
    public float maxCurrentIntensity = 0.85f;
    public float maxHorizontalDisplacement = 0.20f;
    public float maxVerticalDisplacement = 0.15f;
    public float maxForwardDisplacement = 0.15f;
    public float maxLinearSpeed = 0.6f;
    public float maxHeadYawChange = 15f;
    public float maxShoulderElevation = 0.05f;

    [Header("对称性")]
    public float symmetryTolerance = 0.03f;

    public static Game3_DifficultyConfig Simple()
    {
        return new Game3_DifficultyConfig
        {
            targetRetraction = 0.06f,
            minValidRetraction = 0.035f,
            excellentMinRetraction = 0.06f,
            excellentMaxRetraction = 0.10f,
            goodMinRetraction = 0.05f,
            goodMaxRetraction = 0.12f,
            improveMinRetraction = 0.035f,
            improveMaxRetraction = 0.14f,
            holdTime = 0.3f,
            targetReps = 8,
            minimumSuccessReps = 6,
            maxPositionVariance = 0.012f,
            currentTriggerVariance = 0.008f
        };
    }

    public static Game3_DifficultyConfig Normal()
    {
        return new Game3_DifficultyConfig();
    }

    public static Game3_DifficultyConfig Advanced()
    {
        return new Game3_DifficultyConfig
        {
            roundTime = 100f,
            targetReps = 12,
            minimumSuccessReps = 10,
            targetRetraction = 0.10f,
            minValidRetraction = 0.06f,
            excellentMinRetraction = 0.10f,
            excellentMaxRetraction = 0.14f,
            goodMinRetraction = 0.08f,
            goodMaxRetraction = 0.15f,
            improveMinRetraction = 0.06f,
            improveMaxRetraction = 0.16f,
            holdTime = 0.8f,
            maxPositionVariance = 0.005f,
            currentTriggerVariance = 0.003f
        };
    }
}
