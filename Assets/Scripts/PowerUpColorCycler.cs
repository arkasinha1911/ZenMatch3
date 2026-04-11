using UnityEngine;

/// <summary>
/// This script makes the special "Bomb" powerup pieces smoothly cycle through the rainbow!
/// By attaching this to the "ColorBombPrefab", the bomb will shift colors constantly so it stands out to the player.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PowerUpColorCycler : MonoBehaviour
{
    [Header("Color Cycling Settings")]
    [Tooltip("How fast the color cycles through the rainbow spectrum.")]
    public float cycleSpeed = 1f;
    
    [Tooltip("Saturation of the color (0-1). Keep this around 0.4 - 0.6 for Pastel colors!")]
    [Range(0f, 1f)]
    public float saturation = 0.5f;

    [Tooltip("Brightness of the color (0-1). Keep at 1 for bright pastels.")]
    [Range(0f, 1f)]
    public float brightness = 1f;

    // Connection to the actual 2D image component that draws the bomb.
    private SpriteRenderer spriteRenderer;
    
    // The current color position on the rainbow wheel (0.0 to 1.0).
    private float currentHue = 0f;

    void Awake()
    {
        // Automatically grab the SpriteRenderer picture connected to the bomb GameObject.
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Pick a totally random starting point on the color wheel!
        // Why? If there are 3 color bombs on the board, we want them all to be different colors at different times,
        // rather than blinking in perfect sync which looks unnatural.
        currentHue = Random.Range(0f, 1f);
    }

    void Update()
    {
        // Failsafe: If there is no picture, don't crash. Just stop doing anything.
        if (spriteRenderer == null) return;

        // Shift our position along the color wheel according to true time! 
        currentHue += Time.deltaTime * cycleSpeed;
        
        // The color wheel wraps from 0.0 to 1.0. 
        // If we hit 1.01, we subtract 1 so we cleanly wrap back to 0.01 and keep running endlessly.
        if (currentHue > 1f)
        {
            currentHue -= 1f;
        }

        // Color.HSVToRGB calculates the complex math required to turn a "Hue/Saturation/Brightness" wheel
        // into a flat "Red/Green/Blue" color code that the SpriteRenderer needs.
        spriteRenderer.color = Color.HSVToRGB(currentHue, saturation, brightness);
    }
}
