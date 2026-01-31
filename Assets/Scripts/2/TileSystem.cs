using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileSystem : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase tile; 
    [SerializeField] private TeterisBlock tileData;

    void Start()
    {
        GenerateTilemap();
    }

    public void GenerateTilemap()
    {
        if (tilemap == null || tile == null || tileData == null)
        {
            Debug.LogError("필요한 참조가 설정되지 않았습니다!");
            return;
        }

        tilemap.ClearAllTiles();

        foreach (Vector2 position in tileData.posVectors[0].blockPos)
        {
            Vector3Int tilePosition = Vector3Int.FloorToInt(position);
            tilemap.SetTile(tilePosition, tile);
        }
    }

    [ContextMenu("Generate Tilemap")]
    void GenerateFromContext()
    {
        GenerateTilemap();
    }
}
