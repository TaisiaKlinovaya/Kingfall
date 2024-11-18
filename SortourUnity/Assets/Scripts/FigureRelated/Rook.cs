using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rook : MonoBehaviour
{
    private PieceType piece;

    private void Awake()
    {
        piece = GetComponent<PieceType>();
    }

    // Checks if the Rook can move to the target position (only horizontal or vertical moves)
    public bool CanMoveTo(int targetX, int targetY, PieceType[,] allChessPieces)
    {
        if (piece.currentX == targetX || piece.currentY == targetY)
        {
            int directionX = targetX - piece.currentX;
            int directionY = targetY - piece.currentY;

            if (directionX != 0)
            {
                int step = directionX > 0 ? 1 : -1;
                for (int x = piece.currentX + step; x != targetX; x += step)
                {
                    if (allChessPieces[x, piece.currentY] != null)
                        return false;
                }
            }
            else if (directionY != 0)
            {
                int step = directionY > 0 ? 1 : -1;
                for (int y = piece.currentY + step; y != targetY; y += step)
                {
                    if (allChessPieces[piece.currentX, y] != null)
                        return false;
                }
            }
            return true;
        }
        return false;
    }
}
