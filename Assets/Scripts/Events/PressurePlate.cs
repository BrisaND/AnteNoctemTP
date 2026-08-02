//TPFinal - Joaquin Campana

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PressurePlate : MonoBehaviour
{
    [Header("Jugador")]
    public string playerTag = "Player";

    private float _slowMultiplier = 0.3f;

    private readonly Dictionary<Transform, float> originalMultipliers = new Dictionary<Transform, float>();

    void Reset()
    {
        var c = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            // Si no entra corriendo, la placa no hace nada
            if (pc.currentState != PlayerController.MoveState.Running)
            {
                return;
            }

            Transform t = pc.transform;

            //evitamos pisar el dato original
            if (originalMultipliers.ContainsKey(t)) return;

            //multiplicador actual
            originalMultipliers[t] = pc.speedMultiplier;

            //freno modificando el float del PlayerController
            pc.speedMultiplier = _slowMultiplier;
        }
    }

    void OnTriggerExit(Collider other)
    {
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            Transform t = pc.transform;

            //verificamos si esta placa lo había ralentizado
            if (originalMultipliers.TryGetValue(t, out float originalMultiplier))
            {
                //devuelta su velocidad 
                pc.speedMultiplier = originalMultiplier;

                //limpiar la memoria de la placa
                originalMultipliers.Remove(t);
            }
        }
    }
}