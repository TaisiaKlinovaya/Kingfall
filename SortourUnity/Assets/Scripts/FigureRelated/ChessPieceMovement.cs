using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChessPieceMovement : MonoBehaviour {

    public string name;
    public Vector2Int CurrentPosition;
    private Boolean isFirstMove = true;
    public int team;
    private Collider[] overlappingColliders;
    PieceType piece;

    public void SeeFigure(Vector2Int targetPosition)
    {
        if (piece != null)
        {
            Debug.Log($"Found a piece: {piece.type}, Team: {piece.team}");
        }
        else
        {
            Debug.Log("No piece found on this tile.");
        }

            bool isValidMove = false;

            switch (name)
            {
                case "Pawn":
                    isValidMove = CheckMoves(targetPosition, 0, 2, 1);
                    break;
                case "Rook":
                    isValidMove = CheckMoves(targetPosition, 8, 8, 1);
                    break;
                case "Knight":
                    break;
                case "Bishop":
                    isValidMove = CheckMoves(targetPosition, 0, 0, 9);
                    break;
                case "King":
                    break;
                case "Queen":
                    isValidMove = CheckMoves(targetPosition, 8, 8, 8);
                    break;
            }

            if (isValidMove)
            {
                Debug.Log(name + " can move to " + targetPosition);
                MoveToTile(targetPosition);
            }
            else
            {
                Debug.Log(name + " cannot move to " + targetPosition);
            }
        }

        private bool isTileFree(PieceType pieceType) {
            if (pieceType != null)
            {
                if (pieceType.team == team)
                {
                    Debug.Log("cant kill piece from same team");
                    return false;
                }
            }
            return true;
        }
        public bool CheckMoves(Vector2Int targetPosition,
                            int xLimit,
                            int yLimit,
                            int diagonalLimit)
        {
            Debug.Log("CurrentPosition : " + CurrentPosition);

            int distanceX = Math.Abs(targetPosition.x - (int)CurrentPosition.x);
            int distanceY = targetPosition.y - (int)CurrentPosition.y;

            if (name == "Pawn")
            {
                if (PawnSpecific(distanceY) == false)
                {
                    return false;
                }
            }
            // X-axis movement check
            if (distanceX > xLimit)
            {
                Debug.Log(distanceX + " > " + xLimit);
                Debug.Log($"Invalid move: X-axis movement exceeds limit of {xLimit}.");
                return false;
            }


            // Y-axis movement check
            if (Math.Abs(distanceY) > yLimit)
            {
                Debug.Log($"Invalid move: Y-axis movement exceeds limit of {yLimit}.");
                return false;
            }

            // Diagonal movement check
            if (diagonalLimit > 0 && (distanceX != 0 && distanceY != 0))
            {
                // Check if diagonal distance is within the limit
                float diagonalDistance = Mathf.Sqrt(distanceX * distanceX + distanceY * distanceY);
                if (diagonalDistance > diagonalLimit)
                {
                    Debug.Log($"Invalid move: Diagonal movement exceeds limit of {diagonalLimit}.");
                    return false;
                }
            }

            return true;
        }

        private bool PawnSpecific(int distanceY)
        {
            if (!isFirstMove && Mathf.Abs(distanceY) > 1)
            {
                Debug.Log("Pawn can only go 1 forward now");
                return false;
            }

            int direction = (team == 0) ? 1 : -1; // White moves +1, Black moves -1

            if ((team == 0 && distanceY < 0) || (team == 1 && distanceY > 0))
            {
                Debug.Log("Invalid move: Pawn can only move forward.");
                return false;
            }
            isFirstMove = false;
            return true;
        }

        private void MoveToTile(Vector2Int targetPosition)
        {
            // Adjust position to center of tile
            transform.localPosition = new Vector3(targetPosition.x + 0.5f, 0, targetPosition.y + 0.5f);

            CurrentPosition = new Vector2Int(targetPosition.x, targetPosition.y);
            isFirstMove = false;

            Debug.Log($"Figure moved to: ({targetPosition.x}, {targetPosition.y})");
        }
}