using UnityEngine;

public class LevelTile
{
    private TileType m_type;
    public TileType TileType => m_type;

    private int m_maxHp;
    public int MaxHP => m_maxHp;

    public int HP { get; set; }

    private bool m_breakable;
    public bool Breakable => m_breakable;

    public LevelTile(TileType type, int hp, bool breakable)
    {
        m_type = type;
        HP = hp;
        m_maxHp = hp;
        m_breakable = breakable;
    }

    public override string ToString()
    {
        return $"LevelTile\nType: {m_type}\nHP: {HP} (Max: {m_maxHp})\nBreakable: {m_breakable}";
    }
}
