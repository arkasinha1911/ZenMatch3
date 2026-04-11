using UnityEngine;
using TMPro; // TextMeshPro namespace

public class ScoreManager : MonoBehaviour
{
    // Singleton pattern allows other scripts to easily add to the score
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Drag the TextMeshPro UI element here to display the score.")]
    public TextMeshProUGUI scoreText;

    private int currentScore = 0;
    public int CurrentScore => currentScore;

    private void Awake()
    {
        // Simple Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        UpdateScoreUI();
    }

    /// <summary>
    /// Adds points to the player's total score and updates the UI.
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    // You can call this if you need to reset the score via other scripts
    public void ResetScore()
    {
        currentScore = 0;
        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {currentScore}";
        }
    }
}
