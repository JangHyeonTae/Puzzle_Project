using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class TeterisPrefab : MonoBehaviour
{
    [SerializeField] private TeterisBlock blockSO;
    private Transform[] childrenPos;
    private Sprite tetrisSprite;
    private int curRotIndex;

    private CancellationTokenSource token;

    //확인용
    public Vector2[] childrenVec;

    public event Action OnChangeRot;


    public void Start()
    {
        int childCount = transform.childCount;
        curRotIndex = 0;

        childrenPos = new Transform[childCount];
        childrenVec = new Vector2[childCount];

        for (int i = 0; i < childCount; i++)
        {
            childrenPos[i] = transform.GetChild(i);

            childrenVec[i] = childrenPos[i].position;
        }

        OnChangeRot += ChangeRot;
        childrenVec = blockSO.posVectors[curRotIndex].blockPos;
        

        for (int i = 0; i < childCount; i++)
        {
            var childPrefab = transform.GetChild(i);
            childPrefab.GetComponent<SpriteRenderer>().sprite = blockSO.tetrisSprite;
            childPrefab.localPosition = blockSO.posVectors[curRotIndex].blockPos[i];
        }


    }

    private void OnDisable()
    {
        OnChangeRot -= ChangeRot;
    }

    private void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                StartLongPressTimer().Forget();
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                CancelTimer();
            }
        }
    }

    private async UniTaskVoid StartLongPressTimer()
    {
        CancelTimer();
        token = new CancellationTokenSource();

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: token.Token);
            OnChangeRot.Invoke();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("cancel");
        }
    }

    private void ChangeRot()
    {
        //if (!BlockCheck())
        //    return;

        curRotIndex++;

        if (curRotIndex >= 4)
            curRotIndex = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            var childPrefab = transform.GetChild(i);
            childPrefab.localPosition = blockSO.posVectors[curRotIndex].blockPos[i];
        }
    }

    //private bool BlockCheck()
    //{

    //    for (int i = 0; i < transform.childCount; i++)
    //    {
    //        Vector2 checkBlockVec = (Vector2)transform.position + blockSO.posVectors[curRotIndex].blockPos[i];
    //        if (전체 타일중 현재 벡터가 포함되어 있다면)
    //        {
    //         return false;
    //        }

    //    }
    //    return true;
    //}


    private void CancelTimer()
    {
        if (token != null)
        {
            token.Cancel();
            token.Dispose();
            token = null;
        }
    }


    private void OnDestroy()
    {
        CancelTimer();
    }
}
