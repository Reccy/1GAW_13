using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Reccy.DebugExtensions;
using Reccy.ScriptExtensions;
using TMPro;

public class BotAI : MonoBehaviour
{
    [SerializeField] private Tilemap m_tilemap;
    private Vector3Int MinCell => m_tilemap.cellBounds.min;
    private Vector3Int MaxCell => m_tilemap.cellBounds.max;

    private LevelManager m_levelManager;

    private void Awake()
    {
        m_levelManager = FindObjectOfType<LevelManager>();
    }

    /// <summary>
    /// Performs an A* shortest path algorithm between the start and end positions.
    /// Adapted from: https://www.redblobgames.com/pathfinding/a-star/introduction.html
    /// </summary>
    /// <returns>The list of positions as <see cref="Vector3"/> to follow to get to the end position.</returns>
    public List<Vector2Int> FindPathBetween(Vector3Int startPos, Vector3Int endPos)
    {
        // Prepare A*
        Vector2Int startCellIndex = (Vector2Int)startPos;
        Vector2Int endCellIndex = (Vector2Int)endPos;

        if (!InputsAreValid(startCellIndex, endCellIndex))
            return new List<Vector2Int>();

        Vector2Int current = Vector2Int.zero;
        List<Vector2Int> frontier = new List<Vector2Int>();
        List<Vector2Int> visited = new List<Vector2Int>();
        Dictionary<Vector2Int, float> cellPriority = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> currentCost = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        Vector2Int fakeNull = new Vector2Int(int.MinValue, int.MinValue); // Fake "null" value to determine end of cameFrom

        // Start the algorithm
        frontier.Add(startCellIndex);
        cellPriority.Add(startCellIndex, 0);
        currentCost.Add(startCellIndex, 0);
        cameFrom.Add(startCellIndex, fakeNull);

        while (frontier.Count > 0)
        {
            current = cellPriority.GetSmallest();
            cellPriority.Remove(current);

            if (current == endCellIndex)
                break;

            var surroundingCells = GetSurroundingCells(current);

            foreach (Vector2Int next in surroundingCells)
            {
                if (visited.Contains(next))
                    continue;

                float newCost = currentCost[current] + Vector2Int.Distance(current, next) + GetCellValue(next);

                if (!currentCost.ContainsKey(next) || newCost < currentCost[next])
                {
                    currentCost[next] = newCost;

                    float priority = newCost + Vector2Int.Distance(next, endCellIndex);

                    if (!cellPriority.ContainsKey(next))
                        cellPriority.Add(next, priority);
                    else
                        cellPriority[next] = priority;
                        
                    frontier.Add(next);

                    if (!cameFrom.ContainsKey(next))
                        cameFrom.Add(next, current);
                    else
                        cameFrom[next] = current;
                }
            }

            frontier.Remove(current);
            visited.Add(current);
        }

        if (current != endCellIndex)
        {
            Debug.LogWarning("Could not find path!");
            return new List<Vector2Int>();
        }
        
        // Start backtracking through cameFrom to find the cell
        List<Vector2Int> path = new List<Vector2Int>();

        while (cameFrom[current] != fakeNull)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        // Add original start and end positions to path
        path.Add(startCellIndex);
        path.Reverse();
        path.Add(endCellIndex);

        foreach (var x in visited)
        {
            Debug2.DrawCross((Vector3Int)x + m_tilemap.tileAnchor, Color.green);
        }

        return path;
    }

    private bool InputsAreValid(Vector2Int startCellIndex, Vector2Int endCellIndex)
    {
        bool result = true;

        if (!m_tilemap.cellBounds.Contains((Vector3Int)startCellIndex))
        {
            Debug.LogWarning($"Cannot pathfind, startCellIndex is out of bounds {startCellIndex}");
            result = false;
        }

        if (!m_tilemap.cellBounds.Contains((Vector3Int)endCellIndex))
        {
            Debug.LogWarning($"Cannot pathfind, endCellIndex is out of bounds {endCellIndex}");
            result = false;
        }

        return result;
    }

    private int GetCellValue(Vector2Int pos)
    {
        LevelTile tile = m_levelManager.GetTileInfo((Vector3Int)pos);

        if (tile == null)
            return 1;
        else
            return 15 * tile.HP;
    }

    private List<Vector2Int> GetSurroundingCells(Vector2Int from)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        Vector2Int up = from + Vector2Int.up;
        Vector2Int down = from + Vector2Int.down;
        Vector2Int left = from + Vector2Int.left;
        Vector2Int right = from + Vector2Int.right;

        var upTile = m_levelManager.GetTileInfo(up);
        var downTile = m_levelManager.GetTileInfo(down);
        var leftTile = m_levelManager.GetTileInfo(left);
        var rightTile = m_levelManager.GetTileInfo(right);

        if (MaxCell.y > up.y && (upTile == null || upTile.Breakable))
            result.Add(up);

        if (MaxCell.x > right.x && (rightTile == null || rightTile.Breakable))
            result.Add(right);

        if (MinCell.y <= down.y && (downTile == null || downTile.Breakable))
            result.Add(down);

        if (MinCell.x <= left.x && (leftTile == null || leftTile.Breakable))
            result.Add(left);

        return result;
    }
}
