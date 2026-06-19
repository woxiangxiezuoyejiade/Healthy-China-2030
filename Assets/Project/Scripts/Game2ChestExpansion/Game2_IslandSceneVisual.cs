using UnityEngine;
using UnityEngine.Rendering;

public class Game2_IslandSceneVisual : MonoBehaviour
{
    [Header("Scene Roots")]
    public Transform environmentRoot;
    public Transform ground;
    public Transform sunCore;
    public Transform fogRoot;
    public Transform godRaysRoot;
    public Transform cloudsRoot;

    [Header("Auto Setup")]
    public bool applyOnStart = true;
    public bool hideOriginalGround = true;
    public bool createIsland = true;
    public bool createOcean = true;
    public bool createSunHalo = true;
    public bool createMistParticles = true;

    [Header("Island")]
    public float islandRadius = 4.2f;
    public float oceanSize = 42f;
    public Color grassColor = new Color(0.30f, 0.62f, 0.30f);
    public Color islandEdgeColor = new Color(0.72f, 0.62f, 0.38f);
    public Color oceanColor = new Color(0.07f, 0.42f, 0.62f, 0.88f);

    [Header("Sun")]
    public Color sunCoreColor = new Color(1f, 0.78f, 0.23f);
    public Color sunHaloColor = new Color(1f, 0.92f, 0.45f, 0.24f);

    Material grassMaterial;
    Material sandMaterial;
    Material oceanMaterial;
    Material sunMaterial;
    Material haloMaterial;
    Material mistMaterial;

    void Start()
    {
        if (applyOnStart)
        {
            ApplySceneStyle();
        }
    }

    public void ApplySceneStyle()
    {
        ResolveReferences();
        CreateMaterials();

        if (hideOriginalGround && ground != null)
        {
            ground.gameObject.SetActive(false);
        }

        if (createOcean) EnsureOcean();
        if (createIsland) EnsureIsland();
        if (createSunHalo) EnsureSunHalo();
        if (createMistParticles) EnsureMistParticles();
        StyleImportedEffects();
    }

    void ResolveReferences()
    {
        if (environmentRoot == null)
        {
            GameObject environmentObject = GameObject.Find("Game2_Environment");
            environmentRoot = environmentObject != null ? environmentObject.transform : transform;
        }

        if (ground == null) ground = FindChildByName(environmentRoot, "Ground");
        if (sunCore == null) sunCore = FindChildByName(environmentRoot, "SunCore");
        if (fogRoot == null) fogRoot = FindChildByName(environmentRoot, "FogArea");
        if (godRaysRoot == null && sunCore != null) godRaysRoot = FindChildByName(sunCore, "GodRays");
        if (cloudsRoot == null && fogRoot != null) cloudsRoot = FindChildByName(fogRoot, "Clouds");
    }

    void CreateMaterials()
    {
        grassMaterial = CreateLitMaterial("Game2_Runtime_Grass", grassColor, false);
        sandMaterial = CreateLitMaterial("Game2_Runtime_Sand", islandEdgeColor, false);
        oceanMaterial = CreateLitMaterial("Game2_Runtime_Ocean", oceanColor, true);
        sunMaterial = CreateLitMaterial("Game2_Runtime_Sun", sunCoreColor, false);
        haloMaterial = CreateLitMaterial("Game2_Runtime_SunHalo", sunHaloColor, true);
        mistMaterial = CreateParticleMaterial("Game2_Runtime_Mist", new Color(0.78f, 0.90f, 1f, 0.28f));
    }

    void EnsureOcean()
    {
        Transform ocean = FindChildByName(environmentRoot, "Ocean_Plane");
        if (ocean == null)
        {
            ocean = GameObject.CreatePrimitive(PrimitiveType.Plane).transform;
            ocean.name = "Ocean_Plane";
            ocean.SetParent(environmentRoot, false);
            DestroyCollider(ocean);
        }

        ocean.localPosition = new Vector3(0f, -0.05f, 0f);
        ocean.localRotation = Quaternion.identity;
        ocean.localScale = Vector3.one * (oceanSize / 10f);
        AssignMaterial(ocean, oceanMaterial);
    }

    void EnsureIsland()
    {
        Transform sand = FindChildByName(environmentRoot, "Island_Sand_Edge");
        if (sand == null)
        {
            sand = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            sand.name = "Island_Sand_Edge";
            sand.SetParent(environmentRoot, false);
            DestroyCollider(sand);
        }

        sand.localPosition = new Vector3(0f, 0.01f, 0f);
        sand.localRotation = Quaternion.identity;
        sand.localScale = new Vector3(islandRadius * 2.25f, 0.055f, islandRadius * 2.25f);
        AssignMaterial(sand, sandMaterial);

        Transform grass = FindChildByName(environmentRoot, "Island_Grass");
        if (grass == null)
        {
            grass = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            grass.name = "Island_Grass";
            grass.SetParent(environmentRoot, false);
            DestroyCollider(grass);
        }

        grass.localPosition = new Vector3(0f, 0.08f, 0f);
        grass.localRotation = Quaternion.identity;
        grass.localScale = new Vector3(islandRadius * 1.85f, 0.05f, islandRadius * 1.85f);
        AssignMaterial(grass, grassMaterial);
    }

    void EnsureSunHalo()
    {
        if (sunCore == null) return;

        sunCore.localPosition = new Vector3(0f, 2.25f, 3.2f);
        sunCore.localScale = Vector3.one * 0.45f;
        AssignMaterial(sunCore, sunMaterial);

        Transform halo = FindChildByName(sunCore, "SunHalo");
        if (halo == null)
        {
            halo = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            halo.name = "SunHalo";
            halo.SetParent(sunCore, false);
            DestroyCollider(halo);
        }

        halo.localPosition = Vector3.zero;
        halo.localRotation = Quaternion.identity;
        halo.localScale = Vector3.one * 1.9f;
        AssignMaterial(halo, haloMaterial);

        Game2_SunEnergyVisual sunVisual = GetComponent<Game2_SunEnergyVisual>();
        if (sunVisual != null)
        {
            Renderer sunRenderer = sunCore.GetComponent<Renderer>();
            Renderer haloRenderer = halo.GetComponent<Renderer>();
            Light sunLight = sunCore.GetComponentInChildren<Light>(true);

            sunVisual.sunCore = sunCore;
            sunVisual.sunRenderer = sunRenderer;
            sunVisual.sunLight = sunLight;
            sunVisual.haloRenderer = haloRenderer;
            sunVisual.RefreshReferences();
        }
    }

    void EnsureMistParticles()
    {
        if (fogRoot == null) return;

        Transform oldMist = FindChildByName(fogRoot, "SoftMist_Particles");
        if (oldMist != null)
        {
            DestroySafe(oldMist.gameObject);
        }

        Game2_FogVisual fogVisual = GetComponent<Game2_FogVisual>();
        if (fogVisual != null)
        {
            fogVisual.fogRoot = fogRoot;
        }
    }

    void ConfigureMistParticles(ParticleSystem particles, ParticleSystemRenderer renderer)
    {
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4.5f, 7.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.16f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.1f, 2.6f);
        main.startColor = new Color(0.80f, 0.92f, 1f, 0.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 180;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 28f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(5.8f, 1.5f, 2.6f);

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.00f, 0.05f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.04f, 0.04f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.80f, 0.92f, 1f), 0f),
                new GradientColorKey(new Color(0.95f, 0.98f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.28f, 0.2f),
                new GradientAlphaKey(0.18f, 0.7f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 1;
        renderer.material = mistMaterial;

        if (!particles.isPlaying)
        {
            particles.Play();
        }
    }

    void StyleImportedEffects()
    {
        if (godRaysRoot != null)
        {
            godRaysRoot.localPosition = new Vector3(0f, -0.05f, 0.25f);
            godRaysRoot.localRotation = Quaternion.Euler(0f, 180f, 0f);
            godRaysRoot.localScale = Vector3.one * 0.28f;
        }

        if (cloudsRoot != null)
        {
            cloudsRoot.localPosition = new Vector3(0f, 1.1f, 1.6f);
            cloudsRoot.localRotation = Quaternion.identity;
            cloudsRoot.localScale = Vector3.one * 0.55f;
        }
    }

    Material CreateLitMaterial(string materialName, Color color, bool transparent)
    {
        Shader shader = FindSceneShader(false);

        Material material = new Material(shader);
        material.name = materialName;
        SetMaterialColor(material, color);

        if (transparent)
        {
            ConfigureTransparentMaterial(material);
        }

        if (materialName.Contains("Sun"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.4f);
        }

        return material;
    }

    Material CreateParticleMaterial(string materialName, Color color)
    {
        Shader shader = FindSceneShader(true);
        Material material = new Material(shader);
        material.name = materialName;
        SetMaterialColor(material, color);
        ConfigureTransparentMaterial(material);
        return material;
    }

    Shader FindSceneShader(bool particle)
    {
        bool usingScriptablePipeline = GraphicsSettings.currentRenderPipeline != null;

        if (particle)
        {
            Shader particleShader = usingScriptablePipeline
                ? Shader.Find("Universal Render Pipeline/Particles/Unlit")
                : Shader.Find("Particles/Standard Unlit");

            if (particleShader == null) particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (particleShader != null) return particleShader;
        }

        Shader shader = usingScriptablePipeline
            ? Shader.Find("Universal Render Pipeline/Lit")
            : Shader.Find("Standard");

        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Diffuse");

        return shader;
    }

    void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.color = color;
    }

    void ConfigureTransparentMaterial(Material material)
    {
        if (material == null) return;

        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Mode")) material.SetFloat("_Mode", 3f);

        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    void AssignMaterial(Transform target, Material material)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
    }

    Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    void DestroyCollider(Transform target)
    {
        Collider collider = target.GetComponent<Collider>();
        if (collider == null) return;

        DestroySafe(collider);
    }

    void DestroySafe(Object target)
    {
        if (target == null) return;

        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
