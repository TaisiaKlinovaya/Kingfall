using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateBoard : MonoBehaviour
{
    private float tileSize = 1; //1 Meter
    [SerializeField] private float yOffset = 0.7f;
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    [SerializeField] private GameObject[] BlackTeamPrefabs;
    [SerializeField] private GameObject[] WhiteTeamPrefabs;
    private PieceType[,] allChessPieces;

    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllChessPieces();
        positionPieces();
    }
    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject($"Tile_{x}_{y}");
        tileObject.transform.parent = transform;

        // Set the position of the tile based on its grid coordinates
        tileObject.transform.localPosition = new Vector3(x * tileSize + (tileSize / 2), 0, y * tileSize + (tileSize / 2));

        // Create a BoxCollider for the tile
        BoxCollider collider = tileObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(tileSize, 0.1f, tileSize); // Make it thin in the y-axis
        collider.center = new Vector3(0, 0, 0); // Center the collider

        tileObject.layer = LayerMask.NameToLayer("Tile");

        return tileObject;
    }

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        tiles = new GameObject[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    // Chess piece spawn methods

    private PieceType SpawnSinglePiece(ChessPieceType type, int team)
    {
        //checks what team the piece is and chooses black or white prefab accordingly
        GameObject prefab = (team == 0) ? WhiteTeamPrefabs[(int)type - 1] : BlackTeamPrefabs[(int)type - 1];

        PieceType piece = Instantiate(prefab, transform).GetComponent<PieceType>();

        if (piece == null)
        {
            //Debug.LogError($"Failed to instantiate piece for type: {type} and team: {team}");
            return null; 
        }

        piece.type = type;
        piece.team = team;
        piece.gameObject.layer = LayerMask.NameToLayer("ChessPiece");


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
        Debug.Log(allChessPieces[7, 7]);
        for (int i = 0; i < TILE_COUNT_X; i++)
        {
            allChessPieces[i, 6] = SpawnSinglePiece(ChessPieceType.Pawn, blackTeam);
        }
    }

    // positioning pieces

    private void positionPieces()
    {
        Debug.Log("Positioning pieces..."); // Log positioning start
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
        allChessPieces[x, y].transform.localPosition = new Vector3(x * tileSize + (tileSize / 2), yOffset, y * tileSize + (tileSize / 2));
    }
}
