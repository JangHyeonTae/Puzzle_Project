using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSystem : MonoBehaviour
{
    public PolySample poly;
    public PolySO[] polySO;
    public int curStage;

    public void StartGame()
    {
        for(int i =0; i < curStage; i++)
        {
            PolySample obj = Instantiate(poly, transform).GetComponent<PolySample>();
            obj.Init(polySO[Random.Range(0,polySO.Length)]);
        }
        

        
    }
}
