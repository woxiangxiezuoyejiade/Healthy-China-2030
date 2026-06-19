using System;
using UnityEngine;
using UnityEngine.XR;

public class Game2_ActionDetector : MonoBehaviour
{
    [Header("Tracked Objects")]
    public Transform leftController;
    public Transform rightController;
    public Transform head;

    [Header("Runtime")]
    public Game2_DifficultyConfig config = new Game2_DifficultyConfig();

    [Header("Keyboard Simulator")]
    public bool useKeyboardSimulatorInEditor = true;
    public bool forceKeyboardSimulator = false;
    public KeyCode simulatorOpenKey = KeyCode.Space;
    public KeyCode simulatorExcellentKey = KeyCode.LeftShift;
    public float simulatorOpenSpeed = 0.65f;
    public float simulatorReturnSpeed = 0.9f;
    public float simulatorNormalExpansion = 0.58f;
    public float simulatorExcellentExpansion = 0.70f;

    [Header("VR Confirm Gate")]
    public bool requireConfirmButtonOnDevice = true;

    public event Action<float> OnProgressChanged;
    public event Action<Game2_ActionResult> OnActionCompleted;
    public event Action<string> OnActionInvalid;

    public Game2_ActionState CurrentState { get; private set; } = Game2_ActionState.WaitingStart;
    public float CurrentExpansion { get; private set; }
    public bool IsUsingKeyboardSimulator => UseKeyboardSimulator();

    float startDistance;
    float startAverageHeight;
    float startAverageForward;
    float startHeadYaw;
    float actionStartTime;
    float holdStartTime;
    float peakExpansion;
    float maxHandHeightDifference;
    float maxAverageHeightChange;
    float maxForwardBackChange;
    float simulatedExpansion;
    bool isCalibrated;
    bool confirmWasHeld;
    InputDevice leftDevice;
    InputDevice rightDevice;

    public void Configure(Game2_DifficultyConfig difficultyConfig)
    {
        config = difficultyConfig;
        confirmWasHeld = false;
        ResetDetector();
    }

    public bool Calibrate()
    {
        if (!UseKeyboardSimulator() && (leftController == null || rightController == null))
        {
            OnActionInvalid?.Invoke("\u6ca1\u6709\u627e\u5230\u624b\u67c4\u5f15\u7528");
            return false;
        }

        if (UseKeyboardSimulator())
        {
            simulatedExpansion = 0f;
            startDistance = 0f;
            startAverageHeight = 0f;
            startAverageForward = 0f;
        }
        else
        {
            startDistance = Vector3.Distance(leftController.position, rightController.position);
            Vector3 average = GetAverageHandPosition();
            startAverageHeight = average.y;
            startAverageForward = average.z;
        }

        startHeadYaw = head != null ? head.eulerAngles.y : 0f;

        isCalibrated = true;
        confirmWasHeld = false;
        CurrentState = Game2_ActionState.WaitingStart;
        CurrentExpansion = 0f;
        ResetActionMetrics();
        OnProgressChanged?.Invoke(0f);
        return true;
    }

    public void ResetDetector()
    {
        isCalibrated = false;
        CurrentState = Game2_ActionState.WaitingStart;
        CurrentExpansion = 0f;
        ResetActionMetrics();
    }

    public void Tick(float deltaTime)
    {
        if (!isCalibrated || config == null) return;
        if (!UseKeyboardSimulator() && (leftController == null || rightController == null)) return;

        if (!UseKeyboardSimulator() && requireConfirmButtonOnDevice)
        {
            bool confirmHeld = IsAnyConfirmHeld();
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

        CurrentExpansion = UseKeyboardSimulator()
            ? UpdateSimulatedExpansion(deltaTime)
            : Mathf.Max(0f, Vector3.Distance(leftController.position, rightController.position) - startDistance);

        UpdateMotionMetrics();
        OnProgressChanged?.Invoke(Mathf.InverseLerp(0f, config.targetExpansion, CurrentExpansion));

        switch (CurrentState)
        {
            case Game2_ActionState.WaitingStart:
                TickWaitingStart();
                break;
            case Game2_ActionState.Opening:
                TickOpening();
                break;
            case Game2_ActionState.Holding:
                TickHolding();
                break;
            case Game2_ActionState.Returning:
                TickReturning();
                break;
        }
    }

    void TickWaitingStart()
    {
        if (CurrentExpansion < config.startThreshold) return;

        ResetActionMetrics();
        actionStartTime = Time.time;
        CurrentState = Game2_ActionState.Opening;
    }

    void TickOpening()
    {
        bool hasReturnedAfterOpening = CurrentExpansion <= config.returnThreshold &&
                                       peakExpansion >= config.startThreshold &&
                                       CurrentExpansion <= peakExpansion - 0.03f;
        if (hasReturnedAfterOpening)
        {
            CompleteCurrentAction(0f);
            return;
        }

        if (CurrentExpansion > config.maxSafeExpansion)
        {
            InvalidateCurrentAction("\u53cc\u624b\u6253\u5f00\u8fc7\u5927\uff0c\u8bf7\u56de\u5230\u8212\u9002\u8303\u56f4");
            return;
        }

        if (!IsPostureStable(out string reason))
        {
            InvalidateCurrentAction(reason);
            return;
        }

        if (Time.time - actionStartTime > config.maxActionTime)
        {
            InvalidateCurrentAction("\u52a8\u4f5c\u65f6\u95f4\u8fc7\u957f\uff0c\u8bf7\u653e\u6162\u8282\u594f\u540e\u518d\u8bd5\u4e00\u6b21");
            return;
        }

        if (CurrentExpansion >= config.targetExpansion)
        {
            holdStartTime = Time.time;
            CurrentState = Game2_ActionState.Holding;
        }
    }

    void TickHolding()
    {
        if (CurrentExpansion < config.targetExpansion - config.returnThreshold)
        {
            CurrentState = Game2_ActionState.Opening;
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
        if (CurrentExpansion <= config.returnThreshold)
        {
            CurrentState = Game2_ActionState.WaitingStart;
            ResetActionMetrics();
            OnProgressChanged?.Invoke(0f);
        }
    }

    void CompleteCurrentAction(float holdDuration)
    {
        float actionDuration = Time.time - actionStartTime;
        bool postureStable = IsPostureStable(out _);
        bool valid =
            peakExpansion >= config.startThreshold &&
            actionDuration >= 0.15f &&
            actionDuration <= config.maxActionTime &&
            postureStable;

        Game2_ActionResult result = new Game2_ActionResult
        {
            isValid = valid,
            feedback = valid ? "\u5f88\u597d\uff0c\u8bf7\u6162\u6162\u56de\u5230\u8d77\u70b9" : "\u8bf7\u8c03\u6574\u59ff\u52bf\u540e\u518d\u8bd5\u4e00\u6b21",
            peakExpansion = peakExpansion,
            actionDuration = actionDuration,
            holdDuration = holdDuration,
            maxHandHeightDifference = maxHandHeightDifference,
            maxAverageHeightChange = maxAverageHeightChange,
            maxForwardBackChange = maxForwardBackChange,
            postureStable = postureStable
        };

        CurrentState = Game2_ActionState.Returning;
        OnActionCompleted?.Invoke(result);
    }

    void UpdateMotionMetrics()
    {
        peakExpansion = Mathf.Max(peakExpansion, CurrentExpansion);

        if (UseKeyboardSimulator())
        {
            maxHandHeightDifference = 0f;
            maxAverageHeightChange = 0f;
            maxForwardBackChange = 0f;
            return;
        }

        Vector3 average = GetAverageHandPosition();
        maxHandHeightDifference = Mathf.Max(maxHandHeightDifference, Mathf.Abs(leftController.position.y - rightController.position.y));
        maxAverageHeightChange = Mathf.Max(maxAverageHeightChange, Mathf.Abs(average.y - startAverageHeight));
        maxForwardBackChange = Mathf.Max(maxForwardBackChange, Mathf.Abs(average.z - startAverageForward));
    }

    bool IsPostureStable(out string reason)
    {
        if (maxHandHeightDifference > config.maxHandHeightDifference)
        {
            reason = "\u8bf7\u4fdd\u6301\u53cc\u624b\u9ad8\u5ea6\u63a5\u8fd1";
            return false;
        }

        if (maxAverageHeightChange > config.maxAverageHeightChange)
        {
            reason = "\u8bf7\u4fdd\u6301\u80f3\u90e8\u7a33\u5b9a\uff0c\u907f\u514d\u624b\u62ac\u5f97\u8fc7\u9ad8\u6216\u653e\u5f97\u8fc7\u4f4e";
            return false;
        }

        if (maxForwardBackChange > config.maxForwardBackChange)
        {
            reason = "\u8bf7\u5411\u8eab\u4f53\u4e24\u4fa7\u6253\u5f00\uff0c\u4e0d\u8981\u628a\u53cc\u624b\u5411\u524d\u6216\u5411\u540e\u63a8";
            return false;
        }

        if (head != null)
        {
            float yawChange = Mathf.Abs(Mathf.DeltaAngle(startHeadYaw, head.eulerAngles.y));
            if (yawChange > config.maxHeadYawChange)
            {
                reason = "\u8bf7\u9762\u5411\u524d\u65b9\uff0c\u4fdd\u6301\u9888\u90e8\u653e\u677e";
                return false;
            }
        }

        reason = string.Empty;
        return true;
    }

    void InvalidateCurrentAction(string reason)
    {
        CurrentState = Game2_ActionState.Returning;
        OnActionInvalid?.Invoke(reason);
        OnActionCompleted?.Invoke(Game2_ActionResult.Invalid(reason, peakExpansion));
    }

    void ResetActionMetrics()
    {
        peakExpansion = CurrentExpansion;
        maxHandHeightDifference = 0f;
        maxAverageHeightChange = 0f;
        maxForwardBackChange = 0f;
    }

    void ResetToWaitingWithoutScore()
    {
        CurrentState = Game2_ActionState.WaitingStart;
        CurrentExpansion = 0f;
        if (leftController != null && rightController != null)
        {
            startDistance = Vector3.Distance(leftController.position, rightController.position);
            Vector3 average = GetAverageHandPosition();
            startAverageHeight = average.y;
            startAverageForward = average.z;
        }
        ResetActionMetrics();
    }

    void CaptureCurrentPoseAsStart()
    {
        if (leftController == null || rightController == null) return;

        startDistance = Vector3.Distance(leftController.position, rightController.position);
        Vector3 average = GetAverageHandPosition();
        startAverageHeight = average.y;
        startAverageForward = average.z;
        startHeadYaw = head != null ? head.eulerAngles.y : 0f;
        CurrentState = Game2_ActionState.WaitingStart;
        CurrentExpansion = 0f;
        ResetActionMetrics();
    }

    bool IsAnyConfirmHeld()
    {
        if (!leftDevice.isValid) leftDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (!rightDevice.isValid) rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        return IsPressed(leftDevice) || IsPressed(rightDevice);
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

    Vector3 GetAverageHandPosition()
    {
        return (leftController.position + rightController.position) * 0.5f;
    }

    bool UseKeyboardSimulator()
    {
        return forceKeyboardSimulator || (Application.isEditor && useKeyboardSimulatorInEditor);
    }

    float UpdateSimulatedExpansion(float deltaTime)
    {
        bool opening = KeyboardInputCompat.GetKey(simulatorOpenKey);
        float target = 0f;

        if (opening)
        {
            target = KeyboardInputCompat.GetKey(simulatorExcellentKey)
                ? simulatorExcellentExpansion
                : simulatorNormalExpansion;
        }

        float speed = opening ? simulatorOpenSpeed : simulatorReturnSpeed;
        simulatedExpansion = Mathf.MoveTowards(simulatedExpansion, target, speed * deltaTime);
        return simulatedExpansion;
    }
}
