using UnityEngine;

public class ProgressionManager : MonoBehaviour
{
    public static ProgressionManager Instance { get; private set; }

    [Header("Current State")]
    public int currentPlayingLevel = 1;
    public int highestUnlockedLevel = 1;

    private const string UNLOCKED_LEVEL_KEY = "HighestUnlockedLevel";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadProgression();
    }

    private void LoadProgression()
    {
        // Default to level 1 if no save exists
        highestUnlockedLevel = PlayerPrefs.GetInt(UNLOCKED_LEVEL_KEY, 1);
        currentPlayingLevel = highestUnlockedLevel; // Default the selected level to the highest
    }

    public void UnlockLevel(int level)
    {
        if (level > highestUnlockedLevel)
        {
            highestUnlockedLevel = level;
            PlayerPrefs.SetInt(UNLOCKED_LEVEL_KEY, highestUnlockedLevel);
            PlayerPrefs.Save();
        }
    }

    public void SaveHighScore(int level, int score)
    {
        string scoreKey = $"HighScore_Level_{level}";
        int currentHighScore = PlayerPrefs.GetInt(scoreKey, 0);

        if (score > currentHighScore)
        {
            PlayerPrefs.SetInt(scoreKey, score);
            PlayerPrefs.Save();
        }
    }

    public int GetHighScore(int level)
    {
        return PlayerPrefs.GetInt($"HighScore_Level_{level}", 0);
    }
}
