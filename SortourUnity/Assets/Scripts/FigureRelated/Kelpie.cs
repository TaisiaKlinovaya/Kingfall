using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Kelpie : PieceType
{
    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // Bewegung: Die gesamte Horizontale Reihe, auf der das Kelpie steht (nur bewegen, nicht schlagen)
        for (int x = 0; x < tileCountX; x++)
        {
            if (x != currentX) // Das aktuelle Feld wird ausgeschlossen
            {
                if (board[x, currentY] == null) // Nur leere Felder können betreten werden
                {
                    r.Add(new Vector2Int(x, currentY));
                }
            }
        }

        // Angriff: Diagonal von dem Kelpie, ein Feld nach oben (links und rechts)
        int forwardDirection = (team == 0) ? 1 : -1; // Team 0 bewegt sich nach oben, Team 1 nach unten

        // Diagonal ein Feld nach vorne (links und rechts)
        int[] attackOffsetsX1 = { -1, 1 }; // Ein Feld nach links und rechts
        int[] attackOffsetsY1 = { 1 * forwardDirection, 1 * forwardDirection };  // Ein Feld nach oben oder unten

        for (int i = 0; i < attackOffsetsX1.Length; i++)
        {
            int newX = currentX + attackOffsetsX1[i];
            int newY = currentY + attackOffsetsY1[i];

            if (newX >= 0 && newX < tileCountX && newY >= 0 && newY < tileCountY)
            {
                // Angriff auf das diagonale Feld
                if (board[newX, newY] != null && board[newX, newY].team != team)
                {
                    r.Add(new Vector2Int(newX, newY));
                }

                // Angriff auf das vertikale Feld über dem diagonalen Feld
                int verticalY = newY + 1 * forwardDirection;
                if (verticalY >= 0 && verticalY < tileCountY)
                {
                    if (board[newX, verticalY] != null && board[newX, verticalY].team != team)
                    {
                        r.Add(new Vector2Int(newX, verticalY));
                    }
                }
            }
        }

        // Angriff: Das 3te Feld vertikal über dem Kelpie
        int forwardY = currentY + 3 * forwardDirection;
        int intermediateY1 = currentY + 1 * forwardDirection; // Erstes Feld vor dem dritten Feld
        int intermediateY2 = currentY + 2 * forwardDirection; // Zweites Feld vor dem dritten Feld

        if (forwardY >= 0 && forwardY < tileCountY)
        {
            // Überprüfen, ob die Felder dazwischen frei sind
            if (board[currentX, intermediateY1] == null && board[currentX, intermediateY2] == null)
            {
                if (board[currentX, forwardY] != null && board[currentX, forwardY].team != team)
                {
                    r.Add(new Vector2Int(currentX, forwardY));
                }
            }
        }

        return r;
    }
}