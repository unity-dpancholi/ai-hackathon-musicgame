using UnityEngine;

public class PersistentDataManager : MonoBehaviour
{
    public static PersistentDataManager Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private float latencyOffset = 0.0f; // Latency offset in seconds (e.g. 0.05f = 50ms)
    [SerializeField] private SongChart selectedChart;   // The chart chosen in Song Selection

    // Public Accessors
    public float LatencyOffset
    {
        get => latencyOffset;
        set
        {
            latencyOffset = value;
            PlayerPrefs.SetFloat("AudioLatencyOffset", latencyOffset);
            PlayerPrefs.Save();
        }
    }

    public SongChart SelectedChart
    {
        get => selectedChart;
        set => selectedChart = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Load saved latency from PlayerPrefs
            if (PlayerPrefs.HasKey("AudioLatencyOffset"))
            {
                latencyOffset = PlayerPrefs.GetFloat("AudioLatencyOffset");
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
