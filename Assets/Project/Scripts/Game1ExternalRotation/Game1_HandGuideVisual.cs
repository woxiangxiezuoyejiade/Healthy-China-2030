using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Game1_HandGuideVisual : MonoBehaviour
{
    [Header("Tracked Objects")]
    public Transform leftController;
    public Transform rightController;
    public Transform head;
    public Game1_ActionDetector actionDetector;

    [Header("Guide Balls")]
    public bool createGuideBalls = true;
    public Transform leftGuideBall;
    public Transform rightGuideBall;
    public float guideTrackWidth = 0.07f;
    public Color leftBallColor = new Color(0.18f, 0.55f, 1f);
    public Color rightBallColor = new Color(1f, 0.48f, 0.18f);
    [Range(0.5f, 1f)] public float middleBallScale = 0.82f;

    [Header("External Rotation Guide")]
    public bool showMovementArrows = true;
    public int arrowSegments = 28;
    public float elbowHalfWidth = 0.28f;
    public float forearmRadius = 0.42f;
    public float startAngle = -12f;
    public float endAngle = 62f;
    public float guideHeightOffset = -0.32f;
    public float guideForwardDistance = 0.95f;
    public float guideLift = 0.03f;
    public float arrowHeadLength = 0.14f;
    public float arrowHeadWidthMultiplier = 1.55f;
    public Color arrowColor = new Color(1f, 1f, 1f, 0.30f);

    [Header("Tutorial Demo")]
    public float tutorialCycleDuration = 3.0f;

    Game1_TrainingHand trainingHand = Game1_TrainingHand.Right;
    Game1_DifficultyConfig config;
    LineRenderer leftArrow;
    LineRenderer rightArrow;
    LineRenderer leftArrowHead;
    LineRenderer rightArrowHead;
    Material leftMaterial;
    Material rightMaterial;
    Material arrowMaterial;
    Coroutine tutorialRoutine;
    bool tutorialDemoActive;
    float tutorialDemoProgress;
    Game1_TrainingHand tutorialDemoHand = Game1_TrainingHand.Right;

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

    public void Configure(Game1_ActionDetector detector, Game1_TrainingHand hand, Game1_DifficultyConfig difficultyConfig)
    {
        actionDetector = detector;
        trainingHand = hand;
        config = difficultyConfig;
        AutoFindReferences();
        EnsureVisuals();
    }

    public void PlayTutorialDemo(float duration)
    {
        if (tutorialRoutine != null) StopCoroutine(tutorialRoutine);

        tutorialDemoActive = true;
        tutorialDemoProgress = 0f;
        tutorialDemoHand = Game1_TrainingHand.Right;
        tutorialRoutine = StartCoroutine(TutorialDemoRoutine(duration));
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
        tutorialDemoHand = trainingHand;
    }

    void AutoFindReferences()
    {
        if (actionDetector == null) actionDetector = GetComponent<Game1_ActionDetector>();

        if (actionDetector != null)
        {
            if (leftController == null) leftController = actionDetector.leftController;
            if (rightController == null) rightController = actionDetector.rightController;
            if (head == null) head = actionDetector.head;
            trainingHand = actionDetector.TrainingHand;
        }

        if (head == null && Camera.main != null) head = Camera.main.transform;
    }

    void EnsureVisuals()
    {
        guideTrackWidth = Mathf.Max(0.01f, guideTrackWidth);

        if (leftMaterial == null) leftMaterial = CreateColorMaterial("Game1_LeftHandGuide", leftBallColor, false);
        if (rightMaterial == null) rightMaterial = CreateColorMaterial("Game1_RightHandGuide", rightBallColor, false);
        if (arrowMaterial == null) arrowMaterial = CreateColorMaterial("Game1_ArrowGuide", arrowColor, true);

        if (createGuideBalls)
        {
            if (leftGuideBall == null) leftGuideBall = CreateBall("Game1_Left_GuideBall", leftMaterial);
            if (rightGuideBall == null) rightGuideBall = CreateBall("Game1_Right_GuideBall", rightMaterial);
        }

        if (showMovementArrows)
        {
            if (leftArrow == null) leftArrow = CreateArrowLine("Game1_Left_MovementArrow");
            if (rightArrow == null) rightArrow = CreateArrowLine("Game1_Right_MovementArrow");
            if (leftArrowHead == null) leftArrowHead = CreateArrowHead("Game1_Left_ArrowHead");
            if (rightArrowHead == null) rightArrowHead = CreateArrowHead("Game1_Right_ArrowHead");
        }
    }

    Transform CreateBall(string objectName, Material material)
    {
        GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ball.name = objectName;
        ball.transform.SetParent(transform, false);
        ball.transform.localScale = Vector3.one * guideTrackWidth;

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
        line.startWidth = guideTrackWidth;
        line.endWidth = guideTrackWidth;
        line.startColor = arrowColor;
        line.endColor = arrowColor;
        line.numCapVertices = 10;
        line.numCornerVertices = 10;
        line.material = arrowMaterial;
        return line;
    }

    LineRenderer CreateArrowHead(string objectName)
    {
        GameObject arrowHead = new GameObject(objectName);
        arrowHead.transform.SetParent(transform, false);

        LineRenderer line = arrowHead.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 3;
        line.startWidth = guideTrackWidth;
        line.endWidth = guideTrackWidth;
        line.startColor = arrowColor;
        line.endColor = arrowColor;
        line.numCapVertices = 10;
        line.numCornerVertices = 10;
        line.material = arrowMaterial;
        return line;
    }

    void UpdateGuideBalls()
    {
        if (!createGuideBalls || leftGuideBall == null || rightGuideBall == null) return;

        GetHandPositions(out Vector3 leftPosition, out Vector3 rightPosition);
        Game1_TrainingHand activeHand = tutorialDemoActive ? tutorialDemoHand : trainingHand;
        float leftScale = guideTrackWidth * (activeHand == Game1_TrainingHand.Left ? GetBallScaleByProgress() : 1f);
        float rightScale = guideTrackWidth * (activeHand == Game1_TrainingHand.Right ? GetBallScaleByProgress() : 1f);

        leftGuideBall.position = leftPosition;
        rightGuideBall.position = rightPosition;
        leftGuideBall.localScale = Vector3.one * leftScale;
        rightGuideBall.localScale = Vector3.one * rightScale;
    }

    void UpdateArrows()
    {
        bool visible = showMovementArrows && leftArrow != null && rightArrow != null;
        SetLineVisible(leftArrow, visible);
        SetLineVisible(rightArrow, visible);
        SetLineVisible(leftArrowHead, visible);
        SetLineVisible(rightArrowHead, visible);
        if (!visible) return;

        DrawArc(leftArrow, false, out Vector3 leftEnd, out Vector3 leftTangent);
        DrawArc(rightArrow, true, out Vector3 rightEnd, out Vector3 rightTangent);
        DrawArrowHead(leftArrowHead, false, leftEnd, leftTangent);
        DrawArrowHead(rightArrowHead, true, rightEnd, rightTangent);
    }

    void DrawArc(LineRenderer line, bool isRightSide, out Vector3 endPosition, out Vector3 endTangent)
    {
        line.positionCount = arrowSegments + 1;
        line.startWidth = guideTrackWidth;
        line.endWidth = guideTrackWidth;
        line.startColor = arrowColor;
        line.endColor = arrowColor;

        Vector3 previous = GetGuidePoint(isRightSide, startAngle);
        for (int i = 0; i <= arrowSegments; i++)
        {
            float t = (float)i / arrowSegments;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            Vector3 point = GetGuidePoint(isRightSide, angle);
            line.SetPosition(i, point);
            if (i == arrowSegments - 1) previous = point;
        }

        endPosition = GetGuidePoint(isRightSide, endAngle);
        endTangent = (endPosition - previous).normalized;
    }

    void DrawArrowHead(LineRenderer line, bool isRightSide, Vector3 tip, Vector3 tangent)
    {
        if (line == null) return;
        if (tangent.sqrMagnitude < 0.001f) tangent = GetHeadRight() * (isRightSide ? 1f : -1f);

        Vector3 side = GetHeadRight() * (isRightSide ? 1f : -1f);
        Vector3 baseCenter = tip - tangent.normalized * arrowHeadLength;
        float halfWidth = guideTrackWidth * arrowHeadWidthMultiplier;

        line.positionCount = 3;
        line.startWidth = guideTrackWidth;
        line.endWidth = guideTrackWidth;
        line.startColor = arrowColor;
        line.endColor = arrowColor;
        line.SetPosition(0, baseCenter - side * halfWidth);
        line.SetPosition(1, tip);
        line.SetPosition(2, baseCenter + side * halfWidth);
    }

    void GetHandPositions(out Vector3 leftPosition, out Vector3 rightPosition)
    {
        if (tutorialDemoActive)
        {
            float demoAngle = Mathf.Lerp(startAngle, endAngle, tutorialDemoProgress);
            leftPosition = tutorialDemoHand == Game1_TrainingHand.Left ? GetGuidePoint(false, demoAngle) : GetGuidePoint(false, startAngle);
            rightPosition = tutorialDemoHand == Game1_TrainingHand.Right ? GetGuidePoint(true, demoAngle) : GetGuidePoint(true, startAngle);
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
        float angle = Mathf.Lerp(startAngle, endAngle, progress);
        leftPosition = trainingHand == Game1_TrainingHand.Left ? GetGuidePoint(false, angle) : GetGuidePoint(false, startAngle);
        rightPosition = trainingHand == Game1_TrainingHand.Right ? GetGuidePoint(true, angle) : GetGuidePoint(true, startAngle);
    }

    Vector3 GetGuidePoint(bool isRightSide, float angleDegrees)
    {
        Transform reference = head != null ? head : transform;
        Vector3 forward = GetHeadForward();
        Vector3 right = GetHeadRight();
        Vector3 chestCenter = reference.position + forward * guideForwardDistance + Vector3.up * guideHeightOffset;

        float sideSign = isRightSide ? 1f : -1f;
        Vector3 elbowPivot = chestCenter + right * elbowHalfWidth * sideSign;
        float radians = angleDegrees * Mathf.Deg2Rad;

        Vector3 radial =
            forward * Mathf.Cos(radians) +
            right * sideSign * Mathf.Sin(radians);

        float lift = Mathf.Sin(Mathf.InverseLerp(startAngle, endAngle, angleDegrees) * Mathf.PI) * guideLift;
        return elbowPivot + radial * forearmRadius + Vector3.up * lift;
    }

    float GetGuideProgress()
    {
        if (tutorialDemoActive) return tutorialDemoProgress;
        if (actionDetector == null || config == null) return 0f;
        return Mathf.InverseLerp(0f, Mathf.Max(0.01f, config.targetAngle), actionDetector.CurrentAngle);
    }

    float GetBallScaleByProgress()
    {
        float progress = GetGuideProgress();
        float middleDip = Mathf.Sin(progress * Mathf.PI);
        return Mathf.Lerp(1f, middleBallScale, middleDip);
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

    void SetLineVisible(LineRenderer line, bool visible)
    {
        if (line != null) line.enabled = visible;
    }

    IEnumerator TutorialDemoRoutine(float duration)
    {
        float startTime = Time.time;
        float totalDuration = Mathf.Max(0.1f, duration);
        float cycleDuration = Mathf.Max(0.5f, tutorialCycleDuration);

        while (Time.time - startTime < totalDuration)
        {
            float elapsed = Time.time - startTime;
            int cycleIndex = Mathf.FloorToInt(elapsed / cycleDuration);
            float cycleT = Mathf.Repeat(elapsed, cycleDuration) / cycleDuration;

            tutorialDemoHand = cycleIndex % 2 == 0 ? Game1_TrainingHand.Right : Game1_TrainingHand.Left;

            float openClose = cycleT < 0.5f
                ? Mathf.SmoothStep(0f, 1f, cycleT * 2f)
                : Mathf.SmoothStep(1f, 0f, (cycleT - 0.5f) * 2f);

            tutorialDemoProgress = openClose;
            yield return null;
        }

        StopTutorialDemo();
    }

    Material CreateColorMaterial(string materialName, Color color, bool transparent)
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
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.3f);

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
