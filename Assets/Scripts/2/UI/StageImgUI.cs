using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class StageImgUI : MonoBehaviour
{
    private Button stageButton;
    [SerializeField] private int stageIndex;

    public bool isOpen;
    public bool IsOpen { get { return isOpen; } set { isOpen = value; OnOpen?.Invoke(isOpen); } }
    public Action<bool> OnOpen;

    private void Awake()
    {
        stageButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        StageManager.Instance.OnChangeMaxStage += OpenImg;
        OnOpen += OpenImgActive;
        stageButton.onClick.AddListener(ChangeStage);

        OpenImg(StageManager.Instance.MaxStage);
    }

    private void OnDisable()
    {
        StageManager.Instance.OnChangeMaxStage -= OpenImg;
        OnOpen -= OpenImgActive;
        stageButton.onClick.RemoveAllListeners();
    }

    public void Init(int _stageIndex)
    {
        stageIndex = _stageIndex;
        IsOpen = false;

        OpenImg(StageManager.Instance.MaxStage);
    }


    private void OpenImg(int data)
    {
        IsOpen = data >= stageIndex;
    }

    private async void OpenImgActive(bool value)
    {
        stageButton.interactable = value;

        if (!value)
            return;

        if (stageIndex <= 0) 
            return;

        var sprite = await DataManager.Instance.LoadSprite($"StageImg{stageIndex}");
        if (sprite != null)
            gameObject.GetComponent<Image>().sprite = sprite;
    }

    private void ChangeStage()
    {
        StageManager.Instance.isFinishStage = false;
        StageManager.Instance.CurStage = stageIndex;
        DrawGrid.Instance.DrawGridFromChildren().Forget();
    }
}
