using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionPopUp : BaseUI
{
    [SerializeField] private Button outButton;

    public override void Init()
    {
        base.Init();
        outButton.onClick.AddListener(OutBtn);
    }

    public override void Outit()
    {
        outButton.onClick.RemoveListener(OutBtn);
        base.Outit();
    }

}
