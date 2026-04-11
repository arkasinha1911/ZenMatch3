using System.Collections;
using UnityEngine;

/// <summary>
/// This script handles the screen shake effect! It makes the camera vibrate randomly for a short period of time.
/// It uses a Singleton pattern so that ANY script (like GridSpawner when bombs blow up) can trigger a shake easily
/// by calling CameraShake.Instance.Shake() without needing a direct link!
/// </summary>
public class CameraShake : MonoBehaviour
{
    // The globally accessible instance of the CameraShake.
    public static CameraShake Instance { get; private set; }

    // Memory variable to remember exactly where the camera normally sits. 
    // If we don't remember this, the camera might slowly drift away permanently after many shakes!
    private Vector3 originalPos;

    /// <summary>
    /// Set up the Singleton.
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent multiple shakers from existing
        }
    }

    /// <summary>
    /// OnEnable runs right after Awake, or whenever the Camera is turned on.
    /// This is the best place to save its default resting location.
    /// </summary>
    void OnEnable()
    {
        // LocalPosition is used instead of Position in case the Camera is a child of a moving player or rig!
        originalPos = transform.localPosition;
    }

    [Header("Shake Settings")]
    [Tooltip("How long the shake lasts in seconds if you don't provide custom numbers.")]
    public float defaultDuration = 0.2f; // 1/5th of a second
    
    [Tooltip("How violent the shake is. A higher number means the camera jumps further off-center.")]
    public float defaultMagnitude = 0.1f; 

    /// <summary>
    /// The easiest way to trigger a shake. Uses the default settings from the Inspector.
    /// </summary>
    public void Shake()
    {
        // StartCoroutine runs the ShakeCoroutine function "in the background" over multiple frames.
        StartCoroutine(ShakeCoroutine(defaultDuration, defaultMagnitude));
    }

    /// <summary>
    /// An advanced way to trigger a shake, where you can specify giant explosions vs tiny clicks!
    /// </summary>
    public void Shake(float duration, float magnitude)
    {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    /// <summary>
    /// An IEnumerator is a function that can pause itself (using yield return null) and resume on the next frame.
    /// This makes it perfect for animations over time, like shaking the screen for a specific duration!
    /// </summary>
    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0.0f; // Stopwatch starting at 0

        // As long as the stopwatch is less than the requested duration, keep shaking!
        while (elapsed < duration)
        {
            // Pick a random X and Y distance between -1 and 1, then scale it by our chosen intensity (magnitude).
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Apply the random offset to the camera's original saved position.
            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            // Add the time it took to draw this frame to our stopwatch.
            elapsed += Time.deltaTime;

            // 'yield return null' tells Unity: "Pause this function here, draw the screen, and come back next frame!"
            yield return null;
        }

        // Once the stopwatch runs out, firmly snap the camera back to its exact original position to prevent drift.
        transform.localPosition = originalPos;
    }
}
