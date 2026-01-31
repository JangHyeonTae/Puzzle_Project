using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TetrisChild : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private TeterisPrefab parent;

    private void Awake()
    {
        parent = GetComponentInParent<TeterisPrefab>();
    }

    public void OnPointerDown(PointerEventData eventData)
        => parent?.OnPointerDown(eventData);

    public void OnPointerUp(PointerEventData eventData)
        => parent?.OnPointerUp(eventData);

    public void OnBeginDrag(PointerEventData eventData)
        => parent?.OnBeginDrag(eventData);

    public void OnDrag(PointerEventData eventData)
        => parent?.OnDrag(eventData);

    public void OnEndDrag(PointerEventData eventData)
        => parent?.OnEndDrag(eventData);
}
