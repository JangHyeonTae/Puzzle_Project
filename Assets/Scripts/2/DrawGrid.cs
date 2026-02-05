using System;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class DrawGrid : Singleton<DrawGrid>
{
    public Grid grid;
    public Color gridColor = Color.cyan;
    public float lineWidth = 0.05f;
    public List<Vector3> cellList;
    public List<Vector3> cellExitList;

    private Dictionary<Vector3Int, List<LineRenderer>> cellLines = new Dictionary<Vector3Int, List<LineRenderer>>();
    public Color highlightColor = Color.yellow;

    private int exitCount;
    public int ExitCount { get { return exitCount; } set { exitCount = value; OnChangeCount?.Invoke(exitCount); } }
    public event Action<int> OnChangeCount;

    public Action<List<Vector3>, List<Vector3>> OnCheckCell;
    public event Action<List<Vector3>> OnDrawAddVec;
    public Func<Vector3, bool> OnCheck;

    private bool Vector3Equals(Vector3 a, Vector3 b)
    {
        const float epsilon = 0.001f;
        return Mathf.Abs(a.x - b.x) < epsilon &&
               Mathf.Abs(a.y - b.y) < epsilon &&
               Mathf.Abs(a.z - b.z) < epsilon;
    }

    private bool ContainsVector(List<Vector3> list, Vector3 target)
    {
        foreach (var v in list)
        {
            if (Vector3Equals(v, target))
                return true;
        }
        return false;
    }

    private bool RemoveVector(List<Vector3> list, Vector3 target)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (Vector3Equals(list[i], target))
            {
                list.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    void Start()
    {
        if (grid == null)
            grid = FindObjectOfType<Grid>();

        cellList = new List<Vector3>();
        cellExitList = new List<Vector3>();

        OnDrawAddVec += AddVector;
        OnCheckCell += ChangeVector;
        OnChangeCount += FinishCheck;
        OnCheck += CheckPos;
        DrawGridFromChildren();
    }

    private void OnDestroy()
    {
        OnDrawAddVec -= AddVector;
        OnCheckCell -= ChangeVector;
        OnChangeCount -= FinishCheck;
        OnCheck -= CheckPos;
    }

    async void DrawGridFromChildren()
    {
        DrawMapClear();
        GameObject stagePrefab = await DataManager.Instance.LoadStagePrefab(StageManager.Instance.curStage);

        if (stagePrefab == null)
        {
            return;
        }

        if (grid == null)
        {
            grid = FindObjectOfType<Grid>();
            if (grid == null)
            {
                DataManager.Instance.ReleaseStagePrefab(stagePrefab);
                return;
            }
        }

        float cellSizeX = grid.cellSize.x;
        float cellSizeY = grid.cellSize.y;

        HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();

        foreach (Transform child in stagePrefab.transform)
        {
            for (int i = 0; i < child.childCount; i++)
            {
                Transform block = child.GetChild(i);
                Vector3Int cellPos = grid.WorldToCell(block.position);
                cellPositions.Add(cellPos);

                Vector3 cellCenterWorld = grid.CellToWorld(cellPos);
                cellCenterWorld.z = 0;
                cellList.Add(cellCenterWorld);
            }
        }

        if (cellPositions.Count == 0)
        {
            Debug.LogWarning("그릴 블록이 없습니다!");
            return;
        }

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
        Vector3Int cellInt = grid.WorldToCell(cellPos);

        if (cellLines.TryGetValue(cellInt, out List<LineRenderer> lines))
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
        for (int i = 0; i < prevCells.Count; i++)
        {
            if (ContainsVector(cellList, prevCells[i]) && !ContainsVector(cellExitList, prevCells[i]))
            {
                cellExitList.Add(prevCells[i]);
                UpdateCellVisual(prevCells[i], false);
            }
        }

        for (int i = 0; i < checkCells.Count; i++)
        {
            if (ContainsVector(cellExitList, checkCells[i]))
            {
                RemoveVector(cellExitList, checkCells[i]);
                UpdateCellVisual(checkCells[i], true);
            }
            else if (!ContainsVector(cellList, checkCells[i]))
            {
                Debug.LogWarning($"유효하지 않은 셀 위치: {checkCells[i]}");
            }
        }

        ExitCount = cellExitList.Count;
    }

    private bool CheckPos(Vector3 cellPos)
    {
        if (cellList.Contains(cellPos))
        {
            if (cellExitList.Contains(cellPos))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        return true;
    }

    private void FinishCheck(int value)
    {
        if (value == 0)
        {
            StageManager.Instance.UpStage();
            DrawGridFromChildren();
        }
    }

    private void DrawMapClear()
    {
        if (cellList != null)
            cellList.Clear();

        if (cellExitList != null)
            cellExitList.Clear();

        if (cellLines != null)
            cellLines.Clear();

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