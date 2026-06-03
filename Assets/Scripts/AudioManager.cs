using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

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
        }
        else
        {
            Destroy(gameObject);
        }
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
    /// Plays the corresponding blow or draw harmonica clip based on the 1-10 holeIndex and player breath state.
    /// </summary>
    /// <param name="holeIndex">The 1-based index (1 to 10) of the harmonica hole.</param>
    /// <param name="state">The active breath state (Blow vs Draw).</param>
    public void PlayNote(int holeIndex, PlayerController.BreathState state)
    {
        if (sfxSource == null) return;

        // Convert 1-based holeIndex to 0-based array index
        int arrayIndex = Mathf.Clamp(holeIndex - 1, 0, 9);
        AudioClip[] activeNotes = (state == PlayerController.BreathState.Blow) ? blowNotes : drawNotes;

        if (arrayIndex >= 0 && arrayIndex < activeNotes.Length)
        {
            AudioClip clip = activeNotes[arrayIndex];
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"Harmonica clip is missing for {(state == PlayerController.BreathState.Blow ? "Blow" : "Draw")} Hole {holeIndex}!");
            }
        }
    }

    /// <summary>
    /// Plays the spit/air rustle sound effect on a mismatch or input error.
    /// </summary>
    public void PlayMiss()
    {
        if (sfxSource != null && missSound != null)
        {
            sfxSource.PlayOneShot(missSound);
        }
    }
}
