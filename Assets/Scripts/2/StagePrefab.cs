using System.Collections.Generic;
using UnityEngine;

public class StagePrefab : MonoBehaviour
{
    private List<Vector3> blockPositions = new List<Vector3>();
    private List<Sprite> blockSprites = new List<Sprite>();

    public IReadOnlyList<Vector3> BlockPositions => blockPositions;
    public IReadOnlyList<Sprite> BlockSprites => blockSprites;

    public void Init()
    {
        blockPositions.Clear();
        blockSprites.Clear();

        foreach (Transform tetris in transform)
        {
            foreach (Transform block in tetris)
            {
                SpriteRenderer sr = block.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                blockPositions.Add(block.position);
                blockSprites.Add(sr.sprite);
            }
        }
    }
}
