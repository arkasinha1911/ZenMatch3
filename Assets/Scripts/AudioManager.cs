using UnityEngine;

/// <summary>
/// The AudioManager controls all the sound in the game, including looping background music and one-off sound effects (SFX).
/// Like the ScoreManager, this uses a "Singleton" pattern so ANY script can play a sound without needing a direct reference.
/// For example, GridSpawner can just say "AudioManager.Instance.PlaySwapAudio();".
/// </summary>
public class AudioManager : MonoBehaviour
{
    // The global access point for the AudioManager.
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("The AudioSource component used for looping background music. Think of it as a speaker playing a CD.")]
    public AudioSource backgroundMusicSource;
    
    [Tooltip("The AudioSource component used for playing short sound effects, like pops and clicks.")]
    public AudioSource sfxSource;

    [Header("Audio Clips")]
    // AudioClips are the actual sound files (like .mp3 or .wav) we drag and drop into the Unity Inspector.
    public AudioClip themeMusic;
    public AudioClip swapClip;
    public AudioClip matchClip;

    /// <summary>
    /// Awake is called instantly when the script is loaded. We use it to set up our Singleton and start the music.
    /// </summary>
    private void Awake()
    {
        // 1. Singleton Setup
        // If there is no existing Instance, make this the official one.
        if (Instance == null)
        {
            Instance = this;
            // NOTE: If we wanted the music to keep playing seamlessly when changing scenes (like going from Menu to Level),
            // we would uncomment the line below:
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an AudioManager already exists, destroy this duplicate to prevent overlapping music tracks.
            Destroy(gameObject);
            return;
        }

        // 2. Music Initialization
        // Check if we connected the speaker (backgroundMusicSource) AND the audio file (themeMusic) in the Inspector.
        if (backgroundMusicSource != null && themeMusic != null)
        {
            // Load the song into the speaker.
            backgroundMusicSource.clip = themeMusic;
            // Tell the speaker to loop the song forever.
            backgroundMusicSource.loop = true;
            // Start playing the song right away!
            backgroundMusicSource.Play();
        }
        
        // 3. Volume Check
        // As soon as the game starts, look up the player's saved volume settings and apply them.
        ApplySavedVolume();
    }

    /// <summary>
    /// Public method called by the InputController or GridSpawner when the player interacts with pieces.
    /// </summary>
    public void PlaySwapAudio()
    {
        // Check if we have a speaker (sfxSource) and a sound to play (swapClip).
        if (sfxSource != null && swapClip != null)
        {
            // PlayOneShot is special: it plays the sound once, but allows multiple sounds to overlap without cutting each other off!
            sfxSource.PlayOneShot(swapClip);
        }
        else if (sfxSource == null)
        {
            // If the speaker is missing, log a warning in the Console so the developer knows to fix it.
            Debug.LogWarning("AudioManager: Cannot play SwapAudio because SFX Source is not assigned!");
        }
    }

    /// <summary>
    /// Public method called by the GridSpawner when blocks explode and matches occur.
    /// </summary>
    public void PlayMatchAudio()
    {
        // Again, verify the speaker and the sound file are properly linked in the Unity Inspector.
        if (sfxSource != null && matchClip != null)
        {
            // Play the exciting match explosion sound! Allows overlapping if huge combos happen.
            sfxSource.PlayOneShot(matchClip);
        }
        else if (sfxSource == null)
        {
            // Helpful developer warning for debugging empty connections.
            Debug.LogWarning("AudioManager: Cannot play MatchAudio because SFX Source is not assigned!");
        }
    }

    /// <summary>
    /// This method is usually connected to a UI Slider in the Settings menu.
    /// </summary>
    /// <param name="volume">A decimal value between 0.0 (silent) and 1.0 (full volume) sent by the Slider.</param>
    public void SetVolume(float volume)
    {
        // AudioListener is the "virtual ears" in the scene (usually on the Main Camera).
        // Changing its volume scales ALL sound up or down instantly.
        AudioListener.volume = volume;

        // PlayerPrefs is Unity's way of saving simple data (like settings) to the player's hard drive automatically.
        // We save their volume preference so they don't have to change it every time they open the game.
        PlayerPrefs.SetFloat("MasterVolume", volume);
        
        // Forces the save to write to the hard drive immediately.
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Reads the saved volume from the player's hard drive and applies it to the game.
    /// This is called in Awake() right when the game starts.
    /// </summary>
    public void ApplySavedVolume()
    {
        // Retrieve the saved volume. The "1f" at the end is the DEFAULT value! 
        // If the player has never played before (no saved data), it defaults to 1.0 (100% volume).
        float savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        
        // Apply the loaded volume to the game's master "virtual ears".
        AudioListener.volume = savedVolume;
    }
}
