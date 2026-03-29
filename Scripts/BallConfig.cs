using UnityEngine;

/// <summary>
/// ScriptableObject that defines one tier of sports ball.
/// Create via: right-click > Sports Balls > Ball Config
/// </summary>
[CreateAssetMenu(fileName = "BallConfig", menuName = "Sports Balls/Ball Config")]
public class BallConfig : ScriptableObject
{
    [Header("Identity")]
    public string ballName;
    public int levelIndex;          // 0 = Golf … 7 = Basketball

    [Header("Physics Shape")]
    public float radius = 0.30f;    // world-space radius (ball diameter = radius * 2)
    public bool isCapsule = false;  // true only for American Football (level 6)
    public float capsuleHeight = 1.2f; // world-space height; only used when isCapsule = true

    [Header("Visuals")]
    public Sprite sprite;           // assign the ball's PNG sprite here
    public Color colorTint = Color.white;

    [Header("Gameplay")]
    public int scoreValue = 1;      // points awarded when this ball is produced by a merge

    [Header("Audio")]
    public AudioClip mergeSound;    // optional per-ball merge sound override
}
