using UnityEngine;

public class RTSCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraHolder;
    public Camera mainCamera;

    [Header("Movement Settings")]
    public float moveSpeed = 0.1f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 50f;
    public float maxZoom = 500f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 0.2f;

    [Header("Bounds")]
    public Vector2 xBounds = new Vector2(-500f, 500f);
    public Vector2 zBounds = new Vector2(-500f, 500f);

    private float lastPinchDistance = 0f;
    private bool IsMoving = true;
    private Vector2 rotationStartPos;

    void Update()
    {
        HandleTouchInput();
    }

    void HandleTouchInput()
    {
        int count = Input.touchCount;

        if (count == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                float screenMid = Screen.width / 2f;

                if (IsMoving)
                {
                    // Déplacement (doigt à gauche)
                    Vector3 move = new Vector3(-delta.x, 0f, -delta.y) * moveSpeed;
                    move = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * move;
                    transform.position += move;
                    ClampPosition();
                }
                else
                {
                    // Rotation (doigt à droite)
                    float rotX = -delta.y * rotationSpeed;
                    float rotY = delta.x * rotationSpeed;

                    // Appliquer la rotation
                    transform.Rotate(Vector3.right, rotX, Space.Self);   // inclinaison verticale
                    transform.Rotate(Vector3.up, rotY, Space.World);
                    // Limiter l'inclinaison verticale entre 30° et 85°
                    Vector3 currentRotation = transform.eulerAngles;
                    currentRotation.x = Mathf.Clamp(currentRotation.x, 30f, 85f);
                    transform.eulerAngles = currentRotation;
                }
            }
        }
        else if (count == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            float currentDistance = Vector2.Distance(t0.position, t1.position);

            if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                if (lastPinchDistance > 0f)
                {
                    float delta = currentDistance - lastPinchDistance;
                    ZoomCamera(-delta * zoomSpeed); // distance ↑ → dézoome ; ↓ → zoome
                }

                lastPinchDistance = currentDistance;
            }
        }
        else
        {
            lastPinchDistance = 0f;
        }
    }

    public void SetIsMoving() => IsMoving = !IsMoving;

    void ZoomCamera(float delta)
    {
        Vector3 dir = mainCamera.transform.localPosition.normalized;
        float distance = mainCamera.transform.localPosition.magnitude;
        distance -= delta;
        distance = Mathf.Clamp(distance, minZoom, maxZoom);
        mainCamera.transform.localPosition = dir * distance;
    }

    void ClampPosition()
    {
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, xBounds.x, xBounds.y),
            transform.position.y,
            Mathf.Clamp(transform.position.z, zBounds.x, zBounds.y)
        );
    }
}
