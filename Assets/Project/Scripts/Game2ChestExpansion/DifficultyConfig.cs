using System;
using UnityEngine;

[Serializable]
public class Game2_DifficultyConfig
{
    [Header("Round")]
    public float roundTime = 90f;
    public int targetReps = 10;
    public int minimumSuccessReps = 8;

    [Header("Chest Expansion")]
    public float startThreshold = 0.12f;
    public float returnThreshold = 0.10f;
    public float minValidExpansion = 0.30f;
    public float targetExpansion = 0.50f;
    public float excellentExpansion = 0.60f;
    public float maxSafeExpansion = 0.90f;

    [Header("Timing")]
    public float minActionTime = 0.8f;
    public float goodMinActionTime = 1.2f;
    public float goodMaxActionTime = 4.0f;
    public float excellentMinActionTime = 1.8f;
    public float excellentMaxActionTime = 3.2f;
    public float maxActionTime = 5.0f;
    public float holdTime = 0.45f;
    public float excellentHoldTime = 0.75f;

    [Header("Posture Guard")]
    public float maxHandHeightDifference = 0.35f;
    public float maxAverageHeightChange = 0.28f;
    public float maxForwardBackChange = 0.40f;
    public float maxHeadYawChange = 25f;
}
