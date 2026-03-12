using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class Demo : MonoBehaviour
{
    public Button btn;
    private void Start()
    {
        btn.onClick.AddListener(Click);
    }

    private void Click()
    {
        StageManager.Instance.CurStage++;
        StageManager.Instance.MaxStage++;

        StageManager.Instance.Save();
        
    }



}
