using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Reccy.DebugExtensions;

public class DigBot : MonoBehaviour
{
    [SerializeField] private GameObject m_target;
    [SerializeField] private float m_speed = 3.0f;

    private LevelManager m_levelManager;
    private Rigidbody2D m_rb;
    private BotAI m_AI;

    private List<Vector2Int> m_path;
    private int m_pathIdx = 0;

    private Vector2Int DestinationCellPosition => m_path[m_pathIdx];
    private Vector3 DestinationWorldPosition => m_levelManager.WorldPosition((Vector3Int)DestinationCellPosition);

    private LevelTile m_currentLevelTile;

    private Coroutine m_digCoroutine;
    private Vector2Int m_digTilePosition;

    private readonly Vector2Int NULLV2 = Vector2Int.one * int.MaxValue;

    private void Awake()
    {
        m_AI = FindObjectOfType<BotAI>();
        m_levelManager = FindObjectOfType<LevelManager>();
        m_rb = GetComponent<Rigidbody2D>();

        m_levelManager.OnLevelUpdate += UpdatePath;
    }
    
    private void Start()
    {
        UpdatePath();
    }

    private void UpdatePath()
    {
        m_path = m_AI.FindPath(m_levelManager.CellPosition(transform.position), m_levelManager.CellPosition(m_target.transform.position));

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
            StopCoroutine(m_digCoroutine);
            m_digTilePosition = NULLV2;
            m_digCoroutine = null;
        }
    }

    private void Update()
    {
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
