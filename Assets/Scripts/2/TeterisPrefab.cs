using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

public class TeterisPrefab : MonoBehaviour
{
    private Transform[] childrenPos;

    private CancellationTokenSource token;

    //확인용
    public Vector3[] childrenVec;

    public event Action OnChangeRot;
    public void Start()
    {
        int childCount = transform.childCount;

        childrenPos = new Transform[childCount];
        childrenVec = new Vector3[childCount]; 

        for (int i = 0; i < childCount; i++)
        {
            childrenPos[i] = transform.GetChild(i);

            childrenVec[i] = childrenPos[i].position;
        }

        OnChangeRot += ChangeRot;
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
        for(int i =0; i < transform.childCount; i++)
        {
            //테트리스 회전에 대해 공부
            var pos = transform.GetChild(i).position;
            pos = new Vector3(pos.x - 0.5f, pos.y - 0.5f, pos.z);
        }
    }

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
