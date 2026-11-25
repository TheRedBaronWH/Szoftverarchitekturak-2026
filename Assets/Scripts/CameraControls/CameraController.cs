using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private Transform cameraTarget;

    [SerializeField]
    private Vector3 initialOffset;

    [SerializeField]
    private float sensitivity = 0.01f;

    [SerializeField]
    private float rotationLimit = 7.5f;

    private Vector3 offset;

    private InputAction move;
    private InputAction rotate;


    private void Awake()
    {
        offset = initialOffset;

        move = new InputAction(binding: "<Mouse>/rightButton");
        rotate = new InputAction(binding: "<Mouse>/leftButton");
    }

    private void OnEnable()
    {
        move.Enable();
        rotate.Enable();
    }

    private void OnDisable()
    {
        move.Disable();
        rotate.Disable();
    }

    public void ResetCamera()
    {
        cameraTarget.position = Vector3.zero;
        offset = initialOffset;
    }

    private void Update()
    {
        Vector2 delta = Mouse.current.delta.ReadValue();

        if (move.IsPressed())
        {
            cameraTarget.position -= (Vector3)delta * sensitivity;
        }

        else if (rotate.IsPressed())
        {
            offset -= delta.x * sensitivity * Vector3.right;
            offset = new Vector3(Mathf.Clamp(offset.x, -rotationLimit, rotationLimit), offset.y, offset.z);
        }
        transform.position = cameraTarget.position + offset;
        transform.LookAt(cameraTarget);
    }
}
