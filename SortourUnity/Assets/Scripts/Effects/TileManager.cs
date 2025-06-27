using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [SerializeField] private Material disabledTileMaterial;
    public Material defaultTileMaterial;
    private List<TileState> disabledTiles = new List<TileState>();
    private GameObject[,] tiles;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Initialize(GameObject[,] boardTiles, Material defaultMaterial, Material disabledMaterial)
    {
        tiles = boardTiles;
        defaultTileMaterial = defaultMaterial;
        disabledTileMaterial = disabledMaterial;
    }

    public void DisableTile(Vector2Int position, int rounds)
    {
        var existingTile = disabledTiles.Find(t => t.position == position);
        if (existingTile != null)
        {
            existingTile.disabledRounds = rounds;
        }
        else
        {
            disabledTiles.Add(new TileState
            {
                position = position,
                disabledRounds = rounds,
                tileObject = tiles[position.x, position.y]
            });
        }
        UpdateTileVisual(position, true);
    }

    public void ProcessEndOfRound()
    {
        NotificationManager.Instance.ShowMessage("disabledTiles.Count: " + disabledTiles.Count);
        for (int i = disabledTiles.Count - 1; i >= 0; i--)
        {
            var tile = disabledTiles[i];
            
            NotificationManager.Instance.ShowMessage($"Tile at {tile.position} has {tile.disabledRounds} rounds left.");

            if (tile.disabledRounds < 0)
            {
                UpdateTileVisual(tile.position, false); 
                disabledTiles.RemoveAt(i);            
            }
            tile.disabledRounds--;
        }
    }



    public bool IsTileDisabled(Vector2Int position)
    {
        return disabledTiles.Exists(t => t.position == position && t.disabledRounds > 0);
    }

    private void UpdateTileVisual(Vector2Int position, bool isDisabled)
    {
        if (position.x < 0 || position.x >= tiles.GetLength(0) ||
            position.y < 0 || position.y >= tiles.GetLength(1))
        {
            Debug.LogError($"Invalid tile position: {position}");
            return;
        }

        var tileObj = tiles[position.x, position.y];
        if (tileObj == null)
        {
            Debug.LogError($"Tile at {position} is null!");
            return;
        }

        var renderer = tileObj.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            Debug.LogError($"No renderer found on tile at {position}");
            return;
        }

        NotificationManager.Instance.ShowMessage($"Updating tile at {position} - Disabled: {isDisabled}");

        if (isDisabled)
        {
            if (disabledTileMaterial == null)
                Debug.LogError("disabledTileMaterial is not assigned!");
            renderer.material = disabledTileMaterial;
        }
        else
        {
            if (defaultTileMaterial == null)
                Debug.LogError("defaultTileMaterial is not assigned!");
            renderer.material = defaultTileMaterial;
        }
    }

    public void ResetDisabledTiles()
    {
        foreach (var tileState in disabledTiles)
        {
            UpdateTileVisual(tileState.position, false);
        }
        disabledTiles.Clear();
    }


}