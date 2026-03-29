using UnityEngine;

/// <summary>
/// Central game state: score, high score, danger-line detection, and game-over.
/// Singleton — access via GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Ball Setup")]
    public GameObject  ballPrefab;
    public BallConfig[] allBallConfigs;   // drag all 8 assets in level order (0–7)

    [Header("Rules")]
    public float dangerLineY    = 3.0f;   // balls above this Y trigger the timer
    public float dangerTimeLimit = 3.0f;  // seconds before game over

    // ─── State ───────────────────────────────────────────────────────────────
    private int   _score;
    private int   _highScore;
    private float _dangerTimer;

    public bool IsGameOver { get; private set; }

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance  = this;
        _highScore = PlayerPrefs.GetInt("HighScore", 0);
    }

    private void Start()
    {
        UIManager.Instance?.UpdateScore(0);
        UIManager.Instance?.UpdateHighScore(_highScore);
    }

    private void Update()
    {
        if (IsGameOver) return;
        CheckDangerLine();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    // ─── Danger-line detection ────────────────────────────────────────────────

    private void CheckDangerLine()
    {
        bool anyAbove = false;

        foreach (Ball b in FindObjectsByType<Ball>(FindObjectsSortMode.None))
        {
            Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
            // Skip the held/kinematic ball at the top (the one the player hasn't dropped)
            if (rb == null || rb.bodyType == RigidbodyType2D.Kinematic) continue;

            // Use top edge of ball, not just its centre
            float topY = b.transform.position.y + b.config.radius;
            if (topY > dangerLineY)
            {
                anyAbove = true;
                break;
            }
        }

        if (anyAbove)
        {
            _dangerTimer += Time.deltaTime;
            UIManager.Instance?.ShowDanger(true);
            if (_dangerTimer >= dangerTimeLimit)
                TriggerGameOver();
        }
        else
        {
            _dangerTimer = 0f;
            UIManager.Instance?.ShowDanger(false);
        }
    }

    // ─── Public API ──────────────────────────────────────────────────────────

    public void AddScore(int points)
    {
        if (IsGameOver) return;

        _score += points;
        AudioManager.Instance?.PlayScoreTick();

        if (_score > _highScore)
        {
            _highScore = _score;
            PlayerPrefs.SetInt("HighScore", _highScore);
        }

        UIManager.Instance?.UpdateScore(_score);
        UIManager.Instance?.UpdateHighScore(_highScore);
    }

    /// <summary>
    /// Called by Ball.cs after a successful merge to spawn the next-tier ball.
    /// </summary>
    public void SpawnMergedBall(BallConfig config, Vector3 position, BallConfig[] allConfigs)
    {
        if (IsGameOver) return;

        // Special fanfare for max-tier (Basketball) merge
        if (config.levelIndex == allBallConfigs.Length - 1)
            AudioManager.Instance?.PlayBasketballMerge();

        GameObject go = Instantiate(ballPrefab, position, Quaternion.identity);
        go.GetComponent<Ball>().Initialize(config, allConfigs);
    }

    public void RestartGame()
    {
        // Destroy every ball currently in the scene
        foreach (Ball b in FindObjectsByType<Ball>(FindObjectsSortMode.None))
            Destroy(b.gameObject);

        _score       = 0;
        IsGameOver   = false;
        _dangerTimer = 0f;

        UIManager.Instance?.UpdateScore(0);
        UIManager.Instance?.HideGameOver();
        UIManager.Instance?.ShowDanger(false);

        FindFirstObjectByType<BallSpawner>()?.ResetSpawner();
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private void TriggerGameOver()
    {
        IsGameOver = true;

        // Freeze all live balls in place
        foreach (Ball b in FindObjectsByType<Ball>(FindObjectsSortMode.None))
        {
            Rigidbody2D rb = b.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = false;
        }

        AudioManager.Instance?.PlayGameOver();
        UIManager.Instance?.ShowGameOver(_score, _highScore);
    }
}
