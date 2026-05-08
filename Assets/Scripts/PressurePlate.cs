using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Jugador")]
    public string playerTag = "Player";

    [Header("Umbrales y fuerzas")]
    public float forwardImpulse = 6f;
    public float upImpulse = 1.5f;
    [Tooltip("Si quieres aplicar también un empujón físico antes de forzar la rotación")]
    public bool applyPhysicalImpulse = true;

    [Tooltip("Segundos que tarda la rotación al caer (suavizado)")]
    public float rotationBlendTime = 0.15f;

    [Tooltip("Segundos que el jugador permanece tumbado")]
    public float knockDownDuration = 3f;

    [Header("Rearm")]
    public float rearmCooldown = 1f;

    // control interno
    private readonly Dictionary<Transform, float> lastTriggeredAt = new Dictionary<Transform, float>();
    private readonly HashSet<Transform> knockedPlayers = new HashSet<Transform>();

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    //Enter por si entra corriendo de golpe
    void OnTriggerEnter(Collider other)
    {
        EvaluarPlaca(other);
    }

    //Stay para vigilarlo en cada frame mientras esté encima
    void OnTriggerStay(Collider other)
    {
        EvaluarPlaca(other);
    }

    private void EvaluarPlaca(Collider other)
    {
        Transform t = other.transform;

        // Control de cooldown y estado
        if (lastTriggeredAt.TryGetValue(t, out float last) && Time.time - last < rearmCooldown) return;
        if (knockedPlayers.Contains(t)) return;

        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            if (pc.isCrouching || pc.currentState != PlayerController.MoveState.Running)
            {
                return;
            }

            StartCoroutine(HandleKnockDownForcingRotation(pc));
            lastTriggeredAt[t] = Time.time;
            return;
        }

        // Fallback para otros objetos (ciudadanos, enemigos que caigan)
        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            if (rb.linearVelocity.magnitude > 2f)
            {
                StartCoroutine(HandleKnockDownFallbackForcingRotation(rb, t));
                lastTriggeredAt[t] = Time.time;
            }
        }
    }

    private IEnumerator HandleKnockDownForcingRotation(PlayerController pc)
    {
        if (pc == null) yield break;
        pc.ResetMovementState();
        var t = pc.transform;
        if (knockedPlayers.Contains(t)) yield break;
        knockedPlayers.Add(t);

        // Deshabilitar controles
        pc.SetControlsEnabled(false);

        Animator anim = pc.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        Rigidbody rb = pc.GetComponent<Rigidbody>();
        bool hadRigidbody = rb != null;

        if (hadRigidbody && applyPhysicalImpulse)
        {
            Vector3 forward = (t.forward + Vector3.up * 0.1f).normalized;
            rb.AddForce(forward * forwardImpulse + Vector3.up * upImpulse, ForceMode.Impulse);
        }

        bool prevIsKinematic = false;
        RigidbodyConstraints prevConstraints = RigidbodyConstraints.None;
        if (hadRigidbody)
        {
            prevIsKinematic = rb.isKinematic;
            prevConstraints = rb.constraints;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.isKinematic = true;
        }

        // Forzar rotación a X = 90° (mantener Y actual)
        Quaternion start = t.rotation;
        float yaw = t.eulerAngles.y;
        Quaternion target = Quaternion.Euler(90f, yaw, 0f);

        // Suavizado de rotación
        float elapsed = 0f;
        while (elapsed < rotationBlendTime)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / rotationBlendTime);
            t.rotation = Quaternion.Slerp(start, target, p);
            yield return null;
        }

        // Mantener tumbado durante x tiempo
        float timer = 0f;
        while (timer < knockDownDuration)
        {
            // Se fuerza constantemente la rotación para evitar fallos
            t.rotation = target;

            timer += Time.deltaTime;
            yield return null;
        }

        // Restaurar física y controles
        if (hadRigidbody)
        {
            rb.isKinematic = prevIsKinematic;
            rb.constraints = prevConstraints;

            // Enderezar al levantarse
            Vector3 euler = t.eulerAngles;
            t.rotation = Quaternion.Euler(0f, euler.y, 0f);
        }

        if (anim != null) anim.enabled = true;

        pc.SetControlsEnabled(true);
        knockedPlayers.Remove(t);
    }

    private IEnumerator HandleKnockDownFallbackForcingRotation(Rigidbody rb, Transform t)
    {
        if (rb == null) yield break;
        if (knockedPlayers.Contains(t)) yield break;
        knockedPlayers.Add(t); // Registramos que está tumbado

        Animator anim = t.GetComponentInChildren<Animator>();
        if (anim != null) anim.enabled = false;

        if (applyPhysicalImpulse)
        {
            Vector3 forward = (t.forward + Vector3.up * 0.1f).normalized;
            rb.AddForce(forward * forwardImpulse + Vector3.up * upImpulse, ForceMode.Impulse);
        }

        bool prevIsKinematic = rb.isKinematic;
        RigidbodyConstraints prev = rb.constraints;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Elevamos ligeramente la cápsula para que no se hunda en el piso al acostarse
        t.position += new Vector3(0, 0.5f, 0);

        // Forzar rotación a X = 90° (mantener Y actual)
        Quaternion start = t.rotation;
        float yaw = t.eulerAngles.y;
        Quaternion target = Quaternion.Euler(90f, yaw, 0f);

        float elapsed = 0f;
        while (elapsed < rotationBlendTime)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / rotationBlendTime);
            t.rotation = Quaternion.Slerp(start, target, p);
            yield return null;
        }

        // Mantener tumbado durante x tiempo
        float timer = 0f;
        while (timer < knockDownDuration)
        {
            // Fuerza la rotación
            t.rotation = target;

            timer += Time.deltaTime;
            yield return null;
        }

        rb.isKinematic = prevIsKinematic;
        rb.constraints = prev;

        Vector3 euler = t.eulerAngles;
        t.rotation = Quaternion.Euler(0f, euler.y, 0f);

        if (anim != null) anim.enabled = true;

        knockedPlayers.Remove(t); // Liberamos al objeto para que pueda volver a tropezar
    }
}
