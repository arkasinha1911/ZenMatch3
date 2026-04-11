using UnityEngine;
using TMPro; // TextMeshPro namespace is required to interact with advanced text UI components

/// <summary>
/// The ScoreManager class is responsible for tracking the player's score during gameplay.
/// It uses a "Singleton" pattern, meaning there is only ever ONE active ScoreManager in the entire game.
/// Any other script (like GridSpawner) can easily add points by saying "ScoreManager.Instance.AddScore(10)".
/// </summary>
public class ScoreManager : MonoBehaviour
{
    // The static 'Instance' variable is what creates the Singleton pattern.
    // 'static' means it belongs to the class itself, not any single object.
    // 'public get' means other scripts can read it, 'private set' means only THIS script can define it.
    public static ScoreManager Instance { get; private set; }

    [Header("UI References")]
    [Tooltip("Drag the TextMeshPro UI element here to display the score. This connects the code to the visual text on screen.")]
    public TextMeshProUGUI scoreText;

    // This private integer stores the actual raw number of the player's score.
    // It's private so other scripts can't accidentally overwrite it directly!
    private int currentScore = 0;

    // This is a public "PropertyName" that acts as a read-only window into the private currentScore variable.
    // Other scripts can ask "What is the CurrentScore?" but they cannot change it.
    public int CurrentScore => currentScore;

    /// <summary>
    /// Awake is called before the game officially starts (even before Start). 
    /// It's the perfect place to set up crucial core systems like our Singleton.
    /// </summary>
    private void Awake()
    {
        // We check if an Instance already exists. If it does, and it's NOT this specific object...
        if (Instance != null && Instance != this)
        {
            // ...we destroy this duplicate object to ensure there is only ever ONE ScoreManager in the game!
            Destroy(gameObject);
        }
        else
        {
            // If no instance exists yet, we declare THIS script as the official permanent Instance.
            Instance = this;
        }
    }

    /// <summary>
    /// Start is called once on the very first frame the script is active.
    /// We use it to ensure the UI starts by displaying "Score: 0" instead of blank text.
    /// </summary>
    private void Start()
    {
        // Immediately refresh the visual UI text as soon as the level loads.
        UpdateScoreUI();
    }

    /// <summary>
    /// This method is public, meaning other scripts can call it. 
    /// It gets called primarily by the GridSpawner when matches are made and pieces are destroyed!
    /// </summary>
    /// <param name="amount">The number of points to grant the player.</param>
    public void AddScore(int amount)
    {
        // Increase our internal score tracker by the inputted amount.
        currentScore += amount;

        // Since the internal number changed, we MUST update the visual text on the screen so the player sees it!
        UpdateScoreUI();
    }

    /// <summary>
    /// This public method resets the score back to zero.
    /// It can be called by the LevelManager when a player restarts a level or plays a new one.
    /// </summary>
    public void ResetScore()
    {
        // Force the internal count to 0.
        currentScore = 0;

        // Update the visual UI text so the screen shows "Score: 0".
        UpdateScoreUI();
    }

    /// <summary>
    /// This private method handles the physical updating of the TextMeshPro text on the canvas.
    /// </summary>
    private void UpdateScoreUI()
    {
        // Always check if scoreText is NOT null. If it is null, it means we forgot to drag the UI element into the Inspector!
        // This check prevents the game from crashing if the connection is missing.
        if (scoreText != null)
        {
            // We use string interpolation (the $ symbol) to easily inject the currentScore number into a string of text.
            scoreText.text = $"Score: {currentScore}";
        }
    }
}
