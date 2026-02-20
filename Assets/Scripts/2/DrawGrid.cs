using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DrawGrid : Singleton<DrawGrid>
{
    [SerializeField] private TeterisPrefab tetrisPrefabOrigin;

    public Grid grid;
    public Color gridColor = Color.cyan;
    public Color highlightColor = Color.yellow;
    public float lineWidth = 0.05f;
    public StagePrefab stagePrefab;

    public List<Vector3> cellList;
    public List<Vector3> cellExitList;
    public List<Vector3> outList;

    // 진실 데이터
    public Dictionary<Vector3, TeterisPrefab> cellDic;

    private Dictionary<Vector3Int, List<LineRenderer>> cellLines;

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

    private CancellationTokenSource drawCts;

    void Start()
    {
        if (grid == null)
            grid = FindObjectOfType<Grid>();

        InitStage();
        OnCheckCell += ChangeVector;
        OnCheck += CheckPos;
        OnCheckIsMine += CheckIsMine;
        OnChangeCount += FinishCheck;

        DrawGridFromChildren().Forget();
    }

    private void OnDestroy()
    {
        drawCts?.Cancel();
        drawCts?.Dispose();

        OnCheckCell -= ChangeVector;
        OnCheck -= CheckPos;
        OnCheckIsMine -= CheckIsMine;
        OnChangeCount -= FinishCheck;
    }

    public async UniTask DrawGridFromChildren()
    {
        drawCts?.Cancel();
        drawCts?.Dispose();
        drawCts = new CancellationTokenSource();

        ClearAll();
        var token = drawCts.Token;

        await UniTask.WaitUntil(() => StageManager.Instance != null);
        await UniTask.WaitUntil(() => StageManager.Instance.tetrisPool != null);

        var inst = await DataManager.Instance
            .LoadStagePrefab(StageManager.Instance.curStage)
            .AttachExternalCancellation(token);

        if (token.IsCancellationRequested)
            return;

        stagePrefab = inst.GetComponent<StagePrefab>();
        if (stagePrefab == null)
            return;

        stagePrefab.Init();

        Array.Copy(stagePrefab.stageMoveLevel,
                   StageManager.Instance.curStageMoveLevel,
                   stagePrefab.stageMoveLevel.Length);

        HashSet<Vector3Int> cellPositions = new HashSet<Vector3Int>();

        foreach (var pos in stagePrefab.BlockPositions)
        {
            Vector3Int cell = grid.WorldToCell(pos);
            cellPositions.Add(cell);

            Vector3 center = grid.GetCellCenterWorld(cell);
            center.z = 0;
            cellList.Add(center);
        }
        await UniTask.WaitUntil(() =>
            StageManager.Instance != null &&
            StageManager.Instance.tetrisPool != null);
        SpawnStageTetris();

        foreach (var cell in cellPositions)
            DrawCellSquare(cell);

        RebuildCellExitList();
        RebuildOutList();

        StageManager.Instance.isStageChange = false;
    }

    private void InitStage()
    {
        cellList = new List<Vector3>();
        cellExitList = new List<Vector3>();
        outList = new List<Vector3>();

        cellDic = new Dictionary<Vector3, TeterisPrefab>();
        cellLines = new Dictionary<Vector3Int, List<LineRenderer>>();
    }

    private void SpawnStageTetris()
    {
        if (stagePrefab == null)
        {
            Debug.LogError("stagePrefab is NULL");
            return;
        }


        var soArr = stagePrefab.TetrisSO;
        var points = stagePrefab.TetrisPrefabPos;
        var rotArr = stagePrefab.TetrisRotIndex;

        if (soArr == null || points == null || rotArr == null)
        {
            Debug.LogError("StagePrefab arrays are NULL");
            return;
        }

        if (soArr.Length != points.Length || soArr.Length != rotArr.Length)
        {
            Debug.LogError($"Array length mismatch: SO={soArr.Length}, Pos={points.Length}, Rot={rotArr.Length}");
            return;
        }

        if (StageManager.Instance.tetrisPool == null)
        {
            Debug.LogError("Pool 없음");
        }


        for (int i = 0; i < soArr.Length; i++)
        {
            if (soArr[i] == null || points[i] == null)
            {
                Debug.LogError($"Null index {i}");
                continue;
            }

            var prefab = StageManager.Instance.tetrisPool.GetPooled() as TeterisPrefab;

            if (prefab == null)
            {
                Debug.LogError("Pool returned null");
                continue;
            }

            prefab.transform.position = points[i].localPosition;
            prefab.Init(soArr[i], grid, false, rotArr[i]);

            if (prefab.childrenPositions == null)
            {
                Debug.LogError($"childrenPositions NULL at index {i}");
                continue;
            }
        }
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
        if (outList.Count > 0)
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

    private void FinishCheck(int value)
    {
        if (StageManager.Instance == null)
            return;

        if (StageManager.Instance.isStageChange)
            return;

        if (value == 0 && outList.Count == 0)
        {
            StageManager.Instance.isStageChange = true;
            StageManager.Instance.ClearStage();


            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
        }
    }

    private void ClearAll()
    {
        cellList.Clear();
        cellExitList.Clear();
        cellDic.Clear();
        cellLines.Clear();
        outList.Clear();

    }
}
