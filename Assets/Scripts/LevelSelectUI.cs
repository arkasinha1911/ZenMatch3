using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent container (e.g. a Scroll View Content object) where buttons will spawn.")]
    public Transform buttonContainer;

    [Tooltip("A Prefab that contains a Button and text fields. Needs the 'LevelButton' script attached to it.")]
    public LevelButton levelButtonPrefab;

    [Tooltip("The exact name or index of your gameplay scene to load.")]
    public string gameplaySceneName = "SampleScene";

    private void OnEnable()
    {
        GenerateLevelButtons();
    }

    private void GenerateLevelButtons()
    {
        // Clear existing children (editor placeholders, etc)
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // We need ProgressionManager to exist!
        int highestUnlocked = 1;
        if (ProgressionManager.Instance != null)
        {
            highestUnlocked = ProgressionManager.Instance.highestUnlockedLevel;
        }

        // Spawn buttons for all unlocked levels PLUS 10 locked ones!
        int totalToSpawn = highestUnlocked + 10;

        for (int i = 1; i <= totalToSpawn; i++)
        {
            LevelButton newButton = Instantiate(levelButtonPrefab, buttonContainer);
            int currentLevelToSetup = i;

            bool isUnlocked = (currentLevelToSetup <= highestUnlocked);

            // Populate Text
            if (newButton.levelText != null)
            {
                newButton.levelText.text = $"Level {currentLevelToSetup}";
                if (!isUnlocked) newButton.levelText.text += " (Locked)";
            }

            // Populate Score
            if (newButton.scoreText != null)
            {
                if (isUnlocked && ProgressionManager.Instance != null)
                {
                    int score = ProgressionManager.Instance.GetHighScore(currentLevelToSetup);
                    newButton.scoreText.text = $"High Score: {score}";
                }
                else
                {
                    newButton.scoreText.text = "High Score: ???";
                }
            }

            // Button Interactability
            if (newButton.button != null)
            {
                newButton.button.interactable = isUnlocked;

                if (isUnlocked)
                {
                    newButton.button.onClick.AddListener(() => OnLevelButtonClicked(currentLevelToSetup));
                }
            }
        }
    }

    private void OnLevelButtonClicked(int level)
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentPlayingLevel = level;
            
            // Unpause completely in case previous game over paused timescale
            Time.timeScale = 1f;
            
            // Clear prior seeds to prevent map copying from earlier session
            LevelManager.forcedTargetPieceTypes = null;
            
            ProgressionManager.Instance.returnToMenu = false;
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogError("No ProgressionManager found in the scene! Cannot set level.");
        }
    }
}
