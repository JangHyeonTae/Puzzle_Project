using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveDataManager
{
    private readonly string _jsonPath;

    public SaveDataManager(string fileName = "SaveData.json")
    {
        // 프로젝트 경로: Assets/Plugins/JsonData
        // Application.dataPath == <project>/Assets
        _jsonPath = Path.Combine(Application.dataPath, "Plugins", "JsonData", fileName);
    }

    /// <summary>
    /// 데이터를 JSON 파일로 저장. 파일이 없으면 생성 후 저장.
    /// </summary>
    public void Save<T>(T data)
    {
        EnsureDirectoryExists();
        if (data == null) return;

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(_jsonPath, json); // 없으면 자동 생성
    }

    /// <summary>
    /// JSON 파일에서 데이터를 불러와 반환. 파일이 없으면 생성하고 기본값(T new())을 반환.
    /// </summary>
    public T Load<T>() where T : class, new()
    {
        EnsureDirectoryExists();

        if (!File.Exists(_jsonPath))
        {
            var created = new T();
            Save(created);
            return created;
        }

        string json = File.ReadAllText(_jsonPath);
        if (string.IsNullOrEmpty(json))
        {
            var created = new T();
            Save(created);
            return created;
        }

        T loaded = JsonUtility.FromJson<T>(json);
        if (loaded == null)
        {
            loaded = new T();
            Save(loaded);
        }

        return loaded;
    }

    /// <summary>
    /// JSON 파일에서 데이터를 불러와 target에 덮어씀. 파일이 없으면 생성하고 target을 그대로 둠.
    /// </summary>
    public void LoadInto<T>(T target) where T : class, new()
    {
        if (target == null) return;

        EnsureDirectoryExists();
        if (!File.Exists(_jsonPath))
        {
            Save(new T());
            return;
        }

        string json = File.ReadAllText(_jsonPath);
        if (string.IsNullOrEmpty(json)) return;

        JsonUtility.FromJsonOverwrite(json, target);
    }

    private void EnsureDirectoryExists()
    {
        string directory = Path.GetDirectoryName(_jsonPath);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }
}
