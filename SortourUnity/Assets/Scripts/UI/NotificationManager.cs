using UnityEngine;
using TMPro; // Wichtig für TextMeshPro
using System.Collections;

public class NotificationManager : MonoBehaviour
{
    // Singleton-Instanz, damit wir von überall darauf zugreifen können
    public static NotificationManager Instance { get; private set; }

    [Header("UI Referenzen")]
    [SerializeField] private TextMeshProUGUI notificationText; // Referenz zum Text-Objekt
    [SerializeField] private CanvasGroup notificationCanvasGroup; // Referenz zur Canvas Group für Fading

    [Header("Anzeige-Einstellungen")]
    [SerializeField] private float displayDuration = 3.0f; // Wie lange die Nachricht sichtbar bleibt
    [SerializeField] private float fadeDuration = 0.5f;    // Wie lange das Ein- und Ausblenden dauert

    private Coroutine notificationCoroutine; // Um laufende Anzeigen zu verwalten

    private void Awake()
    {
        // Singleton-Setup
        if (Instance == null)
        {
            Instance = this;
            // Optional: DontDestroyOnLoad(gameObject); wenn der Manager über Szenen hinweg bestehen soll
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Stelle sicher, dass das UI am Anfang unsichtbar ist
        if (notificationCanvasGroup != null)
        {
            notificationCanvasGroup.alpha = 0;
        }
        else
        {
            Debug.LogError("NotificationCanvasGroup ist im NotificationManager nicht zugewiesen!");
        }
    }

    /// <summary>
    /// Öffentliche Methode, um eine Nachricht anzuzeigen.
    /// </summary>
    /// <param name="message">Die anzuzeigende Nachricht.</param>
    /// <param name="messageType">Optional: Ein Typ, um die Farbe zu ändern (z.B. für Fehler).</param>
    public void ShowMessage(string message, MessageType messageType = MessageType.Info)
    {
        if (notificationText == null || notificationCanvasGroup == null)
        {
            Debug.LogError("UI-Elemente für Benachrichtigungen sind nicht zugewiesen!");
            return;
        }

        // Stoppe eine eventuell laufende vorherige Nachrichten-Animation
        if (notificationCoroutine != null)
        {
            StopCoroutine(notificationCoroutine);
        }

        // Starte die neue Coroutine, um die Nachricht anzuzeigen
        notificationCoroutine = StartCoroutine(ShowMessageAnimation(message, messageType));
    }

    private IEnumerator ShowMessageAnimation(string message, MessageType messageType)
    {
        // Setze Text und Farbe
        notificationText.text = message;
        notificationText.color = GetColorForMessageType(messageType);

        // --- Phase 1: Einblenden (Fade In) ---
        float timer = 0f;
        while (timer < fadeDuration)
        {
            notificationCanvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        notificationCanvasGroup.alpha = 1; // Sicherstellen, dass es voll sichtbar ist

        // --- Phase 2: Warten ---
        yield return new WaitForSeconds(displayDuration);

        // --- Phase 3: Ausblenden (Fade Out) ---
        timer = 0f;
        while (timer < fadeDuration)
        {
            notificationCanvasGroup.alpha = Mathf.Lerp(1, 0, timer / fadeDuration);
            timer += Time.deltaTime;
            yield return null;
        }
        notificationCanvasGroup.alpha = 0; // Sicherstellen, dass es komplett unsichtbar ist

        notificationCoroutine = null; // Coroutine ist beendet
    }

    // Helfer, um Farben basierend auf dem Nachrichtentyp zu definieren
    private Color GetColorForMessageType(MessageType type)
    {
        switch (type)
        {
            case MessageType.Info:
                return Color.white;
            case MessageType.Warning:
                return Color.yellow;
            case MessageType.Error:
                return Color.red;
            default:
                return Color.white;
        }
    }
}

// Enum zur Unterscheidung von Nachrichtentypen
public enum MessageType
{
    Info,    // Normale Information (weiß)
    Warning, // Warnung (gelb)
    Error    // Fehler (rot)
}