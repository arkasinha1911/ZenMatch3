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

    // Cached UI elements for the lives display
    private VisualElement noLivesOverlay;
    private Label noLivesTimerLabel;
    private Label noLivesInfoLabel;
    private ScrollView levelScrollView;
    private bool wasOutOfLives = false;
    /// <summary>
    /// Now called securely by MainMenuManager when the player opens the Level Select Screen.
    /// </summary>
    public void GenerateLevelButtons()
    {
        if (uiDocument == null || levelButtonTemplate == null) return;
        
        var root = uiDocument.rootVisualElement;
        levelScrollView = root.Q<ScrollView>("levelScrollView");
        if (levelScrollView == null) return;

        // 1. Clear old UI
        levelScrollView.Clear();

        // Remove old no-lives overlay if it exists
        var levelSelectPanel = root.Q<VisualElement>("levelSelectPanel");
        if (noLivesOverlay != null && levelSelectPanel != null)
        {
            if (noLivesOverlay.parent != null)
                noLivesOverlay.parent.Remove(noLivesOverlay);
            noLivesOverlay = null;
        }

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
            bool isPlayable = isUnlocked && hasLives;

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
                buttonComponent.SetEnabled(isPlayable);
                
                // Grey out buttons when no lives available
                if (!hasLives && isUnlocked)
                {
                    buttonComponent.style.opacity = 0.4f;
                }
                
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
            levelScrollView.Add(newButton);
        }

        // --- NO LIVES OVERLAY ---
        // Build and show a banner if the player is out of lives
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.currentLives <= 0)
        {
            BuildNoLivesOverlay(levelSelectPanel);
            wasOutOfLives = true;
        }
        else
        {
            wasOutOfLives = false;
        }
    }

    /// <summary>
    /// Builds a floating overlay banner on the level select screen
    /// telling the player they have no lives and showing a countdown timer.
    /// </summary>
    private void BuildNoLivesOverlay(VisualElement parent)
    {
        if (parent == null) return;

        noLivesOverlay = new VisualElement();
        noLivesOverlay.style.position = Position.Absolute;
        noLivesOverlay.style.top = 80;
        noLivesOverlay.style.left = 0;
        noLivesOverlay.style.right = 0;
        noLivesOverlay.style.alignItems = Align.Center;
        noLivesOverlay.style.justifyContent = Justify.Center;
        noLivesOverlay.pickingMode = PickingMode.Ignore;

        var banner = new VisualElement();
        banner.style.backgroundColor = new StyleColor(new Color(0.85f, 0.15f, 0.15f, 0.95f));
        banner.style.borderTopLeftRadius = 20;
        banner.style.borderTopRightRadius = 20;
        banner.style.borderBottomLeftRadius = 20;
        banner.style.borderBottomRightRadius = 20;
        banner.style.paddingTop = 20;
        banner.style.paddingBottom = 20;
        banner.style.paddingLeft = 40;
        banner.style.paddingRight = 40;
        banner.style.alignItems = Align.Center;
        banner.pickingMode = PickingMode.Ignore;

        var heartIcon = new Label("💔");
        heartIcon.style.fontSize = 50;
        heartIcon.style.unityTextAlign = TextAnchor.MiddleCenter;
        heartIcon.pickingMode = PickingMode.Ignore;
        banner.Add(heartIcon);

        noLivesInfoLabel = new Label("No Lives Remaining!");
        noLivesInfoLabel.style.fontSize = 36;
        noLivesInfoLabel.style.color = Color.white;
        noLivesInfoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        noLivesInfoLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        noLivesInfoLabel.style.marginBottom = 8;
        noLivesInfoLabel.pickingMode = PickingMode.Ignore;
        banner.Add(noLivesInfoLabel);

        noLivesTimerLabel = new Label("Next life in: --:--");
        noLivesTimerLabel.style.fontSize = 30;
        noLivesTimerLabel.style.color = new StyleColor(new Color(1f, 0.9f, 0.7f));
        noLivesTimerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        noLivesTimerLabel.pickingMode = PickingMode.Ignore;
        banner.Add(noLivesTimerLabel);

        noLivesOverlay.Add(banner);
        parent.Add(noLivesOverlay);
    }

    private void Update()
    {
        // Update the countdown timer label if the overlay is showing
        if (noLivesTimerLabel != null && noLivesOverlay != null && ProgressionManager.Instance != null)
        {
            if (ProgressionManager.Instance.currentLives > 0)
            {
                // A life just regenerated! Auto-refresh the level select screen.
                if (wasOutOfLives)
                {
                    wasOutOfLives = false;
                    GenerateLevelButtons();
                }
                return;
            }

            int secondsLeft = ProgressionManager.Instance.GetSecondsUntilNextLife();
            int minutes = secondsLeft / 60;
            int seconds = secondsLeft % 60;
            noLivesTimerLabel.text = $"Next life in: {minutes:D2}:{seconds:D2}";
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
