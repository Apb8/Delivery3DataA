using UnityEngine;

public static class DeathCauseTracker
{
    public static string lastDeathCause = "";
    public static string lastAcidLake = "";
    public static Vector3 lastDeathPosition;
    public static string lastDeathZone = "";

    public static void Reset()
    {
        lastDeathCause = "";
        lastAcidLake = "";
        lastDeathPosition = Vector3.zero;
        lastDeathZone = "";
    }
}