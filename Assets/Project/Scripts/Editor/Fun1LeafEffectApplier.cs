using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class Fun1LeafEffectApplier
{
    const string GreenLeafTexture = "Assets/MyWeeklyGoal/May2026/LeavesFallEffect/Sprites/GreenLeave.png";
    const string YellowLeafTexture = "Assets/MyWeeklyGoal/May2026/LeavesFallEffect/Sprites/YellowLeave.png";
    const string RedLeafTexture = "Assets/MyWeeklyGoal/May2026/LeavesFallEffect/Sprites/RedLeave.png";
    const string BrownLeafTexture = "Assets/MyWeeklyGoal/May2026/LeavesFallEffect/Sprites/BrownLeave.png";
    const string MaterialFolder = "Assets/Project/Materials/Fun1";
    static readonly Color Tier25Color = new Color(0.38f, 1.00f, 0.70f);
    static readonly Color Tier50Color = new Color(0.32f, 0.95f, 1.00f);
    static readonly Color Tier75Color = new Color(1.00f, 0.72f, 0.32f);
    static readonly Color Tier100Color = new Color(1.00f, 0.92f, 0.36f);

    [MenuItem("Tools/Healthy China/Fun1/Apply Leaf Completion Effects")]
    public static void ApplyLeafCompletionEffects()
    {
        Game1_EnergyCoreVisual visual = Object.FindObjectOfType<Game1_EnergyCoreVisual>(true);
        if (visual == null)
        {
            EditorUtility.DisplayDialog("Fun1 Leaf Effects", "Open Func1 scene first. No Game1_EnergyCoreVisual was found.", "OK");
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<Texture2D>(GreenLeafTexture) == null ||
            AssetDatabase.LoadAssetAtPath<Texture2D>(YellowLeafTexture) == null ||
            AssetDatabase.LoadAssetAtPath<Texture2D>(RedLeafTexture) == null ||
            AssetDatabase.LoadAssetAtPath<Texture2D>(BrownLeafTexture) == null)
        {
            EditorUtility.DisplayDialog(
                "Fun1 Leaf Effects",
                "Leaf textures were not found. Please finish importing the FREE Stylized Falling Leaves package first.",
                "OK");
            return;
        }

        EnsureFolder(MaterialFolder);
        ConfigureTexture(GreenLeafTexture);
        ConfigureTexture(YellowLeafTexture);
        ConfigureTexture(RedLeafTexture);
        ConfigureTexture(BrownLeafTexture);

        Material tier25 = CreateLeafMaterial("Func1_Completion25_GreenLeaf.mat", GreenLeafTexture);
        Material tier50 = CreateLeafMaterial("Func1_Completion50_YellowLeaf.mat", YellowLeafTexture);
        Material tier75 = CreateLeafMaterial("Func1_Completion75_RedLeaf.mat", RedLeafTexture);
        Material tier100 = CreateLeafMaterial("Func1_Completion100_GoldLeaf.mat", YellowLeafTexture);

        Undo.RecordObject(visual, "Apply Fun1 Leaf Completion Effects");
        visual.useFallingLeafCompletionEffects = true;
        visual.completion25Color = Tier25Color;
        visual.completion50Color = Tier50Color;
        visual.completion75Color = Tier75Color;
        visual.completion100Color = Tier100Color;
        visual.completion25LeafMaterial = tier25;
        visual.completion50LeafMaterial = tier50;
        visual.completion75LeafMaterial = tier75;
        visual.completion100LeafMaterial = tier100;
        visual.leafEmitterOffset = Vector3.zero;
        visual.leafEmitterBoxSize = new Vector3(0.2f, 0.2f, 0.2f);

        ConfigureParticle(visual.completion25Particles, tier25, 16, 0.24f);
        ConfigureParticle(visual.completion50Particles, tier50, 24, 0.28f);
        ConfigureParticle(visual.completion75Particles, tier75, 34, 0.32f);
        ConfigureParticle(visual.completion100Particles, tier100, 48, 0.38f);

        EditorUtility.SetDirty(visual);
        EditorSceneManager.MarkSceneDirty(visual.gameObject.scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = visual.gameObject;

        EditorUtility.DisplayDialog("Fun1 Leaf Effects", "Leaf completion effects have been applied to Func1.", "OK");
    }

    static void ConfigureParticle(ParticleSystem particles, Material material, int burstCount, float startSize)
    {
        if (particles == null) return;

        Undo.RecordObject(particles, "Configure Fun1 Leaf Particles");
        particles.transform.localPosition = Vector3.zero;

        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.55f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.10f, 1.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.75f, 1.65f);
        main.startSize = new ParticleSystem.MinMaxCurve(startSize * 0.80f, startSize * 1.35f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = Color.white;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

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
                new GradientColorKey(Color.white, 0.45f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.95f, 0.10f),
                new GradientAlphaKey(0.85f, 0.70f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        if (renderer != null)
        {
            Undo.RecordObject(renderer, "Configure Fun1 Leaf Renderer");
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = material;
            renderer.sortingFudge = 2f;
            EditorUtility.SetDirty(renderer);
        }

        EditorUtility.SetDirty(particles);
    }

    static Material CreateLeafMaterial(string fileName, string texturePath)
    {
        string materialPath = $"{MaterialFolder}/{fileName}";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Standard");

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else if (shader != null)
        {
            material.shader = shader;
        }

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        material.name = Path.GetFileNameWithoutExtension(fileName);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_Color")) material.color = Color.white;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", Color.white);

        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;

        EditorUtility.SetDirty(material);
        return material;
    }

    static void ConfigureTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Default;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }

    static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
