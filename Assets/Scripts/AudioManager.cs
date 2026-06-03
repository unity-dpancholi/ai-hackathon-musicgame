using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips - Blow Notes")]
    [SerializeField] private AudioClip[] blowNotes = new AudioClip[10]; // Holes 1-10

    [Header("Audio Clips - Draw Notes")]
    [SerializeField] private AudioClip[] drawNotes = new AudioClip[10]; // Holes 1-10

    [Header("Other Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip spitAirRustle;                      // Error sound

    // Keep legacy array for compatibility
    [SerializeField] private AudioClip[] harmonicaNotes = new AudioClip[10];

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

    public void PlayHarmonicaNote(int index, PlayerController.BreathState state)
    {
        if (sfxSource == null) return;

        AudioClip[] activeArray = (state == PlayerController.BreathState.Blow) ? blowNotes : drawNotes;

        if (index >= 0 && index < activeArray.Length)
        {
            AudioClip clip = activeArray[index];
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
        else if (index >= 0 && index < harmonicaNotes.Length)
        {
            // Fallback to legacy
            AudioClip clip = harmonicaNotes[index];
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }

    // Overload for legacy compatibility
    public void PlayHarmonicaNote(int index)
    {
        if (sfxSource == null) return;

        if (index >= 0 && index < harmonicaNotes.Length)
        {
            AudioClip clip = harmonicaNotes[index];
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }

    public void PlayErrorSound()
    {
        if (sfxSource != null && spitAirRustle != null)
        {
            sfxSource.PlayOneShot(spitAirRustle);
        }
    }
}
