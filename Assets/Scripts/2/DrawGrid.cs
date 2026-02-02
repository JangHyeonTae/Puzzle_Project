using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[RequireComponent(typeof(LineRenderer))]
public class DrawGrid : Singleton<DrawGrid>
{
    public Grid grid;
    public Color gridColor = Color.cyan;
    public float lineWidth = 0.05f;
    public List<Vector3> cellList; // 이미 그려진 셀들
    public List<Vector3> curExitList; // 현재 사용 가능한 셀들

    private int exitCount;
    public int ExitCount { get { return exitCount; } set { exitCount = value;  OnChangeCount?.Invoke(exitCount); } }
    public event Action<int> OnChangeCount;

    public Action<List<Vector3>, bool> OnCheckCell;
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

    private void AddVector(List<Vector3> cellPositions)
    {
        curExitList = new List<Vector3>(cellPositions);
        ExitCount = curExitList.Count;
    }

    /// <summary>
    /// 핵심 수정: cellList는 절대 수정하지 않고, curExitList만 변경
    /// isRemove = true: 블럭이 그리드에 놓임 → curExitList에서 제거
    /// isRemove = false: 블럭이 그리드에서 들어올림 → curExitList에 추가
    /// </summary>
    private void ChangeVector(List<Vector3> checkCells, bool isRemove)
    {
        if (isRemove)
        {
            Debug.Log($"=== 블럭 배치 (curExitList에서 제거) ===");
            for (int i = 0; i < checkCells.Count; i++)
            {
                Vector3 checkCell = checkCells[i];

                // 1. cellList에 해당 셀이 존재하는지 확인
                bool matchedInCellList = cellList.Contains(checkCell);

                if (!matchedInCellList)
                {
                    Debug.Log($"<color=red> 그리드 범위 밖: {checkCell}</color>");
                    continue;
                }

                // 2. curExitList에 해당 셀이 존재하는지 확인
                bool matchedInExitList = curExitList.Contains(checkCell);

                if (matchedInExitList)
                {
                    curExitList.Remove(checkCell);
                    ExitCount--;
                    Debug.Log($"<color=green> 배치 성공: {checkCell} → curExitList에서 제거</color>");
                }
                else
                {
                    Debug.Log($"<color=yellow> 이미 차있는 칸: {checkCell}</color>");
                }
            }
        }
        else
        {
            Debug.Log($"=== 블럭 들어올림 (curExitList에 추가) ===");
            for (int i = 0; i < checkCells.Count; i++)
            {
                Vector3 checkCell = checkCells[i];

                // 1. cellList에 해당 셀이 존재하는지 확인
                Vector3? matchedInCellList = cellList.FirstOrDefault(cell =>
                    Mathf.Approximately(cell.x, checkCell.x) &&
                    Mathf.Approximately(cell.y, checkCell.y));

                if (!matchedInCellList.HasValue)
                {
                    Debug.Log($"<color=red> 그리드 범위 밖: {checkCell}</color>");
                    continue;
                }

                // 2. curExitList에 이미 존재하는지 확인
                Vector3? matchedInExitList = curExitList.FirstOrDefault(cell =>
                    Mathf.Approximately(cell.x, checkCell.x) &&
                    Mathf.Approximately(cell.y, checkCell.y));

                if (!matchedInExitList.HasValue)
                {
                    curExitList.Add(matchedInCellList.Value);
                    ExitCount++;
                    Debug.Log($"<color=cyan> 들어올림 성공: {checkCell} → curExitList에 추가</color>");
                }
                else
                {
                    Debug.Log($"<color=yellow> 이미 비어있는 칸: {checkCell}</color>");
                }
            }
        }

        Debug.Log($"현재 비어있는 칸 개수: {curExitList.Count}/{cellList.Count}");
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

        if (curExitList != null)
            curExitList.Clear();

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
