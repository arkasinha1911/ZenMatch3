using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchResult
{
    public List<GridPiece> pieces = new List<GridPiece>();
    public bool isHorizontal;
    public bool isVertical;
    public bool isSquare;
}

public class GridSpawner : MonoBehaviour
{
    [Header("Grid Settings")]
    [Tooltip("Number of columns in the grid (X-axis).")]
    public int columns = 5;

    [Tooltip("Number of rows in the grid (Y-axis).")]
    public int rows = 5;

    [Tooltip("Spacing between each object along the X axis.")]
    public float spacingX = 2f;

    [Tooltip("Spacing between each object along the Y axis.")]
    public float spacingY = 2f;

    [Tooltip("If true, the grid will be centered on this GameObject's position.")]
    public bool centerGrid = true;
    
    [Tooltip("If true, the grid will be generated when the game starts.")]
    public bool spawnOnStart = true;

    [Header("Procedural Generation")]
    [Tooltip("Enable Perlin Noise for generating lakes/chasms.")]
    public bool usePerlinNoise = true;

    [Tooltip("Scale of the Perlin Noise. Lower values mean larger clustered areas.")]
    public float noiseScale = 0.5f;

    [Tooltip("Threshold below which a cell becomes a 'blank' unplayable area.")]
    [Range(0f, 1f)]
    public float noiseThreshold = 0.3f;

    [Tooltip("Biases blank areas to cluster at the center. At 0, no bias. At 1, the center is heavily biased to be blank.")]
    [Range(0f, 1f)]
    public float centerBlankBias = 0.5f;

    [Tooltip("Optional prefab to spawn in 'blank' areas (e.g., a chasm or lake texture).")]
    public GameObject blankAreaPrefab;

    [Header("Spawn Objects")]
    [Tooltip("List of prefabs to spawn. A random prefab from this list will be chosen for each grid cell.")]
    public List<GameObject> prefabsToSpawn;

    [Header("Power Up Prefabs")]
    public GameObject horizontalBombPrefab;
    public GameObject verticalBombPrefab;
    public GameObject colorBombPrefab;

    public GridPiece[,] grid;
    public bool[,] playableCells;
    private Vector2 noiseOffset;

    public bool isProcessing = false;
    private Vector2Int lastSwapPos1 = -Vector2Int.one;
    private Vector2Int lastSwapPos2 = -Vector2Int.one;

    public static Vector2? forcedNoiseOffset = null;

    void Start()
    {
        if (spawnOnStart)
        {
            SpawnGrid();
        }
    }

    public void SpawnGrid()
    {
        if (prefabsToSpawn == null || prefabsToSpawn.Count == 0)
        {
            Debug.LogWarning("GridSpawner: No prefabs assigned to the 'prefabsToSpawn' list!");
            return;
        }

        grid = new GridPiece[columns, rows];
        playableCells = new bool[columns, rows];

        // Randomize noise offset so each map layout is unique OR reuse static seed if Retrying!
        if (forcedNoiseOffset.HasValue)
        {
            noiseOffset = forcedNoiseOffset.Value;
        }
        else
        {
            noiseOffset = new Vector2(Random.Range(0f, 1000f), Random.Range(0f, 1000f));
            forcedNoiseOffset = noiseOffset;
        }

        float centerX = (columns - 1) / 2f;
        float centerY = (rows - 1) / 2f;
        float maxDist = Mathf.Sqrt(centerX * centerX + centerY * centerY);

        // Generate the grid
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                bool isPlayable = true;

                if (usePerlinNoise)
                {
                    float perlinValue = Mathf.PerlinNoise((x * noiseScale) + noiseOffset.x, (y * noiseScale) + noiseOffset.y);
                    
                    if (centerBlankBias > 0f)
                    {
                        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                        float normalizedDist = maxDist > 0 ? dist / maxDist : 0;
                        
                        // Decrease the perlin value more heavily at the center
                        float bias = Mathf.Lerp(centerBlankBias, 0f, normalizedDist);
                        perlinValue -= bias;
                    }

                    isPlayable = perlinValue >= noiseThreshold;
                }

                playableCells[x, y] = isPlayable;

                if (isPlayable)
                {
                    SpawnRandomPrefab(x, y);
                }
                else
                {
                    // Unplayable 'blank' area
                    grid[x, y] = null;
                    if (blankAreaPrefab != null)
                    {
                        Vector3 position = GetWorldPosition(x, y);
                        GameObject blankObject = Instantiate(blankAreaPrefab, position, Quaternion.identity);
                        blankObject.transform.SetParent(this.transform);
                        blankObject.name = $"Blank_{x}_{y}";
                    }
                }
            }
        }

        // Check and resolve any matches that randomly formed during initial board spawn
        List<MatchResult> initialMatches = GetMatches();
        if (initialMatches.Count > 0)
        {
            isProcessing = true;
            StartCoroutine(ResolveMatchesCoroutine(initialMatches));
        }
    }

    private GridPiece SpawnRandomPrefab(int x, int y, Vector3? startOffscreenPos = null)
    {
        // Pick a random prefab from the list
        int randomIndex = Random.Range(0, prefabsToSpawn.Count);
        GameObject selectedPrefab = prefabsToSpawn[randomIndex];

        if (selectedPrefab != null)
        {
            Vector3 position = startOffscreenPos ?? GetWorldPosition(x, y);
            GameObject spawnedObject = Instantiate(selectedPrefab, position, Quaternion.identity);
            spawnedObject.transform.SetParent(this.transform);

            GridPiece piece = spawnedObject.GetComponent<GridPiece>();
            if (piece == null)
            {
                piece = spawnedObject.AddComponent<GridPiece>();
            }

            piece.pieceType = randomIndex;
            piece.SetCoordinates(x, y);
            grid[x, y] = piece;
            spawnedObject.name = $"Piece_{x}_{y}";
            return piece;
        }
        return null;
    }

    private void SpawnSpecialPrefab(int x, int y, GameObject specialPrefab)
    {
        if (specialPrefab == null) return;
        if (!playableCells[x, y]) return;
        
        Vector3 position = GetWorldPosition(x, y);
        GameObject spawnedObject = Instantiate(specialPrefab, position, Quaternion.identity);
        spawnedObject.transform.SetParent(this.transform);

        GridPiece piece = spawnedObject.GetComponent<GridPiece>();
        if (piece == null)
        {
            piece = spawnedObject.AddComponent<GridPiece>();
        }

        piece.pieceType = -1; // Prevent standard matching
        if (specialPrefab == horizontalBombPrefab) piece.powerUp = PowerUpType.HorizontalBomb;
        else if (specialPrefab == verticalBombPrefab) piece.powerUp = PowerUpType.VerticalBomb;
        else if (specialPrefab == colorBombPrefab) piece.powerUp = PowerUpType.ColorBomb;

        // Ensure user's powerup prefabs are actually clickable by adding a default collider if they forgot one
        if (spawnedObject.GetComponent<Collider2D>() == null)
        {
            BoxCollider2D col = spawnedObject.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.8f, 0.8f);
        }

        piece.SetCoordinates(x, y);
        grid[x, y] = piece; // Overwrites normally empty slot logic since Destruct passes it to null first
        spawnedObject.name = $"PowerUp_{x}_{y}";
    }

    private Vector3 GetWorldPosition(int x, int y)
    {
        Vector3 startPosition = transform.position;
        if (centerGrid)
        {
            float totalWidth = (columns - 1) * spacingX;
            float totalHeight = (rows - 1) * spacingY;
            startPosition -= new Vector3(totalWidth / 2f, totalHeight / 2f, 0f);
        }
        return startPosition + new Vector3(x * spacingX, y * spacingY, 0f);
    }

    public GridPiece GetPieceAt(int x, int y)
    {
        if (x >= 0 && x < columns && y >= 0 && y < rows)
            return grid[x, y];
        return null;
    }

    public void AttemptSwap(GridPiece p1, GridPiece p2)
    {
        StartCoroutine(SwapCoroutine(p1, p2));
    }

    private IEnumerator SwapCoroutine(GridPiece p1, GridPiece p2)
    {
        isProcessing = true;

        SwapInternal(p1, p2);
        
        lastSwapPos1 = new Vector2Int(p1.x, p1.y);
        lastSwapPos2 = new Vector2Int(p2.x, p2.y);

        // BOMB TRIGGER: If either is a bomb, trigger instantly without needing a match!
        if (p1.powerUp != PowerUpType.None || p2.powerUp != PowerUpType.None)
        {
            Vector3 pos1 = GetWorldPosition(p1.x, p1.y);
            Vector3 pos2 = GetWorldPosition(p2.x, p2.y);

            p1.MoveToPosition(pos1);
            p2.MoveToPosition(pos2);

            while (p1.isMoving || p2.isMoving)
                yield return null;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySwapAudio();

            yield return StartCoroutine(DetonateBombsCoroutine(p1, p2));
            
            isProcessing = false;
            yield break; // End execution early so normal matching doesn't occur.
        }

        // NORMAL SWAP
        // Check if this swap will result in a match
        List<MatchResult> initialMatches = GetMatches();
        bool isValidSwap = initialMatches.Count > 0;

        if (isValidSwap && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySwapAudio();
        }

        // Perform visual swap
        Vector3 pos1_visual = GetWorldPosition(p1.x, p1.y);
        Vector3 pos2_visual = GetWorldPosition(p2.x, p2.y);

        p1.MoveToPosition(pos1_visual);
        p2.MoveToPosition(pos2_visual);

        while (p1.isMoving || p2.isMoving)
            yield return null;

        // Check for matches
        List<MatchResult> matches = GetMatches();

        if (matches.Count == 0)
        {
            // SWAP BACK if no matches
            SwapInternal(p1, p2);

            pos1_visual = GetWorldPosition(p1.x, p1.y);
            pos2_visual = GetWorldPosition(p2.x, p2.y);

            p1.MoveToPosition(pos1_visual);
            p2.MoveToPosition(pos2_visual);

            while (p1.isMoving || p2.isMoving)
                yield return null;
                
            isProcessing = false;
        }
        else
        {
            // Proceed to resolve matches and trigger gravity
            StartCoroutine(ResolveMatchesCoroutine(matches));
        }
    }

    private IEnumerator DetonateBombsCoroutine(GridPiece p1, GridPiece p2)
    {
        List<GridPiece> toDestroy = new List<GridPiece>();

        // Find standard targets
        toDestroy.AddRange(GetBombTargets(p1, p2.pieceType));
        toDestroy.AddRange(GetBombTargets(p2, p1.pieceType));

        // Deduplicate targets
        List<GridPiece> distinctToDestroy = new List<GridPiece>();
        foreach (var p in toDestroy)
        {
            if (!distinctToDestroy.Contains(p) && p != null)
            {
                distinctToDestroy.Add(p);
            }
        }

        foreach (var match in distinctToDestroy)
        {
            if (match.gameObject != null)
            {
                if (LevelManager.Instance != null && match.pieceType >= 0)
                {
                    LevelManager.Instance.ReportPieceDestroyed(match.pieceType);
                }

                grid[match.x, match.y] = null;
                Destroy(match.gameObject);
            }
        }

        // Track Score
        if (ScoreManager.Instance != null && distinctToDestroy.Count > 0)
        {
            ScoreManager.Instance.AddScore(distinctToDestroy.Count * 10);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMatchAudio();
        }

        yield return new WaitForSeconds(0.1f);

        // Resume standard resolution loop since the board is now missing pieces
        List<MatchResult> emptyMatchList = new List<MatchResult>();
        yield return StartCoroutine(ResolveMatchesCoroutine(emptyMatchList, false)); // Force a gravity pass
    }

    private List<GridPiece> GetBombTargets(GridPiece bomb, int swapTargetPieceType)
    {
        List<GridPiece> targets = new List<GridPiece>();
        if (bomb == null || bomb.powerUp == PowerUpType.None) return targets;

        // Always destroy the bomb itself
        targets.Add(bomb);

        if (bomb.powerUp == PowerUpType.HorizontalBomb)
        {
            for (int x = 0; x < columns; x++)
            {
                if (grid[x, bomb.y] != null) targets.Add(grid[x, bomb.y]);
            }
        }
        else if (bomb.powerUp == PowerUpType.VerticalBomb)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[bomb.x, y] != null) targets.Add(grid[bomb.x, y]);
            }
        }
        else if (bomb.powerUp == PowerUpType.ColorBomb)
        {
            // Destroy all pieces matching the swapped piece's type
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

    private IEnumerator ResolveMatchesCoroutine(List<MatchResult> initialMatches, bool requiresMatchCheck = true)
    {
        List<MatchResult> currentMatches = initialMatches;
        bool firstRun = !requiresMatchCheck;

        while (currentMatches.Count > 0 || firstRun)
        {
            if (currentMatches.Count > 0)
            {
                List<GridPiece> toDestroy = new List<GridPiece>();
                
                // Using a dictionary keyed by map coordinate perfectly prevents overlapping intersecting match logic 
                // from spawning two bombs on the exact same tile, leaving ghost visuals behind!
                Dictionary<Vector2Int, System.Action> spawnBombActions = new Dictionary<Vector2Int, System.Action>();

                // Process special combinations and destruction
                foreach (MatchResult match in currentMatches)
                {
                    if (match.isSquare && match.pieces.Count >= 4)
                    {
                        GridPiece targetSlot = GetTargetSlotBest(match, lastSwapPos1, lastSwapPos2); 
                        if (targetSlot != null)
                        {
                            int cx = targetSlot.x; int cy = targetSlot.y;
                            Vector2Int coords = new Vector2Int(cx, cy);
                            if (!spawnBombActions.ContainsKey(coords))
                                spawnBombActions.Add(coords, () => SpawnSpecialPrefab(cx, cy, colorBombPrefab));
                        }
                    }
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

                    // Collect all valid pieces for deletion
                    foreach (GridPiece p in match.pieces)
                    {
                        if (p != null && !toDestroy.Contains(p)) toDestroy.Add(p);
                    }
                }

                int destroyCount = 0;
                foreach (GridPiece match in toDestroy)
                {
                    if (match != null && match.gameObject != null)
                    {
                        if (LevelManager.Instance != null && match.pieceType >= 0)
                        {
                            LevelManager.Instance.ReportPieceDestroyed(match.pieceType);
                        }

                        grid[match.x, match.y] = null;
                        Destroy(match.gameObject);
                        destroyCount++;
                    }
                }

                // Actually spawn the bombs now that slots are clear
                foreach (var spawnActionKV in spawnBombActions)
                {
                    spawnActionKV.Value.Invoke();
                }

                if (ScoreManager.Instance != null && destroyCount > 0)
                {
                    ScoreManager.Instance.AddScore(destroyCount * 10);
                }

                if (AudioManager.Instance != null && destroyCount > 0)
                {
                    AudioManager.Instance.PlayMatchAudio();
                }

                yield return new WaitForSeconds(0.1f);
            }

            firstRun = false;
            lastSwapPos1 = -Vector2Int.one;
            lastSwapPos2 = -Vector2Int.one;

            // Apply gravity and refill empty slots
            List<GridPiece> movedPieces = ApplyGravityAndRefill();

            // Wait for all newly spawned and falling pieces to settle
            bool isStillMoving = true;
            while (isStillMoving)
            {
                isStillMoving = false;
                foreach (GridPiece p in movedPieces)
                {
                    if (p != null && p.isMoving)
                    {
                        isStillMoving = true;
                        break;
                    }
                }
                yield return null;
            }

            // Re-evaluate board for further matches resulting from cascading falls
            currentMatches = GetMatches();
        }

        isProcessing = false;
    }

    private List<GridPiece> ApplyGravityAndRefill()
    {
        List<GridPiece> movedPieces = new List<GridPiece>();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (!playableCells[x, y]) continue; // Skip unplayable areas

                if (grid[x, y] == null)
                {
                    // Find nearest piece above
                    GridPiece pieceAbove = null;
                    for (int nextY = y + 1; nextY < rows; nextY++)
                    {
                        if (grid[x, nextY] != null)
                        {
                            pieceAbove = grid[x, nextY];
                            grid[x, nextY] = null;
                            break;
                        }
                    }

                    if (pieceAbove != null)
                    {
                        // Pull piece down
                        grid[x, y] = pieceAbove;
                        pieceAbove.SetCoordinates(x, y);
                        pieceAbove.MoveToPosition(GetWorldPosition(x, y));
                        movedPieces.Add(pieceAbove);
                    }
                    else
                    {
                        // Spawn new from above
                        Vector3 dropPos = GetWorldPosition(x, y);
                        Vector3 offscreenPos = dropPos + new Vector3(0, rows * spacingY, 0); // Spawn above board bounds
                        GridPiece newPiece = SpawnRandomPrefab(x, y, offscreenPos);
                        
                        if (newPiece != null)
                        {
                            newPiece.MoveToPosition(dropPos);
                            movedPieces.Add(newPiece);
                        }
                    }
                }
            }
        }
        
        return movedPieces;
    }

    private GridPiece GetTargetSlotBest(MatchResult match, Vector2Int swap1, Vector2Int swap2)
    {
        if (match.pieces == null || match.pieces.Count == 0) return null;
        foreach (var p in match.pieces)
        {
            if (p != null && p.x == swap1.x && p.y == swap1.y) return p;
            if (p != null && p.x == swap2.x && p.y == swap2.y) return p;
        }
        return match.pieces[0]; // fallback
    }

    private void SwapInternal(GridPiece p1, GridPiece p2)
    {
        int tempX = p1.x;
        int tempY = p1.y;

        p1.SetCoordinates(p2.x, p2.y);
        p2.SetCoordinates(tempX, tempY);

        grid[p1.x, p1.y] = p1;
        grid[p2.x, p2.y] = p2;
    }

    private List<MatchResult> GetMatches()
    {
        List<MatchResult> allMatches = new List<MatchResult>();
        List<GridPiece> matchedPieces = new List<GridPiece>();

        // Check Squares (2x2)
        for (int x = 0; x < columns - 1; x++)
        {
            for (int y = 0; y < rows - 1; y++)
            {
                GridPiece p1 = grid[x, y];
                GridPiece p2 = grid[x + 1, y];
                GridPiece p3 = grid[x, y + 1];
                GridPiece p4 = grid[x + 1, y + 1];

                if (p1 != null && p2 != null && p3 != null && p4 != null)
                {
                    // Ignore negative piece types (like bomb powerups actively on board)
                    if (p1.pieceType != -1 && p1.pieceType == p2.pieceType && p2.pieceType == p3.pieceType && p3.pieceType == p4.pieceType)
                    {
                        if (!matchedPieces.Contains(p1)) // Only register square once so it drops ONE bomb
                        {
                            var match = new MatchResult { isSquare = true };
                            match.pieces.Add(p1); match.pieces.Add(p2); match.pieces.Add(p3); match.pieces.Add(p4);
                            allMatches.Add(match);
                            matchedPieces.AddRange(match.pieces);
                        }
                    }
                }
            }
        }

        // Horizontal matches
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns - 2; x++)
            {
                GridPiece p1 = grid[x, y];
                if (p1 == null || p1.pieceType == -1) continue;

                int matchLength = 1;
                while (x + matchLength < columns)
                {
                    GridPiece nextPiece = grid[x + matchLength, y];
                    if (nextPiece != null && nextPiece.pieceType == p1.pieceType) matchLength++;
                    else break;
                }

                if (matchLength >= 3)
                {
                    MatchResult rowMatch = new MatchResult { isHorizontal = true };
                    for (int i = 0; i < matchLength; i++)
                    {
                        rowMatch.pieces.Add(grid[x + i, y]);
                    }
                    allMatches.Add(rowMatch);
                    x += matchLength - 1; // skip ahead to avoid overlapping internal horizontal counts
                }
            }
        }

        // Vertical matches
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

        return allMatches;
    }

    [ContextMenu("Clear Grid")]
    public void ClearGrid()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            #if UNITY_EDITOR
            DestroyImmediate(transform.GetChild(i).gameObject);
            #else
            Destroy(transform.GetChild(i).gameObject);
            #endif
        }
        grid = null;
    }
}
