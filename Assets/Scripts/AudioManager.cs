using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    
    [Header("SFX Polyphony Pool")]
    [SerializeField] private int poolSize = 16;
    private AudioSource[] sfxPool;
    private int nextPoolIndex = 0;

    [Header("Audio Clips - Blow Notes")]
    public AudioClip[] blowNotes = new AudioClip[10]; // Holes 1-10 (mapped to indices 0-9)

    [Header("Audio Clips - Draw Notes")]
    public AudioClip[] drawNotes = new AudioClip[10]; // Holes 1-10 (mapped to indices 0-9)

    [Header("Other Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    public AudioClip missSound;                      // Spit/air rustle error sound

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePool()
    {
        sfxPool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject("SFX_Pool_Voice_" + i);
            go.transform.SetParent(transform);
            
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f; // 2D Stereo
            
            sfxPool[i] = source;
        }
    }

    private AudioSource GetNextAvailableSource()
    {
        if (sfxPool == null || sfxPool.Length == 0) return null;

        // Round-robin selection of audio source
        AudioSource source = sfxPool[nextPoolIndex];
        nextPoolIndex = (nextPoolIndex + 1) % poolSize;
        return source;
    }

    public void PlayMusic()
    {
        if (musicSource != null && backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    /// <summary>
    /// Plays the corresponding blow or draw harmonica clip based on the 1-10 holeIndex and player breath state
    /// utilizing the Polyphony Pooling System.
    /// </summary>
    /// <param name="holeIndex">The 1-based index (1 to 10) of the harmonica hole.</param>
    /// <param name="state">The active breath state (Blow vs Draw).</param>
    public void PlayNote(int holeIndex, PlayerController.BreathState state)
    {
        int arrayIndex = Mathf.Clamp(holeIndex - 1, 0, 9);
        AudioClip[] activeNotes = (state == PlayerController.BreathState.Blow) ? blowNotes : drawNotes;

        if (arrayIndex >= 0 && arrayIndex < activeNotes.Length)
        {
            AudioClip clip = activeNotes[arrayIndex];
            if (clip != null)
            {
                AudioSource source = GetNextAvailableSource();
                if (source != null)
                {
                    source.clip = clip;
                    source.Play();
                }
            }
            else
            {
                Debug.LogWarning($"Harmonica clip is missing for {(state == PlayerController.BreathState.Blow ? "Blow" : "Draw")} Hole {holeIndex}!");
            }
        }
    }

    /// <summary>
    /// Plays the spit/air rustle sound effect on a mismatch or input error using the pooled voice channels.
    /// </summary>
    public void PlayMiss()
    {
        if (missSound != null)
        {
            AudioSource source = GetNextAvailableSource();
            if (source != null)
            {
                source.clip = missSound;
                source.Play();
            }
        }
    }
}
