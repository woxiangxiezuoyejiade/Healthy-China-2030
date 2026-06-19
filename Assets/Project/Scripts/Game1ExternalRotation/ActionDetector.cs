using System;
using UnityEngine;
using UnityEngine.XR;

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
    public bool useBeckoningCatMotion = true;
    public float beckoningVerticalRange = 0.34f;
    public float beckoningForwardAssist = 0.20f;
    public float beckoningRotationAssist = 0.25f;

    [Header("Keyboard Simulator")]
    public bool useKeyboardSimulatorInEditor = true;
    public bool forceKeyboardSimulator = false;
    public KeyCode simulatorOpenKey = KeyCode.Space;
    public KeyCode simulatorExcellentKey = KeyCode.LeftShift;
    public float simulatorOpenSpeed = 45f;
    public float simulatorReturnSpeed = 65f;
    public float simulatorNormalAngle = 48f;
    public float simulatorExcellentAngle = 58f;

    [Header("VR Confirm Gate")]
    public bool requireConfirmButtonOnDevice = true;

    [Header("Validation Leniency")]
    public bool useLenientVrValidation = true;
    public float postureInvalidMultiplier = 2.2f;
    public float speedInvalidMultiplier = 3.0f;
    public float headYawInvalidMultiplier = 2.0f;
    public float maxActionTimeGrace = 2.0f;
    public float maxSafeAngleGrace = 12f;

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
    bool confirmWasHeld;
    InputDevice leftDevice;
    InputDevice rightDevice;

    public void Configure(Game1_DifficultyConfig difficultyConfig, Game1_TrainingHand hand)
    {
        config = difficultyConfig;
        trainingHand = hand;
        activeController = trainingHand == Game1_TrainingHand.Right ? rightController : leftController;
        confirmWasHeld = false;
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
        confirmWasHeld = false;
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

        if (!UseKeyboardSimulator() && requireConfirmButtonOnDevice)
        {
            bool confirmHeld = IsConfirmHeldForActiveHand();
            if (!confirmHeld)
            {
                confirmWasHeld = false;
                ResetToWaitingWithoutScore();
                OnProgressChanged?.Invoke(0f);
                return;
            }

            if (!confirmWasHeld)
            {
                CaptureCurrentPoseAsStart();
                confirmWasHeld = true;
            }
        }

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
        return useBeckoningCatMotion
            ? CalculateBeckoningCatAngle()
            : CalculateTwistAngle(localRotationAxis);
    }

    float CalculateTwistAngle(Vector3 axis)
    {
        Quaternion delta = Quaternion.Inverse(startRotation) * activeController.rotation;
        axis = axis.sqrMagnitude < 0.001f ? Vector3.up : axis.normalized;

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

    float CalculateBeckoningCatAngle()
    {
        if (activeController == null || config == null) return 0f;

        Vector3 offset = activeController.position - startPosition;
        Vector3 forward = GetHeadForward();
        float upward = Vector3.Dot(offset, Vector3.up);
        float backward = Mathf.Max(0f, -Vector3.Dot(offset, forward));

        float effectiveLift = upward + backward * beckoningForwardAssist;
        float positionProgress = Mathf.InverseLerp(0f, Mathf.Max(0.05f, beckoningVerticalRange), effectiveLift);
        float positionAngle = positionProgress * config.targetAngle;

        // A little rotation assist keeps the motion responsive when the real hand traces a small arc.
        float rotationAngle = CalculateTwistAngle(Vector3.right) * Mathf.Clamp01(beckoningRotationAssist);
        return Mathf.Clamp(Mathf.Max(positionAngle, rotationAngle), 0f, 180f);
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
        Vector3 forward = GetHeadForward();
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        maxHorizontalDisplacement = Mathf.Max(maxHorizontalDisplacement, Mathf.Abs(Vector3.Dot(offset, right)));
        maxVerticalDisplacement = Mathf.Max(maxVerticalDisplacement, Mathf.Abs(Vector3.Dot(offset, Vector3.up)));
        maxForwardDisplacement = Mathf.Max(maxForwardDisplacement, Mathf.Abs(Vector3.Dot(offset, forward)));

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

        if (CurrentAngle > GetMaxSafeAngle())
        {
            InvalidateCurrentAction(Game1_Text.HintAngleTooLarge);
            return;
        }

        if (!IsPostureStable(out string reason))
        {
            InvalidateCurrentAction(reason);
            return;
        }

        if (Time.time - actionStartTime > GetMaxActionTime())
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
                     actionDuration <= GetMaxActionTime() &&
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
        float postureMultiplier = GetPostureInvalidMultiplier();
        float speedMultiplier = GetSpeedInvalidMultiplier();
        float headYawMultiplier = GetHeadYawInvalidMultiplier();

        if (maxHorizontalDisplacement > config.maxHorizontalDisplacement * postureMultiplier)
        {
            reason = Game1_Text.HintKeepElbowClose;
            return false;
        }

        float allowedVertical = config.maxVerticalDisplacement * postureMultiplier;
        if (useBeckoningCatMotion)
        {
            allowedVertical = Mathf.Max(allowedVertical, beckoningVerticalRange + 0.16f);
        }

        if (maxVerticalDisplacement > allowedVertical)
        {
            reason = Game1_Text.HintForearmStable;
            return false;
        }

        if (maxForwardDisplacement > config.maxForwardDisplacement * postureMultiplier)
        {
            reason = Game1_Text.HintDoNotSwingForward;
            return false;
        }

        if (maxLinearSpeed > config.maxLinearSpeed * speedMultiplier ||
            maxAngularSpeed > config.maxAngularSpeed * speedMultiplier)
        {
            reason = Game1_Text.HintSlowDown;
            return false;
        }

        if (head != null)
        {
            float yawChange = Mathf.Abs(Mathf.DeltaAngle(startHeadYaw, head.eulerAngles.y));
            if (yawChange > config.maxHeadYawChange * headYawMultiplier)
            {
                reason = Game1_Text.HintFaceCore;
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    float GetMaxActionTime()
    {
        return config.maxActionTime + (useLenientVrValidation ? maxActionTimeGrace : 0f);
    }

    float GetMaxSafeAngle()
    {
        return config.maxSafeAngle + (useLenientVrValidation ? maxSafeAngleGrace : 0f);
    }

    float GetPostureInvalidMultiplier()
    {
        return useLenientVrValidation ? Mathf.Max(1f, postureInvalidMultiplier) : 1f;
    }

    float GetSpeedInvalidMultiplier()
    {
        return useLenientVrValidation ? Mathf.Max(1f, speedInvalidMultiplier) : 1f;
    }

    float GetHeadYawInvalidMultiplier()
    {
        return useLenientVrValidation ? Mathf.Max(1f, headYawInvalidMultiplier) : 1f;
    }

    Vector3 GetHeadForward()
    {
        Transform reference = head != null ? head : transform;
        Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        return forward;
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

    void ResetToWaitingWithoutScore()
    {
        CurrentState = Game1_ActionState.WaitingStart;
        CurrentAngle = 0f;
        if (activeController != null)
        {
            startRotation = activeController.rotation;
            startPosition = activeController.position;
            previousPosition = startPosition;
            previousRotation = startRotation;
        }
        ResetActionMetrics();
    }

    void CaptureCurrentPoseAsStart()
    {
        if (activeController == null) return;

        startRotation = activeController.rotation;
        startPosition = activeController.position;
        previousPosition = startPosition;
        previousRotation = startRotation;
        startHeadYaw = head != null ? head.eulerAngles.y : 0f;
        CurrentState = Game1_ActionState.WaitingStart;
        CurrentAngle = 0f;
        ResetActionMetrics();
    }

    bool IsConfirmHeldForActiveHand()
    {
        XRNode node = trainingHand == Game1_TrainingHand.Right ? XRNode.RightHand : XRNode.LeftHand;
        InputDevice device = GetDevice(node);
        return IsPressed(device);
    }

    InputDevice GetDevice(XRNode node)
    {
        if (node == XRNode.LeftHand)
        {
            if (!leftDevice.isValid) leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            return leftDevice;
        }

        if (!rightDevice.isValid) rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        return rightDevice;
    }

    bool IsPressed(InputDevice device)
    {
        if (!device.isValid) return false;

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger) && trigger) return true;
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out bool primary) && primary) return true;
        if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondary) && secondary) return true;
        if (device.TryGetFeatureValue(CommonUsages.menuButton, out bool menu) && menu) return true;
        if (device.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool stickClick) && stickClick) return true;
        if (device.TryGetFeatureValue(CommonUsages.secondary2DAxisClick, out bool stickClick2) && stickClick2) return true;
        if (device.TryGetFeatureValue(CommonUsages.gripButton, out bool grip) && grip) return true;
        return false;
    }

    bool UseKeyboardSimulator()
    {
        return forceKeyboardSimulator || (Application.isEditor && useKeyboardSimulatorInEditor);
    }

    float UpdateSimulatedAngle(float deltaTime)
    {
        bool opening = KeyboardInputCompat.GetKey(simulatorOpenKey);
        float target = 0f;

        if (opening)
        {
            target = KeyboardInputCompat.GetKey(simulatorExcellentKey)
                ? simulatorExcellentAngle
                : simulatorNormalAngle;
        }

        float speed = opening ? simulatorOpenSpeed : simulatorReturnSpeed;
        simulatedAngle = Mathf.MoveTowards(simulatedAngle, target, speed * deltaTime);
        return simulatedAngle;
    }
}
