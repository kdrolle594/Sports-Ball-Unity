using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives all HUD and overlay UI.
/// Attach to the Canvas GameObject and wire up references in the Inspector.
/// Singleton — access via UIManager.Instance.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // ─── HUD ──────────────────────────────────────────────────────────────────
    [Header("HUD")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;

    [Header("Next Ball Preview")]
    public Image  nextBallImage;     // Image component whose color + size we change
    public Sprite circleSpriteRef;   // drag Circle.png here for the preview icon

    // ─── Danger feedback ─────────────────────────────────────────────────────
    [Header("Danger")]
    [Tooltip("A semi-transparent red full-screen Image. Toggled on/off by CheckDangerLine.")]
    public GameObject dangerFlash;

    // ─── Game Over panel ─────────────────────────────────────────────────────
    [Header("Game Over")]
    public GameObject      gameOverPanel;
    public TextMeshProUGUI gameOverScoreText;
    public TextMeshProUGUI gameOverHighText;
    public Button          restartButton;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (dangerFlash   != null) dangerFlash.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
    }

    // ─── Score ────────────────────────────────────────────────────────────────

    public void UpdateScore(int score)
    {
        if (scoreText != null)
            scoreText.text = $"Score\n{score}";
    }

    public void UpdateHighScore(int score)
    {
        if (highScoreText != null)
            highScoreText.text = $"Best\n{score}";
    }

    // ─── Next-ball preview ────────────────────────────────────────────────────

    public void SetNextBall(BallConfig cfg)
    {
        if (cfg == null || nextBallImage == null) return;

        // Tint the preview circle to match the upcoming ball's color
        nextBallImage.color = cfg.colorTint;

        // Scale the preview icon proportionally (smallest = 48 px, largest = 96 px)
        float maxRadius = 0.95f; // Basketball radius
        float size = Mathf.Lerp(48f, 96f, cfg.radius / maxRadius);
        nextBallImage.rectTransform.sizeDelta = new Vector2(size, size);
    }

    // ─── Danger indicator ────────────────────────────────────────────────────

    public void ShowDanger(bool show)
    {
        if (dangerFlash != null)
            dangerFlash.SetActive(show);
    }

    // ─── Game Over ────────────────────────────────────────────────────────────

    public void ShowGameOver(int score, int highScore)
    {
        if (gameOverPanel == null) return;

        gameOverPanel.SetActive(true);

        if (gameOverScoreText != null)
            gameOverScoreText.text = $"Score: {score}";

        if (gameOverHighText != null)
            gameOverHighText.text = $"Best: {highScore}";
    }

    public void HideGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
}
