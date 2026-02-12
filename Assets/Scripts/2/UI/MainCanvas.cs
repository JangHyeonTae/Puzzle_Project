using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : MonoBehaviour
{
    [SerializeField] private Button optionButton;
    [SerializeField] private BaseUI optionPopUp;
    [SerializeField] private Button tetrisPanelOnOff;
    [SerializeField] private GameObject ChooseTetrisPanel;

    [SerializeField] private GameObject stageFinishPanel;
    [SerializeField] private GameObject[] stars;
    [SerializeField] private Button nextStageButton;

    private CancellationTokenSource starCts;
    private bool isOpen;
    private void OnEnable()
    {
        optionButton.onClick.AddListener(() => ClickOption(optionPopUp));
        tetrisPanelOnOff.onClick.AddListener(OnOffTetris);
        nextStageButton.onClick.AddListener(NextStage);
        isOpen = true;
        ChooseTetrisPanel.SetActive(isOpen);

        stageFinishPanel.SetActive(false);

        StageManager.Instance.OnClearStage += FinishStage;
    }

    private void OnDisable()
    {
        StageManager.Instance.OnClearStage -= FinishStage;
        optionButton.onClick.RemoveAllListeners();
        tetrisPanelOnOff.onClick.RemoveAllListeners();
        nextStageButton.onClick.RemoveAllListeners();

        starCts?.Cancel();
        starCts?.Dispose();
    }

    private void ClickOption(BaseUI ui)
    {
        UIManager.Instance.AddPopUp(ui);
    }

    private void OnOffTetris()
    {
        isOpen = !isOpen;
        ChooseTetrisPanel.SetActive(isOpen);
    }

    public void FinishStage()
    {
        int starIndex = 0;


        for (int i = StageManager.Instance.curStageMoveLevel.Length - 1; i >= 0; i--)
        {
            var count = StageManager.Instance.moveCount;
            var data = StageManager.Instance.curStageMoveLevel[i];

            if (count < data)
            {
                starIndex = i + 1;
                break;
            }
        }

        ResetUI(false);

        PlayStarAnimation(starIndex).Forget();
    }

    private async UniTaskVoid PlayStarAnimation(int starIndex)
    {
        // 이전 작업 취소
        starCts?.Cancel();
        starCts?.Dispose();
        starCts = new CancellationTokenSource();

        var token = starCts.Token;

        for (int i = 0; i < starIndex; i++)
        {
            await UniTask.Delay(1000, cancellationToken: token);

            var star = stars[i];
            var rect = star.GetComponent<RectTransform>();

            star.SetActive(true);

            // 1단계: 0 → 1 스케일 (뿅 등장)
            rect.localScale = Vector3.zero;

            await rect
                .DOScale(1f, 0.25f)
                .SetEase(Ease.OutBack)
                .AsyncWaitForCompletion();

            //// 2단계: 살짝 통통 튀기
            //await rect
            //    .DOPunchScale(Vector3.one * 0.3f, 0.3f, 5, 0.7f)
            //    .SetEase(Ease.OutQuad)
            //    .AsyncWaitForCompletion();
        }

        // 모든 별 등장 완료 후 Next 버튼 활성화
        nextStageButton.gameObject.SetActive(true);
    }
    private void NextStage()
    {
        ResetUI(true);
        StageManager.Instance.curStage++;
        DrawGrid.Instance.DrawGridFromChildren().Forget();
    }

    private void ResetUI(bool value)
    {
        for (int i = 0; i < stars.Length; i++)
        {
            stars[i].SetActive(value);
        }

        nextStageButton.gameObject.SetActive(value);
        stageFinishPanel.SetActive(!value);


        for (int i = 0; i < StageManager.Instance.curStageMoveLevel.Length; i++)
            StageManager.Instance.curStageMoveLevel[i] = 0;

        StageManager.Instance.moveCount = 0;
    }
}
