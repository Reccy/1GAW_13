using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Reccy.DebugExtensions;
using Reccy.ScriptExtensions;

public class BotAI : MonoBehaviour
{
    [SerializeField] private Tilemap m_tilemap;
    private Vector3Int MinCell => m_tilemap.cellBounds.min;
    private Vector3Int MaxCell => m_tilemap.cellBounds.max;

    [SerializeField] private GameObject m_testObj;
    private Vector3Int TestObjPos => m_tilemap.WorldToCell(m_testObj.transform.position);

    private void Update()
    {
        var coords = FindAllNavigableCoords(TestObjPos);

        for (int x = MinCell.x; x < MaxCell.x; ++x)
        {
            for (int y = MinCell.y; y < MaxCell.y; ++y)
            {
                Color c = Color.red;
                Vector3Int v = new Vector3Int(x, y, 0);

                if (coords.Contains(v))
                    c = Color.green;

                Debug2.DrawCross(v + m_tilemap.tileAnchor, c);
            }
        }
    }

    public List<Vector3Int> FindAllNavigableCoords(Vector3Int botCell)
    {
        // Init
        Stack<Vector3Int> bucket = new Stack<Vector3Int>();
        List<Vector3Int> visitedCells = new List<Vector3Int>();

        bucket.Push(botCell);

        // Run Algorithm
        do
        {
            Vector3Int thisCell = bucket.Pop();

            var nextCells = GetSurroundingCells(thisCell);

            nextCells.RemoveDuplicates(visitedCells);

            foreach (var cell in nextCells)
            {
                bucket.Push(cell);
            }

            visitedCells.Add(thisCell);
        }
        while (bucket.Count > 0);

        return visitedCells;
    }

    public bool IsCellPositionMovable(Vector3Int botCell, Vector3Int targetCell)
    {
        return false;
    }

    public Vector3Int FindClosestCoordToTarget(Vector3Int botCell, Vector3Int targetCell)
    {
        return Vector3Int.zero;
    }

    private List<Vector3Int> GetSurroundingCells(Vector3Int from)
    {
        List<Vector3Int> result = new List<Vector3Int>();

        Vector3Int up = from + Vector3Int.up;
        Vector3Int down = from + Vector3Int.down;
        Vector3Int left = from + Vector3Int.left;
        Vector3Int right = from + Vector3Int.right;

        if (MaxCell.y > up.y && !CellIsOccupied(up))
            result.Add(up);

        if (MaxCell.x > right.x && !CellIsOccupied(right))
            result.Add(right);

        if (MinCell.y <= down.y && !CellIsOccupied(down))
            result.Add(down);

        if (MinCell.x <= left.x && !CellIsOccupied(left))
            result.Add(left);

        return result;
    }

    private bool CellIsOccupied(Vector3Int cell)
    {
        return m_tilemap.GetTile(cell) != null;
    }
}
