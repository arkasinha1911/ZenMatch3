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
            
            bool isChallengeLevel = (level % 10 == 0);
            
            if (isChallengeLevel)
            {
                // Challenge levels are much stricter on time and require more shapes!
                // countdownTimer = Mathf.Max(20f, 45f - (level * 2.0f)); 
                maxMoves = Mathf.Max(15, 30 - (level / 2));
                
                foreach (var target in targets)
                {
                    target.amountRequired = 20 + (level * 5);
                }
            }
            else
            {
                // Standard progression
                // countdownTimer = Mathf.Max(30f, 60f - (level * 1.5f));
                maxMoves = Mathf.Max(20, 40 - (level / 3));
                
                foreach (var target in targets)
                {
                    target.amountRequired = 10 + (level * 3);
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
            if (gridSpawner == null) gridSpawner = FindObjectOfType<GridSpawner>();
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
