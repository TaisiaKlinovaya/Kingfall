using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

using UnityEngine;

public class ChessPieceMovement : MonoBehaviour{

    public string name;
    public Vector2 CurrentPosition;
    //Beispielsmethode
    public void CheckMoves(Vector2Int position)
    {
        if (IsInBound(position))
        {
            Debug.Log("You could move your figure there");

            if (CheckXMoves(position))
            {
                Debug.Log(name + "can go there");
            } else
            {
                Debug.Log(name + " cannot move there");
            }
        } else
        {
            Debug.Log("Movement out of bound" + position);
        }
    }
    private void Start()
    {
        CurrentPosition = transform.position;
    }

    public bool CheckXMoves(Vector2Int targetPosition)
    {
        if(name == "pawn")
        {
            int distanceX = Mathf.Abs(targetPosition.x - (int)CurrentPosition.x);
            int distanceY = targetPosition.y - (int)CurrentPosition.y;
            if (distanceY > 2 || distanceY <= 1)
            {
                Debug.Log("Invalid move: Can only move one step forward.");
                Debug.Log(distanceY + " > 2" + " " + distanceY + " <= 0");
                return false;
            }
            if (distanceX > CurrentPosition.x)
            {
                Debug.Log("Invalid move: Cannot move more than one square sideways.");
                Debug.Log(distanceX + " > " + CurrentPosition.x);
                return false;
            }
            Debug.Log(" DistanceY: " + distanceY);
            return true;
        } else
        {
            return false;
        }
    }

    public bool IsInBound(Vector2Int position)
    {
        if(position.x >= 8 && position.x <= 0)
        {
            if(position.y >= 8 && position.y <= 0)
            {
                return false;
            }
        }
        return true;
    }
}