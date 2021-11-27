using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Reccy.DebugExtensions;

public class DigBot : MonoBehaviour
{
    [SerializeField] private float m_speed = 3.0f;
    [SerializeField] private Collider2D m_collider;

    private LevelManager m_levelManager;
    private Rigidbody2D m_rb;
    private BotAI m_AI;

    private List<Vector2Int> m_path;
    private int m_pathIdx = 0;

    private Vector2Int DestinationCellPosition => m_path[m_pathIdx];
    private Vector3 DestinationWorldPosition => m_levelManager.WorldPosition((Vector3Int)DestinationCellPosition);

    private Vector2Int CurrentCellPosition => (Vector2Int)m_levelManager.CellPosition(transform.position);
    private Vector3 CurrentWorldPosition => m_levelManager.WorldPosition((Vector3Int)CurrentCellPosition);

    private LevelTile m_currentLevelTile;

    private Coroutine m_digCoroutine;
    private Vector2Int m_digTilePosition;

    private Vector2Int m_assignedDigPosition;

    public bool IsAssignedJob => m_assignedDigPosition != NULLV2;

    private readonly Vector2Int NULLV2 = Vector2Int.one * int.MaxValue;

    private void Awake()
    {
        m_AI = FindObjectOfType<BotAI>();
        m_levelManager = FindObjectOfType<LevelManager>();
        m_rb = GetComponent<Rigidbody2D>();
        m_assignedDigPosition = NULLV2;

        m_levelManager.OnLevelUpdate += UpdatePath;
        m_levelManager.OnTileDestroyed += OnTileDestroyed;

        UpdatePath();
    }

    private void UpdatePath()
    {
        if (m_assignedDigPosition == NULLV2)
        {
            m_path = new List<Vector2Int>();
            m_path.Add(CurrentCellPosition);
            m_pathIdx = 0;
            return;
        }

        m_path = m_AI.FindPath(m_levelManager.CellPosition(transform.position), (Vector3Int)m_assignedDigPosition);

        // Prevent backtracking on path when it updates
        if (m_path.Count > 0)
        {
            m_pathIdx = 1;
        }
        else
        {
            m_pathIdx = 0;
        }

        if (m_digTilePosition != NULLV2 && m_levelManager.GetTileInfo(m_digTilePosition) == null)
        {
            if (m_digCoroutine != null)
                StopCoroutine(m_digCoroutine);

            m_digTilePosition = NULLV2;
            m_digCoroutine = null;
        }
    }

    private void OnTileDestroyed(Vector2Int tile)
    {
        if (m_assignedDigPosition == tile)
        {
            m_assignedDigPosition = NULLV2;
            UpdatePath();
        }
    }

    public void AssignDigJob(Vector2Int tile)
    {
        m_assignedDigPosition = tile;
        UpdatePath();
    }

    private void Update()
    {
        // Input

        if (Input.GetMouseButtonDown(0))
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var hit = Physics2D.Raycast(ray.origin, ray.direction);
            
            if (hit.collider == m_collider)
            {
                //
            }
        }

        // Debug Drawing
        for (int i = 1; i < m_path.Count; ++i)
        {
            var from = m_levelManager.WorldPosition((Vector3Int)m_path[i - 1]);
            var to = m_levelManager.WorldPosition((Vector3Int)m_path[i]);
            Debug2.DrawArrow(from, to, Color.green);
        }

        Debug2.DrawArrow(transform.position, transform.position + (Vector3)m_rb.velocity, Color.blue);
    }

    private void FixedUpdate()
    {
        if (m_assignedDigPosition == NULLV2)
        {
            m_rb.velocity = Vector2.zero;
            return;
        }

        m_rb.velocity = (DestinationWorldPosition - transform.position).normalized * m_speed * Time.deltaTime;

        if (Vector3.Distance(DestinationWorldPosition, transform.position) < 0.2f)
        {
            if (m_pathIdx < m_path.Count - 1)
            {
                m_pathIdx++;
                m_currentLevelTile = m_levelManager.GetTileInfo(DestinationCellPosition);

                if (m_currentLevelTile != null && m_currentLevelTile.Breakable)
                {
                    m_digTilePosition = DestinationCellPosition;
                    DigTile();
                }
            }
            else
            {
                m_rb.velocity = Vector2.zero;
            }
        }
    }

    private void DigTile()
    {
        if (m_digCoroutine != null)
            return;

        m_digCoroutine = StartCoroutine(DigTileCoroutine());
    }

    private IEnumerator DigTileCoroutine()
    {
        var tilePos = m_digTilePosition;

        while (m_levelManager.GetTileInfo(tilePos) != null && m_currentLevelTile.HP > 0)
        {
            m_levelManager.DigTile(tilePos);

            yield return new WaitForSeconds(0.8f);
        }

        m_digCoroutine = null;
    }
}
