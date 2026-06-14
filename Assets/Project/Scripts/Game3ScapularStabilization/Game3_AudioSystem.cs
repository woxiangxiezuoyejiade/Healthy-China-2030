using System.Collections;
using UnityEngine;

/// <summary>
/// 射击行动 - 音效系统
/// 使用程序化生成的AudioClip，无需外部音频文件
/// </summary>
public class Game3_AudioSystem : MonoBehaviour
{
    public static Game3_AudioSystem Instance { get; private set; }

    AudioSource sfxSource;
    AudioClip shootClip;
    AudioClip hitClip;
    AudioClip missClip;
    AudioClip comboClip;
    AudioClip countdownClip;
    AudioClip startClip;
    AudioClip endClip;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.spatialBlend = 0f; // 2D音效

        GenerateAllClips();
    }

    void GenerateAllClips()
    {
        shootClip = GenerateTone(800f, 0.08f, 0.3f, true);
        hitClip = GenerateTone(1200f, 0.1f, 0.25f, true);
        missClip = GenerateTone(200f, 0.15f, 0.2f, false);
        comboClip = GenerateTone(1500f, 0.12f, 0.3f, true);
        countdownClip = GenerateTone(600f, 0.15f, 0.2f, false);
        startClip = GenerateTone(1000f, 0.2f, 0.35f, true);
        endClip = GenerateTone(400f, 0.4f, 0.25f, false);
    }

    AudioClip GenerateTone(float frequency, float duration, float volume, bool ascending)
    {
        int sampleRate = 44100;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = (float)i / sampleCount;

            // 频率变化
            float freq = ascending ? frequency * (1f + progress * 0.5f) : frequency * (1f - progress * 0.3f);

            // 波形
            float wave = Mathf.Sin(2f * Mathf.PI * freq * t);

            // 包络（淡入淡出）
            float envelope = 1f;
            if (progress < 0.05f) envelope = progress / 0.05f;
            else if (progress > 0.6f) envelope = (1f - progress) / 0.4f;
            envelope = Mathf.Clamp01(envelope);

            samples[i] = wave * envelope * volume;
        }

        AudioClip clip = AudioClip.Create($"Tone_{frequency}_{duration}", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    public void PlayShoot() { if (shootClip != null) sfxSource.PlayOneShot(shootClip, 0.6f); }
    public void PlayHit() { if (hitClip != null) sfxSource.PlayOneShot(hitClip, 0.7f); }
    public void PlayMiss() { if (missClip != null) sfxSource.PlayOneShot(missClip, 0.5f); }
    public void PlayCombo() { if (comboClip != null) sfxSource.PlayOneShot(comboClip, 0.8f); }
    public void PlayCountdown() { if (countdownClip != null) sfxSource.PlayOneShot(countdownClip, 0.5f); }
    public void PlayStart() { if (startClip != null) sfxSource.PlayOneShot(startClip, 0.7f); }
    public void PlayEnd() { if (endClip != null) sfxSource.PlayOneShot(endClip, 0.6f); }
}
