using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Mantis : PieceType
{
    // --- Variablen für die Falle ---
    private bool isTrapSet = false;
    private Vector2Int trapDirection = Vector2Int.zero;
    private List<Vector2Int> trapZone = new List<Vector2Int>();

    // --- Variablen für die "Lauer"-Animation ---
    private Coroutine lauerAnimationCoroutine;
    public float lauerDuckAmount = 0.05f;
    public float lauerScaleXZAmount = 0.02f;
    public float lauerAnimationDuration = 0.7f;

    // Oben in der Mantis.cs Klasse, bei den anderen Variablen
    public Color lauerFarbe = new Color(0.7f, 1f, 0.7f, 1f); // z.B. ein leicht helleres, sattes Grün
                                                             // Alpha (der letzte Wert) ist hier 1 (opak).
                                                             // Wenn dein Material Transparenz unterstützt und du willst,
                                                             // dass es leicht durchsichtig wird, setze Alpha < 1.

    // Die Update()-Methode wurde hier entfernt, damit PieceType.Update() für die Mantis ausgeführt wird.

    public override void SetPosition(Vector3 position, bool force = false)
    {
        NotificationManager.Instance.ShowMessage($"[Mantis.SetPosition] Called for Mantis at ({currentX},{currentY}). Target: {position}, Force: {force}, Active: {gameObject.activeInHierarchy}");

        if (lauerAnimationCoroutine != null)
        {
            NotificationManager.Instance.ShowMessage("[Mantis.SetPosition] Stopping previous lauer animation.");
            StopCoroutine(lauerAnimationCoroutine);
            transform.localScale = desiredScale;
            lauerAnimationCoroutine = null;
        }

        base.SetPosition(position, force);

        if (!force && gameObject.activeInHierarchy)
        {
            NotificationManager.Instance.ShowMessage("[Mantis.SetPosition] Condition to start LauerHaltungAnimation met. Starting coroutine.");
            lauerAnimationCoroutine = StartCoroutine(LauerHaltungAnimation(position));
        }
        else if (force)
        {
            NotificationManager.Instance.ShowMessage("[Mantis.SetPosition] Force was true, not starting LauerHaltungAnimation. Setting scale to normal.");
            transform.localScale = desiredScale;
        }
        else if (!gameObject.activeInHierarchy)
        {
            NotificationManager.Instance.ShowMessage("[Mantis.SetPosition] GameObject not active, not starting LauerHaltungAnimation.");
        }
    }

    // In Mantis.cs
    // In Mantis.cs

    private IEnumerator LauerHaltungAnimation(Vector3 targetMovementPosition)
    {
        NotificationManager.Instance.ShowMessage($"[Mantis.LauerHaltungAnimation] Coroutine STARTED. Waiting for target: {targetMovementPosition}. Current Pos: {transform.position}");
        // Warte, bis die Figur ihre Zielposition (desiredPosition aus PieceType) ungefähr erreicht hat.
        while (Vector3.Distance(transform.position, targetMovementPosition) > 0.015f)
        {
            yield return null; // Warte auf den nächsten Frame
        }
        NotificationManager.Instance.ShowMessage($"[Mantis.LauerHaltungAnimation] Target REACHED ({transform.position}). Starting scale and color animation. Normal Scale: {desiredScale}");

        // Materialien und Originalfarben sammeln
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true); // true, um auch inaktive Renderer zu finden (falls relevant)
        List<Material> materialsToChange = new List<Material>();
        List<Color> originalColors = new List<Color>();

        foreach (Renderer rend in renderers)
        {
            // Hole alle Materialien des aktuellen Renderers (können mehrere sein)
            foreach (Material matInstance in rend.materials) // Wichtig: rend.materials erstellt Instanzen!
            {
                if (matInstance.HasProperty("_Color")) // Nur Materialien mit der Standard "_Color" Eigenschaft berücksichtigen
                {
                    materialsToChange.Add(matInstance);
                    originalColors.Add(matInstance.color);
                }
                // Optional: Hier könntest du auch nach "_EmissionColor" suchen, wenn du ein Glühen über Emission steuern willst.
            }
        }

        if (materialsToChange.Count == 0)
        {
            Debug.LogWarning("[Mantis.LauerHaltungAnimation] No materials with '_Color' property found on Mantis. Skipping color animation.");
        }

        // Skalierungs- und Zeitvariablen
        Vector3 normaleSkala = desiredScale;
        Vector3 lauerSkala = new Vector3(
            normaleSkala.x + lauerScaleXZAmount,
            normaleSkala.y - lauerDuckAmount,
            normaleSkala.z + lauerScaleXZAmount
        );

        float anpassungsDauer = lauerAnimationDuration * 0.3f;
        float halteDauer = lauerAnimationDuration * 0.4f;
        float rueckkehrDauer = lauerAnimationDuration * 0.3f;
        float timer = 0f;

        // Phase 1: Sanft in die Lauerhaltung skalieren UND Farbe ändern
        NotificationManager.Instance.ShowMessage("[Mantis.LauerHaltungAnimation] Phase 1: Scaling and tinting to Lauerhaltung.");
        Vector3 startSkalaPhase1 = transform.localScale;
        // Wir verwenden die bereits gesammelten originalColors als Startfarben für den Lerp
        while (timer < anpassungsDauer)
        {
            float t = timer / anpassungsDauer;
            float easedT = t * t; // Einfacher quadratischer Ease-In

            transform.localScale = Vector3.Lerp(startSkalaPhase1, lauerSkala, easedT);
            for (int i = 0; i < materialsToChange.Count; i++)
            {
                materialsToChange[i].color = Color.Lerp(originalColors[i], lauerFarbe, easedT);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = lauerSkala; // Sicherstellen der Endskala
        for (int i = 0; i < materialsToChange.Count; i++) { materialsToChange[i].color = lauerFarbe; } // Sicherstellen der Endfarbe

        // Phase 2: In der Lauerhaltung "atmen" (Skalierung) und Farbe ggf. leicht pulsieren
        NotificationManager.Instance.ShowMessage("[Mantis.LauerHaltungAnimation] Phase 2: Breathing in Lauerhaltung.");
        timer = 0f;
        float atemAmplitudeFaktor = 0.2f;
        // Sicherstellen, dass halteDauer nicht 0 ist, um Division durch Null zu vermeiden
        float atemGeschwindigkeit = (halteDauer > 0) ? (Mathf.PI * 2f / halteDauer * 2f) : 0; // z.B. 2 volle Zyklen
        Vector3 basisLauerSkala = lauerSkala;

        while (timer < halteDauer && atemGeschwindigkeit > 0) // Nur atmen, wenn Dauer und Geschwindigkeit sinnvoll sind
        {
            float sinValue = Mathf.Sin(timer * atemGeschwindigkeit);
            transform.localScale = new Vector3(
                basisLauerSkala.x + (sinValue * lauerScaleXZAmount * atemAmplitudeFaktor),
                basisLauerSkala.y - (sinValue * lauerDuckAmount * atemAmplitudeFaktor * 0.5f),
                basisLauerSkala.z + (sinValue * lauerScaleXZAmount * atemAmplitudeFaktor)
            );

            // Optional: Farbpulsieren um die lauerFarbe herum
            // float colorPulseFactor = (sinValue + 1f) / 2f; // Normalisiert sinValue auf 0..1
            // for (int i = 0; i < materialsToChange.Count; i++)
            // {
            //     materialsToChange[i].color = Color.Lerp(lauerFarbe, Color.Lerp(lauerFarbe, originalColors[i], 0.2f), colorPulseFactor); // Pulsiert leicht zur Originalfarbe hin
            // }

            timer += Time.deltaTime;
            yield return null;
        }
        // Nach der Atmung sicherstellen, dass wir auf der exakten Lauer-Skala und -Farbe sind (falls Farbpulsieren aktiv war)
        transform.localScale = lauerSkala;
        for (int i = 0; i < materialsToChange.Count; i++) { materialsToChange[i].color = lauerFarbe; }


        // Phase 3: Sanft zurück zur normalen Haltung skalieren UND Farbe zurücksetzen
        NotificationManager.Instance.ShowMessage("[Mantis.LauerHaltungAnimation] Phase 3: Scaling and tinting back to Normal.");
        timer = 0f;
        Vector3 startSkalaPhase3 = transform.localScale; // Sollte lauerSkala sein
                                                         // Wir verwenden die lauerFarbe als Startfarbe für den Lerp zurück zum Original
        while (timer < rueckkehrDauer)
        {
            float t = timer / rueckkehrDauer;
            float easedT = t * t; // Einfacher quadratischer Ease-In

            transform.localScale = Vector3.Lerp(startSkalaPhase3, normaleSkala, easedT);
            for (int i = 0; i < materialsToChange.Count; i++)
            {
                materialsToChange[i].color = Color.Lerp(lauerFarbe, originalColors[i], easedT);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = normaleSkala; // Sicherstellen der Originalskala
        for (int i = 0; i < materialsToChange.Count; i++) { materialsToChange[i].color = originalColors[i]; } // Sicherstellen der Originalfarben

        NotificationManager.Instance.ShowMessage($"[Mantis.LauerHaltungAnimation] Coroutine FINISHED.");
        lauerAnimationCoroutine = null; // Coroutine-Referenz zurücksetzen
    }    // --- Deine bestehenden Methoden für die Falle (IsTrapActive, GetTrapZone, SetupTrapZone, ResetTrap) ---
    public bool IsTrapActive() { return isTrapSet; }
    public List<Vector2Int> GetTrapZone() { return new List<Vector2Int>(trapZone); }
    public void SetupTrapZone(Vector2Int direction)
    {
        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
        {
            Debug.LogError($"Mantis ({team}) at ({currentX},{currentY}): Invalid trap direction: {direction}. Trap not set.");
            isTrapSet = false; trapZone.Clear(); trapDirection = Vector2Int.zero; return;
        }
        isTrapSet = true; trapDirection = direction; trapZone.Clear();
        int centerX = currentX + direction.x; int centerY = currentY + direction.y;
        List<Vector2Int> potentialZoneTiles = new List<Vector2Int>();
        potentialZoneTiles.Add(new Vector2Int(centerX, centerY));
        if (direction.x == 0) { potentialZoneTiles.Add(new Vector2Int(centerX - 1, centerY)); potentialZoneTiles.Add(new Vector2Int(centerX + 1, centerY)); }
        else { potentialZoneTiles.Add(new Vector2Int(centerX, centerY - 1)); potentialZoneTiles.Add(new Vector2Int(centerX, centerY + 1)); }
        const int TILE_COUNT_X = 8; const int TILE_COUNT_Y = 8; // Sollte idealerweise aus GenerateBoard kommen
        foreach (Vector2Int tile in potentialZoneTiles)
        { if (tile.x >= 0 && tile.x < TILE_COUNT_X && tile.y >= 0 && tile.y < TILE_COUNT_Y) { trapZone.Add(tile); } }
        NotificationManager.Instance.ShowMessage($"Mantis (Team {team}) at ({currentX},{currentY}) set trap facing {direction}. Zone: [{string.Join(", ", trapZone)}]");
        if (trapZone.Count == 0) { Debug.LogWarning($"  Trap zone for Mantis at ({currentX},{currentY}) facing {direction} resulted in zero valid tiles. Deactivating trap."); isTrapSet = false; trapDirection = Vector2Int.zero; }
    }
    public void ResetTrap() { if (isTrapSet) { NotificationManager.Instance.ShowMessage($"Mantis (Team {team}) at ({currentX},{currentY}) trap reset."); isTrapSet = false; trapDirection = Vector2Int.zero; trapZone.Clear(); } }


    // --- Deine GetAvailableMoves Methode ---
    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        NotificationManager.Instance.ShowMessage($"[Mantis.GetAvailableMoves] Called for Mantis at ({currentX},{currentY})");
        if (lauerAnimationCoroutine != null)
        {
            NotificationManager.Instance.ShowMessage("[Mantis.GetAvailableMoves] Stopping lauer animation before calculating moves.");
            StopCoroutine(lauerAnimationCoroutine);
            transform.localScale = desiredScale;
            lauerAnimationCoroutine = null;
        }
        ResetTrap();

        List<Vector2Int> r = new List<Vector2Int>();
        int[] dx = { 1, 1, -1, -1 }; int[] dy = { 1, -1, 1, -1 }; int maxSteps = 3;
        for (int i = 0; i < 4; i++)
        {
            for (int step = 1; step <= maxSteps; step++)
            {
                int currentStepX = currentX + step * dx[i]; int currentStepY = currentY + step * dy[i];
                if (currentStepX >= 0 && currentStepX < tileCountX && currentStepY >= 0 && currentStepY < tileCountY)
                {
                    PieceType pieceAtStep = board[currentStepX, currentStepY];
                    if (pieceAtStep == null) { r.Add(new Vector2Int(currentStepX, currentStepY)); }
                    else { if (pieceAtStep.team != team) { r.Add(new Vector2Int(currentStepX, currentStepY)); } break; }
                }
                else { break; }
            }
        }
        NotificationManager.Instance.ShowMessage($"[Mantis.GetAvailableMoves] Found {r.Count} moves.");
        return r;
    }

    public Vector2Int GetPosition() { return new Vector2Int(currentX, currentY); }
}