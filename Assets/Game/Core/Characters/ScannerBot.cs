using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Reccy.DebugExtensions;

public class ScannerBot : MonoBehaviour
{
    [SerializeField] private float m_speed = 5.0f;

    private LevelManager m_levelManager;
    private Rigidbody2D m_rb;
    private BotAI m_AI;

    private List<Vector2Int> m_patrolRoute;
    private int m_patrolIdx = 0;

    private Vector2Int DestinationCellPosition => m_patrolRoute[m_patrolIdx];
    private Vector3 DestinationWorldPosition => m_levelManager.WorldPosition((Vector3Int)DestinationCellPosition);

    private Vector3Int m_targetPosition;

    private LevelTile m_currentLevelTile;

    private bool m_movePositive = true;

    private void Awake()
    {
        m_AI = FindObjectOfType<BotAI>();
        m_levelManager = FindObjectOfType<LevelManager>();
        m_rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        UpdatePath();
    }

    private void UpdatePath()
    {
        m_patrolRoute = m_AI.FindPatrolRouteForScanBot((Vector2Int)m_levelManager.CellPosition(transform.position));

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
            if (m_patrolIdx == 0)
                m_movePositive = true;

            if (m_patrolIdx == m_patrolRoute.Count - 1)
                m_movePositive = false;

            if (m_movePositive)
                m_patrolIdx++;
            else
                m_patrolIdx--;

            m_currentLevelTile = m_levelManager.GetTileInfo(DestinationCellPosition);
        }
    }

    private void Update()
    {
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
