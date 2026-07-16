
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

    [Header("Seguridad (Anti-Bugs)")]
    [Tooltip("Distancia máxima en metros a la que puedes estar antes de que el tacho te desconecte a la fuerza (por si falla la física de Unity).")]
    public float distanciaSeguridadMax = 3.5f;

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
            ForceExitRange();
        }
    }

    void Update()
    {
        if (playerInRange && playerCtrl != null)
        {
            // Si el jugador ya no está escondido y se alejó físicamente, pero el Trigger de Unity no se enteró, se desconecta a la fuerza.
            float distanciaReal = Vector3.Distance(transform.position, playerCtrl.transform.position);
            if (distanciaReal > distanciaSeguridadMax && !playerCtrl.isHidden)
            {
                ForceExitRange();
                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                if (!playerCtrl.isHidden)
                {
                    playerCtrl.EnterHide(puntoEscondite, puntoCamaraInterna);
                }
                else
                {
                    playerCtrl.ExitHide(puntoSalida);

                    // Esto evita que el bucle de input te vuelva a meter al presionar E.
                    ForceExitRange();
                }
            }
        }
    }

    public void ForceExitRange()
    {
        playerInRange = false;

        if (playerCtrl != null)
        {
            if (playerCtrl.currentHidingSpot == this)
            {
                playerCtrl.currentHidingSpot = null;
            }
            playerCtrl = null;
        }
    }
}