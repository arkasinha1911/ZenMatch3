using System.Collections;
using UnityEngine;

/// <summary>
/// This Enum defines the special "Super Powers" a piece can have!
/// An Enum is basically a fixed menu of options. A piece can be precisely ONE of these things.
/// </summary>
public enum PowerUpType
{
    None,            // A normal, boring square or circle.
    HorizontalBomb,  // A bomb that blows up a whole row left and right.
    VerticalBomb,    // A bomb that blows up a whole column up and down.
    ColorBomb        // A bomb that deletes all pieces of a specific color!
}

/// <summary>
/// This script sits on every single physical block/piece on the grid!
/// It is a simple "Data Container" that remembers its own grid position and manages its own sliding animation.
/// Because it's a MonoBehaviour, we can attach it directly to a 3D or 2D object in Unity.
/// </summary>
public class GridPiece : MonoBehaviour
{
    [Tooltip("The X coordinate (column) where this piece currently lives in the grid memory.")]
    public int x;
    
    [Tooltip("The Y coordinate (row) where this piece currently lives in the grid memory.")]
    public int y;
    
    [Tooltip("The ID shape of the piece. e.g. 0 = Red Square, 1 = Blue Circle, -1 = Special Object")]
    public int pieceType;
    
    [Tooltip("If this piece is a bomb, this tells us what kind of bomb it is!")]
    public PowerUpType powerUp = PowerUpType.None;

    [Tooltip("A safety lock. If a piece is actively sliding, we use this to prevent the player from clicking it again.")]
    public bool isMoving = false;

    /// <summary>
    /// Simply updates the internal memory of where the piece is. 
    /// NOTE: This does NOT physically move the object on screen! 
    /// </summary>
    public void SetCoordinates(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    /// <summary>
    /// This tells the piece to physically slide from where it currently is, to a new target location.
    /// It uses a Coroutine to do this over a short period of time so it looks smooth, rather than teleporting instantly.
    /// </summary>
    /// <param name="targetPosition">The exact 3D world coordinate to slide towards.</param>
    /// <param name="duration">How fast to slide. Defaults to 0.2 seconds (very fast!).</param>
    public void MoveToPosition(Vector3 targetPosition, float duration = 0.2f)
    {
        // Start playing the sliding animation in the background
        StartCoroutine(MoveCoroutine(targetPosition, duration));
    }

    /// <summary>
    /// The actual engine behind the sliding animation. 
    /// An IEnumerator lets us pause our code every frame so the player can actually see the slider move over time!
    /// </summary>
    private IEnumerator MoveCoroutine(Vector3 targetPosition, float duration)
    {
        // 1. Lock the piece so the user can't click it while it's moving!
        isMoving = true;
        
        // 2. Remember exactly where we started sliding from
        Vector3 startPosition = transform.position;
        
        // 3. Our stopwatch timer
        float elapsedTime = 0f;

        // 4. As long as our stopwatch is less than 0.2 seconds...
        while (elapsedTime < duration)
        {
            // Vector3.Lerp smoothly blends the start position and the end position.
            // If we are halfway through the timer (elapsedTime / duration = 0.5), it places the piece exactly halfway between the two spots!
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / duration);
            
            // Add the time it took to draw this frame to our stopwatch
            elapsedTime += Time.deltaTime;
            
            // Pause here, wait for Unity to draw the next frame, then continue the loop!
            yield return null;
        }

        // 5. Sometimes math is slightly imperfect. We force the piece to snap EXACTLY to the target at the very end.
        transform.position = targetPosition;
        
        // 6. Unlock the piece so the player can click it again!
        isMoving = false;
    }
}
