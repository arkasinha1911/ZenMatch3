using UnityEngine;

/// <summary>
/// This script manages the Main Menu experience before the gameplay actually starts.
/// It acts like a giant set of light switches, turning the Game UI ON and the Menu UI OFF, or vice versa!
/// It connects to the GridSpawner and LevelManager to physically disable them while the player is just looking at the menus.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("The main menu screen containing the giant Play button and title.")]
    public GameObject mainMenuPanel;

    [Tooltip("The panel containing your LevelSelectUI script and the grid of unlocked level buttons.")]
    public GameObject levelSelectPanel;

    [Tooltip("The panel containing your Settings and Audio slider controls.")]
    public GameObject settingsPanel;

    [Tooltip("A panel containing your in-game Score, Timer, and Target UI. We hide this during the menus so it doesn't overlap!")]
    public GameObject gameHUDPanel;

    [Header("Gameplay Connections")]
    // We need direct connections to the actual game logic scripts so we can freeze them if a menu is open.
    [Tooltip("Drag your GridSpawner here so we can turn the board generation ON/OFF. Ensures pieces don't fall behind the menu!")]
    public GridSpawner gridSpawner;
    
    [Tooltip("Drag your LevelManager here so we can freeze the countdown timer while in the menu.")]
    public LevelManager levelManager;

    /// <summary>
    /// Start runs when the Menu Scene is loaded. We use it to decide what screen to show first.
    /// </summary>
    private void Start()
    {
        // 1. Check with the ProgressionManager (our permanent memory module).
        // If 'returnToMenu' is FALSE, it means the player clicked "Restart Level" or "Next Level" and we should bypass the menu entirely!
        if (ProgressionManager.Instance != null && !ProgressionManager.Instance.returnToMenu)
        {
            HideAllMenus(); // Starts the game immediately!
        }
        else
        {
            // 2. Otherwise, this is a fresh launch or they intentionally clicked "Back to Menu".
            ShowMainMenu();
        }
    }

    /// <summary>
    /// This method hides all UI menus and actively turns the game ON. 
    /// It is called automatically by Start() if returnToMenu is false, or by a "Resume/Play" button.
    /// </summary>
    public void HideAllMenus()
    {
        // Safely turn off all menu screens (if they are assigned in the Inspector)
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Turn gameplay ON! 
        // Activating these scripts means Update() and Start() will finally run inside them.
        if (gridSpawner != null) gridSpawner.gameObject.SetActive(true);
        if (levelManager != null) levelManager.gameObject.SetActive(true);
        if (gameHUDPanel != null) gameHUDPanel.SetActive(true);
    }

    /// <summary>
    /// Attach this method to your "Back" button or call it to return to the very first title screen.
    /// </summary>
    public void ShowMainMenu()
    {
        // Turn ON just the Main Menu panel, turn OFF everything else.
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Turn gameplay OFF! 
        // Disabling these GameObjects completely pauses all matching and grid spawning logic.
        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
        if (gameHUDPanel != null) gameHUDPanel.SetActive(false);
    }

    /// <summary>
    /// Attach this to your main "Play" button! It opens the big grid of Level Buttons.
    /// </summary>
    public void ShowLevelSelect()
    {
        // Hide the title screen, show the level select screen.
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Keep gameplay OFF so timers don't tick down while they pick a level.
        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
        if (gameHUDPanel != null) gameHUDPanel.SetActive(false);
    }

    /// <summary>
    /// Attach this to your "Settings/Gear" button! It opens the Audio controls.
    /// </summary>
    public void ShowSettings()
    {
        // Hide the title screen, show the settings screen.
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        // Keep gameplay OFF.
        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
        if (gameHUDPanel != null) gameHUDPanel.SetActive(false);
    }
}
