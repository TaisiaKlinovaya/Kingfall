using System;
using UnityEngine;

public class ChessPieceMovement : MonoBehaviour
{
    //public new string name;                                                             // Name der Figur (z.B. "Pawn", "Rook"), bestimmt die Bewegungsregeln.
    //public Vector2Int CurrentPosition;                                                  // Die aktuelle Position der Figur auf dem Schachbrett.
    //private Boolean isFirstMove = true;                                                 // Ob die Figur ihren ersten Zug macht (wichtig für Bauern).
    //public int team;                                                                    // Gibt an, zu welchem Team die Figur gehört (z.B. 0 = Weiß, 1 = Schwarz).
    //private Collider[] overlappingColliders;                                            // Wird verwendet, um Kollisionen mit anderen Figuren zu überprüfen.
    //PieceType piece;                                                                    // Referenz auf den Typ der Figur (z.B. Turm, Läufer).
    //private GameObject[,] tiles;

    //// Methode, um eine Bewegung der Figur zu überprüfen und auszuführen.
    //public void SeeFigure(Vector2Int targetPosition, GameObject[,] tiles)
    //{
    //    this.tiles = tiles;
    //    // Überprüft, ob eine Figur am Zielort vorhanden ist.
    //    if (piece != null)
    //    {
    //        NotificationManager.Instance.ShowMessage($"Found a piece: {piece.type}, Team: {piece.team}");              // Gibt Infos über die Figur aus.
    //    }
    //    else
    //    {
    //        NotificationManager.Instance.ShowMessage("No piece found on this tile.");                                  // Gibt aus, dass kein Objekt auf dem Zielfeld ist.
    //    }

    //    bool isValidMove = false;                                                       // Variabel zum Speichern, ob der Zug gültig ist.

    //    if (CheckForOpponents(targetPosition, tiles) == true)
    //    {
    //        // Bewegungslogik basierend auf dem Namen der Figur.
    //        switch (name)
    //        {
    //            case "Pawn": // Wenn die Figur ein Bauer ist.
    //                isValidMove = CheckMoves(targetPosition, 0, 2, 1);                      // Spezifische Logik für Bauern.
    //                break;
    //            case "Rook": // Wenn die Figur ein Turm ist.
    //                isValidMove = CheckMoves(targetPosition, 8, 8, 1);                      // Kann sich beliebig weit horizontal/vertikal bewegen.
    //                break;
    //            case "Knight": // Wenn die Figur ein Springer ist.
    //                isValidMove = CheckKnightMove(targetPosition);                          // Springerbewegung: "L"-förmig.
    //                break;
    //            case "Bishop": // Wenn die Figur ein Läufer ist.
    //                isValidMove = CheckMoves(targetPosition, 0, 0, 9);                      // Kann sich nur diagonal bewegen.
    //                break;
    //            case "King": // Wenn die Figur ein König ist.
    //                isValidMove = CheckMoves(targetPosition, 1, 1, 1);                      // Kann sich 1 Feld in jede Richtung bewegen.
    //                break;
    //            case "Queen": // Wenn die Figur eine Dame ist.
    //                isValidMove = CheckMoves(targetPosition, 8, 8, 8);                      // Kann sich beliebig weit in jede Richtung bewegen.
    //                break;
    //        }
    //    }

    //    // Überprüfung des Zugs und Aktualisierung der Position.
    //    if (isValidMove)
    //    {
    //        NotificationManager.Instance.ShowMessage(name + " can move to " + targetPosition);                         // Log, dass der Zug gültig ist.
    //        MoveToTile(targetPosition);                                                 // Führt die Bewegung aus.
    //    }
    //    else
    //    {
    //        NotificationManager.Instance.ShowMessage(name + " cannot move to " + targetPosition);                      // Log, dass der Zug ungültig ist.
    //    }
    //}

    //// Prüft, ob ein Feld frei ist oder ob es eine gegnerische Figur enthält.
    //private bool isTileFree(PieceType pieceType)
    //{
    //    if (pieceType != null)                                                          // Wenn eine Figur auf dem Zielfeld ist.
    //    {
    //        if (pieceType.team == team)                                                 // Wenn die Figur zum gleichen Team gehört.
    //        {
    //            NotificationManager.Instance.ShowMessage("cant kill piece from same team");                            // Log, dass eigene Figuren nicht geschlagen werden können.
    //            return false;
    //        }
    //    }
    //    return true;                                                                    // Feld ist frei oder Figur gehört dem Gegner.
    //}

    ////Function to check for opposite figures
    //private bool CheckForOpponents(Vector2Int targetPosition, GameObject[,] tiles)
    //{
    //    GameObject tile = tiles[targetPosition.x, targetPosition.y];
    //    BoxCollider tileCollider = tile.GetComponent<BoxCollider>();
    //    if (tileCollider == null)
    //    {
    //        Debug.LogError("Tile collider not found!");
    //        return false;
    //    }

    //    Bounds tileBounds = tileCollider.bounds;
    //    Collider[] overlappingColliders = Physics.OverlapBox(
    //        tileBounds.center,
    //        tileBounds.extents,
    //        tile.transform.rotation,
    //        LayerMask.GetMask("Piece")
    //    );

    //    // no collision - free tile
    //    if (overlappingColliders.Length == 0)
    //    {
    //        NotificationManager.Instance.ShowMessage("Feld ist frei");
    //        return true;
    //    }

    //    PieceType otherPiece = overlappingColliders[0].GetComponent<PieceType>();

    //    if (otherPiece != null)
    //    {
    //        // if opponent figure, destroy and move
    //        if (otherPiece.team != team)
    //        {
    //            NotificationManager.Instance.ShowMessage("Gegnerische Figur gefunden - kann geschlagen werden");
    //            Destroy(otherPiece.gameObject);
    //            return true;
    //        } //if same team figure dont move
    //        else if (otherPiece.team == team)
    //        {
    //            NotificationManager.Instance.ShowMessage("gegner Figur: " + otherPiece.name);
    //            NotificationManager.Instance.ShowMessage($"Eigene Figur im Weg (Team {team})");
    //            return false;
    //        }
    //    }

    //    return true;
    //}

    //// Hauptlogik für die Überprüfung von Bewegungen.
    //public bool CheckMoves(Vector2Int targetPosition,
    //                       int xLimit,                                                  // Maximale Bewegung auf der X-Achse.
    //                       int yLimit,                                                  // Maximale Bewegung auf der Y-Achse.
    //                       int diagonalLimit)                                           // Maximale diagonale Bewegung.
    //{
    //    NotificationManager.Instance.ShowMessage("CurrentPosition : " + CurrentPosition);                              // Gibt die aktuelle Position der Figur aus.

    //    int distanceX = Math.Abs(targetPosition.x - (int)CurrentPosition.x);            // Berechnet die Bewegung auf der X-Achse.
    //    int distanceY = targetPosition.y - (int)CurrentPosition.y;                      // Berechnet die Bewegung auf der Y-Achse.

    //    if (name == "Pawn")     // Spezielle Regeln für Bauern.
    //    {
    //        if (PawnSpecific(distanceY) == false)                                       // Wenn der Bauer nicht korrekt bewegt wird.
    //        {
    //            return false;
    //        }
    //    }

    //    // Prüfung der X-Achsen-Bewegung.
    //    if (distanceX > xLimit)
    //    {
    //        NotificationManager.Instance.ShowMessage(distanceX + " > " + xLimit);                                      // Log, dass die Bewegung zu weit ist.
    //        NotificationManager.Instance.ShowMessage($"Invalid move: X-axis movement exceeds limit of {xLimit}.");
    //        return false;
    //    }

    //    // Prüfung der Y-Achsen-Bewegung.
    //    if (Math.Abs(distanceY) > yLimit)
    //    {
    //        NotificationManager.Instance.ShowMessage($"Invalid move: Y-axis movement exceeds limit of {yLimit}.");
    //        return false;
    //    }

    //    // Prüfung der diagonalen Bewegung.
    //    if (diagonalLimit > 0 && (distanceX != 0 && distanceY != 0))
    //    {
    //        // Berechnung der Diagonaldistanz.
    //        float diagonalDistance = Mathf.Sqrt(distanceX * distanceX + distanceY * distanceY);
    //        if (diagonalDistance > diagonalLimit)
    //        {
    //            NotificationManager.Instance.ShowMessage($"Invalid move: Diagonal movement exceeds limit of {diagonalLimit}.");
    //            return false;
    //        }
    //    }

    //    return true;                                                                    // Bewegung ist gültig.
    //}

    //// Spezifische Regeln für die Bewegung von Bauern.
    //private bool PawnSpecific(int distanceY)
    //{
    //    if (!isFirstMove && Mathf.Abs(distanceY) > 1)                                   // Bauern können nach dem ersten Zug nur 1 Feld ziehen.
    //    {
    //        NotificationManager.Instance.ShowMessage("Pawn can only go 1 forward now");
    //        return false;
    //    }

    //    int direction = (team == 0) ? 1 : -1;                                           // Weiß bewegt sich nach vorne (1), Schwarz nach hinten (-1).

    //    if ((team == 0 && distanceY < 0) || (team == 1 && distanceY > 0))               // Bauern dürfen nicht rückwärts ziehen.
    //    {
    //        NotificationManager.Instance.ShowMessage("Invalid move: Pawn can only move forward.");
    //        return false;
    //    }

    //    isFirstMove = false;                                                            // Nach dem ersten Zug kann der Bauer nur noch ein Feld ziehen.
    //    return true;
    //}

    //// Prüft die Bewegung des Springers (Knight).
    //private bool CheckKnightMove(Vector2Int targetPosition)
    //{
    //    int deltaX = Mathf.Abs(targetPosition.x - CurrentPosition.x);                   // Abstand auf der X-Achse.
    //    int deltaY = Mathf.Abs(targetPosition.y - CurrentPosition.y);                   // Abstand auf der Y-Achse.

    //    // Überprüfung auf "L"-förmige Bewegung.
    //    if ((deltaX == 2 && deltaY == 1) || (deltaX == 1 && deltaY == 2))
    //    {
    //        // Überprüft, ob das Ziel frei ist.
    //        Collider[] colliders = Physics.OverlapSphere(
    //            new Vector3(targetPosition.x + 0.5f, 0, targetPosition.y + 0.5f), 0.1f);

    //        foreach (Collider collider in colliders)
    //        {
    //            PieceType pieceOnTarget = collider.GetComponent<PieceType>();
    //            if (pieceOnTarget != null)
    //            {
    //                return isTileFree(pieceOnTarget);                                   // Prüft, ob die Figur vom Gegner ist.
    //            }
    //        }

    //        return true;                                                                // Ziel ist frei.
    //    }

    //    return false;                                                                   // Bewegung ist ungültig.
    //}

    //// Führt die Bewegung der Figur aus.
    //private void MoveToTile(Vector2Int targetPosition)
    //{
    //    transform.localPosition = new Vector3(targetPosition.x + 0.5f, 0, targetPosition.y + 0.5f);         // Zentriert auf Feld.
    //    CurrentPosition = new Vector2Int(targetPosition.x, targetPosition.y);                               // Aktualisiert die Position.
    //    isFirstMove = false;                                                                                // Markiert, dass die Figur bewegt wurde.
    //    NotificationManager.Instance.ShowMessage($"Figure moved to: ({targetPosition.x}, {targetPosition.y})");                            // Gibt die neue Position aus.
    //}
}
