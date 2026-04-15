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
    private Label timerText;

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
            countdownTimer = Mathf.Max(30f, 60f - (level * 1.5f));
            foreach (var target in targets)
            {
                target.amountRequired = 10 + (level * 3);
            }
        }

        IsGameActive = true;
        Time.timeScale = 1f;

        // UI Toolkit Querying
        if (uiDocument != null)
        {
            var root = uiDocument.rootVisualElement;

            gameWinPanel = root.Q<VisualElement>("gameWinPanel");
            gameOverPanel = root.Q<VisualElement>("gameOverPanel");
            timerText = root.Q<Label>("timerText");
            
            // Wire buttons if present
            Button nextLevelButton = root.Q<Button>("nextLevelButton");
            Button winMenuButton = root.Q<Button>("winMenuButton");
            Button retryButton = root.Q<Button>("retryButton");
            Button loseMenuButton = root.Q<Button>("loseMenuButton");

            if (nextLevelButton != null) nextLevelButton.clicked += LoadNextLevel;
            if (retryButton != null) retryButton.clicked += RetryLevel;

            // Optional: return to main menu buttons
            if (winMenuButton != null) winMenuButton.clicked += ReturnToMenu;
            if (loseMenuButton != null) loseMenuButton.clicked += ReturnToMenu;

            // Hide Modals initially
            if (gameWinPanel != null) gameWinPanel.style.display = DisplayStyle.None;
            if (gameOverPanel != null) gameOverPanel.style.display = DisplayStyle.None;

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
        if (gameOverPanel != null) gameOverPanel.style.display = DisplayStyle.Flex;
        Time.timeScale = 0f; 
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
        if (ProgressionManager.Instance != null)
            ProgressionManager.Instance.returnToMenu = true;
            
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }
}
