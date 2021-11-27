using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Reccy.DebugExtensions;

public class ScannerBot : MonoBehaviour
{
    [SerializeField] private float m_speed = 5.0f;
    [SerializeField] private int m_scanRadius = 5;
    [SerializeField] private Collider2D m_collider;

    private LevelManager m_levelManager;
    private Rigidbody2D m_rb;
    private BotAI m_AI;

    private List<Vector2Int> m_patrolRoute;
    private int m_patrolIdx = 0;

    private Vector2Int DestinationCellPosition => m_patrolRoute[m_patrolIdx];
    private Vector3 DestinationWorldPosition => m_levelManager.WorldPosition((Vector3Int)DestinationCellPosition);

    private LevelTile m_currentLevelTile;

    private void Awake()
    {
        m_AI = FindObjectOfType<BotAI>();
        m_levelManager = FindObjectOfType<LevelManager>();
        m_rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        UpdatePath(m_levelManager.CellPosition(transform.position));
    }

    private void UpdatePath(Vector3Int targetPosition)
    {
        m_patrolRoute = m_AI.FindPath(m_levelManager.CellPosition(transform.position), targetPosition);

        // Set the patrolIdx as closest route
        float closestDistance = float.MaxValue;

        for (int i = 0; i < m_patrolRoute.Count; ++i)
        {
            Vector2Int currentPosition = (Vector2Int)m_levelManager.CellPosition(transform.position);
            float dist = Vector2Int.Distance(m_patrolRoute[i], currentPosition);

            if (dist < closestDistance)
            {
                closestDistance = dist;
                m_patrolIdx = i;
            }
        }
    }

    private void FixedUpdate()
    {
        m_rb.velocity = (DestinationWorldPosition - transform.position).normalized * m_speed * Time.deltaTime;

        if (Vector3.Distance(DestinationWorldPosition, transform.position) < 0.2f)
        {
            m_currentLevelTile = m_levelManager.GetTileInfo(DestinationCellPosition);

            if (!(m_patrolIdx == m_patrolRoute.Count - 1))
                m_patrolIdx++;
            else
                m_rb.velocity = Vector2Int.zero;
        }
        
        m_AI.ScanForOre(DestinationCellPosition, m_scanRadius);
    }

    private void Update()
    {
        // Input

        if (Input.GetMouseButtonDown(1))
        {
            var point = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var cellPos = m_levelManager.CellPosition(point);

            if (m_levelManager.IsInFogOfWar(cellPos))
                return;

            if (m_levelManager.GetTileInfo(cellPos) != null)
                return;

            UpdatePath(cellPos);
        }

        // Debug Draw

        for (int i = 1; i < m_patrolRoute.Count; ++i)
        {
            Debug2.DrawArrow(m_levelManager.WorldPosition(m_patrolRoute[i - 1]), m_levelManager.WorldPosition(m_patrolRoute[i]), Color.green);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            m_patrolIdx--;
            transform.position = DestinationWorldPosition;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            m_patrolIdx++;
            transform.position = DestinationWorldPosition;
        }
    }
}
