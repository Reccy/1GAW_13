using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapBoundsCompressor : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Tilemap>().CompressBounds();
    }
}
