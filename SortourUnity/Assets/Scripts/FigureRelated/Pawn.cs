using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
public class Pawn : PieceType
{
    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        int direction = (team == 0) ? 1 : -1;

        // One in front
        if (board[currentX, currentY + direction] == null)
        {
            r.Add(new Vector2Int(currentX, currentY + direction));
        }

        // Two in Front
        if (board[currentX, currentY + direction] == null)
        {
            // Weiﬂes Team
            if (team == 0 && currentY == 1 && board[currentX, currentY + (direction * 2)] == null)
            {
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            }

            // Schwarzes Team
            if (team == 1 && currentY == 6 && board[currentX, currentY + (direction * 2)] == null)
            {
                r.Add(new Vector2Int(currentX, currentY + (direction * 2)));
            }
        }
        //Kill move
        if (currentX != tileCountX - 1)
        {
            if (board[currentX + 1, currentY + direction] != null && board[currentX + 1, currentY + direction].team != team)
            {
                r.Add(new Vector2Int(currentX + 1, currentY + direction));
            }
        }

        if (currentX != 0)
        {
            if (board[currentX - 1, currentY + direction] != null && board[currentX - 1, currentY + direction].team != team)
            {
                r.Add(new Vector2Int(currentX - 1, currentY + direction));
            }
        }
        return r;
    }
}