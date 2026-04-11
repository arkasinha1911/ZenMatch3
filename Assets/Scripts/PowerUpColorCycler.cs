using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PowerUpColorCycler : MonoBehaviour
{
    [Header("Color Cycling Settings")]
    [Tooltip("How fast the color cycles through the spectrum.")]
    public float cycleSpeed = 1f;
    
    [Tooltip("Saturation of the color (0-1). Keep this around 0.4 - 0.6 for Pastel colors!")]
    [Range(0f, 1f)]
    public float saturation = 0.5f;

    [Tooltip("Brightness of the color (0-1). Keep at 1 for bright pastels.")]
    [Range(0f, 1f)]
    public float brightness = 1f;

    private SpriteRenderer spriteRenderer;
    private float currentHue = 0f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        // Randomize the starting hue so multiple bombs don't blink in perfect sync
        currentHue = Random.Range(0f, 1f);
    }

    void Update()
    {
        if (spriteRenderer == null) return;

        // Shift the hue through the color wheel
        currentHue += Time.deltaTime * cycleSpeed;
        
        // Loop back to 0 when it reaches 1
        if (currentHue > 1f)
        {
            currentHue -= 1f;
        }

        // Apply exactly as smooth HSV to RGB format
        spriteRenderer.color = Color.HSVToRGB(currentHue, saturation, brightness);
    }
}
