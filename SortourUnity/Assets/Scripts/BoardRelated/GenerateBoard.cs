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

    private void Update()
    {
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
        }

        if (isKingDead == true)
        {
            GameManager.Instance.WinGame(winTeam);
            isKingDead = false;
        }

        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }

        if (GameManager.Instance.State == "GameRun")
        {
            if (GameManager.Instance.CurrentPlayer == 1)
            {
                currentCamera = GameObject.Find("Player1Camera").GetComponent<Camera>();
            }
            if (GameManager.Instance.CurrentPlayer == 2)
            {
                currentCamera = GameObject.Find("Player2Camera").GetComponent<Camera>();
            }

            if (isBoardGenerated)
            {
                RaycastHit info;
                Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
                {
                    Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

                    if (currentHover == -Vector2Int.one)
                    {
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }

                    if (currentHover != hitPosition)
                    {
                        tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }

                    if (Input.GetMouseButtonDown(0))
                    {
                        if (currentlyDragging == null)
                        {
                            if (allChessPieces[hitPosition.x, hitPosition.y] != null)
                            {
                                // Check if the piece belongs to the current player and no move has been made yet
                                if (allChessPieces[hitPosition.x, hitPosition.y].team == GameManager.Instance.CurrentPlayer - 1 && !hasMoved)
                                {
                                    currentlyDragging = allChessPieces[hitPosition.x, hitPosition.y];

                                    if (currentlyDragging != null)
                                    {
                                        // NEUES LOG 1: Welche Figur wurde ausgewählt?
                                        //Debug.Log($"[GenerateBoard.Update] Selected piece: {currentlyDragging.type} at ({currentlyDragging.currentX},{currentlyDragging.currentY})");

                                        // Get available moves
                                        availableMoves = currentlyDragging.GetAvailableMoves(ref allChessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                                        // NEUES LOG 2: Wie viele Züge wurden von GetAvailableMoves zurückgegeben?s

                                        HighlightTiles(); // Highlight the moves
                                    }
                                }
                            }
                        }
                        else
                        {
                            Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                            bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                            if (!validMove)
                            {
                                currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                            }
                            else
                            {
                                hasMoved = true; // Setze das Flag, dass eine Figur bewegt wurde
                            }

                            currentlyDragging = null;
                            RemoveHighlightTiles();
                        }
                    }

                    // In BoardRelated/GenerateBoard.cs -> Update() -> if (Input.GetMouseButtonDown(1))

                    if (Input.GetMouseButtonDown(1))
                    {
                        if (allChessPieces[hitPosition.x, hitPosition.y] != null)
                        {
                            PieceType clickedPiece = allChessPieces[hitPosition.x, hitPosition.y];

                            // Nur die eigenen Figuren können ausgewählt werden
                            if (clickedPiece.team == GameManager.Instance.CurrentPlayer - 1)
                            {
                                if (clickedPiece.type == ChessPieceType.Rook)
                                {
                                    selectedPieceForTransformation = clickedPiece;
                                    Debug.Log("Rook selected for transformation.");
                                }
                                else if (clickedPiece.type == ChessPieceType.Knight)
                                {
                                    selectedPieceForTransformation = clickedPiece;
                                    Debug.Log("Knight selected for transformation.");
                                }
                                // NEU: Bishop für Mantis Transformation auswählen
                                else if (clickedPiece.type == ChessPieceType.Bishop)
                                {
                                    selectedPieceForTransformation = clickedPiece;
                                    Debug.Log("Bishop selected for transformation.");
                                }
                                else
                                {
                                    // Optional: Hinweis, dass diese Figur nicht transformierbar ist
                                    selectedPieceForTransformation = null;
                                    Debug.Log($"{clickedPiece.type} cannot be transformed.");
                                }
                            }
                            else
                            {
                                selectedPieceForTransformation = null; // Auswahl zurücksetzen, wenn es nicht die eigene Figur ist
                            }
                        }
                        else
                        {
                            selectedPieceForTransformation = null; // Auswahl zurücksetzen, wenn leeres Feld geklickt wird
                        }
                    }
                }
                else
                {
                    if (currentHover != -Vector2Int.one)
                    {
                        tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        currentHover = -Vector2Int.one;
                    }
                }

                if (currentlyDragging)
                {
                    Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                    float distance = 0.0f;
                    if (horizontalPlane.Raycast(ray, out distance))
                    {
                        currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
                    }
                }
            }
        }
        // --- Mantis-Fallen-Richtungswahl (wenn letzte Figur eine Mantis war UND noch keine Richtung gewählt wurde) ---
        if (!mantisTrapDirectionChosenThisTurn && // Nur abfragen, wenn noch nicht gewählt
            lastMovedOrTransformedPiece != null &&
            lastMovedOrTransformedPiece is Mantis currentMantis)
        {
            // Check if the Mantis still exists at its location (sanity check)
            if (allChessPieces[currentMantis.currentX, currentMantis.currentY] == currentMantis)
            {
                Vector2Int chosenDirection = Vector2Int.zero;

                // === Abfrage mit WASD ===
                if (Input.GetKeyDown(KeyCode.W)) chosenDirection = Vector2Int.up;    // Norden
                if (Input.GetKeyDown(KeyCode.D)) chosenDirection = Vector2Int.right; // Osten
                if (Input.GetKeyDown(KeyCode.S)) chosenDirection = Vector2Int.down;  // Süden
                if (Input.GetKeyDown(KeyCode.A)) chosenDirection = Vector2Int.left;  // Westen

                if (chosenDirection != Vector2Int.zero)
                {
                    currentMantis.SetupTrapZone(chosenDirection);
                    mantisTrapDirectionChosenThisTurn = true; // Richtung wurde gewählt!
                    Debug.Log($"Mantis Trap direction {chosenDirection} chosen via WASD. Press 'Finished' or wait for timer.");
                    // WICHTIG: lastMovedOrTransformedPiece NICHT zurücksetzen,
                    //          damit wir wissen, dass eine Mantis die letzte war.
                    //          Das Zurücksetzen erfolgt erst in MoveFinished.
                }
            }
            else // Mantis ist nicht mehr da? Setze den Tracker zurück.
            {
                lastMovedOrTransformedPiece = null;
                mantisTrapDirectionChosenThisTurn = false;
            }
        }
        // --- Ende Mantis-Fallen-Richtungswahl ---
    }
    // --- NEUE Helper-Methode in GenerateBoard.cs HINZUFÜGEN ---
    // Füge dies zu BoardRelated/GenerateBoard.cs hinzu:
    public void ResetLastMovedPieceAndTrapChoice()
    {
        lastMovedOrTransformedPiece = null;
        mantisTrapDirectionChosenThisTurn = false; // Wichtig: Hier zurücksetzen!
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

    private bool MoveTo(PieceType cp, int x, int y)
    {
        // Null-Überprüfung für TileManager
        if (TileManager.Instance == null)
        {
            Debug.LogError("TileManager ist nicht initialisiert!");
            return false;
        }
        else
        {
            // Überprüfe auf deaktivierte Kacheln
            if (TileManager.Instance.IsTileDisabled(new Vector2Int(x, y)))
            {
                Debug.Log($"Cannot move to disabled tile at ({x},{y})");
                return false;
            }
        }



        // --- Check for opponent Mantis traps at the target destination ---
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
        // --- End of Mantis Trap Check ---

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            Debug.Log($"Invalid Move for {cp.type}: Target ({x},{y}) is not in the list of available moves.");
            return false;
        }

        PieceType targetPiece = allChessPieces[x, y];
        if (targetPiece != null)
        {
            if (targetPiece.team == cp.team)
            {
                Debug.Log($"Invalid Move for {cp.type}: Cannot capture own piece ({targetPiece.type}) at ({x},{y}).");
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

        // --- Handle Special Piece Logic (Golem Trample) ---
        if (cp.type == ChessPieceType.Golem)
        {
            Golem golem = cp as Golem;
            if (golem != null)
            {
                // Rufe DefeatFiguresOnPath NUR EINMAL auf und speichere das Ergebnis
                bool trampledAnyPieces = golem.DefeatFiguresOnPath(ref allChessPieces, previousPosition, new Vector2Int(x, y));

                // Löse Kamera-Shake aus, BASIEREND AUF DEM ERGEBNIS DES ERSTEN AUFRUFS
                if (trampledAnyPieces)
                {
                    Debug.Log("Golem trampled pieces, triggering camera shake.");
                    GameManager.Instance.TriggerActiveCameraShake(0.6f, 0.15f); // Du kannst Dauer/Stärke anpassen
                }

                // Überprüfe, ob der König durch das Trampeln besiegt wurde
                // Diese Liste (golem.DefeatedPieces) wird durch den obigen Aufruf von DefeatFiguresOnPath gefüllt
                foreach (var defeatedPieceInPath in golem.DefeatedPieces)
                {
                    if (defeatedPieceInPath.type == ChessPieceType.King)
                    {
                        isKingDead = true;
                        winTeam = (defeatedPieceInPath.team == 1) ? "White" : "Black"; // Bestimme Gewinner basierend auf Team der besiegten Königsfigur
                        Debug.LogWarning($"KING TRAMPLED BY GOLEM! Team {winTeam} wins!");
                        break; // König gefunden, Schleife kann beendet werden
                    }
                }
            }
        }


        // --- Finalize the Move ---
        allChessPieces[x, y] = cp;
        allChessPieces[previousPosition.x, previousPosition.y] = null;

        positionSinglePiece(x, y);

        lastMovedOrTransformedPiece = cp;
        hasMoved = true;

        //Debug.Log($"Piece {cp.GetType().Name} (Team {(cp.team == 0 ? "White" : "Black")}) moved from ({previousPosition.x}, {previousPosition.y}) to ({x}, {y}).");

        return true;
    }
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

}