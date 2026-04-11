using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshPro integration for high-quality text

/// <summary>
/// This tiny 'data class' represents a single objective in a level (e.g., "Collect 10 Red Squares").
/// [System.Serializable] forces Unity to show this custom class inside the Unity Inspector!
/// </summary>
[System.Serializable]
public class LevelTarget
{
    [Tooltip("The ID number of the piece. Usually 0 is Red, 1 is Blue, etc. If 'Randomize Targets' is true, the game writes this number automatically!")]
    public int pieceType;
    
    [Tooltip("How many pieces of this specific shape the player must destroy to win.")]
    public int amountRequired;
    
    // Tracks how many the player has smashed so far. We hide it from the Inspector so designers don't accidentally edit it.
    [HideInInspector]
    public int amountCollected = 0;

    [Tooltip("The UI Text component showing the '5/10' progress. Link this manually OR use the Auto UI Setup system!")]
    public TextMeshProUGUI progressText;

    [Tooltip("The UI Image component showing the shape icon. Link this manually OR use the Auto UI Setup system!")]
    public UnityEngine.UI.Image pieceIcon;
}

/// <summary>
/// LevelManager sits at the very top of the game hierarchy. It is the core "Game Loop" controller.
/// It watches the clock (countdownTimer), watches the GridSpawner, and waits for pieces to explode.
/// When pieces explode, it checks if the player won or lost, and handles freezing the game and saving data.
/// </summary>
public class LevelManager : MonoBehaviour
{
    // Singleton Instance pattern: Only ONE level manager can exist at a time.
    public static LevelManager Instance { get; private set; }

    [Header("Level State")]
    // The amount of time in seconds the player has to win the level!
    public float countdownTimer = 60f;
    
    [Tooltip("If true, the game will randomly pick which shapes you need to collect by looking at whatever is in your GridSpawner.")]
    public bool randomizeTargets = true;

    [Tooltip("Required if randomizing: Drag the GridSpawner here so we know exactly what shapes are physically allowed to spawn.")]
    public GridSpawner gridSpawner;

    // A list of our custom 'LevelTarget' objectives. (e.g. Target 1: Red Squares, Target 2: Blue Circles)
    public List<LevelTarget> targets;

    [Header("Auto UI Display Setup")]
    // Instead of forcing you to drag 10 different text boxes, we built an auto-spawner to create the UI icons for you!
    [Tooltip("(Optional) Drag a Prefab containing an Image and TextMeshProUGUI. The game will automatically spawn and wire up goals for you!")]
    public GameObject targetUIPrefab;
    [Tooltip("(Optional) Drag a UI panel (like a Horizontal Layout Group) inside your Canvas to hold the dynamically spawned objectives.")]
    public Transform targetUIParent;

    [Header("UI Panels")]
    // Direct link to the big clock text at the top of the screen.
    public TextMeshProUGUI timerText;
    
    // Direct links to the pop-up screens that appear when the game ends.
    public GameObject gameWinPanel;
    public GameObject gameOverPanel;

    // A public boolean anyone can read (get) to know if the game is active, but only THIS script can modify (private set).
    public bool IsGameActive { get; private set; } = true;

    // Static variables SURVIVE scene reloads! 
    // We use this to ensure that if a player clicks "Retry", they are forced to collect the exact same shapes they failed on last time.
    [HideInInspector]
    public static List<int> forcedTargetPieceTypes = null;

    /// <summary>
    /// Awake initializes the Singleton instance as soon as the LevelManager is created.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    /// <summary>
    /// Start runs when the level actually begins (after closing the Main Menu).
    /// </summary>
    private void Start()
    {
        // 1. Difficulty Scaling! Check which level the player is on.
        if (ProgressionManager.Instance != null)
        {
            int level = ProgressionManager.Instance.currentPlayingLevel;
            
            // MATH TRICK: Make the timer shorter on higher levels.
            // Example: Level 1 is 60 - 1.5 = 58.5s. Level 20 is 60 - 30 = 30s. Mathf.Max ensures it never goes below 30s.
            countdownTimer = Mathf.Max(30f, 60f - (level * 1.5f));
            
            // MATH TRICK: Make the targets harder on higher levels.
            foreach (var target in targets)
            {
                // Example: Level 1 requires 10 + 3 = 13 pieces. Level 10 requires 10 + 30 = 40 pieces.
                target.amountRequired = 10 + (level * 3);
            }
        }

        // 2. Turn the game ON and make sure physics time is moving normally (Time.timeScale = 1)
        IsGameActive = true;
        Time.timeScale = 1f;

        // 3. Hide the Win/Loss popups so they don't cover the screen at the start!
        if (gameWinPanel != null) gameWinPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 4. --- AUTO UI GENERATION SYSTEM ---
        if (targetUIParent != null && targetUIPrefab != null && targets.Count > 0)
        {
            // First, delete any placeholder items sitting in the Content group so it's fresh.
            foreach (Transform t in targetUIParent)
            {
                Destroy(t.gameObject);
            }

            // Loop through each target (e.g. if we have 3 targets, do this 3 times)
            foreach (var target in targets)
            {
                // Instantiate (Spawn) the prefab directly into the UI layout group.
                GameObject newlySpawnedUI = Instantiate(targetUIPrefab, targetUIParent);

                // Auto-link: Search the spawned prefab for its Image and assign it to our Target data.
                UnityEngine.UI.Image img = newlySpawnedUI.GetComponentInChildren<UnityEngine.UI.Image>();
                if (img != null) target.pieceIcon = img;

                // Auto-link: Search the spawned prefab for its Text and assign it to our Target data.
                TextMeshProUGUI txt = newlySpawnedUI.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) target.progressText = txt;
            }
        }

        // 5. Target Randomization
        if (randomizeTargets)
        {
            // Failsafe: if the developer forgot to drag the GridSpawner in, find it automatically!
            if (gridSpawner == null)
            {
                gridSpawner = FindObjectOfType<GridSpawner>();
            }

            // If the GridSpawner DOES have pieces assigned to it, randomly pick which ones we need to collect!
            if (gridSpawner != null && gridSpawner.prefabsToSpawn != null && gridSpawner.prefabsToSpawn.Count > 0)
            {
                RandomizeTargetShapes();
            }
        }

        // 6. Force the UI to update visually to say "0 / 15" instead of blank.
        UpdateTargetsUI();
    }

    /// <summary>
    /// Intelligently picks random shapes to act as the level objectives.
    /// </summary>
    private void RandomizeTargetShapes()
    {
        // A. THE RETRY CHECK:
        // If 'forcedTargetPieceTypes' is NOT null, it means the player clicked "Retry" after losing.
        // We MUST force the exact same targets they had last time to keep it fair!
        if (forcedTargetPieceTypes != null && forcedTargetPieceTypes.Count == targets.Count)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                // Apply the saved historical shapes instead of randomizing.
                targets[i].pieceType = forcedTargetPieceTypes[i];
                UpdateUIIcon(targets[i]);
            }
            return; // EXIT EARLY! Do not randomize!
        }

        // B. THE RANDOMIZER:
        // Create a list representing every shape the GridSpawner is allowed to make (e.g. 0,1,2,3,4,5).
        List<int> availablePieceIDs = new List<int>();
        for (int i = 0; i < gridSpawner.prefabsToSpawn.Count; i++)
        {
            availablePieceIDs.Add(i);
        }

        // Prepare our static memory variable just in case they lose and need to retry later.
        forcedTargetPieceTypes = new List<int>(); 

        // Loop through however many targets the Designer added (usually 2 or 3).
        foreach (var target in targets)
        {
            if (availablePieceIDs.Count > 0)
            {
                // Pick a random number between 0 and the amount of shapes left.
                int randomSlotInList = Random.Range(0, availablePieceIDs.Count);
                
                // Assign that shape ID to our objective!
                target.pieceType = availablePieceIDs[randomSlotInList];
                
                // CRITICAL: Remove that shape from our temporary list so we never pick the exact same shape twice!
                // We don't want "Target 1: Red" AND "Target 2: Red".
                availablePieceIDs.RemoveAt(randomSlotInList); 

                // Save to memory in case of a retry.
                forcedTargetPieceTypes.Add(target.pieceType);

                // Update the visual UI Icon to match the randomized shape!
                UpdateUIIcon(target);
            }
        }
    }

    /// <summary>
    /// Digs into the prefabs to extract their Sprites (pictures) and applies them to the UI panel.
    /// </summary>
    private void UpdateUIIcon(LevelTarget target)
    {
        // Ensure the Icon exists and the PieceType ID is actually valid.
        if (target.pieceIcon != null && target.pieceType >= 0 && target.pieceType < gridSpawner.prefabsToSpawn.Count)
        {
            // Grab the raw 3D/2D GameObject prefab the GridSpawner uses.
            GameObject chosenPrefab = gridSpawner.prefabsToSpawn[target.pieceType];
            
            // Dig deeply into the prefab to find its SpriteRenderer (the picture component).
            // (fixes a bug where pieces have nested graphics!)
            SpriteRenderer sr = chosenPrefab.GetComponentInChildren<SpriteRenderer>();
            
            if (sr != null)
            {
                // Copy the picture.
                target.pieceIcon.sprite = sr.sprite;
                // Copy the tint color (e.g. if a square is tinted green).
                target.pieceIcon.color = sr.color;
                
                // Failsafe: Some primitive prefabs use full transparency (alpha = 0) and use a shader to glow instead.
                // If it's totally transparent, force the UI icon to be fully solid (alpha = 1)!
                if(target.pieceIcon.color.a == 0) 
                {
                    Color c = target.pieceIcon.color;
                    c.a = 1f; // 1 is fully solid
                    target.pieceIcon.color = c;
                }
            }
        }
    }

    /// <summary>
    /// Update runs 60 times a second. We use it solely to run the clock down!
    /// </summary>
    private void Update()
    {
        // If the game ended, totally stop counting down.
        if (!IsGameActive) return;

        // Time.deltaTime is the literal fraction of a second since the last frame.
        // Subtracting it slowly counts our timer down smoothly.
        countdownTimer -= Time.deltaTime;
        
        if (countdownTimer <= 0f)
        {
            // Time is up! Floor it to 0 so it doesn't show negative numbers, and explicitly trigger the Game Over check.
            countdownTimer = 0f;
            TriggerGameOver();
        }

        // Update the visual UI Text. Mathf.CeilToInt rounds "4.2 seconds" up to "5 seconds" so it looks cleaner!
        if (timerText != null)
        {
            timerText.text = $"Time: {Mathf.CeilToInt(countdownTimer)}s";
        }
    }

    /// <summary>
    /// VERY IMPORTANT: This method is called directly by the GridSpawner script EVERY TIME a piece explodes!
    /// </summary>
    /// <param name="pieceType">The ID shape (0=Red, 1=Blue) of the piece that just exploded.</param>
    public void ReportPieceDestroyed(int pieceType)
    {
        // Don't log points after the game is over.
        if (!IsGameActive) return;

        bool allTargetsMet = true; // We optimistically assume the player has won, until we find a target they haven't finished!

        // Loop over every single objective.
        foreach (var target in targets)
        {
            // Did the piece that just exploded match this objective?
            if (target.pieceType == pieceType)
            {
                // If they need more, give them a point and update the "5/10" text!
                if (target.amountCollected < target.amountRequired)
                {
                    target.amountCollected++;
                    UpdateTargetsUI();
                }
            }

            // Check if THIS specific objective is still incomplete.
            if (target.amountCollected < target.amountRequired)
            {
                // We found an incomplete objective! Our optimistic assumption was wrong: they haven't won yet.
                allTargetsMet = false;
            }
        }

        // If there are actual targets, and NONE of them triggered the 'false' check above, the player has won!
        if (allTargetsMet && targets.Count > 0)
        {
            TriggerGameWin();
        }
    }

    /// <summary>
    /// Simple loop to refresh the "X / 10" text on every UI element.
    /// </summary>
    private void UpdateTargetsUI()
    {
        foreach (var target in targets)
        {
            if (target.progressText != null)
            {
                target.progressText.text = $"{target.amountCollected} / {target.amountRequired}";
            }
        }
    }

    /// <summary>
    /// Called when the player collects all required pieces before time runs out.
    /// </summary>
    private void TriggerGameWin()
    {
        // Shut down the game loop
        IsGameActive = false;
        
        // Show the giant "YOU WIN" popup
        if (gameWinPanel != null) gameWinPanel.SetActive(true);
        
        // Setting Time.timeScale to 0f instantly freezes Unity physics. Pieces will freeze mid-air!
        Time.timeScale = 0f; 
        
        if (ProgressionManager.Instance != null)
        {
            // META GAMEPLAY: Tell the memory manager to unlock the next numerical level in the menu.
            ProgressionManager.Instance.UnlockLevel(ProgressionManager.Instance.currentPlayingLevel + 1);
            
            // Check if they got a high score and save it to the hard drive!
            if (ScoreManager.Instance != null)
            {
                ProgressionManager.Instance.SaveHighScore(
                    ProgressionManager.Instance.currentPlayingLevel, 
                    ScoreManager.Instance.CurrentScore
                );
            }
        }
    }

    /// <summary>
    /// Called when the timer hits 0.0 seconds and targets are still unmet.
    /// </summary>
    private void TriggerGameOver()
    {
        // Shut down loop
        IsGameActive = false;
        
        // Show the "YOU LOSE/RETRY" popup
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        
        // Freeze physics
        Time.timeScale = 0f; 
    }

    /// <summary>
    /// Intended to be called by a UI Button on the "Game Win" panel. 
    /// Reloads the current scene but triggers all scripts to generate a brand new layout.
    /// </summary>
    public void LoadNextLevel()
    {
        Debug.Log("Next Level Button Clicked!");
        
        if (ProgressionManager.Instance != null)
        {
            // Increment the tracker so the game knows it's getting harder!
            ProgressionManager.Instance.currentPlayingLevel++;
        }
        
        // THE BRAND NEW LEVEL TRICK:
        // We set the static "forced" memories to null. 
        // When the scene finishes reloading, the GridSpawner and LevelManager will see 'null', and will generate a totally new chaotic map and new shapes!
        GridSpawner.forcedNoiseOffset = null;
        LevelManager.forcedTargetPieceTypes = null;

        // Unpause the game before reloading so the new scene doesn't instantly load completely frozen!
        Time.timeScale = 1f;
        
        // Reload currently active Unity Scene.
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Intended to be called by the UI Button on the "Game Over" panel. 
    /// Reloads the scene but preserves the layout seeds so the player can retry the exact same board!
    /// </summary>
    public void RetryLevel()
    {
        Debug.Log("Retry Level Button Clicked!");
        
        // By NOT setting the 'forced' static variables to null, the GridSpawner and LevelManager will 
        // read their past history and reconstruct the exact same map and targets instead of randomizing!
        
        // Unfreeze
        Time.timeScale = 1f;
        
        // Reload
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
