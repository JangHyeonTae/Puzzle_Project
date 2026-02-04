using UnityEngine;
using UnityEngine.EventSystems;

public class ChooseTetris : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TeterisBlock tetrisSO;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Grid grid;

    private TeterisPrefab currentDrag;

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (grid == null)
            grid = FindObjectOfType<Grid>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Vector3 world = mainCamera.ScreenToWorldPoint(eventData.position);
        world.z = 0;

        Vector3Int cell = grid.WorldToCell(world);
        Vector3 snapped = grid.GetCellCenterWorld(cell);
        snapped.z = 0;

        currentDrag = StageManager.Instance.tetrisPool.GetPooled() as TeterisPrefab;
        currentDrag.transform.position = snapped;
        currentDrag.Init(tetrisSO, grid);

        ExecuteEvents.Execute<IBeginDragHandler>(
            currentDrag.gameObject, eventData, ExecuteEvents.beginDragHandler);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (currentDrag != null)
        {
            ExecuteEvents.Execute<IDragHandler>(
                currentDrag.gameObject, eventData, ExecuteEvents.dragHandler);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (currentDrag != null)
        {
            ExecuteEvents.Execute<IEndDragHandler>(
                currentDrag.gameObject, eventData, ExecuteEvents.endDragHandler);
        }

        currentDrag = null;
    }
}
