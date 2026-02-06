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
    public Dictionary<Vector3, TeterisPrefab> cellDic = new Dictionary<Vector3, TeterisPrefab>();

    private Dictionary<Vector3Int, List<LineRenderer>> cellLines = new Dictionary<Vector3Int, List<LineRenderer>>();
    public Color highlightColor = Color.yellow;

    private int exitCount;
    public int ExitCount { get { return exitCount; } set { exitCount = value; OnChangeCount?.Invoke(exitCount); } }
    public event Action<int> OnChangeCount;

    public Func<List<Vector3>, List<Vector3>,TeterisPrefab, List<Vector3>> OnCheckCell;
    public event Action<List<Vector3>> OnDrawAddVec;
    public Func<Vector3, bool> OnCheck;
    public Func<List<Vector3>,TeterisPrefab, bool> OnCheckIsMine;

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
        OnCheckIsMine += CheckIsMine;
        DrawGridFromChildren();
    }

    private void OnDestroy()
    {
        OnDrawAddVec -= AddVector;
        OnCheckCell -= ChangeVector;
        OnChangeCount -= FinishCheck;
        OnCheck -= CheckPos;
        OnCheckIsMine -= CheckIsMine;
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

    private List<Vector3> ChangeVector(List<Vector3> checkCells, List<Vector3> prevCells, TeterisPrefab tetris)
    {
        List<Vector3> currentlyOccupiedByMe = new List<Vector3>();

        // 이전 위치들을 다시 사용 가능한 리스트(cellExitList)로 복구
        foreach (var pPos in prevCells)
        {
            // 전체 그리드(cellList)에 포함된 좌표이고, 아직 탈출 리스트에 없다면 추가
            if (ContainsVector(cellList, pPos) && !ContainsVector(cellExitList, pPos))
            {
                cellDic.Add(pPos, tetris);
                cellExitList.Add(pPos);
                UpdateCellVisual(pPos, false);
            }
        }

        // 새로운 위치들을 점유 (cellExitList에서 제거)
        foreach (var cPos in checkCells)
        {
            if (ContainsVector(cellExitList, cPos))
            {
                if(cellDic.TryGetValue(cPos, out TeterisPrefab value))
                    cellDic.Remove(cPos);

                RemoveVector(cellExitList, cPos);
                UpdateCellVisual(cPos, true);
                currentlyOccupiedByMe.Add(cPos);
            }
            else if (ContainsVector(cellList, cPos))
            {
                Debug.LogWarning($"이미 점유된 셀 또는 중복 점유 시도: {cPos}");
            }
        }

        ExitCount = cellExitList.Count;
        return currentlyOccupiedByMe;
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

    private bool CheckIsMine(List<Vector3> list, TeterisPrefab tetris)
    {
        if (cellDic == null && tetris == null)
            return false;

        for(int i =0; i < list.Count; i++)
        {
            if (cellDic.TryGetValue(list[i], out TeterisPrefab value))
            {
                if (value == tetris && cellList.Contains(list[i]) && !cellExitList.Contains(list[i]))
                    continue;
            }
            else
            {
                return false;
            }
        }

        return true;

        //if (tetris == null && tetris.curInPos == null)
        //    return false;

        //for(int i =0; i < tetris.curInPos.Length; i++)
        //{
        //    if (cellList.Contains(tetris.curInPos[i]) && 
        //        !cellExitList.Contains(tetris.curInPos[i]) &&
        //        cellDic[tetris.curInPos[i]] == tetris)
        //    {
        //        continue;
        //    }
        //    else
        //    {
        //        return false;
        //    }
        //}

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

        if(cellDic != null)
            cellDic.Clear();

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