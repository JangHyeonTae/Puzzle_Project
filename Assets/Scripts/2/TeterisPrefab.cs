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

    // 회전 필요 시간
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
        prevPos = transform.position; // 현재 위치 저장
        curInPos = new Vector3[childCount];

        for (int i = 0; i < childCount; i++)
        {
            // (0,0,0) 버그 방지를 위해 초기값을 그리드 밖으로 설정
            curInPos[i] = new Vector3(-9999f, -9999f, -9999f);

            Vector3 childWorldPos = transform.GetChild(i).position;
            childWorldPos.z = 0;

            Vector3Int tempCell = grid.WorldToCell(childWorldPos);
            Vector3 cellCenterWorld = grid.GetCellCenterWorld(tempCell);
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

        // 회전 후 마우스를 뗐을 때, 현재 위치가 유효한지 검사
        // 내 자신이 점유하고 있는 칸(IsMine)도 유효한 것으로 인정해야 함
        bool isValidLayout = true;
        for (int i = 0; i < childrenPositions.Length; i++)
        {
            bool isFree = DrawGrid.Instance.OnCheck(childrenPositions[i]);
            bool isMine = DrawGrid.Instance.OnCheckIsMine(childrenPositions.ToList(), this);

            if (!isFree && !isMine)
            {
                isValidLayout = false;
                break;
            }
        }

        if (!isValidLayout)
        {
            // 유효하지 않으면 이전 회전으로 복구
            curRotIndex = prevRotIndex;
            ApplyRotation(curRotIndex);
            // 데이터 동기화를 위해 ChangeVec 호출
            ChangeVec();
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
        catch (OperationCanceledException) { }
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
        prevRotIndex = curRotIndex; // 이전 회전값 저장
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
        List<Vector3> nextPositions = new List<Vector3>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3 childWorldPos = transform.GetChild(i).position;
            Vector3Int tempCell = grid.WorldToCell(childWorldPos);
            Vector3 cellCenterWorld = grid.GetCellCenterWorld(tempCell);
            cellCenterWorld.z = 0;
            nextPositions.Add(cellCenterWorld);
        }

        // 드래그 중이 아닐 때(회전 등)의 유효성 체크
        if (!isPointerDown)
        {
            bool isValidMove = true;
            foreach (var pos in nextPositions)
            {
                // 빈 칸이 아니고, 내가 점유한 칸도 아니라면
                if (!DrawGrid.Instance.OnCheck(pos) && !curInPos.Contains(pos))
                {
                    isValidMove = false;
                    break;
                }
            }

            if (!isValidMove)
            {
                transform.position = prevPos;
                return;
            }
        }

        List<Vector3> prevOccupied = curInPos != null ? curInPos.ToList() : new List<Vector3>();


        var result = DrawGrid.Instance.OnCheckCell?.Invoke(nextPositions, prevOccupied, this);
        if (result == null)
            result = new List<Vector3>();

        curInPos = result.ToArray();

        if (result != null && result.Count > 0 && !isPointerDown && !isDragging)
        {
            Vector3 worldPos = result[0];
            Vector2 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            Vector2 uiPos = screenPos;// - new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            switch(curInPos.Length)
            {
                case 0:
                    UIManager.Instance.ShowPopUp("Bad", 0.3f, PopupAnimationType.Punch, 0.2f, uiPos, textColor: Color.red).Forget();
                    break;
                case 1:
                    UIManager.Instance.ShowPopUp("SoSo", 0.3f, PopupAnimationType.Punch, 0.2f, uiPos, textColor: Color.blue).Forget();
                    break;
                case 2:
                    UIManager.Instance.ShowPopUp("Good", 0.3f, PopupAnimationType.Punch, 0.2f, uiPos, textColor: Color.green).Forget();
                    break;
                case 3:
                    UIManager.Instance.ShowPopUp("Greate", 0.3f, PopupAnimationType.Punch, 0.2f, uiPos, textColor: Color.yellow).Forget();
                    break;
                case 4:
                    UIManager.Instance.ShowPopUp("Excellent", 0.3f, PopupAnimationType.Punch, 0.2f, uiPos, textColor: Color.cyan).Forget();
                    break;
            }
            
        }

        childrenPositions = nextPositions.ToArray();
        prevPos = transform.position;

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
        curInPos = null;
    }

    public void Outit()
    {
        Debug.Log("outit");
        // 중요: 오브젝트가 사라지기 전에 점유 중인 칸을 그리드에 반납
        if (curInPos != null && curInPos.Length > 0)
        {
            DrawGrid.Instance.OnCheckCell?.Invoke(new List<Vector3>(), curInPos.ToList(), this);
        }

        for (int i = 0; i < childrenPositions.Length; i++)
        {
            if (DrawGrid.Instance.outList.Contains(childrenPositions[i]))
            {
                DrawGrid.Instance.outList.Remove(childrenPositions[i]);
            }
        }

        ResetSetting();
        CancelTimer();
        Release();
    }
}