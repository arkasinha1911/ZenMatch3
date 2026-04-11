using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    private GridSpawner gridSpawner;

    private Vector2 touchStartPos;
    private GridPiece touchedPiece;
    private bool isSwiping = false;

    public float minimumSwipeDistance = 10f; // Minimal screen pixels to be considered a swipe

    void Start()
    {
        gridSpawner = GetComponent<GridSpawner>();
        if (gridSpawner == null)
            Debug.LogError("InputController requires GridSpawner on the same GameObject!");
    }

    void Update()
    {
        if (gridSpawner == null) return;

        if (LevelManager.Instance != null && !LevelManager.Instance.IsGameActive) return;

        // Ensure we aren't already swapping/matching
        if (gridSpawner.isProcessing) return;

        var pointer = Pointer.current;
        if (pointer == null) return;

        // Handle Touch or Mouse input using the new Input System
        if (pointer.press.wasPressedThisFrame)
        {
            touchStartPos = pointer.position.ReadValue();

            // Screen to world point for 2D raycast
            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(touchStartPos);
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

            if (hit.collider != null)
            {
                touchedPiece = hit.collider.GetComponent<GridPiece>();
                if (touchedPiece != null)
                {
                    isSwiping = true;
                }
            }
        }
        else if (pointer.press.isPressed && isSwiping)
        {
            Vector2 currentPos = pointer.position.ReadValue();
            Vector2 swipeDir = currentPos - touchStartPos;

            if (swipeDir.magnitude > minimumSwipeDistance)
            {
                ProcessSwipe(swipeDir);
            }
        }
        else if (pointer.press.wasReleasedThisFrame)
        {
            ResetSwipe();
        }
    }

    private void ProcessSwipe(Vector2 swipeDir)
    {
        // Cancel swiping so we process it only once
        isSwiping = false;

        swipeDir.Normalize();

        // Calculate direction
        int dirX = 0;
        int dirY = 0;

        if (Mathf.Abs(swipeDir.x) > Mathf.Abs(swipeDir.y))
        {
            // Horizontal swipe
            dirX = (swipeDir.x > 0) ? 1 : -1;
        }
        else
        {
            // Vertical swipe
            dirY = (swipeDir.y > 0) ? 1 : -1;
        }

        int targetX = touchedPiece.x + dirX;
        int targetY = touchedPiece.y + dirY;

        GridPiece targetPiece = gridSpawner.GetPieceAt(targetX, targetY);
        
        if (targetPiece != null)
        {
            // Attempt to swap
            gridSpawner.AttemptSwap(touchedPiece, targetPiece);
        }

        ResetSwipe();
    }

    private void ResetSwipe()
    {
        isSwiping = false;
        touchedPiece = null;
    }
}
