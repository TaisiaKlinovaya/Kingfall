using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using Unity.VisualScripting;
#endif
using UnityEngine;

[System.Serializable]
public class TileState
{
    public Vector2Int position;
    public int disabledRounds;
    [System.NonSerialized] public GameObject tileObject;
    public bool isDisabled => disabledRounds >= 0;
}

public class GenerateBoard : MonoBehaviour
{

    private enum MantisTrapState { None, AwaitingDirectionInput }
    private MantisTrapState currentMantisTrapState = MantisTrapState.None;
    private Mantis mantisAwaitingTrapSetup = null; // Speichert die Mantis, für die wir die Falle stellen

    public static GenerateBoard ChessboardInstance { get; private set; }

    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float deathSize = 0.2f;
    [SerializeField] private float deathSpacing = 0.4f;
    [SerializeField] private float dragOffset = 1f;
    [SerializeField] private int RegenManaAmount = 2;
    [SerializeField] private GameObject lightningEffectPrefab;
    [SerializeField] private int lightningDuration = 20;

    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    public GameObject[,] tiles;
    private Camera currentCamera;
    private Vector3 bounds;
    private bool isKingDead = false;
    private String winTeam;

    private List<Vector2Int> availableMoves = new List<Vector2Int>();
    [SerializeField] private GameObject[] BlackTeamPrefabs;
    [SerializeField] private GameObject[] WhiteTeamPrefabs;
    private PieceType[,] allChessPieces;
    private Chessboard chessboard;
    private PieceType currentlyDragging;
    private List<PieceType> deadWhites = new List<PieceType>();
    private List<PieceType> deadBlacks = new List<PieceType>();

    public static GenerateBoard Instance { get; private set; }
    GameObject tile;
    private Collider[] overlappingColliders;
    private Vector2Int currentHover;
    private const int TILE_COUNT = 8;
    private bool isBoardGenerated = false;
    private bool isSpawningInProgress = false;

    private PieceType selectedPieceForTransformation = null;
    public bool hasMoved = false; // Flag, um zu überprüfen, ob eine Figur bewegt wurde
    public bool hasTransformed = false; // Flag, um zu überprüfen, ob eine Transformation durchgeführt wurde
    private List<PieceType> deadWhiteTransformations = new List<PieceType>();
    private List<PieceType> deadBlackTransformations = new List<PieceType>();
    private bool isManaStormActive = false;

    [Header("Transformation Costs")]
    public int golemTransformationCost = 5;
    public int kelpieTransformationCost = 5;
    public int mantisTransformationCost = 4; // Beispielwert, passe ihn nach Bedarf an


    private PieceType lastMovedOrTransformedPiece = null;
    private bool mantisTrapDirectionChosenThisTurn = false;
    private List<TileState> disabledTiles = new List<TileState>();
    [SerializeField] public Material disabledTileMaterial; // Assign in Inspector
    private Material defaultTileMaterial;

    private TileManager tileManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        allChessPieces = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];

        // TileManager sicher initialisieren
        if (TileManager.Instance == null)
        {
            var tileManagerObj = new GameObject("TileManager");
            tileManagerObj.AddComponent<TileManager>();
        }

        // Stellen Sie sicher, dass disabledTileMaterial im Inspector zugewiesen ist
        if (disabledTileMaterial == null)
            Debug.LogError("disabledTileMaterial is not assigned in GenerateBoard!");

        TileManager.Instance.Initialize(tiles, tileMaterial, disabledTileMaterial);

        chessboard = gameObject.AddComponent<Chessboard>();
        chessboard.Initialize(tiles);
    }

    private void Start()
    {
        if (ChessboardInstance != null && ChessboardInstance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            ChessboardInstance = this;
        }

        if (currentCamera == null)
        {
            currentCamera = GameObject.Find("Player1Camera").GetComponent<Camera>();
        }
    }

    // In BoardRelated/GenerateBoard.cs


    public bool HasPlayerPerformedActionThisTurn()
    {
        return hasMoved || hasTransformed;
    }
    private void Update()
    {
        // Spielstart und Initialisierungslogik (unverändert)
        if (GameManager.Instance.State == "GameRun" && !isBoardGenerated && !isSpawningInProgress)
        {
            isSpawningInProgress = true;
            StartCoroutine(SpawnAndPositionPiecesWithDelay());
        }
        if (GameManager.Instance.State == "StartMenu")
        {
            isBoardGenerated = false;
            isSpawningInProgress = false;
            DeleteAllPieces();
            currentMantisTrapState = MantisTrapState.None; // Reset Mantis trap state
            mantisAwaitingTrapSetup = null;
        }

        // König tot Logik (unverändert)
        if (isKingDead == true)
        {
            GameManager.Instance.WinGame(winTeam);
            isKingDead = false;
        }

        // Kamera-Fallback (unverändert)
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            if (!currentCamera) // Immer noch keine Kamera? Das ist ein Problem.
            {
                Debug.LogError("Keine Kamera im Spiel gefunden oder zugewiesen!");
                return; // Verhindere weitere Ausführung ohne Kamera
            }
        }

        // Hauptlogik, wenn das Spiel läuft
        if (GameManager.Instance.State == "GameRun")
        {
            // Kamerawechsel basierend auf Spieler (unverändert)
            if (GameManager.Instance.CurrentPlayer == 1)
            {
                // Null-Check für GameObject.Find, bevor GetComponent aufgerufen wird
                GameObject p1CamObj = GameObject.Find("Player1Camera");
                if (p1CamObj != null) currentCamera = p1CamObj.GetComponent<Camera>();
                else Debug.LogError("Player1Camera GameObject nicht gefunden!");
            }
            if (GameManager.Instance.CurrentPlayer == 2)
            {
                GameObject p2CamObj = GameObject.Find("Player2Camera");
                if (p2CamObj != null) currentCamera = p2CamObj.GetComponent<Camera>();
                else Debug.LogError("Player2Camera GameObject nicht gefunden!");
            }

            // Nur fortfahren, wenn das Brett generiert wurde und eine Kamera existiert
            if (isBoardGenerated && currentCamera != null)
            {
                RaycastHit info;
                Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

                // ================================================================================
                // NEUE LOGIK: MANTIS FALLENSTELLEN PER MAUS
                // ================================================================================
                if (currentMantisTrapState == MantisTrapState.AwaitingDirectionInput && mantisAwaitingTrapSetup != null)
                {
                    // UI-Hinweis (sollte über ein richtiges UI-System laufen)
                    // Fürs Erste: Debug.Log("MANTIS FALLE: L-Klick (West), R-Klick (Ost), Mausrad Vor (Nord), Mausrad Zurück (Süd). Oder klicke 'Finished'.");

                    Vector2Int chosenDirection = Vector2Int.zero;

                    if (Input.GetMouseButtonDown(0)) // Linksklick für Westen
                    {
                        chosenDirection = Vector2Int.left;
                        Debug.Log("Mantis Trap Input: Westen (Linksklick)");
                    }
                    else if (Input.GetMouseButtonDown(1)) // Rechtsklick für Osten
                    {
                        chosenDirection = Vector2Int.right;
                        Debug.Log("Mantis Trap Input: Osten (Rechtsklick)");
                    }
                    else if (Input.GetAxis("Mouse ScrollWheel") > 0.05f) // Mausrad nach vorne für Norden (mit Schwellenwert)
                    {
                        chosenDirection = Vector2Int.up;
                        Debug.Log("Mantis Trap Input: Norden (Mausrad Vor)");
                    }
                    else if (Input.GetAxis("Mouse ScrollWheel") < -0.05f) // Mausrad nach hinten für Süden (mit Schwellenwert)
                    {
                        chosenDirection = Vector2Int.down;
                        Debug.Log("Mantis Trap Input: Süden (Mausrad Zurück)");
                    }

                    if (chosenDirection != Vector2Int.zero)
                    {
                        // Überprüfen, ob die Mantis noch gültig ist
                        if (allChessPieces[mantisAwaitingTrapSetup.currentX, mantisAwaitingTrapSetup.currentY] == mantisAwaitingTrapSetup &&
                            mantisAwaitingTrapSetup.team == GameManager.Instance.CurrentPlayer - 1)
                        {
                            mantisAwaitingTrapSetup.SetupTrapZone(chosenDirection);
                            // Modus beenden, nachdem Falle gestellt wurde
                            currentMantisTrapState = MantisTrapState.None;
                            mantisAwaitingTrapSetup = null;
                            Debug.Log("Mantis-Falle erfolgreich gestellt. Zug kann jetzt beendet werden.");
                            // hasMoved wurde schon durch die Bewegung gesetzt.
                            // Wenn das Fallenstellen eine "Aktion" ist, die "hasTransformed" setzen soll,
                            // dann hier: hasTransformed = true; (und Mana-Kosten ggf.)
                        }
                        else
                        {
                            Debug.LogWarning("Mantis für Fallenstellung war nicht mehr gültig. Modus wird zurückgesetzt.");
                            currentMantisTrapState = MantisTrapState.None;
                            mantisAwaitingTrapSetup = null;
                        }
                    }
                    // Wichtig: Hier KEIN return; damit der Rest von Update (z.B. Figurendrehung beim Draggen) noch laufen kann,
                    // FALLS man das so möchte. Fürs Erste ist es sauberer, wenn im Fallenmodus nur die Falle bedient wird.
                    // Wenn man im Fallenmodus ist, sollte das Hovern der Tiles etc. pausiert werden (siehe unten).
                }
                // ================================================================================
                // ENDE NEUE LOGIK: MANTIS FALLENSTELLEN
                // ================================================================================
                // Normale Interaktion (Hover, Figurenauswahl, Bewegung) nur, wenn NICHT im Mantis-Fallen-Modus
                else if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
                {
                    Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

                    // Hover-Logik (unverändert)
                    if (currentHover == -Vector2Int.one && hitPosition != -Vector2Int.one)
                    {
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }
                    if (currentHover != -Vector2Int.one && currentHover != hitPosition)
                    {
                        tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        currentHover = hitPosition;
                        if (hitPosition != -Vector2Int.one) // Nur wenn hitPosition gültig ist
                        {
                            tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                        }
                    }

                    // Linksklick-Logik (Figurenauswahl, Bewegung, Mantis-Fallenmodus-Aktivierung)
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (hitPosition == -Vector2Int.one) // Klick ins Leere
                        {
                            if (currentlyDragging != null) // Wenn eine Figur gezogen wurde, aber nicht auf ein Feld geklickt
                            {
                                currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY)); // Zurücksetzen
                                currentlyDragging = null;
                                RemoveHighlightTiles();
                            }
                        }
                        else // Klick auf ein gültiges Feld
                        {
                            if (currentlyDragging == null) // Phase 1: Figur auswählen oder Mantis-Fallenmodus aktivieren
                            {
                                if (allChessPieces[hitPosition.x, hitPosition.y] != null)
                                {
                                    PieceType clickedPiece = allChessPieces[hitPosition.x, hitPosition.y];

                                    // Fall 1: Auf gerade bewegte Mantis des aktuellen Spielers klicken, um Fallenmodus zu starten
                                    if (clickedPiece == lastMovedOrTransformedPiece &&
                                        clickedPiece is Mantis mantisToSetup &&
                                        clickedPiece.team == GameManager.Instance.CurrentPlayer - 1 &&
                                        hasMoved && !hasTransformed) // Mantis muss sich schon bewegt haben, aber keine andere Aktion (Transformation)
                                    {
                                        currentMantisTrapState = MantisTrapState.AwaitingDirectionInput;
                                        mantisAwaitingTrapSetup = mantisToSetup;
                                        Debug.Log($"Mantis bei ({clickedPiece.currentX},{clickedPiece.currentY}) FÜR FALLENSTELLUNG ausgewählt.");
                                        RemoveHighlightTiles(); // Entferne normale Zug-Highlights
                                                                // Kein 'currentlyDragging' setzen!
                                    }
                                    // Fall 2: Normale Figurenauswahl für Bewegung
                                    else if (clickedPiece.team == GameManager.Instance.CurrentPlayer - 1 && !hasMoved)
                                    {
                                        currentlyDragging = clickedPiece;
                                        Debug.Log($"[GenerateBoard.Update] FIGUR AUSGEWÄHLT: {currentlyDragging.type} at ({currentlyDragging.currentX},{currentlyDragging.currentY}) für Bewegung.");
                                        availableMoves = currentlyDragging.GetAvailableMoves(ref allChessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                                        HighlightTiles();
                                    }
                                    // Fall 3: Klick auf andere Figur (nicht auswählbar unter aktuellen Bedingungen)
                                    // else { Debug.Log("Cannot select this piece now."); }
                                }
                            }
                            else // Phase 2: Figur wurde bereits gezogen (currentlyDragging != null), Zug ausführen
                            {
                                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);

                                if (!validMove)
                                {
                                    // Ungültiger Zug, Figur visuell zurücksetzen
                                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                                }
                                // hasMoved wird jetzt in MoveTo gesetzt, wenn der Zug erfolgreich war.

                                currentlyDragging = null;
                                RemoveHighlightTiles();
                            }
                        }
                    } // Ende Input.GetMouseButtonDown(0)

                    // Rechtsklick für Transformation (deine bestehende Logik, unverändert)
                    if (Input.GetMouseButtonDown(1))
                    {
                        if (hitPosition != -Vector2Int.one && allChessPieces[hitPosition.x, hitPosition.y] != null)
                        {
                            PieceType clickedPiece = allChessPieces[hitPosition.x, hitPosition.y];
                            if (clickedPiece.team == GameManager.Instance.CurrentPlayer - 1)
                            {
                                if (clickedPiece.type == ChessPieceType.Rook) { selectedPieceForTransformation = clickedPiece; Debug.Log("Rook selected for transformation."); }
                                else if (clickedPiece.type == ChessPieceType.Knight) { selectedPieceForTransformation = clickedPiece; Debug.Log("Knight selected for transformation."); }
                                else if (clickedPiece.type == ChessPieceType.Bishop) { selectedPieceForTransformation = clickedPiece; Debug.Log("Bishop selected for transformation."); }
                                else { selectedPieceForTransformation = null; Debug.Log($"{clickedPiece.type} cannot be transformed."); }
                            }
                            else { selectedPieceForTransformation = null; }
                        }
                        else { selectedPieceForTransformation = null; }
                    }
                }
                else // Kein Tile getroffen beim Raycast (Maus ist nicht über dem Brett)
                {
                    if (currentHover != -Vector2Int.one)
                    {
                        // Stelle sicher, dass currentHover gültig ist, bevor auf tiles zugegriffen wird
                        if (currentHover.x >= 0 && currentHover.x < TILE_COUNT_X && currentHover.y >= 0 && currentHover.y < TILE_COUNT_Y)
                        {
                            tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        }
                        currentHover = -Vector2Int.one;
                    }
                }

                // Figur-Dragging-Logik (visuell, unverändert)
                if (currentlyDragging)
                {
                    Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                    float distance = 0.0f;
                    if (horizontalPlane.Raycast(ray, out distance))
                    {
                        currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset, true); // force = true für direktes Dragging
                    }
                }
            } // Ende if (isBoardGenerated && currentCamera != null)
        } // Ende if (GameManager.Instance.State == "GameRun")

        // Die alte WASD-Fallenlogik wurde entfernt.
    }

    public void ResetMantisTrapMode()
    {
        if (currentMantisTrapState == MantisTrapState.AwaitingDirectionInput)
        {
            Debug.Log("Mantis trap mode reset because turn ended before trap was set.");
        }
        currentMantisTrapState = MantisTrapState.None;
        mantisAwaitingTrapSetup = null;
        // mantisTrapDirectionChosenThisTurn wird von ResetLastMovedPieceAndTrapChoice() gehandhabt
    }
    // In BoardRelated/GenerateBoard.cs
    public void ResetLastMovedPieceAndTrapChoice() // Oder umbenennen zu ResetLastMovedPiece
    {
        lastMovedOrTransformedPiece = null;
        // mantisTrapDirectionChosenThisTurn = false; // Diese Zeile ist nicht mehr nötig
    }
    // Füge diese Methoden zu GenerateBoard.cs hinzu:
    public void ResetSelectedPieceForTransformation()
    {
        selectedPieceForTransformation = null;
    }

    public void ResetLastMovedPiece()
    {
        lastMovedOrTransformedPiece = null;
    }
    public void SetCamera(int activeTeam)
    {
        if (activeTeam == 1)
        {
            currentCamera = GameManager.Instance.player1Camera;
        }
        else if (activeTeam == 2)
        {
            currentCamera = GameManager.Instance.player2Camera;
            Debug.Log("camera set to player2 in setCamera");
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject($"Tile{x}{y}");
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(0, yOffset, 0);
        vertices[1] = new Vector3(0, yOffset, tileSize);
        vertices[2] = new Vector3(tileSize, yOffset, 0);
        vertices[3] = new Vector3(tileSize, yOffset, tileSize);

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.transform.localPosition = new Vector3(x * tileSize, 0, y * tileSize);
        tileObject.layer = LayerMask.NameToLayer("Tile");

        BoxCollider collider = tileObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(tileSize, 0.5f, tileSize);
        collider.center = new Vector3(tileSize / 2, 0, tileSize / 2);

        tileObject.layer = LayerMask.NameToLayer("Tile");

        return tileObject;
    }

    public void Initialize(GameObject[,] tiles)
    {
        this.tiles = tiles;
        currentHover = -Vector2Int.one;
    }

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        defaultTileMaterial = tileMaterial;
        bounds = new Vector3((tileCountX / 2) * tileSize, 0, (tileCountX / 2) * tileSize);
        tiles = new GameObject[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }
    public void RemoveHighlightTilesPublic()
    {
        RemoveHighlightTiles();
    }
    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    private bool ContainsValidMove(ref List<Vector2Int> moves, Vector2 pos)
    {
        for (int i = 0; i < moves.Count; i++)
        {
            if (moves[i].x == pos.x && moves[i].y == pos.y)
            {
                return true;
            }
        }
        return false;
    }
    public Mantis GetMantisAwaitingTrapSetting()
    {
        if (lastMovedOrTransformedPiece != null && lastMovedOrTransformedPiece is Mantis mantis)
        {
            // Optional: Zusätzliche Prüfung, ob die Mantis noch auf dem Brett ist
            if (allChessPieces[mantis.currentX, mantis.currentY] == mantis)
            {
                return mantis;
            }
        }
        return null;
    }
    // In BoardRelated/GenerateBoard.cs

    // In BoardRelated/GenerateBoard.cs

    private bool MoveTo(PieceType cp, int x, int y)
    {
        // --- Vorabprüfungen (TileManager, Mantis Trap) ---
        if (TileManager.Instance == null)
        {
            Debug.LogError("TileManager ist nicht initialisiert in MoveTo!");
            return false;
        }
        if (TileManager.Instance.IsTileDisabled(new Vector2Int(x, y)))
        {
            Debug.Log($"Ungültiger Zug: Zielfeld ({x},{y}) ist deaktiviert.");
            return false;
        }

        // Mantis-Fallenprüfung (wie in deinem Code)
        int opponentTeam = 1 - cp.team;
        for (int mx = 0; mx < TILE_COUNT_X; mx++)
        {
            for (int my = 0; my < TILE_COUNT_Y; my++)
            {
                PieceType potentialMantis = allChessPieces[mx, my];
                if (potentialMantis != null &&
                    potentialMantis.team == opponentTeam &&
                    potentialMantis is Mantis mantis &&
                    mantis.IsTrapActive())
                {
                    if (mantis.GetTrapZone().Contains(new Vector2Int(x, y)))
                    {
                        Debug.LogWarning($"MANTIS FALLE AUSGELÖST! Figur {cp.type} (Team {cp.team}) wollte nach ({x},{y}) ziehen und trat in Falle von Mantis (Team {opponentTeam}) auf ({mantis.currentX},{mantis.currentY}).");
                        Vector2Int originalPosition = new Vector2Int(cp.currentX, cp.currentY);
                        ProcessDefeatedPiece(cp); // Die Figur, die in die Falle getreten ist
                        allChessPieces[originalPosition.x, originalPosition.y] = null; // Vom Startfeld entfernen
                        mantis.ResetTrap();
                        hasMoved = true; // Zählt als Aktion für den Zug
                        lastMovedOrTransformedPiece = null; // Keine Figur hat den Zug erfolgreich abgeschlossen
                        return true; // Zug beendet (durch Falle)
                    }
                }
            }
        }
        // --- Ende Mantis-Fallenprüfung ---

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Standard-Zugvalidierung (ob das Feld prinzipiell aus der Figurenlogik erreichbar ist)
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            Debug.Log($"Ungültiger Zug für {cp.type}: Ziel ({x},{y}) ist nicht in der Liste der verfügbaren Züge.");
            return false;
        }

        // === NEU: SIMULATION UND ÜBERPRÜFUNG AUF SELBST-SCHACH ===
        // 1. Erstelle eine temporäre Kopie des Bretts für die Simulation.
        //    Dies stellt sicher, dass wir das echte 'allChessPieces'-Array nicht verändern,
        //    bevor der Zug als legal bestätigt wurde.
        PieceType[,] simulatedBoard = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];
        System.Array.Copy(allChessPieces, simulatedBoard, allChessPieces.Length);

        // 2. Führe den Zug auf dem simulierten Brett aus.
        //    Beachte: Das Schlagen auf dem simulierten Brett ist hier vereinfacht.
        //    Wir entfernen die Zielfigur nicht explizit aus simulatedBoard, da IsKingInCheck
        //    primär die Angriffslinien prüft. Für eine 100% exakte Simulation aller
        //    Spezialfälle (wie Golem-Trample, das den Weg freiräumt) wäre mehr Aufwand nötig.
        simulatedBoard[x, y] = cp; // Setze die ziehende Figur auf das Zielfeld im simulierten Brett
        simulatedBoard[previousPosition.x, previousPosition.y] = null; // Leere das Startfeld im simulierten Brett

        // 3. Überprüfe, ob der eigene König nach diesem simulierten Zug im Schach stünde.
        //    cp.team ist das Team der Figur, die gerade zieht (also das Team des eigenen Königs).
        if (IsKingInCheck(cp.team, simulatedBoard, TILE_COUNT_X, TILE_COUNT_Y))
        {
            Debug.LogWarning($"UNGÜLTIGER ZUG für {cp.type} nach ({x},{y}): Der eigene König (Team {cp.team}) stünde im Schach.");
            // Wichtig: Da der Zug ungültig ist, werden keine Änderungen am echten `allChessPieces`-Array vorgenommen.
            return false; // Der Zug ist nicht erlaubt.
        }
        // === ENDE NEU: SIMULATION UND ÜBERPRÜFUNG ===


        // Wenn wir hier ankommen, ist der Zug legal (stellt den eigenen König nicht ins Schach).
        // Führe den Zug jetzt auf dem ECHTEN Brett (`allChessPieces`) aus:

        // --- Handle Capturing (auf dem echten Brett) ---
        PieceType targetPieceOnRealBoard = allChessPieces[x, y]; // Figur auf dem Zielfeld des echten Bretts
        if (targetPieceOnRealBoard != null) // Ist das Zielfeld besetzt?
        {
            // Die Prüfung, ob es eine eigene Figur ist, sollte durch die Logik von
            // GetAvailableMoves und ContainsValidMove bereits abgedeckt sein (diese sollten keine
            // Züge auf eigene Figuren erlauben, außer ggf. Rochade).
            // Die Simulation oben hätte auch ein Problem gemeldet, wenn man auf eine eigene Figur zieht und dadurch Schach entsteht.
            // Dennoch, als Sicherheitsnetz:
            if (targetPieceOnRealBoard.team == cp.team)
            {
                Debug.LogError($"KRITISCHER FEHLER: Versuch, eigene Figur ({targetPieceOnRealBoard.type}) bei ({x},{y}) zu schlagen, obwohl Zug als legal eingestuft wurde. Dies sollte nicht passieren.");
                return false; // Verhindere den Zug
            }
            else
            {
                // Es ist eine gegnerische Figur
                Debug.Log($"{cp.type} (Team {cp.team}) schlägt {targetPieceOnRealBoard.type} (Team {targetPieceOnRealBoard.team}) auf ({x},{y}).");
                ProcessDefeatedPiece(targetPieceOnRealBoard); // Verarbeitet die besiegte Figur

                if (targetPieceOnRealBoard.type == ChessPieceType.King)
                {
                    isKingDead = true;
                    winTeam = (targetPieceOnRealBoard.team == 1) ? "White" : "Black"; // Gewinner ist das Team, das den König geschlagen hat
                    Debug.LogWarning($"KÖNIG GESCHLAGEN! Team {winTeam} gewinnt!");
                }
            }
        }

        // --- Handle Special Piece Logic (Golem Trample - auf dem echten Brett) ---
        // Diese Logik wird NACH der Schachprüfung ausgeführt, da sie das Brett verändert.
        if (cp.type == ChessPieceType.Golem)
        {
            Golem golem = cp as Golem;
            if (golem != null)
            {
                bool trampledAnyPieces = golem.DefeatFiguresOnPath(ref allChessPieces, previousPosition, new Vector2Int(x, y));
                if (trampledAnyPieces)
                {
                    Debug.Log("Golem hat Figuren zertrampelt, löse Kamera-Shake aus.");
                    GameManager.Instance.TriggerActiveCameraShake(0.6f, 0.15f);
                }
                // Überprüfe, ob der König durch das Trampeln besiegt wurde (nachdem die Figuren entfernt wurden)
                foreach (var defeatedPieceInPath in golem.DefeatedPieces)
                {
                    if (defeatedPieceInPath.type == ChessPieceType.King)
                    {
                        isKingDead = true;
                        winTeam = (defeatedPieceInPath.team == 1) ? "White" : "Black";
                        Debug.LogWarning($"KÖNIG DURCH GOLEM ZERTRAMPELT! Team {winTeam} gewinnt!");
                        break;
                    }
                }
            }
        }

        // --- Finalize the Move (auf dem echten Brett) ---
        // Setze die Figur auf das Zielfeld und leere das Startfeld im echten Brett-Array.
        allChessPieces[x, y] = cp;
        allChessPieces[previousPosition.x, previousPosition.y] = null;

        // Aktualisiere die interne Position der Figur und die visuelle Darstellung.
        positionSinglePiece(x, y); // force=false für sanfte Bewegung

        // Setze Flags für den aktuellen Zug.
        lastMovedOrTransformedPiece = cp;
        hasMoved = true;

        // Debug.Log($"Figur {cp.GetType().Name} (Team {(cp.team == 0 ? "Weiß" : "Schwarz")}) zog von ({previousPosition.x},{previousPosition.y}) nach ({x},{y}).");

        return true; // Der Zug war erfolgreich.
    }

    // Stelle sicher, dass du auch die IsKingInCheck-Methode in GenerateBoard.cs hast:
    //public bool IsKingInCheck(int kingTeam, PieceType[,] boardState, int tileCountX, int tileCountY)
    //{
    //    Vector2Int kingPosition = -Vector2Int.one;
    //    for (int r = 0; r < tileCountX; r++) // r für row/rank (x)
    //    {
    //        for (int c = 0; c < tileCountY; c++) // c für column/file (y)
    //        {
    //            if (boardState[r, c] != null &&
    //                boardState[r, c].type == ChessPieceType.King &&
    //                boardState[r, c].team == kingTeam)
    //            {
    //                kingPosition = new Vector2Int(r, c);
    //                break;
    //            }
    //        }
    //        if (kingPosition != -Vector2Int.one) break;
    //    }

    //    if (kingPosition == -Vector2Int.one)
    //    {
    //        // Dieser Fall sollte idealerweise nie eintreten in einem laufenden Spiel.
    //        // Debug.LogError($"Konnte König für Team {kingTeam} nicht auf dem Brett finden! (In IsKingInCheck)");
    //        return true; // Vorsichtshalber als "im Schach" werten, um Fehler zu vermeiden.
    //    }

    //    int attackerTeam = 1 - kingTeam;
    //    for (int r = 0; r < tileCountX; r++)
    //    {
    //        for (int c = 0; c < tileCountY; c++)
    //        {
    //            PieceType piece = boardState[r, c];
    //            if (piece != null && piece.team == attackerTeam)
    //            {
    //                // Wichtig: Erzeuge eine temporäre Liste, da GetAvailableMoves die Referenz erwartet
    //                PieceType[,] tempBoardRef = boardState; // Für den ref-Parameter
    //                List<Vector2Int> attackerMoves = piece.GetAvailableMoves(ref tempBoardRef, tileCountX, tileCountY);

    //                if (ContainsValidMove(ref attackerMoves, kingPosition))
    //                {
    //                    // Debug.Log($"König von Team {kingTeam} auf ({kingPosition.x},{kingPosition.y}) steht im Schach durch {piece.type} von Team {attackerTeam} auf ({r},{c}).");
    //                    return true;
    //                }
    //            }
    //        }
    //    }
    //    return false;
    //}
    public Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one;
    }

    private PieceType SpawnSinglePiece(ChessPieceType type, int team)
    {
        GameObject prefab = (team == 0) ? WhiteTeamPrefabs[(int)type - 1] : BlackTeamPrefabs[(int)type - 1];

        PieceType piece = Instantiate(prefab, transform).GetComponent<PieceType>();

        if (piece == null)
        {
            return null;
        }

        piece.type = type;
        piece.team = team;
        piece.gameObject.layer = LayerMask.NameToLayer("Piece");

        return piece;
    }

    private IEnumerator SpawnAndPositionPiecesWithDelay()
    {
        DeleteAllPieces();
        allChessPieces = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0;
        int blackTeam = 1;

        void SpawnAndPositionPiece(ChessPieceType type, int team, int x, int y)
        {
            if (allChessPieces[x, y] != null)
            {
                Debug.LogError($"Unexpected piece at {x},{y}. This shouldn't happen!");
                return;
            }

            allChessPieces[x, y] = SpawnSinglePiece(type, team);
            positionSinglePiece(x, y, true);
        }

        SpawnAndPositionPiece(ChessPieceType.Rook, whiteTeam, 0, 0);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Knight, whiteTeam, 1, 0);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Bishop, whiteTeam, 2, 0);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Queen, whiteTeam, 3, 0);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.King, whiteTeam, 4, 0);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Bishop, whiteTeam, 5, 0);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Knight, whiteTeam, 6, 0);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Rook, whiteTeam, 7, 0);
        yield return new WaitForSeconds(0.1f);

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            SpawnAndPositionPiece(ChessPieceType.Pawn, whiteTeam, i, 1);
            yield return new WaitForSeconds(0.1f);
        }

        SpawnAndPositionPiece(ChessPieceType.Rook, blackTeam, 0, 7);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Knight, blackTeam, 1, 7);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Bishop, blackTeam, 2, 7);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Queen, blackTeam, 3, 7);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.King, blackTeam, 4, 7);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Bishop, blackTeam, 5, 7);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Knight, blackTeam, 6, 7);
        yield return new WaitForSeconds(0.1f);
        SpawnAndPositionPiece(ChessPieceType.Rook, blackTeam, 7, 7);

        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            SpawnAndPositionPiece(ChessPieceType.Pawn, blackTeam, i, 6);
            yield return new WaitForSeconds(0.1f);
        }

        isBoardGenerated = true;
        isSpawningInProgress = false;
        yield break;
    }

    private void positionSinglePiece(int x, int y, Boolean force = false)
    {
        allChessPieces[x, y].currentX = x;
        allChessPieces[x, y].currentY = y;
        allChessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
        allChessPieces[x, y].transform.localPosition = new Vector3(x * tileSize + (tileSize / 2), yOffset, y * tileSize + (tileSize / 2));
    }

    public void DeleteAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (allChessPieces[x, y] != null)
                {
                    Destroy(allChessPieces[x, y].gameObject);
                    allChessPieces[x, y] = null;
                }
            }
        }

        foreach (PieceType deadPiece in deadWhites)
        {
            if (deadPiece != null)
            {
                Destroy(deadPiece.gameObject);
            }
        }
        deadWhites.Clear();

        foreach (PieceType deadPiece in deadBlacks)
        {
            if (deadPiece != null)
            {
                Destroy(deadPiece.gameObject);
            }
        }
        deadBlacks.Clear();

        foreach (PieceType deadPiece in deadWhiteTransformations)
        {
            if (deadPiece != null)
            {
                Destroy(deadPiece.gameObject);
            }
        }
        deadWhiteTransformations.Clear();

        foreach (PieceType deadPiece in deadBlackTransformations)
        {
            if (deadPiece != null)
            {
                Destroy(deadPiece.gameObject);
            }
        }
        deadBlackTransformations.Clear();
    }

    public void ResetBoardState()
    {
        hasMoved = false;
        hasTransformed = false;
        currentlyDragging = null;
        RemoveHighlightTiles();
    }

    public void ProcessDefeatedPiece(PieceType defeatedPiece, bool fromManaStorm = false)
    {
        if (defeatedPiece == null) return;

        MoveToGraveyard(defeatedPiece);

        if (!fromManaStorm && IsOpponentsPiece(defeatedPiece))
        {
            HandleManaGain(defeatedPiece);
        }
    }

    private void MoveToGraveyard(PieceType piece, bool fromManaStorm = false)
    {
        if (piece == null) return;

        bool isTransformation = piece.type == ChessPieceType.Golem ||
                              piece.type == ChessPieceType.Kelpie ||
                              piece.type == ChessPieceType.Mantis;

        List<PieceType> graveyard = piece.team == 0 ?
            (isTransformation ? deadWhiteTransformations : deadWhites) :
            (isTransformation ? deadBlackTransformations : deadBlacks);

        graveyard.Add(piece);
        piece.SetScale(Vector3.one * deathSize);

        // Positionierung basierend auf Team und Typ
        Vector3 basePosition = piece.team == 0 ?
            new Vector3(isTransformation ? 9 : 8, yOffset - 0.23f, -1) :
            new Vector3(isTransformation ? -2 : -1, yOffset - 0.23f, 8);

        Vector3 offset = piece.team == 0 ?
            Vector3.forward :
            Vector3.back;

        Vector3 deathPosition = (basePosition * tileSize) - bounds +
                              new Vector3(tileSize / 2, 0, tileSize / 2) +
                              (offset * deathSpacing * graveyard.Count);

        // Besondere Behandlung für Mana-Sturm-Opfer
        if (fromManaStorm)
        {
            // Sofortige Positionierung ohne Animation
            piece.transform.position = deathPosition;
        }
        else
        {
            // Normale sanfte Bewegung
            piece.SetPosition(deathPosition);
        }
    }

    // Hilfsmethode: Überprüft ob Figur dem Gegner gehört
    private bool IsOpponentsPiece(PieceType piece)
    {
        int currentPlayer = GameManager.Instance.CurrentPlayer;
        int opponentTeam = 1 - (currentPlayer - 1);
        return piece.team == opponentTeam;
    }

    // Hilfsmethode: Mana-Verwaltung
    private void HandleManaGain(PieceType defeatedPiece)
    {
        int currentPlayer = GameManager.Instance.CurrentPlayer;
        int currentMana = GameManager.Instance.GetCurrentMana(currentPlayer);
        int newMana = currentMana + RegenManaAmount;

        if (newMana > 10)
        {
            GameManager.Instance.SetCurrentMana(currentPlayer, 10);
            Debug.Log($"Mana overflow! Triggering Mana Storm.");
            TriggerManaStorm(currentPlayer);
        }
        else
        {
            GameManager.Instance.SetCurrentMana(currentPlayer, newMana);
            Debug.Log($"Player {currentPlayer} gained {RegenManaAmount} mana (now: {newMana}/10)");
        }
    }

    public PieceType GetSelectedPieceForTransformation()
    {
        return selectedPieceForTransformation;
    }

    public PieceType TransformRookToGolem(PieceType rook)
    {
        if (rook.type != ChessPieceType.Rook)
        {
            Debug.LogError("Only Rooks can be transformed into Golems.");
            selectedPieceForTransformation = null; // Reset selection on error
            return null; // Indicate failure
        }

        int x = rook.currentX;
        int y = rook.currentY;
        int team = rook.team; // Store team before destroying

        // Remove old piece logically
        allChessPieces[x, y] = null;
        // Remove old piece visually
        Destroy(rook.gameObject);

        // Get the correct Golem prefab based on team
        // Ensure the index matches the enum value (Golem should be at index (int)ChessPieceType.Golem - 1)
        GameObject golemPrefab = (team == 0) ? WhiteTeamPrefabs[(int)ChessPieceType.Golem - 1] : BlackTeamPrefabs[(int)ChessPieceType.Golem - 1];
        if (golemPrefab == null)
        {
            Debug.LogError($"Golem Prefab for team {team} not found or assigned in the inspector!");
            return null; // Indicate failure
        }

        // Instantiate the new Golem
        GameObject golemObject = Instantiate(golemPrefab, transform);
        PieceType golem = golemObject.GetComponent<PieceType>();
        if (golem == null)
        {
            Debug.LogError("Instantiated Golem prefab is missing the PieceType component!");
            Destroy(golemObject); // Clean up the failed instantiation
            return null; // Indicate failure
        }


        // Set Golem's rotation based on team
        if (team == 0) // White
        {
            golemObject.transform.rotation = Quaternion.Euler(0, 270, 0); // Facing 'up'
        }
        else // Black
        {
            golemObject.transform.rotation = Quaternion.Euler(0, 90, 0); // Facing 'down'
        }

        // Set Golem properties
        golem.type = ChessPieceType.Golem;
        golem.team = team;
        golem.currentX = x;
        golem.currentY = y;
        golem.gameObject.layer = LayerMask.NameToLayer("Piece");

        // Place new Golem logically
        allChessPieces[x, y] = golem;
        // Place new Golem visually (force immediate position)
        positionSinglePiece(x, y, true);

        selectedPieceForTransformation = null; // Clear selection after successful transformation
        Debug.Log($"Rook (Team {team}) transformed into Golem at ({x}, {y}).");
        return golem; // Return the new Golem piece
    }

    public PieceType TransformKnightToKelpie(PieceType knight)
    {
        if (knight.type != ChessPieceType.Knight)
        {
            Debug.LogError("Only Knights can be transformed into Kelpies.");
            selectedPieceForTransformation = null; // Reset selection on error
            return null; // Indicate failure
        }

        int x = knight.currentX;
        int y = knight.currentY;
        int team = knight.team; // Store team before destroying

        // Remove old piece logically
        allChessPieces[x, y] = null;
        // Remove old piece visually
        Destroy(knight.gameObject);

        // Get the correct Kelpie prefab based on team
        // Ensure the index matches the enum value (Kelpie should be at index (int)ChessPieceType.Kelpie - 1)
        GameObject kelpiePrefab = (team == 0) ? WhiteTeamPrefabs[(int)ChessPieceType.Kelpie - 1] : BlackTeamPrefabs[(int)ChessPieceType.Kelpie - 1];
        if (kelpiePrefab == null)
        {
            Debug.LogError($"Kelpie Prefab for team {team} not found or assigned in the inspector!");
            return null; // Indicate failure
        }

        // Instantiate the new Kelpie
        GameObject kelpieObject = Instantiate(kelpiePrefab, transform);
        PieceType kelpie = kelpieObject.GetComponent<PieceType>();
        if (kelpie == null)
        {
            Debug.LogError("Instantiated Kelpie prefab is missing the PieceType component!");
            Destroy(kelpieObject); // Clean up the failed instantiation
            return null; // Indicate failure
        }

        // Set Kelpie's rotation based on team
        if (team == 0) // White
        {
            kelpieObject.transform.rotation = Quaternion.Euler(0, 180, 0); // Facing 'up'
        }
        else // Black
        {
            kelpieObject.transform.rotation = Quaternion.Euler(0, 0, 0); // Facing 'down'
        }

        // Set Kelpie properties
        kelpie.type = ChessPieceType.Kelpie;
        kelpie.team = team;
        kelpie.currentX = x;
        kelpie.currentY = y;
        kelpie.gameObject.layer = LayerMask.NameToLayer("Piece");

        // Place new Kelpie logically
        allChessPieces[x, y] = kelpie;
        // Place new Kelpie visually (force immediate position)
        positionSinglePiece(x, y, true);

        selectedPieceForTransformation = null; // Clear selection after successful transformation
        Debug.Log($"Knight (Team {team}) transformed into Kelpie at ({x}, {y}).");
        return kelpie; // Return the new Kelpie piece
    }
    public PieceType TransformBishopToMantis(PieceType bishop)
    {
        if (bishop.type != ChessPieceType.Bishop)
        {
            Debug.LogError("Only Bishops can be transformed into Mantis.");
            selectedPieceForTransformation = null; // Auswahl zurücksetzen bei Fehler
            return null;
        }

        int x = bishop.currentX;
        int y = bishop.currentY;
        int team = bishop.team;

        // Alte Figur entfernen (logisch und visuell)
        allChessPieces[x, y] = null;
        Destroy(bishop.gameObject);

        // Neues Mantis Prefab holen (Achte auf den korrekten Index im Array!)
        GameObject mantisPrefab = (team == 0) ? WhiteTeamPrefabs[(int)ChessPieceType.Mantis - 1] : BlackTeamPrefabs[(int)ChessPieceType.Mantis - 1];
        if (mantisPrefab == null)
        {
            Debug.LogError($"Mantis Prefab for team {team} not found or assigned!");
            return null; // Wichtig: Abbrechen, wenn Prefab fehlt
        }

        GameObject mantisObject = Instantiate(mantisPrefab, transform);
        PieceType mantis = mantisObject.GetComponent<PieceType>();

        // Setze Rotation (optional, je nach Modell, hier Standard)
        // mantisObject.transform.rotation = Quaternion.identity; // Oder spezifische Rotation

        // Eigenschaften setzen
        mantis.type = ChessPieceType.Mantis;
        mantis.team = team;
        mantis.currentX = x;
        mantis.currentY = y;
        mantis.gameObject.layer = LayerMask.NameToLayer("Piece");

        // Neue Figur platzieren (logisch und visuell)
        allChessPieces[x, y] = mantis;
        positionSinglePiece(x, y, true); // force = true für sofortige Positionierung

        selectedPieceForTransformation = null; // Auswahl nach erfolgreicher Transformation zurücksetzen
        Debug.Log($"Bishop transformed into Mantis at ({x}, {y}).");
        return mantis; // Gib die neue Mantis-Figur zurück
    }
    public void ResetDraggingPiece()
    {
        if (currentlyDragging != null)
        {
            // Setze die Figur zurück auf ihre ursprüngliche Position
            currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
            currentlyDragging = null;
            RemoveHighlightTiles();
        }
    }
    public void TransformPiece()
    {
        // Überprüfe, ob bereits eine Bewegung gemacht wurde
        if (!hasMoved)
        {
            Debug.Log("Du musst zuerst eine Figur bewegen, bevor du transformieren kannst.");
            return;
        }

        if (hasTransformed)
        {
            Debug.Log("Du kannst nur eine Figur pro Zug transformieren.");
            return;
        }

        PieceType selectedPiece = GetSelectedPieceForTransformation();
        if (selectedPiece != null)
        {
            // Überprüfe, ob die ausgewählte Figur dem aktuellen Spieler gehört
            if (selectedPiece.team == GameManager.Instance.CurrentPlayer - 1)
            {
                int transformationCost = 0;
                bool canTransform = false;

                if (selectedPiece.type == ChessPieceType.Rook)
                {
                    transformationCost = golemTransformationCost;
                    canTransform = true;
                }
                else if (selectedPiece.type == ChessPieceType.Knight)
                {
                    transformationCost = kelpieTransformationCost;
                    canTransform = true;
                }
                else if (selectedPiece.type == ChessPieceType.Bishop)
                {
                    transformationCost = mantisTransformationCost;
                    canTransform = true;
                }

                if (!canTransform)
                {
                    Debug.Log($"Figur vom Typ {selectedPiece.type} kann nicht transformiert werden.");
                    selectedPieceForTransformation = null;
                    return;
                }

                // Überprüfe, ob der Spieler genug Mana hat
                if (GameManager.Instance.GetCurrentMana(GameManager.Instance.CurrentPlayer) >= transformationCost)
                {
                    PieceType transformedPiece = null;

                    if (selectedPiece.type == ChessPieceType.Rook)
                    {
                        transformedPiece = TransformRookToGolem(selectedPiece);
                        Debug.Log("Rook wurde in einen Golem transformiert");
                    }
                    else if (selectedPiece.type == ChessPieceType.Knight)
                    {
                        transformedPiece = TransformKnightToKelpie(selectedPiece);
                        Debug.Log("Knight wurde in einen Kelpie transformiert");
                    }
                    else if (selectedPiece.type == ChessPieceType.Bishop)
                    {
                        transformedPiece = TransformBishopToMantis(selectedPiece);
                        Debug.Log("Bishop wurde in einen Mantis transformiert");
                    }

                    // Mana abziehen
                    GameManager.Instance.UseMana(GameManager.Instance.CurrentPlayer, transformationCost);
                    hasTransformed = true;
                    lastMovedOrTransformedPiece = transformedPiece;
                }
                else
                {
                    Debug.Log("Nicht genug Mana für die Transformation!");
                }
            }
            else
            {
                Debug.Log("Du kannst nur deine eigenen Figuren transformieren");
                selectedPieceForTransformation = null;
            }
        }
        else
        {
            Debug.Log("Keine Figur für die Transformation ausgewählt");
        }
    }

    private void TriggerManaStorm(int player)
    {
        // Wähle eine zufällige Kachel
        Vector2Int randomTile;
        int attempts = 0;
        const int maxAttempts = 10;

        do
        {
            randomTile = new Vector2Int(
                UnityEngine.Random.Range(0, TILE_COUNT_X),
                UnityEngine.Random.Range(0, TILE_COUNT_Y)
            );
            attempts++;

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("Couldn't find non-king tile after 10 attempts!");
                GameManager.Instance.SetCurrentMana(player, 0);
                return;
            }
        }
        while (allChessPieces[randomTile.x, randomTile.y]?.type == ChessPieceType.King);

        // Deaktiviere die Kachel
        TileManager.Instance.DisableTile(randomTile, 2);


        // Blitz-Effekt
        if (lightningEffectPrefab)
        {
            Vector3 strikePosition = GetTileCenter(randomTile.x, randomTile.y) + Vector3.up * 1f;
            GameObject lightning = Instantiate(lightningEffectPrefab, strikePosition, Quaternion.identity);
            Destroy(lightning, lightningDuration);
        }

        // Überprüfe die Kachel auf Figuren
        PieceType piece = allChessPieces[randomTile.x, randomTile.y];
        if (piece != null)
        {
            Debug.Log($"{piece.type} (Team {piece.team}) was struck by lightning!");

            // Besondere Behandlung für Mana-Sturm-Opfer
            if (piece.type == ChessPieceType.King)
            {
                Debug.LogWarning("King was struck but shouldn't be defeated by mana storm!");
            }
            else
            {
                // Direkte Entfernung der Figur (ohne Mana-Belohnung)
                allChessPieces[randomTile.x, randomTile.y] = null;
                MoveToGraveyard(piece);

                // Optional: Spezialeffekte für bestimmte Figurentypen
                if (piece is Golem)
                {
                    GameManager.Instance.TriggerActiveCameraShake(0.7f, 0.2f);
                }
            }
        }

        // Setze Mana auf 0
        GameManager.Instance.SetCurrentMana(player, 0);
    }
    // In GenerateBoard.cs (oder einer separaten Logik-Klasse)


    public bool IsKingInCheck(int kingTeam, PieceType[,] boardState, int tileCountX, int tileCountY)
    {
        // 1. Finde die Position des zu überprüfenden Königs
        Vector2Int kingPosition = -Vector2Int.one;
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                if (boardState[x, y] != null &&
                    boardState[x, y].type == ChessPieceType.King &&
                    boardState[x, y].team == kingTeam)
                {
                    kingPosition = new Vector2Int(x, y);
                    break;
                }
            }
            if (kingPosition != -Vector2Int.one) break;
        }

        if (kingPosition == -Vector2Int.one)
        {
            // Debug.LogError($"Konnte König für Team {kingTeam} nicht auf dem Brett finden! (In IsKingInCheck)");
            return false; // Oder true, je nachdem wie man einen fehlenden König werten will (sollte nicht passieren)
        }

        // 2. Überprüfe alle gegnerischen Figuren
        int opponentTeam = 1 - kingTeam;
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                PieceType piece = boardState[x, y];
                if (piece != null && piece.team == opponentTeam)
                {
                    // Berechne die möglichen Züge dieser gegnerischen Figur
                    // WICHTIG: GetAvailableMoves braucht den aktuellen Brettzustand!
                    List<Vector2Int> opponentMoves = piece.GetAvailableMoves(ref boardState, tileCountX, tileCountY);

                    // Prüfe, ob einer dieser Züge auf die Position des Königs zeigt
                    if (ContainsValidMove(ref opponentMoves, kingPosition)) // ContainsValidMove prüft, ob kingPosition in opponentMoves ist
                    {
                        // Debug.Log($"König von Team {kingTeam} steht im Schach durch {piece.type} von Team {opponentTeam} auf ({x},{y}) welches ({kingPosition.x},{kingPosition.y}) angreift.");
                        return true; // König steht im Schach
                    }
                }
            }
        }
        return false; // König steht nicht im Schach
    }
}