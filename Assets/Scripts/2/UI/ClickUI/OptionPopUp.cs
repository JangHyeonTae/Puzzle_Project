using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionPopUp : BaseUI
{
    [SerializeField] private Button outButton;
    [SerializeField] private Button setButton;

    public override void Init()
    {
        base.Init();
        outButton.onClick.AddListener(OutBtn);
        setButton.onClick.AddListener(SetBtn);
    }

    public override void Outit()
    {
        outButton.onClick.RemoveListener(OutBtn);
        setButton.onClick.RemoveListener(SetBtn);
        base.Outit();
    }

}
