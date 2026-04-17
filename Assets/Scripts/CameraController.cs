using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public InputActionReference controlLook;
    public Transform playerTransform;
    public float sensitivity = 0.5f;

    private float _upDown;
    private float _limitDown;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        float initialAngle = transform.localEulerAngles.x;

        if (initialAngle > 180) initialAngle -= 360;

        _limitDown = initialAngle;
        _upDown = _limitDown;
    }

    void Update()
    {
        Vector2 look = controlLook.action.ReadValue<Vector2>();

        _upDown -= look.y * sensitivity;
        _upDown = Mathf.Clamp(_upDown, -60f, _limitDown);

        transform.localRotation = Quaternion.Euler(_upDown, 0, 0);

        playerTransform.Rotate(Vector3.up * look.x * sensitivity);
    }
}