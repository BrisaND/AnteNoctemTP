using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using static UnityEngine.EventSystems.EventTrigger;

public class Movement : Human
{
    [Header("Referencias")]
    public InputActionReference moveAction;

    [Header("Ajustes")]
    public float speed = 5f;
    public float crouchSpeed = 5f;

    private Controls _controls;
    private Animator _animator;

    void Start()
    {
        _animator = GetComponent<Animator>();
        _controls = new Controls(moveAction, this);
    }

    void Update()
    {
        _controls.ArtificialUpdate();
    }

    public void Move(Vector3 inputDirection)
    {
        bool isActuallyCrouched = _animator != null && _animator.GetBool("isCrouching");
        float currentSpeed = isActuallyCrouched ? crouchSpeed : speed;

        if (inputDirection.magnitude >= 0.1f)
        {
            Vector3 moveDir = transform.TransformDirection(inputDirection);

            transform.position += moveDir * currentSpeed * Time.deltaTime;
        }
    }

    public void UpdateAnimation(float intensity)
    {
        if (_animator != null)
        {
            _animator.SetFloat("speed", intensity);
        }
    }

    public void Crouch(bool state)
    {
        if (_animator != null)
        {
            _animator.SetBool("isCrouching", state);
        }
    }
}