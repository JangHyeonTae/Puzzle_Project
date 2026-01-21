using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolySample : MonoBehaviour
{
    PolySO curSO;
    string polyName;
    public Color polyColor;

    public void Init(PolySO so)
    {
        curSO = so;
        polyName = so.polyName;
        polyColor = so.objColor;
    }
}
