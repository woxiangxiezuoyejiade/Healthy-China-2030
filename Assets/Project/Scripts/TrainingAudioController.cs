using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class TrainingAudioController : MonoBehaviour
{
    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource chargeLoopSource;

    [Header("Clips")]
    public AudioClip bgmClip;
    public AudioClip startClip;
    public AudioClip countdownClip;
    public AudioClip successClip;
    public AudioClip excellentClip;
    public AudioClip goodClip;
    public AudioClip needsImprovementClip;
    public AudioClip invalidClip;
    public AudioClip comboClip;
    public AudioClip finishClip;
    public AudioClip incompleteClip;
    public AudioClip chargeLoopClip;

    [Header("Auto Setup")]
    public bool autoCreateSources = true;
    public bool autoLoadPureNatureBgmInEditor = true;
    public bool useGeneratedFallbackClips = true;

    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.28f;
    [Range(0f, 1f)] public float sfxVolume = 0.62f;
    [Range(0f, 1f)] public float excellentVolume = 0.76f;
    [Range(0f, 1f)] public float invalidVolume = 0.46f;
    [Range(0f, 1f)] public float chargeVolume = 0.24f;

    const int SampleRate = 44100;

    AudioClip fallbackBgmClip;
    AudioClip fallbackStartClip;
    AudioClip fallbackCountdownClip;
    AudioClip fallbackExcellentClip;
    AudioClip fallbackGoodClip;
    AudioClip fallbackNeedsImprovementClip;
    AudioClip fallbackInvalidClip;
    AudioClip fallbackComboClip;
    AudioClip fallbackFinishClip;
    AudioClip fallbackIncompleteClip;
    AudioClip fallbackChargeLoopClip;

    protected virtual void Awake()
    {
        EnsureSources();
        AutoLoadDefaultBgm();
        CreateFallbackClips();
    }

    protected virtual void Start()
    {
        StartBackgroundMusic();
    }

    public void StartBackgroundMusic()
    {
        if (bgmSource == null) return;

        AudioClip clip = bgmClip != null ? bgmClip : fallbackBgmClip;
        if (clip == null) return;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        bgmSource.spatialBlend = 0f;

        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    public void StopBackgroundMusic()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void SetChargeProgress(float progress)
    {
        if (chargeLoopSource == null) return;

        AudioClip clip = chargeLoopClip != null ? chargeLoopClip : fallbackChargeLoopClip;
        if (clip == null) return;

        float clamped = Mathf.Clamp01(progress);
        chargeLoopSource.clip = clip;
        chargeLoopSource.loop = true;
        chargeLoopSource.volume = Mathf.Lerp(0.02f, chargeVolume, clamped);
        chargeLoopSource.pitch = Mathf.Lerp(0.86f, 1.18f, clamped);
        chargeLoopSource.spatialBlend = 0f;

        if (clamped > 0.05f && !chargeLoopSource.isPlaying)
        {
            chargeLoopSource.Play();
        }
        else if (clamped <= 0.05f && chargeLoopSource.isPlaying)
        {
            chargeLoopSource.Stop();
        }
    }

    public void PlayStart() => Play(startClip, fallbackStartClip, sfxVolume);
    public void PlayCountdown() => Play(countdownClip, fallbackCountdownClip, sfxVolume);
    public void PlayCombo() => Play(comboClip, fallbackComboClip, sfxVolume);
    public void PlayFinish(bool success) => Play(success ? finishClip : incompleteClip, success ? fallbackFinishClip : fallbackIncompleteClip, sfxVolume);
    public void PlayExcellent() => Play(excellentClip, fallbackExcellentClip, excellentVolume);
    public void PlayGood() => Play(goodClip != null ? goodClip : successClip, fallbackGoodClip, sfxVolume);
    public void PlayNeedsImprovement() => Play(needsImprovementClip != null ? needsImprovementClip : successClip, fallbackNeedsImprovementClip, sfxVolume);
    public void PlayInvalid() => Play(invalidClip, fallbackInvalidClip, invalidVolume);

    protected void EnsureSources()
    {
        if (!autoCreateSources) return;

        if (bgmSource == null) bgmSource = CreateChildSource("Audio_BGM");
        if (sfxSource == null) sfxSource = CreateChildSource("Audio_SFX");
        if (chargeLoopSource == null) chargeLoopSource = CreateChildSource("Audio_ChargeLoop");

        if (sfxSource != null) sfxSource.spatialBlend = 0f;
        if (chargeLoopSource != null) chargeLoopSource.spatialBlend = 0f;
    }

    AudioSource CreateChildSource(string childName)
    {
        Transform existing = transform.Find(childName);
        GameObject sourceObject = existing != null ? existing.gameObject : new GameObject(childName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource source = sourceObject.GetComponent<AudioSource>();
        if (source == null) source = sourceObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        return source;
    }

    void AutoLoadDefaultBgm()
    {
#if UNITY_EDITOR
        if (!autoLoadPureNatureBgmInEditor || bgmClip != null) return;

        AudioClip pureNatureClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PureNature/Sounds/Ambient01.ogg");
        if (pureNatureClip != null)
        {
            bgmClip = pureNatureClip;
        }
#endif
    }

    void CreateFallbackClips()
    {
        if (!useGeneratedFallbackClips) return;

        fallbackBgmClip = CreateAmbientLoop("Generated Soft BGM", 7.5f);
        fallbackChargeLoopClip = CreateToneLoop("Generated Charge Loop", 196f, 2.0f, 0.18f);
        fallbackStartClip = CreateToneSequence("Generated Start", new[] { 392f, 523.25f, 659.25f }, 0.11f, 0.34f);
        fallbackCountdownClip = CreateToneSequence("Generated Countdown", new[] { 523.25f }, 0.13f, 0.30f);
        fallbackExcellentClip = CreateToneSequence("Generated Excellent", new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.105f, 0.42f);
        fallbackGoodClip = CreateToneSequence("Generated Good", new[] { 440f, 659.25f }, 0.14f, 0.36f);
        fallbackNeedsImprovementClip = CreateToneSequence("Generated Needs Improvement", new[] { 392f, 493.88f }, 0.16f, 0.28f);
        fallbackInvalidClip = CreateToneSequence("Generated Invalid", new[] { 220f, 164.81f }, 0.18f, 0.30f);
        fallbackComboClip = CreateToneSequence("Generated Combo", new[] { 659.25f, 783.99f, 987.77f }, 0.075f, 0.35f);
        fallbackFinishClip = CreateToneSequence("Generated Finish", new[] { 392f, 523.25f, 659.25f, 783.99f, 1046.5f }, 0.095f, 0.36f);
        fallbackIncompleteClip = CreateToneSequence("Generated Incomplete", new[] { 329.63f, 261.63f }, 0.17f, 0.26f);
    }

    void Play(AudioClip customClip, AudioClip fallbackClip, float volume)
    {
        if (sfxSource == null) return;

        AudioClip clip = customClip != null ? customClip : fallbackClip;
        if (clip == null) return;

        sfxSource.PlayOneShot(clip, volume);
    }

    AudioClip CreateToneSequence(string clipName, float[] frequencies, float segmentDuration, float volume)
    {
        int samplesPerSegment = Mathf.Max(1, Mathf.RoundToInt(segmentDuration * SampleRate));
        int sampleCount = samplesPerSegment * frequencies.Length;
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            int segment = Mathf.Min(frequencies.Length - 1, i / samplesPerSegment);
            float local = (i % samplesPerSegment) / (float)samplesPerSegment;
            float envelope = Mathf.Sin(local * Mathf.PI);
            float frequency = frequencies[segment];
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / SampleRate) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip CreateToneLoop(string clipName, float frequency, float duration, float volume)
    {
        int sampleCount = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            data[i] = (
                Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.7f +
                Mathf.Sin(2f * Mathf.PI * frequency * 1.5f * t) * 0.3f) * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    AudioClip CreateAmbientLoop(string clipName, float duration)
    {
        int sampleCount = Mathf.RoundToInt(duration * SampleRate);
        float[] data = new float[sampleCount];
        float noise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            noise = Mathf.Lerp(noise, Random.Range(-1f, 1f), 0.012f);
            float pad =
                Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.035f +
                Mathf.Sin(2f * Mathf.PI * 146.83f * t) * 0.026f +
                Mathf.Sin(2f * Mathf.PI * 196f * t) * 0.018f;
            data[i] = Mathf.Clamp((pad + noise * 0.015f) * 0.65f, -0.18f, 0.18f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
