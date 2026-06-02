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

    // Public Accessors for Manager and UI Verification
    public BreathState CurrentBreathState => currentBreathState;
    public bool IsPlayingNote => Input.GetKey(KeyCode.Mouse1) && !isStunned; // RMB
    public float OxygenLevel => oxygenLevel;
    public bool IsStunned => isStunned;
    public float StunTimer => stunTimer;
    public float StunDuration => stunDuration;

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
