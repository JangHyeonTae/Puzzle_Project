using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

public class TeterisPrefab : PooledObject,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private TeterisBlock blockSO;
    public Vector3[] childrenPositions;  // ★ Vector3로 변경 (0.5 단위 지원)
    public Vector3[] childrenPrevPositions;  // ★ Vector3로 변경
    private int curRotIndex;

    private CancellationTokenSource token;
    private bool isPointerDown;
    private bool isDragging;

    private Camera mainCamera;
    private Grid grid;

    private Vector3 lastSnappedPosition;

    // 드래그 점프 방지용: "손가락 셀"과 "pivot 셀" 차이
    private Vector3Int dragCellOffset;
    private bool hasDragOffset;

    // 터치 횟수 추적
    private int touchCount;
    private float lastTouchTime;
    private float doubleTouchThreshold = 0.3f;

    //회전 필요 시간
    private float rotationInterval = 2f;

    // 확인용
    public Vector2[] childrenVec;  // ★ Vector2로 변경 (SO의 blockPos 타입과 일치)
    public event Action OnChangeRot;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Init(TeterisBlock tetrisSO, Grid _grid)
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        blockSO = tetrisSO;
        grid = _grid;

        int childCount = transform.childCount;
        childrenPositions = new Vector3[childCount];  // ★ Vector3 배열
        childrenVec = new Vector2[childCount];  // ★ Vector2 배열
        childrenPrevPositions = new Vector3[childCount];  // ★ Vector3 배열

        // ★ 초기화: Grid의 CellToWorld로 정확한 셀 중심 좌표 저장
        for (int i = 0; i < childCount; i++)
        {
            Vector3 childWorldPos = transform.GetChild(i).position;
            childWorldPos.z = 0;

            // WorldToCell -> CellToWorld로 정확한 셀 좌표 얻기
            Vector3Int tempCell = grid.WorldToCell(childWorldPos);
            Vector3 cellCenterWorld = grid.CellToWorld(tempCell);
            cellCenterWorld.z = 0;

            childrenPositions[i] = cellCenterWorld;
            childrenPrevPositions[i] = cellCenterWorld;
        }

        EnsureChildColliderAndForwarder();

        curRotIndex = 0;
        ApplyRotation(curRotIndex);

        // 시작 위치 스냅 상태 저장
        lastSnappedPosition = transform.position;

        OnChangeRot -= ChangeRot;
        OnChangeRot += ChangeRot;

        touchCount = 0;
        hasDragOffset = false;
    }

    private void OnDisable()
    {
        OnChangeRot -= ChangeRot;
        CancelTimer();
        isPointerDown = false;
        isDragging = false;
        hasDragOffset = false;
        touchCount = 0;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        isDragging = false;
        CheckTouchCount();

        StartLongPressTimer().Forget();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        CancelTimer();
    }

    private void CheckTouchCount()
    {
        float currentTime = Time.time;

        // 이전 터치로부터 일정 시간이 지났으면 카운트 리셋
        if (currentTime - lastTouchTime > doubleTouchThreshold)
        {
            touchCount = 0;
        }

        touchCount++;
        lastTouchTime = currentTime;

        // 2번 이상 터치 시 할당 해제
        if (touchCount >= 2)
        {
            Outit();
        }
    }

    private async UniTaskVoid StartLongPressTimer()
    {
        CancelTimer();
        token = new CancellationTokenSource();

        try
        {
            // 첫 회전까지 대기
            await UniTask.Delay(TimeSpan.FromSeconds(rotationInterval), cancellationToken: token.Token);

            // 드래그 중이 아니고 여전히 누르고 있으면 회전
            if (isPointerDown && !isDragging)
            {
                OnChangeRot?.Invoke();

                // 계속 누르고 있는 동안 반복
                while (isPointerDown && !isDragging)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(rotationInterval), cancellationToken: token.Token);

                    if (isPointerDown && !isDragging)
                    {
                        OnChangeRot?.Invoke();
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("회전 타이머 취소됨");
        }
    }

    // ===== Drag =====
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (grid == null || mainCamera == null) return;

        isDragging = true;
        CancelTimer();

        // 드래그 시작 시점에 "손가락 셀"과 "pivot 셀" 차이를 저장
        Vector3 fingerWorld = mainCamera.ScreenToWorldPoint(eventData.position);
        fingerWorld.z = 0f;

        Vector3Int fingerCell = grid.WorldToCell(fingerWorld);
        Vector3Int pivotCell = grid.WorldToCell(transform.position);

        dragCellOffset = pivotCell - fingerCell;
        hasDragOffset = true;

        // 현재 위치를 기준으로 시작
        lastSnappedPosition = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (grid == null || mainCamera == null) return;

        Vector3 fingerWorld = mainCamera.ScreenToWorldPoint(eventData.position);
        fingerWorld.z = 0f;

        Vector3Int fingerCell = grid.WorldToCell(fingerWorld);

        // 손가락 셀 + 오프셋 = 목표 pivot 셀 (점프 없음)
        Vector3Int targetCell = hasDragOffset ? (fingerCell + dragCellOffset) : fingerCell;

        Vector3 snappedPos = grid.GetCellCenterWorld(targetCell);
        snappedPos.z = 0f;

        if (snappedPos != lastSnappedPosition)
        {
            transform.position = snappedPos;
            lastSnappedPosition = snappedPos;
            UpdateChildrenVecWorld();
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        CancelTimer();
        isPointerDown = false;
        hasDragOffset = false;
        ChangeVec();
    }

    public void ForceSnapByScreenPos(Vector2 screenPos)
    {
        if (grid == null || mainCamera == null) return;

        Vector3 worldPos = mainCamera.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        Vector3Int cell = grid.WorldToCell(worldPos);
        Vector3 snapped = grid.GetCellCenterWorld(cell);
        snapped.z = 0f;

        transform.position = snapped;
        lastSnappedPosition = snapped;
        UpdateChildrenVecWorld();
    }

    // ===== Rotation =====
    private void ChangeRot()
    {
        curRotIndex = (curRotIndex + 1) % 4;
        ApplyRotation(curRotIndex);
        ChangeVec();  // ★ 회전 후 바로 ChangeVec 호출
    }

    private void ApplyRotation(int rotIndex)
    {
        if (blockSO == null) return;

        var rot = blockSO.posVectors[rotIndex].blockPos;  // Vector2[]

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            child.GetComponent<SpriteRenderer>().sprite = blockSO.tetrisSprite;
            child.localPosition = rot[i];  // Vector2를 Vector3로 암시적 변환
        }

        // ★ SO 배열 참조 공유 방지(복사) - Vector2[] -> Vector2[]
        if (childrenVec == null || childrenVec.Length != rot.Length)
            childrenVec = new Vector2[rot.Length];

        Array.Copy(rot, childrenVec, rot.Length);
    }

    private void UpdateChildrenVecWorld()
    {
        // childrenVec는 localPosition 정보이므로 여기서는 사용하지 않음
        // 필요시 childrenPositions를 childrenVec에 복사
        for (int i = 0; i < childrenPositions.Length; i++)
        {
            childrenVec[i] = childrenPositions[i];  // Vector3 -> Vector2 암시적 변환
        }
    }

    private void EnsureChildColliderAndForwarder()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);

            if (child.GetComponent<Collider2D>() == null)
                child.gameObject.AddComponent<BoxCollider2D>();

            // TetrisChild가 부모로 이벤트 포워딩하는 스크립트라고 가정
            if (child.GetComponent<TetrisChild>() == null)
                child.gameObject.AddComponent<TetrisChild>();
        }
    }

    /// <summary>
    /// ★ 핵심: 모든 자식의 위치를 Grid 셀 중심 좌표(Vector3)로 변환하여 저장
    /// </summary>
    public void ChangeVec()
    {
        // 먼저 이전 위치 백업
        for (int i = 0; i < childrenPositions.Length; i++)
        {
            childrenPrevPositions[i] = childrenPositions[i];
        }

        // 새 위치 계산
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3 childWorldPos = transform.GetChild(i).position;
            childWorldPos.z = 0;

            // ★ WorldToCell -> CellToWorld로 정확한 셀 중심 좌표 얻기
            Vector3Int tempCell = grid.WorldToCell(childWorldPos);
            Vector3 cellCenterWorld = grid.CellToWorld(tempCell);
            cellCenterWorld.z = 0;

            childrenPositions[i] = cellCenterWorld;
        }

        DrawGrid.Instance.OnCheckCell?.Invoke(
            childrenPositions.ToList(),
            childrenPrevPositions.ToList()
        );
    }

    private void CancelTimer()
    {
        if (token != null)
        {
            token.Cancel();
            token.Dispose();
            token = null;
        }
    }

    private void ResetSetting()
    {
        touchCount = 0;
        childrenPositions = null;
        childrenPrevPositions = null;
        childrenVec = null;
        curRotIndex = 0;
        isPointerDown = false;
        isDragging = false;
        lastSnappedPosition = Vector3.zero;
        dragCellOffset = Vector3Int.zero;
        hasDragOffset = false;
        blockSO = null;
        OnChangeRot -= ChangeRot;
    }

    public void Outit()
    {
        ResetSetting();
        CancelTimer();
        Release();
    }
}