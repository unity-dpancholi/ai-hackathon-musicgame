using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject songSelectPanel;
    [SerializeField] private GameObject optionsPanel;

    [Header("Song Select Assets")]
    [SerializeField] private SongChart[] availableCharts;
    [SerializeField] private Button[] songButtons; // Bind each button to a song index

    [Header("Latencies & Info")]
    [SerializeField] private Text latencyValueText;

    private void Start()
    {
        ShowMainMenu();
        UpdateOptionsUI();
        ConfigureSongSelectButtons();
    }

    // --- NAVIGATION FLOWS ---
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (songSelectPanel != null) songSelectPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void ShowSongSelect()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (songSelectPanel != null) songSelectPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void ShowOptions()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (songSelectPanel != null) songSelectPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(true);
        UpdateOptionsUI();
    }

    public void LoadCalibrationScene()
    {
        SceneManager.LoadScene("CalibrationScene");
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    // --- OPTIONS & CALIBRATION INTERFACE ---
    private void UpdateOptionsUI()
    {
        if (latencyValueText != null && PersistentDataManager.Instance != null)
        {
            int msOffset = Mathf.RoundToInt(PersistentDataManager.Instance.LatencyOffset * 1000f);
            latencyValueText.text = $"{msOffset} ms";
        }
    }

    public void ResetLatencyOffset()
    {
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.LatencyOffset = 0.0f;
            UpdateOptionsUI();
        }
    }

    // --- SONG SELECT LOADING PATTERN ---
    private void ConfigureSongSelectButtons()
    {
        if (songButtons == null || availableCharts == null) return;

        // Automatically assign button click handlers based on chart indexes
        for (int i = 0; i < songButtons.Length; i++)
        {
            if (i >= availableCharts.Length || songButtons[i] == null) continue;

            int index = i; // Local copy to prevent closure capture issues
            songButtons[i].onClick.RemoveAllListeners();
            songButtons[i].onClick.AddListener(() => SelectAndPlaySong(availableCharts[index]));
        }
    }

    public void SelectAndPlaySong(SongChart chart)
    {
        if (chart == null)
        {
            Debug.LogError("Selected chart is null!");
            return;
        }

        // Pass selection dynamically to the persistent data loader instance
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.SelectedChart = chart;
        }

        // Transition seamlessly to the main active gameplay scene
        SceneManager.LoadScene("MainScene");
    }
}
