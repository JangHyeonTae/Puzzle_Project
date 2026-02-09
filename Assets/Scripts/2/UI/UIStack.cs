using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStack
{
    public Stack<BaseUI> popUpStack = new Stack<BaseUI>();

    public void AddUI(BaseUI popup)
    {
        if (popUpStack.Count > 0)
        {
            var top = popUpStack.Peek();
            top.Outit();
        }

        popUpStack.Push(popup);
        var inst = popUpStack.Peek();
        inst.Init();
    }

    public void RemoveUI()
    {
        if (popUpStack.Count == 0)
            return;

        BaseUI top = popUpStack.Pop();
        top.Outit();

        if (popUpStack.Count > 0)
        {
            top = popUpStack.Peek();
            top.Init();
        }
    }

    public void AllRemoveUI()
    {
        while (popUpStack.Count > 0)
        {
            BaseUI top = popUpStack.Peek();
            top.Outit();
        }
    }
}
