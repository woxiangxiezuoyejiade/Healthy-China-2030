using UnityEngine;

public class Game1_AudioController : MonoBehaviour
{
    [Header("Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;
    public AudioSource chargeLoopSource;

    [Header("Clips")]
    public AudioClip startClip;
    public AudioClip countdownClip;
    public AudioClip successClip;
    public AudioClip excellentClip;
    public AudioClip invalidClip;
    public AudioClip comboClip;
    public AudioClip finishClip;
    public AudioClip incompleteClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float bgmVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.6f;
    [Range(0f, 1f)] public float successVolume = 0.75f;
    [Range(0f, 1f)] public float invalidVolume = 0.45f;

    void Start()
    {
        if (bgmSource == null) return;

        bgmSource.loop = true;
        bgmSource.volume = bgmVolume;
        if (!bgmSource.isPlaying) bgmSource.Play();
    }

    public void SetChargeProgress(float progress)
    {
        if (chargeLoopSource == null) return;

        float clamped = Mathf.Clamp01(progress);
        chargeLoopSource.volume = Mathf.Lerp(0.05f, 0.35f, clamped);
        chargeLoopSource.pitch = Mathf.Lerp(0.85f, 1.2f, clamped);

        if (clamped > 0.05f && !chargeLoopSource.isPlaying)
        {
            chargeLoopSource.loop = true;
            chargeLoopSource.Play();
        }
        else if (clamped <= 0.05f && chargeLoopSource.isPlaying)
        {
            chargeLoopSource.Stop();
        }
    }

    public void PlayStart() => Play(startClip, sfxVolume);
    public void PlayCountdown() => Play(countdownClip, sfxVolume);
    public void PlayCombo() => Play(comboClip, sfxVolume);
    public void PlayFinish(bool success) => Play(success ? finishClip : incompleteClip, successVolume);

    public void PlayActionResult(Game1_ActionGrade grade)
    {
        if (grade == Game1_ActionGrade.Excellent)
        {
            Play(excellentClip != null ? excellentClip : successClip, successVolume);
        }
        else if (grade == Game1_ActionGrade.Good || grade == Game1_ActionGrade.NeedsImprovement)
        {
            Play(successClip, successVolume);
        }
        else
        {
            Play(invalidClip, invalidVolume);
        }
    }

    public void PlayInvalid()
    {
        Play(invalidClip, invalidVolume);
    }

    void Play(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volume);
    }
}
