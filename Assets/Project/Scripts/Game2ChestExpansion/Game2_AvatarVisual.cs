using UnityEngine;

public enum Game2_AvatarMode { Idle, Tutorial, Tracking }

public class Game2_AvatarVisual : MonoBehaviour
{
    [Header("Placement")]
    public Vector3 avatarOffset = new Vector3(0.62f, -0.45f, 1.36f);
    public float avatarScale = 0.24f;
    public bool followCamera = true;

    [Header("Animation")]
    public float tutorialCycleTime = 3.2f;
    public float smoothSpeed = 12f;

    [Header("Visual")]
    public float bodyAlpha = 0.95f;
    public float activeArmAlpha = 1f;

    static readonly Color SkinColor = new Color(0.82f, 0.62f, 0.42f);
    static readonly Color HairColor = new Color(0.18f, 0.11f, 0.05f);
    static readonly Color ShirtColor = new Color(0.12f, 0.44f, 0.78f);
    static readonly Color EyeColor = new Color(0.05f, 0.08f, 0.10f);

    Game2_AvatarMode mode = Game2_AvatarMode.Idle;
    float currentProgress;
    float displayedProgress;
    float tutorialTimer;

    Transform visualRoot;
    Transform bodyRoot;
    Transform rightUpperArm;
    Transform rightForearm;
    Transform rightHand;
    Transform leftUpperArm;
    Transform leftForearm;
    Transform leftHand;

    Material skinMaterial;
    Material shirtMaterial;
    Material hairMaterial;
    Material eyeMaterial;

    void Awake()
    {
        BuildAvatar();
        ApplyArmPose(true);
    }

    void Update()
    {
        if (mode == Game2_AvatarMode.Tutorial)
        {
            tutorialTimer += Time.deltaTime;
            float cycle = Mathf.PingPong(tutorialTimer / Mathf.Max(0.5f, tutorialCycleTime), 1f);
            currentProgress = Mathf.SmoothStep(0f, 1f, cycle);
        }

        displayedProgress = Mathf.Lerp(displayedProgress, currentProgress, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));
        ApplyArmPose(false);
    }

    void LateUpdate()
    {
        PlaceAvatar();
    }

    public void SetMode(Game2_AvatarMode newMode)
    {
        mode = newMode;
        tutorialTimer = 0f;

        if (newMode == Game2_AvatarMode.Idle)
        {
            currentProgress = 0f;
            displayedProgress = 0f;
            ApplyArmPose(true);
        }
    }

    public void SetProgress(float progress)
    {
        if (mode != Game2_AvatarMode.Tracking) return;
        currentProgress = Mathf.Clamp01(progress);
    }

    public void SetTutorialMotion(float cycleTime)
    {
        tutorialCycleTime = Mathf.Max(0.5f, cycleTime);
    }

    void BuildAvatar()
    {
        ClearOldAvatar();
        CreateMaterials();

        GameObject rootObject = new GameObject("Game2_BlockAvatarRoot");
        visualRoot = rootObject.transform;
        PlaceAvatar();

        bodyRoot = new GameObject("ChestExpansion_Avatar").transform;
        bodyRoot.SetParent(visualRoot, false);

        Block("Torso", bodyRoot, new Vector3(0f, 0.34f, 0f), new Vector3(0.48f, 0.58f, 0.24f), shirtMaterial);
        Block("Neck", bodyRoot, new Vector3(0f, 0.69f, -0.01f), new Vector3(0.12f, 0.08f, 0.12f), skinMaterial);
        Block("Head", bodyRoot, new Vector3(0f, 0.89f, -0.01f), Vector3.one * 0.30f, skinMaterial);
        Block("Hair_Top", bodyRoot, new Vector3(0f, 1.06f, -0.015f), new Vector3(0.31f, 0.06f, 0.32f), hairMaterial);
        Block("Hair_Back", bodyRoot, new Vector3(0f, 0.93f, 0.15f), new Vector3(0.31f, 0.22f, 0.05f), hairMaterial);
        Block("Right_Eye", bodyRoot, new Vector3(0.065f, 0.91f, -0.165f), new Vector3(0.045f, 0.032f, 0.012f), eyeMaterial);
        Block("Left_Eye", bodyRoot, new Vector3(-0.065f, 0.91f, -0.165f), new Vector3(0.045f, 0.032f, 0.012f), eyeMaterial);

        rightUpperArm = Block("Right_UpperArm", bodyRoot, Vector3.zero, Vector3.one, shirtMaterial).transform;
        rightForearm = Block("Right_Forearm", bodyRoot, Vector3.zero, Vector3.one, shirtMaterial).transform;
        rightHand = Block("Right_Hand", bodyRoot, Vector3.zero, Vector3.one * 0.10f, skinMaterial).transform;
        leftUpperArm = Block("Left_UpperArm", bodyRoot, Vector3.zero, Vector3.one, shirtMaterial).transform;
        leftForearm = Block("Left_Forearm", bodyRoot, Vector3.zero, Vector3.one, shirtMaterial).transform;
        leftHand = Block("Left_Hand", bodyRoot, Vector3.zero, Vector3.one * 0.10f, skinMaterial).transform;

        foreach (Collider collider in visualRoot.GetComponentsInChildren<Collider>())
        {
            Destroy(collider);
        }
    }

    void ApplyArmPose(bool immediate)
    {
        float progress = immediate ? currentProgress : displayedProgress;

        PoseArm(true, progress);
        PoseArm(false, progress);
    }

    void PoseArm(bool rightSide, float progress)
    {
        float side = rightSide ? 1f : -1f;

        Vector3 shoulder = new Vector3(side * 0.30f, 0.58f, -0.035f);
        Vector3 elbowClosed = new Vector3(side * 0.42f, 0.58f, -0.28f);
        Vector3 elbowOpen = new Vector3(side * 0.76f, 0.58f, -0.08f);
        Vector3 elbow = Vector3.Lerp(elbowClosed, elbowOpen, progress);
        Vector3 hand = elbow + Vector3.up * 0.42f;

        Transform upper = rightSide ? rightUpperArm : leftUpperArm;
        Transform forearm = rightSide ? rightForearm : leftForearm;
        Transform palm = rightSide ? rightHand : leftHand;

        SetBlockBetween(upper, shoulder, elbow, 0.105f);
        SetBlockBetween(forearm, elbow, hand, 0.098f);
        if (palm != null)
        {
            palm.localPosition = hand;
            palm.localRotation = Quaternion.identity;
            palm.localScale = Vector3.one * 0.115f;
        }
    }
    void SetBlockBetween(Transform block, Vector3 a, Vector3 b, float width)
    {
        if (block == null) return;

        Vector3 delta = b - a;
        float length = Mathf.Max(0.001f, delta.magnitude);
        block.localPosition = (a + b) * 0.5f;
        block.localRotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        block.localScale = new Vector3(width, width, length);
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
        Transform existing = transform.Find("Game2_BlockAvatarRoot");
        if (existing != null) Destroy(existing.gameObject);

        GameObject oldCameraRoot = GameObject.Find("Game2_BlockAvatarRoot");
        if (oldCameraRoot != null) Destroy(oldCameraRoot);
    }

    void CreateMaterials()
    {
        skinMaterial = CreateMaterial("Game2_Avatar_Skin", SkinColor, activeArmAlpha);
        shirtMaterial = CreateMaterial("Game2_Avatar_Shirt", ShirtColor, bodyAlpha);
        hairMaterial = CreateMaterial("Game2_Avatar_Hair", HairColor, bodyAlpha);
        eyeMaterial = CreateMaterial("Game2_Avatar_Eye", EyeColor, 1f);
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
        }

        return material;
    }
}

