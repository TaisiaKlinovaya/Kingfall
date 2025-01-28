using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using TreeEditor;
using Unity.VisualScripting;
#endif

using UnityEngine;

public class GenerateBoard : MonoBehaviour
{
    [Header("Art stuff")]
    [SerializeField] private Material tileMaterial;
    [SerializeField] private float tileSize = 1;
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.4f;
    [SerializeField] private float dragOffset = 1f;

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
    private Chessboard chessboard; // Chessboard-Klasse für das Hovern
    private PieceType currentlyDragging;
    private List<PieceType> deadWhites = new List<PieceType>();
    private List<PieceType> deadBlacks = new List<PieceType>();

    //Chessboard
    public static Chessboard ChessboardInstance { get; private set; }
    GameObject tile;
    private Collider[] overlappingColliders;
    private Vector2Int currentHover;
    //private Camera currentCamera;
    private const int TILE_COUNT = 8; // 8 by 8 chessboard
    private bool isBoardGenerated = false;
    private bool isSpawningInProgress = false;

    public static GenerateBoard Instance { get; private set; } //W!

    private void Awake()
    {
        //W!
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        //!W
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        // Initialize the allChessPieces array
        allChessPieces = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];

        chessboard = gameObject.AddComponent<Chessboard>();
        chessboard.Initialize(tiles);
    }

    private void Start()
    {
        if (ChessboardInstance != null && ChessboardInstance != this)
        {
            Destroy(this);
        }
        else
        {
            //ChessboardInstance = this;
        }

        if (currentCamera == null)
        {
            currentCamera = GameObject.Find("Player1Camera").GetComponent<Camera>();
        }
    }

    private void Update()
    {
        //spawn and delete all chesspieces based on game state
        if (GameManager.Instance.State == "GameRun" && !isBoardGenerated && !isSpawningInProgress)
        {
            isSpawningInProgress = true;  // Set flag before starting spawn
            StartCoroutine(SpawnAndPositionPiecesWithDelay());
        }
        if (GameManager.Instance.State == "StartMenu")
        {
            isBoardGenerated = false;
            isSpawningInProgress = false;  // Reset the flag when returning to menu
            DeleteAllPieces();
        }
        //check if king is dead, activate win scene if he is
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
            //set camera depending on whose turn it is
            if (GameManager.Instance.CurrentPlayer == 1)
            {
                currentCamera = GameObject.Find("Player1Camera").GetComponent<Camera>();
            }
            if (GameManager.Instance.CurrentPlayer == 2)
            {
                currentCamera = GameObject.Find("Player2Camera").GetComponent<Camera>();
            }

            //only interactable if board is fully generated
            if (isBoardGenerated)
            {
                RaycastHit info;
                Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))
                {
                    Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

                    // If we are hovering a tile after not hovering any tiles
                    if (currentHover == -Vector2Int.one)
                    {
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }

                    // If we were already hovering a tile, change the previous one
                    if (currentHover != hitPosition)
                    {
                        tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                        currentHover = hitPosition;
                        tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                    }

                    // If we press down on the mouse
                    if (Input.GetMouseButtonDown(0))
                    {
                        if (currentlyDragging == null)
                        {
                            if (allChessPieces[hitPosition.x, hitPosition.y] != null)
                            {
                                // Is it our turn?
                                if (true)
                                {
                                    currentlyDragging = allChessPieces[hitPosition.x, hitPosition.y];

                                    // Get a list of where i can go, highlight tiles as well
                                    availableMoves = currentlyDragging.GetAvailableMoves(ref allChessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                                    HighlightTiles();
                                }
                            }
                        }
                        else
                        {
                            // Try to move the piece to the new position
                            Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                            bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                            if (!validMove)
                            {
                                currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                            }

                            currentlyDragging = null;
                            RemoveHighlightTiles();
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

                // If we are dragging a piece
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
    }
    public void SetCamera(int activeTeam)
    {
        if (activeTeam == 1)
        {
            currentCamera = GameManager.Instance.player1Camera; // Reference to Player 1's camera
        }
        else if (activeTeam == 2)
        {
            currentCamera = GameManager.Instance.player2Camera; // Reference to Player 2's camera
            Debug.Log("camera set to player2 in setCamera");
        }
    }
    //!W
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject($"Tile{x}{y}");
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        //Array of 4 vertices to create a square
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(0, yOffset, 0);
        vertices[1] = new Vector3(0, yOffset, tileSize);
        vertices[2] = new Vector3(tileSize, yOffset, 0);
        vertices[3] = new Vector3(tileSize, yOffset, tileSize);

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        //assigning the arrays to the actual mesh component
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        // Set the position of the tile based on its grid coordinates
        tileObject.transform.localPosition = new Vector3(x * tileSize, 0, y * tileSize);
        tileObject.layer = LayerMask.NameToLayer("Tile");

        // Create a BoxCollider for the tile
        BoxCollider collider = tileObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(tileSize, 0.5f, tileSize); // thin in the y-axis
        collider.center = new Vector3(tileSize / 2, 0, tileSize / 2); // Center the collider

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
        //return new Vector3(x * tileSize + (tileSize / 2), yOffset, y * tileSize + (tileSize / 2));
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    // HighlightTiles
    private void HighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Highlight");
        }
    }

    private void RemoveHighlightTiles()
    {
        for (int i = 0; i < availableMoves.Count; i++)
        {
            tiles[availableMoves[i].x, availableMoves[i].y].layer = LayerMask.NameToLayer("Tile");
        }
        availableMoves.Clear();
    }

    // Operations
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

    private bool MoveTo(PieceType cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            Debug.Log("Ung ltiger Zug: Das Ziel ist kein g ltiges Feld.");
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Is there another piece on the target position?
        if (allChessPieces[x, y] != null)
        {
            PieceType ocp = allChessPieces[x, y];

            if (cp.team == ocp.team)
            {
                Debug.Log("Ung ltiger Zug: Eigene Figur auf dem Zielfeld.");
                return false;
            }

            // If it's the enemy team
            if (ocp.team == 0)
            {
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(8 * tileSize, yOffset - 0.23f, -1 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);

                // Meldung: Gegnerische Figur geschlagen
                Debug.Log($"Figur {cp.GetType().Name} (Team {(cp.team == 0 ? "Wei " : "Schwarz")}) hat {ocp.GetType().Name} (Team Wei ) auf Feld ({x}, {y}) geschlagen.");
            }
            else
            {
                deadBlacks.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(-1 * tileSize, yOffset - 0.23f, 8 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.back * deathSpacing) * deadBlacks.Count);

                // Meldung: Gegnerische Figur geschlagen
                Debug.Log($"Figur {cp.GetType().Name} (Team {(cp.team == 0 ? "Wei " : "Schwarz")}) hat {ocp.GetType().Name} (Team Schwarz) auf Feld ({x}, {y}) geschlagen.");
            }


            if (ocp.type == ChessPieceType.King)
            {
                isKingDead = true;
                winTeam = (ocp.team == 1) ? "White" : "Black";

            }
        }

        allChessPieces[x, y] = cp;
        allChessPieces[previousPosition.x, previousPosition.y] = null;

        positionSinglePiece(x, y);

        // Meldung der neuen Position
        Debug.Log($"Figur {cp.GetType().Name} (Team {(cp.team == 0 ? "Wei " : "Schwarz")}) wurde von ({previousPosition.x}, {previousPosition.y}) nach ({x}, {y}) verschoben.");

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
        return -Vector2Int.one; //Invalid
    }

    // Chess piece spawn methods
    private PieceType SpawnSinglePiece(ChessPieceType type, int team)
    {
        //checks what team the piece is and chooses black or white prefab accordingly
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
        // Clear the board first
        DeleteAllPieces();
        allChessPieces = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0;
        int blackTeam = 1;

        // Helper method to spawn and position a piece
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

        // Spawn and position white team
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

        // Spawn and position black team
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
        isSpawningInProgress = false;  // Reset flag after spawning is complete
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
        // Iterate through the array and destroy each piece
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (allChessPieces[x, y] != null)
                {
                    Destroy(allChessPieces[x, y].gameObject);
                    allChessPieces[x, y] = null; // Clear reference in the array
                }
            }
        }

        //deletes all dead pieces and clears the lists

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
    }
    public void ProcessDefeatedPiece(PieceType defeatedPiece)
    {
        if (defeatedPiece == null)
        {
            return;
        }

        if (defeatedPiece.team == 0)
        {
            deadWhites.Add(defeatedPiece);
            defeatedPiece.SetScale(Vector3.one * deathSize);
            defeatedPiece.SetPosition(
                new Vector3(8 * tileSize, yOffset - 0.23f, -1 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.forward * deathSpacing) * deadWhites.Count);
        }
        else
        {
            deadBlacks.Add(defeatedPiece);
            defeatedPiece.SetScale(Vector3.one * deathSize);
            defeatedPiece.SetPosition(
                new Vector3(-1 * tileSize, yOffset - 0.23f, 8 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.back * deathSpacing) * deadBlacks.Count);
        }

        Debug.Log($"Figur {defeatedPiece.GetType().Name} (Team {(defeatedPiece.team == 0 ? "Wei " : "Schwarz")}) wurde besiegt.");
    }
}