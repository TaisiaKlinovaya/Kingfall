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
    // In BoardRelated/GenerateBoard.cs

    // In BoardRelated/GenerateBoard.cs

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
            if (!currentCamera)
            {
                Debug.LogError("Keine Kamera im Spiel gefunden oder zugewiesen!");
                return;
            }
        }

        // Hauptlogik, wenn das Spiel läuft
        if (GameManager.Instance.State == "GameRun")
        {
            // Kamerawechsel (unverändert)
            if (GameManager.Instance.CurrentPlayer == 1)
            {
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

            if (isBoardGenerated && currentCamera != null)
            {
                RaycastHit info;
                Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);

                // Mantis Fallenstellen per Maus (unverändert)
                if (currentMantisTrapState == MantisTrapState.AwaitingDirectionInput && mantisAwaitingTrapSetup != null)
                {
                    Vector2Int chosenDirection = Vector2Int.zero;
                    if (Input.GetMouseButtonDown(0)) { chosenDirection = Vector2Int.left; NotificationManager.Instance.ShowMessage("Mantis Trap Input: Westen (Linksklick)"); }
                    else if (Input.GetMouseButtonDown(1)) { chosenDirection = Vector2Int.right; NotificationManager.Instance.ShowMessage("Mantis Trap Input: Osten (Rechtsklick)"); }
                    else if (Input.GetAxis("Mouse ScrollWheel") > 0.05f) { chosenDirection = Vector2Int.up; NotificationManager.Instance.ShowMessage("Mantis Trap Input: Norden (Mausrad Vor)"); }
                    else if (Input.GetAxis("Mouse ScrollWheel") < -0.05f) { chosenDirection = Vector2Int.down; NotificationManager.Instance.ShowMessage("Mantis Trap Input: Süden (Mausrad Zurück)"); }

                    if (chosenDirection != Vector2Int.zero)
                    {
                        if (allChessPieces[mantisAwaitingTrapSetup.currentX, mantisAwaitingTrapSetup.currentY] == mantisAwaitingTrapSetup &&
                            mantisAwaitingTrapSetup.team == GameManager.Instance.CurrentPlayer - 1)
                        {
                            mantisAwaitingTrapSetup.SetupTrapZone(chosenDirection);
                            currentMantisTrapState = MantisTrapState.None;
                            mantisAwaitingTrapSetup = null;
                            NotificationManager.Instance.ShowMessage("Mantis-Falle erfolgreich gestellt. Zug kann jetzt beendet werden.");
                        }
                        else
                        {
                            Debug.LogWarning("Mantis für Fallenstellung war nicht mehr gültig. Modus wird zurückgesetzt.");
                            currentMantisTrapState = MantisTrapState.None;
                            mantisAwaitingTrapSetup = null;
                        }
                    }
                }
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
                        if (hitPosition != -Vector2Int.one)
                        {
                            tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                        }
                    }

                    // Linksklick-Logik (unverändert)
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (hitPosition == -Vector2Int.one) { if (currentlyDragging != null) { currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY)); currentlyDragging = null; RemoveHighlightTiles(); } }
                        else { if (currentlyDragging == null) { if (allChessPieces[hitPosition.x, hitPosition.y] != null) { PieceType clickedPiece = allChessPieces[hitPosition.x, hitPosition.y]; if (clickedPiece == lastMovedOrTransformedPiece && clickedPiece is Mantis mantisToSetup && clickedPiece.team == GameManager.Instance.CurrentPlayer - 1 && hasMoved && !hasTransformed) { currentMantisTrapState = MantisTrapState.AwaitingDirectionInput; mantisAwaitingTrapSetup = mantisToSetup; NotificationManager.Instance.ShowMessage($"Mantis bei ({clickedPiece.currentX},{clickedPiece.currentY}) FÜR FALLENSTELLUNG ausgewählt."); RemoveHighlightTiles(); } else if (clickedPiece.team == GameManager.Instance.CurrentPlayer - 1 && !hasMoved) { currentlyDragging = clickedPiece; NotificationManager.Instance.ShowMessage($"[GenerateBoard.Update] FIGUR AUSGEWÄHLT: {currentlyDragging.type} at ({currentlyDragging.currentX},{currentlyDragging.currentY}) für Bewegung."); availableMoves = currentlyDragging.GetAvailableMoves(ref allChessPieces, TILE_COUNT_X, TILE_COUNT_Y); HighlightTiles(); } } } else { Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY); bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y); if (!validMove) { currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y)); } currentlyDragging = null; RemoveHighlightTiles(); } }
                    }

                    // === GEÄNDERTE LOGIK FÜR RECHTSKLICK ===
                    if (Input.GetMouseButtonDown(1))
                    {
                        // Ignoriere Rechtsklick, wenn wir gerade eine Falle stellen wollen
                        if (currentMantisTrapState == MantisTrapState.AwaitingDirectionInput)
                        {
                            NotificationManager.Instance.ShowMessage("Mantis trap placement cancelled by right-click.");
                            currentMantisTrapState = MantisTrapState.None;
                            mantisAwaitingTrapSetup = null;
                        }
                        // Prüfe, ob auf ein gültiges Feld mit einer Figur geklickt wurde
                        else if (hitPosition != -Vector2Int.one && allChessPieces[hitPosition.x, hitPosition.y] != null)
                        {
                            PieceType clickedPiece = allChessPieces[hitPosition.x, hitPosition.y];

                            // Prüfe, ob es eine eigene Figur ist
                            if (clickedPiece.team == GameManager.Instance.CurrentPlayer - 1)
                            {
                                // --- ERWEITERTE PRÜFUNG: Wurde bereits transformiert? ---
                                if (hasTransformed)
                                {
                                    NotificationManager.Instance.ShowMessage("Transformation nicht möglich: Es wurde in diesem Zug bereits eine Figur transformiert.");
                                    clickedPiece.FlashColor(Color.red, 0.4f); // Negatives Feedback
                                    selectedPieceForTransformation = null; // Sicherstellen, dass nichts ausgewählt wird
                                }
                                // Prüfe, ob bereits eine Bewegung stattgefunden hat (wie vorher)
                                else if (hasMoved)
                                {
                                    bool isTransformable = (clickedPiece.type == ChessPieceType.Rook ||
                                                            clickedPiece.type == ChessPieceType.Knight ||
                                                            clickedPiece.type == ChessPieceType.Bishop);

                                    if (isTransformable)
                                    {
                                        selectedPieceForTransformation = clickedPiece;
                                        NotificationManager.Instance.ShowMessage($"{clickedPiece.type} ausgewählt für Transformation. (Bedingungen erfüllt)");
                                        clickedPiece.FlashColor(new Color(0.7f, 1f, 1f), 0.5f); // Positives Feedback
                                    }
                                    else
                                    {
                                        selectedPieceForTransformation = null;
                                        NotificationManager.Instance.ShowMessage($"{clickedPiece.type} ist nicht transformierbar.");
                                        clickedPiece.FlashColor(Color.yellow, 0.4f); // Warnung: Falscher Figurentyp
                                    }
                                }
                                else
                                {
                                    // Es wurde noch keine Figur bewegt
                                    selectedPieceForTransformation = null;
                                    NotificationManager.Instance.ShowMessage("Transformation nicht möglich: Es muss zuerst eine Figur bewegt werden.");
                                    clickedPiece.FlashColor(Color.red, 0.4f); // Negatives Feedback
                                }
                            }
                            else
                            {
                                // Klick auf gegnerische Figur
                                selectedPieceForTransformation = null;
                            }
                        }
                        else
                        {
                            // Klick ins Leere deselektiert eine eventuell bestehende Auswahl
                            if (selectedPieceForTransformation != null)
                            {
                                NotificationManager.Instance.ShowMessage("Transformations-Auswahl aufgehoben.");
                                selectedPieceForTransformation = null;
                            }
                        }
                    }
                    // === ENDE GEÄNDERTE LOGIK FÜR RECHTSKLICK ===

                }
                else // Kein Tile getroffen
                {
                    if (currentHover != -Vector2Int.one)
                    {
                        if (currentHover.x >= 0 && currentHover.x < TILE_COUNT_X && currentHover.y >= 0 && currentHover.y < TILE_COUNT_Y)
                        {
                            tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        }
                        currentHover = -Vector2Int.one;
                    }
                }

                // Figur-Dragging-Logik (unverändert)
                if (currentlyDragging)
                {
                    Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                    float distance = 0.0f;
                    if (horizontalPlane.Raycast(ray, out distance))
                    {
                        currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset, true);
                    }
                }
            }
        }
    }
    public void ResetMantisTrapMode()
    {
        if (currentMantisTrapState == MantisTrapState.AwaitingDirectionInput)
        {
            NotificationManager.Instance.ShowMessage("Mantis trap mode reset because turn ended before trap was set.");
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
            NotificationManager.Instance.ShowMessage("camera set to player2 in setCamera");
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
        // Null-Überprüfung für TileManager (wie bei dir)
        if (TileManager.Instance == null)
        {
            Debug.LogError("TileManager ist nicht initialisiert!");
            return false;
        }
        else
        {
            if (TileManager.Instance.IsTileDisabled(new Vector2Int(x, y)))
            {
                NotificationManager.Instance.ShowMessage($"Cannot move to disabled tile at ({x},{y})");
                return false;
            }
        }

        // Mantis-Fallenprüfung (wie bei dir)
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
                        Debug.LogWarning($"MANTIS TRAP TRIGGERED! Piece {cp.type} (Team {cp.team}) moving to ({x},{y}) stepped into Mantis (Team {opponentTeam}) trap originating from ({mantis.currentX},{mantis.currentY}).");
                        Vector2Int originalPosition = new Vector2Int(cp.currentX, cp.currentY);
                        ProcessDefeatedPiece(cp);
                        allChessPieces[originalPosition.x, originalPosition.y] = null;
                        mantis.ResetTrap();
                        hasMoved = true;
                        lastMovedOrTransformedPiece = null;
                        return true;
                    }
                }
            }
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Standard-Zugvalidierung
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            NotificationManager.Instance.ShowMessage($"Invalid Move for {cp.type}: Target ({x},{y}) is not in the list of available moves.");
            // --- NEU: Rotes Aufleuchten bei ungültigem Zug ---
            if (cp != null)
            {
                cp.FlashColor(Color.red, 0.4f); // Lasse die Figur kurz rot aufleuchten
            }
            return false;
        }

        // Simulation und Überprüfung auf Selbst-Schach
        PieceType[,] simulatedBoard = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];
        System.Array.Copy(allChessPieces, simulatedBoard, allChessPieces.Length);
        simulatedBoard[x, y] = cp;
        simulatedBoard[previousPosition.x, previousPosition.y] = null;

        if (IsKingInCheck(cp.team, simulatedBoard, TILE_COUNT_X, TILE_COUNT_Y))
        {
            NotificationManager.Instance.ShowMessage("Ungültiger Zug: Der eigene König stünde im Schach.", MessageType.Error);
            // --- NEU: Rotes Aufleuchten auch bei Selbst-Schach ---
            if (cp != null)
            {
                cp.FlashColor(Color.red, 0.6f); // Etwas länger, da es ein wichtigerer Fehler ist
            }
            return false;
        }

        // --- Rest der Methode (wie bei dir) ---
        PieceType targetPiece = allChessPieces[x, y];
        if (targetPiece != null)
        {
            if (targetPiece.team == cp.team)
            {
                NotificationManager.Instance.ShowMessage($"Invalid Move for {cp.type}: Cannot capture own piece ({targetPiece.type}) at ({x},{y}).");
                return false;
            }
            else
            {
                ProcessDefeatedPiece(targetPiece);
                if (targetPiece.type == ChessPieceType.King)
                {
                    isKingDead = true;
                    winTeam = (targetPiece.team == 1) ? "White" : "Black";
                    Debug.LogWarning($"KING CAPTURED! Team {winTeam} wins!");
                }
            }
        }

        if (cp.type == ChessPieceType.Golem)
        {
            Golem golem = cp as Golem;
            if (golem != null)
            {
                bool trampledAnyPieces = golem.DefeatFiguresOnPath(ref allChessPieces, previousPosition, new Vector2Int(x, y));
                if (trampledAnyPieces)
                {
                    NotificationManager.Instance.ShowMessage("Golem trampled pieces, triggering camera shake.");
                    GameManager.Instance.TriggerActiveCameraShake(0.6f, 0.15f);
                }
                foreach (var defeatedPieceInPath in golem.DefeatedPieces)
                {
                    if (defeatedPieceInPath.type == ChessPieceType.King)
                    {
                        isKingDead = true;
                        winTeam = (defeatedPieceInPath.team == 1) ? "White" : "Black";
                        Debug.LogWarning($"KING TRAMPLED BY GOLEM! Team {winTeam} wins!");
                        break;
                    }
                }
            }
        }

        allChessPieces[x, y] = cp;
        allChessPieces[previousPosition.x, previousPosition.y] = null;
        positionSinglePiece(x, y);
        lastMovedOrTransformedPiece = cp;
        hasMoved = true;
        return true;
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
    //                    // NotificationManager.Instance.ShowMessage($"König von Team {kingTeam} auf ({kingPosition.x},{kingPosition.y}) steht im Schach durch {piece.type} von Team {attackerTeam} auf ({r},{c}).");
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
            NotificationManager.Instance.ShowMessage($"Mana overflow! Triggering Mana Storm.");
            TriggerManaStorm(currentPlayer);
        }
        else
        {
            GameManager.Instance.SetCurrentMana(currentPlayer, newMana);
            NotificationManager.Instance.ShowMessage($"Player {currentPlayer} gained {RegenManaAmount} mana (now: {newMana}/10)");
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
        NotificationManager.Instance.ShowMessage($"Rook (Team {team}) transformed into Golem at ({x}, {y}).");
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
        NotificationManager.Instance.ShowMessage($"Knight (Team {team}) transformed into Kelpie at ({x}, {y}).");
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
        NotificationManager.Instance.ShowMessage($"Bishop transformed into Mantis at ({x}, {y}).");
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

    // In BoardRelated/GenerateBoard.cs

    // In BoardRelated/GenerateBoard.cs

    public void TransformPiece()
    {
        // Vorabprüfung 1: Hat sich schon eine Figur bewegt?
        if (!hasMoved)
        {
            NotificationManager.Instance.ShowMessage("Du musst zuerst eine Figur bewegen.", MessageType.Warning);
            return;
        }

        // Vorabprüfung 2: Wurde in diesem Zug bereits transformiert?
        if (hasTransformed)
        {
            NotificationManager.Instance.ShowMessage("Es wurde in diesem Zug bereits transformiert.", MessageType.Error);
            return;
        }

        PieceType selectedPiece = GetSelectedPieceForTransformation();
        if (selectedPiece == null)
        {
            NotificationManager.Instance.ShowMessage("Keine Figur für die Transformation ausgewählt.", MessageType.Info);
            return;
        }

        // Vorabprüfung 3: Gehört die Figur dem aktuellen Spieler?
        if (selectedPiece.team != GameManager.Instance.CurrentPlayer - 1)
        {
            NotificationManager.Instance.ShowMessage("Du kannst nur deine eigenen Figuren transformieren.", MessageType.Error);
            selectedPieceForTransformation = null;
            return;
        }

        // Kosten und Typ der Transformation bestimmen
        int transformationCost = 0;
        ChessPieceType targetType = ChessPieceType.None;

        switch (selectedPiece.type)
        {
            case ChessPieceType.Rook:
                transformationCost = golemTransformationCost;
                targetType = ChessPieceType.Golem;
                break;
            case ChessPieceType.Knight:
                transformationCost = kelpieTransformationCost;
                targetType = ChessPieceType.Kelpie;
                break;
            case ChessPieceType.Bishop:
                transformationCost = mantisTransformationCost;
                targetType = ChessPieceType.Mantis;
                break;
            default:
                // Sollte durch die Rechtsklick-Logik eigentlich nicht passieren, aber als Absicherung
                NotificationManager.Instance.ShowMessage($"Figur vom Typ {selectedPiece.type} kann nicht transformiert werden.", MessageType.Warning);
                selectedPieceForTransformation = null;
                return;
        }

        // --- ZENTRALE MANA-PRÜFUNG ---
        if (GameManager.Instance.GetCurrentMana(GameManager.Instance.CurrentPlayer) >= transformationCost)
        {
            // Genug Mana: Transformation durchführen
            PieceType transformedPiece = null;
            switch (targetType)
            {
                case ChessPieceType.Golem:
                    transformedPiece = TransformRookToGolem(selectedPiece);
                    break;
                case ChessPieceType.Kelpie:
                    transformedPiece = TransformKnightToKelpie(selectedPiece);
                    break;
                case ChessPieceType.Mantis:
                    transformedPiece = TransformBishopToMantis(selectedPiece);
                    break;
            }

            if (transformedPiece != null) // Nur wenn die Transformation im Backend erfolgreich war
            {
                // Mana abziehen
                GameManager.Instance.UseMana(GameManager.Instance.CurrentPlayer, transformationCost);
                hasTransformed = true; // Aktion für diesen Zug als "verbraucht" markieren
                lastMovedOrTransformedPiece = transformedPiece; // Tracker für Spezialfähigkeiten (z.B. Mantis)
                NotificationManager.Instance.ShowMessage($"{selectedPiece.type} wurde zu {transformedPiece.type} transformiert!", MessageType.Info);
            }
            else
            {
                // Dieser Fall sollte selten auftreten, deutet auf einen Fehler in den Transform... Methoden hin
                NotificationManager.Instance.ShowMessage("Transformation fehlgeschlagen!", MessageType.Error);
            }
        }
        else
        {
            // Nicht genug Mana
            string message = $"Nicht genug Mana! Benötigt: {transformationCost}, Vorhanden: {GameManager.Instance.GetCurrentMana(GameManager.Instance.CurrentPlayer)}";
            NotificationManager.Instance.ShowMessage(message, MessageType.Error);
            selectedPiece.FlashColor(Color.magenta, 0.5f); // Visuelles Feedback
            selectedPieceForTransformation = null; // Auswahl zurücksetzen
        }
    }

    public void CheckForCheckmateOrStalemate(int teamToCheck)
    {
        // Das Team wird vom GameManager übergeben (1 für Spieler 1, 2 für Spieler 2)
        // Wir brauchen den Index 0 oder 1.
        int teamIndex = teamToCheck - 1;

        // Generiere alle überhaupt möglichen legalen Züge für dieses Team
        List<Vector2Int> legalMoves = GenerateAllLegalMovesForTeam(teamIndex);

        // Wenn die Liste der legalen Züge leer ist, ist das Spiel vorbei.
        if (legalMoves.Count == 0)
        {
            // Prüfe, ob der König des Teams aktuell im Schach steht.
            if (IsKingInCheck(teamIndex, allChessPieces, TILE_COUNT_X, TILE_COUNT_Y))
            {
                // SCHACHMATT: Keine legalen Züge und König steht im Schach.
                int winnerTeamNumber = 1 - teamIndex; // Das andere Team (0 oder 1) hat gewonnen.
                string winnerTeamName = (winnerTeamNumber == 0) ? "White" : "Black";

                NotificationManager.Instance.ShowMessage($"Schachmatt! Team {winnerTeamName} gewinnt!", MessageType.Info);
                GameManager.Instance.WinGame(winnerTeamName);
            }
            else
            {
                // PATT: Keine legalen Züge und König steht NICHT im Schach.
                NotificationManager.Instance.ShowMessage("Patt! Das Spiel ist unentschieden.", MessageType.Info);
                GameManager.Instance.WinGame("Patt"); // Dein GameManager muss "Patt" im Text verarbeiten können.
            }
        }
        // Wenn es noch legale Züge gibt, geht das Spiel einfach weiter.
    }

    // In BoardRelated/GenerateBoard.cs
    public PieceType[,] GetAllChessPieces()
    {
        return allChessPieces;
    }

    public List<Vector2Int> GenerateAllLegalMovesForTeam(int team)
    {
        List<Vector2Int> allLegalMoves = new List<Vector2Int>();

        // Gehe durch jede Figur des Teams
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                PieceType piece = allChessPieces[x, y];
                if (piece != null && piece.team == team)
                {
                    // Hole alle theoretisch möglichen Züge dieser Figur
                    List<Vector2Int> pseudoLegalMoves = piece.GetAvailableMoves(ref allChessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                    // Filtere jeden dieser Züge
                    foreach (Vector2Int move in pseudoLegalMoves)
                    {
                        // Simuliere den Zug
                        PieceType[,] simulatedBoard = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];
                        System.Array.Copy(allChessPieces, simulatedBoard, allChessPieces.Length);
                        simulatedBoard[move.x, move.y] = piece;
                        simulatedBoard[x, y] = null;

                        // Prüfe, ob der eigene König nach diesem Zug im Schach stünde
                        if (!IsKingInCheck(team, simulatedBoard, TILE_COUNT_X, TILE_COUNT_Y))
                        {
                            // Nur wenn nicht im Schach, ist es ein wirklich legaler Zug
                            // Wir brauchen diese Liste hier nicht direkt, aber die Erkenntnis,
                            // dass es mindestens einen legalen Zug gibt, ist wichtig.
                            // Für die aktuelle Logik, die nur die Züge EINER Figur anzeigt, ist das hier
                            // die Grundlage für die Schachmatt-Prüfung.
                            // Wir fügen den Zug einer imaginären Gesamtliste hinzu.
                            allLegalMoves.Add(move);
                        }
                    }
                }
            }
        }
        return allLegalMoves;
    }
    /// <summary>
    ///**NEU** Transformationslogik anhand der spezifischen Karten angepasst ***NEU***
    /// <summary>
    public void TryCardTransformation(ChessPieceType requiredType, ChessPieceType targetType)
    {
        // Überprüfe Basisvoraussetzungen
        if (hasTransformed)
        {
            NotificationManager.Instance.ShowMessage("Nur eine Transformation pro Zug erlaubt!");
            return;
        }

        PieceType selectedPiece = GetSelectedPieceForTransformation();

        if (selectedPiece == null)
        {
            NotificationManager.Instance.ShowMessage("Keine Figur für Transformation ausgewählt!");
            return;
        }

        // Prüfe, ob die ausgewählte Figur dem richtigen Typ entspricht
        if (selectedPiece.type != requiredType)
        {
            NotificationManager.Instance.ShowMessage($"Falsche Figur! Diese Karte benötigt {requiredType}, aber {selectedPiece.type} ist ausgewählt");
            return;
        }

        // Bestimme Manakosten
        int cost = targetType switch
        {
            ChessPieceType.Golem => golemTransformationCost,
            ChessPieceType.Kelpie => kelpieTransformationCost,
            ChessPieceType.Mantis => mantisTransformationCost,
            _ => 0
        };

        // Mana-Prüfung
        if (GameManager.Instance.GetCurrentMana(GameManager.Instance.CurrentPlayer) < cost)
        {
            NotificationManager.Instance.ShowMessage("Nicht genug Mana für diese Transformation!");
            return;
        }

        // Führe Transformation durch
        PieceType transformedPiece = targetType switch
        {
            ChessPieceType.Golem when selectedPiece.type == ChessPieceType.Rook => TransformRookToGolem(selectedPiece),
            ChessPieceType.Kelpie when selectedPiece.type == ChessPieceType.Knight => TransformKnightToKelpie(selectedPiece),
            ChessPieceType.Mantis when selectedPiece.type == ChessPieceType.Bishop => TransformBishopToMantis(selectedPiece),
            _ => null
        };

        if (transformedPiece != null)
        {
            GameManager.Instance.UseMana(GameManager.Instance.CurrentPlayer, cost);
            hasTransformed = true;
            lastMovedOrTransformedPiece = transformedPiece;
            NotificationManager.Instance.ShowMessage($"Erfolgreich transformiert zu {targetType}!");
        }
        else
        {
            Debug.LogError("Transformation fehlgeschlagen - Typen stimmen nicht überein");
        }
    }


    /// <summary>
    /// ***NEU*** Angepasste Methode, der ManaSturm sollte nun auch ausgelöst werden wenn die Zeit auf 0 fällt.
    /// </summary>
    public void TriggerManaStorm(int player)
    {
        if (isManaStormActive) return; // Verhindere mehrfache Auslösung

        isManaStormActive = true;
        Debug.Log($"Mana-Sturm wird ausgelöst für Spieler {player}!");

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
                isManaStormActive = false;
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

            if (piece.type == ChessPieceType.King)
            {
                Debug.LogWarning("King was struck but shouldn't be defeated by mana storm!");
            }
            else
            {
                allChessPieces[randomTile.x, randomTile.y] = null;
                ProcessDefeatedPiece(piece, true); // true = fromManaStorm
            }
        }

        // Setze Mana auf 0
        GameManager.Instance.SetCurrentMana(player, 0);
        isManaStormActive = false;
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
                        // NotificationManager.Instance.ShowMessage($"König von Team {kingTeam} steht im Schach durch {piece.type} von Team {opponentTeam} auf ({x},{y}) welches ({kingPosition.x},{kingPosition.y}) angreift.");
                        return true; // König steht im Schach
                    }
                }
            }
        }
        return false; // König steht nicht im Schach
    }
}