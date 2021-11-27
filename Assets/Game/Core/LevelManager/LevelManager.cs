using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelManager : MonoBehaviour
{
    [Header("Level Config")]
    [SerializeField] private Grid m_grid;
    [SerializeField] private Tilemap m_groundTilemap;

    [Header("Fog of War")]
    [SerializeField] private Tilemap m_fogOfWar;
    [SerializeField] private TileBase m_fogTile;

    [Header("Object Config")]
    [SerializeField] private List<TileTypeDefinition> m_tileTypes;

    private Dictionary<Vector3Int, LevelTile> m_levelTiles;

    public delegate void OnLevelUpdateEvent();
    public OnLevelUpdateEvent OnLevelUpdate;
    private void OnLevelUpdateInvoke()
    {
        if (OnLevelUpdate != null)
            OnLevelUpdate();
    }

    public delegate void OnTileDestroyedEvent(Vector2Int cellPos);
    public OnTileDestroyedEvent OnTileDestroyed;
    private void OnTileDestroyedInvoke(Vector2Int cellPos)
    {
        if (OnTileDestroyed != null)
            OnTileDestroyed(cellPos);
    }

    private int m_fogOfWarFilledFrame = -1;

    private void Awake()
    {
        m_levelTiles = new Dictionary<Vector3Int, LevelTile>();

        foreach (var position in m_groundTilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = m_groundTilemap.GetTile(position);

            if (tile == null)
                continue;

            foreach (var ttd in m_tileTypes)
            {
                if (ttd.tile == tile)
                {
                    var levelTile = CreateLevelTile(ttd);
                    m_levelTiles.Add(position, levelTile);
                    break;
                }
            }
        }

        FillFogOfWar();
    }

    private void FillFogOfWar()
    {
        if (Time.frameCount == m_fogOfWarFilledFrame)
            return;

        m_fogOfWarFilledFrame = Time.frameCount;

        for (int x = m_groundTilemap.cellBounds.min.x; x < m_groundTilemap.cellBounds.max.x; ++x)
        {
            for (int y = m_groundTilemap.cellBounds.min.y; y < m_groundTilemap.cellBounds.max.y; ++y)
            {
                var pos = new Vector3Int(x, y, 0);

                m_fogOfWar.SetTile(pos, m_fogTile);
            }
        }
    }

    public bool IsInFogOfWar(Vector3Int cellPoint)
    {
        return m_fogOfWar.GetTile(cellPoint) != null;
    }

    private LevelTile CreateLevelTile(TileTypeDefinition ttd)
    {
        switch (ttd.tileType)
        {
            case TileType.GROUND:
                return BuildGroundTile();
            case TileType.ORE:
                return BuildOreTile();
        }

        throw new Exception($"FATAL: TileTypeDefinition not implemented for {ttd.tileType}");
    }

    private LevelTile BuildGroundTile()
    {
        return new LevelTile(TileType.GROUND, 3, true);
    }

    private LevelTile BuildOreTile()
    {
        return new LevelTile(TileType.ORE, 5, true);
    }

    public LevelTile GetTileInfo(Vector2Int position)
    {
        return GetTileInfo((Vector3Int)position);
    }

    public LevelTile GetTileInfo(Vector3Int position)
    {
        if (m_levelTiles.ContainsKey(position))
        {
            return m_levelTiles[position];
        }

        return null;
    }

    public Vector3Int CellPosition(Vector3 realPosition)
    {
        return m_grid.WorldToCell(realPosition);
    }

    public Vector3 WorldPosition(Vector2Int cellPosition)
    {
        return WorldPosition((Vector3Int)cellPosition);
    }

    public Vector3 WorldPosition(Vector3Int cellPosition)
    {
        return m_grid.CellToWorld(cellPosition) + m_groundTilemap.tileAnchor;
    }

    public void DigTile(Vector3Int position)
    {
        if (!m_levelTiles.ContainsKey(position))
            return;

        if (!m_levelTiles[position].Breakable)
            return;

        var tile = m_levelTiles[position];

        tile.HP -= 1;

        if (tile.HP <= 0)
        {
            DestroyTileAt(position);
        }
    }

    public void DestroyTileAt(Vector2Int destinationCellPosition)
    {
        DestroyTileAt((Vector3Int)destinationCellPosition);
    }

    public void DigTile(Vector2Int destinationCellPosition)
    {
        DigTile((Vector3Int)destinationCellPosition);
    }

    public void DestroyTileAt(Vector3Int position)
    {
        if (!m_levelTiles.ContainsKey(position))
            return;

        m_levelTiles.Remove(position);
        m_groundTilemap.SetTile(position, null);

        OnTileDestroyedInvoke((Vector2Int)position);
        OnLevelUpdateInvoke();
    }

    public void ClearFogOfWar(Vector3Int coords)
    {
        FillFogOfWar();

        m_fogOfWar.SetTile(coords, null);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var cellPosition = m_groundTilemap.WorldToCell(position);

            Debug.Log(GetTileInfo(cellPosition));
        }
    }
}
