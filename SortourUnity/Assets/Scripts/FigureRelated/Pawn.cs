using System;
using System.Collections;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;

public class Pawn : PieceType
{
    private bool isFirstMove = true;
    private int movingDistance;
    public GameObject[] tiles; // Reference to all tiles
    public Vector2 CurrentPosition;

    private void Start()
    {
        CurrentPosition = transform.position;
    }
    public bool IsValidMove(Vector2Int targetPosition)
    {
        int distanceX = Mathf.Abs(targetPosition.x - (int)CurrentPosition.x);
        int distanceY = targetPosition.y - (int)CurrentPosition.y;

        // Assuming white pawn moving up the board (positive Y direction)
        if (distanceY > 2 || distanceY <= 1)
        {
            Debug.Log("Invalid move: Can only move one step forward.");
            Debug.Log(distanceY + " > 2" + " " + distanceY + " <= 0");
            return false;
        }

        // Prevent sideways movement unless capturing diagonally
        if (distanceX > CurrentPosition.x)
        {
            Debug.Log("Invalid move: Cannot move more than one square sideways.");
            Debug.Log(distanceX + " > " + CurrentPosition.x);
            return false;
        }
        Debug.Log(" DistanceY: " + distanceY);
        return true;
    }

    public void MoveToTile(Vector2Int targetPosition)
    {
        if (IsValidMove(targetPosition))
        {
            transform.position = new Vector3(targetPosition.x, transform.position.y, targetPosition.y);
            CurrentPosition = transform.position;

            Debug.Log("Pawn moved to: " + transform.position);
        }
        else
        {
            Debug.Log("Move failed.");
        }
    }
}