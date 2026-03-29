using UnityEngine;

/// <summary>
/// Centralised audio playback for all game SFX.
/// Attach to the GameManager GameObject (or any persistent object).
/// Wire up clip references in the Inspector.
/// Singleton — access via AudioManager.Instance.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ─── Clip References ──────────────────────────────────────────────────────
    [Header("SFX Clips")]
    [Tooltip("Played when the player releases a ball.")]
    public AudioClip dropClip;

    [Tooltip("Merge sound for levels 0–2 (Golf, Pool, Tennis).")]
    public AudioClip mergeSmallClip;

    [Tooltip("Merge sound for levels 3–4 (Baseball, Volleyball).")]
    public AudioClip mergeMediumClip;

    [Tooltip("Merge sound for levels 5–6 (Soccer, Football).")]
    public AudioClip mergeLargeClip;

    [Tooltip("Short blip played every time the score increments.")]
    public AudioClip scoreTickClip;

    [Tooltip("Pulsing alarm played while balls are above the danger line.")]
    public AudioClip dangerClip;

    [Tooltip("Played when the game ends.")]
    public AudioClip gameOverClip;

    [Tooltip("Triumphant arpeggio for the max-tier (Basketball) merge.")]
    public AudioClip basketballMergeClip;

    [Header("Volume")]
    [Range(0f, 1f)] public float sfxVolume = 0.9f;

    // ─── Internal ─────────────────────────────────────────────────────────────
    private AudioSource _sfx;
    private AudioSource _loop;  // for looping danger alarm

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // One-shot SFX source
        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.volume      = sfxVolume;

        // Looping source for danger alarm
        _loop = gameObject.AddComponent<AudioSource>();
        _loop.playOnAwake = false;
        _loop.loop        = true;
        _loop.volume      = sfxVolume * 0.7f;
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public void PlayDrop()            => PlayOneShot(dropClip);
    public void PlayScoreTick()       => PlayOneShot(scoreTickClip);
    public void PlayGameOver()        => PlayOneShot(gameOverClip);
    public void PlayBasketballMerge() => PlayOneShot(basketballMergeClip);

    public void PlayMerge(int level)
    {
        if      (level <= 2) PlayOneShot(mergeSmallClip);
        else if (level <= 4) PlayOneShot(mergeMediumClip);
        else                 PlayOneShot(mergeLargeClip);
    }

    /// <summary>Start or stop the looping danger alarm.</summary>
    public void SetDangerLoop(bool active)
    {
        if (dangerClip == null) return;

        if (active && !_loop.isPlaying)
        {
            _loop.clip = dangerClip;
            _loop.Play();
        }
        else if (!active && _loop.isPlaying)
        {
            _loop.Stop();
        }
    }

    // ─── Private ──────────────────────────────────────────────────────────────

    private void PlayOneShot(AudioClip clip)
    {
        if (clip != null)
            _sfx.PlayOneShot(clip, sfxVolume);
    }
}
