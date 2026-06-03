using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float shakeMagnitude = 0.15f;
    
    private Vector3 originalPos;

    private void Start()
    {
        originalPos = transform.position;
        if (playerController == null)
        {
            playerController = FindFirstObjectByType<PlayerController>();
        }
    }

    private void Update()
    {
        if (playerController != null && playerController.IsStunned)
        {
            // Vigorously shake screen when the player is gasping for air
            float randomX = Random.Range(-1f, 1f) * shakeMagnitude;
            float randomY = Random.Range(-1f, 1f) * shakeMagnitude;
            transform.position = originalPos + new Vector3(randomX, randomY, 0f);
        }
        else
        {
            // Reset position smoothly
            transform.position = Vector3.MoveTowards(transform.position, originalPos, Time.deltaTime * 5f);
        }
    }
}
