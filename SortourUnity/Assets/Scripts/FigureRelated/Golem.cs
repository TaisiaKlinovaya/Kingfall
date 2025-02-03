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
    public void DefeatFiguresOnPath(ref PieceType[,] board, Vector2Int startPosition, Vector2Int endPosition)
    {
        DefeatedPieces.Clear(); // Liste zurücksetzen

        // Bestimme die Bewegungsrichtung
        Vector2Int direction = new Vector2Int(
            Mathf.Clamp(endPosition.x - startPosition.x, -1, 1),
            Mathf.Clamp(endPosition.y - startPosition.y, -1, 1)
        );

        // Gehe den Weg des Golems ab und besiege alle Figuren, aber NICHT den Golem selbst
        for (int i = 1; i <= 5; i++)
        {
            int newX = startPosition.x + direction.x * i;
            int newY = startPosition.y + direction.y * i;

            // Prüfen, ob das Feld innerhalb des Spielfelds liegt
            if (newX >= 0 && newX < board.GetLength(0) && newY >= 0 && newY < board.GetLength(1))
            {
                if (board[newX, newY] != null && board[newX, newY] != this) // Golem darf nicht sich selbst zerstören
                {
                    // Figur besiegen
                    PieceType defeatedPiece = board[newX, newY];
                    board[newX, newY] = null; // Figur entfernen
                    GenerateBoard.Instance.ProcessDefeatedPiece(defeatedPiece); // Figur an GenerateBoard melden
                    DefeatedPieces.Add(defeatedPiece); // Geschlagene Figur zur Liste hinzufügen

                    Debug.Log($"Golem zerstört Figur {defeatedPiece.GetType().Name} auf ({newX}, {newY})");
                }

                // Bewegung stoppen, wenn das Endfeld erreicht ist
                if (newX == endPosition.x && newY == endPosition.y)
                {
                    break;
                }
            }
            else
            {
                break; // Bewegung stoppen, wenn das Spielfeldende erreicht ist
            }
        }
    }
}