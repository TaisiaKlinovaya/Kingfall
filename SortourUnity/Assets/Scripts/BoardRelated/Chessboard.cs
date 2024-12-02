using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

public class Chessboard : MonoBehaviour
{
    private GameObject[,] tiles;
    GameObject tile;
    private Collider[] overlappingColliders;
    private Vector2Int currentHover;
    private Camera currentCamera;
    private const int TILE_COUNT = 8; // 8 by 8 chessboard

    public void Initialize(GameObject[,] tiles)
    {
        this.tiles = tiles;
        currentHover = -Vector2Int.one;
    }

    public void HoverTiles(Camera currentCamera)
    {
        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = -Vector2Int.one;
            }
        }
    }

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
            Debug.LogError("Tile collider not found!");
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
            // Get the piece component from the first overlapping collider
            PieceType piece = overlappingColliders[0].GetComponent<PieceType>();
            if (piece != null)
            {
                string pieceName = GetPieceTypeString(piece.type);
                string teamColor = piece.team == 0 ? "White" : "Black";
                int teamNum = piece.team;
                //temporary, add conditions
                CallPieces(pieceName, teamColor, teamNum, tilePosition.x, tilePosition.y, overlappingColliders);
            }
            else
            {
                Debug.Log($"Tile ({tilePosition.x}, {tilePosition.y}): Piece found but type unknown");
            }
        }
        else
        {
            Debug.Log($"Tile ({tilePosition.x}, {tilePosition.y}): Empty");
        }
    }

    private void CallPieces(string pieceName, string teamColor, int teamNum, int PosX, int PosY, Collider[] overlappingColliders)
    {
        Rook rook = new Rook();


        switch (pieceName)
        {
            case "Bishop":
                Debug.Log("You clicked on a " + teamColor + " Bishop on tile (" + PosX + "|" + PosY + ")");
                break;

            case "Pawn":
                //Debug.Log("You clicked on a " + teamColor + " Pawn on tile (" + PosX + "|" + PosY + ")");
                Pawn pawn = overlappingColliders[0].GetComponent<Pawn>();
                if (pawn != null)
                {
                    pawn.MovePawn(teamNum);
                }
                break;

            case "Rook":
                //Debug.Log("You clicked on a " + teamColor + " Rook on tile (" + PosX + "|" + PosY + ")");
                rook.GetPossibleMoves(PosX, PosY);
                break;
            case "Knight":
                Debug.Log("You clicked on a " + teamColor + " Knight on tile (" + PosX + "|" + PosY + ")");
                break;
            case "Queen":
                Debug.Log("You clicked on a " + teamColor + " Queen on tile (" + PosX + "|" + PosY + ")");
                break;
            case "King":
                Debug.Log("You clicked on a " + teamColor + " King on tile (" + PosX + "|" + PosY + ")");
                break;
            default:
                Debug.Log("Unknown piece type clicked.");
                break;
        }
    }


    private void Update()
    {
        if (currentCamera == null)
        {
            currentCamera = Camera.main;
            return;
        }

        HoverTiles(currentCamera);

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // checking for Tile layer
            if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Tile", "Hover")))
            {
                Vector2Int tilePosition = LookupTileIndex(hit.transform.gameObject);
                if (tilePosition != -Vector2Int.one)
                {
                    CheckTileClick(tilePosition);
                }
            }
        }
    }
}