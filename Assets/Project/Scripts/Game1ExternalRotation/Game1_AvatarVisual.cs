using UnityEngine;

public enum AvatarMode { Idle, Tutorial, Tracking }

public class Game1_AvatarVisual : MonoBehaviour
{
    [Header("Placement")]
    public Vector3 avatarOffset = new Vector3(0.52f, -0.26f, 1.45f);
    public float avatarScale = 0.22f;
    public bool followCamera = true;

    [Header("Animation")]
    public float tutorialCycleTime = 3.2f;
    public bool alternateHandsInTutorial = true;
    public float tutorialVisualAngle = 74f;
    public float smoothSpeed = 12f;
    public float forearmStartRotation = -90f;
    public float forearmEndRotation = 0f;

    [Header("Visual")]
    public float bodyAlpha = 0.88f;
    public float activeArmAlpha = 1f;
    public float inactiveArmAlpha = 0.72f;
    public bool showLegs = false;
    public bool deleteLegacyCapsuleObject = true;

    static readonly Color SkinColor = new Color(0.82f, 0.62f, 0.42f);
    static readonly Color HairColor = new Color(0.18f, 0.11f, 0.05f);
    static readonly Color ShirtColor = new Color(0.13f, 0.40f, 0.82f);
    static readonly Color PantsColor = new Color(0.09f, 0.16f, 0.42f);
    static readonly Color ShoeColor = new Color(0.10f, 0.10f, 0.12f);
    static readonly Color EyeColor = new Color(0.05f, 0.08f, 0.10f);

    AvatarMode mode = AvatarMode.Idle;
    Game1_TrainingHand activeHand = Game1_TrainingHand.Right;
    float targetAngle = 45f;
    float currentAngle;
    float displayedAngle;
    float tutorialTimer;
    Game1_TrainingHand tutorialStartHand = Game1_TrainingHand.Right;

    Transform visualRoot;
    Transform rightForearmPivot;
    Transform leftForearmPivot;
    Renderer[] rightArmRenderers;
    Renderer[] leftArmRenderers;

    Material skinBody;
    Material skinActive;
    Material skinInactive;
    Material shirtBody;
    Material shirtActive;
    Material shirtInactive;
    Material pantsMaterial;
    Material shoeMaterial;
    Material hairMaterial;
    Material eyeMaterial;

    void Awake()
    {
        BuildAvatar();
        ApplyArmHighlight();
        ApplyArmRotations(true);
    }

    void Update()
    {
        if (mode == AvatarMode.Tutorial)
        {
            tutorialTimer += Time.deltaTime;
            float cycleDuration = Mathf.Max(0.5f, tutorialCycleTime);
            int cycleIndex = Mathf.FloorToInt(tutorialTimer / cycleDuration);
            float cycleT = Mathf.Repeat(tutorialTimer, cycleDuration) / cycleDuration;

            Game1_TrainingHand tutorialHand = tutorialStartHand;
            if (alternateHandsInTutorial && cycleIndex % 2 == 1)
            {
                tutorialHand = tutorialStartHand == Game1_TrainingHand.Right
                    ? Game1_TrainingHand.Left
                    : Game1_TrainingHand.Right;
            }

            if (tutorialHand != activeHand)
            {
                activeHand = tutorialHand;
                ApplyArmHighlight();
            }

            float openClose = cycleT < 0.5f
                ? Mathf.SmoothStep(0f, 1f, cycleT * 2f)
                : Mathf.SmoothStep(1f, 0f, (cycleT - 0.5f) * 2f);

            currentAngle = tutorialVisualAngle * openClose;
        }

        displayedAngle = Mathf.Lerp(displayedAngle, currentAngle, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        ApplyArmRotations(false);
    }

    void LateUpdate()
    {
        PlaceAvatar();
    }

    public void SetMode(AvatarMode newMode)
    {
        mode = newMode;
        tutorialTimer = 0f;
        tutorialStartHand = activeHand;

        if (newMode == AvatarMode.Idle)
        {
            currentAngle = 0f;
            displayedAngle = 0f;
            ApplyArmRotations(true);
        }
    }

    public void SetTargetAngle(float angle)
    {
        targetAngle = Mathf.Max(1f, angle);
        if (tutorialVisualAngle <= 0f)
        {
            tutorialVisualAngle = targetAngle;
        }
    }

    public void SetTutorialMotion(float cycleTime, float visualAngle, bool alternateHands)
    {
        tutorialCycleTime = Mathf.Max(0.5f, cycleTime);
        tutorialVisualAngle = Mathf.Max(1f, visualAngle);
        alternateHandsInTutorial = alternateHands;
    }

    public void SetCurrentAngle(float angle)
    {
        if (mode != AvatarMode.Tracking) return;
        currentAngle = Mathf.Clamp(angle, 0f, targetAngle * 1.35f);
    }

    public void SetActiveHand(Game1_TrainingHand hand)
    {
        activeHand = hand;
        ApplyArmHighlight();
        if (mode == AvatarMode.Idle)
        {
            currentAngle = 0f;
            displayedAngle = 0f;
        }
    }

    void BuildAvatar()
    {
        DeleteLegacyAvatarObjects();
        ClearOldAvatar();
        CreateMaterials();

        GameObject rootObject = new GameObject("Game1_BlockAvatarRoot");
        visualRoot = rootObject.transform;
        PlaceAvatar();

        Transform bodyRoot = new GameObject("SteveStyle_Bust").transform;
        bodyRoot.SetParent(visualRoot, false);

        float torsoWidth = 0.42f;
        float torsoHeight = 0.55f;
        float torsoDepth = 0.24f;
        float headSize = 0.28f;
        float armWidth = 0.13f;
        float upperArmLength = 0.34f;
        float forearmLength = 0.42f;

        Block("Torso", bodyRoot, new Vector3(0f, 0.35f, 0f), new Vector3(torsoWidth, torsoHeight, torsoDepth), shirtBody);
        Block("Neck", bodyRoot, new Vector3(0f, 0.67f, 0f), new Vector3(0.11f, 0.08f, 0.11f), skinBody);
        Block("Head", bodyRoot, new Vector3(0f, 0.86f, 0f), Vector3.one * headSize, skinBody);
        Block("Hair_Top", bodyRoot, new Vector3(0f, 1.02f, -0.005f), new Vector3(headSize * 1.04f, 0.055f, headSize * 1.06f), hairMaterial);
        Block("Hair_Back", bodyRoot, new Vector3(0f, 0.91f, 0.15f), new Vector3(headSize * 1.05f, 0.20f, 0.045f), hairMaterial);
        Block("Right_Eye", bodyRoot, new Vector3(0.06f, 0.89f, -0.145f), new Vector3(0.045f, 0.032f, 0.012f), eyeMaterial);
        Block("Left_Eye", bodyRoot, new Vector3(-0.06f, 0.89f, -0.145f), new Vector3(0.045f, 0.032f, 0.012f), eyeMaterial);

        BuildArm(bodyRoot, true, torsoWidth, torsoHeight, armWidth, upperArmLength, forearmLength);
        BuildArm(bodyRoot, false, torsoWidth, torsoHeight, armWidth, upperArmLength, forearmLength);

        if (showLegs)
        {
            Block("Right_Leg", bodyRoot, new Vector3(0.10f, -0.03f, 0f), new Vector3(0.14f, 0.35f, 0.15f), pantsMaterial);
            Block("Left_Leg", bodyRoot, new Vector3(-0.10f, -0.03f, 0f), new Vector3(0.14f, 0.35f, 0.15f), pantsMaterial);
            Block("Right_Shoe", bodyRoot, new Vector3(0.10f, -0.23f, -0.04f), new Vector3(0.16f, 0.07f, 0.22f), shoeMaterial);
            Block("Left_Shoe", bodyRoot, new Vector3(-0.10f, -0.23f, -0.04f), new Vector3(0.16f, 0.07f, 0.22f), shoeMaterial);
        }

        foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>())
        {
            Destroy(collider);
        }
    }

    void BuildArm(Transform parent, bool right, float torsoWidth, float torsoHeight, float armWidth, float upperArmLength, float forearmLength)
    {
        float side = right ? 1f : -1f;
        string prefix = right ? "Right" : "Left";

        Transform shoulder = new GameObject(prefix + "_Shoulder").transform;
        shoulder.SetParent(parent, false);
        shoulder.localPosition = new Vector3(side * (torsoWidth * 0.5f + armWidth * 0.30f), 0.35f + torsoHeight * 0.38f, 0f);

        Renderer upper = Block(prefix + "_UpperArm", shoulder, new Vector3(side * upperArmLength * 0.5f, 0f, 0f), new Vector3(upperArmLength, armWidth, armWidth), shirtBody).GetComponent<Renderer>();

        Transform elbow = new GameObject(prefix + "_ElbowPivot").transform;
        elbow.SetParent(shoulder, false);
        elbow.localPosition = new Vector3(side * upperArmLength, 0f, 0f);

        Renderer elbowBlock = Block(prefix + "_Elbow", elbow, Vector3.zero, Vector3.one * armWidth * 0.78f, skinBody).GetComponent<Renderer>();
        Renderer forearm = Block(prefix + "_Forearm", elbow, new Vector3(0f, forearmLength * 0.5f, 0f), new Vector3(armWidth * 0.95f, forearmLength, armWidth * 0.95f), shirtBody).GetComponent<Renderer>();
        Renderer hand = Block(prefix + "_Hand", elbow, new Vector3(0f, forearmLength + armWidth * 0.42f, 0f), Vector3.one * armWidth * 0.92f, skinBody).GetComponent<Renderer>();

        if (right)
        {
            rightForearmPivot = elbow;
            rightArmRenderers = new[] { upper, elbowBlock, forearm, hand };
        }
        else
        {
            leftForearmPivot = elbow;
            leftArmRenderers = new[] { upper, elbowBlock, forearm, hand };
        }
    }

    GameObject Block(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = localPosition;
        block.transform.localScale = localScale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }

    void ApplyArmRotations(bool immediate)
    {
        float angle = immediate ? currentAngle : displayedAngle;

        float progress = Mathf.InverseLerp(0f, Mathf.Max(1f, targetAngle), angle);
        float activeRotation = Mathf.Lerp(forearmStartRotation, forearmEndRotation, progress);
        float idleRotation = forearmStartRotation;

        float rightAngle = activeHand == Game1_TrainingHand.Right ? activeRotation : idleRotation;
        float leftAngle = activeHand == Game1_TrainingHand.Left ? activeRotation : idleRotation;

        if (rightForearmPivot != null) rightForearmPivot.localRotation = Quaternion.Euler(rightAngle, 0f, 0f);
        if (leftForearmPivot != null) leftForearmPivot.localRotation = Quaternion.Euler(leftAngle, 0f, 0f);
    }

    void ApplyArmHighlight()
    {
        SetArmMaterials(rightArmRenderers, activeHand == Game1_TrainingHand.Right);
        SetArmMaterials(leftArmRenderers, activeHand == Game1_TrainingHand.Left);
    }

    void SetArmMaterials(Renderer[] renderers, bool active)
    {
        if (renderers == null) return;

        Material shirt = active ? shirtActive : shirtInactive;
        Material skin = active ? skinActive : skinInactive;

        if (renderers.Length > 0 && renderers[0] != null) renderers[0].sharedMaterial = shirt;
        if (renderers.Length > 1 && renderers[1] != null) renderers[1].sharedMaterial = skin;
        if (renderers.Length > 2 && renderers[2] != null) renderers[2].sharedMaterial = shirt;
        if (renderers.Length > 3 && renderers[3] != null) renderers[3].sharedMaterial = skin;
    }

    void PlaceAvatar()
    {
        if (visualRoot == null) return;

        Transform parent = followCamera && Camera.main != null ? Camera.main.transform : transform;
        if (visualRoot.parent != parent)
        {
            visualRoot.SetParent(parent, false);
        }

        visualRoot.localPosition = avatarOffset;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one * avatarScale;
    }

    void ClearOldAvatar()
    {
        Transform existing = transform.Find("Game1_BlockAvatarRoot");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject oldCameraRoot = GameObject.Find("Game1_BlockAvatarRoot");
        if (oldCameraRoot != null)
        {
            Destroy(oldCameraRoot);
        }

        Transform oldHologram = transform.Find("HologramRoot");
        if (oldHologram != null)
        {
            Destroy(oldHologram.gameObject);
        }
    }

    void DeleteLegacyAvatarObjects()
    {
        if (!deleteLegacyCapsuleObject) return;

        GameObject capsule = GameObject.Find("Capsule");
        if (capsule == null || capsule == gameObject) return;
        if (capsule.GetComponent<Game1_ExternalRotationController>() != null) return;

        Destroy(capsule);
    }

    void CreateMaterials()
    {
        skinBody = CreateMaterial("Game1_Avatar_Skin_Body", SkinColor, bodyAlpha);
        skinActive = CreateMaterial("Game1_Avatar_Skin_Active", SkinColor, activeArmAlpha);
        skinInactive = CreateMaterial("Game1_Avatar_Skin_Inactive", SkinColor, inactiveArmAlpha);
        shirtBody = CreateMaterial("Game1_Avatar_Shirt_Body", ShirtColor, bodyAlpha);
        shirtActive = CreateMaterial("Game1_Avatar_Shirt_Active", ShirtColor, activeArmAlpha);
        shirtInactive = CreateMaterial("Game1_Avatar_Shirt_Inactive", ShirtColor, inactiveArmAlpha);
        pantsMaterial = CreateMaterial("Game1_Avatar_Pants", PantsColor, bodyAlpha);
        shoeMaterial = CreateMaterial("Game1_Avatar_Shoe", ShoeColor, bodyAlpha);
        hairMaterial = CreateMaterial("Game1_Avatar_Hair", HairColor, bodyAlpha);
        eyeMaterial = CreateMaterial("Game1_Avatar_Eye", EyeColor, 1f);
    }

    Material CreateMaterial(string materialName, Color baseColor, float alpha)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.name = materialName;
        Color color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);

        if (alpha < 0.999f && shader.name == "Standard")
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;

            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        }

        return material;
    }
}
