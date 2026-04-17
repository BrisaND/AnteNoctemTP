using UnityEngine;
using UnityEngine.InputSystem;

public class Controls
{
    InputActionReference _moveAction;
    Movement _movement;
    private bool _isCrouchToggled = false;

    public Controls(InputActionReference i, Movement m)
    {
        _moveAction = i;
        _movement = m;
    }

    public void ArtificialUpdate()
    {
        // Movimiento
        Vector2 input = _moveAction.action.ReadValue<Vector2>();
        _movement.Move(new Vector3(input.x, 0, input.y));
        _movement.UpdateAnimation(input.magnitude);

        // Agacharse
        if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
        {
            _isCrouchToggled = !_isCrouchToggled;
            _movement.Crouch(_isCrouchToggled);
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            TrySteal();
        }
    }

    private void TrySteal()
    {
        RaycastHit hit;
        Vector3 rayOrigin = _movement.transform.position + Vector3.up * 1.2f;

        if (Physics.Raycast(rayOrigin, _movement.transform.forward, out hit, 2f))
        {
            Citizen citizen = hit.collider.GetComponentInParent<Citizen>();

            if (citizen != null)
            {
                citizen.AttemptRob(_isCrouchToggled);
            }
        }
    }
}