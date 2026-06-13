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

    [Header("Pillars")]
    public Renderer[] pillarRenderers;
    public Light[] pillarLights;

    [Header("Material Colors")]
    public Color idleColor = new Color(0.5f, 0.84f, 0.76f);
    public Color chargedColor = new Color(0.36f, 0.75f, 1f);
    public Color excellentColor = new Color(1f, 0.85f, 0.45f);
    public Color warningColor = new Color(1f, 0.54f, 0.40f);
    public Color completion25Color = new Color(0.66f, 0.72f, 1f);
    public Color completion50Color = new Color(0.25f, 0.95f, 0.92f);
    public Color completion75Color = new Color(0.88f, 0.48f, 1f);
    public Color completion100Color = new Color(1f, 0.84f, 0.18f);

    [Header("Motion")]
    public float idleFloatAmplitude = 0.05f;
    public float idleFloatSpeed = 1.1f;

    Vector3 baseScale = Vector3.one;
    Vector3 basePosition;
    Material coreMaterial;
    float currentProgress;

    void Awake()
    {
        if (energyCore == null) energyCore = transform;
        baseScale = energyCore.localScale;
        basePosition = energyCore.localPosition;

        if (energyCoreRenderer != null)
        {
            coreMaterial = energyCoreRenderer.material;
        }

        EnsureCompletionParticles();
    }

    void Update()
    {
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
            energyCore.localScale = baseScale * scale;
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
            PlayCompletionTier(completion100Particles, completion100Color, 4.8f);
        }
        else if (completion >= 0.75f)
        {
            PlayCompletionTier(completion75Particles, completion75Color, 4.0f);
        }
        else if (completion >= 0.50f)
        {
            PlayCompletionTier(completion50Particles, completion50Color, 3.2f);
        }
        else if (completion >= 0.25f)
        {
            PlayCompletionTier(completion25Particles, completion25Color, 2.5f);
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

    void PlayCompletionTier(ParticleSystem particles, Color color, float intensity)
    {
        SetEmission(coreMaterial, color, intensity);
        ConfigureCompletionParticles(particles, color, 0, 0f);

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
        if (completion25Particles == null) completion25Particles = CreateCompletionParticles("Completion_25_Particles", completion25Color, 18, 0.10f);
        if (completion50Particles == null) completion50Particles = CreateCompletionParticles("Completion_50_Particles", completion50Color, 28, 0.14f);
        if (completion75Particles == null) completion75Particles = CreateCompletionParticles("Completion_75_Particles", completion75Color, 40, 0.18f);
        if (completion100Particles == null) completion100Particles = CreateCompletionParticles("Completion_100_Particles", completion100Color, 58, 0.24f);

        ConfigureCompletionParticles(completion25Particles, completion25Color, 18, 0.10f);
        ConfigureCompletionParticles(completion50Particles, completion50Color, 28, 0.14f);
        ConfigureCompletionParticles(completion75Particles, completion75Color, 40, 0.18f);
        ConfigureCompletionParticles(completion100Particles, completion100Color, 58, 0.24f);
    }

    ParticleSystem CreateCompletionParticles(string objectName, Color color, int burstCount, float startSize)
    {
        if (energyCore == null) return null;

        GameObject particleObject = new GameObject(objectName);
        particleObject.transform.SetParent(energyCore, false);
        particleObject.transform.localPosition = Vector3.zero;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSize = startSize;
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, (short)burstCount)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

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
            particleRenderer.material = CreateParticleMaterial(objectName + "_Material", color);
        }

        return particles;
    }

    void ConfigureCompletionParticles(ParticleSystem particles, Color color, int burstCount, float startSize)
    {
        if (particles == null) return;

        ParticleSystem.MainModule main = particles.main;
        main.startColor = color;
        if (startSize > 0f) main.startSize = startSize;

        if (burstCount > 0)
        {
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)burstCount)
            });
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
