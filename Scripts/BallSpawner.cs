using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player input for aiming and dropping balls.
/// Place on an empty GameObject at Y = +4.3.
/// Attach a LineRenderer child named "AimLine".
/// </summary>
public class BallSpawner : MonoBehaviour
{
    [Header("References")]
    public GameObject   ballPrefab;
    public BallConfig[] allBallConfigs;     // all 8 configs in level order
    public LineRenderer aimLine;            // child LineRenderer for the drop guide

    [Header("Container Bounds")]
    public float wallLeftX  = -2.9f;        // inner face of left wall
    public float wallRightX =  2.9f;        // inner face of right wall
    public float spawnY     =  4.3f;        // height at which balls are held
    public float bottomY    = -4.5f;        // bottom of aim-line guide

    [Header("Timing")]
    public float dropDelay        = 0.8f;   // seconds before the next ball appears
    public int   spawnableMaxLevel = 4;     // only levels 0–4 can be randomly spawned

    // ─── State ───────────────────────────────────────────────────────────────
    private BallConfig  _currentConfig;
    private BallConfig  _nextConfig;
    private GameObject  _pendingBall;
    private bool        _canDrop  = true;
    private float       _currentX = 0f;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    private void Start() => ResetSpawner();

    private void Update()
    {
        if (!_canDrop) return;
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver) return;

        HandleInput();
        UpdateAimLine();
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    /// <summary>Called by GameManager.RestartGame() to re-initialise the spawner.</summary>
    public void ResetSpawner()
    {
        StopAllCoroutines();
        if (_pendingBall != null) Destroy(_pendingBall);

        _canDrop  = true;
        _currentX = 0f;
        _nextConfig = PickRandom();
        SpawnPendingBall();
    }

    // ─── Input ───────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        float targetX = _currentX;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector3 wp  = Camera.main.ScreenToWorldPoint(
                new Vector3(touch.position.x, touch.position.y, 0f));
            targetX = wp.x;

            if (touch.phase == TouchPhase.Ended)
                DropBall();
        }
#else
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        targetX = mouseWorld.x;

        if (Input.GetMouseButtonDown(0))
            DropBall();
#endif

        _currentX = Mathf.Clamp(targetX, wallLeftX, wallRightX);

        if (_pendingBall != null)
            _pendingBall.transform.position = new Vector3(_currentX, spawnY, 0f);
    }

    // ─── Drop / spawn ─────────────────────────────────────────────────────────

    private void DropBall()
    {
        if (_pendingBall == null) return;

        _canDrop = false;

        if (aimLine != null) aimLine.enabled = false;

        AudioManager.Instance?.PlayDrop();

        // Release the ball: switch to Dynamic physics
        _pendingBall.GetComponent<Ball>().SetHeld(false);
        _pendingBall = null;

        StartCoroutine(SpawnNextAfterDelay());
    }

    private IEnumerator SpawnNextAfterDelay()
    {
        yield return new WaitForSeconds(dropDelay);

        _currentConfig = _nextConfig;
        _nextConfig    = PickRandom();

        UIManager.Instance?.SetNextBall(_nextConfig);
        SpawnPendingBall();

        _canDrop = true;
    }

    private void SpawnPendingBall()
    {
        // First call from ResetSpawner: _currentConfig may still be null
        if (_currentConfig == null) _currentConfig = PickRandom();

        _pendingBall = Instantiate(
            ballPrefab,
            new Vector3(_currentX, spawnY, 0f),
            Quaternion.identity);

        Ball ball = _pendingBall.GetComponent<Ball>();
        ball.Initialize(_currentConfig, allBallConfigs);
        ball.SetHeld(true);   // Kinematic + trigger while player aims

        if (aimLine != null) aimLine.enabled = true;

        UIManager.Instance?.SetNextBall(_nextConfig);
    }

    // ─── Aim line ─────────────────────────────────────────────────────────────

    private void UpdateAimLine()
    {
        if (aimLine == null || _pendingBall == null) return;

        Vector3 start = _pendingBall.transform.position;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, new Vector3(start.x, bottomY, 0f));
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private BallConfig PickRandom()
        => allBallConfigs[Random.Range(0, Mathf.Min(spawnableMaxLevel + 1, allBallConfigs.Length))];
}
