using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private int maxStage;
    public int MaxStage { get => maxStage;  set { maxStage = value; OnChangeMaxStage?.Invoke(maxStage); }  }
    public Action<int> OnChangeMaxStage;

    private int curStage;
    public int CurStage { get => curStage; set { curStage = value; OnChangeStage?.Invoke(curStage); } } 

    public int moveCount;
    public int[] curStageMoveLevel;
    public bool isStageChange;

    public TeterisPrefab tetrisPrefab;
    public ObjectPool tetrisPool;
    public GameObject tetrisParent;

    public event Action OnFinishStage;
    public event Action OnExitGame;
    public event Action OnEnterGame;

    public Action<int> OnChangeStage;

    private StageClearAnim stageClearAnim;
    private CancellationTokenSource stageCts;

    public SaveData saveData;

    protected void Awake()
    {
        base.Awake();

        stageCts = new CancellationTokenSource();

        JsonController.Load();

        saveData = JsonController.Data;
        maxStage = saveData.maxStage;
        curStage = saveData.curStage;

        if (stageClearAnim == null)
            stageClearAnim = GetComponentInChildren<StageClearAnim>(true);

        curStageMoveLevel = new int[3];
    }

    private void Start()
    {
        InitStage().Forget();
    }
    private void OnDestroy()
    {
        stageCts?.Cancel();
        stageCts?.Dispose();
    }

    private async UniTaskVoid InitStage()
    {
        var token = stageCts.Token;

        if (tetrisPrefab == null)
        {
            var data = await DataManager.Instance.LoadTetrisPrefab().AttachExternalCancellation(token);

            if (token.IsCancellationRequested)
                return;

            TeterisPrefab inst = data.GetComponent<TeterisPrefab>();
            tetrisPrefab = inst;
        }

        if (tetrisPool == null)
        {
            InstTetrisPool();
        }
    }
    public void Save()
    {
        saveData.maxStage = maxStage;
        saveData.curStage = curStage;
        JsonController.Save(saveData);
    }
    private void InstTetrisPool()
    {
        tetrisParent = new GameObject($"TetrisParent");
        tetrisParent.transform.parent = this.transform;
        tetrisPool = new ObjectPool(tetrisPrefab, 100, tetrisParent.transform, false);
    }

    public void ClearStage()
    {
        //해당 부분에서 실행
        stageClearAnim.PlayClearEffect().Forget();

        for (int i = 0; i < tetrisParent.transform.childCount; i++)
        {
            Transform child = tetrisParent.transform.GetChild(i);

            if (child.gameObject.activeSelf)
            {
                var data = child.GetComponent<TeterisPrefab>();
                data.Outit().Forget();
            }
        }

        ChangeStage();
    }


    public void EnterGame()
    {
        OnEnterGame?.Invoke();
    }

    public void SaveStage()
    {
        OnExitGame?.Invoke();
    }

    public void ChangeStage()
    {
        OnFinishStage?.Invoke();
    }
}
