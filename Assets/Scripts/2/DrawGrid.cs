using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class DrawGrid : Singleton<DrawGrid>
{
    public Grid grid;
    public Transform targetObject;
    public Color gridColor = Color.cyan;
    public float lineWidth = 0.05f;
    public List<Vector3Int> cellList;

    public Action OnDrawCell;

    void Start()
    {
        if (grid == null)
            grid = FindObjectOfType<Grid>();

        cellList = new List<Vector3Int>();
        DrawGridFromChildren();
    }

    void DrawGridFromChildren()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target Object가 할당되지 않았습니다!");
            return;
        }

        if (grid == null)
        {
            Debug.LogWarning("Grid를 찾을 수 없습니다!");
            return;
        }

        float cellSizeX = grid.cellSize.x;
        float cellSizeY = grid.cellSize.y;

        // 모든 자식 블록의 셀 좌표 수집
        HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();

        foreach (Transform child in targetObject)
        {
            for (int i = 0; i < child.childCount; i++)
            {
                Transform block = child.GetChild(i);
                Vector3Int cellPos = grid.WorldToCell(block.position);
                cellPositions.Add(cellPos);
                cellList.Add(cellPos);
            }
        }

        if (cellPositions.Count == 0)
        {
            Debug.LogWarning("그릴 블록이 없습니다!");
            return;
        }

        // 각 셀마다 사각형 그리기
        foreach (Vector3Int cellPos in cellPositions)
        {
            DrawCellSquare(cellPos, cellSizeX, cellSizeY);
        }

    }

    void DrawCellSquare(Vector3Int cellPos, float cellSizeX, float cellSizeY)
    {
        // 셀의 왼쪽 하단 모서리
        Vector3 bottomLeft = grid.CellToWorld(cellPos);// + new Vector3(-0.25f, -0.25f, 0);

        // 4개의 모서리 좌표
        Vector3 bottomRight = bottomLeft + new Vector3(cellSizeX, 0, 0);
        Vector3 topLeft = bottomLeft + new Vector3(0, cellSizeY, 0);
        Vector3 topRight = bottomLeft + new Vector3(cellSizeX, cellSizeY, 0);

        // 4개의 선 그리기
        CreateLine(bottomLeft, bottomRight);  // 하단
        CreateLine(bottomRight, topRight);    // 오른쪽
        CreateLine(topRight, topLeft);        // 상단
        CreateLine(topLeft, bottomLeft);      // 왼쪽
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(transform);
        lineObj.transform.localPosition = Vector3.zero;

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = gridColor;
        lr.endColor = gridColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.sortingOrder = 100;

        // Z 좌표를 0으로 설정
        start.z = 0;
        end.z = 0;

        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}

