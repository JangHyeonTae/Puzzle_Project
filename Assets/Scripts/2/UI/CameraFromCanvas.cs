using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class CameraFromCanvas : MonoBehaviour
{
    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 0.01f;      // 핀치 속도
    [SerializeField] private float mouseScrollSpeed = 5f; // 마우스 휠 속도
    [SerializeField] private float minSize = 2f;          // 최소 줌
    [SerializeField] private float maxSize = 20f;         // 최대 줌

    public Canvas mainCanvas { get; set; }
    private RectTransform canvasRect { get; set; }
    private Camera cam;

    public async UniTask InitCanvas()
    {
        cam = Camera.main;

        if (mainCanvas == null)
        {
            var obj = await DataManager.Instance.LoadData("MainCanvas");
            if (obj != null)
            {
                mainCanvas = Instantiate(obj).GetComponent<Canvas>();
                mainCanvas.transform.SetParent(transform);
            }
        }

        if (mainCanvas != null)
            canvasRect = mainCanvas.GetComponent<RectTransform>();

        SyncCameraToCanvas();
    }

    private void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
            return;
        }

        HandlePinchZoom();

        //scroll 동작안함
        HandleMouseScroll();
    }

    // 1. 모바일 핀치 줌 처리
    private void HandlePinchZoom()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // 각 터치의 이전 프레임 위치 계산
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // 이전 프레임과 현재 프레임의 두 손가락 사이 거리 계산
            float prevMagnitude = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float currentMagnitude = (touchZero.position - touchOne.position).magnitude;

            // 거리의 차이만큼 줌 수치 결정
            float difference = currentMagnitude - prevMagnitude;

            Zoom(difference * zoomSpeed);
        }
    }

    // 2. PC 마우스 휠 처리 (테스트용)
    private void HandleMouseScroll()
    {
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel"); // GetAxisRaw로 날것의 데이터 받기
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // 스크롤 방향을 명확히 하고, 현재 사이즈에 비례해서 줌이 되도록 수정
            float zoomAmount = scroll * mouseScrollSpeed;
            Zoom(zoomAmount);
        }
    }

    // 실제 OrthographicSize 적용
    private void Zoom(float increment)
    {
        if (cam == null) return;

        float newSize = cam.orthographicSize - increment;
        cam.orthographicSize = Mathf.Clamp(newSize, minSize, maxSize);
    }

    public void SyncCameraToCanvas()
    {
        if (cam == null || canvasRect == null) return;

        // 초기 카메라 사이즈 설정 (기존 로직 유지)
        float halfWidth = canvasRect.rect.width / 2f;
        float aspect = (float)Screen.width / Screen.height;
        float cameraSize = halfWidth / aspect;

        float padding = 1.05f;
        cam.orthographicSize = Mathf.Clamp(cameraSize * padding, minSize, maxSize);
    }
}
