using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageChoosePopUp : BaseUI
{
    [SerializeField] private Button outButton;
    [SerializeField] private Transform stagePrefabParent;

    StageImgUI stageImgPrefab;

    private async void Start()
    {
        GameObject data = await DataManager.Instance.LoadData("StageImgPrefab");
        stageImgPrefab = data.GetComponent<StageImgUI>();
        for (int i = 1; i <= 5; i++)
        {
            var inst = Instantiate(stageImgPrefab, stagePrefabParent);
            inst.gameObject.SetActive(false);
            inst.Init(i);
            inst.gameObject.SetActive(true);
        }
    }

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
