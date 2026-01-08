using UnityEngine;
using Gamekit3D;

public class EnemyDamageTracker : MonoBehaviour
{
    public string enemyType = "zombie";
    public string zoneName = "Unknown";

    private GameMetricsSender metrics;
    private Damageable enemyDamageable;
    private float spawnTime;

    void Start()
    {
        metrics = FindFirstObjectByType<GameMetricsSender>();
        enemyDamageable = GetComponent<Damageable>();
        spawnTime = Time.time;

        if (!gameObject.CompareTag("Enemy"))
            gameObject.tag = "Enemy";

        if (enemyDamageable != null)
            enemyDamageable.OnDeath.AddListener(OnEnemyDeath);
    }

    void OnEnemyDeath()
    {
        float timeAlive = Time.time - spawnTime;

        if (metrics != null)
        {
            float damageDealt = enemyDamageable != null ? enemyDamageable.maxHitPoints : 50f;
            metrics.RecordEnemyKilled(enemyType, timeAlive, damageDealt);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            DeathCauseTracker.lastDeathCause = enemyType;
    }
}