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
    public Vector3[] childrenReturnPositions;
    private Vector3 prevPos;

    public Vector3[] curInPos;

    private int curRotIndex;
    private int prevRotIndex;

    private CancellationTokenSource token;
    private bool isPointerDown;
    private bool isDragging;

    private Camera mainCamera;
    private Grid grid;

    private Vector3 lastSnappedPosition;

    private Vector3Int dragCellOffset;
    private bool hasDragOffset;

    // 터치 횟수 추적
    private int touchCount;
    private float lastTouchTime;
    private float doubleTouchThreshold = 0.3f;

    //회전 필요 시간
    private float rotationInterval = 2f;

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
        childrenPrevPositions = new Vector3[childCount];
        childrenReturnPositions = new Vector3[childCount];
        prevPos = Vector3.zero;
        curInPos = new Vector3[childCount];
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
            childrenReturnPositions[i] = cellCenterWorld + new Vector3(1, 1, 1);
        }

        EnsureChildColliderAndForwarder();

        curRotIndex = 0;
        prevRotIndex = 0;
        ApplyRotation(curRotIndex);

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
        prevRotIndex = curRotIndex;
        CheckTouchCount();

        StartLongPressTimer().Forget();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;

        for (int i = 0; i < childrenPositions.Length; i++)
        {
            if (DrawGrid.Instance.OnCheck(childrenPositions[i]) || DrawGrid.Instance.OnCheckIsMine(this))
            {
                continue;
            }
            else
            {
                curRotIndex = prevRotIndex;
                ApplyRotation(curRotIndex);
            }
        }
        

        CancelTimer();
    }

    private void CheckTouchCount()
    {
        float currentTime = Time.time;

        if (currentTime - lastTouchTime > doubleTouchThreshold)
        {
            touchCount = 0;
        }

        touchCount++;
        lastTouchTime = currentTime;

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
            await UniTask.Delay(TimeSpan.FromSeconds(rotationInterval), cancellationToken: token.Token);

            if (isPointerDown && !isDragging)
            {
                OnChangeRot?.Invoke();

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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (grid == null || mainCamera == null) return;

        prevPos = gameObject.transform.position;

        isDragging = true;
        CancelTimer();

        Vector3 fingerWorld = mainCamera.ScreenToWorldPoint(eventData.position);
        fingerWorld.z = 0f;

        Vector3Int fingerCell = grid.WorldToCell(fingerWorld);
        Vector3Int pivotCell = grid.WorldToCell(transform.position);

        dragCellOffset = pivotCell - fingerCell;
        hasDragOffset = true;

        lastSnappedPosition = transform.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (grid == null || mainCamera == null) return;

        Vector3 fingerWorld = mainCamera.ScreenToWorldPoint(eventData.position);
        fingerWorld.z = 0f;

        Vector3Int fingerCell = grid.WorldToCell(fingerWorld);

        Vector3Int targetCell = hasDragOffset ? (fingerCell + dragCellOffset) : fingerCell;

        Vector3 snappedPos = grid.GetCellCenterWorld(targetCell);
        snappedPos.z = 0f;

        if (snappedPos != lastSnappedPosition)
        {
            transform.position = snappedPos;
            lastSnappedPosition = snappedPos;
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
    }

    private void ChangeRot()
    {
        curRotIndex = (curRotIndex + 1) % 4;
        ApplyRotation(curRotIndex);
        ChangeVec(); 
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
    }

    private void EnsureChildColliderAndForwarder()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);

            if (child.GetComponent<Collider2D>() == null)
                child.gameObject.AddComponent<BoxCollider2D>();

            if (child.GetComponent<TetrisChild>() == null)
                child.gameObject.AddComponent<TetrisChild>();
        }
    }

    public void ChangeVec()
    {
        for (int i = 0; i < childrenPositions.Length; i++)
        {
            childrenReturnPositions[i] = childrenPrevPositions[i];
            childrenPrevPositions[i] = childrenPositions[i];
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3 childWorldPos = transform.GetChild(i).position;
            childWorldPos.z = 0;

            Vector3Int tempCell = grid.WorldToCell(childWorldPos);
            Vector3 cellCenterWorld = grid.CellToWorld(tempCell);
            cellCenterWorld.z = 0;

            if (!isPointerDown)
            {
                if (!DrawGrid.Instance.OnCheck(cellCenterWorld))
                {
                    gameObject.transform.position = prevPos;
                    childrenPrevPositions = (Vector3[])childrenReturnPositions.Clone();
                    childrenPositions = (Vector3[])childrenPrevPositions.Clone();
                }
                else
                {
                    childrenPositions[i] = cellCenterWorld;
                }
            }
        }

        curInPos = DrawGrid.Instance.OnCheckCell?.Invoke(
            childrenPositions.ToList(),
            childrenPrevPositions.ToList()
        ).ToArray();
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