using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageImgUI : MonoBehaviour
{
    private Button stageButton;
    [SerializeField] private int stageIndex;

    private bool isOpen;
    public bool IsOpen { get { return isOpen; } set { value = isOpen; OnOpen?.Invoke(isOpen); } }
    public Action<bool> OnOpen;


    private void OnEnable()
    {
        StageManager.Instance.OnChangeMaxStage += OpenIntImg;
        OnOpen += OpenBoolImg;
        stageButton.onClick.AddListener(ChangeStage);
    }

    private void OnDisable()
    {
        StageManager.Instance.OnChangeMaxStage -= OpenIntImg;
        OnOpen -= OpenBoolImg;
        stageButton.onClick.RemoveAllListeners();
    }

    public void Init(int _stageIndex)
    {
        if (StageManager.Instance.MaxStage <= stageIndex + 1)
            stageIndex = _stageIndex;
    }

    public void OpenIntImg(int value)
    {
        if (value >= stageIndex + 1)
        {
            isOpen = true;
        }
    }

    private async void OpenBoolImg(bool value)
    {
        int index = value ? 1 : 0;
        stageButton.interactable = value;

        var sprite = await DataManager.Instance.LoadSprite($"StageImg{stageIndex + 1}");
        if (sprite != null)
            gameObject.GetComponent<Image>().sprite = sprite;
    }

    private void ChangeStage()
    {
        StageManager.Instance.CurStage = stageIndex + 1;
    }
}
