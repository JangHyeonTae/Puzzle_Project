using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Demo : MonoBehaviour
{
    int value = 0;
    Canvas mainCanvas;
    private void Start()
    {
        value = -1;
        mainCanvas = FindObjectOfType<UIManager>().GetComponentInChildren<Canvas>();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            
        }
    }

    
}
