using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    public int curStage;
    public int moveCount;
    public int[] curStageMoveLevel;
    public bool isStageChange;

    public TeterisPrefab tetrisPrefab;
    public ObjectPool tetrisPool;
    public GameObject tetrisParent;

    public event Action OnClearStage;

    private StageClearAnim stageClearAnim;
    private CancellationTokenSource stageCts;

    protected void Awake()
    {
        base.Awake();

        stageCts = new CancellationTokenSource();

        curStage = 4;

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

        OnClearStage?.Invoke();
    }

    
}
