using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class ChessPieceMovement : MonoBehaviour{

    public string name;

    //Beispielsmethode
    public void SayHi(Vector2Int position)
    {
        Debug.Log("Hallo von " + name + " auf Position " + position);
    }
}