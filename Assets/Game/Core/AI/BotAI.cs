using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Reccy.DebugExtensions;
using Reccy.ScriptExtensions;
using System.Linq; // I know this is bad practice but fuck it, it's a prototype

public class BotAI : MonoBehaviour
{
    [SerializeField] private Tilemap m_groundTilemap;
    private Vector3Int MinCell => m_groundTilemap.cellBounds.min;
    private Vector3Int MaxCell => m_groundTilemap.cellBounds.max;
    private Vector2Int TopLeftCell => new Vector2Int(MinCell.x, MaxCell.y);

    private LevelManager m_levelManager;

    private HashSet<Vector2Int> m_discoveredOres;
    private HashSet<Vector2Int> m_assignedOres;

    private List<ScannerBot> m_scannerBots;
    private List<DigBot> m_digBots;

    [SerializeField] private readonly float m_aiTickSecond = 2.0f;
    private float m_aiTick = 0;

    private void Awake()
    {
        m_levelManager = FindObjectOfType<LevelManager>();
        m_discoveredOres = new HashSet<Vector2Int>();
        m_assignedOres = new HashSet<Vector2Int>();

        m_scannerBots = FindObjectsOfType<ScannerBot>().ToList();
        m_digBots = FindObjectsOfType<DigBot>().ToList();

        m_levelManager.OnTileDestroyed += OnTileDestroyed;
    }

    private void Update()
    {
        if (m_oreScansDebug != null)
        {
            foreach (var v in m_oreScansDebug)
            {
                Debug2.DrawCross(m_levelManager.WorldPosition(v), Color.red);
            }
        }
    }

    private void FixedUpdate()
    {
        m_aiTick -= Time.deltaTime;

        if (m_aiTick <= 0)
        {
            m_aiTick = m_aiTickSecond;

            AssignDigJobs();
        }
    }

    private void AssignDigJobs()
    {
        if (m_discoveredOres.Count == 0)
            return;

        foreach (var bot in m_digBots)
        {
            if (bot.IsAssignedJob)
                continue;

            var set = m_discoveredOres.Except(m_assignedOres).ToList();

            if (set.Count == 0)
                return;

            var tile = set.ClosestToZero((Vector2Int v) => Vector2Int.Distance((Vector2Int)m_levelManager.CellPosition(bot.transform.position), v));

            m_assignedOres.Add(tile);

            bot.AssignDigJob(tile);
        }
    }

    private void OnTileDestroyed(Vector2Int tileCell)
    {
        if (m_discoveredOres.Contains(tileCell))
            m_discoveredOres.Remove(tileCell);

        if (m_assignedOres.Contains(tileCell))
            m_assignedOres.Remove(tileCell);
    }

    public List<Vector2Int> FindPatrolRouteForScanBot(Vector2Int startPos)
    {
        if (!InputsAreValid(startPos))
            return new List<Vector2Int>();

        List<Vector2Int> potentialPositions = FloodSearchEmptyCells(startPos);
        potentialPositions = RemoveOrphanedCells(potentialPositions);
        var current = GetClosestCell(potentialPositions, TopLeftCell);
        var previous = current;

        Dictionary<Vector2Int, int> visitCount = new Dictionary<Vector2Int, int>();

        List<Vector2Int> result = new List<Vector2Int>();

        foreach (var pos in potentialPositions)
        {
            if (!visitCount.ContainsKey(pos))
                visitCount.Add(pos, 0);
        }

        while (visitCount.MinValue() == 0)
        {
            // visit current
            visitCount[current] += 1;

            result.Add(current);

            var potentiallyNext = GetSurroundingCells(current, false);
            potentiallyNext = potentiallyNext.AsQueryable().Intersect(potentialPositions).ToList();
            potentiallyNext = GetWithLeastVisits(visitCount, potentiallyNext);

            Vector2Int next = potentiallyNext.ClosestToZero((Vector2Int v) => {
                var score = Vector2Int.Distance(previous, v);

                // Discourage immediate backtracking
                if (v == previous)
                    score += 2;

                return score;
            });

            // Move to next
            previous = current;
            current = next;
        }

        return result;

        List<Vector2Int> GetWithLeastVisits(Dictionary<Vector2Int, int> visitCounts, List<Vector2Int> potentiallyNext)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            int currentMinValue = visitCounts.MaxValue();

            foreach (var v in potentiallyNext)
            {
                int newMinValue = visitCounts[v];

                if (newMinValue < currentMinValue)
                    currentMinValue = newMinValue;
            }

            foreach (var v in potentiallyNext)
            {
                if (visitCounts[v] == currentMinValue)
                    result.Add(v);
            }

            return result;
        }
    }

    private List<Vector2Int> FloodSearchEmptyCells(Vector2Int startPos)
    {
        if (m_levelManager.GetTileInfo(startPos) != null)
        {
            Debug.LogWarning("Can't get empty cells. Passed in cell position is not empty.");
            return new List<Vector2Int>();
        }

        List<Vector2Int> frontier = new List<Vector2Int>();
        List<Vector2Int> visited = new List<Vector2Int>();

        frontier.Add(startPos);
        
        while (!frontier.IsEmpty())
        {
            var current = frontier.Take();

            var surrounding = GetSurroundingCells(current, false);

            surrounding.RemoveDuplicates(visited);

            foreach (var cell in surrounding)
                frontier.Add(cell);

            visited.Add(current);
        }

        return visited;
    }

    private List<Vector2Int> RemoveOrphanedCells(List<Vector2Int> originalList)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        foreach (var cell in originalList)
        {
            if (GetSurroundingBlocks(cell).IsEmpty())
                continue;

            result.Add(cell);
        }

        return result;
    }

    private Vector2Int GetClosestCell(List<Vector2Int> searchSpace, Vector2Int startPos)
    {
        float closestDistance = float.MaxValue;
        Vector2Int result = startPos;

        foreach (var cell in searchSpace)
        {
            float dist = Vector2Int.Distance(startPos, cell);

            if (dist < closestDistance)
            {
                result = cell;
                closestDistance = dist;
            }
        }

        return result;
    }

    /// <summary>
    /// Performs an A* shortest path algorithm between the start and end positions.
    /// Adapted from: https://www.redblobgames.com/pathfinding/a-star/introduction.html
    /// </summary>
    /// <returns>The list of positions as <see cref="Vector3"/> to follow to get to the end position.</returns>
    public List<Vector2Int> FindPath(Vector3Int startPos, Vector3Int endPos, bool canDig = true)
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

            var surroundingCells = GetSurroundingCells(current, canDig);

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
            Debug2.DrawCross((Vector3Int)x + m_groundTilemap.tileAnchor, Color.green);
        }

        return path;
    }

    private bool InputsAreValid(Vector2Int startCellIndex)
    {
        bool result = true;

        if (!m_groundTilemap.cellBounds.Contains((Vector3Int)startCellIndex))
        {
            Debug.LogWarning($"Cannot pathfind, startCellIndex is out of bounds {startCellIndex}");
            result = false;
        }

        return result;
    }

    private bool InputsAreValid(Vector2Int startCellIndex, Vector2Int endCellIndex)
    {
        bool result = true;

        if (!m_groundTilemap.cellBounds.Contains((Vector3Int)startCellIndex))
        {
            Debug.LogWarning($"Cannot pathfind, startCellIndex is out of bounds {startCellIndex}");
            result = false;
        }

        if (!m_groundTilemap.cellBounds.Contains((Vector3Int)endCellIndex))
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

    private List<Vector2Int> m_oreScansDebug;

    public void ScanForOre(Vector2Int cellOrigin, int radius)
    {
        m_oreScansDebug = new List<Vector2Int>();

        for (int x = 0; x <= radius; ++x)
        {
            for (int y = 0; y <= radius; ++y)
            {
                if (Mathf.Abs(x) + Mathf.Abs(y) > radius)
                    continue;

                ScanOre(cellOrigin.x + x, cellOrigin.y + y);

                if (x != 0)
                    ScanOre(cellOrigin.x - x, cellOrigin.y + y);

                if (y != 0)
                    ScanOre(cellOrigin.x + x, cellOrigin.y - y);

                if (x != 0 && y != 0)
                    ScanOre(cellOrigin.x - x, cellOrigin.y - y);
            }
        }
    }

    private void ScanOre(int x, int y)
    {
        var coords = new Vector2Int(x, y);
        m_oreScansDebug.Add(coords);
        m_levelManager.ClearFogOfWar((Vector3Int)coords);

        var tile = m_levelManager.GetTileInfo(coords);

        if (tile == null)
            return;

        if (tile.TileType == TileType.ORE)
        {
            if (!m_discoveredOres.Contains(coords))
            {
                m_discoveredOres.Add(coords);
            }
        }
    }

    private void SetTileColor(Vector2Int tile, Color col)
    {
        var t = (Vector3Int)tile;
        var flags = m_groundTilemap.GetTileFlags((Vector3Int)tile);

        m_groundTilemap.SetTileFlags(t, TileFlags.None);
        m_groundTilemap.SetColor((Vector3Int)tile, col);
        m_groundTilemap.SetTileFlags(t, flags);
    }

    private List<Vector2Int> GetSurroundingCells(Vector2Int from, bool canDig)
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

        if (MaxCell.y > up.y && (upTile == null || (upTile.Breakable && canDig)))
            result.Add(up);

        if (MaxCell.x > right.x && (rightTile == null || (rightTile.Breakable && canDig)))
            result.Add(right);

        if (MinCell.y <= down.y && (downTile == null || (downTile.Breakable && canDig)))
            result.Add(down);

        if (MinCell.x <= left.x && (leftTile == null || (leftTile.Breakable && canDig)))
            result.Add(left);

        return result;
    }

    private List<Vector2Int> GetSurroundingBlocks(Vector2Int from)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        Vector2Int up = from + Vector2Int.up;
        Vector2Int down = from + Vector2Int.down;
        Vector2Int left = from + Vector2Int.left;
        Vector2Int right = from + Vector2Int.right;
        Vector2Int upLeft = from + Vector2Int.up + Vector2Int.left;
        Vector2Int upRight = from + Vector2Int.up + Vector2Int.right;
        Vector2Int downLeft = from + Vector2Int.down + Vector2Int.left;
        Vector2Int downRight = from + Vector2Int.down + Vector2Int.right;

        var upTile = m_levelManager.GetTileInfo(up);
        var downTile = m_levelManager.GetTileInfo(down);
        var leftTile = m_levelManager.GetTileInfo(left);
        var rightTile = m_levelManager.GetTileInfo(right);
        var upLeftTile = m_levelManager.GetTileInfo(upLeft);
        var upRightTile = m_levelManager.GetTileInfo(upRight);
        var downLeftTile = m_levelManager.GetTileInfo(downLeft);
        var downRightTile = m_levelManager.GetTileInfo(downRight);

        if (MaxCell.y > up.y && upTile != null)
            result.Add(up);

        if (MaxCell.x > right.x && rightTile != null)
            result.Add(right);

        if (MinCell.y <= down.y && downTile != null)
            result.Add(down);

        if (MinCell.x <= left.x && leftTile != null)
            result.Add(left);

        if (MaxCell.y > up.y && MaxCell.x > right.x && upRightTile != null)
            result.Add(upRight);

        if (MaxCell.y > up.y && MinCell.x <= left.x && upLeftTile != null)
            result.Add(upLeft);

        if (MinCell.y <= down.y && MinCell.x <= left.x && downLeftTile != null)
            result.Add(downLeft);

        if (MinCell.y <= down.y && MaxCell.x > right.x && downRightTile != null)
            result.Add(downRight);

        return result;
    }
}
