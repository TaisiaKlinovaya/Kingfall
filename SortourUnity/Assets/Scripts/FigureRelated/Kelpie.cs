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
    // In FigureRelated/Kelpie.cs

    public override List<Vector2Int> GetAvailableMoves(ref PieceType[,] board, int tileCountX, int tileCountY)
    {
        List<Vector2Int> r = new List<Vector2Int>();

        // 1. Horizontale Bewegung (bleibt unverändert)
        // Kann sich auf jedes leere Feld in der aktuellen Reihe bewegen.
        for (int x = 0; x < tileCountX; x++)
        {
            if (x != currentX) // Das aktuelle Feld ausschließen
            {
                if (board[x, currentY] == null) // Nur wenn das Feld leer ist
                {
                    r.Add(new Vector2Int(x, currentY));
                }
                // Kein Schlagen in der horizontalen Bewegung
            }
        }

        // Bestimme die Vorwärtsrichtung basierend auf dem Team
        int forwardDirection = (team == 0) ? 1 : -1; // Team 0 (Weiß) bewegt sich typischerweise in positive Y-Richtung

        // 2. Spezielle "Angriffs-/Bewegungs"-Felder

        // A. Diagonal ein Feld nach vorne (links und rechts)
        int[] XOffsets = { -1, 1 }; // (-1 für links, +1 für rechts)

        foreach (int dx in XOffsets)
        {
            int zielX = currentX + dx;
            int zielY = currentY + forwardDirection; // Ein Feld nach vorne

            // Prüfen, ob das Zielfeld innerhalb des Bretts liegt
            if (zielX >= 0 && zielX < tileCountX && zielY >= 0 && zielY < tileCountY)
            {
                // Das Kelpie kann auf dieses Feld ziehen, wenn es leer ist ODER von einem Gegner besetzt ist.
                if (board[zielX, zielY] == null || board[zielX, zielY].team != team)
                {
                    r.Add(new Vector2Int(zielX, zielY));
                }
            }

            // B. Vertikal ein Feld *über* dem gerade geprüften diagonalen Feld
            // Dieses Feld ist nur relevant, wenn das diagonale Feld existiert.
            // (Die vorherige Logik hat dieses Feld nur als Angriffsziel betrachtet,
            // wir erweitern es jetzt auch für Bewegung, wenn leer).
            if (zielX >= 0 && zielX < tileCountX && zielY >= 0 && zielY < tileCountY) // Sicherstellen, dass das diagonale Feld gültig war
            {
                int vertikalUeberDiagonalY = zielY + forwardDirection; // Ein weiteres Feld nach vorne

                // Prüfen, ob dieses "darüberliegende" Feld innerhalb des Bretts liegt
                if (vertikalUeberDiagonalY >= 0 && vertikalUeberDiagonalY < tileCountY)
                {
                    // Das Kelpie kann auf dieses Feld ziehen, wenn es leer ist ODER von einem Gegner besetzt ist.
                    if (board[zielX, vertikalUeberDiagonalY] == null || board[zielX, vertikalUeberDiagonalY].team != team)
                    {
                        r.Add(new Vector2Int(zielX, vertikalUeberDiagonalY));
                    }
                }
            }
        }


        // C. Das 3. Feld vertikal direkt vor dem Kelpie
        int direktVorneY1 = currentY + forwardDirection;      // Erstes Feld davor
        int direktVorneY2 = currentY + 2 * forwardDirection;  // Zweites Feld davor
        int direktVorneY3 = currentY + 3 * forwardDirection;  // Drittes Feld davor (Ziel)

        // Prüfen, ob alle drei Felder (inklusive Ziel) auf dem Brett liegen
        // und die ersten beiden Felder davor frei sind.
        if (direktVorneY1 >= 0 && direktVorneY1 < tileCountY &&
            direktVorneY2 >= 0 && direktVorneY2 < tileCountY &&
            direktVorneY3 >= 0 && direktVorneY3 < tileCountY)
        {
            // Die ersten beiden Felder müssen leer sein für diesen speziellen Sprung/Zug
            if (board[currentX, direktVorneY1] == null && board[currentX, direktVorneY2] == null)
            {
                // Das Kelpie kann auf das 3. Feld ziehen, wenn es leer ist ODER von einem Gegner besetzt ist.
                if (board[currentX, direktVorneY3] == null || board[currentX, direktVorneY3].team != team)
                {
                    r.Add(new Vector2Int(currentX, direktVorneY3));
                }
            }
        }

        return r;
    }
}