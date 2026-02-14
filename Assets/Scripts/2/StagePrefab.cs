using System.Collections.Generic;
using UnityEngine;

public class StagePrefab : MonoBehaviour
{
    [SerializeField] private TeterisBlock[] tetrisSO;
    public TeterisBlock[] TetrisSO => tetrisSO;

    [ SerializeField] private Transform[] tetrisPrefabPos;
    public Transform[] TetrisPrefabPos => tetrisPrefabPos;

    [ SerializeField] private int[] tetrisRotIndex;
    public int[] TetrisRotIndex => tetrisRotIndex;


    private List<Vector3> blockPositions = new List<Vector3>();
    private List<Sprite> blockSprites = new List<Sprite>();
    public int[] stageMoveLevel;
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
