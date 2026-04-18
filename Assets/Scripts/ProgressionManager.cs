using UnityEngine;
using System;

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

    [Header("Lives System")]
    public const int MAX_LIVES = 6;
    public int currentLives { get; private set; } = 6;
    
    private const string LIVES_KEY = "PlayerLives";
    private const string LIVES_TIMESTAMP_KEY = "LivesTimestamp";
    
    /// <summary>
    /// How many real-world seconds between each life regeneration.
    /// 1800 seconds = 30 minutes.
    /// </summary>
    public const int LIFE_REGEN_SECONDS = 1800;

    [Header("Currency & Shop")]
    public int TotalStars { get; private set; } = 0;
    public int MagnetCount { get; private set; } = 0;
    public int XBombCount { get; private set; } = 0;
    public int AreaBombCount { get; private set; } = 0;

    private const string STARS_KEY = "PlayerStars";
    private const string MAGNET_KEY = "MagnetCount";
    private const string XBOMB_KEY = "XBombCount";
    private const string AREABOMB_KEY = "AreaBombCount";

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
        
        // Load the stored lives, default to MAX_LIVES if it's their first time playing.
        currentLives = PlayerPrefs.GetInt(LIVES_KEY, MAX_LIVES);

        // Load Shop & Currency
        TotalStars = PlayerPrefs.GetInt(STARS_KEY, 0);
        MagnetCount = PlayerPrefs.GetInt(MAGNET_KEY, 0);
        XBombCount = PlayerPrefs.GetInt(XBOMB_KEY, 0);
        AreaBombCount = PlayerPrefs.GetInt(AREABOMB_KEY, 0);

        // Regenerate any lives that accumulated while the game was closed!
        RegenerateOfflineLives();

        // We set the current level to whatever their highest level is to save them time opening the menu!
        currentPlayingLevel = highestUnlockedLevel; 
    }

    /// <summary>
    /// Checks how much real time has passed since lives dropped below max,
    /// and grants 1 life per 30 minutes that elapsed (even while the app was closed).
    /// </summary>
    private void RegenerateOfflineLives()
    {
        if (currentLives >= MAX_LIVES) return; // Already full, nothing to regenerate.

        string savedTimestamp = PlayerPrefs.GetString(LIVES_TIMESTAMP_KEY, "");
        if (string.IsNullOrEmpty(savedTimestamp)) return; // No timestamp saved, skip.

        DateTime lastLossTime;
        if (!DateTime.TryParse(savedTimestamp, out lastLossTime)) return; // Corrupted timestamp, skip.

        double secondsElapsed = (DateTime.UtcNow - lastLossTime).TotalSeconds;
        int livesToAdd = (int)(secondsElapsed / LIFE_REGEN_SECONDS);

        if (livesToAdd > 0)
        {
            currentLives = Mathf.Min(currentLives + livesToAdd, MAX_LIVES);
            PlayerPrefs.SetInt(LIVES_KEY, currentLives);

            if (currentLives >= MAX_LIVES)
            {
                // Fully regenerated! Clear the timestamp.
                PlayerPrefs.DeleteKey(LIVES_TIMESTAMP_KEY);
            }
            else
            {
                // Still not full — advance the saved timestamp by however many lives were granted
                // so the "remainder" time carries forward correctly.
                DateTime advancedTime = lastLossTime.AddSeconds(livesToAdd * LIFE_REGEN_SECONDS);
                PlayerPrefs.SetString(LIVES_TIMESTAMP_KEY, advancedTime.ToString("o"));
            }
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Called every frame by the UI to tick the live regeneration timer in real-time.
    /// Grants 1 life when the timer reaches 0 and restarts the cycle.
    /// </summary>
    private void Update()
    {
        if (currentLives < MAX_LIVES)
        {
            string savedTimestamp = PlayerPrefs.GetString(LIVES_TIMESTAMP_KEY, "");
            if (!string.IsNullOrEmpty(savedTimestamp))
            {
                DateTime lastLossTime;
                if (DateTime.TryParse(savedTimestamp, out lastLossTime))
                {
                    double secondsElapsed = (DateTime.UtcNow - lastLossTime).TotalSeconds;
                    if (secondsElapsed >= LIFE_REGEN_SECONDS)
                    {
                        // Grant a life in real-time!
                        currentLives = Mathf.Min(currentLives + 1, MAX_LIVES);
                        PlayerPrefs.SetInt(LIVES_KEY, currentLives);

                        if (currentLives >= MAX_LIVES)
                        {
                            PlayerPrefs.DeleteKey(LIVES_TIMESTAMP_KEY);
                        }
                        else
                        {
                            // Advance timestamp for the next cycle
                            DateTime advancedTime = lastLossTime.AddSeconds(LIFE_REGEN_SECONDS);
                            PlayerPrefs.SetString(LIVES_TIMESTAMP_KEY, advancedTime.ToString("o"));
                        }
                        PlayerPrefs.Save();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Returns the number of seconds remaining until the next life regenerates.
    /// Returns 0 if lives are already full.
    /// </summary>
    public int GetSecondsUntilNextLife()
    {
        if (currentLives >= MAX_LIVES) return 0;

        string savedTimestamp = PlayerPrefs.GetString(LIVES_TIMESTAMP_KEY, "");
        if (string.IsNullOrEmpty(savedTimestamp)) return LIFE_REGEN_SECONDS;

        DateTime lastLossTime;
        if (!DateTime.TryParse(savedTimestamp, out lastLossTime)) return LIFE_REGEN_SECONDS;

        double secondsElapsed = (DateTime.UtcNow - lastLossTime).TotalSeconds;
        int remaining = LIFE_REGEN_SECONDS - (int)secondsElapsed;
        return Mathf.Max(0, remaining);
    }

    /// <summary>
    /// Call this when the player fails a level.
    /// </summary>
    public void LoseLife()
    {
        bool wasFullBefore = (currentLives >= MAX_LIVES);
        currentLives--;
        if (currentLives < 0) currentLives = 0;
        PlayerPrefs.SetInt(LIVES_KEY, currentLives);

        // If this is the first life lost from a full tank, stamp the current UTC time.
        // This starts the regeneration countdown clock.
        if (wasFullBefore && currentLives < MAX_LIVES)
        {
            PlayerPrefs.SetString(LIVES_TIMESTAMP_KEY, DateTime.UtcNow.ToString("o"));
        }
        // If there was no timestamp yet (edge case), set one now
        else if (string.IsNullOrEmpty(PlayerPrefs.GetString(LIVES_TIMESTAMP_KEY, "")))
        {
            PlayerPrefs.SetString(LIVES_TIMESTAMP_KEY, DateTime.UtcNow.ToString("o"));
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Call this to refill the player's lives back to max (e.g. for testing or waiting).
    /// </summary>
    public void RefillLives()
    {
        currentLives = MAX_LIVES;
        PlayerPrefs.SetInt(LIVES_KEY, currentLives);
        PlayerPrefs.DeleteKey(LIVES_TIMESTAMP_KEY); // Clear regen timer since we're full
        PlayerPrefs.Save();
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

    /// <summary>
    /// Adds stars to the player's bank and saves safely.
    /// </summary>
    public void AddStars(int amount)
    {
        TotalStars += amount;
        PlayerPrefs.SetInt(STARS_KEY, TotalStars);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Deducts stars and adds an item to inventory if affordable.
    /// </summary>
    public bool BuyItem(string itemType, int cost)
    {
        if (TotalStars >= cost)
        {
            TotalStars -= cost;
            PlayerPrefs.SetInt(STARS_KEY, TotalStars);
            
            if (itemType == "Magnet") { MagnetCount++; PlayerPrefs.SetInt(MAGNET_KEY, MagnetCount); }
            else if (itemType == "XBomb") { XBombCount++; PlayerPrefs.SetInt(XBOMB_KEY, XBombCount); }
            else if (itemType == "AreaBomb") { AreaBombCount++; PlayerPrefs.SetInt(AREABOMB_KEY, AreaBombCount); }
            
            PlayerPrefs.Save();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Spends an item from inventory to spawn it on the board.
    /// </summary>
    public void ConsumeItem(string itemType)
    {
        if (itemType == "Magnet" && MagnetCount > 0) { MagnetCount--; PlayerPrefs.SetInt(MAGNET_KEY, MagnetCount); }
        else if (itemType == "XBomb" && XBombCount > 0) { XBombCount--; PlayerPrefs.SetInt(XBOMB_KEY, XBombCount); }
        else if (itemType == "AreaBomb" && AreaBombCount > 0) { AreaBombCount--; PlayerPrefs.SetInt(AREABOMB_KEY, AreaBombCount); }
        PlayerPrefs.Save();
    }

    public void SaveLevelStars(int level, int stars)
    {
        string key = $"LevelStars_{level}";
        int previousStars = PlayerPrefs.GetInt(key, 0);
        if (stars > previousStars)
        {
            PlayerPrefs.SetInt(key, stars);
            PlayerPrefs.Save();
        }
    }

    public int GetLevelStars(int level)
    {
        return PlayerPrefs.GetInt($"LevelStars_{level}", 0);
    }
}
