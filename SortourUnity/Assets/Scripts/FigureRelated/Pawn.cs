using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : PieceType
{
    private bool isFirstMove = true;
    private int movingDistance;
    public GameObject[] tiles; // Reference to all tiles
    public Vector2Int CurrentPosition;

    private void Start()
    {
        CurrentPosition = new Vector2Int((int)transform.position.x, (int)transform.position.z);
    }
    public bool IsValidMove(Vector2Int targetPosition)
    {
        int distanceX = Mathf.Abs(targetPosition.x - CurrentPosition.x);
        int distanceY = targetPosition.y - CurrentPosition.y; // Moving forward on the y-axis

        // Check if the move is only forward and within one step
        if (distanceX != CurrentPosition.x)
        {
            Debug.Log("Invalid move: Cannot move sideways.");
            Debug.Log(distanceX + " != " + CurrentPosition.x);
            return false;
        }

        if (distanceY != CurrentPosition.y + 1)
        {
            Debug.Log("Invalid move: Can only move one step forward.");
            Debug.Log(distanceY + " != " + CurrentPosition.y + 1);
            return false;
        }

        return true;
    }

    public void MoveToTile(Vector2Int targetPosition)
    {
        if (IsValidMove(targetPosition))
        {
            // Update pawn position
            CurrentPosition = targetPosition;

            // Move the pawn visually (assuming grid cells map to world units)
            transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.y);

            Debug.Log("Pawn moved to: " + targetPosition);
        }
        else
        {
            Debug.Log("Move failed.");
        }
    }
}