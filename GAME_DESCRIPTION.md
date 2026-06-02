# UNITY IMPLEMENTATION SPECIFICATION: MOUTH ORGAN MAESTRO
**Specification Version:** 1.1.0  
**Target Engine:** Unity 2022.3 LTS or newer 
**Input System:** Built-in Input Manager or Input System Package (Keyboard & Mouse)

---

## 1. PROJECT ARCHITECTURE & ENVIRONMENTAL SETUP

### 1.1. 2D Orthographic Camera Setup
* **Camera Type:** Orthographic
* **Position:** `(0, 0, -10)`
* **Orthographic Size:** `5.0` (Provides a standard 10-unit vertical viewport from `Y = -5.0` to `Y = 5.0`).

### 1.2. Spatial Coordinate Blueprint
The gameplay arena operates on a 2D vertical grid divided into 10 explicit horizontal lanes.

| Lane ID | Virtual Harmonica Hole | World X-Coordinate |
| :--- | :--- | :--- |
| **Lane 1** | Hole 1 | `-4.5f` |
| **Lane 2** | Hole 2 | `-3.5f` |
| **Lane 3** | Hole 3 | `-2.5f` |
| **Lane 4** | Hole 4 | `-1.5f` |
| **Lane 5** | Hole 5 | `-0.5f` |
| **Lane 6** | Hole 6 | `0.5f` |
| **Lane 7** | Hole 7 | `1.5f` |
| **Lane 8** | Hole 8 | `2.5f` |
| **Lane 9** | Hole 9 | `3.5f` |
| **Lane 10** | Hole 10 | `4.5f` |

* **The Target Line (Hit Zone):** Positioned statically at `Y = -4.0f` at the bottom of the screen.
* **The Spawn Threshold:** Notes instantiate off-screen at `Y = 6.0f` and descend vertically.

---

## 2. CORE CODING ARCHITECTURE (C# SCRIPTS)

### 2.1. PlayerController.cs
Manages horizontal mouse positioning, the state engine for breathing, and the biological oxygen constraint loop.

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum BreathState { Blow, Draw }
    
    [Header("Movement Settings")]
    [SerializeField] private Transform mouthReticle;
    [SerializeField] private float minWorldX = -4.5f;
    [SerializeField] private float maxWorldX = 4.5f;
    
    [Header("Breath & Oxygen Settings")]
    [SerializeField] private float oxygenLevel = 0.5f; // Clamped 0.0f to 1.0f
    [SerializeField] private float drainRate = 0.15f;   // Loss per second while blowing
    [SerializeField] private float fillRate = 0.15f;    // Gain per second while drawing
    [SerializeField] private float recoveryRate = 0.25f; // Passive return to neutral (0.5f)
    
    private BreathState currentBreathState = BreathState.Blow;
    private bool isStunned = false;
    private float stunDuration = 2.0f;
    private float stunTimer = 0.0f;

    // Public Accessors for Manager Verification
    public BreathState CurrentBreathState => currentBreathState;
    public bool IsPlayingNote => Input.GetKey(KeyCode.Mouse1) && !isStunned; // RMB
    public int GetCurrentHole()
    {
        // Rounds X coordinate to determine nearest active lane index (1 to 10)
        float roundedX = Mathf.Clamp(Mathf.Round(mouthReticle.position.x), minWorldX, maxWorldX);
        return Mathf.RoundToInt(roundedX + 5.5f);
    }

    void Update()
    {
        HandleStun();
        if (isStunned) return;

        HandleMovement();
        HandleStateToggle();
        HandleOxygenMechanic();
    }

    private void HandleMovement()
    {
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float clampedX = Mathf.Clamp(mouseWorldPos.x, minWorldX, maxWorldX);
        mouthReticle.position = new Vector3(clampedX, -4.0f, 0.0f);
    }

    private void HandleStateToggle()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            currentBreathState = (currentBreathState == BreathState.Blow) ? BreathState.Draw : BreathState.Blow;
            // Trigger State UI Toggle updates here
        }
    }

    private void HandleOxygenMechanic()
    {
        if (IsPlayingNote)
        {
            if (currentBreathState == BreathState.Blow)
            {
                oxygenLevel -= drainRate * Time.deltaTime;
                if (oxygenLevel <= 0.0f) TriggerStun();
            }
            else
            {
                oxygenLevel += fillRate * Time.deltaTime;
                if (oxygenLevel >= 1.0f) TriggerStun();
            }
        }
        else
        {
            // Passive recovery toward baseline 0.5f when not interacting
            oxygenLevel = Mathf.MoveTowards(oxygenLevel, 0.5f, recoveryRate * Time.deltaTime);
        }
    }

    private void TriggerStun()
    {
        isStunned = true;
        stunTimer = stunDuration;
        oxygenLevel = 0.5f; // Reset to safe capacity baseline
    }

    private void HandleStun()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0.0f) isStunned = false;
        }
    }
}
```

### 2.2. NoteObject.cs
Attached directly to falling note prefabs to track positional lifecycle and note properties.

```csharp
using UnityEngine;

public class NoteObject : MonoBehaviour
{
    public int targetHole; // 1 to 10
    public PlayerController.BreathState requiredState;
    public float fallSpeed = 4.0f;
    public float duration = 0.0f; // 0 = standard tap, >0 = long hold note

    private bool isCleared = false;

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // Fail-safe cleanup past hit platform window
        if (transform.position.y < -5.5f && !isCleared)
        {
            isCleared = true;
            // Notify RhythmManager of a MISS event
            Destroy(gameObject);
        }
    }
}
```

### 2.3. RhythmManager.cs
Calculates the timing accuracy vector relative to the target window intersection points.

* **Accuracy Threshold Window Specifications:**
  * **Perfect Range:** `+/- 0.15` units offset from target row center.
  * **Good Range:** `+/- 0.35` units offset from target row center.
  * **OK Range:** `+/- 0.60` units offset from target row center.

---

## 3. CHART DATA STRUCTURE & SAMPLE CONTENT

### 3.1. ScriptableObject Definition (`SongChart.cs`)
```csharp
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoteData
{
    public float spawnTimeOffset; // Time delay relative to song launch marker
    public int holeLaneIndex;     // Integer 1 through 10
    public PlayerController.BreathState actionType; 
    public float holdLength;      // Value 0 for normal taps
}

[CreateAssetMenu(fileName = "NewSongChart", menuName = "MouthOrgan/SongChart")]
public class SongChart : ScriptableObject
{
    public string trackingTitle = "Mary Had a Little Lamb";
    public float beatsPerMinute = 120f;
    public List<NoteData> trackTimeline;
}
```

### 3.2. Baseline Production Map: "Mary Had a Little Lamb"
Use the following configuration mapping inside the inspector window to build out the verification song timeline:

| Track Entry | Spawn Time Offset | Target Lane Index | Required Breath State | Hold Length Value |
| :--- | :--- | :--- | :--- | :--- |
| **01** | `1.00s` | Lane 5 | `Blow` (Blue) | `0.0s` (Tap) |
| **02** | `1.50s` | Lane 4 | `Draw` (Amber) | `0.0s` (Tap) |
| **03** | `2.00s` | Lane 3 | `Blow` (Blue) | `0.0s` (Tap) |
| **04** | `2.50s` | Lane 4 | `Draw` (Amber) | `0.0s` (Tap) |
| **05** | `3.00s` | Lane 5 | `Blow` (Blue) | `0.0s` (Tap) |
| **06** | `3.50s` | Lane 5 | `Blow` (Blue) | `0.0s` (Tap) |
| **07** | `4.00s` | Lane 5 | `Blow` (Blue) | `1.0s` (Sustain) |

---

## 4. UI CANVAS & AUDIO ROUTING CONFIGURATION

### 4.1. Visual Canvas Bindings
* **Oxygen Level Indicator:** A UI Slider linked directly to `PlayerController.oxygenLevel` mapping the slider's display visually across a center-anchored layout.
* **The Toggle Panel Indicator:** Color-swaps dynamically based on the spacebar state. Use hexadecimal color blocks `#1E90FF` (Vibrant Neon Blue for Blow mode) and `#FF8C00` (Warm Amber for Draw mode).

### 4.2. Sound Event Array Layout
Create an array structure inside your Audio Subsystem Engine holding clean reference files matching the standard C-major baseline scale:

* **Index 0-9 Array Elements:** Individual distinct `.wav` or `.ogg` sound bites capturing standard harmonica note channels.
* **Mismatched Error State Node:** `spit_air_rustle.wav` triggers immediately on any input event executed where the active state configuration fails to mirror the descending chart sequence definition.
