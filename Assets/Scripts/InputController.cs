using UnityEngine;
using UnityEngine.InputSystem; // We use Unity's modern New Input System!

/// <summary>
/// This script acts as the "Eyes and Hands" of the player. 
/// It detects mouse clicks or mobile screen touches, figures out what grid piece you touched,
/// measures which direction you swiped your finger, and tells the GridSpawner to attempt a swap!
/// </summary>
public class InputController : MonoBehaviour
{
    // A direct link to the brains of the board. We need to tell it when we swipe!
    private GridSpawner gridSpawner;

    // Memory variable: Where did the player first touch their finger on the screen?
    private Vector2 touchStartPos;
    
    // Memory variable: Which specific grid block did they physically touch?
    private GridPiece touchedPiece;
    
    // Are they currently dragging their finger across the screen?
    private bool isSwiping = false;

    [Tooltip("How far their finger must move before the game registers it as a deliberate 'swipe'. Prevents accidental jitters!")]
    public float minimumSwipeDistance = 10f; 

    void Start()
    {
        // Automatically find the GridSpawner attached to this object!
        gridSpawner = GetComponent<GridSpawner>();
        
        // If the developer forgot to add a GridSpawner, throw a big red error in the console.
        if (gridSpawner == null)
            Debug.LogError("InputController requires GridSpawner on the same GameObject!");
    }

    /// <summary>
    /// Update runs 60 times a second, constantly checking if the player is touching the screen.
    /// </summary>
    void Update()
    {
        // 1. FAILSAFES
        // If there's no grid, stop.
        if (gridSpawner == null) return;

        // If the game is paused or over, DO NOT let them click anything!
        if (LevelManager.Instance != null && !LevelManager.Instance.IsGameActive) return;

        // If the board is currently exploding or dropping pieces, lock the controls so they don't break the game!
        if (gridSpawner.isProcessing) return;

        // Obtain the "Pointer" (which represents either a Mouse Cursor or a Mobile Touch).
        var pointer = Pointer.current;
        if (pointer == null) return;

        // 2. THE TOUCH DOWN EVENT
        // "wasPressedThisFrame" is exactly when they first tap the screen or click the mouse.
        if (pointer.press.wasPressedThisFrame)
        {
            // Record exactly where on the computer screen they clicked (in raw pixels).
            touchStartPos = pointer.position.ReadValue();

            // Convert the flat 2D screen pixels into a 3D coordinate inside the game world
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(touchStartPos);
            
            // Shoot a tiny laser (Raycast) straight down into the screen where they tapped
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

            // If the laser hit something physical (a Collider)...
            if (hit.collider != null)
            {
                // Check if the thing they hit was actually a GridPiece!
                touchedPiece = hit.collider.GetComponent<GridPiece>();
                
                // If it was a valid piece, officially unlock our "Waiting for Swipe" mode!
                if (touchedPiece != null)
                {
                    isSwiping = true;
                }
            }
        }
        // 3. THE DRAGGING EVENT
        // "isPressed" means their finger is currently held down on the screen.
        else if (pointer.press.isPressed && isSwiping)
        {
            // Where is their finger right now?
            Vector2 currentPos = pointer.position.ReadValue();
            
            // Subtract the start point from the exact point to get an arrow (Vector) pointing in the direction they dragged!
            Vector2 swipeDir = currentPos - touchStartPos;

            // If the length (magnitude) of that arrow is longer than our minimum threshold...
            if (swipeDir.magnitude > minimumSwipeDistance)
            {
                // Trigger the swap!
                ProcessSwipe(swipeDir);
            }
        }
        // 4. THE RELEASE EVENT
        // They lifted their finger off the screen.
        else if (pointer.press.wasReleasedThisFrame)
        {
            // Cancel everything.
            ResetSwipe();
        }
    }

    /// <summary>
    /// Takes the raw pixel direction the player moved their finger and translates it into Grid coordinates.
    /// </summary>
    /// <param name="swipeDir">The directional arrow representing the swipe.</param>
    private void ProcessSwipe(Vector2 swipeDir)
    {
        // Immediately turn off swipe mode so they don't accidentally trigger 50 swipes in one fast drag.
        isSwiping = false;

        // Standardize the length of the arrow to exactly 1. We only care about the DIRECTION, not the speed.
        swipeDir.Normalize();

        int dirX = 0;
        int dirY = 0;

        // Which was bigger: the horizontal movement (X) or the vertical movement (Y)?
        // Mathf.Abs turns negative numbers positive so we can cleanly compare lengths!
        if (Mathf.Abs(swipeDir.x) > Mathf.Abs(swipeDir.y))
        {
            // Horizontal swipe! Was it right (+) or left (-)?
            dirX = (swipeDir.x > 0) ? 1 : -1;
        }
        else
        {
            // Vertical swipe! Was it up (+) or down (-)?
            dirY = (swipeDir.y > 0) ? 1 : -1;
        }

        // Add the swipe direction onto the Grid position of the piece they touched.
        // If they touched piece (2,2) and swiped Right (+1 X), the target is (3,2)!
        int targetX = touchedPiece.x + dirX;
        int targetY = touchedPiece.y + dirY;

        // Ask the GridSpawner if there is actually a physical piece sitting at that location.
        // (Prevents crashing if they swipe off the edge of the board into thin air!)
        GridPiece targetPiece = gridSpawner.GetPieceAt(targetX, targetY);
        
        if (targetPiece != null)
        {
            // Both pieces exist! Command the GridSpawner to attempt to swap them!
            gridSpawner.AttemptSwap(touchedPiece, targetPiece);
        }

        // Clean up our memory for the next click
        ResetSwipe();
    }

    /// <summary>
    /// Clears any cached memory about what the player was touching.
    /// </summary>
    private void ResetSwipe()
    {
        isSwiping = false;
        touchedPiece = null;
    }
}
