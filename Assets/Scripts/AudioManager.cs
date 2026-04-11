using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("The AudioSource used for looping background music.")]
    public AudioSource backgroundMusicSource;
    
    [Tooltip("The AudioSource used for playing short sound effects.")]
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    public AudioClip themeMusic;
    public AudioClip swapClip;
    public AudioClip matchClip;

    private void Awake()
    {
        // Set up the Singleton instance
        if (Instance == null)
        {
            Instance = this;
            // Optional: Uncomment the line below if you want audio to persist across scene unloads/loads
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize Background Music
        if (backgroundMusicSource != null && themeMusic != null)
        {
            backgroundMusicSource.clip = themeMusic;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }
        
        ApplySavedVolume();
    }

    public void PlaySwapAudio()
    {
        if (sfxSource != null && swapClip != null)
        {
            sfxSource.PlayOneShot(swapClip);
        }
        else if (sfxSource == null)
        {
            Debug.LogWarning("AudioManager: Cannot play SwapAudio because SFX Source is not assigned!");
        }
    }

    public void PlayMatchAudio()
    {
        if (sfxSource != null && matchClip != null)
        {
            sfxSource.PlayOneShot(matchClip);
        }
        else if (sfxSource == null)
        {
            Debug.LogWarning("AudioManager: Cannot play MatchAudio because SFX Source is not assigned!");
        }
    }

    /// <summary>
    /// Attach this to your Slider's 'OnValueChanged (Single)' event!
    /// </summary>
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("MasterVolume", volume);
        PlayerPrefs.Save();
    }

    public void ApplySavedVolume()
    {
        // Default to full volume (1.0f) if they haven't saved a setting yet
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        AudioListener.volume = savedVolume;
    }
}
