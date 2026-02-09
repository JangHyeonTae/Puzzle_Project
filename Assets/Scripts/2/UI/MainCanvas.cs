using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : MonoBehaviour
{
    [SerializeField] private Button optionButton;
    [SerializeField] private BaseUI optionPopUp;
    [SerializeField] private Button tetrisPanelOnOff;
    [SerializeField] private GameObject ChooseTetrisPanel;

    private bool isOpen;
    private void OnEnable()
    {
        optionButton.onClick.AddListener(() => ClickOption(optionPopUp));
        tetrisPanelOnOff.onClick.AddListener(OnOffTetris);
        isOpen = true;
        ChooseTetrisPanel.SetActive(isOpen);
    }

    private void OnDisable()
    {
        optionButton.onClick.RemoveListener(() => ClickOption(optionPopUp));
        tetrisPanelOnOff.onClick.RemoveListener(OnOffTetris);
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
}
