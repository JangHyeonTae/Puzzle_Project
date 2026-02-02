using Cysharp.Threading.Tasks;
using System.Net;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class DataManager : Singleton<DataManager>
{
    public async UniTask<GameObject> LoadStagePrefab(int value)
    {
        string address = value.ToString();

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(address);

        GameObject prefab = null;
        try
        {
            prefab = await handle.Task;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LoadStagePrefab failed. address={address} error={e}");
            Addressables.Release(handle);
            return null;
        }

        if (prefab == null)
        {
            Debug.LogError($"LoadStagePrefab failed. address={address} result is null");
            Addressables.Release(handle);
            return null;
        }

        // 주의: 이 프리팹은 사용 후 반드시 Release 해야 합니다
        return prefab;
    }

    public void ReleaseStagePrefab(GameObject prefab)
    {
        if (prefab != null)
        {
            Addressables.Release(prefab);
        }
    }

    public async UniTask<GameObject> LoadTetrisPrefab()
    {
        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>("TetrisSample");
        GameObject prefab = null;

        try
        {
            prefab = await handle.Task;
        }
        catch (System.Exception e)
        {
            Debug.Log("Exception e 취소");
            Addressables.Release(handle);
            return null;
        }

        if (prefab == null)
        {
            Debug.Log("Prefab null 취소");
            Addressables.Release(handle);
            return null;
        }

        // 주의: 이 프리팹은 사용 후 반드시 Release 해야 합니다
        return prefab;
    }

    public void ReleaseTetrisPrefab(GameObject prefab)
    {
        if (prefab != null)
            Addressables.Release(prefab);
    }

}
