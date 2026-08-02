//TPFinal - Juan Cruz Villarreo

using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public class CitizenSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab del Citizen que se va a spawnear")]
    public GameObject citizenPrefab;

    [Header("Zona de spawn")]
    [Tooltip("Centro del rectangulo de spawn")]
    public Transform spawnZoneCenter;
    [Tooltip("Tamaño del area de spawn (X = ancho, Z = profundidad)")]
    public Vector2 spawnZoneSize = new Vector2(4f, 2f);

    [Header("Zona de despawn (destino)")]
    [Tooltip("Centro del rectangulo donde llegan y desaparecen")]
    public Transform endZoneCenter;
    [Tooltip("Tamaño del area de despawn")]
    public Vector2 endZoneSize = new Vector2(4f, 2f);

    [Header("Ritmo de spawn (segundos)")]
    public float minSpawnInterval = 2f;
    public float maxSpawnInterval = 5f;

    [Header("Limites")]
    [Tooltip("Cantidad maxima de ciudadanos vivos al mismo tiempo de este spawner")]
    public int maxAlive = 8;

    [Header("Debug")]
    public bool showGizmo = true;

    private List<GameObject> aliveCitizens = new List<GameObject>();

    void Start()
    {
        StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            float wait = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(wait);

            aliveCitizens.RemoveAll(c => c == null);
            if (aliveCitizens.Count >= maxAlive) continue;

            SpawnCitizen();
        }
    }

    void SpawnCitizen()
    {
        if (citizenPrefab == null || spawnZoneCenter == null || endZoneCenter == null) return;

        // Posicion aleatoria dentro de la zona de spawn
        Vector3 spawnPos = GetRandomPointInZone(spawnZoneCenter, spawnZoneSize);

        // Lo "snappeamos" al NavMesh por las dudas (encuentra el punto valido mas cercano)
        if (NavMesh.SamplePosition(spawnPos, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            spawnPos = hit.position;
        }

        // Posicion aleatoria dentro de la zona de despawn (cada ciudadano tiene su propio destino)
        Vector3 endPos = GetRandomPointInZone(endZoneCenter, endZoneSize);

        // Instanciamos
        GameObject newCitizen = Instantiate(citizenPrefab, spawnPos, spawnZoneCenter.rotation);
        aliveCitizens.Add(newCitizen);

        // Creamos un GameObject temporal en endPos para usarlo como patrol point
        GameObject endTarget = new GameObject("EndTarget_" + newCitizen.name);
        endTarget.transform.position = endPos;
        endTarget.transform.parent = newCitizen.transform;

        // Le pasamos el patrol point al CitizenAI
        CitizenAI ai = newCitizen.GetComponent<CitizenAI>();
        if (ai != null)
        {
            ai.patrolPoints = new List<Transform> { endTarget.transform };
        }

        // Agregamos el componente que lo destruye al entrar a la endZone
        CitizenDestroyOnReach destroyer = newCitizen.AddComponent<CitizenDestroyOnReach>();
        destroyer.endZoneCenter = endZoneCenter;
        destroyer.endZoneSize = endZoneSize;
    }

    Vector3 GetRandomPointInZone(Transform center, Vector2 size)
    {
        float x = Random.Range(-size.x / 2f, size.x / 2f);
        float z = Random.Range(-size.y / 2f, size.y / 2f);
        Vector3 localOffset = new Vector3(x, 0, z);
        // Aplicamos la rotacion del centro para que el rectangulo siga la rotacion del transform
        return center.position + center.rotation * localOffset;
    }

    void OnDrawGizmos()
    {
        if (!showGizmo) return;

        if (spawnZoneCenter != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            DrawZone(spawnZoneCenter, spawnZoneSize);
        }
        if (endZoneCenter != null)
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            DrawZone(endZoneCenter, endZoneSize);
        }
        if (spawnZoneCenter != null && endZoneCenter != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnZoneCenter.position, endZoneCenter.position);
        }
    }

    void DrawZone(Transform center, Vector2 size)
    {
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center.position, center.rotation, Vector3.one);
        Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, 0.1f, size.y));
        Gizmos.matrix = oldMatrix;
    }
}

/// <summary>
/// Componente auxiliar: destruye al ciudadano cuando entra a la zona de despawn,
/// usando un area rectangular en vez de una distancia a un punto.
/// </summary>
public class CitizenDestroyOnReach : MonoBehaviour
{
    public Transform endZoneCenter;
    public Vector2 endZoneSize;

    void Update()
    {
        if (endZoneCenter == null) return;

        // Calculamos la posicion local del ciudadano respecto al centro de la zona
        Vector3 localPos = endZoneCenter.InverseTransformPoint(transform.position);

        // Si esta dentro del rectangulo (en X y Z), lo destruimos
        if (Mathf.Abs(localPos.x) <= endZoneSize.x / 2f &&
            Mathf.Abs(localPos.z) <= endZoneSize.y / 2f)
        {
            Destroy(gameObject);
        }
    }
}