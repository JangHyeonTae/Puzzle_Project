using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class JsonController
{
    private static SaveDataManager _saveDataManager;
    private static SaveData _saveData;


    public static SaveData Data => _saveData ??= new SaveData();

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
    }

    public static void Load()
    {
        _saveData ??= new SaveData();
        _saveDataManager ??= new SaveDataManager();
        _saveData = _saveDataManager.Load<SaveData>();

        if(_saveData.maxStage <= 0)
            _saveData.maxStage = 1;

        if(_saveData.curStage <= 0)
            _saveData.curStage = 1;

    }
}
