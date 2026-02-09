using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopUpPrefab : PooledObject
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Image backgroundImage;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // 컴포넌트 캐싱
        if (messageText == null)
            messageText = GetComponentInChildren<TextMeshProUGUI>();

        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(string message, int fontSize, Color? textColor)
    {
        // 텍스트 설정
        if (messageText != null)
        {
            messageText.text = message;
            messageText.fontSize = fontSize;
            messageText.color = textColor ?? Color.white;
        }

        // CanvasGroup 초기화
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        // Transform 초기화
        transform.localScale = Vector3.one;
        transform.localRotation = Quaternion.identity;
    }

    public void Cleanup()
    {
        // DOTween 애니메이션 정리
        transform.DOKill();
        if (canvasGroup != null)
            canvasGroup.DOKill();

        // 초기 상태로 되돌리기
        transform.localScale = Vector3.one;
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public new void Release(float delay = 0f)
    {
        Cleanup();
        base.Release(delay);
    }
}