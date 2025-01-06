using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using TreeEditor;
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


    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllChessPieces();
        positionPieces();
        
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

        if (!currentCamera)
        {
            currentCamera = Camera.main;
            return;
        }
        //
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
            //
            RaycastHit info;
            Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight")))//W!, "Highlight"
            {
                //Get the indexes of tile we hit
                Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);
                //If we are hovering any tile after not hovering any tile
                if (currentHover == -Vector2Int.one)
                {
                    currentHover = hitPosition;
                    tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }
                //if we were already hovernig a tile, change prewius
                if (currentHover != hitPosition)
                {
                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                    currentHover = hitPosition;
                    tiles[currentHover.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
                }
                // If we press down on the mouse
                if (Input.GetMouseButtonDown(0))
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
                // If we are releasing the mouse button
                if (currentlyDragging != null && Input.GetMouseButtonUp(0))
                {
                    Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                    bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                    if (!validMove)//!
                    {
                        currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                        currentlyDragging = null;
                    }


                    currentlyDragging = null;

                    RemoveHighlightTiles();

                }

            }
            else
            {
                if (currentHover != -Vector2Int.one)
                {
                    tiles[currentHover.x, currentHover.y].layer = (ContainsValidMove(ref availableMoves, currentHover)) ? LayerMask.NameToLayer("Highlight") : LayerMask.NameToLayer("Tile");
                    currentHover = -Vector2Int.one;
                }
                if (currentlyDragging && Input.GetMouseButtonUp(0))
                {
                    currentlyDragging.SetPosition(GetTileCenter(currentlyDragging.currentX, currentlyDragging.currentY));
                    currentlyDragging = null;
                    RemoveHighlightTiles();
                }
            }
            //if we are dragging a piece
            if (currentlyDragging)
            {
                Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * yOffset);
                float distance = 0.0f;
                if (horizontalPlane.Raycast(ray, out distance))
                {
                    currentlyDragging.SetPosition(ray.GetPoint(distance) + Vector3.up * dragOffset);
                }
            }
            //if (Input.GetMouseButtonDown(0))
            //{
            //    //Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            //    RaycastHit hit;

            //    if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Tile", "Hover")))
            //    {
            //        Vector2Int tilePosition = LookupTileIndex(hit.transform.gameObject);
            //        if (tilePosition != -Vector2Int.one)
            //        {
            //            Wenn FFigur Selektiert, führe Methode dieser Figur aus
            //            if (selectedPiece != null)
            //            {
            //                selectedPiece.SeeFigure(tilePosition, tiles);
            //                selectedPiece = null;
            //            }
            //            else
            //            {
            //                CheckTileClick(tilePosition);
            //            }
            //        }
            //    }
            //}
        }
    }
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
        //W!
        if (!ContainsValidMove(ref availableMoves, new Vector2(x, y)))
        {
            return false;
        }
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Is there another piece on the target position?
        if (allChessPieces[x, y] != null)
        {
            PieceType ocp = allChessPieces[x, y];

            if (cp.team == ocp.team)
            {
                return false;
            }
            //If its the enmy team
            if (ocp.team == 0)
            {
                deadWhites.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(
                new Vector3(8 * tileSize, yOffset - 0.23f, -1 * tileSize)
                - bounds
                + new Vector3(tileSize / 2, 0, tileSize / 2)
                + (Vector3.forward * deathSpacing) * deadWhites.Count);
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
            }
        }

        allChessPieces[x, y] = cp;
        allChessPieces[previousPosition.x, previousPosition.y] = null;

        positionSinglePiece(x, y);
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

    private void SpawnAllChessPieces()
    {
        allChessPieces = new PieceType[TILE_COUNT_X, TILE_COUNT_Y];

        int whiteTeam = 0;
        int blackTeam = 1;

        //white team
        allChessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        allChessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        allChessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        allChessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Queen, whiteTeam);
        allChessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.King, whiteTeam);
        allChessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Bishop, whiteTeam);
        allChessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Knight, whiteTeam);
        allChessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Rook, whiteTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            allChessPieces[i, 1] = SpawnSinglePiece(ChessPieceType.Pawn, whiteTeam);
        }

        //black team
        allChessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        allChessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        allChessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        allChessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Queen, blackTeam);
        allChessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.King, blackTeam);
        allChessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Bishop, blackTeam);
        allChessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Knight, blackTeam);
        allChessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Rook, blackTeam);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            allChessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }

    // positioning pieces

    private void positionPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (allChessPieces[x, y] != null)
                {
                    positionSinglePiece(x, y, true);
                }
            }
        }
    }
    private void positionSinglePiece(int x, int y, Boolean force = false)
    {
        allChessPieces[x, y].currentX = x;
        allChessPieces[x, y].currentY = y;
        allChessPieces[x, y].SetPosition(GetTileCenter(x, y), force); //W!
        //allChessPieces[x, y].transform.localPosition = new Vector3(x * tileSize + (tileSize / 2), yOffset, y * tileSize + (tileSize / 2));
    }
}
