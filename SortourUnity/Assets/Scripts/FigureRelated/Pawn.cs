using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
public class Pawn : PieceType
{
    private bool isFirstMove = true;
    public Vector2 CurrentPosition;
    public int Team;

    private void Start()
    {
        CurrentPosition = transform.position;
    }

    public bool IsValidMove(Vector2Int targetPosition)
    {
        int direction = (Team == 0) ? 1 : -1; // White moves +1, Black moves -1

        // Calculate distances
        int distanceX = Mathf.Abs(targetPosition.x - (int)CurrentPosition.x);
        int distanceY = targetPosition.y - (int)CurrentPosition.y;

        // Ensure movement is in the correct direction
        if (distanceY * direction < 0)
        {
            Debug.Log("Invalid move: Can only move forward in the correct direction.");
            return false;
        }

        // Normalize distance for direction
        int normalizedDistanceY = Mathf.Abs(distanceY);

        // Allow one step forward (or two steps on first move)
        if (normalizedDistanceY > 2 || (normalizedDistanceY == 2 && !isFirstMove))
        {
            Debug.Log("Invalid move: Too many steps forward.");
            return false;
        }

        // Sideways movement restricted to 1 square (for captures)
        if (distanceX > 1)
        {
            Debug.Log("Invalid move: Cannot move more than one square sideways.");
            return false;
        }

        // Additional check for pure forward movement (no sideways movement for non-capture)
        if (distanceX > 0 && normalizedDistanceY == 1)
        {
            Debug.Log("Invalid move: Can only move sideways when capturing.");
            return false;
        }


            return true;
    }

    public void MoveToTile(Vector2Int targetPosition)
    {
        if (IsValidMove(targetPosition))
        {
            // Adjust position to center of tile
            transform.localPosition = new Vector3(targetPosition.x + 0.5f, transform.position.y, targetPosition.y + 0.5f);

            CurrentPosition = new Vector2(targetPosition.x, targetPosition.y);
            isFirstMove = false;

            Debug.Log($"Pawn moved to: ({targetPosition.x}, {targetPosition.y})");
        }
        else
        {
            Debug.Log("Move failed.");
        }
    }
}