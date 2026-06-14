using System;
using System.Collections.Generic;
using UnityEngine;

public class Game3_ActionDetector : MonoBehaviour
{
    [Header("追踪对象")]
    public Transform rightController;
    public Transform leftController;
    public Transform head;

    public event Action<float> OnProgressChanged;
    public event Action<Game3_ActionResult> OnActionCompleted;
    public event Action<string> OnActionInvalid;
    public event Action<float> OnCurrentIntensityChanged;

    public Game3_ActionState CurrentState { get; private set; } = Game3_ActionState.WaitingStart;
    public float CurrentRetraction { get; private set; }
    public float CurrentStability { get; private set; }
    public float CurrentCurrentIntensity { get; private set; }

    Game3_DifficultyConfig config;
    Vector3 rightStartPos;
    Vector3 leftStartPos;
    Vector3 headStartPos;
    Quaternion headStartRot;
    Vector3 headForward;
    Vector3 headRight;
    float actionStartTime;
    float holdStartTime;
    float peakRetraction;
    float maxLinearSpeed;
    float maxHorizontalDisplacement;
    float maxVerticalDisplacement;
    float maxForwardDisplacement;
    Vector3 prevRightPos;
    Vector3 prevLeftPos;
    bool isCalibrated;

    const int StabilitySampleCount = 30;
    Queue<Vector3> rightPositionHistory = new Queue<Vector3>();
    Queue<Vector3> leftPositionHistory = new Queue<Vector3>();

    public void Configure(Game3_DifficultyConfig difficultyConfig)
    {
        config = difficultyConfig;
        ResetDetector();
    }

    public bool Calibrate()
    {
        if (rightController == null || leftController == null)
        {
            OnActionInvalid?.Invoke(Game3_Text.HintControllerMissing);
            return false;
        }

        rightStartPos = rightController.position;
        leftStartPos = leftController.position;
        prevRightPos = rightStartPos;
        prevLeftPos = leftStartPos;

        if (head != null)
        {
            headStartPos = head.position;
            headStartRot = head.rotation;
            headForward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            headRight = Vector3.ProjectOnPlane(head.right, Vector3.up).normalized;
        }
        else
        {
            headForward = Vector3.forward;
            headRight = Vector3.right;
        }

        isCalibrated = true;
        CurrentState = Game3_ActionState.WaitingStart;
        ResetActionMetrics();
        OnProgressChanged?.Invoke(0f);
        return true;
    }

    public void ResetDetector()
    {
        CurrentState = Game3_ActionState.WaitingStart;
        CurrentRetraction = 0f;
        CurrentStability = 1f;
        CurrentCurrentIntensity = 0f;
        isCalibrated = false;
        ResetActionMetrics();
        rightPositionHistory.Clear();
        leftPositionHistory.Clear();
    }

    public void Tick(float deltaTime)
    {
        if (!isCalibrated || rightController == null || leftController == null || config == null) return;

        UpdateRetraction();
        UpdateStability();
        UpdateCurrentIntensity();
        UpdateMotionMetrics(deltaTime);

        OnProgressChanged?.Invoke(Mathf.InverseLerp(0f, config.targetRetraction, CurrentRetraction));
        OnCurrentIntensityChanged?.Invoke(CurrentCurrentIntensity);

        switch (CurrentState)
        {
            case Game3_ActionState.WaitingStart:
                TickWaitingStart();
                break;
            case Game3_ActionState.Retracting:
                TickRetracting();
                break;
            case Game3_ActionState.Holding:
                TickHolding();
                break;
            case Game3_ActionState.Returning:
                TickReturning();
                break;
        }
    }

    void UpdateRetraction()
    {
        Vector3 rightDisp = rightController.position - rightStartPos;
        Vector3 leftDisp = leftController.position - leftStartPos;

        float rightBackward = -Vector3.Dot(rightDisp, headForward);
        float leftBackward = -Vector3.Dot(leftDisp, headForward);

        CurrentRetraction = (rightBackward + leftBackward) / 2f;
        CurrentRetraction = Mathf.Max(0f, CurrentRetraction);
    }

    void UpdateStability()
    {
        rightPositionHistory.Enqueue(rightController.position);
        leftPositionHistory.Enqueue(leftController.position);

        while (rightPositionHistory.Count > StabilitySampleCount)
            rightPositionHistory.Dequeue();
        while (leftPositionHistory.Count > StabilitySampleCount)
            leftPositionHistory.Dequeue();

        if (rightPositionHistory.Count < 5)
        {
            CurrentStability = 1f;
            return;
        }

        float rightVariance = CalculatePositionVariance(rightPositionHistory);
        float leftVariance = CalculatePositionVariance(leftPositionHistory);
        float avgVariance = (rightVariance + leftVariance) / 2f;

        CurrentStability = 1f - Mathf.Clamp01(avgVariance / config.maxPositionVariance);
    }

    float CalculatePositionVariance(Queue<Vector3> positions)
    {
        Vector3 mean = Vector3.zero;
        foreach (Vector3 p in positions)
            mean += p;
        mean /= positions.Count;

        float variance = 0f;
        foreach (Vector3 p in positions)
            variance += Vector3.SqrMagnitude(p - mean);
        variance /= positions.Count;

        return variance;
    }

    void UpdateCurrentIntensity()
    {
        float rightVariance = CalculatePositionVariance(rightPositionHistory);
        float leftVariance = CalculatePositionVariance(leftPositionHistory);
        float avgVariance = (rightVariance + leftVariance) / 2f;

        if (avgVariance > config.currentTriggerVariance)
        {
            CurrentCurrentIntensity = Mathf.Clamp01((avgVariance - config.currentTriggerVariance) /
                (config.maxPositionVariance - config.currentTriggerVariance));
        }
        else
        {
            CurrentCurrentIntensity = Mathf.Max(0f, CurrentCurrentIntensity - Time.deltaTime * 2f);
        }
    }

    void UpdateMotionMetrics(float deltaTime)
    {
        peakRetraction = Mathf.Max(peakRetraction, CurrentRetraction);

        Vector3 rightDisp = rightController.position - rightStartPos;
        Vector3 leftDisp = leftController.position - leftStartPos;
        Vector3 avgDisp = (rightDisp + leftDisp) / 2f;

        maxHorizontalDisplacement = Mathf.Max(maxHorizontalDisplacement, Mathf.Abs(Vector3.Dot(avgDisp, headRight)));
        maxVerticalDisplacement = Mathf.Max(maxVerticalDisplacement, Mathf.Abs(Vector3.Dot(avgDisp, Vector3.up)));
        maxForwardDisplacement = Mathf.Max(maxForwardDisplacement, Mathf.Abs(Vector3.Dot(avgDisp, headForward)));

        if (deltaTime > 0.0001f)
        {
            float rightSpeed = Vector3.Distance(rightController.position, prevRightPos) / deltaTime;
            float leftSpeed = Vector3.Distance(leftController.position, prevLeftPos) / deltaTime;
            maxLinearSpeed = Mathf.Max(maxLinearSpeed, (rightSpeed + leftSpeed) / 2f);
        }

        prevRightPos = rightController.position;
        prevLeftPos = leftController.position;
    }

    void TickWaitingStart()
    {
        if (CurrentRetraction < config.startThresholdDistance) return;

        ResetActionMetrics();
        actionStartTime = Time.time;
        CurrentState = Game3_ActionState.Retracting;
    }

    void TickRetracting()
    {
        if (CurrentRetraction > config.maxSafeRetraction)
        {
            InvalidateCurrentAction(Game3_Text.HintRetractionTooDeep);
            return;
        }

        if (!IsPostureValid(out string reason))
        {
            InvalidateCurrentAction(reason);
            return;
        }

        if (Time.time - actionStartTime > config.maxActionTime)
        {
            InvalidateCurrentAction(Game3_Text.HintActionTooLong);
            return;
        }

        if (CurrentRetraction >= config.targetRetraction)
        {
            holdStartTime = Time.time;
            CurrentState = Game3_ActionState.Holding;
        }
    }

    void TickHolding()
    {
        if (CurrentRetraction < config.targetRetraction - config.holdDropTolerance)
        {
            CurrentState = Game3_ActionState.Retracting;
            return;
        }

        if (!IsPostureValid(out string reason))
        {
            InvalidateCurrentAction(reason);
            return;
        }

        if (CurrentCurrentIntensity > config.maxCurrentIntensity)
        {
            InvalidateCurrentAction(Game3_Text.HintCurrentWarning);
            return;
        }

        float holdDuration = Time.time - holdStartTime;
        float effectiveHold = holdDuration * (1f - CurrentCurrentIntensity * 0.6f);

        if (effectiveHold >= config.holdTime)
        {
            CompleteCurrentAction(holdDuration);
        }
    }

    void TickReturning()
    {
        if (CurrentRetraction <= config.returnThresholdDistance)
        {
            CurrentState = Game3_ActionState.WaitingStart;
            ResetActionMetrics();
            OnProgressChanged?.Invoke(0f);
        }
    }

    void CompleteCurrentAction(float holdDuration)
    {
        float actionDuration = Time.time - actionStartTime;
        bool postureStable = IsPostureValid(out _);
        float symmetry = CalculateSymmetry();

        bool valid = peakRetraction >= config.minValidRetraction &&
                     actionDuration >= config.minActionTime &&
                     actionDuration <= config.maxActionTime &&
                     holdDuration >= config.improveHoldTime &&
                     postureStable &&
                     CurrentCurrentIntensity < config.maxCurrentIntensity;

        Game3_ActionResult result = new Game3_ActionResult
        {
            isValid = valid,
            feedback = valid ? Game3_Text.HintReturnStart : Game3_Text.HintAdjustPosture,
            retractionDepth = peakRetraction,
            holdDuration = holdDuration,
            actionDuration = actionDuration,
            stabilityScore = CurrentStability,
            symmetryScore = symmetry,
            postureStable = postureStable,
            currentIntensity = CurrentCurrentIntensity,
            maxHorizontalDisplacement = maxHorizontalDisplacement,
            maxVerticalDisplacement = maxVerticalDisplacement,
            maxForwardDisplacement = maxForwardDisplacement,
            maxLinearSpeed = maxLinearSpeed
        };

        CurrentState = Game3_ActionState.Returning;
        OnActionCompleted?.Invoke(result);
    }

    float CalculateSymmetry()
    {
        Vector3 rightDisp = rightController.position - rightStartPos;
        Vector3 leftDisp = leftController.position - leftStartPos;

        float rightBack = -Vector3.Dot(rightDisp, headForward);
        float leftBack = -Vector3.Dot(leftDisp, headForward);

        float diff = Mathf.Abs(rightBack - leftBack);
        return 1f - Mathf.Clamp01(diff / config.symmetryTolerance);
    }

    bool IsPostureValid(out string reason)
    {
        if (maxHorizontalDisplacement > config.maxHorizontalDisplacement)
        {
            reason = Game3_Text.HintKeepElbowsClose;
            return false;
        }

        if (maxVerticalDisplacement > config.maxVerticalDisplacement)
        {
            reason = Game3_Text.HintDoNotShrug;
            return false;
        }

        if (maxForwardDisplacement > config.maxForwardDisplacement)
        {
            reason = Game3_Text.HintSlowDown;
            return false;
        }

        if (maxLinearSpeed > config.maxLinearSpeed)
        {
            reason = Game3_Text.HintSlowDown;
            return false;
        }

        if (head != null)
        {
            float yawChange = Mathf.Abs(Mathf.DeltaAngle(headStartRot.eulerAngles.y, head.rotation.eulerAngles.y));
            if (yawChange > config.maxHeadYawChange)
            {
                reason = Game3_Text.HintFaceTarget;
                return false;
            }

            float elevation = head.position.y - headStartPos.y;
            if (elevation > config.maxShoulderElevation)
            {
                reason = Game3_Text.HintDoNotShrug;
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    void InvalidateCurrentAction(string reason)
    {
        CurrentState = Game3_ActionState.Returning;
        OnActionCompleted?.Invoke(Game3_ActionResult.Invalid(reason, peakRetraction, CurrentCurrentIntensity));
    }

    void ResetActionMetrics()
    {
        peakRetraction = CurrentRetraction;
        maxLinearSpeed = 0f;
        maxHorizontalDisplacement = 0f;
        maxVerticalDisplacement = 0f;
        maxForwardDisplacement = 0f;

        if (rightController != null) prevRightPos = rightController.position;
        if (leftController != null) prevLeftPos = leftController.position;

        rightPositionHistory.Clear();
        leftPositionHistory.Clear();
    }
}
