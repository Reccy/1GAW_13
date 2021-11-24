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

    [SerializeField] private GameObject m_originObj;
    private Vector3Int OriginPos => m_tilemap.WorldToCell(m_originObj.transform.position);

    [SerializeField] private GameObject m_targetObj;
    private Vector3Int TargetPos => m_tilemap.WorldToCell(m_targetObj.transform.position);

    private void Update()
    {
        var results = FindPathBetween(OriginPos, TargetPos);

        for (int i = 1; i < results.Count; ++i)
        {
            var from = results[i - 1];
            var to = results[i];

            Debug2.DrawArrow((Vector3Int)from + m_tilemap.tileAnchor, (Vector3Int)to + m_tilemap.tileAnchor, Color.blue);
        }
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

                float newCost = currentCost[current] + Vector2Int.Distance(current, next) + GetCostValue(next);

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

    private int GetCostValue(Vector2Int pos)
    {
        if (CellIsOccupied(pos))
            return 50;
        else
            return 1;
    }

    public class PathfindResults
    {
        private List<Vector3Int> m_checkedPositions;
        private List<Vector3Int> m_path;
        private Dictionary<Vector3Int, int> m_totalDistance;
        private Vector3Int m_target;
        private Vector3Int m_origin;

        public bool CanReachTarget => m_path.Last() == m_target;

        public List<Vector3Int> CheckedPositions => m_checkedPositions;
        public List<Vector3Int> Path => m_path;
        public Dictionary<Vector3Int, int> TotalDistance => m_totalDistance;
        public Vector3Int Origin => m_origin;
        public Vector3Int Target => m_target;
        public Vector3Int Closest => m_path.Last();

        public PathfindResults(Dictionary<Vector3Int, int> totalDistance, Dictionary<Vector3Int, List<Vector3Int>> backtrack, List<Vector3Int> checkedPositions, Vector3Int target, Vector3Int origin)
        {
            m_checkedPositions = checkedPositions;
            m_totalDistance = totalDistance;

            Vector3Int current = target;
            m_target = target;
            m_origin = origin;

            m_path = new List<Vector3Int>();
            m_path.Add(target);

            while (current != origin)
            {
                var smallestIdx = -1;
                var smallestDist = int.MaxValue;

                for (int i = 0; i < backtrack[current].Count; ++i)
                {
                    var path = backtrack[current][i];
                    var dist = totalDistance[path];

                    if (dist < smallestDist)
                    {
                        smallestDist = dist;
                        smallestIdx = i;
                    }
                }

                var next = backtrack[current][smallestIdx];

                m_path.Add(next);
                current = next;
            }
        }
    }

    // Could seperate this into a Lib class?
    private class ClosestDistanceComparer : IComparer<Vector3Int>
    {
        private Vector3Int m_target;

        public ClosestDistanceComparer(Vector3Int target)
        {
            m_target = target;
        }

        public int Compare(Vector3Int x, Vector3Int y)
        {
            float distX = Vector3Int.Distance(x, m_target);
            float distY = Vector3Int.Distance(y, m_target);

            if (distX < distY)
                return 1;
            else if (distX > distY)
                return -1;
            else
                return 0;
        }
    }

    private List<Vector2Int> GetSurroundingCells(Vector2Int from)
    {
        List<Vector2Int> result = new List<Vector2Int>();

        Vector2Int up = from + Vector2Int.up;
        Vector2Int down = from + Vector2Int.down;
        Vector2Int left = from + Vector2Int.left;
        Vector2Int right = from + Vector2Int.right;

        if (MaxCell.y > up.y)
            result.Add(up);

        if (MaxCell.x > right.x)
            result.Add(right);

        if (MinCell.y <= down.y)
            result.Add(down);

        if (MinCell.x <= left.x)
            result.Add(left);

        return result;
    }

    private bool CellIsOccupied(Vector2Int cell)
    {
        return m_tilemap.GetTile((Vector3Int)cell) != null;
    }
}
