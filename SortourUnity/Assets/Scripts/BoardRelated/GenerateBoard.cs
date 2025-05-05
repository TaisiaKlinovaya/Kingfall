using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using Unity.VisualScripting;
#endif
using UnityEngine;

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

    [Header("Transformation Costs")]
    public int golemTransformationCost = 5;
    public int kelpieTransformationCost = 5;
    public int mantisTransformationCost = 4; // Beispielwert, passe ihn nach Bedarf an


    private PieceType lastMovedOrTransformedPiece = null;
    private bool mantisTrapDirectionChosenThisTurn = false;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        allChessPieces = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];

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
                                        Debug.Log($"[GenerateBoard.Update] Selected piece: {currentlyDragging.type} at ({currentlyDragging.currentX},{currentlyDragging.currentY})");

                                        // Get available moves
                                        availableMoves = currentlyDragging.GetAvailableMoves(ref allChessPieces, TILE_COUNT_X, TILE_COUNT_Y);

                                        // NEUES LOG 2: Wie viele Züge wurden von GetAvailableMoves zurückgegeben?
                                        Debug.Log($"[GenerateBoard.Update] Received {availableMoves.Count} moves from GetAvailableMoves.");

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
    private bool MoveTo(PieceType cp, int x, int y)
    {
        // --- Check for opponent Mantis traps at the target destination ---
        int opponentTeam = 1 - cp.team; // Determine the opponent's team index (0 or 1)
                                        // Iterate through all board tiles to find opponent Mantis pieces
        for (int mx = 0; mx < TILE_COUNT_X; mx++)
        {
            for (int my = 0; my < TILE_COUNT_Y; my++)
            {
                PieceType potentialMantis = allChessPieces[mx, my];
                // Check if there's a piece, it belongs to the opponent, it's a Mantis, and its trap is set
                if (potentialMantis != null &&
                    potentialMantis.team == opponentTeam &&
                    potentialMantis is Mantis mantis && // Use 'is' pattern matching for type check and cast
                    mantis.IsTrapActive())
                {
                    // Check if the target coordinates (x, y) of the current move fall within this Mantis's trap zone
                    if (mantis.GetTrapZone().Contains(new Vector2Int(x, y)))
                    {
                        Debug.LogWarning($"MANTIS TRAP TRIGGERED! Piece {cp.type} (Team {cp.team}) moving to ({x},{y}) stepped into Mantis (Team {opponentTeam}) trap originating from ({mantis.currentX},{mantis.currentY}).");

                        Vector2Int originalPosition = new Vector2Int(cp.currentX, cp.currentY); // Store original pos before removing

                        // Process the piece that stepped into the trap as defeated (handles visuals, dead list, mana gain)
                        ProcessDefeatedPiece(cp);

                        // Remove the trapped piece from its starting position on the logical board
                        // It never reaches the destination (x, y)
                        allChessPieces[originalPosition.x, originalPosition.y] = null;

                        // Reset the trap of the Mantis that caught the piece
                        mantis.ResetTrap();

                        // Mark the turn flags as if a move happened, consuming the action
                        hasMoved = true;
                        lastMovedOrTransformedPiece = null; // No piece successfully completed the move

                        // Return true because an action occurred and the turn should proceed (Highlights removed, player switched)
                        return true;
                    }
                }
            }
        }
        // --- End of Mantis Trap Check ---


        // --- Standard Move Validation ---
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Check if the target tile (x, y) is among the available moves calculated earlier
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            // If not a valid destination based on piece movement rules
            Debug.Log($"Invalid Move for {cp.type}: Target ({x},{y}) is not in the list of available moves.");
            // The piece visually snaps back in the Update loop if dragging was involved
            return false; // Indicate the move was illegal/impossible
        }


        // --- Handle Capturing ---
        PieceType targetPiece = allChessPieces[x, y];
        if (targetPiece != null) // Is the destination tile occupied?
        {
            // Check if the occupying piece is an ally
            if (targetPiece.team == cp.team)
            {
                Debug.Log($"Invalid Move for {cp.type}: Cannot capture own piece ({targetPiece.type}) at ({x},{y}).");
                // The piece visually snaps back
                return false; // Indicate illegal move
            }
            else
            {
                // It's an enemy piece, capture it
                Debug.Log($"{cp.type} (Team {cp.team}) captures {targetPiece.type} (Team {targetPiece.team}) at ({x},{y}).");
                ProcessDefeatedPiece(targetPiece); // Handle visual removal, dead list, mana gain

                // Check if the captured piece was a King (Game Over condition)
                if (targetPiece.type == ChessPieceType.King)
                {
                    isKingDead = true; // Flag for game over check in Update
                    winTeam = (targetPiece.team == 1) ? "White" : "Black"; // Determine winner
                    Debug.LogWarning($"KING CAPTURED! Team {winTeam} wins!");
                }
                // The target square will be logically overwritten by the moving piece later
            }
        }

        // --- Handle Special Piece Logic (Golem Trample) ---
        if (cp.type == ChessPieceType.Golem)
        {
            // If the moving piece is a Golem, execute its trample effect
            Golem golem = cp as Golem; // Safe cast
            if (golem != null)
            {
                // Call the method to defeat pieces along the path (excluding the Golem itself and the final target square piece which was already handled)
                golem.DefeatFiguresOnPath(ref allChessPieces, previousPosition, new Vector2Int(x, y));

                // Check if the Golem's trample defeated a King
                foreach (var defeatedPiece in golem.DefeatedPieces) // Check list populated by DefeatFiguresOnPath
                {
                    if (defeatedPiece.type == ChessPieceType.King)
                    {
                        isKingDead = true;
                        winTeam = (defeatedPiece.team == 1) ? "White" : "Black";
                        Debug.LogWarning($"KING TRAMPLED BY GOLEM! Team {winTeam} wins!");
                        break; // No need to check further
                    }
                }
            }
        }


        // --- Finalize the Move ---
        // Update the logical board state:
        allChessPieces[x, y] = cp; // Place the moving piece at the destination
        allChessPieces[previousPosition.x, previousPosition.y] = null; // Clear the starting square

        // Update the piece's internal coordinates
        // (positionSinglePiece updates currentX/currentY and handles visual positioning)
        positionSinglePiece(x, y); // Use force=false for smooth Lerp movement

        // Track the piece that just completed its move (for potential Mantis trap setup)
        lastMovedOrTransformedPiece = cp;

        // Set flag indicating a piece has moved this turn
        hasMoved = true;

        Debug.Log($"Piece {cp.GetType().Name} (Team {(cp.team == 0 ? "White" : "Black")}) moved from ({previousPosition.x}, {previousPosition.y}) to ({x}, {y}).");

        // If a Mantis moved, its trap should be reset (handled in Mantis.GetAvailableMoves now)

        return true; // Move was successful
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

    public void ProcessDefeatedPiece(PieceType defeatedPiece)
    {
        if (defeatedPiece == null)
        {
            return;
        }

        // Überprüfen, ob die besiegte Figur eine Transformationsfigur ist
        bool isTransformationPiece = defeatedPiece.type == ChessPieceType.Golem || defeatedPiece.type == ChessPieceType.Kelpie;

        if (defeatedPiece.team == 0) // Weiße Figur
        {
            if (isTransformationPiece)
            {
                // Transformationsfigur: Platziere sie in einer separaten Reihe
                deadWhiteTransformations.Add(defeatedPiece);
                defeatedPiece.SetScale(Vector3.one * deathSize);

                // Positionierung der besiegten weißen Transformationsfiguren
                Vector3 deathPosition = new Vector3(
                    9 * tileSize, // Eine Spalte weiter rechts als die normalen besiegten Figuren
                    yOffset - 0.23f,
                    -1 * tileSize
                ) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * deathSpacing * deadWhiteTransformations.Count);

                defeatedPiece.SetPosition(deathPosition);
            }
            else
            {
                // Normale Figur: Platziere sie in der normalen Reihe
                deadWhites.Add(defeatedPiece);
                defeatedPiece.SetScale(Vector3.one * deathSize);

                // Positionierung der besiegten weißen Figuren
                Vector3 deathPosition = new Vector3(
                    8 * tileSize,
                    yOffset - 0.23f,
                    -1 * tileSize
                ) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.forward * deathSpacing * deadWhites.Count);

                defeatedPiece.SetPosition(deathPosition);
            }
        }
        else // Schwarze Figur
        {
            if (isTransformationPiece)
            {
                deadBlackTransformations.Add(defeatedPiece);
                defeatedPiece.SetScale(Vector3.one * deathSize);

                // Positionierung der besiegten schwarzen Transformationsfiguren
                Vector3 deathPosition = new Vector3(
                    -2 * tileSize, // Eine Spalte weiter links als die normalen besiegten Figuren
                    yOffset - 0.23f,
                    8 * tileSize
                ) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.back * deathSpacing * deadBlackTransformations.Count);

                defeatedPiece.SetPosition(deathPosition);
            }
            else
            {
                // Normale Figur: Platziere sie in der normalen Reihe
                deadBlacks.Add(defeatedPiece);
                defeatedPiece.SetScale(Vector3.one * deathSize);

                // Positionierung der besiegten schwarzen Figuren
                Vector3 deathPosition = new Vector3(
                    -1 * tileSize,
                    yOffset - 0.23f,
                    8 * tileSize
                ) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2) + (Vector3.back * deathSpacing * deadBlacks.Count);

                defeatedPiece.SetPosition(deathPosition);
            }
        }

        int currentPlayer = GameManager.Instance.CurrentPlayer;

        // Überprüfen, ob die besiegte Figur dem gegnerischen Team angehört
        if (defeatedPiece.team != currentPlayer - 1)
        {
            GameManager.Instance.SetCurrentMana(currentPlayer, GameManager.Instance.GetCurrentMana(currentPlayer) + RegenManaAmount);
            Debug.Log($"Spieler {currentPlayer} hat {RegenManaAmount} Mana regeneriert, nachdem eine gegnerische Figur besiegt wurde.");
        }

        Debug.Log($"Figur {defeatedPiece.GetType().Name} (Team {(defeatedPiece.team == 0 ? "Weiß" : "Schwarz")}) wurde besiegt.");
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

    private void KillRandomOpponentPiece(int player)
    {
        // Bestimme das gegnerische Team
        int opponentTeam = (player == 1) ? 1 : 0; // Spieler 1 (Team 0) vs. Spieler 2 (Team 1)

        // Sammle alle Figuren des gegnerischen Teams (außer König)
        List<PieceType> opponentPieces = new List<PieceType>();
        for (int x = 0; x < GenerateBoard.TILE_COUNT_X; x++)
        {
            for (int y = 0; y < GenerateBoard.TILE_COUNT_Y; y++)
            {
                PieceType piece = GenerateBoard.Instance.allChessPieces[x, y];
                if (piece != null && piece.team == opponentTeam && piece.type != ChessPieceType.King)
                {
                    opponentPieces.Add(piece);
                }
            }
        }

        // Wenn es Figuren gibt, wähle eine zufällige aus und töte sie
        if (opponentPieces.Count > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, opponentPieces.Count);
            PieceType randomPiece = opponentPieces[randomIndex];

            // Töte die zufällige Figur
            GenerateBoard.Instance.ProcessDefeatedPiece(randomPiece);
            Debug.Log($"Zufällige gegnerische Figur ({randomPiece.type}) wurde getötet.");
        }
        else
        {
            Debug.Log("Keine gegnerischen Figuren (außer König) verfügbar, die getötet werden könnten.");
        }
    }
    // In BoardRelated/GenerateBoard.cs

    public void TransformPiece()
    {
        if (hasTransformed)
        {
            Debug.Log("Du kannst nur eine Figur pro Zug transformieren.");
            return;
        }
        // NEU: Variable für letzte bewegte/transformierte Figur hinzufügen (ganz oben in GenerateBoard.cs)
        // private PieceType lastMovedOrTransformedPiece = null;
        // (Diese Variable wird später in MoveTo und den Transform-Methoden gesetzt)


        PieceType selectedPiece = GetSelectedPieceForTransformation(); // Diese Methode existiert bereits
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
                // NEU: Bishop prüfen
                else if (selectedPiece.type == ChessPieceType.Bishop)
                {
                    transformationCost = mantisTransformationCost;
                    canTransform = true;
                }

                if (!canTransform)
                {
                    Debug.Log($"Figur vom Typ {selectedPiece.type} kann nicht transformiert werden.");
                    selectedPieceForTransformation = null; // Auswahl zurücksetzen
                    return;
                }


                // Überprüfe, ob der Spieler genug Mana hat
                if (GameManager.Instance.GetCurrentMana(GameManager.Instance.CurrentPlayer) >= transformationCost)
                {
                    // Führe die Transformation durch
                    PieceType transformedPiece = null; // NEU: Um die neue Figur zu speichern

                    if (selectedPiece.type == ChessPieceType.Rook)
                    {
                        transformedPiece = TransformRookToGolem(selectedPiece); // Methode muss PieceType zurückgeben
                        Debug.Log("Rook wurde in einen Golem transformiert");
                    }
                    else if (selectedPiece.type == ChessPieceType.Knight)
                    {
                        transformedPiece = TransformKnightToKelpie(selectedPiece); // Methode muss PieceType zurückgeben
                        Debug.Log("Knight wurde in einen Kelpie transformiert");
                    }
                    // NEU: Bishop transformieren
                    else if (selectedPiece.type == ChessPieceType.Bishop)
                    {
                        transformedPiece = TransformBishopToMantis(selectedPiece); // Neue Methode, muss PieceType zurückgeben
                        Debug.Log("Bishop wurde in einen Mantis transformiert");
                    }

                    // Mana abziehen
                    GameManager.Instance.UseMana(GameManager.Instance.CurrentPlayer, transformationCost);
                    hasTransformed = true;
                    lastMovedOrTransformedPiece = transformedPiece; // Die NEUE Figur merken

                    // Auswahl zurücksetzen (passiert jetzt in den Transform-Methoden)
                    // selectedPieceForTransformation = null; // Wird in den Transform... Methoden gemacht
                }
                else
                {
                    Debug.Log("Nicht genug Mana für die Transformation!");
                    // Optional: Auswahl zurücksetzen, damit Spieler etwas anderes tun kann
                    // selectedPieceForTransformation = null;
                }
            }
            else
            {
                Debug.Log("Du kannst nur deine eigenen Figuren transformieren");
                selectedPieceForTransformation = null; // Auswahl zurücksetzen
            }
        }
        else
        {
            Debug.Log("Keine Figur für die Transformation ausgewählt");
        }
    }
}
