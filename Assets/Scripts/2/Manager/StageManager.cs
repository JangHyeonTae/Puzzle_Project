using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    [SerializeField] private int maxStage;
    public int MaxStage 
    { 
        get => maxStage;  
        set 
        {
            if (maxStage > 5)
                return;

            maxStage = value; OnChangeMaxStage?.Invoke(maxStage); 
        }  
    }
    public Action<int> OnChangeMaxStage;

    [SerializeField] private int curStage;
    public int CurStage 
    { 
        get => curStage; 
        set 
        {
            if (curStage > 5)
                return;

            curStage = value; 
            OnChangeStage?.Invoke(curStage); 
        } 
    }
    public Action<int> OnChangeStage;

    public int moveCount;
    public int[] curStageMoveLevel;
    public bool isStageChange;

    public TeterisPrefab tetrisPrefab;
    public ObjectPool tetrisPool;
    public GameObject tetrisParent;

    private StageClearAnim stageClearAnim;
    private CancellationTokenSource stageCts;

    public SaveData saveData;

    public bool isFinishStage;
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
        OnChangeStage += ClearStage;
    }
    private void OnDestroy()
    {
        OnChangeStage -= ClearStage;
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

    public void ClearStage(int value)
    {
        if(isFinishStage)
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

        isFinishStage = true;
    }


}
