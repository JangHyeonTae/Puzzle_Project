using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChooseTetris : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TeterisBlock tetrisSO;
    [SerializeField] private TeterisPrefab tetrisSample;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Grid grid; // Grid 참조

    private TeterisPrefab currentDragObject;
    private bool isDragging = false;
    private Vector3 lastSnappedPosition;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (grid == null)
            grid = FindObjectOfType<Grid>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentDragObject == null && !isDragging)
        {
            isDragging = true;

            // 터치/마우스 위치를 월드 좌표로 변환
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(eventData.position);
            worldPos.z = 0;

            // Grid의 내장 함수를 사용하여 스냅
            Vector3Int cell = grid.WorldToCell(worldPos);
            Vector3 snappedPos = grid.GetCellCenterWorld(cell);
            snappedPos.z = 0;
            lastSnappedPosition = snappedPos;

            // 오브젝트 생성
            currentDragObject = Instantiate(tetrisSample, snappedPos, Quaternion.identity);
            currentDragObject.Init(tetrisSO, grid);

            // 드래그 이벤트 넘기기
            ExecuteEvents.Execute<IBeginDragHandler>(
                currentDragObject.gameObject, eventData, ExecuteEvents.beginDragHandler
                );
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentDragObject != null && isDragging)
        {
            // TeterisPrefab이 직접 처리하므로 여기서는 이벤트만 전달
            ExecuteEvents.Execute<IDragHandler>(
                currentDragObject.gameObject, eventData, ExecuteEvents.dragHandler
                );
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentDragObject != null)
        {
            ExecuteEvents.Execute<IEndDragHandler>(
                currentDragObject.gameObject, eventData, ExecuteEvents.endDragHandler
                );
        }
        
        isDragging = false;
        currentDragObject = null;
    }
}
