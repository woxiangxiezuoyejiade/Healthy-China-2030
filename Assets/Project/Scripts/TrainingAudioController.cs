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
    public bool keepBackgroundMusicAlive = true;
    public bool useGeneratedFallbackClips = true;
    public bool enableChargeLoop = false;

    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.48f;
    [Range(0f, 1f)] public float sfxVolume = 0.70f;
    [Range(0f, 1f)] public float excellentVolume = 0.82f;
    [Range(0f, 1f)] public float invalidVolume = 0.42f;
    [Range(0f, 1f)] public float chargeVolume = 0.06f;

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
    float nextBgmCheckTime;
    float chargeTargetVolume;
    float chargeCurrentVolume;
    float chargeTargetPitch = 1f;

    protected virtual void Awake()
    {
        EnsureSources();
        AutoLoadDefaultBgm();
        AutoLoadDefaultSfx();
        CreateFallbackClips();
    }

    protected virtual void Start()
    {
        StartBackgroundMusic();
    }

    protected virtual void OnEnable()
    {
        EnsureSources();
        StartBackgroundMusic();
    }

    protected virtual void Update()
    {
        UpdateChargeLoopFade();

        if (!keepBackgroundMusicAlive || Time.unscaledTime < nextBgmCheckTime) return;

        nextBgmCheckTime = Time.unscaledTime + 2f;
        if (AudioListener.volume <= 0.01f) AudioListener.volume = 1f;

        if (bgmSource != null && !bgmSource.isPlaying)
        {
            StartBackgroundMusic();
        }
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
        bgmSource.mute = false;
        bgmSource.ignoreListenerPause = true;

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
        if (!enableChargeLoop)
        {
            chargeTargetVolume = 0f;
            chargeCurrentVolume = 0f;
            if (chargeLoopSource.isPlaying) chargeLoopSource.Stop();
            return;
        }

        AudioClip clip = chargeLoopClip != null ? chargeLoopClip : fallbackChargeLoopClip;
        if (clip == null) return;

        float clamped = Mathf.Clamp01(progress);
        chargeLoopSource.clip = clip;
        chargeLoopSource.loop = true;
        chargeTargetVolume = clamped > 0.05f ? Mathf.Lerp(0.008f, chargeVolume, clamped) : 0f;
        chargeTargetPitch = Mathf.Lerp(0.98f, 1.06f, clamped);
        chargeLoopSource.spatialBlend = 0f;

        if (chargeTargetVolume > 0.005f && !chargeLoopSource.isPlaying)
        {
            chargeLoopSource.Play();
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

        EnsureAudioListener();

        if (bgmSource == null) bgmSource = CreateChildSource("Audio_BGM");
        if (sfxSource == null) sfxSource = CreateChildSource("Audio_SFX");
        if (chargeLoopSource == null) chargeLoopSource = CreateChildSource("Audio_ChargeLoop");

        if (sfxSource != null) sfxSource.spatialBlend = 0f;
        if (chargeLoopSource != null) chargeLoopSource.spatialBlend = 0f;
        if (bgmSource != null) bgmSource.spatialBlend = 0f;
    }

    void EnsureAudioListener()
    {
        if (FindObjectOfType<AudioListener>() != null) return;
        if (Camera.main == null) return;

        Camera.main.gameObject.AddComponent<AudioListener>();
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

    void UpdateChargeLoopFade()
    {
        if (chargeLoopSource == null) return;

        float fadeSpeed = chargeTargetVolume > chargeCurrentVolume ? 5.5f : 1.8f;
        chargeCurrentVolume = Mathf.MoveTowards(chargeCurrentVolume, chargeTargetVolume, fadeSpeed * Time.unscaledDeltaTime);
        chargeLoopSource.volume = chargeCurrentVolume;
        chargeLoopSource.pitch = Mathf.MoveTowards(chargeLoopSource.pitch, chargeTargetPitch, 2.5f * Time.unscaledDeltaTime);

        if (chargeLoopSource.isPlaying && chargeTargetVolume <= 0.001f && chargeCurrentVolume <= 0.003f)
        {
            chargeLoopSource.Stop();
        }
    }

    void AutoLoadDefaultBgm()
    {
        if (bgmClip == null)
        {
            bgmClip = Resources.Load<AudioClip>("Audio/Ambient01");
        }

#if UNITY_EDITOR
        if (!autoLoadPureNatureBgmInEditor || bgmClip != null) return;

        AudioClip pureNatureClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/PureNature/Sounds/Ambient01.ogg");
        if (pureNatureClip != null)
        {
            bgmClip = pureNatureClip;
        }
#endif
    }

    void AutoLoadDefaultSfx()
    {
        AudioClip rightClip = Resources.Load<AudioClip>("Audio/right");
        if (rightClip == null) return;

        if (excellentClip == null) excellentClip = rightClip;
        if (goodClip == null) goodClip = rightClip;
        if (successClip == null) successClip = rightClip;
    }

    void CreateFallbackClips()
    {
        if (!useGeneratedFallbackClips) return;

        fallbackBgmClip = CreateAmbientLoop("Generated Soft BGM", 7.5f);
        fallbackChargeLoopClip = CreateToneLoop("Generated Soft Charge Loop", 220f, 2.8f, 0.022f);
        fallbackStartClip = CreateToneSequence("Generated Start", new[] { 783.99f, 1046.5f, 1318.51f }, 0.11f, 0.36f);
        fallbackCountdownClip = CreateToneSequence("Generated Countdown", new[] { 1046.5f }, 0.12f, 0.32f);
        fallbackExcellentClip = CreateToneSequence("Generated Excellent", new[] { 1046.5f, 1318.51f, 1760f, 2349.32f }, 0.10f, 0.43f);
        fallbackGoodClip = CreateToneSequence("Generated Good", new[] { 783.99f, 1174.66f, 1567.98f }, 0.11f, 0.37f);
        fallbackNeedsImprovementClip = CreateToneSequence("Generated Needs Improvement", new[] { 659.25f, 880f }, 0.14f, 0.29f);
        fallbackInvalidClip = CreateToneSequence("Generated Invalid", new[] { 440f, 349.23f }, 0.16f, 0.24f);
        fallbackComboClip = CreateToneSequence("Generated Combo", new[] { 1174.66f, 1567.98f, 2093f }, 0.075f, 0.39f);
        fallbackFinishClip = CreateToneSequence("Generated Finish", new[] { 783.99f, 1046.5f, 1318.51f, 1760f, 2349.32f }, 0.10f, 0.39f);
        fallbackIncompleteClip = CreateToneSequence("Generated Incomplete", new[] { 587.33f, 440f }, 0.16f, 0.25f);
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
            float frequency = frequencies[segment];
            float t = i / (float)SampleRate;
            float attack = Mathf.Clamp01(local / 0.08f);
            float decay = Mathf.Exp(-3.6f * local);
            float tail = Mathf.SmoothStep(0f, 1f, 1f - local);
            float envelope = attack * decay * tail;
            float vibrato = 1f + Mathf.Sin(2f * Mathf.PI * 7.5f * t) * 0.0025f;
            float tunedFrequency = frequency * vibrato;
            float tone =
                Mathf.Sin(2f * Mathf.PI * tunedFrequency * t) * 0.62f +
                Mathf.Sin(2f * Mathf.PI * tunedFrequency * 2.01f * t) * 0.24f +
                Mathf.Sin(2f * Mathf.PI * tunedFrequency * 3.02f * t) * 0.10f +
                Mathf.Sin(2f * Mathf.PI * tunedFrequency * 5.01f * t) * 0.04f;
            data[i] = Mathf.Clamp(tone * envelope * volume, -0.95f, 0.95f);
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
            float pulse = 0.82f + 0.18f * Mathf.Sin(2f * Mathf.PI * 0.45f * t);
            float shimmer = 0.80f + 0.20f * Mathf.Sin(2f * Mathf.PI * 0.72f * t + 0.8f);
            data[i] = (
                Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.52f +
                Mathf.Sin(2f * Mathf.PI * frequency * 2f * t) * 0.16f +
                Mathf.Sin(2f * Mathf.PI * frequency * 0.5f * t) * 0.08f) * volume * pulse * shimmer;
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
