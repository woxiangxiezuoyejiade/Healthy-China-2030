using System;
using UnityEngine;

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

    public void Configure(Game2_DifficultyConfig difficultyConfig)
    {
        config = difficultyConfig;
        ResetDetector();
    }

    public bool Calibrate()
    {
        if (!UseKeyboardSimulator() && (leftController == null || rightController == null))
        {
            OnActionInvalid?.Invoke("Controller reference missing.");
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
            InvalidateCurrentAction("Open arms too far. Return to a comfortable range.");
            return;
        }

        if (!IsPostureStable(out string reason))
        {
            InvalidateCurrentAction(reason);
            return;
        }

        if (Time.time - actionStartTime > config.maxActionTime)
        {
            InvalidateCurrentAction("Action took too long. Try again smoothly.");
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
            feedback = valid ? "Good. Return slowly." : "Adjust posture and try again.",
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
            reason = "Keep both hands at a similar height.";
            return false;
        }

        if (maxAverageHeightChange > config.maxAverageHeightChange)
        {
            reason = "Keep elbows steady. Avoid lifting or dropping the arms.";
            return false;
        }

        if (maxForwardBackChange > config.maxForwardBackChange)
        {
            reason = "Open to the sides. Avoid pushing both hands forward or backward.";
            return false;
        }

        if (head != null)
        {
            float yawChange = Mathf.Abs(Mathf.DeltaAngle(startHeadYaw, head.eulerAngles.y));
            if (yawChange > config.maxHeadYawChange)
            {
                reason = "Face forward and keep the neck relaxed.";
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
        bool opening = Input.GetKey(simulatorOpenKey);
        float target = 0f;

        if (opening)
        {
            target = Input.GetKey(simulatorExcellentKey)
                ? simulatorExcellentExpansion
                : simulatorNormalExpansion;
        }

        float speed = opening ? simulatorOpenSpeed : simulatorReturnSpeed;
        simulatedExpansion = Mathf.MoveTowards(simulatedExpansion, target, speed * deltaTime);
        return simulatedExpansion;
    }
}
