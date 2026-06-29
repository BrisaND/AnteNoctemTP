
using UnityEngine;

public class HidingSpot : MonoBehaviour
{
    [Header("Puntos de Referencia")]
    [Tooltip("Punto adentro del tacho (en el suelo)")]
    public Transform puntoEscondite;

    [Tooltip("Punto afuera y adelante del tacho")]
    public Transform puntoSalida;

    [Tooltip("Punto a la altura de los ojos/ranura apuntando HACIA AFUERA del tacho")]
    public Transform puntoCamaraInterna;

    [Header("Configuración de Tipo de Escondite")]
    [Tooltip("Si está marcado, el perro podrá meterse y sacarte a la fuerza si te vio entrar")]
    public bool isVulnerableToDog = false;

    private bool playerInRange = false;
    private PlayerController playerCtrl;

    void Awake()
    {
        if (puntoEscondite == null || puntoSalida == null || puntoCamaraInterna == null)
        {
            Debug.LogError($"HidingSpot en {gameObject.name} le faltan puntos de referencia críticos.");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerCtrl = other.GetComponent<PlayerController>();

            if (playerCtrl != null)
            {
                playerCtrl.currentHidingSpot = this;
            }
        }

    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            playerCtrl = null;

            if (playerCtrl != null && playerCtrl.currentHidingSpot == this)
            {
                playerCtrl.currentHidingSpot = null;
            }
        }
    }

    void Update()
    {
        if (playerInRange && playerCtrl != null)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!playerCtrl.isHidden)
                {
                    playerCtrl.EnterHide(puntoEscondite, puntoCamaraInterna);
                }
                else
                {
                    playerCtrl.ExitHide(puntoSalida);
                }
            }
        }
    }
}