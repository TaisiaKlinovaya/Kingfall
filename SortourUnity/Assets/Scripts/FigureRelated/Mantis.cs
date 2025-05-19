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

    // Die Update()-Methode wurde hier entfernt, damit PieceType.Update() für die Mantis ausgeführt wird.

    public override void SetPosition(Vector3 position, bool force = false)
    {
        Debug.Log($"[Mantis.SetPosition] Called for Mantis at ({currentX},{currentY}). Target: {position}, Force: {force}, Active: {gameObject.activeInHierarchy}");

        if (lauerAnimationCoroutine != null)
        {
            Debug.Log("[Mantis.SetPosition] Stopping previous lauer animation.");
            StopCoroutine(lauerAnimationCoroutine);
            transform.localScale = desiredScale;
            lauerAnimationCoroutine = null;
        }

        base.SetPosition(position, force);

        if (!force && gameObject.activeInHierarchy)
        {
            Debug.Log("[Mantis.SetPosition] Condition to start LauerHaltungAnimation met. Starting coroutine.");
            lauerAnimationCoroutine = StartCoroutine(LauerHaltungAnimation(position));
        }
        else if (force)
        {
            Debug.Log("[Mantis.SetPosition] Force was true, not starting LauerHaltungAnimation. Setting scale to normal.");
            transform.localScale = desiredScale;
        }
        else if (!gameObject.activeInHierarchy)
        {
            Debug.Log("[Mantis.SetPosition] GameObject not active, not starting LauerHaltungAnimation.");
        }
    }

    private IEnumerator LauerHaltungAnimation(Vector3 targetMovementPosition)
    {
        Debug.Log($"[Mantis.LauerHaltungAnimation] Coroutine STARTED. Waiting for target: {targetMovementPosition}. Current Pos: {transform.position}");

        // Warte, bis die Figur ihre Zielposition (desiredPosition aus PieceType) ungefähr erreicht hat.
        // Die Bewegung selbst wird durch PieceType.Update() gesteuert.
        while (Vector3.Distance(transform.position, targetMovementPosition) > 0.01f)
        {
            // Debug.Log($"[Mantis.LauerHaltungAnimation] Waiting... Distance: {Vector3.Distance(transform.position, targetMovementPosition)}");
            yield return null;
        }

        Debug.Log($"[Mantis.LauerHaltungAnimation] Target REACHED ({transform.position}). Starting scale animation. Normal Scale: {desiredScale}");

        Vector3 normaleSkala = desiredScale; // Hole die normale Skala von der Basisklasse
        Vector3 lauerSkala = new Vector3(
            normaleSkala.x + lauerScaleXZAmount,
            normaleSkala.y - lauerDuckAmount,
            normaleSkala.z + lauerScaleXZAmount
        );

        float halbeDauer = lauerAnimationDuration / 2f;
        float timer = 0f;

        Debug.Log("[Mantis.LauerHaltungAnimation] Phase 1: Scaling to Lauerhaltung.");
        while (timer < halbeDauer)
        {
            transform.localScale = Vector3.Lerp(normaleSkala, lauerSkala, timer / halbeDauer);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = lauerSkala;
        Debug.Log($"[Mantis.LauerHaltungAnimation] Reached Lauer Scale: {transform.localScale}");

        timer = 0f; // Timer für die zweite Phase zurücksetzen
        Debug.Log("[Mantis.LauerHaltungAnimation] Phase 2: Scaling back to Normal.");
        while (timer < halbeDauer)
        {
            transform.localScale = Vector3.Lerp(lauerSkala, normaleSkala, timer / halbeDauer);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.localScale = normaleSkala;
        Debug.Log($"[Mantis.LauerHaltungAnimation] Reached Normal Scale: {transform.localScale}. Coroutine FINISHED.");
        lauerAnimationCoroutine = null;
    }

    // --- Deine bestehenden Methoden für die Falle (IsTrapActive, GetTrapZone, SetupTrapZone, ResetTrap) ---
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
        Debug.Log($"Mantis (Team {team}) at ({currentX},{currentY}) set trap facing {direction}. Zone: [{string.Join(", ", trapZone)}]");
        if (trapZone.Count == 0) { Debug.LogWarning($"  Trap zone for Mantis at ({currentX},{currentY}) facing {direction} resulted in zero valid tiles. Deactivating trap."); isTrapSet = false; trapDirection = Vector2Int.zero; }
    }
    public void ResetTrap() { if (isTrapSet) { Debug.Log($"Mantis (Team {team}) at ({currentX},{currentY}) trap reset."); isTrapSet = false; trapDirection = Vector2Int.zero; trapZone.Clear(); } }


    // --- Deine GetAvailableMoves Methode ---
    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        Debug.Log($"[Mantis.GetAvailableMoves] Called for Mantis at ({currentX},{currentY})");
        if (lauerAnimationCoroutine != null)
        {
            Debug.Log("[Mantis.GetAvailableMoves] Stopping lauer animation before calculating moves.");
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
        Debug.Log($"[Mantis.GetAvailableMoves] Found {r.Count} moves.");
        return r;
    }

    public Vector2Int GetPosition() { return new Vector2Int(currentX, currentY); }
}