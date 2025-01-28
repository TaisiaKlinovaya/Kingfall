using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Golem : PieceType
{
    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // Nach unten (kein Blockieren)
        for (int i = currentY - 1; i >= 0; i--)
        {
            r.Add(new Vector2Int(currentX, i));
        }

        // Nach oben (kein Blockieren)
        for (int i = currentY + 1; i < tileCountY; i++)
        {
            r.Add(new Vector2Int(currentX, i));
        }

        // Nach links (kein Blockieren)
        for (int i = currentX - 1; i >= 0; i--)
        {
            r.Add(new Vector2Int(i, currentY));
        }

        // Nach rechts (kein Blockieren)
        for (int i = currentX + 1; i < tileCountX; i++)
        {
            r.Add(new Vector2Int(i, currentY));
        }

        return r;
    }

}
