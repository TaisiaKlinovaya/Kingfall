using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Chessboard : MonoBehaviour
{
    public static Chessboard ChessboardInstance { get; private set; }
    private GameObject[,] tiles;
    GameObject tile;
    private Collider[] overlappingColliders;
    private Vector2Int currentHover;
    private Camera currentCamera;
    private const int TILE_COUNT = 8; // 8 by 8 chessboard
    private ChessPieceMovement selectedPiece = null;

    public void Initialize(GameObject[,] tiles)
    {
        this.tiles = tiles;
        currentHover = -Vector2Int.one;
    }

    private void Start()
    {
        if (ChessboardInstance != null && ChessboardInstance != this)
        {
            Destroy(this);
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


            //HoverTiles(currentCamera);

            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Tile", "Hover")))
                {
                    Vector2Int tilePosition = LookupTileIndex(hit.transform.gameObject);
                    if (tilePosition != -Vector2Int.one)
                    {
                        //Wenn FFigur Selektiert, f hre Methode dieser Figur aus
                        if (selectedPiece != null)
                        {
                            //selectedPiece.SeeFigure(tilePosition, tiles);
                            //selectedPiece = null;
                        }
                        else
                        {
                            CheckTileClick(tilePosition);
                        }
                    }
                }
            }
        }

    }

    //public void HoverTiles(Camera currentCamera)
    //{

    //}

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT; x++)
            for (int y = 0; y < TILE_COUNT; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);

        return -Vector2Int.one;
    }

    private string GetPieceTypeString(ChessPieceType type)
    {
        return type switch
        {
            ChessPieceType.Pawn => "Pawn",
            ChessPieceType.Rook => "Rook",
            ChessPieceType.Knight => "Knight",
            ChessPieceType.Bishop => "Bishop",
            ChessPieceType.Queen => "Queen",
            ChessPieceType.King => "King",
            ChessPieceType.Golem => "Golem",
            ChessPieceType.Kelpie => "Kelpie",

            _ => "Unknown"
        };
    }

    private void CheckTileClick(Vector2Int tilePosition)
    {
        if (tilePosition == -Vector2Int.one)
            return;

        tile = tiles[tilePosition.x, tilePosition.y];
        BoxCollider tileCollider = tile.GetComponent<BoxCollider>();

        if (tileCollider == null)
        {
            //Debug.LogError("Tile collider not found!");
            return;
        }

        // Get the bounds of the tile collider in world space
        Bounds tileBounds = tileCollider.bounds;

        // Check for any pieces that overlap with the tile's collider
        overlappingColliders = Physics.OverlapBox(
            tileBounds.center,
            tileBounds.extents,
            tile.transform.rotation,
            LayerMask.GetMask("Piece")
        );

        if (overlappingColliders.Length > 0)
        {

            PieceType pieceType = overlappingColliders[0].GetComponent<PieceType>();
            ChessPieceMovement chessPiece = overlappingColliders[0].GetComponent<ChessPieceMovement>();

            if (chessPiece != null)
            {
                // Bestimme Figurenname und Teamfarbe
                string pieceName = GetPieceTypeString(pieceType.type);
                string teamColor = pieceType.team == 0 ? "White" : "Black";
                int teamNum = pieceType.team;

                // Wenn keine Figur ausgew hlt ist, w hle diese Figur aus
                if (selectedPiece == null)
                {
                    //selectedPiece = chessPiece;
                    //selectedPiece.name = pieceName;
                    //selectedPiece.team = teamNum;
                    //selectedPiece.CurrentPosition = tilePosition;
                    //Debug.Log($"{teamColor} {pieceName} selected");
                }
                else if (selectedPiece == chessPiece)
                {
                    //Debug.Log($"Same {pieceName} clicked again");
                }
            }
            else
            {
                //Debug.LogWarning($"Piece components missing on collider");
            }
        }
        else
        {
            //Debug.Log($"No piece found on this tile");
        }
    }
}