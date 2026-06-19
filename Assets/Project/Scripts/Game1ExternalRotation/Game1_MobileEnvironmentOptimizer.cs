using UnityEngine;
using UnityEngine.Rendering;

public class Game1_MobileEnvironmentOptimizer : MonoBehaviour
{
    [Header("Runtime")]
    public bool applyOnStart = true;
    public bool androidOnly = true;
    public bool createMobileEnvironment = true;
    public bool hideHeavyImportedEnvironment = true;
    public int mobileQualityLevel = 2;
    public int targetFrameRate = 72;

    [Header("Mobile Island")]
    public float islandRadius = 4.4f;
    public float oceanSize = 44f;
    public float oceanHeight = -0.08f;
    public Color grassColor = new Color(0.34f, 0.70f, 0.30f, 1f);
    public Color sandColor = new Color(0.78f, 0.66f, 0.38f, 1f);
    public Color oceanColor = new Color(0.07f, 0.43f, 0.64f, 1f);

    Material grassMaterial;
    Material sandMaterial;
    Material oceanMaterial;
    Transform mobileRoot;

    void Start()
    {
        if (applyOnStart)
        {
            Apply();
        }
    }

    public void Apply()
    {
        if (!ShouldApply()) return;

        ApplyMobileQuality();

        if (hideHeavyImportedEnvironment)
        {
            HideHeavyObjects();
        }

        if (createMobileEnvironment)
        {
            EnsureMobileEnvironment();
        }
    }

    bool ShouldApply()
    {
        return !androidOnly || Application.platform == RuntimePlatform.Android;
    }

    void ApplyMobileQuality()
    {
        int qualityLevel = Mathf.Clamp(mobileQualityLevel, 0, QualitySettings.names.Length - 1);
        QualitySettings.SetQualityLevel(qualityLevel, true);
        QualitySettings.pixelLightCount = 0;
        QualitySettings.shadows = ShadowQuality.Disable;
        QualitySettings.shadowDistance = 0f;
        QualitySettings.antiAliasing = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
        QualitySettings.softParticles = false;
        QualitySettings.realtimeReflectionProbes = false;
        Application.targetFrameRate = targetFrameRate;
    }

    void HideHeavyObjects()
    {
        DisableObjectByName("Ocean");
        DisableObjectByName("Terrain");
        DisableObjectByName("Post-process Volume ");
        DisableObjectByName("Post-process Volume");
        DisableObjectByName("Water Reflection Probe");

        Terrain[] terrains = FindObjectsOfType<Terrain>(true);
        foreach (Terrain terrain in terrains)
        {
            terrain.gameObject.SetActive(false);
        }

        ReflectionProbe[] probes = FindObjectsOfType<ReflectionProbe>(true);
        foreach (ReflectionProbe probe in probes)
        {
            probe.gameObject.SetActive(false);
        }
    }

    void DisableObjectByName(string objectName)
    {
        GameObject target = GameObject.Find(objectName);
        if (target != null)
        {
            target.SetActive(false);
        }
    }

    void EnsureMobileEnvironment()
    {
        CreateMaterials();

        if (mobileRoot == null)
        {
            GameObject existing = GameObject.Find("Game1_MobileEnvironment");
            mobileRoot = existing != null ? existing.transform : new GameObject("Game1_MobileEnvironment").transform;
        }

        Transform ocean = EnsurePrimitive("Mobile_Ocean", PrimitiveType.Plane);
        ocean.localPosition = new Vector3(0f, oceanHeight, 0f);
        ocean.localRotation = Quaternion.identity;
        ocean.localScale = Vector3.one * (oceanSize / 10f);
        AssignMaterial(ocean, oceanMaterial);

        Transform sand = EnsurePrimitive("Mobile_Sand_Edge", PrimitiveType.Cylinder);
        sand.localPosition = new Vector3(0f, 0.015f, 0f);
        sand.localRotation = Quaternion.identity;
        sand.localScale = new Vector3(islandRadius * 2.25f, 0.05f, islandRadius * 2.25f);
        AssignMaterial(sand, sandMaterial);

        Transform grass = EnsurePrimitive("Mobile_Grass", PrimitiveType.Cylinder);
        grass.localPosition = new Vector3(0f, 0.075f, 0f);
        grass.localRotation = Quaternion.identity;
        grass.localScale = new Vector3(islandRadius * 1.88f, 0.055f, islandRadius * 1.88f);
        AssignMaterial(grass, grassMaterial);
    }

    Transform EnsurePrimitive(string objectName, PrimitiveType primitiveType)
    {
        Transform child = FindChild(mobileRoot, objectName);
        if (child == null)
        {
            child = GameObject.CreatePrimitive(primitiveType).transform;
            child.name = objectName;
            child.SetParent(mobileRoot, false);
            DestroyCollider(child);
        }

        SetRendererMobileFlags(child);
        return child;
    }

    void CreateMaterials()
    {
        if (grassMaterial == null) grassMaterial = CreateMobileMaterial("Game1_Mobile_Grass", grassColor);
        if (sandMaterial == null) sandMaterial = CreateMobileMaterial("Game1_Mobile_Sand", sandColor);
        if (oceanMaterial == null) oceanMaterial = CreateMobileMaterial("Game1_Mobile_Ocean", oceanColor);
    }

    Material CreateMobileMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material material = new Material(shader);
        material.name = materialName;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;
        return material;
    }

    void AssignMaterial(Transform target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null) return;

        renderer.sharedMaterial = material;
        SetRendererMobileFlags(target);
    }

    void SetRendererMobileFlags(Transform target)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null) return;

        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    Transform FindChild(Transform root, string objectName)
    {
        if (root == null) return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == objectName) return child;
        }

        return null;
    }

    void DestroyCollider(Transform target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider == null) return;

        if (Application.isPlaying) Destroy(collider);
        else DestroyImmediate(collider);
    }
}
