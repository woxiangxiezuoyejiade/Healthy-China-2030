using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Game2_HandGuideVisual : MonoBehaviour
{
    [Header("Tracked Objects")]
    public Transform leftController;
    public Transform rightController;
    public Transform head;
    public Game2_ActionDetector actionDetector;

    [Header("Guide Balls")]
    public bool createGuideBalls = true;
    public Transform leftGuideBall;
    public Transform rightGuideBall;
    public float ballRadius = 0.06f;
    public Color leftBallColor = new Color(0.18f, 0.55f, 1f);
    public Color rightBallColor = new Color(1f, 0.48f, 0.18f);
    public bool exaggeratePerspectiveScale = false;
    public float nearBallScaleBoost = 0.06f;
    [Range(0.5f, 1f)] public float middleBallScale = 0.78f;
    public bool compensateCameraPerspective = true;

    [Header("Arc Track Arrows")]
    public bool showMovementArrows = true;
    public bool showArrowHeads = false;
    public int arrowSegments = 32;
    public float guideTrackWidth = 0.10f;
    public float movementTrackWidth = 0.055f;
    public float shoulderHalfWidth = 0.30f;
    public float guideRadius = 0.38f;
    public float guideStartAngle = 0f;
    public float guideEndAngle = 76f;
    public float guideLift = 0.0f;
    public float arrowHeadLength = 0.22f;
    public float arrowHeadWidthMultiplier = 2.1f;
    [Range(0.02f, 0.5f)] public float movementTrackAlpha = 0.18f;
    public Color arrowColor = new Color(1f, 1f, 1f, 0.28f);

    [Header("Simulator Layout")]
    public float simulatorForwardDistance = 1.10f;
    public float simulatorChestHeightOffset = -0.02f;
    public float simulatorExpansionMultiplier = 1.0f;

    LineRenderer leftArrow;
    LineRenderer rightArrow;
    LineRenderer leftArrowHeadLine;
    LineRenderer rightArrowHeadLine;
    Material leftMaterial;
    Material rightMaterial;
    Material arrowMaterial;
    Coroutine tutorialRoutine;
    bool tutorialDemoActive;
    float tutorialDemoProgress;

    void Awake()
    {
        AutoFindReferences();
        EnsureVisuals();
    }

    void LateUpdate()
    {
        AutoFindReferences();
        EnsureVisuals();
        UpdateGuideBalls();
        UpdateArrows();
    }

    public void Configure(Game2_ActionDetector detector)
    {
        actionDetector = detector;
        AutoFindReferences();
        EnsureVisuals();
    }

    public void PlayTutorialDemo(float duration)
    {
        if (tutorialRoutine != null) StopCoroutine(tutorialRoutine);
        tutorialRoutine = StartCoroutine(TutorialDemoRoutine(Mathf.Max(0.1f, duration)));
    }

    public void StopTutorialDemo()
    {
        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }

        tutorialDemoActive = false;
        tutorialDemoProgress = 0f;
    }

    void AutoFindReferences()
    {
        if (actionDetector == null) actionDetector = GetComponent<Game2_ActionDetector>();

        if (actionDetector != null)
        {
            if (leftController == null) leftController = actionDetector.leftController;
            if (rightController == null) rightController = actionDetector.rightController;
            if (head == null) head = actionDetector.head;
        }

        if (head == null && Camera.main != null) head = Camera.main.transform;
    }

    void EnsureVisuals()
    {
        SyncBallAndTrackSize();

        if (leftMaterial == null) leftMaterial = CreateColorMaterial("Game2_LeftHandGuide", leftBallColor);
        if (rightMaterial == null) rightMaterial = CreateColorMaterial("Game2_RightHandGuide", rightBallColor);
        if (arrowMaterial == null) arrowMaterial = CreateColorMaterial("Game2_ArrowGuide", arrowColor, true);

        if (createGuideBalls)
        {
            if (leftGuideBall == null) leftGuideBall = CreateBall("Left_GuideBall", leftMaterial);
            if (rightGuideBall == null) rightGuideBall = CreateBall("Right_GuideBall", rightMaterial);
        }

        if (showMovementArrows)
        {
            if (leftArrow == null) leftArrow = CreateArrowLine("Left_MovementArrow");
            if (rightArrow == null) rightArrow = CreateArrowLine("Right_MovementArrow");
            if (showArrowHeads && leftArrowHeadLine == null) leftArrowHeadLine = CreateArrowHead("Left_ArrowHead");
            if (showArrowHeads && rightArrowHeadLine == null) rightArrowHeadLine = CreateArrowHead("Right_ArrowHead");
        }
    }

    Transform CreateBall(string objectName, Material material)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = objectName;
        ball.transform.SetParent(transform, false);
        ball.transform.localScale = Vector3.one * (ballRadius * 2f);

        Renderer renderer = ball.GetComponent<Renderer>();
        if (renderer != null) renderer.material = material;

        Collider collider = ball.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        return ball.transform;
    }

    LineRenderer CreateArrowLine(string objectName)
    {
        GameObject arrowObject = new GameObject(objectName);
        arrowObject.transform.SetParent(transform, false);

        LineRenderer line = arrowObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = arrowSegments + 1;
        line.startWidth = GetMovementTrackWidth();
        line.endWidth = GetMovementTrackWidth();
        line.startColor = GetMovementTrackColor();
        line.endColor = GetMovementTrackColor();
        line.numCapVertices = 10;
        line.numCornerVertices = 10;
        line.material = arrowMaterial;
        line.enabled = showMovementArrows;

        return line;
    }

    LineRenderer CreateArrowHead(string objectName)
    {
        GameObject arrowHead = new GameObject(objectName);
        arrowHead.transform.SetParent(transform, false);

        LineRenderer line = arrowHead.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 3;
        line.startWidth = GetMovementTrackWidth();
        line.endWidth = GetMovementTrackWidth();
        line.startColor = GetMovementTrackColor();
        line.endColor = GetMovementTrackColor();
        line.numCapVertices = 10;
        line.numCornerVertices = 10;
        line.material = arrowMaterial;

        return line;
    }

    void UpdateGuideBalls()
    {
        if (!createGuideBalls || leftGuideBall == null || rightGuideBall == null) return;

        GetHandPositions(out Vector3 leftPosition, out Vector3 rightPosition);
        leftGuideBall.position = leftPosition;
        rightGuideBall.position = rightPosition;

        float progress = GetGuideProgress();
        leftGuideBall.localScale = Vector3.one * GetBallDiameter(leftPosition, progress);
        rightGuideBall.localScale = Vector3.one * GetBallDiameter(rightPosition, progress);
    }

    void UpdateArrows()
    {
        bool visible = showMovementArrows && leftArrow != null && rightArrow != null;
        if (leftArrow != null) leftArrow.enabled = visible;
        if (rightArrow != null) rightArrow.enabled = visible;
        if (leftArrowHeadLine != null) leftArrowHeadLine.enabled = visible && showArrowHeads;
        if (rightArrowHeadLine != null) rightArrowHeadLine.enabled = visible && showArrowHeads;
        if (!visible) return;

        SyncBallAndTrackSize();
        DrawArcArrow(leftArrow, false, out Vector3 leftEnd, out Vector3 leftTangent);
        DrawArcArrow(rightArrow, true, out Vector3 rightEnd, out Vector3 rightTangent);
        if (showArrowHeads)
        {
            DrawArrowHead(leftArrowHeadLine, false, leftEnd, leftTangent);
            DrawArrowHead(rightArrowHeadLine, true, rightEnd, rightTangent);
        }
    }

    void DrawArcArrow(LineRenderer line, bool isRightSide, out Vector3 endPosition, out Vector3 endTangent)
    {
        if (line.positionCount != arrowSegments + 1)
        {
            line.positionCount = arrowSegments + 1;
        }

        line.startWidth = GetMovementTrackWidth();
        line.endWidth = GetMovementTrackWidth();
        line.startColor = GetMovementTrackColor();
        line.endColor = GetMovementTrackColor();

        Vector3 previous = GetArcPoint(isRightSide, guideStartAngle);
        for (int i = 0; i <= arrowSegments; i++)
        {
            float t = (float)i / arrowSegments;
            float angle = Mathf.Lerp(guideStartAngle, guideEndAngle, t);
            Vector3 point = GetArcPoint(isRightSide, angle);
            line.SetPosition(i, point);
            if (i == arrowSegments - 1) previous = point;
        }

        endPosition = GetArcPoint(isRightSide, guideEndAngle);
        endTangent = (endPosition - previous).normalized;
    }

    void DrawArrowHead(LineRenderer arrowHead, bool isRightSide, Vector3 tip, Vector3 direction)
    {
        if (arrowHead == null) return;
        if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;

        Vector3 forward = direction.normalized;
        Vector3 side = GetHeadRight() * (isRightSide ? 1f : -1f);

        Vector3 baseCenter = tip - forward * arrowHeadLength;
        float trackWidth = GetMovementTrackWidth();
        float halfWidth = trackWidth * arrowHeadWidthMultiplier;

        arrowHead.positionCount = 3;
        arrowHead.startWidth = trackWidth;
        arrowHead.endWidth = trackWidth;
        arrowHead.startColor = GetMovementTrackColor();
        arrowHead.endColor = GetMovementTrackColor();
        arrowHead.SetPosition(0, baseCenter - side * halfWidth);
        arrowHead.SetPosition(1, tip);
        arrowHead.SetPosition(2, baseCenter + side * halfWidth);
    }

    void GetHandPositions(out Vector3 leftPosition, out Vector3 rightPosition)
    {
        if (tutorialDemoActive)
        {
            float demoAngle = Mathf.Lerp(guideStartAngle, guideEndAngle, tutorialDemoProgress);
            leftPosition = GetArcPoint(false, demoAngle);
            rightPosition = GetArcPoint(true, demoAngle);
            return;
        }

        bool useSimulator = actionDetector != null && actionDetector.IsUsingKeyboardSimulator;
        if (!useSimulator && leftController != null && rightController != null)
        {
            leftPosition = leftController.position;
            rightPosition = rightController.position;
            return;
        }

        float progress = GetGuideProgress();
        float angle = Mathf.Lerp(guideStartAngle, guideEndAngle, progress);

        leftPosition = GetArcPoint(false, angle);
        rightPosition = GetArcPoint(true, angle);
    }

    Vector3 GetHeadRight()
    {
        Vector3 forward = GetHeadForward();
        return Vector3.Cross(Vector3.up, forward).normalized;
    }

    Vector3 GetHeadForward()
    {
        Transform reference = head != null ? head : transform;
        Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        return forward;
    }

    float GetPerspectiveScale(Vector3 position)
    {
        if (!exaggeratePerspectiveScale || head == null) return 1f;

        Vector3 forward = GetHeadForward();
        float distanceAlongView = Vector3.Dot(position - head.position, forward);
        float normalizedNear = Mathf.InverseLerp(simulatorForwardDistance + 0.25f, simulatorForwardDistance - 0.05f, distanceAlongView);
        return 1f + normalizedNear * nearBallScaleBoost;
    }

    float GetBallDiameter(Vector3 position, float progress)
    {
        float diameter = ballRadius * 2f * GetBallScaleByProgress(progress);
        if (compensateCameraPerspective && head != null)
        {
            float currentDistance = Vector3.Distance(head.position, position);
            float referenceDistance = GetReferenceBallDistance(progress);
            if (referenceDistance > 0.001f)
            {
                diameter *= currentDistance / referenceDistance;
            }
        }

        return diameter * GetPerspectiveScale(position);
    }

    float GetReferenceBallDistance(float progress)
    {
        if (head == null) return 1f;

        Vector3 start = GetArcPoint(false, guideStartAngle);
        return Vector3.Distance(head.position, start);
    }

    float GetGuideProgress()
    {
        if (tutorialDemoActive) return tutorialDemoProgress;

        float rawProgress = actionDetector != null
            ? Mathf.InverseLerp(0f, Mathf.Max(0.01f, actionDetector.config.targetExpansion), actionDetector.CurrentExpansion)
            : 0f;

        return Mathf.Clamp01(rawProgress * simulatorExpansionMultiplier);
    }

    IEnumerator TutorialDemoRoutine(float duration)
    {
        tutorialDemoActive = true;
        float elapsed = 0f;
        float cycleDuration = 3.2f;

        while (elapsed < duration)
        {
            float cycle = Mathf.PingPong(elapsed / cycleDuration, 1f);
            tutorialDemoProgress = Mathf.SmoothStep(0f, 1f, cycle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        tutorialDemoProgress = 0f;
        tutorialDemoActive = false;
        tutorialRoutine = null;
    }

    float GetBallScaleByProgress(float progress)
    {
        float middleDip = Mathf.Sin(progress * Mathf.PI);
        return Mathf.Lerp(1f, middleBallScale, middleDip);
    }

    void SyncBallAndTrackSize()
    {
        guideTrackWidth = Mathf.Max(0.01f, guideTrackWidth);
        movementTrackWidth = Mathf.Max(0.01f, movementTrackWidth);
        ballRadius = guideTrackWidth * 0.5f;
    }

    float GetMovementTrackWidth()
    {
        return Mathf.Max(0.01f, movementTrackWidth);
    }

    Color GetMovementTrackColor()
    {
        return new Color(arrowColor.r, arrowColor.g, arrowColor.b, Mathf.Min(arrowColor.a, movementTrackAlpha));
    }

    Vector3 GetArcPoint(bool isRightSide, float angleDegrees)
    {
        Transform reference = head != null ? head : transform;
        Vector3 forward = GetHeadForward();
        Vector3 right = GetHeadRight();
        Vector3 chestCenter = reference.position + forward * simulatorForwardDistance + Vector3.up * simulatorChestHeightOffset;

        float sideSign = isRightSide ? 1f : -1f;
        Vector3 shoulderPivot = chestCenter + right * shoulderHalfWidth * sideSign;
        float radians = angleDegrees * Mathf.Deg2Rad;

        Vector3 radial =
            forward * Mathf.Cos(radians) +
            right * sideSign * Mathf.Sin(radians);

        float lift = Mathf.Sin(Mathf.InverseLerp(guideStartAngle, guideEndAngle, angleDegrees) * Mathf.PI) * guideLift;
        return shoulderPivot + radial * guideRadius + Vector3.up * lift;
    }

    Material CreateColorMaterial(string materialName, Color color, bool transparent = false)
    {
        Shader shader = transparent ? Shader.Find("Sprites/Default") : null;
        bool usingScriptablePipeline = GraphicsSettings.currentRenderPipeline != null;
        if (shader == null)
        {
            shader = usingScriptablePipeline
                ? Shader.Find("Universal Render Pipeline/Unlit")
                : Shader.Find("Unlit/Color");
        }

        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = materialName;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;

        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.4f);

        if (transparent)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);
            material.SetOverrideTag("RenderType", "Transparent");
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_ALPHABLEND_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        return material;
    }
}
