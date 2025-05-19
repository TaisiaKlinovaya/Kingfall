// In FigureRelated/Kelpie.cs

using UnityEngine;
using System.Collections.Generic; // Hinzugefügt für List<Vector2Int>

public class Kelpie : PieceType
{
    // Variablen für die Wellenanimation
    private float waveTimer = 0f;
    public float waveAmplitude = 0.05f; // Wie hoch die "Welle" ist
    public float waveSpeed = 5f;     // Wie schnell die "Welle" oszilliert

    // Überschreibe die Update-Methode der Basisklasse PieceType
    private void Update()
    {
        // Die problematische Zeile wurde entfernt.

        // Prüfe, ob sich das Kelpie bewegt (d.h. aktuelle Position ist nicht die Zielposition)
        // Da wir in der Kelpie-Klasse sind, ist this.type == ChessPieceType.Kelpie implizit.
        if (transform.position != desiredPosition)
        {
            waveTimer += Time.deltaTime * waveSpeed;
            float waveOffsetY = Mathf.Sin(waveTimer) * waveAmplitude;

            // Erstelle eine temporäre Zielposition für die visuelle Bewegung,
            // die den Wellenoffset auf der Y-Achse beinhaltet.
            // Die 'desiredPosition' selbst (logisches Ziel) bleibt unverändert.
            Vector3 visualTargetPosition = new Vector3(desiredPosition.x, desiredPosition.y + waveOffsetY, desiredPosition.z);

            // Bewege die Figur sanft (Lerp) zur visuellen Zielposition
            transform.position = Vector3.Lerp(transform.position, visualTargetPosition, Time.deltaTime * 10);
        }
        else
        {
            // Wenn das Kelpie seine Zielposition erreicht hat (oder sich nicht bewegt),
            // stelle sicher, dass es genau auf der desiredPosition ist und resette den waveTimer.
            if (transform.position != desiredPosition) // Finale Korrektur, falls Lerp nicht exakt war
            {
                transform.position = desiredPosition;
            }
            waveTimer = 0f; // Reset für die nächste Bewegung
        }

        // Die Skalierung wird weiterhin von der Basislogik gehandhabt, da Kelpie.Update
        // die PieceType.Update überschreibt und diese Logik hier enthalten sein muss.
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    // Deine GetAvailableMoves-Methode für Kelpie...
    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // Bewegung: Die gesamte Horizontale Reihe, auf der das Kelpie steht (nur bewegen, nicht schlagen)
        for (int x = 0; x < tileCountX; x++)
        {
            if (x != currentX) // Das aktuelle Feld wird ausgeschlossen
            {
                if (board[x, currentY] == null) // Nur leere Felder können betreten werden
                {
                    r.Add(new Vector2Int(x, currentY));
                }
            }
        }

        // Angriff: Diagonal von dem Kelpie, ein Feld nach oben (links und rechts)
        int forwardDirection = (team == 0) ? 1 : -1; // Team 0 bewegt sich nach oben, Team 1 nach unten

        // Diagonal ein Feld nach vorne (links und rechts)
        int[] attackOffsetsX1 = { -1, 1 }; // Ein Feld nach links und rechts
        int[] attackOffsetsY1 = { 1 * forwardDirection, 1 * forwardDirection };  // Ein Feld nach oben oder unten

        for (int i = 0; i < attackOffsetsX1.Length; i++)
        {
            int newX = currentX + attackOffsetsX1[i];
            int newY = currentY + attackOffsetsY1[i];

            if (newX >= 0 && newX < tileCountX && newY >= 0 && newY < tileCountY)
            {
                // Angriff auf das diagonale Feld
                if (board[newX, newY] != null && board[newX, newY].team != team)
                {
                    r.Add(new Vector2Int(newX, newY));
                }

                // Angriff auf das vertikale Feld über dem diagonalen Feld
                int verticalY = newY + 1 * forwardDirection;
                if (verticalY >= 0 && verticalY < tileCountY)
                {
                    if (board[newX, verticalY] != null && board[newX, verticalY].team != team)
                    {
                        r.Add(new Vector2Int(newX, verticalY));
                    }
                }
            }
        }

        // Angriff: Das 3te Feld vertikal über dem Kelpie
        int forwardY = currentY + 3 * forwardDirection;
        int intermediateY1 = currentY + 1 * forwardDirection; // Erstes Feld vor dem dritten Feld
        int intermediateY2 = currentY + 2 * forwardDirection; // Zweites Feld vor dem dritten Feld

        if (forwardY >= 0 && forwardY < tileCountY)
        {
            // Überprüfen, ob die Felder dazwischen frei sind
            if (board[currentX, intermediateY1] == null && board[currentX, intermediateY2] == null)
            {
                if (board[currentX, forwardY] != null && board[currentX, forwardY].team != team)
                {
                    r.Add(new Vector2Int(currentX, forwardY));
                }
            }
        }
        return r;
    }
}