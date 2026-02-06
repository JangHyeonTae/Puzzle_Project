using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : Singleton<UIManager>
{
    public Canvas canvas { get; private set; }
    private CanvasScaler canvasScaler;
    [field : SerializeField] public Vector2 setCanvasScale { get; private set; }
    private RectTransform rect;

    protected void Awake()
    {
        base.Awake();

        if (canvas == null)
        {
            var obj = new GameObject("Main Canvas");
            canvas = obj.AddComponent<Canvas>();
            canvas.GetOrAddComponent<CanvasScaler>();
            canvas.GetOrAddComponent<GraphicRaycaster>();
            canvas.transform.SetParent(transform);
        }
        

        canvasScaler = canvas.GetComponent<CanvasScaler>();
        rect = canvas.GetComponent<RectTransform>();
        SetCanvasScale();

        //rect √ ±‚»≠ æ»µ ...πÆ¡¶ ∏Ù∞⁄¿Ω
        RectScale();
    }

    private void SetCanvasScale()
    {
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = setCanvasScale;

    }

    private void RectScale()
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localPosition = Vector3.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
}
