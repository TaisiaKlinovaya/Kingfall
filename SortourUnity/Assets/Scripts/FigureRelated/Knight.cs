using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knight : PieceType
{
    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        for (int i = -2; i < 3; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if (j != 0 && i != 0 && Mathf.Abs(i) != Mathf.Abs(j))
                {
                    for (int k = 0; k < 2; k++)
                    {
                        int a = i;
                        int b = j;
                        if (k == 1)
                        {
                            a = j;
                            b = i;
                        }
                        if (currentX + a < tileCountX && currentY + b < tileCountY && currentX + a >= 0 && currentY + b >= 0)
                        {
                            if (board[currentX + a, currentY + b] == null)
                                r.Add(new Vector2Int(currentX + a, currentY + b));
                            else if (board[currentX + a, currentY + b].team != team)
                                r.Add(new Vector2Int(currentX + a, currentY + b));
                        }
                    }

                }
            }

        }
        return r;

    }
}
