using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Game2_SunEnergyVisual : MonoBehaviour
{
    [Header("Sun Core")]
    public Transform sunCore;
    public Renderer sunRenderer;
    public Light sunLight;
    public ParticleSystem successParticles;
    public Renderer haloRenderer;
    public ParticleSystem excellentSunburstParticles;
    public ParticleSystem goodRayParticles;
    public ParticleSystem improvementMistParticles;
    public ParticleSystem invalidFlashParticles;

    [Header("Colors")]
    public Color idleColor = new Color(1f, 0.78f, 0.32f);
    public Color activeColor = new Color(1f, 0.92f, 0.45f);
    public Color excellentColor = new Color(1f, 1f, 0.72f);
    public Color invalidColor = new Color(1f, 0.42f, 0.30f);
    public Color goodEffectColor = new Color(0.34f, 0.94f, 1f);
    public Color improvementEffectColor = new Color(0.68f, 1f, 0.78f);
    public Color[] ribbonColors =
    {
        new Color(1f, 0.86f, 0.18f, 0.92f),
        new Color(0.12f, 0.82f, 1f, 0.88f),
        new Color(1f, 0.36f, 0.62f, 0.90f),
        new Color(0.42f, 1f, 0.54f, 0.86f)
    };

    [Header("Intensity")]
    public float idleEmission = 0.8f;
    public float maxEmission = 4.5f;
    public float idleLightIntensity = 0.8f;
    public float maxLightIntensity = 1.6f;
    [Tooltip("Keep off for VR/Pico: driving the scene light every frame can tint the whole scene.")]
    public bool driveSceneLight = false;
    public bool pulseSunCoreOnActionResult = false;
    public bool pulseSceneLightOnActionResult = false;
    [Tooltip("Keep off on Pico/Android: billboard particles can render as dark quads on some mobile pipelines. Confetti ribbons still play.")]
    public bool useBillboardResultParticles = false;
    public bool capConfettiOnAndroid = true;
    public int maxConfettiCountOnAndroid = 28;
    public float androidConfettiSpawnWindowMultiplier = 0.90f;

    Material sunMaterial;
    Material haloMaterial;
    Material[] ribbonMaterials;
    Vector3 baseScale = Vector3.one;
    Vector3 haloBaseScale = Vector3.one;
    float storedEnergy;
    float actionProgress;
    Coroutine pulseRoutine;

    void Awake()
    {
        RefreshReferences();
        EnsureResultEffects();
        UpdateVisual();
    }

    public void RefreshReferences()
    {
        if (sunCore == null) sunCore = transform;
        if (sunRenderer == null) sunRenderer = GetComponent<Renderer>();
        if (sunCore != null) baseScale = sunCore.localScale;
        if (sunRenderer != null) sunMaterial = sunRenderer.material;
        if (haloRenderer != null)
        {
            haloMaterial = haloRenderer.material;
            haloBaseScale = haloRenderer.transform.localScale;
        }
    }

    public void SetStoredEnergy(float value)
    {
        storedEnergy = Mathf.Clamp01(value);
        UpdateVisual();
    }

    public void SetActionProgress(float value)
    {
        actionProgress = Mathf.Clamp01(value);
        UpdateVisual();
    }

    public void PlayActionResult(Game2_ActionGrade grade)
    {
        int approximateScore = grade == Game2_ActionGrade.Excellent ? 100 :
            grade == Game2_ActionGrade.Good ? 75 :
            grade == Game2_ActionGrade.NeedsImprovement ? 35 : 0;
        PlayActionResult(grade, approximateScore);
    }

    public void PlayActionResult(Game2_ActionGrade grade, int actionScore)
    {
        PlayResultEffect(grade, actionScore);
        if (pulseSunCoreOnActionResult || pulseSceneLightOnActionResult)
        {
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(Pulse(grade));
        }
    }

    void UpdateVisual()
    {
        float level = Mathf.Clamp01(storedEnergy * 0.75f + actionProgress * 0.25f);
        Color color = Color.Lerp(idleColor, activeColor, level);
        float emission = Mathf.Lerp(idleEmission, maxEmission, level);

        SetEmission(color, emission);

        if (driveSceneLight && sunLight != null)
        {
            sunLight.color = color;
            sunLight.intensity = Mathf.Lerp(idleLightIntensity, maxLightIntensity, level);
        }

        if (sunCore != null)
        {
            float scale = Mathf.Lerp(0.85f, 1.25f, level);
            sunCore.localScale = baseScale * scale;
        }

        if (haloMaterial != null)
        {
            Color haloColor = color;
            haloColor.a = Mathf.Lerp(0.18f, 0.48f, level);
            SetMaterialColor(haloMaterial, haloColor);
            SetEmission(haloMaterial, color, Mathf.Lerp(0.8f, 2.5f, level));
            haloRenderer.transform.localScale = haloBaseScale * Mathf.Lerp(0.95f, 1.35f, level);
        }
    }

    IEnumerator Pulse(Game2_ActionGrade grade)
    {
        Color color = grade == Game2_ActionGrade.Excellent ? excellentColor : activeColor;
        if (grade == Game2_ActionGrade.Invalid) color = invalidColor;

        SetEmission(color, grade == Game2_ActionGrade.Invalid ? 1.5f : maxEmission + 1f);
        if (pulseSceneLightOnActionResult && sunLight != null)
        {
            sunLight.color = color;
            sunLight.intensity = grade == Game2_ActionGrade.Invalid ? 1.2f : maxLightIntensity + 1f;
        }
        yield return new WaitForSeconds(0.18f);
        UpdateVisual();
        pulseRoutine = null;
    }

    void EnsureResultEffects()
    {
        if (sunCore == null) sunCore = transform;

        if (excellentSunburstParticles == null)
        {
            excellentSunburstParticles = CreateResultParticles(
                "Excellent_Sunburst_Effect",
                excellentColor,
                34,
                0.72f,
                1.2f,
                ParticleSystemShapeType.Circle,
                0.42f);
        }

        if (goodRayParticles == null)
        {
            goodRayParticles = CreateResultParticles(
                "Good_Cyan_Ray_Effect",
                goodEffectColor,
                22,
                0.50f,
                0.95f,
                ParticleSystemShapeType.Cone,
                0.28f);
        }

        if (improvementMistParticles == null)
        {
            improvementMistParticles = CreateResultParticles(
                "Improvement_Mist_Effect",
                improvementEffectColor,
                16,
                0.38f,
                1.45f,
                ParticleSystemShapeType.Sphere,
                0.32f);
        }

        if (invalidFlashParticles == null)
        {
            invalidFlashParticles = CreateResultParticles(
                "Invalid_Orange_Flash_Effect",
                invalidColor,
                12,
                0.30f,
                0.55f,
                ParticleSystemShapeType.Hemisphere,
                0.24f);
        }
    }

    void PlayResultEffect(Game2_ActionGrade grade, int actionScore)
    {
        EnsureResultEffects();

        if (grade == Game2_ActionGrade.Invalid || actionScore < 25)
        {
            if (useBillboardResultParticles)
            {
                PlayParticles(invalidFlashParticles);
            }
            else
            {
                StartCoroutine(ConfettiRain(6, 0.42f, 0.55f));
            }
            return;
        }

        if (actionScore >= 95)
        {
            StartCoroutine(ConfettiRain(58, 1.42f, 1.35f));
            return;
        }

        if (actionScore >= 75)
        {
            StartCoroutine(ConfettiRain(38, 1.10f, 1.15f));
            return;
        }

        if (actionScore >= 50)
        {
            StartCoroutine(ConfettiRain(22, 0.82f, 0.95f));
            return;
        }

        StartCoroutine(ConfettiRain(10, 0.50f, 0.75f));
    }

    IEnumerator ConfettiRain(int count, float width, float duration)
    {
        EnsureRibbonMaterials();
        count = GetSafeConfettiCount(count);

        Transform reference = Camera.main != null ? Camera.main.transform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        Vector3 center = reference.position + forward * 1.35f + Vector3.up * 0.95f;
        float spawnWindow = IsAndroidDevice()
            ? Mathf.Min(duration * androidConfettiSpawnWindowMultiplier, duration)
            : Mathf.Min(0.48f, duration * 0.45f);

        for (int i = 0; i < count; i++)
        {
            Vector3 position =
                center +
                right * Random.Range(-width, width) +
                forward * Random.Range(-0.18f, 0.18f) +
                Vector3.up * Random.Range(0f, 0.22f);

            CreateRibbon(position, reference.rotation, Random.Range(1.4f, 2.2f));

            if (spawnWindow > 0.01f)
            {
                yield return new WaitForSeconds(spawnWindow / count);
            }
        }
    }

    void CreateRibbon(Vector3 position, Quaternion viewRotation, float lifetime)
    {
        GameObject ribbon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ribbon.name = "Game2_Falling_Ribbon";
        ribbon.transform.position = position;
        ribbon.transform.rotation = viewRotation * Quaternion.Euler(
            Random.Range(-80f, 80f),
            Random.Range(0f, 360f),
            Random.Range(-35f, 35f));
        ribbon.transform.localScale = new Vector3(
            Random.Range(0.014f, 0.022f),
            Random.Range(0.075f, 0.13f),
            0.004f);

        Renderer renderer = ribbon.GetComponent<Renderer>();
        if (renderer != null && ribbonMaterials != null && ribbonMaterials.Length > 0)
        {
            renderer.material = ribbonMaterials[Random.Range(0, ribbonMaterials.Length)];
        }

        Collider collider = ribbon.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        StartCoroutine(AnimateRibbon(ribbon.transform, lifetime));
    }

    IEnumerator AnimateRibbon(Transform ribbon, float lifetime)
    {
        if (ribbon == null) yield break;

        Vector3 fallVelocity = new Vector3(
            Random.Range(-0.10f, 0.10f),
            -Random.Range(0.48f, 0.72f),
            Random.Range(-0.08f, 0.08f));
        Vector3 spin = new Vector3(
            Random.Range(120f, 260f),
            Random.Range(90f, 230f),
            Random.Range(160f, 320f));

        float elapsed = 0f;
        while (elapsed < lifetime && ribbon != null)
        {
            float sway = Mathf.Sin((elapsed * 7f) + ribbon.position.x * 3f) * 0.12f;
            ribbon.position += (fallVelocity + Vector3.right * sway * 0.2f) * Time.deltaTime;
            ribbon.Rotate(spin * Time.deltaTime, Space.Self);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (ribbon != null)
        {
            Destroy(ribbon.gameObject);
        }
    }

    void EnsureRibbonMaterials()
    {
        if (ribbonColors == null || ribbonColors.Length == 0)
        {
            ribbonColors = new[]
            {
                new Color(1f, 0.86f, 0.18f, 0.92f),
                new Color(0.12f, 0.82f, 1f, 0.88f),
                new Color(1f, 0.36f, 0.62f, 0.90f),
                new Color(0.42f, 1f, 0.54f, 0.86f)
            };
        }

        if (ribbonMaterials != null && ribbonMaterials.Length == ribbonColors.Length) return;

        ribbonMaterials = new Material[ribbonColors.Length];
        for (int i = 0; i < ribbonColors.Length; i++)
        {
            ribbonMaterials[i] = CreateRibbonMaterial("Game2_Ribbon_Material_" + i, ribbonColors[i]);
        }
    }

    int GetSafeConfettiCount(int count)
    {
        if (capConfettiOnAndroid && IsAndroidDevice())
        {
            return Mathf.Min(count, Mathf.Max(6, maxConfettiCountOnAndroid));
        }

        return count;
    }

    bool IsAndroidDevice()
    {
        return Application.platform == RuntimePlatform.Android;
    }

    void PlayParticles(ParticleSystem particles)
    {
        if (particles == null) return;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.Play(true);
    }

    ParticleSystem CreateResultParticles(
        string objectName,
        Color color,
        short burstCount,
        float startSize,
        float startSpeed,
        ParticleSystemShapeType shapeType,
        float shapeRadius)
    {
        GameObject effectObject = new GameObject(objectName);
        Transform parent = sunCore != null ? sunCore : transform;
        effectObject.transform.SetParent(parent, false);
        effectObject.transform.localPosition = Vector3.zero;
        effectObject.transform.localRotation = Quaternion.identity;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.9f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(startSpeed * 0.65f, startSpeed * 1.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.55f, startSize);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, burstCount)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = shapeType;
        shape.radius = shapeRadius;
        shape.angle = 28f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        Color transparent = color;
        transparent.a = 0f;
        Color visible = color;
        visible.a = 0.85f;
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(visible, 0f),
                new GradientColorKey(color, 0.55f),
                new GradientColorKey(transparent, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.82f, 0.14f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.35f, 1f),
            new Keyframe(1f, 0.15f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = CreateParticleMaterial(objectName + "_Material", color);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortingOrder = 5;

        return particles;
    }

    Material CreateParticleMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = materialName;

        if (material.HasProperty("_Color")) material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);

        material.SetOverrideTag("RenderType", "Transparent");
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)RenderQueue.Transparent;

        return material;
    }

    Material CreateRibbonMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = materialName;

        Color opaqueColor = color;
        opaqueColor.a = 1f;
        if (material.HasProperty("_Color")) material.color = opaqueColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", opaqueColor);
        material.renderQueue = (int)RenderQueue.Geometry;

        return material;
    }

    void SetEmission(Color color, float intensity)
    {
        if (sunMaterial == null) return;

        SetEmission(sunMaterial, color, intensity);
    }

    void SetEmission(Material material, Color color, float intensity)
    {
        if (material == null) return;

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * intensity);
        SetMaterialColor(material, color);
    }

    void SetMaterialColor(Material material, Color color)
    {
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Color"))
        {
            material.color = color;
        }
    }
}
