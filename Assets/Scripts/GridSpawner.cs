using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A helper class serving as a "Memory Box" whenever the game finds a Match-3.
/// It remembers exactly which pieces formed the match, and whether the line they formed
/// was horizontal, vertical, or a 2x2 square. 
/// </summary>
public class MatchResult
{
    // A dynamically sized list of all the pieces involved in this specific match.
    public List<GridPiece> pieces = new List<GridPiece>();
    
    public bool isHorizontal;
    public bool isVertical;
    public bool isSquare;
}

/// <summary>
/// GridSpawner is the absolute core engine of the game! 
/// It procedurally handles spawning pieces, mathematically detecting matches (horizontal, vertical, squares),
/// handling the "Gravity" that makes pieces fall down, and triggering bombs!
/// </summary>
public class GridSpawner : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Number of columns in the grid (X-axis).")]
    public int columns = 5;

    [Tooltip("Number of rows in the grid (Y-axis).")]
    public int rows = 5;

    [Tooltip("Spacing between each piece along the X axis. Usually slightly larger than the piece's graphic.")]
    public float spacingX = 2f;

    [Tooltip("Spacing between each piece along the Y axis. Usually slightly larger than the piece's graphic.")]
    public float spacingY = 2f;

    [Tooltip("If true, the game automatically calculates offset math so the grid is perfectly centered in the middle of your screen!")]
    public bool centerGrid = true;
    
    [Tooltip("If true, the grid automatically generates itself the instant the game boots up.")]
    public bool spawnOnStart = true;

    [Header("Procedural Generation")]
    [Tooltip("Enable Perlin Noise! This uses a classic math algorithm to generate organic clumped shapes (like lakes or chasms) instead of just pure random static.")]
    public bool usePerlinNoise = true;

    [Tooltip("Scale of the noise. Try setting this to 0.5. A lower value creates massive, slow-rolling hills, while high values create jagged static.")]
    public float noiseScale = 0.5f;

    [Tooltip("If the generated noise drops below this threshold number, the game declares that specific tile 'Unplayable' (like a hole in the ground).")]
    [Range(0f, 1f)]
    public float noiseThreshold = 0.3f;

    [Tooltip("A math trick to force 'holes' to spawn more frequently in the exact center of the map rather than the edges!")]
    [Range(0f, 1f)]
    public float centerBlankBias = 0.5f;

    [Tooltip("An optional physical object to spawn over holes (like a dark pit graphic or water texture).")]
    public GameObject blankAreaPrefab;

    [Header("Board Aesthetics")]
    public bool drawGridBackgrounds = true;
    public Color gridBackgroundColor = new Color(0.1f, 0.2f, 0.4f, 0.5f); // Translucent deep blue
    private Sprite proceduralTileSprite;

    [Header("Spawn Objects")]
    [Tooltip("The actual playable shapes! The game will randomly pick one from this list every time it needs a new piece.")]
    public List<GameObject> prefabsToSpawn;

    [Header("Power Up Prefabs")]
    public GameObject horizontalBombPrefab;
    public GameObject verticalBombPrefab;
    public GameObject colorBombPrefab;

    [Header("Visual Effects")]
    [Tooltip("The particle system prefab to spawn when pieces are destroyed!")]
    public GameObject explosionParticlePrefab;

    // ---------------------------------------------------------
    // INTERNAL MEMORY
    // ---------------------------------------------------------

    // A 2D Array: Think of this as a gigantic Excel Spreadsheet. 
    // If the game asks "What piece is sitting at column 2, row 4?", it looks up "grid[2, 4]".
    public GridPiece[,] grid;
    
    // An identical spreadsheet, but instead of storing pieces, it stores "True/False" to remember if a tile is a hole (unplayable) or solid ground.
    public bool[,] playableCells;

    // The unique 'seed coordinate' used to generate the Perlin Noise map.
    private Vector2 noiseOffset;

    // A crucial safety lock. When true, the board is actively swapping, falling, or exploding. 
    // The InputController reads this, and completely blocks the player from touching anything until it's false!
    public bool isProcessing = false;

    // Memory tracking where the player clicked right before a match so we know EXACTLY where to spawn a bomb powerup.
    private Vector2Int lastSwapPos1 = -Vector2Int.one;
    private Vector2Int lastSwapPos2 = -Vector2Int.one;

    // A STATIC variable persists even if the scene reloads completely! 
    // If the player clicks "Retry Level" we use this memorized seed to force the game to generate the EXACT same layout.
    public static Vector2? forcedNoiseOffset = null;

    // Tracks the physical scale-down mathematical ratio
    private float gridScaleMultiplier = 1f;

    void Start()
    {
        // 1. Dynamic Grid Scaling based on Difficulty
        if (ProgressionManager.Instance != null)
        {
            int level = ProgressionManager.Instance.currentPlayingLevel;
            
            // Scale grid size every 10 levels. e.g. Levels 1-9 = 5x5, 10-19 = 6x6. Capped at 9x9.
            columns = Mathf.Clamp(5 + (level / 10), 5, 9);
            rows = Mathf.Clamp(5 + (level / 10), 5, 9);
        }

        // Squeeze multiplier: Force massive boards to shrink their pieces so it physically fits inside the same space a 5x5 board takes up!
        int largestAxis = Mathf.Max(columns, rows);
        gridScaleMultiplier = 5f / (float)largestAxis;

        // 2. Adjust Camera so it's always fully visible regardless of how large the board became
        AdjustCameraToFitGrid();

        if (drawGridBackgrounds)
        {
            GenerateProceduralTile();
        }

        if (spawnOnStart)
        {
            SpawnGrid();
        }
    }

    private void GenerateProceduralTile()
    {
        Texture2D tex = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        
        proceduralTileSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    private void AdjustCameraToFitGrid()
    {
        if (Camera.main != null)
        {
            float scaledSpacingY = spacingY * gridScaleMultiplier;
            float scaledSpacingX = spacingX * gridScaleMultiplier;
            
            // Calculate necessary orthographic size for height (leaving a tiny padding border instead of a massive one so pieces look visually much larger)
            float heightNeeded = (rows * scaledSpacingY) / 2f + 0.5f; 
            
            float widthNeeded = ((columns * scaledSpacingX) / 2f + 0.5f) / Camera.main.aspect;

            // Pick the larger of the two to ensure both width and height perfectly fit
            Camera.main.orthographicSize = Mathf.Max(3f, Mathf.Max(heightNeeded, widthNeeded));
        }
    }

    /// <summary>
    /// The master method responsible for creating the initial board from scratch.
    /// </summary>
    public void SpawnGrid()
    {
        // Fail-safe. Don't crash if the designer forgot to add the shapes!
        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0)
        {
            Debug.LogWarning("GridSpawner: No prefabs assigned to the 'prefabsToSpawn' list!");
            return;
        }

        // Initialize our empty Excel Spreadsheets to exactly match the requested width and height.
        grid = new GridPiece[columns, rows];
        playableCells = new bool[columns, rows];

        // SEED GENERATION
        // Did the player click "Retry"? If so, use the saved memory seed!
        if (forcedNoiseOffset.HasValue)
        {
            noiseOffset = forcedNoiseOffset.Value;
        }
        else
        {
            // Otherwise, pick a totally random coordinate far out in the mathematical noise field.
            noiseOffset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));
            forcedNoiseOffset = noiseOffset; // Memorize it in case they want to retry later!
        }

        // We calculate the exact mathematical center of the board so we can apply our "Center Bias" trick later.
        float centerX = (columns - 1) / 2f;
        float centerY = (rows - 1) / 2f;
        float maxDist = Mathf.Sqrt(centerX * centerX + centerY * centerY);

        // --------------------------------------------------
        // THE GENERATION LOOP
        // --------------------------------------------------
        // We step over every single column (x) and every single row (y)...
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                bool isPlayable = true;

                // Should we make holes (chasms) in the board?
                if (usePerlinNoise)
                {
                    // Look up the exact height of the Perlin Noise map at this coordinate
                    float perlinValue = Mathf.PerlinNoise((x * noiseScale) + noiseOffset.x, (y * noiseScale) + noiseOffset.y);
                    
                    if (centerBlankBias > 0f)
                    {
                        // Calculate how far we physically are from the center pixel of the board
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                        float normalizedDist = maxDist > 0 ? dist / maxDist : 0;
                        
                        // Apply a heavy visual multiplier. The closer "dist" is to the center, the heavier the penalty!
                        float bias = Mathf.Lerp(centerBlankBias, 0f, normalizedDist);
                        perlinValue -= bias; // Subtract this penalty from the Perlin height!
                    }

                    // Does the terrain survive the cut? Or is it too low?
                    isPlayable = perlinValue >= noiseThreshold;
                }

                // Save our Yes/No decision into the True/False spreadsheet
                playableCells[x, y] = isPlayable;

                if (isPlayable)
                {
                    // It's solid ground! 
                    if (drawGridBackgrounds && proceduralTileSprite != null)
                    {
                        SpawnBackgroundTile(x, y);
                    }
                    
                    // Spawn a random physics piece here.
                    SpawnRandomPrefab(x, y);
                }
                else
                {
                    // It's a hole! Leave the slot blank (null).
                    grid[x, y] = null;
                    
                    // If the designer gave us a graphic for a pit, spawn it here for decoration.
                    if (blankAreaPrefab != null)
                    {
                        Vector3 position = GetWorldPosition(x, y);
                        GameObject blankObject = Instantiate(blankAreaPrefab, position, Quaternion.identity);
                        blankObject.transform.SetParent(this.transform);
                        blankObject.name = $"Blank_{x}_{y}";
                        
                        // Shrink background decorations as well so they don't visually overlap the squeezed board!
                        blankObject.transform.localScale = blankObject.transform.localScale * gridScaleMultiplier;
                    }
                }
            }
        }

        // Because placing entirely random pieces usually accidentally creates 3-in-a-rows right away...
        // We do an immediate "sanity check" to scan the fresh board, and quietly explode any accidental starting matches.
        List<MatchResult> initialMatches = GetMatches();
        if (initialMatches.Count > 0)
        {
            isProcessing = true;
            StartCoroutine(ResolveMatchesCoroutine(initialMatches));
        }
        else
        {
            // Edge case: what if we just randomly generated a board with ZERO valid moves to start with?
            if (!HasPossibleMoves())
            {
                Debug.Log("SpawnGrid created a dead board... Reshuffling immediately!");
                StartCoroutine(ReshuffleBoardCoroutine());
            }
        }
    }

    private void SpawnBackgroundTile(int x, int y)
    {
        GameObject bgObj = new GameObject($"BgTile_{x}_{y}");
        bgObj.transform.position = GetWorldPosition(x, y);
        bgObj.transform.SetParent(this.transform);

        SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
        sr.sprite = proceduralTileSprite;
        sr.color = gridBackgroundColor;
        sr.sortingOrder = -10; // Render strictly behind physical pieces

        // Scale it visually to match the board spacing. The baseline multiplier is raised to make the squares look massive!
        float scaleFactor = 1.05f * gridScaleMultiplier;
        bgObj.transform.localScale = new Vector3(spacingX * scaleFactor, spacingY * scaleFactor, 1f);
    }

    /// <summary>
    /// Spawns a standard game piece (Square, Circle, Star, etc) at a specific coordinate.
    /// </summary>
    private GridPiece SpawnRandomPrefab(int x, int y, Vector3? startOffscreenPos = null)
    {
        // 1. Pick a random number between 0 and the amount of shapes we have.
        int randomIndex = Random.Range(0, prefabsToSpawn.Count);
        GameObject selectedPrefab = prefabsToSpawn[randomIndex];

        if (selectedPrefab != null)
        {
            // 2. Identify exactly where in the Unity 3D/2D space this shape should sit.
            // (If startOffscreenPos is provided, spawn it way up in the sky instead so it can fall down!)
            Vector3 position = startOffscreenPos ?? GetWorldPosition(x, y);
            
            // 3. Physically create the clone.
            GameObject spawnedObject = Instantiate(selectedPrefab, position, Quaternion.identity);
            spawnedObject.transform.SetParent(this.transform);
            
            // Physically boost the mesh size by 1.2x so the graphic fills the square grid nicely, without overlapping into neighbor bounds!
            spawnedObject.transform.localScale = spawnedObject.transform.localScale * gridScaleMultiplier * 1.2f;

            // 4. Attach or locate our GridPiece memory script
            GridPiece piece = spawnedObject.GetComponent<GridPiece>();
            if (piece == null)
            {
                piece = spawnedObject.AddComponent<GridPiece>();
            }

            // 5. Update the memory! Tell the piece who it is, and where it lives!
            piece.pieceType = randomIndex;
            piece.SetCoordinates(x, y);
            
            // 6. Log the piece into our Master Spreadsheet!
            grid[x, y] = piece;
            
            spawnedObject.name = $"Piece_{x}_{y}";
            return piece;
        }
        return null;
    }

    /// <summary>
    /// Specifically used when the player makes a Match-4+ to spawn an explosive Bomb!
    /// </summary>
    private void SpawnSpecialPrefab(int x, int y, GameObject specialPrefab)
    {
        if (specialPrefab == null) return;
        if (!playableCells[x, y]) return; // Failsafe: Don't spawn a bomb in a bottomless pit!
        
        Vector3 position = GetWorldPosition(x, y);
        GameObject spawnedObject = Instantiate(specialPrefab, position, Quaternion.identity);
        spawnedObject.transform.SetParent(this.transform);
        
        // Match the boosted visual size ratio applied to standard pieces (1.2x multiplier).
        spawnedObject.transform.localScale = spawnedObject.transform.localScale * gridScaleMultiplier * 1.2f;

        GridPiece piece = spawnedObject.GetComponent<GridPiece>();
        if (piece == null)
        {
            piece = spawnedObject.AddComponent<GridPiece>();
        }

        // Bombs have an ID of -1 so they NEVER naturally match with regular pieces!
        piece.pieceType = -1; 
        
        // Identify what power it has!
        if (specialPrefab == horizontalBombPrefab) piece.powerUp = PowerUpType.HorizontalBomb;
        else if (specialPrefab == verticalBombPrefab) piece.powerUp = PowerUpType.VerticalBomb;
        else if (specialPrefab == colorBombPrefab) piece.powerUp = PowerUpType.ColorBomb;

        // Failsafe: Ensure user's powerup prefabs are actually clickable by adding a default collider if they forgot one
        if (spawnedObject.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = spawnedObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
        }

        piece.SetCoordinates(x, y);
        grid[x, y] = piece; // Overwrite the now-empty grid slot with our fancy new bomb!
        spawnedObject.name = $"PowerUp_{x}_{y}";
    }

    /// <summary>
    /// Translates a grid coordinate (like Column 2, Row 4) into absolute Unity Space (like X:100.5f, Y:50f, Z:0)
    /// </summary>
    private Vector3 GetWorldPosition(int x, int y)
    {
        Vector3 startPosition = transform.position;
        
        float scaledSpacingX = spacingX * gridScaleMultiplier;
        float scaledSpacingY = spacingY * gridScaleMultiplier;
        
        // Offset math used to ensure the board sits exactly in the center of the camera.
        if (centerGrid)
        {
            float totalWidth = (columns - 1) * scaledSpacingX;
            float totalHeight = (rows - 1) * scaledSpacingY;
            startPosition -= new Vector3(totalWidth / 2f, totalHeight / 2f, 0f);
        }
        
        return startPosition + new Vector3(x * scaledSpacingX, y * scaledSpacingY, 0f);
    }

    /// <summary>
    /// Public helper for the InputController to look up who lives at a specific coordinate!
    /// </summary>
    public GridPiece GetPieceAt(int x, int y)
    {
        if (x >= 0 && x < columns && y >= 0 && y < rows)
            return grid[x, y];
        return null;
    }

    /// <summary>
    /// Re-routes the InputController's swap request into our asynchronous Coroutine logic.
    /// </summary>
    public void AttemptSwap(GridPiece p1, GridPiece p2)
    {
        StartCoroutine(SwapCoroutine(p1, p2));
    }

    /// <summary>
    /// Handles the physics and logic of sliding two pieces and checking if it was a valid move.
    /// </summary>
    private IEnumerator SwapCoroutine(GridPiece p1, GridPiece p2)
    {
        // 1. Lock the board! No one can touch anything while this solves.
        isProcessing = true;

        // 2. Instantly swap their memory coordinates in the Master Spreadsheet
        SwapInternal(p1, p2);
        
        // Memorize exactly where they swapped. If they make a Match-4, we spawn the bomb on THIS exact tile.
        lastSwapPos1 = new Vector2Int(p1.x, p1.y);
        lastSwapPos2 = new Vector2Int(p2.x, p2.y);

        // --- BOMB OVERRIDE ---
        // If either piece is a Bomb, we IGNORE normal matching rules! Swapping a bomb detonates it instantly!
        if (p1.powerUp != PowerUpType.None || p2.powerUp != PowerUpType.None)
        {
            // Tell them to physically slide visually.
            Vector3 pos1 = GetWorldPosition(p1.x, p1.y);
            Vector3 pos2 = GetWorldPosition(p2.x, p2.y);

            p1.MoveToPosition(pos1);
            p2.MoveToPosition(pos2);

            // Wait patiently for the visual sliding animations to finish!
            while (p1.isMoving || p2.isMoving)
                yield return null;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySwapAudio();

            // Consume a move for this action!
            if (LevelManager.Instance != null) LevelManager.Instance.UseMove();

            // Run the huge bomb detonation code!
            yield return StartCoroutine(DetonateBombsCoroutine(p1, p2));
            
            // Unlock the board and EXIT entirely!
            isProcessing = false;
            yield break; 
        }

        // --- NORMAL SWAP ---
        // 3. Scan the whole board. Did our swap physically cause 3 colors to align?
        List<MatchResult> initialMatches = GetMatches();
        bool isValidSwap = initialMatches.Count > 0;

        if (isValidSwap && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwapAudio();
        }

        // 4. Actually instruct the visuals to physically slide places!
        Vector3 pos1_visual = GetWorldPosition(p1.x, p1.y);
        Vector3 pos2_visual = GetWorldPosition(p2.x, p2.y);

        p1.MoveToPosition(pos1_visual);
        p2.MoveToPosition(pos2_visual);

        // Pause loop until pieces finish sliding
        while (p1.isMoving || p2.isMoving)
            yield return null;

        // Wait... verify matches again because animations take time.
        List<MatchResult> matches = GetMatches();

        // 5. IF NO MATCH WAS MADE:
        if (matches.Count == 0)
        {
            // It was an illegal move! Swap their memory back to their original spots.
            SwapInternal(p1, p2);

            // Command them to physically slide BACK backwards to their original spots!
            pos1_visual = GetWorldPosition(p1.x, p1.y);
            pos2_visual = GetWorldPosition(p2.x, p2.y);

            p1.MoveToPosition(pos1_visual);
            p2.MoveToPosition(pos2_visual);

            // Wait for reverse animation
            while (p1.isMoving || p2.isMoving)
                yield return null;
                
            // Unlock the board so they can try again.
            isProcessing = false;
        }
        else
        {
            // 6. IF A VALID MATCH WAS MADE:
            // Consume a move!
            if (LevelManager.Instance != null) LevelManager.Instance.UseMove();

            // This triggers the massive chain reaction! (We pass initial matches since matches 
            // recalculation will just return the same result but we avoid duplicate loops)
            yield return StartCoroutine(ResolveMatchesCoroutine(matches));
        }
    }

    /// <summary>
    /// Complex system mapping exactly what dies when a bomb goes off.
    /// </summary>
    private IEnumerator DetonateBombsCoroutine(GridPiece p1, GridPiece p2)
    {
        // A master hitlist of everything slated to explode.
        List<GridPiece> toDestroy = new List<GridPiece>();

        // Gather targets from Bomb 1 and Bomb 2...
        toDestroy.AddRange(GetBombTargets(p1, p2.pieceType));
        toDestroy.AddRange(GetBombTargets(p2, p1.pieceType));

        // DUPLICATE REMOVAL
        // If a horizontal bomb hits a piece, AND a vertical bomb hits the exact same piece, 
        // we can't blow it up twice! Deduplicate the list using a loop.
        List<GridPiece> distinctToDestroy = new List<GridPiece>();
        foreach (var p in toDestroy)
        {
            if (!distinctToDestroy.Contains(p) && p != null)
            {
                distinctToDestroy.Add(p);
            }
        }

        // ACTUALLY BLOW THEM UP!
        foreach (var match in distinctToDestroy)
        {
            if (match.gameObject != null)
            {
                // Tell the LevelManager "Hey, a Blue piece died!" in case it's one of their objectives!
                if (LevelManager.Instance != null && match.pieceType >= 0)
                {
                    LevelManager.Instance.ReportPieceDestroyed(match.pieceType);
                }

                // Trigger the particle explosion!
                if (explosionParticlePrefab != null)
                {
                    Instantiate(explosionParticlePrefab, match.gameObject.transform.position, Quaternion.identity);
                }

                // Delete it from the spreadsheet
                grid[match.x, match.y] = null;
                
                // Physically delete the 3D model!
                Destroy(match.gameObject);
            }
        }

        // Grant 10 points for every single piece blown up. (e.g. 10 pieces = 100 points instantly!)
        if (ScoreManager.Instance != null && distinctToDestroy.Count > 0)
        {
            ScoreManager.Instance.AddScore(distinctToDestroy.Count * 10);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMatchAudio();
        }

        // Shake the camera violently because bombs are cool!
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake();
        }

        // Pause for a tiny fraction of a second so the player registers the giant explosion visually
        yield return new WaitForSeconds(0.1f);

        // A bomb just cleared out half the board. That means GRAVITY needs to happen.
        // We trick the game into running the gravity phase by sending it an empty match list!
        List<MatchResult> emptyMatchList = new List<MatchResult>();
        yield return StartCoroutine(ResolveMatchesCoroutine(emptyMatchList, false)); // Force a gravity pass
    }

    /// <summary>
    /// Calculates exactly what a specific powerup is touching!
    /// </summary>
    private List<GridPiece> GetBombTargets(GridPiece bomb, int swapTargetPieceType)
    {
        List<GridPiece> targets = new List<GridPiece>();
        
        // Failsafe exit
        if (bomb == null || bomb.powerUp == PowerUpType.None) return targets;

        // Always add the bomb itself to the hitlist!
        targets.Add(bomb);

        // LOGIC: Horizontal Bombs
        if (bomb.powerUp == PowerUpType.HorizontalBomb)
        {
            // Scan across the entire width of the board (all X columns) on the exact Y row where the bomb is sitting!
            for (int x = 0; x < columns; x++)
            {
                if (grid[x, bomb.y] != null) targets.Add(grid[x, bomb.y]);
            }
        }
        // LOGIC: Vertical Bombs
        else if (bomb.powerUp == PowerUpType.VerticalBomb)
        {
            // Scan down the entire height of the board!
            for (int y = 0; y < rows; y++)
            {
                if (grid[bomb.x, y] != null) targets.Add(grid[bomb.x, y]);
            }
        }
        // LOGIC: Disco Color Bombs
        else if (bomb.powerUp == PowerUpType.ColorBomb)
        {
            // Destroy all pieces matching the type of piece the bomb was swapped into!
            // Example: Swap a ColorBomb with a RED square? ALL red squares blow up.
            for (int x = 0; x < columns; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (grid[x, y] != null && grid[x, y].pieceType == swapTargetPieceType)
                    {
                        targets.Add(grid[x, y]);
                    }
                }
            }
        }

        return targets;
    }

    /// <summary>
    /// The Master loop that loops endlessly until NO MORE MATCHES occur. 
    /// Handles cascades, chain reactions, and gravity!
    /// </summary>
    private IEnumerator ResolveMatchesCoroutine(List<MatchResult> initialMatches, bool requiresMatchCheck = true)
    {
        List<MatchResult> currentMatches = initialMatches;
        bool firstRun = !requiresMatchCheck;

        // CORE LOOP: As long as there are matches, OR this is a forced gravity run... keep looping!
        while (currentMatches.Count > 0 || firstRun)
        {
            if (currentMatches.Count > 0)
            {
                List<GridPiece> toDestroy = new List<GridPiece>();
                
                // Using a Dictionary keyed by Vector2Int (map coordinates) perfectly prevents "ghosting"
                // where two intersecting matches might accidentally spawn two bombs occupying the EXACT same physical tile!
                Dictionary<Vector2Int, System.Action> spawnBombActions = new Dictionary<Vector2Int, System.Action>();

                // STEP 1: Process special combinations (Match-4, Match-5, Squares)
                foreach (MatchResult match in currentMatches)
                {
                    // SQUARE MATCH (2x2) -> Spawn Color Bomb (Disco Ball)
                    if (match.isSquare && match.pieces.Count >= 4)
                    {
                        // Pinpoint where the player touched so the bomb spawns exactly under their finger.
                        GridPiece targetSlot = GetTargetSlotBest(match, lastSwapPos1, lastSwapPos2); 
                        if (targetSlot != null)
                        {
                            int cx = targetSlot.x; int cy = targetSlot.y;
                            Vector2Int coords = new Vector2Int(cx, cy);
                            
                            // Log the request to spawn a bomb into our Dictionary! We delay spawning it until AFTER the pieces are deleted!
                            if (!spawnBombActions.ContainsKey(coords))
                                spawnBombActions.Add(coords, () => SpawnSpecialPrefab(cx, cy, colorBombPrefab));
                        }
                    }
                    // HORIZONTAL MATCH-4 -> Spawn Horizontal Bomb
                    else if (match.isHorizontal && match.pieces.Count >= 4)
                    {
                        GridPiece targetSlot = GetTargetSlotBest(match, lastSwapPos1, lastSwapPos2);
                        if (targetSlot != null)
                        {
                            int cx = targetSlot.x; int cy = targetSlot.y;
                            Vector2Int coords = new Vector2Int(cx, cy);
                            if (!spawnBombActions.ContainsKey(coords))
                                spawnBombActions.Add(coords, () => SpawnSpecialPrefab(cx, cy, horizontalBombPrefab));
                        }
                    }
                    // VERTICAL MATCH-4 -> Spawn Vertical Bomb
                    else if (match.isVertical && match.pieces.Count >= 4)
                    {
                        GridPiece targetSlot = GetTargetSlotBest(match, lastSwapPos1, lastSwapPos2);
                        if (targetSlot != null)
                        {
                            int cx = targetSlot.x; int cy = targetSlot.y;
                            Vector2Int coords = new Vector2Int(cx, cy);
                            if (!spawnBombActions.ContainsKey(coords))
                                spawnBombActions.Add(coords, () => SpawnSpecialPrefab(cx, cy, verticalBombPrefab));
                        }
                    }

                    // Collect all valid pieces for deletion (even if they formed a bomb, we must delete them!)
                    foreach (GridPiece p in match.pieces)
                    {
                        if (p != null && !toDestroy.Contains(p)) toDestroy.Add(p);
                    }
                }

                // STEP 2: Demolition!
                int destroyCount = 0;
                foreach (GridPiece match in toDestroy)
                {
                    if (match != null && match.gameObject != null)
                    {
                        // Ping the LevelManager for objectives...
                        if (LevelManager.Instance != null && match.pieceType >= 0)
                        {
                            LevelManager.Instance.ReportPieceDestroyed(match.pieceType);
                        }

                        // Trigger the particle explosion!
                        if (explosionParticlePrefab != null)
                        {
                            Instantiate(explosionParticlePrefab, match.gameObject.transform.position, Quaternion.identity);
                        }

                        // Wipe from memory and reality.
                        grid[match.x, match.y] = null;
                        Destroy(match.gameObject);
                        destroyCount++;
                    }
                }

                // STEP 3: Now that slots are perfectly clear, physically spawn the pending Bombs!
                foreach (var spawnActionKV in spawnBombActions)
                {
                    // "Invoke" runs the arbitrary Action logic we saved in the Dictionary earlier!
                    spawnActionKV.Value.Invoke();
                }

                // STEP 4: Audio and Score
                if (ScoreManager.Instance != null && destroyCount > 0)
                {
                    ScoreManager.Instance.AddScore(destroyCount * 10);
                }

                if (AudioManager.Instance != null && destroyCount > 0)
                {
                    AudioManager.Instance.PlayMatchAudio();
                }

                // Give the player 0.1 seconds to enjoy the visual pop!
                yield return new WaitForSeconds(0.1f);
            }

            firstRun = false; 
            // Wipe memory of the player's swipe so bombs spawned by random chain-reactions appear in random spots 
            // rather than clinging to the player's finger location.
            lastSwapPos1 = -Vector2Int.one;
            lastSwapPos2 = -Vector2Int.one;

            // STEP 5: Gravity! Make the pieces slide down into the empty holes!
            List<GridPiece> movedPieces = ApplyGravityAndRefill();

            // STEP 6: Wait patiently for ALL falling pieces to smack into the ground!
            bool isStillMoving = true;
            while (isStillMoving)
            {
                isStillMoving = false;
                foreach (GridPiece p in movedPieces)
                {
                    // Has the sliding Coroutine finished for every single piece?
                    if (p != null && p.isMoving)
                    {
                        isStillMoving = true;
                        break;
                    }
                }
                // Pause for a frame if they are still falling!
                yield return null;
            }

            // STEP 7: CHAIN REACTIONS! 
            // Now that everything landed, check the board again. Did they accidentally make another Match-3 while falling?
            currentMatches = GetMatches();
            
            // NOTE: Since we are in a 'while' loop, if currentMatches.Count > 0, the ENTIRE demolition logic repeats!
        }

        // Loop finished. No more matches found. The board settles.
        
        // Final sanity check: Are there actually any legal moves left for the player to make?
        if (!HasPossibleMoves())
        {
            Debug.Log("No valid moves remain! Reshuffling the board...");
            StartCoroutine(ReshuffleBoardCoroutine());
        }
        else
        {
            // Unlock the controls!
            isProcessing = false;
        }
    }

    /// <summary>
    /// Searches upward to find floating pieces, drops them into holes, and spawns new ones out of thin air at the top!
    /// </summary>
    private List<GridPiece> ApplyGravityAndRefill()
    {
        // Tracks everything that actually fell or popped in so we can wait for their animation to finish later.
        List<GridPiece> movedPieces = new List<GridPiece>();

        // IMPORTANT GAME DEV TRICK: We scan bottom to top!
        // This ensures lower pieces fall first, clearing way for top pieces to slide smoothly!
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                // Unplayable Hole? Ignore it! Pieces slip over it.
                if (!playableCells[x, y]) continue; 

                // Is THIS exact slot empty from a recent demolition?
                if (grid[x, y] == null)
                {
                    // Scan vertically up the column, searching for the nearest survivor piece!
                    GridPiece pieceAbove = null;
                    for (int nextY = y + 1; nextY < rows; nextY++)
                    {
                        if (grid[x, nextY] != null)
                        {
                            // Found a target! Grab it from memory, and delete its old data since it's going to fall.
                            pieceAbove = grid[x, nextY];
                            grid[x, nextY] = null;
                            break; // Stop looking up! We found one!
                        }
                    }

                    // A: WE FOUND SURVIVORS ABOVE
                    if (pieceAbove != null)
                    {
                        // Pull piece down into this slot in the Spreadsheet.
                        grid[x, y] = pieceAbove;
                        pieceAbove.SetCoordinates(x, y);
                        // Trigger the visual slide down.
                        pieceAbove.MoveToPosition(GetWorldPosition(x, y));
                        movedPieces.Add(pieceAbove);
                    }
                    // B: COLUMN IS EMPTY (REFILL MODE)
                    else
                    {
                        // No survivors existed above it. We must spawn brand new pieces off-screen high in the sky!
                        Vector3 dropPos = GetWorldPosition(x, y);
                        
                        // Spawn them (rows * spacingY) units artificially higher into the unseen camera void!
                        float scaledSpacingY = spacingY * gridScaleMultiplier;
                        Vector3 offscreenPos = dropPos + new Vector3(0, rows * scaledSpacingY, 0); 
                        
                        GridPiece newPiece = SpawnRandomPrefab(x, y, offscreenPos);
                        
                        if (newPiece != null)
                        {
                            // Trigger the visual falling drop animation from the sky void down onto the board.
                            newPiece.MoveToPosition(dropPos);
                            movedPieces.Add(newPiece);
                        }
                    }
                }
            }
        }
        
        return movedPieces;
    }

    /// <summary>
    /// Helper to find out where the player swiped. Prioritizes spawning bonuses where the finger touched!
    /// </summary>
    private GridPiece GetTargetSlotBest(MatchResult match, Vector2Int swap1, Vector2Int swap2)
    {
        if (match.pieces == null || match.pieces.Count == 0) return null;
        
        // Scan the pieces involved in the match. Were any of them the exact coordinate the player swapped?
        foreach (var p in match.pieces)
        {
            if (p != null && p.x == swap1.x && p.y == swap1.y) return p;
            if (p != null && p.x == swap2.x && p.y == swap2.y) return p;
        }
        // Fallback: If it was an accidental cascade, just spawn it lazily on the first block in the line!
        return match.pieces[0]; 
    }

    /// <summary>
    /// Swaps the deep Spreadsheet memory of two objects, without any visual fluff.
    /// </summary>
    private void SwapInternal(GridPiece p1, GridPiece p2)
    {
        // Must use a temporary memory holding-cell so data isn't wiped instantly
        int tempX = p1.x;
        int tempY = p1.y;

        p1.SetCoordinates(p2.x, p2.y);
        p2.SetCoordinates(tempX, tempY);

        grid[p1.x, p1.y] = p1;
        grid[p2.x, p2.y] = p2;
    }

    /// <summary>
    /// The complex Scanning Engine that detects Match-3s! Think of this like a laser scanner sliding horizontally and vertically across the board.
    /// </summary>
    /// <returns>A huge list containing all matches discovered across the board.</returns>
    private List<MatchResult> GetMatches()
    {
        List<MatchResult> allMatches = new List<MatchResult>();
        List<GridPiece> matchedPieces = new List<GridPiece>();

        // 1. SCAN FOR SQUARES (2x2)
        // Iterate slightly less than maximum to prevent 'IndexOutOfBounds' crashes on the right edges!
        for (int x = 0; x < columns - 1; x++)
        {
            for (int y = 0; y < rows - 1; y++)
            {
                // Grab the 4 pieces comprising a tiny 2x2 box
                GridPiece p1 = grid[x, y];
                GridPiece p2 = grid[x + 1, y];
                GridPiece p3 = grid[x, y + 1];
                GridPiece p4 = grid[x + 1, y + 1];

                if (p1 != null && p2 != null && p3 != null && p4 != null)
                {
                    // IMPORTANT: We ignore negative piece types (-1). Powerups cannot naturally match with anything!
                    if (p1.pieceType != -1 && p1.pieceType == p2.pieceType && p2.pieceType == p3.pieceType && p3.pieceType == p4.pieceType)
                    {
                        // Found a matching box of 4 colors! 
                        if (!matchedPieces.Contains(p1)) // Only register square once so it drops exactly ONE bomb, not four!
                        {
                            var match = new MatchResult { isSquare = true }; // Flag it as a square trigger!
                            match.pieces.Add(p1); match.pieces.Add(p2); match.pieces.Add(p3); match.pieces.Add(p4);
                            allMatches.Add(match);
                            matchedPieces.AddRange(match.pieces);
                        }
                    }
                }
            }
        }

        // 2. SCAN FOR HORIZONTAL (Rows)
        for (int y = 0; y < rows; y++)
        {
            // Stop scanning at columns-2 because you physically cannot fit a "Match-3" in the final 2 columns anyway!
            for (int x = 0; x < columns - 2; x++)
            {
                GridPiece p1 = grid[x, y];
                if (p1 == null || p1.pieceType == -1) continue;

                int matchLength = 1;
                
                // Keep shooting a horizontal laser rightwards until we hit an edge or a non-matching color.
                while (x + matchLength < columns)
                {
                    GridPiece nextPiece = grid[x + matchLength, y];
                    if (nextPiece != null && nextPiece.pieceType == p1.pieceType) matchLength++;
                    else break; // Hit something different! Quit searching!
                }

                // If the laser found 3 or more contiguous colors... 
                if (matchLength >= 3)
                {
                    MatchResult rowMatch = new MatchResult { isHorizontal = true };
                    for (int i = 0; i < matchLength; i++)
                    {
                        rowMatch.pieces.Add(grid[x + i, y]);
                    }
                    allMatches.Add(rowMatch);
                    
                    // PERFORMANCE BOOST! Skip our scanning 'x' coordinate forward ahead of the match we just found! 
                    // This wildly optimizes CPU, and ensures a "Match-5" doesn't falsely get read as three broken overlapping Match-3s!
                    x += matchLength - 1; 
                }
            }
        }

        // 3. SCAN FOR VERTICAL (Columns)
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows - 2; y++)
            {
                GridPiece p1 = grid[x, y];
                if (p1 == null || p1.pieceType == -1) continue;

                int matchLength = 1;
                while (y + matchLength < rows)
                {
                    GridPiece nextPiece = grid[x, y + matchLength];
                    if (nextPiece != null && nextPiece.pieceType == p1.pieceType) matchLength++;
                    else break;
                }

                if (matchLength >= 3)
                {
                    MatchResult colMatch = new MatchResult { isVertical = true };
                    for (int i = 0; i < matchLength; i++)
                    {
                        colMatch.pieces.Add(grid[x, y + i]);
                    }
                    allMatches.Add(colMatch);
                    y += matchLength - 1; // skip ahead
                }
            }
        }

        // Send our master list back up to the loop for demolition!
        return allMatches;
    }

    /// <summary>
    /// ContextMenu lets designers right-click the Script component in the Unity Editor and trigger "Clear Grid" manually.
    /// Helpful for debugging!
    /// </summary>
    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        // Scans from the absolute highest child down to 0, completely wiping them. 
        // We go backwards (i--) because deleting objects forwards (0 -> 10) screws up the index list as it shrinks!
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            #if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject); // Hard delete if we are in Editor mode (not playing)
            #else
            Destroy(transform.GetChild(i).gameObject); // Soft delete during gameplay
            #endif
        }
        grid = null;
    }

    /// <summary>
    /// Checks the entire board to see if there is at least one legal Match-3 move remaining.
    /// </summary>
    public bool HasPossibleMoves()
    {
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GridPiece p1 = grid[x, y];
                if (p1 == null) continue;

                // Bombs can always be swiped to detonate instantly! This counts as a valid move!
                if (p1.powerUp != PowerUpType.None) return true;

                // Try swiping right
                if (x + 1 < columns)
                {
                    GridPiece p2 = grid[x + 1, y];
                    if (p2 != null && p2.powerUp == PowerUpType.None)
                    {
                        if (SimulateSwapAndCheck(p1, p2)) return true;
                    }
                }

                // Try swiping up
                if (y + 1 < rows)
                {
                    GridPiece p2 = grid[x, y + 1];
                    if (p2 != null && p2.powerUp == PowerUpType.None)
                    {
                        if (SimulateSwapAndCheck(p1, p2)) return true;
                    }
                }
            }
        }

        // We scanned the whole board and found 0 moves. The player is softlocked!
        return false;
    }

    /// <summary>
    /// Temporarily swaps the memory of two pieces and checks if it mathematically triggers a match.
    /// </summary>
    private bool SimulateSwapAndCheck(GridPiece p1, GridPiece p2)
    {
        if (p1 == null || p2 == null) return false;
        if (p1.pieceType == -1 || p2.pieceType == -1) return false; 

        // 1. Temporarily swap their internal color types
        int tempType = p1.pieceType;
        p1.pieceType = p2.pieceType;
        p2.pieceType = tempType;

        // 2. Scan the board! Did our fake swap cause a Match-3?
        bool hasMatch = GetMatches().Count > 0;

        // 3. Revert their colors back to normal before anyone notices!
        p2.pieceType = p1.pieceType; 
        p1.pieceType = tempType;

        return hasMatch;
    }

    /// <summary>
    /// Flushes the entire board and drops in fresh pieces when the player runs out of moves.
    /// </summary>
    private IEnumerator ReshuffleBoardCoroutine()
    {
        // Lock the board (already locked from ResolveMatches, but safe to explicitly state)
        isProcessing = true;
        
        // Give the player a tiny window to realize nothing matchable exists
        yield return new WaitForSeconds(0.5f);

        // Nuke all the physical objects
        ClearGrid();
        
        // A visual pause before dropping in fresh ones
        yield return new WaitForSeconds(0.5f);

        // Unlock the board before resolving, SpawnGrid handles relocking if there are accidental matches!
        isProcessing = false;
        
        // Respawn the entire board from scratch!
        SpawnGrid();
    }
}
