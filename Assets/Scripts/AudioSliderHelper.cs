using UnityEngine;
using UnityEngine.UI; // Required for interacting with the Slider component

/// <summary>
/// This tiny helper script is designed to sit directly on the physical Audio "Volume Slider" UI.
/// When the menu opens it ensures the visual drag-handle actually aligns with the player's saved volume.
/// </summary>
[RequireComponent(typeof(Slider))]
public class AudioSliderHelper : MonoBehaviour
{
    // The physical slider component managing the audio.
    private Slider volumeSlider;

    /// <summary>
    /// Start runs when the slider appears on the screen.
    /// </summary>
    private void Start()
    {
        // Finds the exact slider connected to this object.
        volumeSlider = GetComponent<Slider>();
        
        // Ensure the slider visual handle instantly snaps to match the actual saved volume on launch
        if (volumeSlider != null)
        {
            // Grab the saved volume out of the computer's hard drive using PlayerPrefs.
            // If they haven't played before, default the handle to 1.0 (Full max volume!).
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            
            // Overwrite the visual slider position with the real number!
            volumeSlider.value = savedVolume;
        }
    }
}
