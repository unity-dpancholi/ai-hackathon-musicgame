using System.Collections.Generic;
using UnityEngine;

public class RhythmManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private SongChart activeChart;
    
    [Header("Note Prefabs")]
    [SerializeField] private GameObject noteBlowPrefab; // Blue (Blow) note prefab
    [SerializeField] private GameObject noteDrawPrefab; // Amber (Draw) note prefab

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] harmonicaNotes; // Index 0-9 for Holes 1-10
    [SerializeField] private AudioClip spitAirRustle;    // Mismatched error sound

    [Header("Scoring & Combo")]
    [SerializeField] private int score = 0;
    [SerializeField] private int combo = 0;
    [SerializeField] private int maxCombo = 0;
    [SerializeField] private int perfectCount = 0;
    [SerializeField] private int goodCount = 0;
    [SerializeField] private int okCount = 0;
    [SerializeField] private int missCount = 0;

    private List<NoteObject> activeNotes = new List<NoteObject>();
    private int nextNoteIndex = 0;
    private float songTimer = 0f;
    private bool isSongPlaying = false;

    // Public getters for UI
    public int Score => score;
    public int Combo => combo;
    public int MaxCombo => maxCombo;
    public int PerfectCount => perfectCount;
    public int GoodCount => goodCount;
    public int OkCount => okCount;
    public int MissCount => missCount;
    public float SongTimer => songTimer;
    public bool IsSongPlaying => isSongPlaying;

    private void OnEnable()
    {
        NoteObject.OnAnyNoteMissed += HandleNoteMissed;
    }

    private void OnDisable()
    {
        NoteObject.OnAnyNoteMissed -= HandleNoteMissed;
    }

    public void StartSong()
    {
        if (activeChart == null)
        {
            Debug.LogError("Cannot start song: No SongChart assigned!");
            return;
        }

        // Sort chart timeline by spawnTimeOffset just in case
        activeChart.trackTimeline.Sort((a, b) => a.spawnTimeOffset.CompareTo(b.spawnTimeOffset));

        songTimer = 0f;
        nextNoteIndex = 0;
        activeNotes.Clear();
        score = 0;
        combo = 0;
        maxCombo = 0;
        perfectCount = 0;
        goodCount = 0;
        okCount = 0;
        missCount = 0;
        isSongPlaying = true;
    }

    private void Update()
    {
        if (!isSongPlaying) return;

        songTimer += Time.deltaTime;

        // 1. Spawning logic
        SpawnNotesInTimeline();

        // 2. Player Input / Hit Window logic
        HandlePlayerInput();
    }

    private void SpawnNotesInTimeline()
    {
        while (nextNoteIndex < activeChart.trackTimeline.Count &&
               songTimer >= activeChart.trackTimeline[nextNoteIndex].spawnTimeOffset)
        {
            NoteData noteData = activeChart.trackTimeline[nextNoteIndex];
            SpawnNote(noteData);
            nextNoteIndex++;
        }
    }

    private void SpawnNote(NoteData data)
    {
        // Calculate world X coordinate based on hole index
        // Hole 1 is at -4.5f, Hole 10 is at 4.5f.
        float spawnX = data.holeLaneIndex - 5.5f;
        Vector3 spawnPosition = new Vector3(spawnX, 6.0f, 0f);

        // Select prefab based on breath state
        GameObject prefabToSpawn = (data.actionType == PlayerController.BreathState.Blow) ? noteBlowPrefab : noteDrawPrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogError($"Note prefab is missing for state: {data.actionType}");
            return;
        }

        GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        NoteObject noteObj = spawnedObj.GetComponent<NoteObject>();
        
        if (noteObj != null)
        {
            noteObj.targetHole = data.holeLaneIndex;
            noteObj.requiredState = data.actionType;
            noteObj.duration = data.holdLength;
            activeNotes.Add(noteObj);
        }
    }

    private void HandlePlayerInput()
    {
        // Register key down to handle tap scoring and play error sounds on click mismatch
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            CheckHit(true);
        }
        // Also support continuous holding for hold notes if player is actively holding and matching
        else if (playerController.IsPlayingNote)
        {
            CheckHit(false);
        }
    }

    private void CheckHit(bool isInitialClick)
    {
        int playerHole = playerController.GetCurrentHole();
        PlayerController.BreathState playerState = playerController.CurrentBreathState;

        NoteObject bestTarget = null;
        float smallestOffset = float.MaxValue;

        // Find the closest active note in the player's current lane requiring the current breath state
        for (int i = 0; i < activeNotes.Count; i++)
        {
            NoteObject note = activeNotes[i];
            if (note == null || note.IsCleared) continue;

            if (note.targetHole == playerHole && note.requiredState == playerState)
            {
                // Target hit zone is at Y = -4.0f
                float offset = Mathf.Abs(note.transform.position.y - (-4.0f));
                
                // Max OK range is +/- 0.60 units offset
                if (offset <= 0.60f && offset < smallestOffset)
                {
                    // If it is NOT the initial click (meaning player is holding RMB down),
                    // only allow hitting hold notes (duration > 0). Tap notes (duration == 0)
                    // MUST be cleared with an initial key down (GetKeyDown) to prevent cheesy holding.
                    if (!isInitialClick && note.duration == 0f)
                    {
                        continue;
                    }

                    smallestOffset = offset;
                    bestTarget = note;
                }
            }
        }

        if (bestTarget != null)
        {
            // We have a hit! Calculate accuracy rating
            bestTarget.IsCleared = true;
            activeNotes.Remove(bestTarget);

            RegisterHitScore(smallestOffset);
            PlayHarmonicaNote(playerHole - 1);

            Destroy(bestTarget.gameObject);
        }
        else if (isInitialClick)
        {
            // If the player clicked but didn't hit any valid note, play the mismatch rustle sound
            PlaySpitAirRustle();
        }
    }

    private void RegisterHitScore(float offset)
    {
        // Accuracy limits: Perfect (0.15), Good (0.35), OK (0.60)
        if (offset <= 0.15f)
        {
            perfectCount++;
            score += 100;
            combo++;
            Debug.Log("PERFECT! Offset: " + offset);
        }
        else if (offset <= 0.35f)
        {
            goodCount++;
            score += 50;
            combo++;
            Debug.Log("GOOD! Offset: " + offset);
        }
        else
        {
            okCount++;
            score += 25;
            combo++;
            Debug.Log("OK! Offset: " + offset);
        }

        if (combo > maxCombo)
        {
            maxCombo = combo;
        }
    }

    private void HandleNoteMissed(NoteObject note)
    {
        if (activeNotes.Contains(note))
        {
            activeNotes.Remove(note);
        }

        missCount++;
        combo = 0; // Reset combo on miss
        Debug.Log("MISS! Lane: " + note.targetHole);
    }

    private void PlayHarmonicaNote(int index)
    {
        if (audioSource == null) return;

        if (harmonicaNotes != null && index >= 0 && index < harmonicaNotes.Length)
        {
            AudioClip clip = harmonicaNotes[index];
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    private void PlaySpitAirRustle()
    {
        if (audioSource == null || spitAirRustle == null) return;
        audioSource.PlayOneShot(spitAirRustle);
    }
}
