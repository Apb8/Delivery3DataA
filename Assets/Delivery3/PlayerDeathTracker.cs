using UnityEngine;
using Gamekit3D;

public class PlayerDeathTracker : MonoBehaviour
{
    private GameMetricsSender metrics;
    private Damageable damageable;

    void Start()
    {
        metrics = FindFirstObjectByType<GameMetricsSender>();
        damageable = GetComponent<Damageable>();

        if (damageable != null)
            damageable.OnDeath.AddListener(OnPlayerDeath);
    }

    void OnPlayerDeath()
    {
        string deathCause = DeathCauseTracker.lastDeathCause;
        if (string.IsNullOrEmpty(deathCause)) deathCause = "unknown";

        string zone = GetCurrentZone();

        if (metrics != null)
        {
            metrics.RecordPlayerDeath(deathCause, transform.position, zone);
            Debug.Log($"Death: {deathCause} at {zone}");
        }

        // Si muere en lago acido, registrar específ.
        if (deathCause == "acido" && !string.IsNullOrEmpty(DeathCauseTracker.lastAcidLake))
        {
            metrics.RecordAcidLakeDeath(DeathCauseTracker.lastAcidLake, transform.position, zone);
        }

        DeathCauseTracker.Reset();
    }

    string GetCurrentZone()
    {
        // Simple: divide el mapa en cuadrantes
        float x = transform.position.x;
        float z = transform.position.z;

        if (x < 0 && z < 0) return "SW";
        if (x < 0 && z >= 0) return "NW";
        if (x >= 0 && z < 0) return "SE";
        return "NE";
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AcidLake"))
        {
            DeathCauseTracker.lastDeathCause = "acido";
            DeathCauseTracker.lastAcidLake = other.gameObject.name;
        }
        else if (other.CompareTag("Enemy"))
        {
            DeathCauseTracker.lastDeathCause = "enemigo";
        }
    }
}