using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.EventSystems.EventTrigger;

public class Citizen : Human
{
    [Header("Visuales")]
    public Renderer meshRenderer;
    public Material shadowMaterial;
    public Material realMaterial;

    [Header("IA")]
    public List<Transform> waypoints;
    public SkillCheck skillCheck;
    private NavMeshAgent _agent;
    private Animator _animator;
    private bool _isRobbed = false;

    private Player _player;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponent<Animator>();
        meshRenderer.material = shadowMaterial;
        StartCoroutine(Patrol());
    }

    IEnumerator Patrol()
    {
        int i = 0;
        while (!_isRobbed)
        {
            _agent.SetDestination(waypoints[i].position);
            _animator.SetFloat("Speed", 1f);

            while (_agent.remainingDistance > 0.5f) yield return null;

            _agent.isStopped = true;
            _animator.SetFloat("Speed", 0f);

            if (Random.value > 0.5f)
            {
                _animator.SetTrigger("LookAround");
                yield return new WaitForSeconds(3f);
            }
            else
            {
                yield return new WaitForSeconds(2f);
            }

            _agent.isStopped = false;
            i = (i + 1) % waypoints.Count;
        }
    }

    public void AttemptRob(bool isPlayerCrouching)
    {
        if (_agent.isStopped && !_isRobbed)
        {
            if (isPlayerCrouching)
                skillCheck.StartSkillCheck(this);
            else
                Debug.Log("¡Te vio! No estabas agachado.");
        }
    }

    public void OnStealResult(bool success)
    {
        if (success)
        {
            _isRobbed = true;
            meshRenderer.material = realMaterial;
        }
        else
        {
            _player.TakeDamage(10);
            Debug.Log($"Daño realizado, queda {_player.health}");
        }
    }
}