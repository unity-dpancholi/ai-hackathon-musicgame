using UnityEngine;

public class FeedbackPopup : MonoBehaviour
{
    private float floatSpeed = 2.0f;
    private float fadeDuration = 0.4f;
    private float lifetime = 0.5f;
    private TextMesh textMesh;
    private Color textColor;

    public void Setup(string text, Color color)
    {
        textMesh = GetComponent<TextMesh>();
        if (textMesh == null)
        {
            textMesh = gameObject.AddComponent<TextMesh>();
        }
        
        textMesh.text = text;
        textMesh.fontSize = 24;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        textColor = color;
        textMesh.color = textColor;
        
        // Scale down for 2D orthographic screen sizing
        transform.localScale = Vector3.one * 0.15f;
    }

    private void Update()
    {
        // Float upwards
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime);

        // Add physical pop effect
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * 0.22f, Time.deltaTime * 12f);

        lifetime -= Time.deltaTime;
        if (lifetime <= fadeDuration)
        {
            float alpha = Mathf.Max(0f, lifetime / fadeDuration);
            textColor.a = alpha;
            if (textMesh != null)
            {
                textMesh.color = textColor;
            }
        }

        if (lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
