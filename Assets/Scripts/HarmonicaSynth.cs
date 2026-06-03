using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class HarmonicaSynth : MonoBehaviour
{
    public static HarmonicaSynth Instance { get; private set; }

    [Header("Synth Settings")]
    [Range(0f, 1f)] [SerializeField] private float masterVolume = 0.35f;
    [SerializeField] private float portamentoSpeed = 18f; // Speed of pitch slide between holes
    [SerializeField] private float attackSpeed = 15f;     // Attack envelope speed
    [SerializeField] private float releaseSpeed = 12f;    // Release envelope speed

    // Standard C Major Diatonic Harmonica Richter Tuning Frequencies (Holes 1 to 10)
    private readonly float[] blowFrequencies = new float[10]
    {
        261.63f, // 1: C4
        329.63f, // 2: E4
        392.00f, // 3: G4
        523.25f, // 4: C5
        659.25f, // 5: E5
        783.99f, // 6: G5
        1046.50f,// 7: C6
        1318.51f,// 8: E6
        1567.98f,// 9: G6
        2093.00f // 10: C7
    };

    private readonly float[] drawFrequencies = new float[10]
    {
        293.66f, // 1: D4
        392.00f, // 2: G4
        493.88f, // 3: B4
        587.33f, // 4: D5
        698.46f, // 5: F5
        880.00f, // 6: A5
        987.77f, // 7: B5
        1174.66f,// 8: D6
        1396.91f,// 9: F6
        1760.00f // 10: A6
    };

    private AudioSource audioSource;
    private double sampleRate;
    private double phase;
    private double noisePhase;

    // Real-time state
    private float targetFrequency = 440f;
    private float currentFrequency = 440f;
    private float targetEnvelope = 0f;
    private float currentEnvelope = 0f;

    // LFO phases
    private float lfoPhasePitch = 0f;
    private float lfoPhaseAmp = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            audioSource = GetComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f; // 2D Stereo
            
            // OnAudioFilterRead requires the source to actually be playing
            audioSource.Play();
            sampleRate = AudioSettings.outputSampleRate;
            if (sampleRate <= 0) sampleRate = 44100;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayNote(int holeIndex, PlayerController.BreathState state)
    {
        int clampedHole = Mathf.Clamp(holeIndex - 1, 0, 9);
        targetFrequency = (state == PlayerController.BreathState.Blow) ? blowFrequencies[clampedHole] : drawFrequencies[clampedHole];
        targetEnvelope = 1f;
    }

    public void StopNote()
    {
        targetEnvelope = 0f;
    }

    private void Update()
    {
        // Smoothly interpolate current frequency to target frequency for pitch slides
        currentFrequency = Mathf.Lerp(currentFrequency, targetFrequency, Time.deltaTime * portamentoSpeed);

        // Smoothly shape the volume envelope (Attack and Release)
        float envelopeSpeed = (targetEnvelope > 0.5f) ? attackSpeed : releaseSpeed;
        currentEnvelope = Mathf.Lerp(currentEnvelope, targetEnvelope, Time.deltaTime * envelopeSpeed);

        // Update LFOs for tremolo and vibrato
        lfoPhasePitch += Time.deltaTime * 5.5f * Mathf.PI * 2f; // 5.5 Hz vibrato
        lfoPhaseAmp += Time.deltaTime * 6.0f * Mathf.PI * 2f;   // 6.0 Hz tremolo

        // Wrap phases to prevent overflow
        if (lfoPhasePitch > Mathf.PI * 2f) lfoPhasePitch -= Mathf.PI * 2f;
        if (lfoPhaseAmp > Mathf.PI * 2f) lfoPhaseAmp -= Mathf.PI * 2f;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (currentEnvelope < 0.001f)
        {
            // Reset phases when completely silent to keep processing light
            phase = 0.0;
            return;
        }

        double packSampleRate = sampleRate;
        float freq = currentFrequency;
        float env = currentEnvelope;

        // Apply slight Vibrato to frequency
        float vibratoAmount = 0.004f; // +/- 0.4% pitch modulation
        float vibrato = 1.0f + Mathf.Sin(lfoPhasePitch) * vibratoAmount;
        double modulatedFreq = freq * vibrato;

        // Apply slight Tremolo to amplitude
        float tremoloAmount = 0.15f; // 15% amplitude modulation
        float tremolo = 1.0f - tremoloAmount + (Mathf.Sin(lfoPhaseAmp) * tremoloAmount);

        for (int i = 0; i < data.Length; i += channels)
        {
            // Waveform generation: Harmonic synthesis of free reeds
            double t = phase;
            double signal = 0.0;

            // Free reed timbre: Odd harmonics dominate, but even harmonics are present
            signal += System.Math.Sin(t);                  // 1st Harmonic (Fundamental)
            signal += System.Math.Sin(t * 2.0) * 0.45;     // 2nd Harmonic
            signal += System.Math.Sin(t * 3.0) * 0.65;     // 3rd Harmonic (Rich reed odd)
            signal += System.Math.Sin(t * 4.0) * 0.20;     // 4th Harmonic
            signal += System.Math.Sin(t * 5.0) * 0.35;     // 5th Harmonic (Reed edge buzz)
            signal += System.Math.Sin(t * 6.0) * 0.10;     // 6th Harmonic
            signal += System.Math.Sin(t * 7.0) * 0.18;     // 7th Harmonic

            // Normalize signal height to fit [-1.0, 1.0] range
            signal /= 2.93; 

            // Add organic high-passed breathing noise (breath wind texture)
            double whiteNoise = (Random.value * 2.0 - 1.0) * 0.025;
            signal += whiteNoise;

            // Apply calculated envelope, master volume, and tremolo
            float finalSample = (float)(signal * env * masterVolume * tremolo);

            // Output to all channels (Mono to Stereo)
            for (int c = 0; channels > c; c++)
            {
                data[i + c] = finalSample;
            }

            // Advance phase based on modulated frequency
            double phaseIncrement = (double)modulatedFreq * 2.0 * System.Math.PI / packSampleRate;
            phase += phaseIncrement;
            if (phase > 2.0 * System.Math.PI)
            {
                phase -= 2.0 * System.Math.PI;
            }
        }
    }
}
