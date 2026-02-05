using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChooseTetris : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TeterisBlock tetrisSO;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Grid grid;

    Image img;

    private TeterisPrefab currentDrag;
    [SerializeField] private Color chooseColor;
    [SerializeField] private Color normalColor;
    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (grid == null)
            grid = FindObjectOfType<Grid>();

        normalColor = Color.white;
        img = gameObject.GetComponent<Image>();
        img.color = normalColor;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        img.color = chooseColor;

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
        img.color = normalColor;
        if (currentDrag != null)
        {
            ExecuteEvents.Execute<IEndDragHandler>(
                currentDrag.gameObject, eventData, ExecuteEvents.endDragHandler);
        }
        currentDrag = null;
    }
}
