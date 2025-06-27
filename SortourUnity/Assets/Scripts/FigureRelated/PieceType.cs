using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6,
    Golem = 7,
    Kelpie = 8,
    Mantis = 9
}

public class PieceType : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    protected Vector3 desiredPosition;
    protected Vector3 desiredScale = Vector3.one;

    private Coroutine flashCoroutine;
    private List<KeyValuePair<Material, Color>> originalMaterialColors;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        r.Add(new Vector2Int(3, 3));
        r.Add(new Vector2Int(3, 4));
        r.Add(new Vector2Int(4, 3));
        r.Add(new Vector2Int(4, 4));

        if (TileManager.Instance != null)
        {
            r.RemoveAll(move => TileManager.Instance.IsTileDisabled(move));
        }

        return r;

    }
    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
        {
            transform.position = desiredPosition;
        }
    }

    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if (force)
        {
            transform.localScale = desiredScale;
        }
    }
    public string GetPieceInfo()
    {
        return $"Figur {type} (Team {(team == 0 ? "Weiß" : "Schwarz")}) auf Position ({currentX}, {currentY}).";
    }
    public void FlashColor(Color flashColor, float duration = 200.0f)
    {
        // Stoppe eine eventuell bereits laufende Flash-Animation
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            // --- NEU: Setze die Farben sofort auf ihren ursprünglichen Zustand zurück ---
            RestoreOriginalColors();
        }
        // Starte die neue Flash-Animation
        flashCoroutine = StartCoroutine(FlashColorAnimation(flashColor, duration));
    }


    private IEnumerator FlashColorAnimation(Color flashColor, float duration)
    {
        // Materialien und Originalfarben sammeln und speichern
        originalMaterialColors = new List<KeyValuePair<Material, Color>>();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer rend in renderers)
        {
            foreach (Material matInstance in rend.materials)
            {
                if (matInstance.HasProperty("_Color"))
                {
                    // Speichere das Material und seine Originalfarbe
                    originalMaterialColors.Add(new KeyValuePair<Material, Color>(matInstance, matInstance.color));
                }
            }
        }

        if (originalMaterialColors.Count == 0)
        {
            Debug.LogWarning("Konnte keine Materialien mit '_Color'-Eigenschaft finden zum Aufleuchten.");
            flashCoroutine = null;
            yield break;
        }

        float halfDuration = duration;
        float timer = 0f;

        // Phase 1: Zur flashColor überblenden
        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            foreach (var matColorPair in originalMaterialColors)
            {
                matColorPair.Key.color = Color.Lerp(matColorPair.Value, flashColor, t);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // Phase 2: Zurück zur Originalfarbe überblenden
        timer = 0f;
        while (timer < halfDuration)
        {
            float t = timer / halfDuration;
            foreach (var matColorPair in originalMaterialColors)
            {
                matColorPair.Key.color = Color.Lerp(flashColor, matColorPair.Value, t);
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // Finales Zurücksetzen
        RestoreOriginalColors();
        flashCoroutine = null;
    }
    private void RestoreOriginalColors()
    {
        if (originalMaterialColors != null)
        {
            foreach (var matColorPair in originalMaterialColors)
            {
                if (matColorPair.Key != null) // Sicherstellen, dass das Material noch existiert
                {
                    matColorPair.Key.color = matColorPair.Value; // Setze die gespeicherte Originalfarbe
                }
            }
            originalMaterialColors = null; // Liste leeren
        }
    }
}

