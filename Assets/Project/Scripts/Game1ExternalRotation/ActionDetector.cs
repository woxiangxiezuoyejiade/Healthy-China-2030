using System;
using UnityEngine;

public class Game1_ActionDetector : MonoBehaviour
{
    [Header("Tracked Objects")]
    public Transform rightController;
    public Transform leftController;
    public Transform head;

    [Header("External Rotation Axis")]
    public Vector3 localRotationAxis = Vector3.up;
    public bool invertRightHandAngle = false;
    public bool invertLeftHandAngle = true;

    [Header("Keyboard Simulator")]
    public bool useKeyboardSimulatorInEditor = true;
    public bool forceKeyboardSimulator = false;
    public KeyCode simulatorOpenKey = KeyCode.Space;
    public KeyCode simulatorExcellentKey = KeyCode.LeftShift;
    public float simulatorOpenSpeed = 45f;
    public float simulatorReturnSpeed = 65f;
    public float simulatorNormalAngle = 48f;
    public float simulatorExcellentAngle = 58f;

    public event Action<float> OnProgressChanged;
    public event Action<Game1_ActionResult> OnActionCompleted;
    public event Action<string> OnActionInvalid;

    public Game1_ActionState CurrentState { get; private set; } = Game1_ActionState.WaitingStart;
    public float CurrentAngle { get; private set; }
    public Game1_TrainingHand TrainingHand => trainingHand;
    public bool IsUsingKeyboardSimulator => UseKeyboardSimulator();

    Game1_DifficultyConfig config;
    Game1_TrainingHand trainingHand;
    Transform activeController;
    Quaternion startRotation;
    Vector3 startPosition;
    float startHeadYaw;
    float actionStartTime;
    float holdStartTime;
    float peakAngle;
    float maxLinearSpeed;
    float maxAngularSpeed;
    float maxHorizontalDisplacement;
    float maxVerticalDisplacement;
    float maxForwardDisplacement;
    Vector3 previousPosition;
    Quaternion previousRotation;
    float simulatedAngle;
    bool isCalibrated;

    public void Configure(Game1_DifficultyConfig difficultyConfig, Game1_TrainingHand hand)
    {
        config = difficultyConfig;
        trainingHand = hand;
        activeController = trainingHand == Game1_TrainingHand.Right ? rightController : leftController;
        ResetDetector();
    }

    public bool Calibrate()
    {
        activeController = trainingHand == Game1_TrainingHand.Right ? rightController : leftController;
        if (!UseKeyboardSimulator() && activeController == null)
        {
            OnActionInvalid?.Invoke(Game1_Text.HintControllerMissing);
            return false;
        }

        if (UseKeyboardSimulator())
        {
            simulatedAngle = 0f;
            startRotation = Quaternion.identity;
            startPosition = Vector3.zero;
            previousPosition = Vector3.zero;
            previousRotation = Quaternion.identity;
        }
        else
        {
            startRotation = activeController.rotation;
            startPosition = activeController.position;
            previousPosition = startPosition;
            previousRotation = startRotation;
        }

        startHeadYaw = head != null ? head.eulerAngles.y : 0f;
        isCalibrated = true;
        CurrentState = Game1_ActionState.WaitingStart;
        ResetActionMetrics();
        OnProgressChanged?.Invoke(0f);
        return true;
    }

    public void ResetDetector()
    {
        CurrentState = Game1_ActionState.WaitingStart;
        CurrentAngle = 0f;
        isCalibrated = false;
        ResetActionMetrics();
    }

    public void Tick(float deltaTime)
    {
        if (!isCalibrated || config == null) return;
        if (!UseKeyboardSimulator() && activeController == null) return;

        CurrentAngle = UseKeyboardSimulator()
            ? UpdateSimulatedAngle(deltaTime)
            : CalculateExternalRotationAngle();

        UpdateMotionMetrics(deltaTime);
        OnProgressChanged?.Invoke(Mathf.InverseLerp(0f, config.targetAngle, CurrentAngle));

        switch (CurrentState)
        {
            case Game1_ActionState.WaitingStart:
                TickWaitingStart();
                break;
            case Game1_ActionState.RotatingOut:
                TickRotatingOut();
                break;
            case Game1_ActionState.Holding:
                TickHolding();
                break;
            case Game1_ActionState.Returning:
                TickReturning();
                break;
        }
    }

    float CalculateExternalRotationAngle()
    {
        Quaternion delta = Quaternion.Inverse(startRotation) * activeController.rotation;
        Vector3 axis = localRotationAxis.sqrMagnitude < 0.001f ? Vector3.up : localRotationAxis.normalized;

        Vector3 vectorPart = new Vector3(delta.x, delta.y, delta.z);
        Vector3 projected = Vector3.Project(vectorPart, axis);
        Quaternion twist = new Quaternion(projected.x, projected.y, projected.z, delta.w).normalized;
        twist.ToAngleAxis(out float rawAngle, out Vector3 rawAxis);
        if (rawAngle > 180f) rawAngle = 360f - rawAngle;

        float signedAngle = rawAngle * Mathf.Sign(Vector3.Dot(rawAxis, axis));
        bool invert = trainingHand == Game1_TrainingHand.Right ? invertRightHandAngle : invertLeftHandAngle;
        if (invert) signedAngle *= -1f;

        return Mathf.Clamp(Mathf.Abs(signedAngle), 0f, 180f);
    }

    void UpdateMotionMetrics(float deltaTime)
    {
        peakAngle = Mathf.Max(peakAngle, CurrentAngle);

        if (UseKeyboardSimulator())
        {
            maxLinearSpeed = 0f;
            maxAngularSpeed = 0f;
            maxHorizontalDisplacement = 0f;
            maxVerticalDisplacement = 0f;
            maxForwardDisplacement = 0f;
            return;
        }

        Vector3 offset = activeController.position - startPosition;
        maxHorizontalDisplacement = Mathf.Max(maxHorizontalDisplacement, Mathf.Abs(offset.x));
        maxVerticalDisplacement = Mathf.Max(maxVerticalDisplacement, Mathf.Abs(offset.y));
        maxForwardDisplacement = Mathf.Max(maxForwardDisplacement, Mathf.Abs(offset.z));

        if (deltaTime > 0.0001f)
        {
            float linearSpeed = Vector3.Distance(activeController.position, previousPosition) / deltaTime;
            maxLinearSpeed = Mathf.Max(maxLinearSpeed, linearSpeed);

            Quaternion deltaRotation = Quaternion.Inverse(previousRotation) * activeController.rotation;
            deltaRotation.ToAngleAxis(out float angleDelta, out _);
            if (angleDelta > 180f) angleDelta = 360f - angleDelta;
            maxAngularSpeed = Mathf.Max(maxAngularSpeed, angleDelta / deltaTime);
        }

        previousPosition = activeController.position;
        previousRotation = activeController.rotation;
    }

    void TickWaitingStart()
    {
        if (CurrentAngle < config.startThresholdAngle) return;

        ResetActionMetrics();
        actionStartTime = Time.time;
        CurrentState = Game1_ActionState.RotatingOut;
    }

    void TickRotatingOut()
    {
        bool hasReturnedAfterOpening = CurrentAngle <= config.returnThresholdAngle &&
                                       peakAngle >= config.startThresholdAngle &&
                                       CurrentAngle <= peakAngle - 2f;
        if (hasReturnedAfterOpening)
        {
            CompleteCurrentAction(0f);
            return;
        }

        if (CurrentAngle > config.maxSafeAngle)
        {
            InvalidateCurrentAction(Game1_Text.HintAngleTooLarge);
            return;
        }

        if (!IsPostureStable(out string reason))
        {
            InvalidateCurrentAction(reason);
            return;
        }

        if (Time.time - actionStartTime > config.maxActionTime)
        {
            InvalidateCurrentAction(Game1_Text.HintActionTooLong);
            return;
        }

        if (CurrentAngle >= config.targetAngle)
        {
            holdStartTime = Time.time;
            CurrentState = Game1_ActionState.Holding;
        }
    }

    void TickHolding()
    {
        if (CurrentAngle < config.targetAngle - config.holdDropTolerance)
        {
            CurrentState = Game1_ActionState.RotatingOut;
            return;
        }

        if (!IsPostureStable(out string reason))
        {
            InvalidateCurrentAction(reason);
            return;
        }

        float holdDuration = Time.time - holdStartTime;
        if (holdDuration >= config.holdTime)
        {
            CompleteCurrentAction(holdDuration);
        }
    }

    void TickReturning()
    {
        if (CurrentAngle <= config.returnThresholdAngle)
        {
            CurrentState = Game1_ActionState.WaitingStart;
            ResetActionMetrics();
            OnProgressChanged?.Invoke(0f);
        }
    }

    void CompleteCurrentAction(float holdDuration)
    {
        float actionDuration = Time.time - actionStartTime;
        bool postureStable = IsPostureStable(out _);
        bool valid = peakAngle >= config.startThresholdAngle &&
                     actionDuration >= 0.15f &&
                     actionDuration <= config.maxActionTime &&
                     postureStable;

        Game1_ActionResult result = new Game1_ActionResult
        {
            isValid = valid,
            feedback = valid ? Game1_Text.HintReturnStart : Game1_Text.HintAdjustPosture,
            peakAngle = peakAngle,
            actionDuration = actionDuration,
            holdDuration = holdDuration,
            maxLinearSpeed = maxLinearSpeed,
            maxAngularSpeed = maxAngularSpeed,
            maxHorizontalDisplacement = maxHorizontalDisplacement,
            maxVerticalDisplacement = maxVerticalDisplacement,
            maxForwardDisplacement = maxForwardDisplacement,
            postureStable = postureStable
        };

        CurrentState = Game1_ActionState.Returning;
        OnActionCompleted?.Invoke(result);
    }

    bool IsPostureStable(out string reason)
    {
        if (maxHorizontalDisplacement > config.maxHorizontalDisplacement)
        {
            reason = Game1_Text.HintKeepElbowClose;
            return false;
        }

        if (maxVerticalDisplacement > config.maxVerticalDisplacement)
        {
            reason = Game1_Text.HintForearmStable;
            return false;
        }

        if (maxForwardDisplacement > config.maxForwardDisplacement)
        {
            reason = Game1_Text.HintDoNotSwingForward;
            return false;
        }

        if (maxLinearSpeed > config.maxLinearSpeed || maxAngularSpeed > config.maxAngularSpeed)
        {
            reason = Game1_Text.HintSlowDown;
            return false;
        }

        if (head != null)
        {
            float yawChange = Mathf.Abs(Mathf.DeltaAngle(startHeadYaw, head.eulerAngles.y));
            if (yawChange > config.maxHeadYawChange)
            {
                reason = Game1_Text.HintFaceCore;
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    void InvalidateCurrentAction(string reason)
    {
        CurrentState = Game1_ActionState.Returning;
        OnActionCompleted?.Invoke(Game1_ActionResult.Invalid(reason, peakAngle));
    }

    void ResetActionMetrics()
    {
        peakAngle = CurrentAngle;
        maxLinearSpeed = 0f;
        maxAngularSpeed = 0f;
        maxHorizontalDisplacement = 0f;
        maxVerticalDisplacement = 0f;
        maxForwardDisplacement = 0f;

        if (activeController != null)
        {
            previousPosition = activeController.position;
            previousRotation = activeController.rotation;
        }
    }

    bool UseKeyboardSimulator()
    {
        return forceKeyboardSimulator || (Application.isEditor && useKeyboardSimulatorInEditor);
    }

    float UpdateSimulatedAngle(float deltaTime)
    {
        bool opening = Input.GetKey(simulatorOpenKey);
        float target = 0f;

        if (opening)
        {
            target = Input.GetKey(simulatorExcellentKey)
                ? simulatorExcellentAngle
                : simulatorNormalAngle;
        }

        float speed = opening ? simulatorOpenSpeed : simulatorReturnSpeed;
        simulatedAngle = Mathf.MoveTowards(simulatedAngle, target, speed * deltaTime);
        return simulatedAngle;
    }
}
