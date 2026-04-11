using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement; // Allows us to tell Unity to load new map levels!

/// <summary>
/// This script controls the "Level Select" menu grid. Instead of manually putting 50 buttons in the scene,
/// this script programmatically SPAWNS the buttons dynamically, colors them gray if they are locked, 
/// and wires them up so clicking one instantly loads the game!
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The parent container folder (usually a Scroll View Content box) where the buttons will be physically spawned.")]
    public Transform buttonContainer;

    [Tooltip("The blueprint 'Prefab' representing a single button. It must have the 'LevelButton' script attached so we can talk to it!")]
    public LevelButton levelButtonPrefab;

    [Tooltip("The literal spelling of the game scene we want to load when a player clicks a button.")]
    public string gameplaySceneName = "SampleScene";

    /// <summary>
    /// OnEnable runs every single time this menu pops up on the screen!
    /// By regenerating the buttons every time we open the menu, it ensures the buttons cleanly update 
    /// from "Locked" to "Unlocked" if the player just beat a level!
    /// </summary>
    private void OnEnable()
    {
        GenerateLevelButtons();
    }

    private void GenerateLevelButtons()
    {
        // 1. CLEAR OLD BUTTONS
        // If we open the menu multiple times, we don't want 500 overlapping buttons. 
        // So we destroy every button currently sitting in the container first.
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // 2. CHECK PROGRESS
        // Ask the memory manager what the highest unlocked level record currently is.
        int highestUnlocked = 1; // Default to 1
        if (ProgressionManager.Instance != null)
        {
            highestUnlocked = ProgressionManager.Instance.highestUnlockedLevel;
        }

        // 3. DECIDE HOW MANY TO SPAWN
        // Instead of spawning 1000 buttons, we only show however many they unlocked, plus a teasing "preview" of the next 10 locked levels.
        int totalToSpawn = highestUnlocked + 10;

        // 4. THE SPAWNING LOOP
        for (int i = 1; i <= totalToSpawn; i++)
        {
            // Create a physical clone of the button prefab, put it inside the buttonContainer layout group!
            LevelButton newButton = Instantiate(levelButtonPrefab, buttonContainer);
            int currentLevelToSetup = i;

            // A level is only Unlocked if its number is equal to or less than their highest record!
            bool isUnlocked = (currentLevelToSetup <= highestUnlocked);

            // A. Populate the Button Text
            if (newButton.levelText != null)
            {
                newButton.levelText.text = $"Level {currentLevelToSetup}";
                
                // If it is locked, physically append the word "(Locked)" to the UI text.
                if (!isUnlocked) newButton.levelText.text += " (Locked)";
            }

            // B. Populate the High Score Text
            if (newButton.scoreText != null)
            {
                if (isUnlocked && ProgressionManager.Instance != null)
                {
                    // Fetch the unique high score for this exact level number and display it.
                    int score = ProgressionManager.Instance.GetHighScore(currentLevelToSetup);
                    newButton.scoreText.text = $"High Score: {score}";
                }
                else
                {
                    // If they haven't unlocked it yet, obscure the high score with question marks!
                    newButton.scoreText.text = "High Score: ???";
                }
            }

            // C. Button Click Logic
            if (newButton.button != null)
            {
                // This Unity feature physically disables clicking. It instantly turns the button gray and prevents input!
                newButton.button.interactable = isUnlocked;

                // If it IS unlocked...
                if (isUnlocked)
                {
                    // This is an advanced "Lambda" feature: `() =>` 
                    // It securely wires the button to run the `OnLevelButtonClicked` command secretly passing in its specific level number!
                    newButton.button.onClick.AddListener(() => OnLevelButtonClicked(currentLevelToSetup));
                }
            }
        }
    }

    /// <summary>
    /// Triggered exclusively when a player clicks a specific Unlocked Level Button!
    /// </summary>
    /// <param name="level">The number of the button they clicked.</param>
    private void OnLevelButtonClicked(int level)
    {
        if (ProgressionManager.Instance != null)
        {
            // Tell the permanent memory system what level we are attempting to play!
            ProgressionManager.Instance.currentPlayingLevel = level;
            
            // Failsafe: Make absolutely sure the time speed is perfectly normal (not frozen at Game Over).
            Time.timeScale = 1f;
            
            // Clear prior seeds. If the player plays Level 5, goes to the menu, and clicks Level 2,
            // we do NOT want Level 2 forcing them to replay the layout from Level 5!
            LevelManager.forcedTargetPieceTypes = null;
            GridSpawner.forcedNoiseOffset = null;
            
            // Signal to the MainMenuManager that when the scene loads, it should instantly hide itself.
            ProgressionManager.Instance.returnToMenu = false;
            
            // Command Unity to physically load the gameplay arena map!
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            // If the Menu is broken, developer logging helps identify the problem.
            Debug.LogError("No ProgressionManager found in the scene! Cannot set level.");
        }
    }
}
