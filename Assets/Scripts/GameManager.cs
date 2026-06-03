using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject resultsCanvas;
    [SerializeField] private UnityEngine.UI.Text finalScoreText;
    [SerializeField] private UnityEngine.UI.Text maxComboText;
    [SerializeField] private UnityEngine.UI.Text gradeText;

    private bool isGameOver = false;

    // Public getter for game over status
    public bool IsGameOver => isGameOver;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (resultsCanvas != null)
        {
            resultsCanvas.SetActive(false);
        }
    }

    public void EndGame()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Pause/stop song and active notes
        var rhythmManager = FindFirstObjectByType<RhythmManager>();
        if (rhythmManager != null)
        {
            rhythmManager.StopSongGameplay();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopMusic();
        }

        // Show Results Panel
        if (resultsCanvas != null)
        {
            resultsCanvas.SetActive(true);
            PopulateResults(rhythmManager);
        }
    }

    private void PopulateResults(RhythmManager rhythmManager)
    {
        if (rhythmManager == null) return;

        if (finalScoreText != null)
        {
            finalScoreText.text = $"Final Score: {rhythmManager.Score}";
        }

        if (maxComboText != null)
        {
            maxComboText.text = $"Max Combo: {rhythmManager.MaxCombo}";
        }

        if (gradeText != null)
        {
            string grade = rhythmManager.GetGrade();
            string description = GetGradeDescription(grade);
            gradeText.text = $"Grade: {grade}\n({description})";
        }
    }

    private string GetGradeDescription(string grade)
    {
        switch (grade)
        {
            case "SSS": return "Sucking & Blowing Superstar";
            case "SS": return "Breath Control Master";
            case "S": return "Mouth Organ Virtuoso";
            case "A": return "Talented Tooter";
            case "B": return "Decent Wind Capacity";
            case "C": return "Slightly Wheezy";
            case "D": return "Out of Breath";
            default: return "Fainting";
        }
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
