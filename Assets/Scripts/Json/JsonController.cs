using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JsonController
{
    private static SaveDataManager _saveDataManager;
    private static SaveData _saveData;


    public static SaveData Data => _saveData ??= new SaveData();

    /// <summary>
    /// JSON 파일에 SaveData 저장. (인자를 넘기면 그 데이터를 저장하고 내부 데이터도 갱신)
    /// </summary>
    public static void Save(SaveData data = null)
    {
        _saveData ??= new SaveData();
        _saveDataManager ??= new SaveDataManager();
        if (data != null) _saveData = data;

        if (data.curStage <= 0)
            data.curStage = 1;

        if (data.maxStage <= 0)
            data.maxStage = 1;

        _saveDataManager.Save(Data);
        Debug.Log("Save");
    }

    /// <summary>
    /// JSON 파일에서 SaveData를 불러와 내부 데이터로 설정.
    /// </summary>
    public static void Load()
    {
        _saveData ??= new SaveData();
        _saveDataManager ??= new SaveDataManager();
        _saveData = _saveDataManager.Load<SaveData>();

        if(_saveData.maxStage <= 0)
            _saveData.maxStage = 1;

        if(_saveData.curStage <= 0)
            _saveData.curStage = 1;

        Debug.Log("Load");
    }
}
