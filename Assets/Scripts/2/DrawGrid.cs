using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.ReorderableList;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class DrawGrid : Singleton<DrawGrid>
{
    public Grid grid;
    public Color gridColor = Color.cyan;
    public float lineWidth = 0.05f;
    public List<Vector3> cellList; // 이미 그려진 셀들
    public List<Vector3> cellExitList; // 현재 사용 가능한 셀들

    //하이라이트
    private Dictionary<Vector3, List<LineRenderer>> cellLines = new Dictionary<Vector3, List<LineRenderer>>();
    public Color highlightColor = Color.yellow;

    private int exitCount;
    public int ExitCount { get { return exitCount; } set { exitCount = value;  OnChangeCount?.Invoke(exitCount); } }
    public event Action<int> OnChangeCount;

    public Action<List<Vector3>, List<Vector3>> OnCheckCell;
    public event Action<List<Vector3>> OnDrawAddVec;

    void Start()
    {
        //curStage Json에서 받아오기
        if (grid == null)
            grid = FindObjectOfType<Grid>();

        cellList = new List<Vector3>();

        OnDrawAddVec += AddVector;
        OnCheckCell += ChangeVector;
        OnChangeCount += FinishCheck;
        DrawGridFromChildren();
    }

    private void OnDestroy()
    {
        OnDrawAddVec -= AddVector;
        OnCheckCell -= ChangeVector;
        OnChangeCount -= FinishCheck;
    }

    async void DrawGridFromChildren()
    {
        DrawMapClear();
        GameObject stagePrefab = await DataManager.Instance.LoadStagePrefab(StageManager.Instance.curStage);
        
        if (stagePrefab == null)
        {
            Debug.LogWarning("스테이지 프리팹을 로드할 수 없습니다!");
            return;
        }

        if (grid == null)
        {
            grid = FindObjectOfType<Grid>();
            if (grid == null)
            {
                Debug.LogWarning("Grid를 찾을 수 없습니다!");
                DataManager.Instance.ReleaseStagePrefab(stagePrefab);
                return;
            }
        }

        float cellSizeX = grid.cellSize.x;
        float cellSizeY = grid.cellSize.y;

        // 모든 자식 블록의 셀 좌표 수집
        HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();

        foreach (Transform child in stagePrefab.transform)
        {
            for (int i = 0; i < child.childCount; i++)
            {
                Transform block = child.GetChild(i);
                Vector3Int cellPos = grid.WorldToCell(block.position);
                cellPositions.Add(cellPos);
                cellList.Add(new Vector3(cellPos.x, cellPos.y, cellPos.z));
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

        OnDrawAddVec.Invoke(cellList);
    }
    
    void DrawCellSquare(Vector3Int cellPos, float cellSizeX, float cellSizeY)
    {
        Vector3 bottomLeft = grid.CellToWorld(cellPos);
        Vector3 bottomRight = bottomLeft + new Vector3(cellSizeX, 0, 0);
        Vector3 topLeft = bottomLeft + new Vector3(0, cellSizeY, 0);
        Vector3 topRight = bottomLeft + new Vector3(cellSizeX, cellSizeY, 0);

        // 생성된 라인들을 리스트에 담아 딕셔너리에 저장
        List<LineRenderer> lines = new List<LineRenderer>();
        lines.Add(CreateLine(bottomLeft, bottomRight));
        lines.Add(CreateLine(bottomRight, topRight));
        lines.Add(CreateLine(topRight, topLeft));
        lines.Add(CreateLine(topLeft, bottomLeft));

        cellLines[cellPos] = lines;
    }

    LineRenderer CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = gridColor;
        lr.endColor = gridColor;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.sortingOrder = 100;

        start.z = 0;
        end.z = 0;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        return lr;
    }
    private void AddVector(List<Vector3> cellPositions)
    {
        cellExitList = new List<Vector3>(cellPositions);
        ExitCount = cellExitList.Count;
    }
    private void UpdateCellVisual(Vector3 cellPos, bool isOccupied)
    {
        if (cellLines.TryGetValue(cellPos, out List<LineRenderer> lines))
        {
            Color targetColor = isOccupied ? highlightColor : gridColor;
            foreach (var lr in lines)
            {
                lr.startColor = targetColor;
                lr.endColor = targetColor;
            }
        }
    }

    private void ChangeVector(List<Vector3> checkCells, List<Vector3> prevCells)
    {
        for(int i =0; i < checkCells.Count; i++)
        {
            if (cellExitList.Contains(checkCells[i]))
            {
                cellExitList.Remove(checkCells[i]);
                ExitCount--;
                UpdateCellVisual(checkCells[i], true);
            }
            else
            {
                if (cellList.Contains(prevCells[i]))
                {
                    cellExitList.Add(prevCells[i]);
                    ExitCount = cellExitList.Count;
                    UpdateCellVisual(prevCells[i], false);
                }
                
                // 이미 List에 존재하는 테트리스 cell
            }


        }
    }


    private void FinishCheck(int value)
    {
        if (value == 0)
        {
            StageManager.Instance.UpStage();
            Debug.Log("스테이지 끝");
            DrawGridFromChildren();
        }
    }

    private void DrawMapClear()
    {
        if (cellList != null)
            cellList.Clear();

        if (cellExitList != null)
            cellExitList.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
