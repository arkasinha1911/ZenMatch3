using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Generates and populates VisualElement buttons based on player progression levels.
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("UI Toolkit References")]
    public UIDocument uiDocument;
    
    [Tooltip("The UXML template representing a single Level Button (LevelButtonTemplate.uxml)")]
    public VisualTreeAsset levelButtonTemplate;

    public string gameplaySceneName = "SampleScene";

    /// <summary>
    /// Now called securely by MainMenuManager when the player opens the Level Select Screen.
    /// </summary>
    public void GenerateLevelButtons()
    {
        if (uiDocument == null || levelButtonTemplate == null) return;
        
        var root = uiDocument.rootVisualElement;
        var scrollView = root.Q<ScrollView>("levelScrollView");
        if (scrollView == null) return;

        // 1. Clear old UI
        scrollView.Clear();

        // 2. Data Lookup
        int highestUnlocked = 1;
        if (ProgressionManager.Instance != null)
        {
            highestUnlocked = ProgressionManager.Instance.highestUnlockedLevel;
        }

        int totalToSpawn = highestUnlocked + 10;

        // 3. Spawn Buttons from the UXML Template
        for (int i = 1; i <= totalToSpawn; i++)
        {
            // Clone the layout structure
            TemplateContainer newButton = levelButtonTemplate.Instantiate();
            
            // Extract the interactable core parts
            Button buttonComponent = newButton.Q<Button>("RootLevelButton");
            Label levelText = newButton.Q<Label>("levelText");
            Label scoreText = newButton.Q<Label>("scoreText");
            VisualElement starsContainer = newButton.Q<VisualElement>("starsContainer");

            int currentLevelToSetup = i;
            bool isUnlocked = (currentLevelToSetup <= highestUnlocked);
            bool hasLives = ProgressionManager.Instance != null && ProgressionManager.Instance.currentLives > 0;

            // Populate Text
            if (levelText != null)
            {
                levelText.text = $"Level {currentLevelToSetup}";
                if (!isUnlocked) levelText.text += " (Locked)";
            }

            // Populate Score and Stars
            int stars = 0;
            if (isUnlocked && ProgressionManager.Instance != null)
            {
                int score = ProgressionManager.Instance.GetHighScore(currentLevelToSetup);
                stars = ProgressionManager.Instance.GetLevelStars(currentLevelToSetup);
                if (scoreText != null) scoreText.text = $"High Score: {score}";
            }
            else
            {
                if (scoreText != null) scoreText.text = "High Score: ???";
            }

            // Visually fill stars
            if (starsContainer != null)
            {
                for (int s = 1; s <= 3; s++)
                {
                    Label starLabel = starsContainer.Q<Label>($"star{s}");
                    if (starLabel != null)
                    {
                        starLabel.text = s <= stars ? "\u2605" : "\u2606"; // ★ vs ☆
                        starLabel.style.color = s <= stars
                            ? new StyleColor(new Color(1f, 0.84f, 0f))   // Bright gold
                            : new StyleColor(new Color(0.5f, 0.5f, 0.5f)); // Dim grey
                    }
                }
            }

            // Action Wiring and Styling
            if (buttonComponent != null)
            {
                buttonComponent.SetEnabled(isUnlocked); // Replaces standard interactable property
                
                if (currentLevelToSetup % 10 == 0)
                {
                    // Challenge Level! Gold button
                    buttonComponent.style.backgroundColor = new StyleColor(new Color(0.9f, 0.7f, 0f));
                }
                else
                {
                    // Shift background color hue based on the group (every 10 levels)
                    int groupIndex = (currentLevelToSetup - 1) / 10;
                    float hue = (groupIndex * 0.15f) % 1f; // Shift hue by 15% per group
                    buttonComponent.style.backgroundColor = new StyleColor(Color.HSVToRGB(hue, 0.6f, 0.8f));
                }

                if (isUnlocked)
                {
                    buttonComponent.clicked += () => 
                    {
                        if (ProgressionManager.Instance != null && ProgressionManager.Instance.currentLives <= 0)
                        {
                            Debug.LogWarning("NO LIVES! GO RESET THEM IN THE SETTINGS MENU.");
                            return; // PREVENT ENTRY!
                        }
                        OnLevelButtonClicked(currentLevelToSetup);
                    };
                }
            }

            // Add the fully constructed widget into the ScrollView!
            scrollView.Add(newButton);
        }
    }

    private void OnLevelButtonClicked(int level)
    {
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentPlayingLevel = level;
            Time.timeScale = 1f;
            LevelManager.forcedTargetPieceTypes = null;
            GridSpawner.forcedNoiseOffset = null;
            ProgressionManager.Instance.returnToMenu = false;
            SceneManager.LoadScene(gameplaySceneName);
        }
        else
        {
            Debug.LogError("No ProgressionManager found in the scene! Cannot set level.");
        }
    }
}
