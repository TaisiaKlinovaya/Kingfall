using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TileManager : MonoBehaviour
{
    public static TileManager Instance { get; private set; }

    [SerializeField] private Material disabledTileMaterial;
    private Material defaultTileMaterial;
    private List<TileState> disabledTiles = new List<TileState>();
    private GameObject[,] tiles;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void Initialize(GameObject[,] boardTiles, Material defaultMaterial)
    {
        tiles = boardTiles;
        defaultTileMaterial = defaultMaterial;
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
        UpdateTileVisual(position);
    }

    public void ProcessDisabledTurns()
    {
        Debug.Log($"Processing disabled turns. Current disabled tiles: {disabledTiles.Count}");

        for (int i = disabledTiles.Count - 1; i >= 0; i--)
        {
            disabledTiles[i].disabledRounds--;
            Debug.Log($"Tile at {disabledTiles[i].position} now has {disabledTiles[i].disabledRounds} rounds left");

            if (disabledTiles[i].disabledRounds <= 0)
            {
                Debug.Log($"Removing tile at {disabledTiles[i].position} (expired)");
                UpdateTileVisual(disabledTiles[i].position);
                disabledTiles.RemoveAt(i);
            }
        }
    }

    public bool IsTileDisabled(Vector2Int position)
    {
        return disabledTiles.Exists(t => t.position == position && t.disabledRounds > 0);
    }

    private void UpdateTileVisual(Vector2Int position)
    {
        var tileState = disabledTiles.Find(t => t.position == position);
        var renderer = tiles[position.x, position.y].GetComponent<MeshRenderer>();

        if (tileState != null && tileState.disabledRounds > 0)
        {
            renderer.sharedMaterial = disabledTileMaterial;
        }
        else
        {
            renderer.sharedMaterial = defaultTileMaterial;
        }
    }
}