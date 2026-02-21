using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UISafeArea : MonoBehaviour
{
    private Camera mainCamera;
    public RectTransform safeAreaRect;

    private void Awake()
    {
        mainCamera = Camera.main;
    }


    private void SafeAreaRectInit()
    {

    }
}
