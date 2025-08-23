using UnityEngine;

public class RTSCameraController : MonoBehaviour
{
    [Header("References")]
    public Transform cameraHolder;  // L'objet qui contient la caméra (permet rotation/zoom)
    public Camera mainCamera;

    [Header("Movement Settings")]
    public float moveSpeed = 100f;

    [Header("Zoom Settings")]
    public float zoomSpeed = 2000f;
    public float minZoom = 25f;
    public float maxZoom = 150f;
    private bool scrollHasChanged = false;

    [Header("Rotation Settings")]
    public float rotationSpeed = 200f;

    [Header("Movement Bounds")]
    public Vector2 xBounds = new Vector2(-500f, 500f);
    public Vector2 zBounds = new Vector2(-500f, 500f);

    void Update()
    {
        if (HandleMovement() || HandleZoom() || HandleRotation())
        {
            UIManager.Instance.RefreshLevels();
            scrollHasChanged = false;
        }
    }

    bool HandleMovement()
    {
        Vector3 inputDir = Vector3.zero;

        // Clavier
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.W)) inputDir += Vector3.forward;
        if (Input.GetKey(KeyCode.S)) inputDir += Vector3.back;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.A)) inputDir += Vector3.left;
        if (Input.GetKey(KeyCode.D)) inputDir += Vector3.right;

        // Appliquer le mouvement
        Vector3 move = Quaternion.Euler(0f, transform.eulerAngles.y, 0f) * inputDir.normalized;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Clamp dans les limites
        transform.position = new Vector3(
            Mathf.Clamp(transform.position.x, xBounds.x, xBounds.y),
            transform.position.y,
            Mathf.Clamp(transform.position.z, zBounds.x, zBounds.y)
        );

        return inputDir != Vector3.zero;
    }

    bool HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            scrollHasChanged = true;
            Vector3 dir = mainCamera.transform.localPosition.normalized;
            float distance = mainCamera.transform.localPosition.magnitude;
            distance -= scroll * zoomSpeed * Time.deltaTime;
            distance = Mathf.Clamp(distance, minZoom, maxZoom);
            mainCamera.transform.localPosition = dir * distance;
        }

        return scroll == 0f && scrollHasChanged;
    }

    bool HandleRotation()
    {
        // Clavier
        float mouseX = Input.GetKey(KeyCode.LeftArrow) ? -1 : (Input.GetKey(KeyCode.RightArrow) ? 1 : 0);
        float mouseY = Input.GetKey(KeyCode.DownArrow) ? -1 : (Input.GetKey(KeyCode.UpArrow) ? 1 : 0);
        transform.Rotate(Vector3.up, mouseX * rotationSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right, -mouseY * rotationSpeed * Time.deltaTime);

        return !(mouseX == 0f && mouseY == 0f);
    }
}
