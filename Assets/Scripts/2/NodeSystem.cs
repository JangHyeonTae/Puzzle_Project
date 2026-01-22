using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class NodeSystem : MonoBehaviour
{
    [SerializeField] private Tilemap tilemap;

    private List<Node> nodeList;

    private void Awake()
    {
        nodeList = new List<Node>();

        
    }

    private void ResetNode()
    {

    }

    public void CreateMap(int curStage)
    {

    }
}

