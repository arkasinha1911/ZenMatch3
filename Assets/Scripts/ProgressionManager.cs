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

        if (PlayerPrefs.GetInt("ReceivedStarterPack", 0) == 0)
        {
            MagnetCount = PlayerPrefs.GetInt(MAGNET_KEY, 0) + 2;
            XBombCount = PlayerPrefs.GetInt(XBOMB_KEY, 0) + 2;
            AreaBombCount = PlayerPrefs.GetInt(AREABOMB_KEY, 0) + 2;
            
            PlayerPrefs.SetInt(MAGNET_KEY, MagnetCount);
            PlayerPrefs.SetInt(XBOMB_KEY, XBombCount);
            PlayerPrefs.SetInt(AREABOMB_KEY, AreaBombCount);
            PlayerPrefs.SetInt("ReceivedStarterPack", 1);
            PlayerPrefs.Save();
        }
        else
        {
            MagnetCount = PlayerPrefs.GetInt(MAGNET_KEY, 0);
            XBombCount = PlayerPrefs.GetInt(XBOMB_KEY, 0);
            AreaBombCount = PlayerPrefs.GetInt(AREABOMB_KEY, 0);
        }

        // Regenerate any lives that accumulated while the game was closed!
        RegenerateOfflineLives();

        // We set the current level to whatever their highest level is to save them time opening the menu!
        currentPlayingLevel = highestUnlockedLevel; 
    }

    private float activeRegenTimer = 0f;
    private int suspendTickCount = 0;

    /// <summary>
    /// Checks how much real time has passed since lives dropped below max.
    /// We use Environment.TickCount to track device uptime, which CANNOT be spoofed 
    /// by changing the device clock!
    /// </summary>
    private void RegenerateOfflineLives()
    {
        if (currentLives >= MAX_LIVES) return; 

        activeRegenTimer = PlayerPrefs.GetFloat("ActiveRegenTimer", LIFE_REGEN_SECONDS);

        // Check if we saved a TickCount from a previous session
        int lastSavedTick = PlayerPrefs.GetInt("LastTickCount", Environment.TickCount);
        int currentTick = Environment.TickCount;

        // Calculate ticks passed. If the device was rebooted, currentTick will be smaller than lastSavedTick.
        // In that case, we cannot safely calculate time passed without internet, so we grant 0 offline time to prevent device clock spoofing.
        if (currentTick >= lastSavedTick)
        {
            float secondsPassed = (currentTick - lastSavedTick) / 1000f;
            ProcessPassedSeconds(secondsPassed);
        }
        else
        {
            // Device was rebooted. We ignore offline time to satisfy the strict anti-cheat requirement offline.
        }
    }

    private void ProcessPassedSeconds(float secondsPassed)
    {
        activeRegenTimer -= secondsPassed;
        if (activeRegenTimer < 0) activeRegenTimer = 0;

        // Calculate how many lives should still be missing based on remaining time
        int livesStillMissing = Mathf.CeilToInt(activeRegenTimer / LIFE_REGEN_SECONDS);
        int targetLives = MAX_LIVES - livesStillMissing;

        if (targetLives > currentLives)
        {
            currentLives = Mathf.Min(targetLives, MAX_LIVES);
            PlayerPrefs.SetInt(LIVES_KEY, currentLives);
        }

        if (currentLives >= MAX_LIVES)
            activeRegenTimer = 0;

        PlayerPrefs.SetFloat("ActiveRegenTimer", activeRegenTimer);
        PlayerPrefs.SetInt("LastTickCount", Environment.TickCount);
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (isPaused)
        {
            suspendTickCount = Environment.TickCount;
            PlayerPrefs.SetFloat("ActiveRegenTimer", activeRegenTimer);
            PlayerPrefs.SetInt("LastTickCount", suspendTickCount);
            PlayerPrefs.Save();
        }
        else
        {
            if (suspendTickCount != 0)
            {
                int currentTick = Environment.TickCount;
                if (currentTick >= suspendTickCount)
                {
                    float secondsPassed = (currentTick - suspendTickCount) / 1000f;
                    ProcessPassedSeconds(secondsPassed);
                }
                suspendTickCount = 0;
            }
        }
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetFloat("ActiveRegenTimer", activeRegenTimer);
        PlayerPrefs.SetInt("LastTickCount", Environment.TickCount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Ticks the cumulative regen timer using unscaledDeltaTime.
    /// Grants a life every time a 30-minute chunk completes.
    /// </summary>
    private void Update()
    {
        if (currentLives < MAX_LIVES)
        {
            activeRegenTimer -= Time.unscaledDeltaTime;
            if (activeRegenTimer < 0) activeRegenTimer = 0;

            // How many lives should still be missing based on remaining time
            int livesStillMissing = Mathf.CeilToInt(activeRegenTimer / LIFE_REGEN_SECONDS);
            int targetLives = MAX_LIVES - livesStillMissing;

            if (targetLives > currentLives)
            {
                currentLives = Mathf.Min(targetLives, MAX_LIVES);
                PlayerPrefs.SetInt(LIVES_KEY, currentLives);

                if (currentLives >= MAX_LIVES)
                    activeRegenTimer = 0;

                PlayerPrefs.SetFloat("ActiveRegenTimer", activeRegenTimer);
                PlayerPrefs.SetInt("LastTickCount", Environment.TickCount);
                PlayerPrefs.Save();
            }
        }
    }

    /// <summary>
    /// Returns seconds remaining until the NEXT single life regenerates.
    /// </summary>
    public int GetSecondsUntilNextLife()
    {
        if (currentLives >= MAX_LIVES) return 0;
        int missingLives = MAX_LIVES - currentLives;
        float nextLifeThreshold = (missingLives - 1) * LIFE_REGEN_SECONDS;
        return Mathf.Max(0, (int)(activeRegenTimer - nextLifeThreshold));
    }

    /// <summary>
    /// Returns total seconds remaining until ALL missing lives are regenerated.
    /// </summary>
    public int GetTotalSecondsUntilFullLives()
    {
        if (currentLives >= MAX_LIVES) return 0;
        return Mathf.Max(0, (int)activeRegenTimer);
    }

    /// <summary>
    /// Call this when the player fails a level.
    /// Each lost life adds 30 minutes to the regeneration timer.
    /// </summary>
    public void LoseLife()
    {
        bool wasFullBefore = (currentLives >= MAX_LIVES);
        currentLives--;
        if (currentLives < 0) currentLives = 0;
        PlayerPrefs.SetInt(LIVES_KEY, currentLives);

        if (wasFullBefore)
        {
            // First life lost — start a fresh 30-minute timer
            activeRegenTimer = LIFE_REGEN_SECONDS;
        }
        else
        {
            // Already regenerating — stack another 30 minutes on top
            activeRegenTimer += LIFE_REGEN_SECONDS;
        }

        PlayerPrefs.SetFloat("ActiveRegenTimer", activeRegenTimer);
        PlayerPrefs.SetInt("LastTickCount", Environment.TickCount);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Call this to refill the player's lives back to max.
    /// </summary>
    public void RefillLives()
    {
        currentLives = MAX_LIVES;
        activeRegenTimer = 0;
        PlayerPrefs.SetInt(LIVES_KEY, currentLives);
        PlayerPrefs.SetFloat("ActiveRegenTimer", 0);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Grants exactly 1 extra life (up to the maximum) to the player.
    /// Usually granted after watching a Rewarded Ad.
    /// </summary>
    public void GiveOneLife()
    {
        if (currentLives >= MAX_LIVES) return;
        
        currentLives++;
        PlayerPrefs.SetInt(LIVES_KEY, currentLives);
        
        // Decrease the timer by 30 minutes since we just gained a life!
        activeRegenTimer -= LIFE_REGEN_SECONDS;
        if (activeRegenTimer < 0) activeRegenTimer = 0;
        PlayerPrefs.SetFloat("ActiveRegenTimer", activeRegenTimer);
        
        if (currentLives >= MAX_LIVES)
        {
            activeRegenTimer = 0;
        }
        
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

    public void ResetPowerups()
    {
        MagnetCount = 2;
        XBombCount = 2;
        AreaBombCount = 2;
        
        PlayerPrefs.SetInt(MAGNET_KEY, MagnetCount);
        PlayerPrefs.SetInt(XBOMB_KEY, XBombCount);
        PlayerPrefs.SetInt(AREABOMB_KEY, AreaBombCount);
        PlayerPrefs.SetInt("ReceivedStarterPack", 1);
        PlayerPrefs.Save();
    }

    public void UnlockAllLevels(int maxLevels = 500)
    {
        highestUnlockedLevel = maxLevels;
        PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, maxLevels);
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
