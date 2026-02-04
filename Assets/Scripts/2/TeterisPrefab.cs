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
    public Vector3[] childrenPositions;
    public Vector3[] childrenPrevPositions;
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
    public Vector2[] childrenVec;
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
        childrenPositions = new Vector3[childCount];
        childrenVec = new Vector2[childCount];
        childrenPrevPositions = new Vector3[childCount];

        // ★ 초기화 시점에도 셀 좌표로 저장
        for (int i = 0; i < childCount; i++)
        {
            Vector3 childWorldPos = transform.GetChild(i).position;
            childWorldPos.z = 0;
            Vector3Int cellPos = grid.WorldToCell(childWorldPos);
            childrenPositions[i] = cellPos;
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

        Debug.Log($"터치 횟수: {touchCount}");

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
                Debug.Log("회전 1회 실행");

                // 계속 누르고 있는 동안 반복
                while (isPointerDown && !isDragging)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(rotationInterval), cancellationToken: token.Token);

                    if (isPointerDown && !isDragging)
                    {
                        OnChangeRot?.Invoke();
                        Debug.Log("회전 계속 실행");
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

        // 디버그 필요하면 켜
        // Debug.Log($"fingerCell={fingerCell} targetCell={targetCell} pos={transform.position}");
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

        UpdateChildrenVecWorld();
    }

    private void ApplyRotation(int rotIndex)
    {
        if (blockSO == null) return;

        var rot = blockSO.posVectors[rotIndex].blockPos;

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            child.GetComponent<SpriteRenderer>().sprite = blockSO.tetrisSprite;
            child.localPosition = rot[i];
        }

        // SO 배열 참조 공유 방지(복사)
        if (childrenVec == null || childrenVec.Length != rot.Length)
            childrenVec = new Vector2[rot.Length];

        Array.Copy(rot, childrenVec, rot.Length);
        ChangeVec();
    }

    private void UpdateChildrenVecWorld()
    {
        for (int i = 0; i < childrenPositions.Length; i++)
            childrenVec[i] = childrenPositions[i];

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
    /// 핵심 수정: 모든 자식의 위치를 셀 좌표로 변환하여 저장
    /// </summary>
    public void ChangeVec()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3 childWorldPos = transform.GetChild(i).position;
            childWorldPos.z = 0;

            // 월드 좌표를 셀 좌표로 변환하여 저장
            Vector3Int cellPos = grid.WorldToCell(childWorldPos);
            childrenPrevPositions[i] = childrenPositions[i];
            childrenPositions[i] = cellPos;
        }

        DrawGrid.Instance.OnCheckCell?.Invoke(childrenPositions.ToList(), childrenPrevPositions.ToList());
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
