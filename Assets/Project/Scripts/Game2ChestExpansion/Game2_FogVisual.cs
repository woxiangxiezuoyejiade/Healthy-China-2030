using UnityEngine;

/// <summary>
/// Func2 的雾气系统。
/// 这版不再用大面积 billboard 粒子当雾，避免出现“白色纸片/白色贴图块”。
/// 主要使用 Unity RenderSettings 的真实距离雾：开始时场景远处发白、模糊，
/// 随训练完成度提高逐渐变透明、变清晰。
/// </summary>
public class Game2_FogVisual : MonoBehaviour
{
    [Header("Root Cleanup")]
    public Transform fogRoot;
    public bool autoCleanOldFogObjects = true;

    [Header("Atmospheric Fog")]
    public Color fogColor = new Color(0.76f, 0.91f, 0.97f, 0.5f);
    public bool changeFogColorWithProgress = false;
    public FogMode fogMode = FogMode.Linear;
    public float startFogStartDistance = 2.5f;
    public float startFogEndDistance = 15.0f;
    public float clearFogStartDistance = 24f;
    public float clearFogEndDistance = 85f;

    [Header("Pico/Android Safety")]
    public bool smoothFogClearOnDevice = true;
    public float fogClearSmoothSpeed = 0.45f;
    public bool useDissolveParticlesOnAndroid = false;

    [Header("Fallback Exponential Fog")]
    [Range(0f, 0.08f)] public float startFogDensity = 0.026f;
    [Range(0f, 0.08f)] public float clearFogDensity = 0.001f;

    [Header("Visible Fog Wisps")]
    public bool useVisibleFogWisps = true;
    [Range(2, 16)] public int wispCount = 9;
    public Color wispColor = new Color(0.88f, 0.98f, 1f, 0.34f);
    public Vector3 wispCenter = new Vector3(0f, 1.02f, 1.3f);
    public Vector3 wispArea = new Vector3(8.4f, 1.15f, 2.4f);
    public float wispWidth = 0.24f;
    public float wispLength = 5.8f;
    public float wispDriftSpeed = 0.35f;

    [Header("Dissolve Feedback")]
    public bool useTinyDissolveParticles = true;
    public Color dissolveColor = new Color(0.82f, 0.97f, 1f, 0.32f);
    public Vector3 dissolveCenter = new Vector3(0f, 1.2f, 1.45f);
    public Vector3 dissolveArea = new Vector3(6.2f, 1.8f, 2.7f);

    float clearProgress;
    float targetClearProgress;
    float previousProgress = -1f;
    bool initialized;
    bool originalFogEnabled;
    bool fogApplied;
    FogMode originalFogMode;
    Color originalFogColor;
    float originalFogDensity;
    float originalFogStartDistance;
    float originalFogEndDistance;

    Transform generatedRoot;
    LineRenderer[] fogWisps;
    Vector3[][] wispBasePoints;
    float[] wispPhase;
    Material wispMaterial;
    ParticleSystem dissolveParticles;

    void Awake()
    {
        CacheOriginalFogSettings();
        ApplyFog(0f);
    }

    void Start()
    {
        EnsureVisuals();
        SetClearProgress(0f);
    }

    void OnDisable()
    {
        RestoreOriginalFogSettings();
    }

    void Update()
    {
        if (smoothFogClearOnDevice && Application.isPlaying && !Mathf.Approximately(clearProgress, targetClearProgress))
        {
            previousProgress = clearProgress;
            clearProgress = Mathf.MoveTowards(clearProgress, targetClearProgress, fogClearSmoothSpeed * Time.deltaTime);
            ApplyFog(clearProgress);
        }

        if (initialized && useVisibleFogWisps)
        {
            ApplyWispState(clearProgress);
        }
    }

    public void AddClearProgress(float amount)
    {
        SetClearProgress(targetClearProgress + amount);
    }

    public void SetClearProgress(float value)
    {
        EnsureVisuals();

        value = Mathf.Clamp01(value);
        previousProgress = clearProgress;
        targetClearProgress = value;

        bool shouldSmooth = smoothFogClearOnDevice && Application.isPlaying && value > clearProgress;
        if (!shouldSmooth)
        {
            clearProgress = value;
            ApplyFog(clearProgress);
        }

        if (ShouldUseDissolveFeedback() && previousProgress >= 0f && value > previousProgress + 0.015f)
        {
            PlayDissolveFeedback(value - previousProgress);
        }
    }

    public float GetClearProgress()
    {
        return clearProgress;
    }

    void EnsureVisuals()
    {
        if (initialized && generatedRoot != null)
        {
            if (fogRoot == null || generatedRoot.parent == fogRoot) return;
            DestroySafe(generatedRoot.gameObject);
            initialized = false;
        }

        ResolveFogRoot();

        if (autoCleanOldFogObjects)
        {
            CleanLegacyFogObjects();
        }

        Transform oldDynamic = FindChildByName(fogRoot, "DynamicFog_System");
        if (oldDynamic != null)
        {
            DestroySafe(oldDynamic.gameObject);
        }

        generatedRoot = new GameObject("DynamicFog_System").transform;
        generatedRoot.SetParent(fogRoot, false);
        generatedRoot.localPosition = Vector3.zero;
        generatedRoot.localRotation = Quaternion.identity;
        generatedRoot.localScale = Vector3.one;

        if (useVisibleFogWisps)
        {
            CreateFogWisps();
        }

        if (ShouldUseDissolveFeedback())
        {
            dissolveParticles = CreateTinyDissolveParticles();
        }

        initialized = true;
    }

    void ResolveFogRoot()
    {
        if (fogRoot != null) return;

        GameObject environment = GameObject.Find("Game2_Environment");
        Transform parent = environment != null ? environment.transform : transform;
        Transform existingFogRoot = FindChildByName(parent, "FogArea");

        if (existingFogRoot != null)
        {
            fogRoot = existingFogRoot;
            return;
        }

        GameObject fogObject = new GameObject("FogArea");
        fogRoot = fogObject.transform;
        fogRoot.SetParent(parent, false);
    }

    void CleanLegacyFogObjects()
    {
        if (fogRoot == null) return;

        for (int i = fogRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = fogRoot.GetChild(i);
            string childName = child.name;

            bool isOldFog =
                childName.StartsWith("FogCube") ||
                childName.StartsWith("SoftMist") ||
                childName.StartsWith("Mist") ||
                childName.StartsWith("FogWisp") ||
                childName.StartsWith("Depth_Mist") ||
                childName.StartsWith("Ground_Haze") ||
                childName == "DynamicFog_System";

            bool keepImportedEffect =
                childName.Contains("Cloud") ||
                childName.Contains("GodRay") ||
                childName.Contains("Ray");

            if (isOldFog && !keepImportedEffect)
            {
                DestroySafe(child.gameObject);
            }
        }
    }

    void ApplyFog(float progress)
    {
        float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));

        // Always apply fog for Game2 (fog clearing is the core mechanic).
        // Use safe defaults: low alpha, further start distance for VR.
        RenderSettings.fog = true;
        fogApplied = true;
        RenderSettings.fogMode = fogMode;

        Color targetFogColor = changeFogColorWithProgress
            ? Color.Lerp(fogColor, new Color(0.86f, 0.97f, 1f, fogColor.a * 0.3f), t)
            : fogColor;
        // Clamp alpha to prevent full-opacity fog on mobile/VR.
        targetFogColor.a = Mathf.Min(targetFogColor.a, 0.6f);
        RenderSettings.fogColor = targetFogColor;

        if (fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = Mathf.Lerp(startFogStartDistance, clearFogStartDistance, t);
            RenderSettings.fogEndDistance = Mathf.Lerp(startFogEndDistance, clearFogEndDistance, t);
        }
        else
        {
            RenderSettings.fogDensity = Mathf.Lerp(startFogDensity, clearFogDensity, t);
        }

        ApplyWispState(progress);
    }

    void CreateFogWisps()
    {
        wispMaterial = CreateWispMaterial();
        fogWisps = new LineRenderer[wispCount];
        wispBasePoints = new Vector3[wispCount][];
        wispPhase = new float[wispCount];

        for (int i = 0; i < wispCount; i++)
        {
            GameObject wispObject = new GameObject($"FogWisp_{i + 1:00}");
            wispObject.transform.SetParent(generatedRoot, false);

            LineRenderer line = wispObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 18;
            line.textureMode = LineTextureMode.Stretch;
            line.alignment = LineAlignment.View;
            line.numCapVertices = 8;
            line.numCornerVertices = 6;
            line.widthMultiplier = wispWidth * Mathf.Lerp(0.65f, 1.2f, (i % 4) / 3f);
            line.material = wispMaterial;
            line.sortingOrder = 2;

            float row = wispCount <= 1 ? 0f : i / (float)(wispCount - 1);
            float y = wispCenter.y + Mathf.Lerp(-wispArea.y * 0.5f, wispArea.y * 0.5f, row);
            float z = wispCenter.z + Mathf.Sin(i * 1.37f) * wispArea.z * 0.5f;
            float xOffset = Mathf.Sin(i * 0.91f) * 0.55f;
            float length = wispLength * Mathf.Lerp(0.72f, 1.08f, ((i * 37) % 100) / 100f);

            Vector3[] points = new Vector3[line.positionCount];
            for (int p = 0; p < points.Length; p++)
            {
                float u = p / (float)(points.Length - 1);
                float x = wispCenter.x + xOffset + Mathf.Lerp(-length * 0.5f, length * 0.5f, u);
                float wave = Mathf.Sin(u * Mathf.PI * 2f + i * 0.73f) * 0.09f;
                points[p] = new Vector3(x, y + wave, z);
                line.SetPosition(p, points[p]);
            }

            fogWisps[i] = line;
            wispBasePoints[i] = points;
            wispPhase[i] = i * 0.67f;
        }
    }

    void ApplyWispState(float progress)
    {
        if (fogWisps == null) return;

        float clear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
        float alpha = wispColor.a * (1f - clear) * (1f - clear);
        Color color = new Color(wispColor.r, wispColor.g, wispColor.b, alpha);

        for (int i = 0; i < fogWisps.Length; i++)
        {
            LineRenderer line = fogWisps[i];
            if (line == null) continue;

            bool visible = alpha > 0.012f;
            line.enabled = visible;
            if (!visible) continue;

            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, alpha * 0.55f);
            line.widthMultiplier = wispWidth * Mathf.Lerp(0.65f, 1.2f, (i % 4) / 3f) * Mathf.Lerp(1f, 0.42f, clear);

            Vector3[] basePoints = wispBasePoints[i];
            float phase = Time.time * wispDriftSpeed + wispPhase[i];
            for (int p = 0; p < basePoints.Length; p++)
            {
                float u = p / (float)(basePoints.Length - 1);
                Vector3 point = basePoints[p];
                point.x += Mathf.Sin(phase + u * 2.2f) * 0.08f;
                point.y += Mathf.Sin(phase * 0.75f + u * 5.1f) * 0.045f;
                point.z += clear * 1.15f;
                line.SetPosition(p, point);
            }
        }
    }

    Material CreateWispMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.name = "Game2_Runtime_FogWisp";
        material.color = wispColor;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", wispColor);
        if (material.HasProperty("_Color")) material.SetColor("_Color", wispColor);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }

    ParticleSystem CreateTinyDissolveParticles()
    {
        GameObject particleObject = new GameObject("Mist_Clear_Tiny_Particles");
        particleObject.transform.SetParent(generatedRoot, false);
        particleObject.transform.localPosition = dissolveCenter;

        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.38f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.095f);
        main.startColor = dissolveColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 70;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = dissolveArea;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.03f, 0.18f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.12f);

        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.78f, 0.95f, 1f), 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.28f, 0.18f),
                new GradientAlphaKey(0.18f, 0.58f),
                new GradientAlphaKey(0f, 1f)
            });
        color.color = gradient;

        ParticleSystemRenderer renderer = particleObject.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateDissolveParticleMaterial(dissolveColor);
        renderer.sortingOrder = 3;

        return particles;
    }

    Material CreateDissolveParticleMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.name = "Game2_Runtime_Dissolve_Particle";
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }

    bool ShouldUseDissolveFeedback()
    {
        return useTinyDissolveParticles && (!IsAndroidDevice() || useDissolveParticlesOnAndroid);
    }

    bool IsAndroidDevice()
    {
        return Application.platform == RuntimePlatform.Android;
    }

    void PlayDissolveFeedback(float delta)
    {
        if (dissolveParticles == null) return;

        int count = Mathf.Clamp(Mathf.RoundToInt(delta * 120f), 4, 18);
        dissolveParticles.Emit(count);
    }

    void CacheOriginalFogSettings()
    {
        originalFogEnabled = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
        originalFogStartDistance = RenderSettings.fogStartDistance;
        originalFogEndDistance = RenderSettings.fogEndDistance;
    }

    void RestoreOriginalFogSettings()
    {
        RenderSettings.fog = originalFogEnabled;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogDensity = originalFogDensity;
        RenderSettings.fogStartDistance = originalFogStartDistance;
        RenderSettings.fogEndDistance = originalFogEndDistance;
    }

    Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null) return null;
        if (parent.name == childName) return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildByName(parent.GetChild(i), childName);
            if (found != null) return found;
        }

        return null;
    }

    void DestroySafe(GameObject target)
    {
        if (target == null) return;

        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
