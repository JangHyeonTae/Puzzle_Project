using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideFrame : MonoBehaviour
{
    [SerializeField] private float rotSpeed;
    [SerializeField] private float returnSpeed;
    private Vector2 lastTouchPosition;
    private Quaternion originalRotation;

    private bool isRotating;
    private void Start()
    {
        originalRotation = transform.localRotation;
        isRotating = false;
    }

    private void Update()
    {
        
        if (Input.touchCount > 0 )
        {
            Touch touch = Input.GetTouch(0);

            if (Input.GetTouch(0).phase == TouchPhase.Began)
            {

                lastTouchPosition = Input.GetTouch(0).position;
                isRotating = true;
            }
            else if (Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                float deltaX = touch.position.x - lastTouchPosition.x;
                transform.Rotate(0, 0, deltaX * rotSpeed);
                lastTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isRotating = false;
            }
        }

        if (!isRotating)
        {
            transform.localRotation = Quaternion.Lerp(
                transform.localRotation,
                originalRotation,
                Time.deltaTime * returnSpeed
            );
        }
    }
}
