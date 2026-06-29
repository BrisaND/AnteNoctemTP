using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using AnteNoctem.Core;

// ===== NAMESPACE =====
// Esta clase vive en su propio espacio "Enemies" para mantener todo organizado
namespace AnteNoctem.Enemies
{
    
    /// Clase base para todos los enemigos del juego (Warden, Perro, Ciudadano).
    /// La idea es que todos los enemigos comparten muchas cosas: patrullan, persiguen al jugador,
    /// se mueven con NavMeshAgent, cambian de comportamiento de noche, etc.
    /// En vez de copiar todo ese codigo en cada enemigo, lo pusimos aca una sola vez.
    /// Cada enemigo despues hereda de esta clase y agrega lo suyo.
    
    [RequireComponent(typeof(NavMeshAgent))]
    // ===== CLASE ABSTRACTA + HERENCIA =====
    // CLASE ABSTRACTA: no se puede crear un EnemyBase directamente, siempre hay que hacer una clase hija.
    // No tendria sentido poner un "EnemyBase" en la escena, porque no es un enemigo en si, es la base.
    // HERENCIA: hereda de MonoBehaviour para ser un componente de Unity.
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Patrullaje")]
        public List<Transform> patrolPoints = new List<Transform>();
        public float patrolSpeed = 2.5f;
        public float waitAtPoint = 1.5f;

        [Header("Modificadores de Noche")]
        [Tooltip("Multiplicador de velocidad de noche")]
        public float nightSpeedMultiplier = 1.4f;

        // ===== COMPOSICION =====
        // El enemigo NO es un NavMeshAgent, sino que TIENE uno adentro.
        // Eso es composicion: en vez de heredar, agregamos un componente como parte nuestra.
        // Lo mismo con las referencias al jugador.
        protected NavMeshAgent agent;
        protected Transform player;
        protected PlayerController playerCtrl;

        // ===== ENCAPSULAMIENTO =====
        // 'protected' significa que solo esta clase y sus clases hijas pueden tocar estas variables.
        // Asi protegemos las variables internas para que no las modifiquen desde afuera.
        protected int currentPatrolIndex = 0;
        protected float waitTimer = 0f;
        protected float basePatrolSpeed;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        protected virtual void Start()
        {
            // Buscamos al jugador en la escena y guardamos sus referencias
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerCtrl = p.GetComponent<PlayerController>();
            }

            basePatrolSpeed = patrolSpeed;

            if (patrolPoints.Count > 0) GoToNextPatrolPoint();
        }

        protected virtual void Update()
        {
            // Si el juego no esta corriendo (pausa o game over), no hacemos nada
            if (GameManager.Instance != null &&
                GameManager.Instance.gameState != GameManager.GameState.Playing) return;

            ApplyDayNightModifiers();
        }

        protected virtual void ApplyDayNightModifiers()
        {
            // A medida que oscurece, los enemigos se vuelven mas rapidos
            if (GameManager.Instance == null) return;
            float dayProgress = GameManager.Instance.GetDayProgress01();
            float darkness = 1f - dayProgress;
            patrolSpeed = Mathf.Lerp(basePatrolSpeed, basePatrolSpeed * nightSpeedMultiplier, darkness);
        }

        protected void Patrol()
        {
            agent.speed = patrolSpeed;
            if (patrolPoints.Count == 0) return;

            // Si llegamos al punto, esperamos unos segundos y vamos al siguiente
            if (HasReachedDestination())
            {
                waitTimer += Time.deltaTime;
                if (waitTimer >= waitAtPoint)
                {
                    waitTimer = 0f;
                    GoToNextPatrolPoint();
                }
            }
        }

        protected void GoToNextPatrolPoint()
        {
            if (patrolPoints.Count == 0) return;
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
        }

        protected bool HasReachedDestination()
        {
            if (agent.pathPending) return false;
            if (!agent.hasPath) return false;
            return agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance);
        }

        
        /// Funcion compartida que cualquier enemigo puede usar para encontrar al Warden mas cercano.
        /// Es 'static' porque no depende de un enemigo en particular, es una utilidad general.
        
        public static Transform FindNearestWardenTransform(Vector3 fromPosition)
        {
            var wardens = FindObjectsByType<WardenAI>(FindObjectsSortMode.None);
            Transform closest = null;
            float minDist = float.MaxValue;
            foreach (var w in wardens)
            {
                if (w == null) continue;
                float d = Vector3.Distance(w.transform.position, fromPosition);
                if (d < minDist) { minDist = d; closest = w.transform; }
            }
            return closest;
        }

        // ===== METODO ABSTRACTO =====
        // Este metodo NO tiene codigo aca. Cada enemigo que herede de EnemyBase esta OBLIGADO a implementarlo.
        // Es la forma de decir "todo enemigo debe tener un comportamiento principal, pero cada uno define el suyo".
        // Warden tiene su HandleBehavior (patrullar/perseguir/buscar), Perro tiene el suyo (patrullar/rastrear/morder), etc.
        protected abstract void HandleBehavior();
    }
}