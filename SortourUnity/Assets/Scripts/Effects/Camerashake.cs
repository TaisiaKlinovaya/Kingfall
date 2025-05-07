using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    private Transform cameraTransform;
    private Vector3 originalPosition; // Wird jetzt beim Start des Shakes von der aktiven Kamera gesetzt
    private float currentShakeDuration = 0f;
    private float currentShakeAmount = 0.07f; // Standard-Intensität
    private float currentDecreaseFactor = 1.0f;

    void Awake()
    {
        cameraTransform = GetComponent<Transform>();
        if (cameraTransform == null)
        {
            Debug.LogError("CameraShake script needs to be attached to a GameObject with a Transform (the Camera).");
            enabled = false;
        }
    }

    void Start()
    {
        // originalPosition wird jetzt dynamisch gesetzt, wenn Shake beginnt
    }

    void Update()
    {
        if (currentShakeDuration > 0)
        {
            // Wende Shake auf die lokale Position an
            cameraTransform.localPosition = originalPosition + Random.insideUnitSphere * currentShakeAmount;
            currentShakeDuration -= Time.deltaTime * currentDecreaseFactor;
        }
        else
        {
            currentShakeDuration = 0f;
            // Nur zurücksetzen, wenn es nicht schon die Originalposition ist (verhindert Jitter)
            if (cameraTransform.localPosition != originalPosition && originalPosition != Vector3.zero) // Vector3.zero als initialer Check
            {
                cameraTransform.localPosition = originalPosition;
            }
        }
    }

    public void TriggerShake(float duration, float amount, float decreaseFactor = 1.0f)
    {
        // Setze die originale Position relativ zur aktuellen Kameraposition zu Beginn des Shakes
        originalPosition = cameraTransform.localPosition;
        currentShakeDuration = duration;
        currentShakeAmount = amount;
        currentDecreaseFactor = decreaseFactor;
        Debug.Log($"CameraShake triggered: Duration={duration}, Amount={amount} on camera {cameraTransform.name}");
    }

    // Optional: Methode, um den Shake sofort zu stoppen
    public void StopShakeImmediately()
    {
        currentShakeDuration = 0f;
        cameraTransform.localPosition = originalPosition;
    }
}