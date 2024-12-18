using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChessPieceMovement : MonoBehaviour {

    public string name;
    public Vector2 CurrentPosition;
    private Boolean isFirstMove = true;
    public int team;

    private void Start()
    {
        CurrentPosition = transform.position;
    }
    public void SeeFigure(Vector2Int targetPosition)
    {

        bool isValidMove = false;

        switch (name)
        {
            case "Pawn":
                isValidMove = CheckMoves(targetPosition, 0, 2, 1, true);
                break;
            case "Rook":
                isValidMove = CheckMoves(targetPosition, 8, 8, 1, false);
                break;
            case "Knight":
                break;
            case "Bishop":
                isValidMove = CheckMoves(targetPosition, 0, 0, 9, false);
                break;
            case "King":
                break;
            case "Queen":
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

    public bool CheckMoves(Vector2Int targetPosition,
                        int xLimit,
                        int yLimit,
                        int diagonalLimit,
                        Boolean oneDirection)
    {
        int direction = (team == 0) ? 1 : -1; // White moves +1, Black moves -1

        // Calculate distances
        int distanceX = Mathf.Abs(targetPosition.x - (int)CurrentPosition.x);
        int distanceY = targetPosition.y - (int)CurrentPosition.y;

        if (oneDirection == true)
        {
            if (team == 0 && distanceY * direction < 0)
            {
                Debug.Log("Invalid move: Can only move forward in the correct direction.");
                return false;
            }
            if (team == 1 && distanceY * direction > 0)
            {
                Debug.Log("Invalid move: Can only move forward in the correct direction.");
                return false;
            }
        }

        // X-axis movement check
        if (distanceX > xLimit)
        {
            Debug.Log($"Invalid move: X-axis movement exceeds limit of {xLimit}.");
            return false;
        }

        // Y-axis movement check
        if (Mathf.Abs(distanceY) > yLimit)
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

    public void MoveToTile(Vector2Int targetPosition)
    {
            // Adjust position to center of tile
        transform.localPosition = new Vector3(targetPosition.x + 0.5f, transform.position.y, targetPosition.y + 0.5f);

        CurrentPosition = new Vector2(targetPosition.x, targetPosition.y);
        isFirstMove = false;

        Debug.Log($"Figure moved to: ({targetPosition.x}, {targetPosition.y})");
    }
    
}