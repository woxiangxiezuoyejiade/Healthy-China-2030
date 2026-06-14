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

    [Header("Pillars")]
    public Renderer[] pillarRenderers;
    public Light[] pillarLights;

    [Header("Material Colors")]
    public Color idleColor = new Color(0.5f, 0.84f, 0.76f);
    public Color chargedColor = new Color(0.36f, 0.75f, 1f);
    public Color excellentColor = new Color(1f, 0.85f, 0.45f);
    public Color warningColor = new Color(1f, 0.54f, 0.40f);

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
}
