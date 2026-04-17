using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements; // UI Toolkit Support

[System.Serializable]
public class LevelTarget
{
    public int pieceType;
    public int amountRequired;
    [HideInInspector] public int amountCollected = 0;

    // Direct UI Elements mappings from UI Toolkit
    [HideInInspector] public Label progressText;
    [HideInInspector] public VisualElement pieceIcon;
}

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [Header("Level State")]
    public float countdownTimer = 60f;
    public bool randomizeTargets = true;
    public GridSpawner gridSpawner;
    public List<LevelTarget> targets;

    [Header("UI Toolkit Setup")]
    public UIDocument uiDocument;
    public VisualTreeAsset targetUIAsset; // (ObjectiveTargetTemplate.uxml)

    private VisualElement gameWinPanel;
    private VisualElement gameOverPanel;
    private VisualElement gamePausePanel;
    private Label timerText;
    private Label scoreText;
    private Label movesText;
    private Label livesText;

    public int maxMoves = 20;
    public int currentMoves;

    public bool IsGameActive { get; private set; } = true;
    public bool IsPaused { get; private set; } = false;

    [Header("Shop & Powerups")]
    public string ActivePowerup { get; private set; } = null;
    public bool IsPowerupTargetingMode => !string.IsNullOrEmpty(ActivePowerup);

    private Button btnMagnet;
    private Button btnXBomb;
    private Button btnAreaBomb;
    private Label lblMagnetCount;
    private Label lblXBombCount;
    private Label lblAreaBombCount;

    [HideInInspector]
    public static List<int> forcedTargetPieceTypes = null;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        if (gridSpawner == null) gridSpawner = FindAnyObjectByType<GridSpawner>();

        if (ProgressionManager.Instance != null)
        {
            int level = ProgressionManager.Instance.currentPlayingLevel;
            
            // DYNAMIC TARGET COUNT SCALING
            // The number of distinct shapes to collect scales with level 
            int numTargetsToSpawn = 1;
            if (level >= 31 && level <= 50) numTargetsToSpawn = 2;
            else if (level >= 51 && level <= 70) numTargetsToSpawn = 3;
            else if (level >= 71 && level <= 100) numTargetsToSpawn = 4;
            else if (level > 100) numTargetsToSpawn = 5;
            if (gridSpawner != null && gridSpawner.prefabsToSpawn != null)
            {
                numTargetsToSpawn = Mathf.Min(numTargetsToSpawn, gridSpawner.prefabsToSpawn.Count);
            }
            // Need to recreate the list fully
            targets = new List<LevelTarget>();
            for(int i = 0; i < numTargetsToSpawn; i++)
            {
                targets.Add(new LevelTarget());
            }

            bool isChallengeLevel = (level % 10 == 0);
            
            if (isChallengeLevel)
            {
                // Challenge levels are stricter on moves! Minimum of 15.
                maxMoves = Mathf.Max(15, 25 - (level / 15));
                
                foreach (var target in targets)
                {
                    // Get base expected total blocks to clear, then verify it's mathematically possible!
                    int totalExpected = 20 + (level * 2);
                    
                    // A very good player clears ~3.5 target blocks per move via cascades/bombs.
                    // We hard-cap the required blocks so it never asks for more than is physically possible.
                    totalExpected = Mathf.Min(totalExpected, (int)(maxMoves * 3.5f));
                    
                    target.amountRequired = Mathf.Max(10, totalExpected / numTargetsToSpawn);
                }
            }
            else
            {
                // Standard progression: Minimum of 25 moves
                maxMoves = Mathf.Max(25, 35 - (level / 10));
                
                foreach (var target in targets)
                {
                    // Slow steady scaling
                    int totalExpected = 15 + level;
                    
                    // A standard player clears ~2.5 blocks of their target per move.
                    // Hard cap to prevent impossible requirements at high levels!
                    totalExpected = Mathf.Min(totalExpected, (int)(maxMoves * 2.5f));
                    
                    target.amountRequired = Mathf.Max(5, totalExpected / numTargetsToSpawn);
                }
            }
            
            currentMoves = maxMoves;
        }

        IsGameActive = true;
        Time.timeScale = 1f;

        // UI Toolkit Querying
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;

            gameWinPanel = root.Q<VisualElement>("gameWinPanel");
            gameOverPanel = root.Q<VisualElement>("gameOverPanel");
            gamePausePanel = root.Q<VisualElement>("gamePausePanel");
            timerText = root.Q<Label>("timerText");
            scoreText = root.Q<Label>("scoreText");
            movesText = root.Q<Label>("movesText");
            livesText = root.Q<Label>("livesText");
            
            // Wire buttons if present
            Button nextLevelButton = root.Q<Button>("nextLevelButton");
            Button winMenuButton = root.Q<Button>("winMenuButton");
            Button retryButton = root.Q<Button>("retryButton");
            Button loseMenuButton = root.Q<Button>("loseMenuButton");
            
            // HUD and Pause Buttons
            Button hudBackButton = root.Q<Button>("hudBackButton");
            Button hudPauseButton = root.Q<Button>("hudPauseButton");
            Button resumeButton = root.Q<Button>("resumeButton");
            Button pauseMenuButton = root.Q<Button>("pauseMenuButton");

            if (nextLevelButton != null) nextLevelButton.clicked += LoadNextLevel;
            if (retryButton != null) retryButton.clicked += RetryLevel;

            // Return to main menu buttons
            if (winMenuButton != null) winMenuButton.clicked += ReturnToMenu;
            if (loseMenuButton != null) loseMenuButton.clicked += ReturnToMenu;
            if (hudBackButton != null) hudBackButton.clicked += ReturnToMenu;
            if (pauseMenuButton != null) pauseMenuButton.clicked += ReturnToMenu;

            // Pause mechanics
            if (hudPauseButton != null) hudPauseButton.clicked += PauseGame;
            if (resumeButton != null) resumeButton.clicked += ResumeGame;

            // Hide Modals initially
            if (gameWinPanel != null) gameWinPanel.style.display = DisplayStyle.None;
            if (gameOverPanel != null) gameOverPanel.style.display = DisplayStyle.None;
            if (gamePausePanel != null) gamePausePanel.style.display = DisplayStyle.None;

            // Powerup Buttons
            btnMagnet = root.Q<Button>("btnMagnet");
            btnXBomb = root.Q<Button>("btnXBomb");
            btnAreaBomb = root.Q<Button>("btnAreaBomb");

            lblMagnetCount = root.Q<Label>("lblMagnetCount");
            lblXBombCount = root.Q<Label>("lblXBombCount");
            lblAreaBombCount = root.Q<Label>("lblAreaBombCount");

            if (btnMagnet != null) btnMagnet.clicked += OnMagnetClicked;
            if (btnXBomb != null) btnXBomb.clicked += () => OnTargetedPowerupClicked("XBomb");
            if (btnAreaBomb != null) btnAreaBomb.clicked += () => OnTargetedPowerupClicked("AreaBomb");

            // Generative UI for Objectives
            VisualElement targetContainer = root.Q<VisualElement>("targetContainer");
            if (targetContainer != null && targetUIAsset != null && targets.Count > 0)
            {
                targetContainer.Clear();
                foreach (var target in targets)
                {
                    TemplateContainer clone = targetUIAsset.Instantiate();
                    target.pieceIcon = clone.Q<VisualElement>("targetIcon");
                    target.progressText = clone.Q<Label>("targetProgressText");
                    targetContainer.Add(clone);
                }
            }
        }

        if (randomizeTargets)
        {
            if (gridSpawner != null && gridSpawner.prefabsToSpawn != null && gridSpawner.prefabsToSpawn.Count > 0)
            {
                RandomizeTargetShapes();
            }
        }

        UpdateTargetsUI();
        UpdatePowerupUI();
    }

    private void UpdatePowerupUI()
    {
        if (ProgressionManager.Instance == null) return;
        if (lblMagnetCount != null) lblMagnetCount.text = ProgressionManager.Instance.MagnetCount.ToString();
        if (lblXBombCount != null) lblXBombCount.text = ProgressionManager.Instance.XBombCount.ToString();
        if (lblAreaBombCount != null) lblAreaBombCount.text = ProgressionManager.Instance.AreaBombCount.ToString();
    }

    private void OnMagnetClicked()
    {
        if (ProgressionManager.Instance != null && ProgressionManager.Instance.MagnetCount > 0 && !IsPaused && IsGameActive && !gridSpawner.isProcessing)
        {
            ProgressionManager.Instance.ConsumeItem("Magnet");
            UpdatePowerupUI();
            if (gridSpawner != null) gridSpawner.DetonateMagnet();
        }
    }

    private void OnTargetedPowerupClicked(string powerupName)
    {
        if (IsPaused || !IsGameActive || gridSpawner.isProcessing) return;
        
        if (ProgressionManager.Instance != null)
        {
            int count = powerupName == "XBomb" ? ProgressionManager.Instance.XBombCount : ProgressionManager.Instance.AreaBombCount;
            if (count > 0)
            {
                ActivePowerup = powerupName;
                Debug.Log($"Targeting mode activated for: {powerupName}. Click a tile on the board!");
            }
        }
    }
    
    public void ExecuteTargetedPowerup(GridPiece targetPiece)
    {
        if (!IsPowerupTargetingMode || targetPiece == null) return;

        ProgressionManager.Instance.ConsumeItem(ActivePowerup);
        UpdatePowerupUI();

        if (ActivePowerup == "XBomb")
        {
            gridSpawner.DetonateXBomb(targetPiece.x, targetPiece.y);
        }
        else if (ActivePowerup == "AreaBomb")
        {
            gridSpawner.DetonateAreaBomb(targetPiece.x, targetPiece.y);
        }

        ActivePowerup = null; // Exit targeting mode
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
                availablePieceIDs.RemoveAt(randomSlotInList); 
                forcedTargetPieceTypes.Add(target.pieceType);
                UpdateUIIcon(target);
            }
        }
    }

    private void UpdateUIIcon(LevelTarget target)
    {
        if (target.pieceIcon != null && target.pieceType >= 0 && target.pieceType < gridSpawner.prefabsToSpawn.Count)
        {
            GameObject chosenPrefab = gridSpawner.prefabsToSpawn[target.pieceType];
            SpriteRenderer sr = chosenPrefab.GetComponentInChildren<SpriteRenderer>();
            
            if (sr != null)
            {
                // UI Toolkit uses Background objects for Sprites instead of Image components
                target.pieceIcon.style.backgroundImage = new StyleBackground(sr.sprite);
                
                Color c = sr.color;
                if(c.a == 0) c.a = 1f; 
                target.pieceIcon.style.unityBackgroundImageTintColor = c;
            }
        }
    }

    private void Update()
    {
        if (!IsGameActive || IsPaused) return;

        // --- PAUSED TIMER LOGIC ---
        // countdownTimer -= Time.deltaTime;
        // if (countdownTimer <= 0f)
        // {
        //     countdownTimer = 0f;
        //     TriggerGameOver();
        // }

        if (timerText != null)
        {
            // timerText.text = $"Time: {Mathf.CeilToInt(countdownTimer)}s";
            timerText.style.display = DisplayStyle.None; // Hide timer
        }

        if (movesText != null)
        {
            movesText.text = $"Moves: {currentMoves}";
        }

        if (livesText != null && ProgressionManager.Instance != null)
        {
            livesText.text = $"Lives: {ProgressionManager.Instance.currentLives}/{ProgressionManager.MAX_LIVES}";
        }

        if (scoreText != null && ScoreManager.Instance != null)
        {
            scoreText.text = $"Score: {ScoreManager.Instance.CurrentScore}";
        }
        
        // CHECK MOVE LIMIT FAIL STATE
        if (currentMoves <= 0 && gridSpawner != null && !gridSpawner.isProcessing && IsGameActive)
        {
            // Since ReportPieceDestroyed would have already triggered a win if targets were met,
            // sitting here with 0 moves and a settled board means we inevitably lost.
            TriggerGameOver();
        }
    }

    public void UseMove()
    {
        if (!IsGameActive || IsPaused) return;
        currentMoves--;
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
        IsPaused = false;
        if (gameWinPanel != null) gameWinPanel.style.display = DisplayStyle.Flex;
        Time.timeScale = 0f; 
        
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.UnlockLevel(ProgressionManager.Instance.currentPlayingLevel + 1);

            // Calculate Stars based on moves remaining
            int earnedStars = 1;
            if (currentMoves >= maxMoves * 0.5f) earnedStars = 3;
            else if (currentMoves >= maxMoves * 0.25f) earnedStars = 2;
            
            ProgressionManager.Instance.AddStars(earnedStars);
            ProgressionManager.Instance.SaveLevelStars(ProgressionManager.Instance.currentPlayingLevel, earnedStars);

            // Update UI with Stars earned (visual star icons)
            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                var starsContainer = root.Q<VisualElement>("starsEarnedContainer");
                if (starsContainer != null)
                {
                    for (int s = 1; s <= 3; s++)
                    {
                        var starLabel = starsContainer.Q<Label>($"winStar{s}");
                        if (starLabel != null)
                        {
                            starLabel.text = s <= earnedStars ? "\u2605" : "\u2606"; // ★ vs ☆
                            starLabel.style.color = s <= earnedStars
                                ? new StyleColor(new Color(1f, 0.84f, 0f))   // Bright gold
                                : new StyleColor(new Color(0.5f, 0.5f, 0.5f)); // Dim grey
                        }
                    }
                }
            }

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
        IsPaused = false;
        if (gameOverPanel != null) gameOverPanel.style.display = DisplayStyle.Flex;
        Time.timeScale = 0f; 
        
        // Penalty for losing!
        if (ProgressionManager.Instance != null)
        {
            ProgressionManager.Instance.LoseLife();
        }
    }

    public void LoadNextLevel()
    {
        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.currentPlayingLevel++;
        
        GridSpawner.forcedNoiseOffset = null;
        LevelManager.forcedTargetPieceTypes = null;
        
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToMenu()
    {
        IsPaused = false;
        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.returnToMenu = true;
            
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
    
    public void PauseGame()
    {
        if (!IsGameActive) return;
        IsPaused = true;
        Time.timeScale = 0f;
        if (gamePausePanel != null) gamePausePanel.style.display = DisplayStyle.Flex;
    }

    public void ResumeGame()
    {
        if (!IsGameActive) return;
        IsPaused = false;
        Time.timeScale = 1f;
        if (gamePausePanel != null) gamePausePanel.style.display = DisplayStyle.None;
    }
}
