using UnityEngine;

[RequireComponent(typeof(Camera))]
public class BackgroundColorCycler : MonoBehaviour
{
    [Tooltip("How fast the background transitions to the next color.")]
    public float transitionSpeed = 0.5f;

    private Camera cam;
    private Color startColor;
    private Color endColor;
    private float t = 0f;

    void Start()
    {
        cam = GetComponent<Camera>();
        
        // Ensure the camera is set to render a solid color background
        cam.clearFlags = CameraClearFlags.SolidColor;
        
        startColor = cam.backgroundColor;
        endColor = GetRandomPastelColor();
    }

    void Update()
    {
        t += Time.deltaTime * transitionSpeed;

        cam.backgroundColor = Color.Lerp(startColor, endColor, t);

        // When the transition completes, pick a new random color
        if (t >= 1f)
        {
            t = 0f;
            startColor = endColor;
            endColor = GetRandomPastelColor();
        }
    }

    private Color GetRandomPastelColor()
    {
        // High Value (brightness) and Low-to-Medium Saturation creates typical pastel colors
        return Random.ColorHSV(0f, 1f, 0.2f, 0.4f, 0.8f, 1f);
    }
}
