using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainCanvas : MonoBehaviour
{
    [SerializeField] private Button optionButton;
    [SerializeField] private BaseUI optionPopUp;

    private void OnEnable()
    {
        optionButton.onClick.AddListener(() => ClickOption(optionPopUp));
    }

    private void OnDisable()
    {
        optionButton.onClick.RemoveListener(() => ClickOption(optionPopUp));
    }

    private void ClickOption(BaseUI ui)
    {
        UIManager.Instance.AddPopUp(ui);
    }
}
