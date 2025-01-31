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
                                // Überprüfen, ob die Figur dem aktuellen Spieler gehört
                                if (allChessPieces[hitPosition.x, hitPosition.y].team == GameManager.Instance.CurrentPlayer - 1 && !hasMoved)
                                {
                                    currentlyDragging = allChessPieces[hitPosition.x, hitPosition.y];

                                    if (currentlyDragging != null)
                                    {
                                        Debug.Log(currentlyDragging.GetPieceInfo());
                                    }

                                    availableMoves = currentlyDragging.GetAvailableMoves(ref allChessPieces, TILE_COUNT_X, TILE_COUNT_Y);
                                    HighlightTiles();
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

                    if (Input.GetMouseButtonDown(1))
                    {
                        if (allChessPieces[hitPosition.x, hitPosition.y] != null)
                        {
                            if (allChessPieces[hitPosition.x, hitPosition.y].type == ChessPieceType.Rook)
                            {
                                selectedPieceForTransformation = allChessPieces[hitPosition.x, hitPosition.y];
                                Debug.Log("Rook selected for transformation.");
                            }
                            else if (allChessPieces[hitPosition.x, hitPosition.y].type == ChessPieceType.Knight)
                            {
                                selectedPieceForTransformation = allChessPieces[hitPosition.x, hitPosition.y];
                                Debug.Log("Knight selected for transformation.");
                            }
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

    private bool MoveTo(PieceType cp, int x, int y)
    {
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            Debug.Log("Ungültiger Zug: Das Ziel ist kein gültiges Feld.");
            return false;
        }

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        if (allChessPieces[x, y] != null)
        {
            PieceType ocp = allChessPieces[x, y];

            if (cp.team == ocp.team)
            {
                Debug.Log("Ungültiger Zug: Eigene Figur auf dem Zielfeld.");
                return false;
            }

            GenerateBoard.Instance.ProcessDefeatedPiece(ocp);
            allChessPieces[x, y] = null;

            if (ocp.team == 0)
            {
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                    new Vector3(8 * tileSize, yOffset - 0.23f, -1 * tileSize)
                    - bounds
                    + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadWhites.Count);

                Debug.Log($"Figur {cp.GetType().Name} (Team {(cp.team == 0 ? "Weiß" : "Schwarz")}) hat {ocp.GetType().Name} (Team Weiß) auf Feld ({x}, {y}) geschlagen.");
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

                Debug.Log($"Figur {cp.GetType().Name} (Team {(cp.team == 0 ? "Weiß" : "Schwarz")}) hat {ocp.GetType().Name} (Team Schwarz) auf Feld ({x}, {y}) geschlagen.");
            }

            if (ocp.type == ChessPieceType.King)
            {
                isKingDead = true;
                winTeam = (ocp.team == 1) ? "White" : "Black";
            }
        }

        if (cp.type == ChessPieceType.Golem)
        {
            Golem golem = cp as Golem;
            golem.DefeatFiguresOnPath(ref allChessPieces, previousPosition, new Vector2Int(x, y));
        }

        allChessPieces[x, y] = cp;
        allChessPieces[previousPosition.x, previousPosition.y] = null;
        positionSinglePiece(x, y);

        Debug.Log($"Figur {cp.GetType().Name} (Team {(cp.team == 0 ? "Weiß" : "Schwarz")}) wurde von ({previousPosition.x}, {previousPosition.y}) nach ({x}, {y}) verschoben.");

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

        Debug.Log($"Figur {defeatedPiece.GetType().Name} (Team {(defeatedPiece.team == 0 ? "Weiß" : "Schwarz")}) wurde besiegt.");
    }

    public PieceType GetSelectedPieceForTransformation()
    {
        return selectedPieceForTransformation;
    }

    public void TransformRookToGolem(PieceType rook)
    {
        if (rook.type != ChessPieceType.Rook)
        {
            Debug.LogError("Only Rooks can be transformed into Golems.");
            return;
        }

        int x = rook.currentX;
        int y = rook.currentY;

        allChessPieces[x, y] = null;
        Destroy(rook.gameObject);

        GameObject golemPrefab = (rook.team == 0) ? WhiteTeamPrefabs[(int)ChessPieceType.Golem - 1] : BlackTeamPrefabs[(int)ChessPieceType.Golem - 1];
        GameObject golemObject = Instantiate(golemPrefab, transform);
        PieceType golem = golemObject.GetComponent<PieceType>();

        golem.type = ChessPieceType.Golem;
        golem.team = rook.team;
        golem.currentX = x;
        golem.currentY = y;
        golem.gameObject.layer = LayerMask.NameToLayer("Piece");

        allChessPieces[x, y] = golem;
        positionSinglePiece(x, y, true);

        selectedPieceForTransformation = null;

        Debug.Log($"Rook transformed into Golem at ({x}, {y}).");
    }

    public void TransformKnightToKelpie(PieceType knight)
    {
        if (knight.type != ChessPieceType.Knight)
        {
            Debug.LogError("Only Knights can be transformed into Kelpies.");
            return;
        }

        int x = knight.currentX;
        int y = knight.currentY;

        allChessPieces[x, y] = null;
        Destroy(knight.gameObject);

        GameObject kelpiePrefab = (knight.team == 0) ? WhiteTeamPrefabs[(int)ChessPieceType.Kelpie - 1] : BlackTeamPrefabs[(int)ChessPieceType.Kelpie - 1];
        GameObject kelpieObject = Instantiate(kelpiePrefab, transform);
        PieceType kelpie = kelpieObject.GetComponent<PieceType>();

        kelpie.type = ChessPieceType.Kelpie;
        kelpie.team = knight.team;
        kelpie.currentX = x;
        kelpie.currentY = y;
        kelpie.gameObject.layer = LayerMask.NameToLayer("Piece");

        allChessPieces[x, y] = kelpie;
        positionSinglePiece(x, y, true);

        selectedPieceForTransformation = null;

        Debug.Log($"Knight transformed into Kelpie at ({x}, {y}).");
    }
}