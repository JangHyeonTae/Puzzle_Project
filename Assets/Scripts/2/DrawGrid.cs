using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawGrid : Singleton<DrawGrid>
{
    public Grid grid;
    public Color gridColor = Color.cyan;
    public Color highlightColor = Color.yellow;
    public float lineWidth = 0.05f;

    public List<Vector3> cellList = new List<Vector3>();
    public List<Vector3> cellExitList = new List<Vector3>();
    public List<Vector3> outList = new List<Vector3>();

    // 진실 데이터
    public Dictionary<Vector3, TeterisPrefab> cellDic = new Dictionary<Vector3, TeterisPrefab>();

    private Dictionary<Vector3Int, List<LineRenderer>> cellLines = new Dictionary<Vector3Int, List<LineRenderer>>();

    public Func<List<Vector3>, List<Vector3>, TeterisPrefab, List<Vector3>> OnCheckCell;
    public Func<Vector3, bool> OnCheck;
    public Func<List<Vector3>, TeterisPrefab, bool> OnCheckIsMine;
    public event Action<int> OnChangeCount;

    private int exitCount;
    public int ExitCount
    {
        get => exitCount;
        set
        {
            exitCount = value;
            OnChangeCount?.Invoke(exitCount);
        }
    }

    void Start()
    {
        if (grid == null)
            grid = FindObjectOfType<Grid>();

        OnCheckCell += ChangeVector;
        OnCheck += CheckPos;
        OnCheckIsMine += CheckIsMine;
        OnChangeCount += FinishCheck;

        DrawGridFromChildren();
    }

    private void OnDestroy()
    {
        OnCheckCell -= ChangeVector;
        OnCheck -= CheckPos;
        OnCheckIsMine -= CheckIsMine;
        OnChangeCount -= FinishCheck;
    }

    async void DrawGridFromChildren()
    {
        ClearAll();

        GameObject stagePrefab = await DataManager.Instance.LoadStagePrefab(StageManager.Instance.curStage);
        if (stagePrefab == null) return;

        HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();

        foreach (Transform child in stagePrefab.transform)
        {
            foreach (Transform block in child)
            {
                Vector3Int cell = grid.WorldToCell(block.position);
                cellPositions.Add(cell);

                Vector3 center = grid.GetCellCenterWorld(cell);
                center.z = 0;
                cellList.Add(center);
            }
        }

        foreach (var cell in cellPositions)
            DrawCellSquare(cell);

        RebuildCellExitList();
        RebuildOutList();
    }

    void DrawCellSquare(Vector3Int cellPos)
    {
        Vector3 size = grid.cellSize;
        Vector3 bl = grid.CellToWorld(cellPos);
        Vector3 br = bl + new Vector3(size.x, 0);
        Vector3 tl = bl + new Vector3(0, size.y);
        Vector3 tr = bl + new Vector3(size.x, size.y);

        List<LineRenderer> lines = new List<LineRenderer>
        {
            CreateLine(bl, br),
            CreateLine(br, tr),
            CreateLine(tr, tl),
            CreateLine(tl, bl)
        };

        cellLines[cellPos] = lines;
    }

    LineRenderer CreateLine(Vector3 a, Vector3 b)
    {
        GameObject go = new GameObject("GridLine");
        go.transform.SetParent(transform);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.sortingOrder = 100;
        lr.startColor = gridColor;
        lr.endColor = gridColor;

        a.z = b.z = 0;
        lr.SetPosition(0, a);
        lr.SetPosition(1, b);

        return lr;
    }

    private List<Vector3> ChangeVector(List<Vector3> checkCells, List<Vector3> prevCells, TeterisPrefab tetris)
    {
        List<Vector3> occupied = new List<Vector3>();

        // 이전 점유 제거
        foreach (var prev in prevCells)
        {
            if (cellDic.TryGetValue(prev, out var owner) && owner == tetris)
                cellDic.Remove(prev);
        }

        // 새 점유 등록
        foreach (var pos in checkCells)
        {
            if (cellDic.TryGetValue(pos, out var owner) && owner != tetris)
                continue;

            cellDic[pos] = tetris;
            occupied.Add(pos);
        }

        RebuildCellExitList();
        RebuildOutList();

        ExitCount = cellExitList.Count;
        return occupied;
    }

    private void RebuildCellExitList()
    {
        cellExitList.Clear();

        foreach (var cell in cellList)
        {
            if (!cellDic.ContainsKey(cell))
            {
                cellExitList.Add(cell);
                UpdateCellVisual(cell, false);
            }
            else
            {
                UpdateCellVisual(cell, true);
            }
        }
    }

    private void RebuildOutList()
    {
        outList.Clear();

        foreach (var pair in cellDic)
        {
            if (!cellList.Contains(pair.Key))
                outList.Add(pair.Key);
        }
    }

    private bool CheckPos(Vector3 pos)
    {
        return !cellDic.ContainsKey(pos);
    }

    private bool CheckIsMine(List<Vector3> cells, TeterisPrefab tetris)
    {
        foreach (var pos in cells)
        {
            if (!cellDic.TryGetValue(pos, out var owner) || owner != tetris)
                return false;
        }
        return true;
    }

    private void UpdateCellVisual(Vector3 pos, bool occupied)
    {
        Vector3Int cell = grid.WorldToCell(pos);
        if (!cellLines.TryGetValue(cell, out var lines)) return;

        Color c = occupied ? highlightColor : gridColor;
        foreach (var lr in lines)
        {
            lr.startColor = c;
            lr.endColor = c;
        }
    }

    private void FinishCheck(int _)
    {
        if (cellExitList.Count == 0 && outList.Count == 0)
        {
            StageManager.Instance.UpStage();
            DrawGridFromChildren();
        }
    }

    private void ClearAll()
    {
        cellList.Clear();
        cellExitList.Clear();
        outList.Clear();
        cellDic.Clear();
        cellLines.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }
}
