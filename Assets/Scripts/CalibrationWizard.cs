using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CalibrationWizard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image metronomeVisual;
    [SerializeField] private Text offsetText;
    [SerializeField] private Text instructionText;
    [SerializeField] private Button backButton;

    [Header("Calibration Settings")]
    [SerializeField] private float bpm = 120f;
    [SerializeField] private int requiredTaps = 10;

    private float beatInterval;
    private float beatTimer;
    private float lastBeatTime;
    
    private List<float> recordedOffsets = new List<float>();
    private bool isVisualOn = false;

    private void Start()
    {
        beatInterval = 60f / bpm;
        beatTimer = 0f;
        lastBeatTime = Time.time;

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
        }

        UpdateUI();
    }

    private void Update()
    {
        // 1. Metronome pulsing logic
        beatTimer += Time.deltaTime;
        if (beatTimer >= beatInterval)
        {
            beatTimer -= beatInterval;
            lastBeatTime = Time.time - beatTimer;
            TriggerVisualPulse();
        }

        // Fade visual metronome color back to normal
        if (metronomeVisual != null)
        {
            metronomeVisual.color = Color.Lerp(metronomeVisual.color, isVisualOn ? Color.white : new Color(0.2f, 0.2f, 0.22f, 1f), Time.deltaTime * 15f);
            if (metronomeVisual.color.a < 0.1f) isVisualOn = false; // reset flag
        }

        // 2. Spacebar tap tracking
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RecordTap();
        }
    }

    private void TriggerVisualPulse()
    {
        isVisualOn = true;
        if (metronomeVisual != null)
        {
            metronomeVisual.color = Color.green; // Flash Green on visual beat
        }
    }

    private void RecordTap()
    {
        float tapTime = Time.time;
        
        // Find nearest visual beat time (could be the previous one or the next one)
        float prevBeat = lastBeatTime;
        float nextBeat = lastBeatTime + beatInterval;
        
        float diffToPrev = Mathf.Abs(tapTime - prevBeat);
        float diffToNext = Mathf.Abs(tapTime - nextBeat);
        
        float nearestBeatTime = (diffToPrev < diffToNext) ? prevBeat : nextBeat;
        
        // Signed offset: positive means player tapped late, negative means early
        float offset = tapTime - nearestBeatTime;
        
        recordedOffsets.Add(offset);
        
        // Limit list size to requiredTaps
        if (recordedOffsets.Count > requiredTaps)
        {
            recordedOffsets.RemoveAt(0);
        }

        UpdateCalibrationResult();
    }

    private void UpdateCalibrationResult()
    {
        if (recordedOffsets.Count == 0) return;

        // Calculate average offset
        float sum = 0f;
        foreach (float val in recordedOffsets)
        {
            sum += val;
        }
        float averageOffset = sum / recordedOffsets.Count;

        // Save latency offset globally to Persistent Data Manager
        if (PersistentDataManager.Instance != null)
        {
            PersistentDataManager.Instance.LatencyOffset = averageOffset;
        }

        UpdateUI(averageOffset);
    }

    private void UpdateUI(float averageOffset = 999f)
    {
        if (offsetText != null)
        {
            if (averageOffset == 999f)
            {
                offsetText.text = "Tap SPACEBAR on the visual pulse...";
            }
            else
            {
                // Convert from seconds to milliseconds for display
                int msOffset = Mathf.RoundToInt(averageOffset * 1000f);
                offsetText.text = $"Current Latency Offset:\n{msOffset} ms\n({(msOffset >= 0 ? "+" : "")}{msOffset} ms)";
            }
        }

        if (instructionText != null)
        {
            instructionText.text = $"Taps recorded: {recordedOffsets.Count} / {requiredTaps}\n(Resets automatically with subsequent taps)";
        }
    }

    private void GoBack()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
    }
}
