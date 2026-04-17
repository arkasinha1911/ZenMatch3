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

    private VisualElement mainMenuPanel;
    private VisualElement levelSelectPanel;
    private VisualElement settingsPanel;
    private VisualElement gameHUDPanel;
    private Label lblTotalStars;
    private Label lblShopMagnetOwned;
    private Label lblShopXBombOwned;
    private Label lblShopAreaBombOwned;
    private Label lblShopFeedback;

    private void Start()
    {
        if (uiDocument == null) return;
        var root = uiDocument.rootVisualElement;

        // Core Panels
        mainMenuPanel = root.Q<VisualElement>("mainMenuPanel");
        levelSelectPanel = root.Q<VisualElement>("levelSelectPanel");
        settingsPanel = root.Q<VisualElement>("settingsPanel");
        gameHUDPanel = root.Q<VisualElement>("gameHUDPanel");

        // Buttons
        Button playButton = root.Q<Button>("playButton");
        Button settingsOpenButton = root.Q<Button>("settingsOpenButton");
        Button backToMenuButton = root.Q<Button>("backToMenuButton");
        Button closeSettingsButton = root.Q<Button>("closeSettingsButton");
        Button exitButton = root.Q<Button>("exitButton");
        Button resetLivesButton = root.Q<Button>("resetLivesButton");

        // Shop UI
        lblTotalStars = root.Q<Label>("lblTotalStars");
        lblShopMagnetOwned = root.Q<Label>("lblShopMagnetOwned");
        lblShopXBombOwned = root.Q<Label>("lblShopXBombOwned");
        lblShopAreaBombOwned = root.Q<Label>("lblShopAreaBombOwned");
        lblShopFeedback = root.Q<Label>("lblShopFeedback");
        Button buyMagnetButton = root.Q<Button>("buyMagnetButton");
        Button buyXBombButton = root.Q<Button>("buyXBombButton");
        Button buyAreaBombButton = root.Q<Button>("buyAreaBombButton");

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
