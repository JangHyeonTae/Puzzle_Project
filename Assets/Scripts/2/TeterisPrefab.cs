using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class TeterisPrefab : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private TeterisBlock blockSO;
    private Transform[] childrenPos;
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

    // 확인용
    public Vector2[] childrenVec;
    public event Action OnChangeRot;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Init(TeterisBlock tetrisSO, Grid _grid)
    {
        blockSO = tetrisSO;
        grid = _grid;

        int childCount = transform.childCount;
        childrenPos = new Transform[childCount];
        childrenVec = new Vector2[childCount];

        for (int i = 0; i < childCount; i++)
            childrenPos[i] = transform.GetChild(i);

        EnsureChildColliderAndForwarder();

        curRotIndex = 0;
        ApplyRotation(curRotIndex);

        // 시작 위치 스냅 상태 저장
        lastSnappedPosition = transform.position;

        OnChangeRot -= ChangeRot;
        OnChangeRot += ChangeRot;

        hasDragOffset = false;
    }

    private void OnDisable()
    {
        OnChangeRot -= ChangeRot;
        CancelTimer();
        isPointerDown = false;
        isDragging = false;
        hasDragOffset = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPointerDown = true;
        isDragging = false;
        StartLongPressTimer().Forget();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPointerDown = false;
        CancelTimer();
    }

    private async UniTaskVoid StartLongPressTimer()
    {
        CancelTimer();
        token = new CancellationTokenSource();

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token.Token);
            if (isPointerDown && !isDragging)
                OnChangeRot?.Invoke();
        }
        catch (OperationCanceledException) { }
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
    }

    private void UpdateChildrenVecWorld()
    {
        for (int i = 0; i < childrenPos.Length; i++)
            childrenVec[i] = childrenPos[i].position;
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

    private void CancelTimer()
    {
        if (token != null)
        {
            token.Cancel();
            token.Dispose();
            token = null;
        }
    }

    private void OnDestroy()
    {
        CancelTimer();
    }
}
