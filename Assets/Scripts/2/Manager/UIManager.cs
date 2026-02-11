using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    private CanvasScaler canvasScaler;
    [field: SerializeField] public Vector2 setCanvasScale { get; private set; }
    private RectTransform rect;

    [SerializeField] private PooledObject popupPrefab; // 팝업 프리팹
    private ObjectPool popupPool;

    UIStack uiStack;

    protected void Awake()
    {
        base.Awake();
        CheckCanvas().Forget();
    }


    private async UniTaskVoid CheckCanvas()
    {
        if (uiStack == null)
            uiStack = new UIStack();

        if (mainCanvas == null)
        {
            var obj = await DataManager.Instance.LoadData("MainCanvas");
            if (obj != null)
            {
                mainCanvas = Instantiate(obj).GetComponent<Canvas>();
                mainCanvas.transform.SetParent(transform);
            }
        }

        if (popupPrefab != null)
        {
            var parent = new GameObject("PopUpParent");
            parent.transform.SetParent(mainCanvas.transform);
            popupPool = new ObjectPool(popupPrefab, 50, parent.transform);
        }
        else
        {
            Debug.LogError("UIManager: popupPrefab이 Inspector에서 할당되지 않았습니다!");
        }
    }

    private void SetPool(ObjectPool pool, PooledObject ui, string parentName, int size)
    {
        var parent = new GameObject(parentName);
        parent.transform.SetParent(mainCanvas.transform);
        pool = new ObjectPool(ui, size, parent.transform);
    }

    public void AddPopUp(BaseUI ui)
    {
        uiStack.AddUI(ui);
    }

    public void RemovePopUp()
    {
        uiStack.RemoveUI();
    }

    #region PopUp Dotween
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

    public async UniTask ShowPopUp(
        string message,
        float duration = 2f,
        PopupAnimationType animationType = PopupAnimationType.FadeScale,
        float animationDuration = 0.3f,
        Vector2? position = null,
        int fontSize = 100,
        Color? textColor = null,
        Ease ease = Ease.OutQuad,
        Action onComplete = null)
    {
        PopUpPrefab popup = popupPool.GetPooled() as PopUpPrefab;
        popup.transform.SetAsLastSibling();

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        TextMeshProUGUI textComponent = popup.GetComponentInChildren<TextMeshProUGUI>();
        Image backgroundImage = popup.GetComponent<Image>();
        CanvasGroup canvasGroup = popup.GetOrAddComponent<CanvasGroup>();

        popup.Init(message, fontSize, textColor);

        Vector2 targetPosition = position ?? Vector2.zero;

        await PlayShowAnimation(popup.gameObject, popupRect, canvasGroup, targetPosition, animationType, animationDuration, ease);

        await UniTask.Delay(TimeSpan.FromSeconds(duration));

        await PlayHideAnimation(popup.gameObject, popupRect, canvasGroup, targetPosition, animationType, animationDuration, ease);

        popup.Release();

        onComplete?.Invoke();
    }

    // 간단한 팝업 표시 (기본 설정)
    public async UniTask ShowSimplePopUp(string message, float duration = 2f, PopupAnimationType animationType = PopupAnimationType.FadeScale)
    {
        await ShowPopUp(message, duration, animationType);
    }

    // 팝업 등장 애니메이션
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

    // 팝업 퇴장 애니메이션
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