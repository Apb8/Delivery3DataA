using UnityEngine;
using Gamekit3D;

public class AcidZoneTracker : MonoBehaviour
{
    public string lakeName = "AcidLake";
    public string zoneName = "Unknown";

    void Start()
    {
        // Asegurar tag
        if (!gameObject.CompareTag("AcidLake"))
            gameObject.tag = "AcidLake";

        // Asegurar que es trigger
        Collider collider = GetComponent<Collider>();
        if (collider != null && !collider.isTrigger)
            collider.isTrigger = true;

        Debug.Log($"Acid lake tracker: {lakeName} in {zoneName}");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DeathCauseTracker.lastDeathCause = "acido";
            DeathCauseTracker.lastAcidLake = lakeName;
            DeathCauseTracker.lastDeathZone = zoneName;
        }
    }
}