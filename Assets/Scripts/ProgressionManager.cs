using UnityEngine;

/// <summary>
/// The ProgressionManager handles all "Meta-Game" data, meaning things that persist over long periods of time.
/// It tracks what level the player is currently on, what the highest level they've unlocked is, 
/// and securely saves/loads their high scores using Unity's PlayerPrefs system.
/// </summary>
public class ProgressionManager : MonoBehaviour
{
    // Singleton Instance pattern so any script can access the player's progression data.
    // Example: The LevelSelectUI queries this script to know which level buttons to color gray (locked) or blue (unlocked).
    public static ProgressionManager Instance { get; private set; }

    [Header("Current State")]
    // Tracks the specific level number the player has just clicked and loaded into.
    public int currentPlayingLevel = 1;
    
    // Tracks the furthest the player has ever made it in the game.
    public int highestUnlockedLevel = 1;

    // [HideInInspector] hides this variable from the Unity Editor UI so designers don't accidentally check it,
    // but keeps the variable "public" so other scripts can read/write to it.
    // We use this boolean to tell the MainMenuManager whether it should open directly to the Level Select screen or not!
    [HideInInspector]
    public bool returnToMenu = true;

    // "const" means this string is permanent and cannot be changed by the game. 
    // We use this key to safely ask the PlayerPrefs hardware for the "HighestUnlockedLevel" save file.
    private const string UNLOCKED_LEVEL_KEY = "HighestUnlockedLevel";

    /// <summary>
    /// Awake runs before the scene even finishes loading. Setting our Singleton up here is crucial.
    /// </summary>
    private void Awake()
    {
        // Standard singleton duplication prevention check
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Assign this globally.
        Instance = this;
        
        // DontDestroyOnLoad is a VERY powerful Unity command. 
        // It prevents this specific GameObject from being destroyed when the player switches scenes (like going from Menu to Level 1).
        // Thanks to this, ProgressionManager acts as our permanent memory chip across the entire game session.
        DontDestroyOnLoad(gameObject);

        // Immediately load the player's history from their hard drive.
        LoadProgression();
    }

    /// <summary>
    /// Looks into the player's save file and retrieves their highest level.
    /// </summary>
    private void LoadProgression()
    {
        // PlayerPrefs.GetInt searches the hard drive for UNLOCKED_LEVEL_KEY.
        // If it finds nothing (first time playing), it cleanly defaults to "1".
        highestUnlockedLevel = PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
        
        // We set the current level to whatever their highest level is to save them time opening the menu!
        currentPlayingLevel = highestUnlockedLevel; 
    }

    /// <summary>
    /// Call this method when a player wins a level! It checks if they deserve to unlock the next one.
    /// It is typically called by the LevelManager.
    /// </summary>
    /// <param name="level">The level number to potentially unlock.</param>
    public void UnlockLevel(int level)
    {
        // We only unlock the level if it's strictly greater than their current record!
        // We don't want them replaying Level 2 and overriding a theoretical Level 5 unlock!
        if (level > highestUnlockedLevel)
        {
            // Update the internal tracker.
            highestUnlockedLevel = level;
            
            // Save the new record directly to the hard drive.
            PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, highestUnlockedLevel);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Call this method when a level ends to check if the player set a new personal best score!
    /// </summary>
    /// <param name="level">The specific level where the score was achieved.</param>
    /// <param name="score">The final score the player got.</param>
    public void SaveHighScore(int level, int score)
    {
        // We create a unique save-file name based on the level. 
        // Example: If level is 5, the key becomes "HighScore_Level_5".
        string scoreKey = $"HighScore_Level_{level}";
        
        // Grab the OLD high score from the hard drive. If they never played before, grab 0.
        int currentHighScore = PlayerPrefs.GetInt(scoreKey, 0);

        // Compare the new score they just got against the old record.
        if (score > currentHighScore)
        {
            // They beat their record! Write the new score to the hard drive under that unique level key.
            PlayerPrefs.SetInt(scoreKey, score);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// This method is usually called by the MainMenu or LevelSelectUI to display little stars or numbers showing the player's best scores!
    /// </summary>
    /// <param name="level">The level being checked.</param>
    /// <returns>The highest score the player has on that specific level.</returns>
    public int GetHighScore(int level)
    {
        // Re-calculate the unique string (e.g. "HighScore_Level_1") and fetch it. Defaults to 0 if no record exists.
        return PlayerPrefs.GetInt($"HighScore_Level_{level}", 0);
    }
}
