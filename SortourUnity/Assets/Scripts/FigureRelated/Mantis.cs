using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mantis : PieceType
{
    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // Diagonal moves: 3 fields in all four directions
        int[] dx = { 1, 1, -1, -1 };
        int[] dy = { 1, -1, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            int newX = currentX + 3 * dx[i];
            int newY = currentY + 3 * dy[i];

            // Check if the new position is within the board boundaries
            if (newX >= 0 && newX < tileCountX && newY >= 0 && newY < tileCountY)
            {
                // Check if the target field is empty or occupied by an enemy piece
                if (board[newX, newY] == null || board[newX, newY].team != team)
                {
                    r.Add(new Vector2Int(newX, newY));
                }
            }
        }
        return r;
    }
}