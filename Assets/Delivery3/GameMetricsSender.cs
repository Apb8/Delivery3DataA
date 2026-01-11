using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class GameMetricsSender : MonoBehaviour
{
    [Header("API Configuration")]
    // Updated to point to game_analytics.php as requested
    public string apiURL = "http://citmalumnes.upc.es/~hugocc2/game_analytics.php";

    [Header("Player Info")]
    public string playerID = "anonymous";
    public string sessionID;

    void Start()
    {
        // Force the correct URL in code to prevent Inspector overriding it with old values
        apiURL = "http://citmalumnes.upc.es/~hugocc2/game_analytics.php";

        // Generate unique IDs
        playerID = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(playerID))
        {
            if (PlayerPrefs.HasKey("PlayerID"))
            {
                playerID = PlayerPrefs.GetString("PlayerID");
            }
            else
            {
                playerID = "player_" + Random.Range(10000, 99999);
                PlayerPrefs.SetString("PlayerID", playerID);
            }
        }

        sessionID = "session_" + System.DateTime.Now.Ticks;
        Debug.Log($"[Metrics] Session Started: {sessionID}");
    }

    // ==================== PUBLIC METHODS ====================

    public void RecordPlayerDeath(string deathCause, Vector3 position, string zoneName = "")
    {
        StartCoroutine(PostDeath(deathCause, position, zoneName));
    }

    public void RecordTutorialDeath(string tutorialPhase, string deathCause, bool completed = false)
    {
        StartCoroutine(PostTutorial(tutorialPhase, deathCause, completed));
    }

    public void RecordCubeDestroyed(string cubeType, Vector3 position, string zoneName = "")
    {
        StartCoroutine(PostCube(cubeType, position, zoneName));
    }

    public void RecordEnemyKilled(string enemyType, float timeToKill, float damageDealt = 0)
    {
        StartCoroutine(PostEnemy(enemyType, timeToKill, damageDealt));
    }

    public void RecordAcidLakeDeath(string lakeName, Vector3 position, string zoneName = "")
    {
        // For acid lakes, we send a normal death but include the lake_name
        StartCoroutine(PostDeath("acido", position, zoneName, lakeName));
    }

    // ==================== COROUTINES (SENDING DATA) ====================

    IEnumerator PostDeath(string cause, Vector3 pos, string zone, string lake = "")
    {
        WWWForm form = new WWWForm();
        form.AddField("metric_type", "player_death"); // Required by PHP
        
        // Data fields
        form.AddField("player_id", playerID);
        form.AddField("death_cause", cause);
        form.AddField("position_x", pos.x.ToString().Replace(',', '.'));
        form.AddField("position_y", pos.y.ToString().Replace(',', '.'));
        form.AddField("position_z", pos.z.ToString().Replace(',', '.'));
        form.AddField("zone_name", zone);
        form.AddField("level_name", UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        
        if (!string.IsNullOrEmpty(lake))
        {
            form.AddField("lake_name", lake);
        }

        using (UnityWebRequest www = UnityWebRequest.Post(apiURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Metrics Error] Player Death: {www.error} - {www.downloadHandler.text}");
            }
            else
            {
                Debug.Log($"[Metrics Success] Player Death recorded: {www.downloadHandler.text}");
            }
        }
    }

    IEnumerator PostTutorial(string phase, string cause, bool completed)
    {
        WWWForm form = new WWWForm();
        form.AddField("metric_type", "tutorial_death");
        
        form.AddField("player_id", playerID);
        form.AddField("tutorial_phase", phase);
        form.AddField("death_cause", cause);
        form.AddField("completed", completed ? "1" : "0");

        using (UnityWebRequest www = UnityWebRequest.Post(apiURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Metrics Error] Tutorial: {www.error}");
            }
            else
            {
                Debug.Log($"[Metrics Success] Tutorial recorded: {www.downloadHandler.text}");
            }
        }
    }

    IEnumerator PostCube(string type, Vector3 pos, string zone)
    {
        WWWForm form = new WWWForm();
        form.AddField("metric_type", "cube_destroyed");
        
        form.AddField("cube_type", type);
        form.AddField("position_x", pos.x.ToString().Replace(',', '.'));
        form.AddField("position_y", pos.y.ToString().Replace(',', '.'));
        form.AddField("position_z", pos.z.ToString().Replace(',', '.'));
        form.AddField("zone_name", zone);

        using (UnityWebRequest www = UnityWebRequest.Post(apiURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Metrics Error] Cube: {www.error}");
            }
            else
            {
                Debug.Log($"[Metrics Success] Cube recorded: {www.downloadHandler.text}");
            }
        }
    }

    IEnumerator PostEnemy(string type, float time, float damage)
    {
        WWWForm form = new WWWForm();
        form.AddField("metric_type", "enemy_killed");
        
        form.AddField("enemy_type", type);
        form.AddField("time_to_kill", time.ToString().Replace(',', '.'));
        form.AddField("damage_dealt", damage.ToString().Replace(',', '.'));

        using (UnityWebRequest www = UnityWebRequest.Post(apiURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Metrics Error] Enemy: {www.error}");
            }
            else
            {
                Debug.Log($"[Metrics Success] Enemy recorded: {www.downloadHandler.text}");
            }
        }
    }
}