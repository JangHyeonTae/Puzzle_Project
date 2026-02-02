using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectPool
{
    private PooledObject poolPrefab;
    public GameObject poolParent;

    public List<PooledObject> poolList;

    public ObjectPool(PooledObject _poolPrefab, int _poolSize, Transform _parent = null, bool needParent = false)
    {
        SetupPool(_poolPrefab, _poolSize, _parent, needParent);
    }

    private void SetupPool(PooledObject _poolPrefab, int _poolSize, Transform _parent, bool needParent)
    {
        poolList = new List<PooledObject>(_poolSize);
        poolPrefab = _poolPrefab;

        if (!needParent)
        {
            poolParent = _parent.gameObject;
        }
        else
        {
            poolParent = new GameObject($"{_poolPrefab.name} Pool");
            poolParent.transform.parent = _parent;
        }

        for (int i = 0; i < _poolSize; i++)
        {
            PooledObject instance = MonoBehaviour.Instantiate(poolPrefab, poolParent.transform);
            instance.gameObject.SetActive(false);
            instance.PooledInit(this);
            poolList.Add(instance);
        }
    }


    public PooledObject GetPooled()
    {
        if (poolList.Count == 0)
        {
            PooledObject newInst = MonoBehaviour.Instantiate(poolPrefab, poolParent.transform);
            newInst.gameObject.SetActive(true);
            newInst.PooledInit(this);
            return newInst;
        }

        int lastIndex = poolList.Count - 1;
        PooledObject nextInst = poolList[lastIndex];
        poolList.RemoveAt(lastIndex);
        nextInst.gameObject.SetActive(true);
        nextInst.PooledInit(this);
        return nextInst;
    }

    public void ReturnToPool(PooledObject pooledObject)
    {
        pooledObject.gameObject.SetActive(false);
        pooledObject.PooledInit(this);
        //pooledObject.transform.parent = poolObject.transform;
        poolList.Add(pooledObject);
    }

    public void ReturAllPool(List<PooledObject> pools)
    {
        foreach (var p in pools)
        {
            p.gameObject.SetActive(false);
            p.PooledInit(this);
            poolList.Add(p);
        }
    }

}
