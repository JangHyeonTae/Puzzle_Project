using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NodeSystem : Singleton<NodeSystem>
{
    [SerializeField] private Tilemap tilemap;

    private List<Node> nodeList;
    private List<Vector3> vecList;

    protected void Awake()
    {
        base.Awake();
        nodeList = new List<Node>();
        vecList = new List<Vector3>();
    }

    private void ResetVec()
    {
        vecList.Clear();

    }

    public void CreateMap(int curStage)
    {

    }
}

