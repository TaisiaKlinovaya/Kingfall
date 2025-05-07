using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem : PieceType
{
    public List<PieceType> DefeatedPieces = new List<PieceType>(); // Liste der geschlagenen Figuren

    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // Bewegungsrichtungen: oben, unten, links, rechts
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // oben
            new Vector2Int(0, -1), // unten
            new Vector2Int(-1, 0), // links
            new Vector2Int(1, 0)   // rechts
        };

        foreach (var direction in directions)
        {
            for (int i = 1; i <= 5; i++)
            {
                int newX = currentX + direction.x * i;
                int newY = currentY + direction.y * i;

                // Prüfen, ob das Feld innerhalb des Spielfelds liegt
                if (newX >= 0 && newX < tileCountX && newY >= 0 && newY < tileCountY)
                {
                    r.Add(new Vector2Int(newX, newY));
                }
                else
                {
                    break; // Bewegung stoppen, wenn das Spielfeldende erreicht ist
                }
            }
        }

        return r;
    }

    // Diese Methode wird aufgerufen, nachdem der Golem sich bewegt hat
    public bool DefeatFiguresOnPath(ref PieceType[,] board, Vector2Int startPosition, Vector2Int endPosition)
    {
        DefeatedPieces.Clear(); // Reset list from previous moves
        bool hasDefeatedAny = false; // Track if any piece is defeated

        // Determine movement direction (normalized)
        Vector2Int direction = new Vector2Int(
            Mathf.Clamp(endPosition.x - startPosition.x, -1, 1),
            Mathf.Clamp(endPosition.y - startPosition.y, -1, 1)
        );

        // Determine path length (max steps between start and end)
        int pathLength = Mathf.Max(Mathf.Abs(endPosition.x - startPosition.x), Mathf.Abs(endPosition.y - startPosition.y));

        // Iterate along the path, EXCLUDING the start and end tiles themselves
        // The end tile is handled by the main MoveTo capture logic.
        for (int i = 1; i < pathLength; i++) // Stop BEFORE reaching endPosition
        {
            int pathX = startPosition.x + direction.x * i;
            int pathY = startPosition.y + direction.y * i;

            // Check bounds
            if (pathX >= 0 && pathX < board.GetLength(0) && pathY >= 0 && pathY < board.GetLength(1))
            {
                PieceType pieceOnPath = board[pathX, pathY];
                if (pieceOnPath != null && pieceOnPath != this) // Piece exists and is not the Golem itself
                {
                    Debug.Log($"Golem tramples {pieceOnPath.type} (Team {pieceOnPath.team}) at ({pathX},{pathY}) on its way to ({endPosition.x},{endPosition.y}).");

                    // Use GenerateBoard.Instance to process the defeated piece (handles dead list, mana etc.)
                    GenerateBoard.Instance.ProcessDefeatedPiece(pieceOnPath);
                    DefeatedPieces.Add(pieceOnPath); // Add to Golem's personal list of trample victims

                    // Remove the piece from the logical board
                    board[pathX, pathY] = null;
                    hasDefeatedAny = true; // Mark that at least one piece was defeated
                }
            }
            else
            {
                // Path went out of bounds, should not happen if endPosition is valid
                break;
            }
        }
        return hasDefeatedAny; // Return whether any pieces were trampled
    }
}