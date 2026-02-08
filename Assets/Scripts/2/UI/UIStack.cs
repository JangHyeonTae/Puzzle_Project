using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIStack : MonoBehaviour
{
    private Stack<ClickUI> clickStack = new Stack<ClickUI>();

    public void AddUI(ClickUI popup)
    {
        if (clickStack.Count > 0)
        {
            var top = clickStack.Peek();
            top.Outit();
        }

        clickStack.Push(popup);
        var inst = clickStack.Peek();
        inst.Init();
    }

    public void RemoveUI()
    {
        if (clickStack.Count == 0)
            return;

        ClickUI top = clickStack.Pop();
        top.Outit();

        if (clickStack.Count > 0)
        {
            top = clickStack.Peek();
            top.Init();
        }
    }

    public void AllRemoveUI()
    {
        while (clickStack.Count > 0)
        {
            ClickUI top = clickStack.Peek();
            top.Outit();
        }
    }
}
