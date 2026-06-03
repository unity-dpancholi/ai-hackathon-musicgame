using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private RhythmManager rhythmManager;

    [Header("Oxygen UI")]
    [SerializeField] private UnityEngine.UI.Slider oxygenSlider;
    [SerializeField] private UnityEngine.UI.Image oxygenFillImage;
    [SerializeField] private UnityEngine.UI.Text stunText; // Standard UI Text

    [Header("Breath State UI")]
    [SerializeField] private UnityEngine.UI.Image statePanelImage;
    [SerializeField] private UnityEngine.UI.Text stateText; // Standard UI Text

    [Header("Score & Combo UI")]
    [SerializeField] private UnityEngine.UI.Text scoreText; // Standard UI Text
    [SerializeField] private UnityEngine.UI.Text comboText; // Standard UI Text

    private readonly Color blowColor = new Color(0.117f, 0.564f, 1.0f); // #1E90FF
    private readonly Color drawColor = new Color(1.0f, 0.549f, 0.0f);   // #FF8C00
    private readonly Color stunColor = Color.red;

    void Update()
    {
        UpdateOxygenUI();
        UpdateBreathStateUI();
        UpdateScoreComboUI();
    }

    private void UpdateOxygenUI()
    {
        if (playerController == null) return;

        if (oxygenSlider != null)
        {
            oxygenSlider.value = playerController.OxygenLevel;
        }

        bool isStunned = playerController.IsStunned;

        if (stunText != null)
        {
            stunText.gameObject.SetActive(isStunned);
            if (isStunned)
            {
                stunText.text = $"STUNNED! ({playerController.StunTimer:F1}s)";
            }
        }

        if (oxygenFillImage != null)
        {
            oxygenFillImage.color = isStunned ? stunColor : Color.Lerp(Color.yellow, Color.green, 1.0f - Mathf.Abs(playerController.OxygenLevel - 0.5f) * 2f);
        }
    }

    private void UpdateBreathStateUI()
    {
        if (playerController == null) return;

        PlayerController.BreathState state = playerController.CurrentBreathState;

        if (statePanelImage != null)
        {
            statePanelImage.color = (state == PlayerController.BreathState.Blow) ? blowColor : drawColor;
        }

        if (stateText != null)
        {
            if (playerController.IsStunned)
            {
                stateText.text = "STUNNED";
            }
            else
            {
                stateText.text = (state == PlayerController.BreathState.Blow) ? "BLOW" : "DRAW";
            }
        }
    }

    private void UpdateScoreComboUI()
    {
        if (rhythmManager == null) return;

        if (scoreText != null)
        {
            scoreText.text = $"Score: {rhythmManager.Score}";
        }

        if (comboText != null)
        {
            comboText.text = rhythmManager.Combo > 0 ? $"Combo: {rhythmManager.Combo}" : "";
        }
    }
}
