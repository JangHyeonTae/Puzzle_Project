using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI0 : PooledObject, ClickUI
{
    
    public void Init()
    {
        gameObject.SetActive(true);
    }

    public void Outit()
    {
        gameObject.SetActive(false);
    }
}
