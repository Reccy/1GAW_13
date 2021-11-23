using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Reccy.DebugExtensions;
using Reccy.ScriptExtensions;


// Resolves collisions against an obstacle tilemap and moves according to attached rigidbody velocity
[RequireComponent(typeof(Rigidbody2D))]
public class TilemapCollider : MonoBehaviour
{
    private Rigidbody2D m_rb;

    [Header("Settings")]
    [SerializeField] private BoxCollider2D m_collisionBox;
    [SerializeField] private Tilemap m_obstacleTilemap;
    public void SetTilemap(Tilemap tilemap) => m_obstacleTilemap = tilemap;

    private const float HALF_TILE_SIZE = 0.5f;
    private const float GROUNDED_TOLERANCE = 0.0001f;

    private bool m_isGrounded = true;
    public bool IsGrounded => m_isGrounded;

    private Bounds AABB => m_collisionBox.bounds;
    private float LeftEdgeX => AABB.min.x;
    private float RightEdgeX => AABB.max.x;
    private float UpEdgeY => AABB.max.y;
    private float DownEdgeY => AABB.min.y;

    private bool IsMovingRight => m_rb.velocity.x > 0;
    private bool IsMovingLeft => m_rb.velocity.x < 0;
    private bool IsMovingUp => m_rb.velocity.y > 0;
    private bool IsMovingDown => m_rb.velocity.y < 0;

    private Vector3Int m_currentCellPosition;
    public Vector3Int CurrentCellPosition => m_currentCellPosition;

    public delegate void OnTilemapCollidedEvent();
    public OnTilemapCollidedEvent OnTilemapCollided;

    #region DEBUG
    [Header("Debug")]
    [SerializeField] private bool m_drawArrows = false;

    private void DebugDrawArrow(Vector3Int cellCoords, Color c)
    {
        if (m_drawArrows)
            Debug2.DrawArrow(m_obstacleTilemap.GetCellCenterWorld(CurrentCellPosition), m_obstacleTilemap.GetCellCenterWorld(cellCoords), c);
    }
    #endregion

    private void Awake()
    {
        m_rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        m_currentCellPosition = m_obstacleTilemap.WorldToCell(transform.position);

        ResolveCollisions();

        IsGroundedCheck();

        m_rb.MovePosition(m_rb.position + new Vector2(m_rb.velocity.x, m_rb.velocity.y));
    }

    private void ResolveCollisions()
    {
        Vector3Int xCell = CurrentCellPosition;
        Vector3Int yCell = CurrentCellPosition;

        Vector3Int topRightCell = CurrentCellPosition + Vector3Int.up + Vector3Int.right;
        Vector3Int topLeftCell = CurrentCellPosition + Vector3Int.up + Vector3Int.left;
        Vector3Int bottomLeftCell = CurrentCellPosition + Vector3Int.down + Vector3Int.left;
        Vector3Int bottomRightCell = CurrentCellPosition + Vector3Int.down + Vector3Int.right;

        if (IsMovingUp)
        {
            yCell += Vector3Int.up;
        }
        else
        {
            yCell += Vector3Int.down;
        }

        if (IsMovingRight)
        {
            xCell += Vector3Int.right;
        }
        else
        {
            xCell += Vector3Int.left;
        }

        DebugDrawArrow(xCell, Color.blue);
        DebugDrawArrow(yCell, Color.red);

        ResolveCellCollision(xCell);
        ResolveCellCollision(yCell);

        if (IsMovingUp || IsMovingLeft)
        {
            DebugDrawArrow(topLeftCell, Color.green);
            ResolveCellCollision(topLeftCell);
        }

        if (IsMovingUp || IsMovingRight)
        {
            DebugDrawArrow(topRightCell, Color.green);
            ResolveCellCollision(topRightCell);
        }

        if (IsMovingDown || IsMovingRight)
        {
            DebugDrawArrow(bottomRightCell, Color.green);
            ResolveCellCollision(bottomRightCell);
        }

        if (IsMovingDown || IsMovingLeft)
        {
            DebugDrawArrow(bottomLeftCell, Color.green);
            ResolveCellCollision(bottomLeftCell);
        }
    }

    private void ResolveCellCollision(Vector3Int nextCellPos)
    {
        var otherTile = m_obstacleTilemap.GetTile(nextCellPos);

        float overlapCorrectionX = 0;
        float overlapCorrectionY = 0;
        float absOverlapX = 0;
        float absOverlapY = 0;
        float otherCenterX = m_obstacleTilemap.GetCellCenterWorld(nextCellPos).x;
        float otherCenterY = m_obstacleTilemap.GetCellCenterWorld(nextCellPos).y;

        var cellDir = GetCellDir(nextCellPos);

        bool correctedOverlap = false;

        if (otherTile != null)
        {
            if (cellDir.x == -1) // Cell is to our left
            {
                var otherRightEdge = otherCenterX + HALF_TILE_SIZE;
                absOverlapX = Mathf.Max(otherRightEdge - (LeftEdgeX + m_rb.velocity.x), 0);
                overlapCorrectionX = absOverlapX;
                correctedOverlap = true;
            }
            else if (cellDir.x == 1) // Cell is to our right
            {
                var otherLeftEdge = otherCenterX - HALF_TILE_SIZE;
                absOverlapX = Mathf.Max((RightEdgeX + m_rb.velocity.x) - otherLeftEdge, 0);
                overlapCorrectionX = -absOverlapX;
                correctedOverlap = true;
            }
            else
            {
                absOverlapX = 1;
                overlapCorrectionX = 1;
            }

            if (cellDir.y == 1) // Cell is above
            {
                var otherDownEdge = otherCenterY - HALF_TILE_SIZE;
                absOverlapY = Mathf.Max((UpEdgeY + m_rb.velocity.y) - otherDownEdge, 0);
                overlapCorrectionY = -absOverlapY;
                correctedOverlap = true;
            }
            else if (cellDir.y == -1) // Cell is below
            {
                var otherUpEdge = otherCenterY + HALF_TILE_SIZE;
                absOverlapY = Mathf.Max(otherUpEdge - (DownEdgeY + m_rb.velocity.y), 0);
                overlapCorrectionY = absOverlapY;
                correctedOverlap = true;
            }
            else
            {
                absOverlapY = 1;
                overlapCorrectionY = 1;
            }
        }

        // Correct cell on the axis of least displacement
        if (absOverlapX > absOverlapY)
        {
            if (OnTilemapCollided != null && correctedOverlap && overlapCorrectionY != 0)
                OnTilemapCollided();

            m_rb.velocity = new Vector2(m_rb.velocity.x, m_rb.velocity.y + overlapCorrectionY);
        }
        else
        {
            if (OnTilemapCollided != null && correctedOverlap && overlapCorrectionY != 0)
                OnTilemapCollided();

            m_rb.velocity = new Vector2(m_rb.velocity.x + overlapCorrectionX, m_rb.velocity.y);
        }
    }

    private Vector3Int GetCellDir(Vector3Int nextCellPos)
    {
        return nextCellPos - CurrentCellPosition;
    }

    private void IsGroundedCheck()
    {
        Vector3Int downLeftTilePos = CurrentCellPosition + Vector3Int.down + Vector3Int.left;
        Vector3Int downTilePos = CurrentCellPosition + Vector3Int.down;
        Vector3Int downRightTilePos = CurrentCellPosition + Vector3Int.down + Vector3Int.right;

        if (m_obstacleTilemap.GetTile(downTilePos) != null)
        {
            var expr = (m_obstacleTilemap.GetCellCenterWorld(downTilePos).y + HALF_TILE_SIZE);

            if (DownEdgeY < expr || Mathf2.Approximately(DownEdgeY, expr, GROUNDED_TOLERANCE))
            {
                m_isGrounded = true;
                return;
            }
        }

        if (m_obstacleTilemap.GetTile(downLeftTilePos) != null)
        {
            if (LeftEdgeX < (m_obstacleTilemap.GetCellCenterWorld(downLeftTilePos).x + HALF_TILE_SIZE))
            {
                var expr = (m_obstacleTilemap.GetCellCenterWorld(downLeftTilePos).y + HALF_TILE_SIZE);

                if (DownEdgeY < expr || Mathf2.Approximately(DownEdgeY, expr, GROUNDED_TOLERANCE))
                {
                    m_isGrounded = true;
                    return;
                }
            }
        }

        if (m_obstacleTilemap.GetTile(downRightTilePos) != null)
        {
            if (RightEdgeX > (m_obstacleTilemap.GetCellCenterWorld(downRightTilePos).x - HALF_TILE_SIZE))
            {
                var expr = (m_obstacleTilemap.GetCellCenterWorld(downRightTilePos).y + HALF_TILE_SIZE);

                if (DownEdgeY < expr || Mathf2.Approximately(DownEdgeY, expr, GROUNDED_TOLERANCE))
                {
                    m_isGrounded = true;
                    return;
                }
            }
        }

        m_isGrounded = false;
    }
}
