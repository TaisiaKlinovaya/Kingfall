using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pawn : PieceType
{
    private Boolean isFirstMove = true;
    private int movingDistance;
    public void MovePawn(int teamNum)
    {
        int direction = (teamNum == 0) ? 1 : -1;

        if(isFirstMove)
        {
            movingDistance = 2;
            isFirstMove = false;
        } else
        {
            movingDistance = 1;
        }
        
        Vector3 newPosition = transform.position + new Vector3(0, 0, direction * movingDistance);

        // Check for pieces at the destination
        Collider[] hitColliders = Physics.OverlapBox(
            newPosition,
            new Vector3(0.5f, 0.5f, 0.5f),
            Quaternion.identity,
            LayerMask.GetMask("Piece")
        );

        // Check if there are any pieces at the destination
        foreach (var hitCollider in hitColliders)
        {
            // Get the PieceType component of the hit object
            PieceType hitPiece = hitCollider.GetComponent<PieceType>();

            // Check if the piece is from a different team
            if (hitPiece != null && hitPiece.team != teamNum)
            {
                // Destroy the piece at the destination
                Destroy(hitCollider.gameObject);
                Debug.Log($"Destroyed enemy piece of team {hitPiece.team}");
            }
        }

        transform.position = newPosition;

        Debug.Log("Pawn moved " + teamNum + " direction " + direction);

        //currentY += (int)(direction * 2);


    }  
}

