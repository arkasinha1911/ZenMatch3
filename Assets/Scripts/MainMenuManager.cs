using UnityEngine;
using UnityEngine.UIElements; // UI Toolkit Support

/// <summary>
/// This script manages the Main Menu experience before the gameplay actually starts using UI Toolkit.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument uiDocument;
    
    [Header("Gameplay Connections")]
    public GridSpawner gridSpawner;
    public LevelManager levelManager;
    public LevelSelectUI levelSelectUI; // Reference to refresh the level buttons when the panel opens!

    [Header("Power Up Icons")]
    public Sprite magnetIcon;
    public Sprite xBombIcon;
    public Sprite areaBombIcon;

    private VisualElement mainMenuPanel;
    private VisualElement levelSelectPanel;
    private VisualElement settingsPanel;
    private VisualElement gameHUDPanel;
    private Label lblTotalStars;
    private Label lblShopMagnetOwned;
    private Label lblShopXBombOwned;
    private Label lblShopAreaBombOwned;
    private Label lblShopFeedback;

    [Header("Lives Display Sprites")]
    public Sprite fullLifeIcon;
    public Sprite lostLifeIcon;

    private VisualElement[] lifeIcons;
    private Label livesTimerLabel;
    private VisualElement livesContainer;
    private Label gameTitleLabel;

    private void Start()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        // Core Panels
        mainMenuPanel = root.Q<VisualElement>("mainMenuPanel");
        levelSelectPanel = root.Q<VisualElement>("levelSelectPanel");
        settingsPanel = root.Q<VisualElement>("settingsPanel");
        gameHUDPanel = root.Q<VisualElement>("gameHUDPanel");
        gameTitleLabel = root.Q<Label>("gameTitleLabel");

        // Buttons
        Button playButton = root.Q<Button>("playButton");
        Button settingsOpenButton = root.Q<Button>("settingsOpenButton");
        Button backToMenuButton = root.Q<Button>("backToMenuButton");
        Button closeSettingsButton = root.Q<Button>("closeSettingsButton");
        Button exitButton = root.Q<Button>("exitButton");
        Button resetLivesButton = root.Q<Button>("resetLivesButton");
        Button watchAdForLifeButton = root.Q<Button>("watchAdForLifeButton");
        Button resetPowerupsButton = root.Q<Button>("resetPowerupsButton");
        Button unlockAllLevelsButton = root.Q<Button>("unlockAllLevelsButton");

        // Shop UI
        lblTotalStars = root.Q<Label>("lblTotalStars");
        lblShopMagnetOwned = root.Q<Label>("lblShopMagnetOwned");
        lblShopXBombOwned = root.Q<Label>("lblShopXBombOwned");
        lblShopAreaBombOwned = root.Q<Label>("lblShopAreaBombOwned");
        lblShopFeedback = root.Q<Label>("lblShopFeedback");
        Button buyMagnetButton = root.Q<Button>("buyMagnetButton");
        Button buyXBombButton = root.Q<Button>("buyXBombButton");
        Button buyAreaBombButton = root.Q<Button>("buyAreaBombButton");

        // Swap out shop emojis with Sprites if provided
        if (settingsPanel != null)
        {
            var magnetCard = settingsPanel.Q<VisualElement>(className: "shop-card-magnet");
            if (magnetCard != null && magnetIcon != null)
            {
                var iconLbl = magnetCard.Q<Label>(className: "shop-card-icon");
                if (iconLbl != null) {
                    iconLbl.text = "";
                    iconLbl.style.backgroundImage = new StyleBackground(magnetIcon);
                    iconLbl.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    iconLbl.style.width = 60; // Better proportion for a sprite
                    iconLbl.style.height = 60;
                }
            }

            var xBombCard = settingsPanel.Q<VisualElement>(className: "shop-card-xbomb");
            if (xBombCard != null && xBombIcon != null)
            {
                var iconLbl = xBombCard.Q<Label>(className: "shop-card-icon");
                if (iconLbl != null) {
                    iconLbl.text = "";
                    iconLbl.style.backgroundImage = new StyleBackground(xBombIcon);
                    iconLbl.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    iconLbl.style.width = 60;
                    iconLbl.style.height = 60;
                }
            }

            var areaBombCard = settingsPanel.Q<VisualElement>(className: "shop-card-areabomb");
            if (areaBombCard != null && areaBombIcon != null)
            {
                var iconLbl = areaBombCard.Q<Label>(className: "shop-card-icon");
                if (iconLbl != null) {
                    iconLbl.text = "";
                    iconLbl.style.backgroundImage = new StyleBackground(areaBombIcon);
                    iconLbl.style.unityBackgroundScaleMode = ScaleMode.ScaleToFit;
                    iconLbl.style.width = 60;
                    iconLbl.style.height = 60;
                }
            }
        }

        // Button Listeners
        if (playButton != null) playButton.clicked += ShowLevelSelect;
        if (settingsOpenButton != null) settingsOpenButton.clicked += ShowSettings;
        if (backToMenuButton != null) backToMenuButton.clicked += ShowMainMenu;
        if (closeSettingsButton != null) closeSettingsButton.clicked += ShowMainMenu;
        
        if (buyMagnetButton != null) buyMagnetButton.clicked += () => TryBuyItem("Magnet", 150);
        if (buyXBombButton != null) buyXBombButton.clicked += () => TryBuyItem("XBomb", 50);
        if (buyAreaBombButton != null) buyAreaBombButton.clicked += () => TryBuyItem("AreaBomb", 75);
        
        if (resetLivesButton != null)
        {
            resetLivesButton.clicked += () => 
            {
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.RefillLives();
                    Debug.Log("Lives Reset explicitly by Dev button!");
                    // Need to refresh level menu to re-enable disabled buttons
                    if (levelSelectPanel.style.display == DisplayStyle.Flex)
                    {
                        if (levelSelectUI != null) levelSelectUI.GenerateLevelButtons();
                    }
                }
            };
        }
        
        if (resetPowerupsButton != null)
        {
            resetPowerupsButton.clicked += () => 
            {
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.ResetPowerups();
                    UpdateShopUI();
                    SetFeedback("Powerups Reset!", new Color(0.91f, 0.3f, 0.24f));
                    Debug.Log("Powerups reset by Dev button");
                }
            };
        }

        if (unlockAllLevelsButton != null)
        {
            unlockAllLevelsButton.clicked += () => 
            {
                if (ProgressionManager.Instance != null)
                {
                    ProgressionManager.Instance.UnlockAllLevels(500); // Unlocks 500 levels
                    if (levelSelectUI != null) levelSelectUI.GenerateLevelButtons(); // Refresh scroll view
                    SetFeedback("All Levels Unlocked!", new Color(0.9f, 0.7f, 0.1f));
                    Debug.Log("Levels unlocked by Dev button");
                }
            };
        }
        
        if (watchAdForLifeButton != null)
        {
            watchAdForLifeButton.clicked += () =>
            {
                if (AdManager.Instance != null && ProgressionManager.Instance != null)
                {
                    // Disable button to prevent spam click
                    watchAdForLifeButton.SetEnabled(false);
                    AdManager.Instance.ShowRewardedAd((success) => 
                    {
                        if (success)
                        {
                            ProgressionManager.Instance.GiveOneLife();
                        }
                        watchAdForLifeButton.SetEnabled(true);
                    });
                }
            };
        }
        
        if (exitButton != null)
        {
            exitButton.clicked += () => 
            {
                Debug.Log("Exiting Game...");
                Application.Quit();
            };
        }

        // Determine which screen to show on boot based on whether we "Restarted" a level
        if (ProgressionManager.Instance != null && !ProgressionManager.Instance.returnToMenu)
        {
            HideAllMenus(); // Starts the game immediately!
        }
        else
        {
            ShowMainMenu();
        }

        // Hook up the lives display
        HookUpLivesDisplay(root);
    }

    /// <summary>
    /// Hooks up the UI Toolkit elements for the hearts and timer display.
    /// </summary>
    private void HookUpLivesDisplay(VisualElement root)
    {
        if (root == null) return;

        livesTimerLabel = root.Q<Label>("livesTimerLabel");

        int max = ProgressionManager.MAX_LIVES;
        lifeIcons = new VisualElement[max];
        for (int i = 0; i < max; i++)
        {
            lifeIcons[i] = root.Q<VisualElement>($"lifeIcon{i}");
        }
    }

    private void Update()
    {
        // Animate title when Main Menu is active
        if (gameTitleLabel != null && mainMenuPanel != null && mainMenuPanel.style.display == DisplayStyle.Flex)
        {
            float time = Time.time;
            float scale = 1f + 0.05f * Mathf.Sin(time * 2f);
            float rot = 3f * Mathf.Sin(time * 1.5f);
            
            gameTitleLabel.style.scale = new StyleScale(new Vector2(scale, scale));
            gameTitleLabel.style.rotate = new StyleRotate(new Rotate(new Angle(rot, AngleUnit.Degree)));
        }

        if (ProgressionManager.Instance == null) return;

        // Update lives display every frame
        int lives = ProgressionManager.Instance.currentLives;
        int max = ProgressionManager.MAX_LIVES;

        if (lifeIcons != null)
        {
            for (int i = 0; i < max; i++)
            {
                if (i < lifeIcons.Length && lifeIcons[i] != null)
                {
                    if (i < lives && fullLifeIcon != null)
                        lifeIcons[i].style.backgroundImage = new StyleBackground(fullLifeIcon);
                    else if (i >= lives && lostLifeIcon != null)
                        lifeIcons[i].style.backgroundImage = new StyleBackground(lostLifeIcon);
                }
            }
        }

        if (livesTimerLabel != null)
        {
            if (lives < max)
            {
                int secondsLeft = ProgressionManager.Instance.GetTotalSecondsUntilFullLives();
                int minutes = secondsLeft / 60;
                int seconds = secondsLeft % 60;
                livesTimerLabel.text = $"Full \u2764 in {minutes:D2}:{seconds:D2}";
                livesTimerLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                livesTimerLabel.text = "";
                livesTimerLabel.style.display = DisplayStyle.None;
            }
        }
    }

    private void TryBuyItem(string item, int cost)
    {
        if (ProgressionManager.Instance == null) return;

        if (ProgressionManager.Instance.BuyItem(item, cost))
        {
            UpdateShopUI();
            SetFeedback($"Purchased {item}!", new Color(0.18f, 0.8f, 0.44f));
        }
        else
        {
            SetFeedback("Not enough stars!", new Color(0.91f, 0.3f, 0.24f));
        }
    }

    private void SetFeedback(string msg, Color color)
    {
        if (lblShopFeedback != null)
        {
            lblShopFeedback.text = msg;
            lblShopFeedback.style.color = new StyleColor(color);
        }
    }

    private void UpdateShopUI()
    {
        if (ProgressionManager.Instance == null) return;
        if (lblTotalStars != null) lblTotalStars.text = ProgressionManager.Instance.TotalStars.ToString();
        if (lblShopMagnetOwned != null) lblShopMagnetOwned.text = $"Owned: {ProgressionManager.Instance.MagnetCount}";
        if (lblShopXBombOwned != null) lblShopXBombOwned.text = $"Owned: {ProgressionManager.Instance.XBombCount}";
        if (lblShopAreaBombOwned != null) lblShopAreaBombOwned.text = $"Owned: {ProgressionManager.Instance.AreaBombCount}";
    }

    public void HideAllMenus()
    {
        if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.None;
        if (levelSelectPanel != null) levelSelectPanel.style.display = DisplayStyle.None;
        if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;

        if (gridSpawner != null) gridSpawner.gameObject.SetActive(true);
        if (levelManager != null) levelManager.gameObject.SetActive(true);
        if (gameHUDPanel != null) gameHUDPanel.style.display = DisplayStyle.Flex; // Show HUD overlay
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.Flex;
        if (levelSelectPanel != null) levelSelectPanel.style.display = DisplayStyle.None;
        if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;

        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
        if (gameHUDPanel != null) gameHUDPanel.style.display = DisplayStyle.None;
    }

    public void ShowLevelSelect()
    {
        if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.None;
        if (levelSelectPanel != null) levelSelectPanel.style.display = DisplayStyle.Flex;
        if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.None;

        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
        if (gameHUDPanel != null) gameHUDPanel.style.display = DisplayStyle.None;
        
        // Dynamically tell the other script to generate the UI Buttons now that the screen is visible!
        if (levelSelectUI != null) levelSelectUI.GenerateLevelButtons();
    }

    public void ShowSettings()
    {
        UpdateShopUI();
        if (lblShopFeedback != null) lblShopFeedback.text = "";

        if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.None;
        if (levelSelectPanel != null) levelSelectPanel.style.display = DisplayStyle.None;
        if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.Flex;

        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
        if (gameHUDPanel != null) gameHUDPanel.style.display = DisplayStyle.None;
    }
}
