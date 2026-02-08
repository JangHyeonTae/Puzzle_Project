using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.CanvasScaler;

public enum PopupAnimationType
{
    Fade,           // 페이드 인/아웃
    Scale,          // 스케일 애니메이션
    SlideUp,        // 아래에서 위로
    SlideDown,      // 위에서 아래로
    SlideLeft,      // 오른쪽에서 왼쪽으로
    SlideRight,     // 왼쪽에서 오른쪽으로
    Bounce,         // 바운스 효과
    Punch,          // 펀치 효과
    Elastic,        // 탄성 효과
    FadeScale       // 페이드 + 스케일 조합
}

public class UIManager : Singleton<UIManager>
{
    private Canvas mainCanvas;
    private Canvas clickCanvas;
    private CanvasScaler canvasScaler;
    [field: SerializeField] public Vector2 setCanvasScale { get; private set; }
    private RectTransform rect;

    [SerializeField] private PooledObject popupPrefab; // 팝업 프리팹
    private ObjectPool popupPool;

    [SerializeField] private PooledObject clickPrefab;
    private ObjectPool clickPool;

    private Dictionary<int, ClickUI> clickUIDictionary;

    protected void Awake()
    {
        base.Awake();
        clickUIDictionary = new Dictionary<int, ClickUI>();
        // if : Inst Canvas 
        CheckCanvas().Forget();
    }

    private async UniTaskVoid CheckCanvas()
    {
        if (mainCanvas == null)
        {
            var obj = await DataManager.Instance.LoadData("MainCanavs");
            mainCanvas = Instantiate(obj).GetComponent<Canvas>();
            //mainCanvas.GetOrAddComponent<CanvasScaler>();
            //mainCanvas.GetOrAddComponent<GraphicRaycaster>();
            mainCanvas.transform.SetParent(transform);
        }

        if (clickCanvas == null)
        {
            var obj = await DataManager.Instance.LoadData("ClickCanvas");
            clickCanvas = Instantiate(obj).GetComponent<Canvas>();
            clickCanvas.transform.SetParent(transform);
        }

        // Canvas가 준비된 후에 설정
        //canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
        //rect = mainCanvas.GetComponent<RectTransform>();
        //SetCanvasScale();
        //RectScale();

        // Pool 초기화 (Canvas가 준비된 후)
        SetPool(popupPool, popupPrefab, "PopUpParent", 50);
        SetPool(clickPool, clickPrefab, "ClickParent", 20);
    }

    private void SetPool(ObjectPool pool, PooledObject ui, string parentName, int size)
    {
        var parent = new GameObject(parentName);
        parent.transform.SetParent(mainCanvas.transform); // mainCanvas의 자식으로 설정
        pool = new ObjectPool(ui, size, parent.transform);
    }

    //public async UniTask ShowClickPopUp(int value)
    //{
    //    var data = await UILoadData($"UI{value}");
    //    clickCanvas.GetComponent<UIStack>().AddUI(data);
        
    //}
    //private async UniTask UILoadData(string s)
    //{
    //    var inst = await DataManager.Instance.LoadData(s);
    //    var data = clickPool.GetPooled(inst) as PooledObject;
    //    PooledObject obj = clickPool.GetPooled(data);
    //}
    public void CloseClickPopUp()
    {

    }



    #region 초기세팅
    private void SetCanvasScale()
    {
        mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
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

    /// <summary>
    /// 팝업을 표시합니다. (DOTween 버전)
    /// </summary>
    public async UniTask ShowPopUp(
        string message,
        float duration = 2f,
        PopupAnimationType animationType = PopupAnimationType.FadeScale,
        float animationDuration = 0.3f,
        Vector2? position = null,
        int fontSize = 24,
        Color? textColor = null,
        Color? backgroundColor = null,
        Ease ease = Ease.OutQuad,
        Action onComplete = null)
    {
        // Pool에서 가져오기
        PopUpPrefab popup = popupPool.GetPooled() as PopUpPrefab;
        popup.transform.SetParent(mainCanvas.transform, false);
        popup.transform.SetAsLastSibling();

        // 컴포넌트 가져오기
        RectTransform popupRect = popup.GetComponent<RectTransform>();
        TextMeshProUGUI textComponent = popup.GetComponentInChildren<TextMeshProUGUI>();
        Image backgroundImage = popup.GetComponent<Image>();
        CanvasGroup canvasGroup = popup.GetOrAddComponent<CanvasGroup>();

        // 초기화
        popup.Init(message, fontSize, textColor, backgroundColor);

        // 위치 설정
        Vector2 targetPosition = position ?? Vector2.zero;

        // 애니메이션 인 효과
        await PlayShowAnimation(popup.gameObject, popupRect, canvasGroup, targetPosition, animationType, animationDuration, ease);

        // 대기
        await UniTask.Delay(TimeSpan.FromSeconds(duration));

        // 애니메이션 아웃 효과
        await PlayHideAnimation(popup.gameObject, popupRect, canvasGroup, targetPosition, animationType, animationDuration, ease);

        // Pool로 반환
        popup.Release();

        // 콜백 실행
        onComplete?.Invoke();
    }

    /// <summary>
    /// 간단한 팝업 표시 (기본 설정)
    /// </summary>
    public async UniTask ShowSimplePopUp(string message, float duration = 2f, PopupAnimationType animationType = PopupAnimationType.FadeScale)
    {
        await ShowPopUp(message, duration, animationType);
    }

    /// <summary>
    /// 팝업 등장 애니메이션
    /// </summary>
    private async UniTask PlayShowAnimation(GameObject popup, RectTransform popupRect, CanvasGroup canvasGroup,
        Vector2 targetPosition, PopupAnimationType animationType, float duration, Ease ease)
    {
        // 모든 애니메이션 Kill
        popup.transform.DOKill();
        canvasGroup.DOKill();

        DG.Tweening.Sequence sequence = DOTween.Sequence();

        switch (animationType)
        {
            case PopupAnimationType.Fade:
                canvasGroup.alpha = 0f;
                popupRect.anchoredPosition = targetPosition;
                sequence.Append(canvasGroup.DOFade(1f, duration).SetEase(ease));
                break;

            case PopupAnimationType.Scale:
                canvasGroup.alpha = 1f;
                popupRect.anchoredPosition = targetPosition;
                popupRect.localScale = Vector3.zero;
                sequence.Append(popupRect.DOScale(Vector3.one, duration).SetEase(ease));
                break;

            case PopupAnimationType.SlideUp:
                canvasGroup.alpha = 1f;
                popupRect.localScale = Vector3.one;
                popupRect.anchoredPosition = targetPosition + new Vector2(0, -300);
                sequence.Append(popupRect.DOAnchorPos(targetPosition, duration).SetEase(ease));
                break;

            case PopupAnimationType.SlideDown:
                canvasGroup.alpha = 1f;
                popupRect.localScale = Vector3.one;
                popupRect.anchoredPosition = targetPosition + new Vector2(0, 300);
                sequence.Append(popupRect.DOAnchorPos(targetPosition, duration).SetEase(ease));
                break;

            case PopupAnimationType.SlideLeft:
                canvasGroup.alpha = 1f;
                popupRect.localScale = Vector3.one;
                popupRect.anchoredPosition = targetPosition + new Vector2(300, 0);
                sequence.Append(popupRect.DOAnchorPos(targetPosition, duration).SetEase(ease));
                break;

            case PopupAnimationType.SlideRight:
                canvasGroup.alpha = 1f;
                popupRect.localScale = Vector3.one;
                popupRect.anchoredPosition = targetPosition + new Vector2(-300, 0);
                sequence.Append(popupRect.DOAnchorPos(targetPosition, duration).SetEase(ease));
                break;

            case PopupAnimationType.Bounce:
                canvasGroup.alpha = 1f;
                popupRect.anchoredPosition = targetPosition;
                popupRect.localScale = Vector3.zero;
                sequence.Append(popupRect.DOScale(Vector3.one, duration).SetEase(Ease.OutBounce));
                break;

            case PopupAnimationType.Punch:
                canvasGroup.alpha = 1f;
                popupRect.anchoredPosition = targetPosition;
                popupRect.localScale = Vector3.one;
                sequence.Append(popupRect.DOPunchScale(Vector3.one * 0.3f, duration, 5, 1).SetEase(ease));
                break;

            case PopupAnimationType.Elastic:
                canvasGroup.alpha = 1f;
                popupRect.anchoredPosition = targetPosition;
                popupRect.localScale = Vector3.zero;
                sequence.Append(popupRect.DOScale(Vector3.one, duration).SetEase(Ease.OutElastic));
                break;

            case PopupAnimationType.FadeScale:
                canvasGroup.alpha = 0f;
                popupRect.anchoredPosition = targetPosition;
                popupRect.localScale = Vector3.zero;
                sequence.Append(canvasGroup.DOFade(1f, duration).SetEase(ease));
                sequence.Join(popupRect.DOScale(Vector3.one, duration).SetEase(ease));
                break;
        }

        await sequence.AsyncWaitForCompletion();
    }

    /// <summary>
    /// 팝업 퇴장 애니메이션
    /// </summary>
    private async UniTask PlayHideAnimation(GameObject popup, RectTransform popupRect, CanvasGroup canvasGroup,
        Vector2 currentPosition, PopupAnimationType animationType, float duration, Ease ease)
    {
        // 모든 애니메이션 Kill
        popup.transform.DOKill();
        canvasGroup.DOKill();

        DG.Tweening.Sequence sequence = DOTween.Sequence();

        switch (animationType)
        {
            case PopupAnimationType.Fade:
                sequence.Append(canvasGroup.DOFade(0f, duration).SetEase(ease));
                break;

            case PopupAnimationType.Scale:
                sequence.Append(popupRect.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
                break;

            case PopupAnimationType.SlideUp:
                sequence.Append(popupRect.DOAnchorPos(currentPosition + new Vector2(0, 300), duration).SetEase(ease));
                break;

            case PopupAnimationType.SlideDown:
                sequence.Append(popupRect.DOAnchorPos(currentPosition + new Vector2(0, -300), duration).SetEase(ease));
                break;

            case PopupAnimationType.SlideLeft:
                sequence.Append(popupRect.DOAnchorPos(currentPosition + new Vector2(-300, 0), duration).SetEase(ease));
                break;

            case PopupAnimationType.SlideRight:
                sequence.Append(popupRect.DOAnchorPos(currentPosition + new Vector2(300, 0), duration).SetEase(ease));
                break;

            case PopupAnimationType.Bounce:
            case PopupAnimationType.Punch:
            case PopupAnimationType.Elastic:
                sequence.Append(popupRect.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
                break;

            case PopupAnimationType.FadeScale:
                sequence.Append(canvasGroup.DOFade(0f, duration).SetEase(ease));
                sequence.Join(popupRect.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
                break;
        }

        await sequence.AsyncWaitForCompletion();
    }

    #endregion

    
}