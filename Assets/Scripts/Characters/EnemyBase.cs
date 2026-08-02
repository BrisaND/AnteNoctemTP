//TPFinal - Pedro Valle

using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Collections;
using AnteNoctem.Core;

namespace AnteNoctem.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [Header("Patrullaje")]
        public List<Transform> patrolPoints = new List<Transform>();
        public float patrolSpeed = 2.5f;
        private float _waitAtPoint = 3f;

        [Header("Modificadores de Noche")]
        [Tooltip("Multiplicador de velocidad de noche")]
        public float nightSpeedMultiplier = 1.4f;

        protected NavMeshAgent agent;
        protected Transform player;
        protected PlayerController playerCtrl;

        protected int currentPatrolIndex = 0;
        protected bool isWaiting = false;
        protected float basePatrolSpeed;
        private Coroutine waitCoroutine;

        protected virtual void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        protected virtual void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                player = p.transform;
                playerCtrl = p.GetComponent<PlayerController>();
            }

            basePatrolSpeed = patrolSpeed;

            if (patrolPoints != null && patrolPoints.Count > 0)
            {
                GoToNextPatrolPoint();
            }
        }

        protected virtual void Update()
        {
            if (GameManager.Instance != null &&
                GameManager.Instance.gameState != GameManager.GameState.Playing) return;

            ApplyDayNightModifiers();
        }

        protected virtual void ApplyDayNightModifiers()
        {
            if (GameManager.Instance == null) return;
            float dayProgress = GameManager.Instance.GetDayProgress01();
            float darkness = 1f - dayProgress;
            patrolSpeed = Mathf.Lerp(basePatrolSpeed, basePatrolSpeed * nightSpeedMultiplier, darkness);
        }

        // --- LÓGICA DE PATRULLA CON ESPERA MEDIANTE CORRUTINA ---
        protected void Patrol()
        {
            if (patrolPoints == null || patrolPoints.Count == 0) return;
            if (isWaiting) return; // Si está esperando, no evalúa nada

            agent.speed = patrolSpeed;

            if (HasReachedDestination())
            {
                StopWaitCoroutine();
                waitCoroutine = StartCoroutine(WaitRoutine());
            }
        }

        private IEnumerator WaitRoutine()
        {
            isWaiting = true;
            agent.isStopped = true;

            yield return new WaitForSeconds(_waitAtPoint);

            GoToNextPatrolPoint();
        }

        protected void GoToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Count == 0) return;

            StopWaitCoroutine();

            if (agent != null && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }

            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
        }

        public void StopWaitCoroutine()
        {
            if (waitCoroutine != null)
            {
                StopCoroutine(waitCoroutine);
                waitCoroutine = null;
            }
            isWaiting = false;
        }

        protected bool HasReachedDestination()
        {
            if (agent == null || !agent.isOnNavMesh) return false;
            if (agent.pathPending) return false;
            if (!agent.hasPath) return false;
            return agent.remainingDistance <= Mathf.Max(0.5f, agent.stoppingDistance);
        }

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

        protected abstract void HandleBehavior();
    }
}