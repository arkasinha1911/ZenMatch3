using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LevelButton : MonoBehaviour
{
    [Tooltip("The main Button component that gets clicked.")]
    public Button button;

    [Tooltip("Text field to display 'Level 1', 'Level 2', etc.")]
    public TextMeshProUGUI levelText;

    [Tooltip("Text field to display 'High Score: 500'")]
    public TextMeshProUGUI scoreText;
}
