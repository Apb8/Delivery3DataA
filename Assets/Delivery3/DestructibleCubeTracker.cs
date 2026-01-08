using UnityEngine;
using Gamekit3D;

public class DestructibleCubeTracker : MonoBehaviour
{
    public string cubeType = "wood";
    public string zoneName = "Unknown";

    private GameMetricsSender metrics;
    private Damageable cubeDamageable;

    void Start()
    {
        metrics = FindFirstObjectByType<GameMetricsSender>();
        cubeDamageable = GetComponent<Damageable>();

        if (!gameObject.CompareTag("Destructible"))
            gameObject.tag = "Destructible";

        if (cubeDamageable != null)
            cubeDamageable.OnDeath.AddListener(OnCubeDestroyed);
        else
            Debug.LogWarning($"Cube {gameObject.name} has no Damageable component!");
    }

    void OnCubeDestroyed()
    {
        if (metrics != null)
            metrics.RecordCubeDestroyed(cubeType, transform.position, zoneName);
    }
}