using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("The main menu screen containing the Play button.")]
    public GameObject mainMenuPanel;

    [Tooltip("The panel containing your LevelSelectUI script and button container.")]
    public GameObject levelSelectPanel;

    [Header("Gameplay Connections")]
    [Tooltip("Drag your GridSpawner here so we can turn it off while in the menu.")]
    public GridSpawner gridSpawner;
    
    [Tooltip("Drag your LevelManager here so we can turn it off while in the menu.")]
    public LevelManager levelManager;

    private void Start()
    {
        if (ProgressionManager.Instance != null && !ProgressionManager.Instance.returnToMenu)
        {
            HideAllMenus(); // Starts the game!
        }
        else
        {
            ShowMainMenu();
        }
    }

    public void HideAllMenus()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);

        // Turn gameplay ON
        if (gridSpawner != null) gridSpawner.gameObject.SetActive(true);
        if (levelManager != null) levelManager.gameObject.SetActive(true);
    }

    /// <summary>
    /// Attach this to your back button.
    /// </summary>
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(false);

        // Turn gameplay OFF
        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
    }

    /// <summary>
    /// Attach this to your main Play button!
    /// </summary>
    public void ShowLevelSelect()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (levelSelectPanel != null) levelSelectPanel.SetActive(true);

        // Turn gameplay OFF
        if (gridSpawner != null) gridSpawner.gameObject.SetActive(false);
        if (levelManager != null) levelManager.gameObject.SetActive(false);
    }
}
