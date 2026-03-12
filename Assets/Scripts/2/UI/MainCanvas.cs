using UnityEngine;
using UnityEngine.UI;

public enum PopUpType
{
    StageChoose,
    StageFinish
}

public class MainCanvas : MonoBehaviour
{
    [SerializeField] private Button stageChooseBtn;
    [SerializeField] private Button tetrisPanelOnOff;

    [SerializeField] private GameObject ChooseTetrisPanel;

    [SerializeField] private BaseUI stageChoosePopUp;
    [SerializeField] private BaseUI stageFinishPopUp;
    //Stage可记 捞固瘤积己

    private bool isOpen;


    private void OnEnable()
    {
        stageChooseBtn.onClick.AddListener(() => MainCanvasAddPopUp(PopUpType.StageChoose));
        tetrisPanelOnOff.onClick.AddListener(OnOffTetris);
        isOpen = true;
        ChooseTetrisPanel.SetActive(isOpen);

    }

    private void OnDisable()
    {
        stageChooseBtn.onClick.RemoveAllListeners();
        tetrisPanelOnOff.onClick.RemoveAllListeners();
    }

    public void MainCanvasAddPopUp(PopUpType type)
    {
        UIManager.Instance.AddPopUp(GetUIType(type));
    }

    private BaseUI GetUIType(PopUpType type)
    {
        switch (type)
        {
            case PopUpType.StageFinish:
                return stageFinishPopUp;
            case PopUpType.StageChoose:
                return stageChoosePopUp;
        }
        return null;
    }
    private void OnOffTetris()
    {
        isOpen = !isOpen;
        ChooseTetrisPanel.SetActive(isOpen);
    }
}
