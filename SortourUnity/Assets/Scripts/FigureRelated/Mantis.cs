using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mantis : PieceType
{
    // --- Variablen für die Falle ---
    private bool isTrapSet = false;
    private Vector2Int trapDirection = Vector2Int.zero; // Speichert die Richtung (N,E,S,W)
    private List<Vector2Int> trapZone = new List<Vector2Int>(); // Speichert die Koordinaten der Falle

    // --- Öffentliche Methoden für die Falle (werden von GenerateBoard genutzt) ---

    /// <summary>
    /// Checks if the Mantis currently has an active trap.
    /// </summary>
    /// <returns>True if the trap is set, false otherwise.</returns>
    public bool IsTrapActive()
    {
        return isTrapSet;
    }

    /// <summary>
    /// Gets the list of coordinates covered by the active trap.
    /// </summary>
    /// <returns>A list of Vector2Int representing the trap zone. Returns an empty list if trap is not active.</returns>
    public List<Vector2Int> GetTrapZone()
    {
        // Rückgabe einer Kopie, um externe Modifikation zu verhindern (optional aber sicherer)
        return new List<Vector2Int>(trapZone);
    }

    /// <summary>
    /// Sets up the trap in the specified direction (North, East, South, West).
    /// Calculates and stores the trap zone coordinates.
    /// </summary>
    /// <param name="direction">The direction the trap faces (e.g., Vector2Int.up for North).</param>
    public void SetupTrapZone(Vector2Int direction)
    {
        // Validate direction (must be cardinal: N, E, S, W)
        if (Mathf.Abs(direction.x) + Mathf.Abs(direction.y) != 1)
        {
            Debug.LogError($"Mantis ({team}) at ({currentX},{currentY}): Invalid trap direction provided: {direction}. Trap not set.");
            isTrapSet = false; // Ensure trap is not set on error
            trapZone.Clear();
            trapDirection = Vector2Int.zero;
            return;
        }

        isTrapSet = true;
        trapDirection = direction;
        trapZone.Clear(); // Clear previous zone

        // Calculate the 3 tiles in front based on the direction
        for (int i = 1; i <= 3; i++)
        {
            int targetX = currentX + direction.x * i;
            int targetY = currentY + direction.y * i;

            // Only add tiles that are within the board boundaries
            // Use constants if available, otherwise use fixed size 8x8
            // Assuming GenerateBoard.TILE_COUNT_X and GenerateBoard.TILE_COUNT_Y exist and are accessible
            // If not, replace with 8 or get the values appropriately.
            // For simplicity, let's assume 8x8 here if constants aren't easily accessible.
            const int TILE_COUNT_X = 8;
            const int TILE_COUNT_Y = 8;
            if (targetX >= 0 && targetX < TILE_COUNT_X && targetY >= 0 && targetY < TILE_COUNT_Y)
            {
                trapZone.Add(new Vector2Int(targetX, targetY));
            }
        }
        Debug.Log($"Mantis (Team {team}) at ({currentX},{currentY}) set trap facing {direction}. Zone: [{string.Join(", ", trapZone)}]");
    }

    /// <summary>
    /// Resets the trap, clearing its state and zone.
    /// Called when the trap triggers or the Mantis moves.
    /// </summary>
    public void ResetTrap()
    {
        if (isTrapSet) // Only log/reset if a trap was actually active
        {
            Debug.Log($"Mantis (Team {team}) at ({currentX},{currentY}) trap reset.");
            isTrapSet = false;
            trapDirection = Vector2Int.zero;
            trapZone.Clear();
        }
    }

    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        Debug.Log($"[Mantis.GetAvailableMoves] === START === Called for Mantis at ({currentX},{currentY}), Team {team}. Board dimensions: {tileCountX}x{tileCountY}");

        ResetTrap(); // Reset trap before calculating new moves

        List<Vector2Int> r = new List<Vector2Int>();

        int[] dx = { 1, 1, -1, -1 };
        int[] dy = { 1, -1, 1, -1 };
        int maxSteps = 3;

        for (int i = 0; i < 4; i++) // Loop through 4 diagonal directions
        {
            Debug.Log($"  Checking Direction i={i}: ({dx[i]}, {dy[i]})");
            for (int step = 1; step <= maxSteps; step++) // Loop through steps 1, 2, 3
            {
                int currentStepX = currentX + step * dx[i];
                int currentStepY = currentY + step * dy[i];
                Debug.Log($"    Step {step}: Checking Tile ({currentStepX},{currentStepY})");

                // Check Bounds first
                if (currentStepX >= 0 && currentStepX < tileCountX && currentStepY >= 0 && currentStepY < tileCountY)
                {
                    // Tile is within bounds, check its content
                    PieceType pieceAtStep = null;
                    try // Safety Try-Catch for board access
                    {
                        pieceAtStep = board[currentStepX, currentStepY];
                    }
                    catch (System.IndexOutOfRangeException ex)
                    {
                        Debug.LogError($"      INDEX OUT OF RANGE accessing board[{currentStepX},{currentStepY}]! Exception: {ex.Message}");
                        break; // Stop checking this direction if board access fails
                    }


                    if (pieceAtStep == null)
                    {
                        // Tile is empty
                        Debug.Log($"      Tile ({currentStepX},{currentStepY}) is EMPTY. Adding move.");
                        r.Add(new Vector2Int(currentStepX, currentStepY));
                        // Continue to the next step in this direction
                    }
                    else
                    {
                        // Tile is occupied
                        Debug.Log($"      Tile ({currentStepX},{currentStepY}) is OCCUPIED by Type {pieceAtStep.type}, Team {pieceAtStep.team}.");
                        if (pieceAtStep.team != team)
                        {
                            // It's an enemy piece
                            Debug.Log($"        It's an ENEMY. Adding capture move.");
                            r.Add(new Vector2Int(currentStepX, currentStepY));
                            // Stop checking further in this direction (cannot move past capture)
                            Debug.Log($"        Path blocked by enemy, stopping direction {i}.");
                            break; // Break step loop
                        }
                        else
                        {
                            // It's a friendly piece
                            Debug.Log($"        It's FRIENDLY. Path blocked, stopping direction {i}.");
                            // Stop checking further in this direction (cannot move past friendly)
                            break; // Break step loop
                        }
                    }
                }
                else
                {
                    // Tile is out of bounds
                    Debug.Log($"      Tile ({currentStepX},{currentStepY}) is OUT OF BOUNDS. Stopping direction {i}.");
                    // Stop checking further in this direction
                    break; // Break step loop
                }
            } // End of steps loop
            Debug.Log($"  Finished checking Direction i={i}");

        } // End of directions loop

        Debug.Log($"[Mantis.GetAvailableMoves] === END === Found {r.Count} moves: [{string.Join(", ", r)}]");
        return r;
    }
    public Vector2Int GetPosition()
    {
        return new Vector2Int(currentX, currentY);
    }
}