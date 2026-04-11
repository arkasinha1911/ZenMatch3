using UnityEngine;
using UnityEngine.UI; // Required for the Button class
using TMPro; // Required for TextMeshPro text elements

/// <summary>
/// This is a "Data Container script". It doesn't actually DO anything on its own.
/// Instead, we attach it to the LevelButton Prefab UI so that the LevelSelectUI script 
/// has an easy way to pinpoint exactly where the text boxes and button are!
/// </summary>
public class LevelButton : MonoBehaviour
{
    [Tooltip("The main Button component that detects clicks.")]
    public Button button;

    [Tooltip("The text field showing the level number (e.g., 'Level 1').")]
    public TextMeshProUGUI levelText;

    [Tooltip("The text field showing the player's best record (e.g., 'High Score: 500').")]
    public TextMeshProUGUI scoreText;
}
