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
    public List<Vector3> cellList; // ★ Vector3로 변경 (0.5 단위 지원)
    public List<Vector3> cellExitList; // ★ Vector3로 변경

    //하이라이트
    private Dictionary<Vector3Int, List<LineRenderer>> cellLines = new Dictionary<Vector3Int, List<LineRenderer>>();
    public Color highlightColor = Color.yellow;

    private int exitCount;
    public int ExitCount { get { return exitCount; } set { exitCount = value; OnChangeCount?.Invoke(exitCount); } }
    public event Action<int> OnChangeCount;

    public Action<List<Vector3>, List<Vector3>> OnCheckCell;  // ★ Vector3로 변경
    public event Action<List<Vector3>> OnDrawAddVec;  // ★ Vector3로 변경

    // ★ Vector3 비교를 위한 헬퍼 메서드
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
        //curStage Json에서 받아오기
        if (grid == null)
            grid = FindObjectOfType<Grid>();

        cellList = new List<Vector3>();
        cellExitList = new List<Vector3>();

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

                // ★ CellToWorld로 정확한 셀 중심 좌표 저장
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
        // ★ Vector3를 Vector3Int로 변환하여 딕셔너리 검색
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

    /// <summary>
    /// ★ 핵심 로직 수정: 이전 위치 복원 후 새 위치 점유
    /// </summary>
    private void ChangeVector(List<Vector3> checkCells, List<Vector3> prevCells)
    {
        Debug.Log($"=== ChangeVector 시작 ===");
        Debug.Log($"checkCells 개수: {checkCells.Count}, prevCells 개수: {prevCells.Count}");
        Debug.Log($"변경 전 cellExitList 개수: {cellExitList.Count}");

        // 1단계: 이전 위치들을 cellExitList에 다시 추가 (유효한 셀만)
        for (int i = 0; i < prevCells.Count; i++)
        {
            // cellList에 있는 유효한 셀이고, cellExitList에 없다면 추가
            if (ContainsVector(cellList, prevCells[i]) && !ContainsVector(cellExitList, prevCells[i]))
            {
                cellExitList.Add(prevCells[i]);
                UpdateCellVisual(prevCells[i], false);
                Debug.Log($"이전 위치 복원: {prevCells[i]}");
            }
        }

        // 2단계: 새 위치들을 cellExitList에서 제거
        for (int i = 0; i < checkCells.Count; i++)
        {
            if (ContainsVector(cellExitList, checkCells[i]))
            {
                RemoveVector(cellExitList, checkCells[i]);
                UpdateCellVisual(checkCells[i], true);
                Debug.Log($"새 위치 점유: {checkCells[i]}");
            }
            else if (!ContainsVector(cellList, checkCells[i]))
            {
                // 그리드 밖으로 나간 경우
                Debug.LogWarning($"유효하지 않은 셀 위치: {checkCells[i]}");
            }
        }

        ExitCount = cellExitList.Count;
        Debug.Log($"변경 후 cellExitList 개수: {cellExitList.Count}");
        Debug.Log($"=== ChangeVector 종료 ===\n");
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