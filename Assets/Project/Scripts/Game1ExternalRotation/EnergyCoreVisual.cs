using System.Collections;
using UnityEngine;

public class Game1_EnergyCoreVisual : MonoBehaviour
{
    [Header("Core")]
    public Transform energyCore;
    public Renderer energyCoreRenderer;
    public Light coreLight;
    public ParticleSystem coreParticles;
    public ParticleSystem successParticles;
    public ParticleSystem invalidParticles;

    [Header("Completion Particles")]
    public ParticleSystem completion25Particles;
    public ParticleSystem completion50Particles;
    public ParticleSystem completion75Particles;
    public ParticleSystem completion100Particles;

    [Header("Leaf Completion Effects")]
    public bool useFallingLeafCompletionEffects = true;
    public Material completion25LeafMaterial;
    public Material completion50LeafMaterial;
    public Material completion75LeafMaterial;
    public Material completion100LeafMaterial;
    public Vector3 leafEmitterOffset = Vector3.zero;
    public Vector3 leafEmitterBoxSize = new Vector3(0.2f, 0.2f, 0.2f);

    [Header("Pillars")]
    public Renderer[] pillarRenderers;
    public Light[] pillarLights;

    [Header("Material Colors")]
    public Color idleColor = new Color(0.5f, 0.84f, 0.76f);
    public Color chargedColor = new Color(0.36f, 0.75f, 1f);
    public Color excellentColor = new Color(1f, 0.85f, 0.45f);
    public Color warningColor = new Color(1f, 0.54f, 0.40f);
    public Color completion25Color = new Color(0.38f, 1.00f, 0.70f);
    public Color completion50Color = new Color(0.32f, 0.95f, 1.00f);
    public Color completion75Color = new Color(1.00f, 0.72f, 0.32f);
    public Color completion100Color = new Color(1.00f, 0.92f, 0.36f);

    [Header("Motion")]
    public float idleFloatAmplitude = 0.05f;
    public float idleFloatSpeed = 1.1f;

    [Header("Android Placement")]
    public bool anchorToHeadOnAndroid = true;
    public Vector3 coreHeadOffset = new Vector3(0f, -0.12f, 3.00f);
    public float androidCoreScaleMultiplier = 0.45f;
    public float pillarSideOffset = 2.05f;
    public float pillarForwardOffset = 3.20f;
    public float pillarCenterBelowHead = 0.22f;
    public float androidPillarScaleMultiplier = 0.55f;

    Vector3 baseScale = Vector3.one;
    Vector3[] basePillarScales;
    Vector3 basePosition;
    Material coreMaterial;
    float currentProgress;
    Transform head;

    void Awake()
    {
        if (energyCore == null) energyCore = transform;
        AutoFindPillars();
        baseScale = energyCore.localScale;
        basePosition = energyCore.localPosition;
        CachePillarScales();

        if (energyCoreRenderer != null)
        {
            coreMaterial = energyCoreRenderer.material;
        }

        EnsureCompletionParticles();
    }

    void Update()
    {
        ApplyAndroidPlacement();

        if (energyCore == null) return;

        Vector3 floatOffset = Vector3.up * (Mathf.Sin(Time.time * idleFloatSpeed) * idleFloatAmplitude);
        energyCore.localPosition = basePosition + floatOffset;
    }

    public void SetChargeProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        float intensity = Mathf.Lerp(1.0f, 3.5f, currentProgress);
        Color color = Color.Lerp(idleColor, chargedColor, currentProgress);

        if (energyCore != null)
        {
            float scale = Mathf.Lerp(0.95f, 1.15f, currentProgress);
            energyCore.localScale = baseScale * scale * GetAndroidScaleMultiplier();
        }

        SetEmission(coreMaterial, color, intensity);

        if (coreLight != null)
        {
            coreLight.color = color;
            coreLight.intensity = intensity;
        }
    }

    public void PlaySuccessEffect(Game1_ActionGrade grade)
    {
        Color color = grade == Game1_ActionGrade.Excellent ? excellentColor : chargedColor;
        float intensity = grade == Game1_ActionGrade.Excellent ? 4.5f : 3.5f;
        SetEmission(coreMaterial, color, intensity);

        if (successParticles != null) successParticles.Play();

        StartCoroutine(PulsePillars(color));
    }

    public void PlayCompletionEffect(float completion, Game1_ActionGrade grade)
    {
        completion = Mathf.Clamp01(completion);

        if (completion >= 0.95f)
        {
            PlayCompletionTier(completion100Particles, completion100Color, 4.8f, completion100LeafMaterial);
        }
        else if (completion >= 0.75f)
        {
            PlayCompletionTier(completion75Particles, completion75Color, 4.0f, completion75LeafMaterial);
        }
        else if (completion >= 0.50f)
        {
            PlayCompletionTier(completion50Particles, completion50Color, 3.2f, completion50LeafMaterial);
        }
        else if (completion >= 0.25f)
        {
            PlayCompletionTier(completion25Particles, completion25Color, 2.5f, completion25LeafMaterial);
        }
        else
        {
            PlayInvalidEffect(string.Empty);
            return;
        }

        Color pulseColor = grade == Game1_ActionGrade.Excellent ? completion100Color : chargedColor;
        StartCoroutine(PulsePillars(pulseColor));
    }

    public void PlayInvalidEffect(string reason)
    {
        SetEmission(coreMaterial, warningColor, 1.8f);
        if (invalidParticles != null) invalidParticles.Play();
    }

    public void ResetCore()
    {
        currentProgress = 0f;
        SetChargeProgress(0f);
    }

    IEnumerator PulsePillars(Color color)
    {
        AutoFindPillars();
        if (pillarRenderers == null || pillarRenderers.Length == 0)
        {
            yield break;
        }

        for (int i = 0; i < pillarRenderers.Length; i++)
        {
            if (pillarRenderers[i] != null)
            {
                SetEmission(pillarRenderers[i].material, color, 3.5f);
            }

            if (i < pillarLights.Length && pillarLights[i] != null)
            {
                pillarLights[i].color = color;
                pillarLights[i].intensity = 2.5f;
            }

            yield return new WaitForSeconds(0.08f);
        }

        yield return new WaitForSeconds(0.25f);

        for (int i = 0; i < pillarRenderers.Length; i++)
        {
            if (pillarRenderers[i] != null)
            {
                SetEmission(pillarRenderers[i].material, chargedColor, 1.2f);
            }

            if (i < pillarLights.Length && pillarLights[i] != null)
            {
                pillarLights[i].intensity = 0.8f;
            }
        }

        SetChargeProgress(currentProgress);
    }

    void SetEmission(Material material, Color color, float intensity)
    {
        if (material == null) return;

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * intensity);
        if (material.HasProperty("_Color"))
        {
            material.color = color;
        }
    }

    void ApplyAndroidPlacement()
    {
        if (!anchorToHeadOnAndroid || Application.platform != RuntimePlatform.Android) return;

        if (head == null && Camera.main != null) head = Camera.main.transform;
        if (head == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        if (energyCore != null)
        {
            energyCore.position = head.position +
                                  right * coreHeadOffset.x +
                                  Vector3.up * coreHeadOffset.y +
                                  forward * coreHeadOffset.z;
            energyCore.localScale = baseScale * Mathf.Lerp(0.95f, 1.15f, currentProgress) * androidCoreScaleMultiplier;
            basePosition = energyCore.localPosition;
        }

        AutoFindPillars();
        if (pillarRenderers == null || pillarRenderers.Length == 0) return;
        CachePillarScales();

        for (int i = 0; i < pillarRenderers.Length; i++)
        {
            if (pillarRenderers[i] == null) continue;

            Transform pillar = pillarRenderers[i].transform;
            string lowerName = pillar.name.ToLowerInvariant();
            float side = lowerName.Contains("_l") || lowerName.EndsWith("l") ? -1f : 1f;
            float forwardOffset = lowerName.Contains("_b") || lowerName.EndsWith("b")
                ? pillarForwardOffset - 0.70f
                : pillarForwardOffset;

            Vector3 targetPosition = head.position +
                                     forward * forwardOffset +
                                     right * pillarSideOffset * side -
                                     Vector3.up * pillarCenterBelowHead;

            if (basePillarScales != null && i < basePillarScales.Length)
            {
                pillar.localScale = basePillarScales[i] * androidPillarScaleMultiplier;
            }

            pillar.position = targetPosition;
            Bounds bounds = pillarRenderers[i].bounds;
            targetPosition.y += 0.03f - bounds.min.y;
            pillar.position = targetPosition;
        }
    }

    void AutoFindPillars()
    {
        if (pillarRenderers != null && pillarRenderers.Length > 0) return;

        Renderer[] renderers = FindObjectsOfType<Renderer>(true);
        System.Collections.Generic.List<Renderer> foundRenderers = new System.Collections.Generic.List<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (renderer.name.IndexOf("Pillar", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                foundRenderers.Add(renderer);
            }
        }

        foundRenderers.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        pillarRenderers = foundRenderers.ToArray();

        Light[] lights = FindObjectsOfType<Light>(true);
        System.Collections.Generic.List<Light> foundLights = new System.Collections.Generic.List<Light>();
        foreach (Light light in lights)
        {
            if (light == null) continue;
            if (light.name.IndexOf("Pillar", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                foundLights.Add(light);
            }
        }

        foundLights.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        pillarLights = foundLights.ToArray();
    }

    void CachePillarScales()
    {
        if (pillarRenderers == null) return;
        if (basePillarScales != null && basePillarScales.Length == pillarRenderers.Length) return;

        basePillarScales = new Vector3[pillarRenderers.Length];
        for (int i = 0; i < pillarRenderers.Length; i++)
        {
            basePillarScales[i] = pillarRenderers[i] != null ? pillarRenderers[i].transform.localScale : Vector3.one;
        }
    }

    float GetAndroidScaleMultiplier()
    {
        return Application.platform == RuntimePlatform.Android && anchorToHeadOnAndroid
            ? androidCoreScaleMultiplier
            : 1f;
    }

    void PlayCompletionTier(ParticleSystem particles, Color color, float intensity, Material leafMaterial)
    {
        SetEmission(coreMaterial, color, intensity);
        ConfigureCompletionParticles(particles, color, 0, 0f, leafMaterial);

        if (coreLight != null)
        {
            coreLight.color = color;
            coreLight.intensity = intensity;
        }

        if (particles != null)
        {
            particles.Play();
        }
    }

    void EnsureCompletionParticles()
    {
        if (completion25Particles == null) completion25Particles = CreateCompletionParticles("Completion_25_Leaves", completion25Color, 16, 0.24f, completion25LeafMaterial);
        if (completion50Particles == null) completion50Particles = CreateCompletionParticles("Completion_50_Leaves", completion50Color, 24, 0.28f, completion50LeafMaterial);
        if (completion75Particles == null) completion75Particles = CreateCompletionParticles("Completion_75_Leaves", completion75Color, 34, 0.32f, completion75LeafMaterial);
        if (completion100Particles == null) completion100Particles = CreateCompletionParticles("Completion_100_Leaves", completion100Color, 48, 0.38f, completion100LeafMaterial);

        ConfigureCompletionParticles(completion25Particles, completion25Color, 16, 0.24f, completion25LeafMaterial);
        ConfigureCompletionParticles(completion50Particles, completion50Color, 24, 0.28f, completion50LeafMaterial);
        ConfigureCompletionParticles(completion75Particles, completion75Color, 34, 0.32f, completion75LeafMaterial);
        ConfigureCompletionParticles(completion100Particles, completion100Color, 48, 0.38f, completion100LeafMaterial);
    }

    ParticleSystem CreateCompletionParticles(string objectName, Color color, int burstCount, float startSize, Material leafMaterial)
    {
        if (energyCore == null) return null;

        GameObject particleObject = new GameObject(objectName);
        particleObject.transform.SetParent(energyCore, false);
        particleObject.transform.localPosition = useFallingLeafCompletionEffects ? leafEmitterOffset : Vector3.zero;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ConfigureCompletionParticles(particles, color, burstCount, startSize, leafMaterial);

        return particles;
    }

    void ConfigureCompletionParticles(ParticleSystem particles, Color color, int burstCount, float startSize, Material leafMaterial)
    {
        if (particles == null) return;

        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particles.transform.localPosition = useFallingLeafCompletionEffects ? leafEmitterOffset : Vector3.zero;

        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startColor = useFallingLeafCompletionEffects ? Color.white : color;
        if (startSize > 0f) main.startSize = startSize;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        if (burstCount > 0)
        {
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)burstCount)
            });
        }

        if (useFallingLeafCompletionEffects)
        {
            ConfigureLeafBurstParticles(particles, color, startSize, leafMaterial);
            return;
        }

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.material = CreateParticleMaterial(particles.name + "_Material", color);
        }
    }

    void ConfigureLeafBurstParticles(ParticleSystem particles, Color color, float startSize, Material leafMaterial)
    {
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.55f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.10f, 1.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.75f, 1.65f);
        main.startSize = startSize > 0f
            ? new ParticleSystem.MinMaxCurve(startSize * 0.80f, startSize * 1.35f)
            : main.startSize;
        main.startColor = Color.white;
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.gravityModifier = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.12f;
        shape.radiusThickness = 0.25f;
        shape.randomDirectionAmount = 0.75f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.36f, 0.36f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.24f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.36f, 0.36f);

        ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-2.8f, 2.8f);

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        noise.frequency = 0.65f;
        noise.scrollSpeed = 0.45f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 0.55f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.96f, 0.12f),
                new GradientAlphaKey(0.88f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.3f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0.65f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        if (particleRenderer != null)
        {
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.material = leafMaterial != null ? leafMaterial : CreateParticleMaterial(particles.name + "_LeafMaterial", color);
            particleRenderer.sortingFudge = 2f;
        }
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
        if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", color);
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 1.5f);

        material.renderQueue = 3000;
        return material;
    }
}
