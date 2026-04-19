using UnityEngine;

/// <summary>
/// The ScoreManager class is responsible for tracking the player's score during gameplay.
/// It uses a "Singleton" pattern, meaning there is only ever ONE active ScoreManager in the entire game.
/// Any other script (like GridSpawner) can easily add points by saying "ScoreManager.Instance.AddScore(10)".
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int currentScore = 0;

    /// <summary>
    /// Read-only access to the current score for other scripts.
    /// </summary>
    public int CurrentScore => currentScore;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Adds points to the player's score. Called by GridSpawner when matches are made.
    /// </summary>
    public void AddScore(int amount)
    {
        currentScore += amount;
    }
}
