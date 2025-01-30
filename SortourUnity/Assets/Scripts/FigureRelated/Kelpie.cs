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

        // Angriff: Diagonal von dem Kelpie, zwei Felder nach oben, sowie das 3te Feld vor ihm
        int[] attackOffsetsX = { -1, 1 }; // Zwei Felder nach links und rechts
        int[] attackOffsetsY = { 2, 2 };  // Zwei Felder nach oben

        for (int i = 0; i < attackOffsetsX.Length; i++)
        {
            int newX = currentX + attackOffsetsX[i];
            int newY = currentY + attackOffsetsY[i];

            if (newX >= 0 && newX < tileCountX && newY >= 0 && newY < tileCountY)
            {
                if (board[newX, newY] != null && board[newX, newY].team != team)
                {
                    r.Add(new Vector2Int(newX, newY));
                }
            }
        }

        // Angriff: Das 3te Feld vor dem Kelpie
        int forwardY = currentY + 3;
        if (forwardY < tileCountY)
        {
            if (board[currentX, forwardY] != null && board[currentX, forwardY].team != team)
            {
                r.Add(new Vector2Int(currentX, forwardY));
            }
        }

        return r;
    }
}
