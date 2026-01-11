using UnityEngine;
using Gamekit3D;

public class DestructibleCubeTracker : MonoBehaviour
{
    [Header("Cube Configuration")]
    public string cubeType = "wood";
    public string zoneName = "Unknown";

    [Header("Debug")]
    public bool showDebugInfo = false;

    private GameMetricsSender metrics;
    private Damageable cubeDamageable;
    private Transform parentPrefab;

    void Start()
    {
        metrics = FindFirstObjectByType<GameMetricsSender>();
        cubeDamageable = GetComponent<Damageable>();
        parentPrefab = transform.parent;

        if (cubeDamageable != null)
        {
            cubeDamageable.OnDeath.AddListener(OnCubeDestroyed);

            //if (showDebugInfo)
            //    Debug.Log($"Tracker added to {gameObject.name} in prefab {parentPrefab?.name}");
        }
        else
        {
            //Debug.LogError($"No Damageable component found on {gameObject.name}! " +
            //              "Make sure this script is on the GameObject with Damageable.");
                        
            cubeDamageable = GetComponentInChildren<Damageable>();
            if (cubeDamageable != null)
            {
                cubeDamageable.OnDeath.AddListener(OnCubeDestroyed);
                //Debug.Log($"Found Damageable in children, using that.");
            }
        }
    }

    void OnCubeDestroyed()
    {
        if (metrics != null)
        {            
            Vector3 recordPosition = parentPrefab != null ?
                parentPrefab.position : transform.position;

            metrics.RecordCubeDestroyed(cubeType, recordPosition, zoneName);

            if (showDebugInfo)
                Debug.Log($"Cube {cubeType} destroyed at {recordPosition}");
        }
    }
    
    //debugging
    void OnDrawGizmosSelected()
    {
        if (!showDebugInfo) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        if (parentPrefab != null && parentPrefab != transform)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, parentPrefab.position);
        }
    }
}