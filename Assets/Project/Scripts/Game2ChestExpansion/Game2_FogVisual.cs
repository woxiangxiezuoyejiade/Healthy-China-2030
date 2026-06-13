using UnityEngine;

public class Game2_FogVisual : MonoBehaviour
{
    [Header("Fog Objects")]
    public Transform fogRoot;
    public Renderer[] fogRenderers;
    public ParticleSystem[] fogParticles;

    [Header("Fog Settings")]
    [Range(0f, 1f)] public float startDensity = 0.85f;
    public Color fogColor = new Color(0.85f, 0.93f, 1f, 0.85f);

    Material[] fogMaterials;
    float clearProgress;

    void Awake()
    {
        CacheMaterials();
        SetClearProgress(0f);
    }

    public void AddClearProgress(float amount)
    {
        SetClearProgress(clearProgress + amount);
    }

    public void SetClearProgress(float value)
    {
        clearProgress = Mathf.Clamp01(value);
        float alpha = Mathf.Lerp(startDensity, 0f, clearProgress);
        ApplyAlpha(alpha);
        ApplyParticleDensity(1f - clearProgress);
    }

    public float GetClearProgress()
    {
        return clearProgress;
    }

    void CacheMaterials()
    {
        Transform searchRoot = fogRoot != null ? fogRoot : transform;

        if (fogRenderers == null || fogRenderers.Length == 0)
        {
            fogRenderers = searchRoot.GetComponentsInChildren<Renderer>(true);
        }

        if (fogParticles == null || fogParticles.Length == 0)
        {
            fogParticles = searchRoot.GetComponentsInChildren<ParticleSystem>(true);
        }

        if (fogRenderers == null) return;

        fogMaterials = new Material[fogRenderers.Length];
        for (int i = 0; i < fogRenderers.Length; i++)
        {
            if (fogRenderers[i] == null) continue;
            fogMaterials[i] = fogRenderers[i].material;
            ConfigureTransparentMaterial(fogMaterials[i]);
        }
    }

    void ApplyAlpha(float alpha)
    {
        if (fogMaterials == null) CacheMaterials();
        if (fogMaterials == null) return;

        for (int i = 0; i < fogMaterials.Length; i++)
        {
            Material material = fogMaterials[i];
            if (material == null) continue;

            Color color = fogColor;
            color.a = alpha;

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

    void ApplyParticleDensity(float density)
    {
        if (fogParticles == null) return;

        for (int i = 0; i < fogParticles.Length; i++)
        {
            if (fogParticles[i] == null) continue;

            ParticleSystem.EmissionModule emission = fogParticles[i].emission;
            emission.rateOverTime = Mathf.Lerp(0f, 35f, density);
        }
    }

    void ConfigureTransparentMaterial(Material material)
    {
        if (material == null) return;

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
