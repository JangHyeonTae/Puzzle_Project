using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUI : MonoBehaviour
{
    public virtual void Init()
    {
        if(!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    public virtual void Outit()
    {
        if(gameObject.activeSelf)
            gameObject.SetActive(false);
    }


    protected void OutBtn()
    {
        UIManager.Instance.RemovePopUp();
    }

    protected void SetBtn()
    {
        UIManager.Instance.RemovePopUp();
    }
}
