/*
using UnityEngine;

public class PulsatingAlphaButton : MonoBehaviour
{
    public float pulsateSpeed = 1f;    // Speed of the pulsating effect
    public float minAlpha = 0.06f;      // Minimum alpha (transparency) value
    public float maxAlpha = 0.85f;        // Maximum alpha value

    public CanvasGroup canvasGroup;   // Reference to the Canvas Group component

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>(); // Get the Canvas Group component
        if (canvasGroup == null)
        {
            Debug.LogError("Canvas komponente auf dem GameObject nicht gefunden!");
        }
    }

    void Update()
    {
        if (canvasGroup != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.PingPong(Time.time * pulsateSpeed, 1));
            canvasGroup.alpha = alpha; // Update the alpha value to create a pulsating effect
            Debug.Log("Pulsing effect running!");
        }
    }
}
*/


/*
using UnityEngine;
using UnityEngine.UI;

public class pulsatingAlphaButton : MonoBehaviour
{
    public float pulsateSpeed = 1f;
    public float minAlpha = 0.06f;
    public float maxAlpha = 0.85f;

    public Image buttonImage;

    public void Start()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.Log("Image-komponente auf dem GameObject nicht gefunden!");
        }
    }

    public void Update()
    {
        if (buttonImage != null)
        {
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.PingPong(Time.time * pulsateSpeed, 1));
            Color color = buttonImage.color;
            color.a = alpha;
            buttonImage.color = color;

        }
    }
}
*/