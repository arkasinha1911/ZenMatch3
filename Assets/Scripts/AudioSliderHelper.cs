using UnityEngine;
using UnityEngine.UIElements; // UI Toolkit Support

/// <summary>
/// This tiny helper script is designed to sit alongside a UIDocument.
/// It queries the actual Volume Slider out of the layout and binds it to PlayerPrefs!
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class AudioSliderHelper : MonoBehaviour
{
    private Slider volumeSlider;

    private void Start()
    {
        var doc = GetComponent<UIDocument>();
        if (doc == null || doc.rootVisualElement == null) return;
        
        volumeSlider = doc.rootVisualElement.Q<Slider>("audioSlider");

        if (volumeSlider != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
            volumeSlider.value = savedVolume;

            // Bind the listener dynamically
            volumeSlider.RegisterValueChangedCallback(evt =>
            {
                PlayerPrefs.SetFloat("MasterVolume", evt.newValue);
                PlayerPrefs.Save();
                
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetVolume(evt.newValue);
                }
            });
        }
    }
}
