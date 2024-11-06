using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chessboard : MonoBehaviour
{
    private GameObject[,] tiles;
    private Vector2Int currentHover;

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
            //Get the indexes of tile we hit
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            //If we are hovering any tile after not hovering any tile
            if (currentHover == -Vector2Int.one)
            {
                currentHover = hitPosition;
                tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
            }
            //if we were already hovering a tile, change previous
            if (currentHover != hitPosition)
            {
                tiles[currentHover.x, currentHover.y].layer = LayerMask.NameToLayer("Tile");
                currentHover = hitPosition;
                tiles[currentHover.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover");
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
        for (int x = 0; x < tiles.GetLength(0); x++)
        {
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one; //Invalid
    }
    public void MovePiece()
    {

    }
    public void HoverPieces()
    {

    }
}
