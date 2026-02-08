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

    /// <summary>
    /// 팝업 초기화
    /// </summary>
    public void Init(string message, int fontSize, Color? textColor, Color? backgroundColor)
    {
        // 텍스트 설정
        if (messageText != null)
        {
            messageText.text = message;
            messageText.fontSize = fontSize;
            messageText.color = textColor ?? Color.white;
        }

        // 배경 색상 설정
        if (backgroundImage != null)
        {
            backgroundImage.color = backgroundColor ?? new Color(0, 0, 0, 0.8f);
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

    /// <summary>
    /// Pool로 반환하기 전 정리
    /// </summary>
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

    /// <summary>
    /// Pool로 반환 (오버라이드)
    /// </summary>
    public new void Release(float delay = 0f)
    {
        Cleanup();
        base.Release(delay);
    }
}