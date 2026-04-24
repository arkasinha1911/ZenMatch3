using UnityEngine;
#if UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class PlayGamesManager : MonoBehaviour
{
    public static PlayGamesManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializePlayGamesLogin();
    }

    public void InitializePlayGamesLogin()
    {
#if UNITY_ANDROID
        PlayGamesPlatform.DebugLogEnabled = true;
        
        // Activate the Google Play Games platform
        PlayGamesPlatform.Activate();

        SignInToGooglePlay();
#else
        Debug.LogWarning("Google Play Games Services is only available on Android.");
#endif
    }

    public void SignInToGooglePlay()
    {
#if UNITY_ANDROID
        PlayGamesPlatform.Instance.Authenticate(ProcessAuthentication);
#endif
    }

#if UNITY_ANDROID
    private void ProcessAuthentication(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Debug.Log("Successfully Logged in to Google Play Games.");
            string displayName = PlayGamesPlatform.Instance.GetUserDisplayName();
            string userId = PlayGamesPlatform.Instance.GetUserId();
            Debug.Log($"User Name: {displayName}, User ID: {userId}");
        }
        else
        {
            Debug.LogWarning("Failed to login to Google Play Games. Status: " + status);
            // Optionally, handle specific status codes like Canceled, InternalError, etc.
        }
    }
#endif
}
