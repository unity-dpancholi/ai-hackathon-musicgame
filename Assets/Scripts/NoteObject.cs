using UnityEngine;
using System;

public class NoteObject : MonoBehaviour
{
    public int targetHole; // 1 to 10
    public PlayerController.BreathState requiredState;
    public float fallSpeed = 4.0f;
    public float duration = 0.0f; // 0 = standard tap, >0 = long hold note

    private bool isCleared = false;

    // Static event for decoupled communication with the RhythmManager
    public static event Action<NoteObject> OnAnyNoteMissed;

    // Getter and setter for cleared state
    public bool IsCleared
    {
        get => isCleared;
        set => isCleared = value;
    }

    void Update()
    {
        // Descend vertically
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime);

        // Fail-safe cleanup past hit platform window Y = -5.5f
        if (transform.position.y < -5.5f && !isCleared)
        {
            isCleared = true;
            
            // Raise the missed event
            OnAnyNoteMissed?.Invoke(this);
            
            Destroy(gameObject);
        }
    }
}
