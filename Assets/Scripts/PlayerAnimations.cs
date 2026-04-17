using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimations : MonoBehaviour
{
    [Header("Ajustes de Movimiento")]
    public float walkSpeed = 5f;
    public float crouchSpeed = 2f;
    public float rotationSpeed = 10f;

    private Rigidbody _rb;
    private Animator _animator;
    private Vector2 _moveInput;
    private bool _isCrouching;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _animator = GetComponent<Animator>();
    }

    void Update()
    {
        ApplyMovement();
    }

    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
        _animator.SetFloat("Speed", _moveInput.magnitude);
    }

    public void OnCrouch(InputValue value)
    {
        _isCrouching = value.isPressed;
        _animator.SetBool("isCrouching", _isCrouching);
    }

    private void ApplyMovement()
    {
        float currentSpeed = _isCrouching ? crouchSpeed : walkSpeed;

        Vector3 move = new Vector3(_moveInput.x, 0, _moveInput.y).normalized;

        if (move.magnitude >= 0.1f)
        {
            transform.Translate(move * currentSpeed * Time.deltaTime, Space.World);

            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}