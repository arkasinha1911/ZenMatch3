using System.Collections.Generic;
using UnityEngine;
using TMPro;

[System.Serializable]
public class LevelTarget
{
    [Tooltip("The ID of the piece. If 'Randomize Targets' is true, this gets overwritten automatically!")]
    public int pieceType;
    
    [Tooltip("How many pieces of this type the player must collect.")]
    public int amountRequired;
    
    [HideInInspector]
    public int amountCollected = 0;

    [Tooltip("Link this or use Auto UI Setup below!")]
    public TextMeshProUGUI progressText;

    [Tooltip("Link this or use Auto UI Setup below!")]
    public UnityEngine.UI.Image pieceIcon;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level State")]
    public float countdownTimer = 60f;
    
    [Tooltip("If true, the game will randomly pick which shapes you need to collect based on your GridSpawner.")]
    public bool randomizeTargets = true;

    [Tooltip("Required for randomization: Drag the GridSpawner here so we know what shapes exist.")]
    public GridSpawner gridSpawner;

    public List<LevelTarget> targets;

    [Header("Auto UI Display Setup")]
    [Tooltip("(Optional) Drag a Prefab containing an Image and TextMeshProUGUI. The game will automatically spawn and wire up goals for you!")]
    public GameObject targetUIPrefab;
    [Tooltip("(Optional) Drag a panel (like a Horizontal Layout Group) inside your Canvas to hold the spawned objectives.")]
    public Transform targetUIParent;

    [Header("UI Panels")]
    public TextMeshProUGUI timerText;
    public GameObject gameWinPanel;
    public GameObject gameOverPanel;

    public bool IsGameActive { get; private set; } = true;

    [HideInInspector]
    public static List<int> forcedTargetPieceTypes = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (ProgressionManager.Instance != null)
        {
            int level = ProgressionManager.Instance.currentPlayingLevel;
            
            // Scale time: Starts at ~60s, gets shorter each level (minimum 30s)
            countdownTimer = Mathf.Max(30f, 60f - (level * 1.5f));
            
            // Scale targets: each target requires more as level increases
            foreach (var target in targets)
            {
                target.amountRequired = 10 + (level * 3);
            }
        }

        IsGameActive = true;
        Time.timeScale = 1f;

        if (gameWinPanel != null) gameWinPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // --- AUTO UI GENERATION ---
        if (targetUIParent != null && targetUIPrefab != null && targets.Count > 0)
        {
            // Clear out any old placeholder items in the UI group
            foreach (Transform t in targetUIParent)
            {
                Destroy(t.gameObject);
            }

            // Spawn fresh UI items exactly equal to how many targets exist
            foreach (var target in targets)
            {
                GameObject newlySpawnedUI = Instantiate(targetUIPrefab, targetUIParent);

                // Auto-link the Image
                UnityEngine.UI.Image img = newlySpawnedUI.GetComponentInChildren<UnityEngine.UI.Image>();
                if (img != null) target.pieceIcon = img;

                // Auto-link the Text
                TextMeshProUGUI txt = newlySpawnedUI.GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) target.progressText = txt;
            }
        }

        if (randomizeTargets)
        {
            if (gridSpawner == null)
            {
                gridSpawner = FindObjectOfType<GridSpawner>();
            }

            if (gridSpawner != null && gridSpawner.prefabsToSpawn != null && gridSpawner.prefabsToSpawn.Count > 0)
            {
                RandomizeTargetShapes();
            }
        }

        UpdateTargetsUI();
    }

    private void RandomizeTargetShapes()
    {
        if (forcedTargetPieceTypes != null && forcedTargetPieceTypes.Count == targets.Count)
        {
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].pieceType = forcedTargetPieceTypes[i];
                UpdateUIIcon(targets[i]);
            }
            return;
        }

        // Get all possible piece IDs
        List<int> availablePieceIDs = new List<int>();
        for (int i = 0; i < gridSpawner.prefabsToSpawn.Count; i++)
        {
            availablePieceIDs.Add(i);
        }

        forcedTargetPieceTypes = new List<int>(); 

        foreach (var target in targets)
        {
            if (availablePieceIDs.Count > 0)
            {
                int randomSlotInList = Random.Range(0, availablePieceIDs.Count);
                target.pieceType = availablePieceIDs[randomSlotInList];
                // Remove so we never pick the exact same shape for two different targets
                availablePieceIDs.RemoveAt(randomSlotInList); 

                forcedTargetPieceTypes.Add(target.pieceType);

                // Update the visual UI Icon to match the random shape!
                UpdateUIIcon(target);
            }
        }
    }

    private void UpdateUIIcon(LevelTarget target)
    {
        if (target.pieceIcon != null && target.pieceType >= 0 && target.pieceType < gridSpawner.prefabsToSpawn.Count)
        {
            GameObject chosenPrefab = gridSpawner.prefabsToSpawn[target.pieceType];
            
            // Look for SpriteRenderer anywhere on the object or its children (fixes 'wrong shape' bugs!)
            SpriteRenderer sr = chosenPrefab.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                target.pieceIcon.sprite = sr.sprite;
                target.pieceIcon.color = sr.color;
                
                // If it's pure white, the prefab might rely on a specific alpha or layout, force solid
                if(target.pieceIcon.color.a == 0) 
                {
                    Color c = target.pieceIcon.color;
                    c.a = 1f;
                    target.pieceIcon.color = c;
                }
            }
        }
    }

    private void Update()
    {
        // "dont stop" timer while physically exploding, but DO stop it if they won/lost
        if (!IsGameActive) return;

        countdownTimer -= Time.deltaTime;
        if (countdownTimer <= 0f)
        {
            countdownTimer = 0f;
            TriggerGameOver();
        }

        if (timerText != null)
        {
            timerText.text = $"Time: {Mathf.CeilToInt(countdownTimer)}s";
        }
    }

    public void ReportPieceDestroyed(int pieceType)
    {
        if (!IsGameActive) return;

        bool allTargetsMet = true;

        foreach (var target in targets)
        {
            if (target.pieceType == pieceType)
            {
                if (target.amountCollected < target.amountRequired)
                {
                    target.amountCollected++;
                    UpdateTargetsUI();
                }
            }

            if (target.amountCollected < target.amountRequired)
            {
                allTargetsMet = false;
            }
        }

        // If there are valid targets and they were all met, player wins!
        if (allTargetsMet && targets.Count > 0)
        {
            TriggerGameWin();
        }
    }

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

    private void TriggerGameWin()
    {
        IsGameActive = false;
        if (gameWinPanel != null) gameWinPanel.SetActive(true);
        Time.timeScale = 0f; // Pauses physics and interaction
        
        if (ProgressionManager.Instance != null)
        {
            // Unlock the next level
            ProgressionManager.Instance.UnlockLevel(ProgressionManager.Instance.currentPlayingLevel + 1);
            
            // Save the high score for this level
            if (ScoreManager.Instance != null)
            {
                ProgressionManager.Instance.SaveHighScore(
                    ProgressionManager.Instance.currentPlayingLevel, 
                    ScoreManager.Instance.CurrentScore
                );
            }
        }
    }

    private void TriggerGameOver()
    {
        IsGameActive = false;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f; // Pauses physics and interaction
    }

    /// <summary>
    /// Intended to be called by a UI Button. Reloads the current scene to generate a brand new layout and new targets.
    /// </summary>
    public void LoadNextLevel()
    {
        Debug.Log("Next Level Button Clicked!");
        
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.currentPlayingLevel++;
        }
        
        // CLEAR the static seeds so the game successfully generates a brand new level!
        GridSpawner.forcedNoiseOffset = null;
        LevelManager.forcedTargetPieceTypes = null;

        // Unpause the game before reloading so the new scene doesn't start completely frozen
        Time.timeScale = 1f;
        
        // Reload currently active scene
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Intended to be called by the Game Over retry Button. Reloads the scene but preserves the layout seeds!
    /// </summary>
    public void RetryLevel()
    {
        Debug.Log("Retry Level Button Clicked!");
        
        // Notice we do NOT clear the seeds! They will be automatically reused in Start().
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
