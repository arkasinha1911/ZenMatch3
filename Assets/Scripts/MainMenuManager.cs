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

        // Button Listeners
        if (playButton != null) playButton.clicked += ShowLevelSelect;
        if (settingsOpenButton != null) settingsOpenButton.clicked += ShowSettings;
        if (backToMenuButton != null) backToMenuButton.clicked += ShowMainMenu;
        if (closeSettingsButton != null) closeSettingsButton.clicked += ShowMainMenu;
        
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
        if (mainMenuPanel != null) mainMenuPanel.style.display = DisplayStyle.None;
        if (levelSelectPanel != null) levelSelectPanel.style.display = DisplayStyle.None;
        if (settingsPanel != null) settingsPanel.style.display = DisplayStyle.Flex;

        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
        if (gameHUDPanel != null) gameHUDPanel.style.display = DisplayStyle.None;
    }
}
