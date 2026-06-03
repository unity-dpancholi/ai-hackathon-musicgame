using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip[] harmonicaNotes = new AudioClip[10]; // Indices 0-9 for Holes 1-10
    [SerializeField] private AudioClip spitAirRustle;                      // Error sound

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
