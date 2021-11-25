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

    private List<Vector2Int> m_path;
    private int m_pathIdx = 0;

    private Vector2Int DestinationCellPosition => m_path[m_pathIdx];
    private Vector3 DestinationWorldPosition => m_levelManager.WorldPosition((Vector3Int)DestinationCellPosition);

    private Vector3Int m_targetPosition;

    private LevelTile m_currentLevelTile;

    private void Awake()
    {
        m_AI = FindObjectOfType<BotAI>();
        m_levelManager = FindObjectOfType<LevelManager>();
        m_rb = GetComponent<Rigidbody2D>();

        //m_levelManager.OnLevelUpdate += Updat
    }

    private void Start()
    {
        UpdatePath();
    }

    private void UpdatePath()
    {
        m_path = m_AI.FindPatrolRouteForScanBot((Vector2Int)m_levelManager.CellPosition(transform.position));

        // Prevent backtracking on path when it updates
        if (m_path.Count > 0)
        {
            m_pathIdx = 1;
        }
        else
        {
            m_pathIdx = 0;
        }
    }

    private void FixedUpdate()
    {
        return;

        m_rb.velocity = (DestinationWorldPosition - transform.position).normalized * m_speed * Time.deltaTime;

        if (Vector3.Distance(DestinationWorldPosition, transform.position) < 0.2f)
        {
            if (m_pathIdx < m_path.Count - 1)
            {
                m_pathIdx++;
                m_currentLevelTile = m_levelManager.GetTileInfo(DestinationCellPosition);
            }
            else
            {
                m_rb.velocity = Vector2.zero;
            }
        }
    }

    private void Update()
    {
        /*
        for (int i = 1; i < m_path.Count; ++i)
        {
            var from = m_levelManager.WorldPosition((Vector3Int)m_path[i - 1]);
            var to = m_levelManager.WorldPosition((Vector3Int)m_path[i]);
            Debug2.DrawArrow(from, to, Color.green);
        }

        Debug2.DrawArrow(transform.position, transform.position + (Vector3)m_rb.velocity, Color.blue);
        */

        /*
        foreach (var t in m_path)
        {
            Debug2.DrawCross(m_levelManager.WorldPosition((Vector3Int)t), Color.red);
        }
        */

        for (int i = 1; i < m_path.Count; ++i)
        {
            Debug2.DrawArrow(m_levelManager.WorldPosition(m_path[i - 1]), m_levelManager.WorldPosition(m_path[i]), Color.green);
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            m_pathIdx--;
            transform.position = DestinationWorldPosition;
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            m_pathIdx++;
            transform.position = DestinationWorldPosition;
        }
    }
}
