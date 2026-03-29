using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to every ball in the scene.
/// Call Initialize() immediately after Instantiate() to configure the ball.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Ball : MonoBehaviour
{
    // Set by Initialize() — not serialised so the prefab stays generic
    [HideInInspector] public BallConfig config;
    [HideInInspector] public BallConfig[] allBallConfigs;

    private bool _canMerge  = false;
    private bool _hasMerged = false;
    private Rigidbody2D  _rb;
    private Collider2D   _col;

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>
    /// Configure this ball at runtime. Must be called right after Instantiate().
    /// </summary>
    public void Initialize(BallConfig cfg, BallConfig[] allConfigs)
    {
        config        = cfg;
        allBallConfigs = allConfigs;

        _rb = GetComponent<Rigidbody2D>();

        // ── Visuals ──────────────────────────────────────────────────────────
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (cfg.sprite != null) sr.sprite = cfg.sprite;
        sr.color = cfg.colorTint;

        // Scale: Circle.png is 1 world-unit diameter at localScale (1,1,1).
        // For football, stretch Y to capsuleHeight so the sprite looks oval.
        if (cfg.isCapsule)
            transform.localScale = new Vector3(cfg.radius * 2f, cfg.capsuleHeight, 1f);
        else
            transform.localScale = new Vector3(cfg.radius * 2f, cfg.radius * 2f, 1f);

        // ── Collider ─────────────────────────────────────────────────────────
        // We add the collider at runtime so the prefab can stay collider-free
        // and work for all 8 ball types (circle vs capsule).
        if (cfg.isCapsule)
        {
            CapsuleCollider2D cap = gameObject.AddComponent<CapsuleCollider2D>();
            cap.direction = CapsuleDirection2D.Vertical;
            // size is in LOCAL space; with scale (1.54, 1.2, 1):
            // world shape = (size.x * scale.x, size.y * scale.y) = (1.54, 1.2) ✓
            cap.size = new Vector2(1f, 1f);
            _col = cap;
        }
        else
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.radius = 0.5f; // local → world = 0.5 * diameter = radius ✓
            _col = circle;
        }

        // ── Rigidbody2D ──────────────────────────────────────────────────────
        _rb.gravityScale            = 1.5f;
        _rb.linearDamping           = 0.3f;   // Unity 6+; use _rb.drag for older versions
        _rb.angularDamping          = cfg.isCapsule ? 0.1f : 0.5f; // football tumbles more
        _rb.collisionDetectionMode  = CollisionDetectionMode2D.Continuous;
        _rb.interpolation           = RigidbodyInterpolation2D.Interpolate;

        // Delay merge eligibility so freshly-spawned balls don't accidentally
        // trigger mid-drop collisions.
        StartCoroutine(EnableMerge(0.5f));
    }

    /// <summary>
    /// Toggle held/dropped state.
    /// While held: Kinematic + trigger so live balls pass through.
    /// On drop: Dynamic + solid collider.
    /// </summary>
    public void SetHeld(bool held)
    {
        if (_rb  != null) _rb.bodyType    = held ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        if (_col != null) _col.isTrigger  = held;
    }

    // ─── Private ──────────────────────────────────────────────────────────────

    private IEnumerator EnableMerge(float delay)
    {
        yield return new WaitForSeconds(delay);
        _canMerge = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!_canMerge || _hasMerged) return;

        Ball other = collision.gameObject.GetComponent<Ball>();
        if (other == null || !other._canMerge || other._hasMerged) return;

        // Must be the same tier
        if (other.config.levelIndex != config.levelIndex) return;

        // InstanceID tie-breaker: only the ball with the HIGHER ID drives the
        // merge.  This prevents both OnCollisionEnter2D calls from each
        // spawning a new ball.
        if (other.GetInstanceID() < GetInstanceID()) return;

        // Lock both balls so no further merges can claim them this frame
        _hasMerged       = true;
        other._hasMerged = true;

        Vector3 mid = Vector3.Lerp(transform.position, other.transform.position, 0.5f);
        mid.z = 0f;

        int nextLevel = config.levelIndex + 1;
        int points;
        if (nextLevel < allBallConfigs.Length)
            points = allBallConfigs[nextLevel].scoreValue;
        else
            points = config.scoreValue * 2; // max-tier bonus

        AudioManager.Instance?.PlayMerge(config.levelIndex);
        GameManager.Instance.AddScore(points);

        Destroy(gameObject);
        Destroy(other.gameObject);

        if (nextLevel < allBallConfigs.Length)
            GameManager.Instance.SpawnMergedBall(allBallConfigs[nextLevel], mid, allBallConfigs);
        else
            // Double-bonus: merging two Basketballs
            GameManager.Instance.AddScore(config.scoreValue * 2);
    }
}
