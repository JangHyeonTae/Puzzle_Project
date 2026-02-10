using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : Singleton<StageManager>
{
    public int curStage;
    public int moveCount;


    public TeterisPrefab tetrisPrefab;
    public ObjectPool tetrisPool;
    public GameObject tetrisParent;


    protected void Awake()
    {
        base.Awake();
        curStage = 1;

    }

    private void Start()
    {
        FindTetris();
    }

    private async void FindTetris()
    {
        if(tetrisPrefab == null)
        {
            var data = await DataManager.Instance.LoadTetrisPrefab();
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
    public void UpStage()
    {
        curStage++;
        ClearStage();
    }

    public void ClearStage()
    {
        for (int i = 0; i < tetrisParent.transform.childCount; i++)
        {
            Transform child = tetrisParent.transform.GetChild(i);

            if (child.gameObject.activeSelf)
            {
                var data = child.GetComponent<TeterisPrefab>();
                data.Outit();
            }
        }

    }
}
