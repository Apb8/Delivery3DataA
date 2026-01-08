using UnityEngine;
using System.Collections;
using System.Text;

public class GameMetricsSender : MonoBehaviour
{
    [Header("API Configuration")]
    public string apiURL = "http://citmalumnes.upc.es/~hugocc2/game_metrics_api.php"; //mirar si aixo esta be

    [Header("Player Info")]
    public string playerID = "anonymous";
    public string sessionID;

    void Start()
    {
        // Generar IDs unicos
        playerID = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(playerID))
        {
            playerID = "player_" + Random.Range(10000, 99999);
        }

        sessionID = "session_" + System.DateTime.Now.Ticks;

        // Iniciar sesion en la base de datos
        StartCoroutine(StartGameSession());
    }

    // ==================== METODOS PUBLICOS ====================

    public void RecordPlayerDeath(string deathCause, Vector3 position, string zoneName = "")
    {
        StartCoroutine(SendMetric("player_death", new
        {
            player_id = playerID,
            session_id = sessionID,
            death_cause = deathCause,
            position_x = position.x,
            position_y = position.y,
            position_z = position.z,
            zone_name = zoneName,
            level_name = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        }));
    }

    public void RecordTutorialDeath(string tutorialPhase, string deathCause, bool completed = false)
    {
        StartCoroutine(SendMetric("tutorial_death", new
        {
            player_id = playerID,
            tutorial_phase = tutorialPhase,
            death_cause = deathCause,
            completed = completed
        }));
    }

    public void RecordCubeDestroyed(string cubeType, Vector3 position, string zoneName = "")
    {
        StartCoroutine(SendMetric("cube_destroyed", new
        {
            cube_type = cubeType,
            position_x = position.x,
            position_y = position.y,
            position_z = position.z,
            zone_name = zoneName,
            session_id = sessionID
        }));
    }

    public void RecordEnemyKilled(string enemyType, float timeToKill, float damageDealt = 0)
    {
        StartCoroutine(SendMetric("enemy_killed", new
        {
            enemy_type = enemyType,
            time_to_kill = timeToKill,
            damage_dealt = damageDealt,
            player_id = playerID
        }));
    }

    public void RecordAcidLakeDeath(string lakeName, Vector3 position, string zoneName = "")
    {
        StartCoroutine(SendMetric("player_death", new
        {
            player_id = playerID,
            session_id = sessionID,
            death_cause = "acido",
            lake_name = lakeName,
            position_x = position.x,
            position_y = position.y,
            position_z = position.z,
            zone_name = zoneName
        }));
    }

    // ==================== METODO PRIVADO PRINCIPAL ====================

    private IEnumerator SendMetric(string metricType, object data)
    {
        // Crear objeto con tipo de metrica
        var metricData = new
        {
            metric_type = metricType,
            timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
        };

        // Combinar datos
        var combinedData = new System.Dynamic.ExpandoObject();
        var combinedDict = (System.Collections.Generic.IDictionary<string, object>)combinedData;

        // Añadir metric_type primero
        combinedDict["metric_type"] = metricType;

        // Añadir timestamp
        combinedDict["timestamp"] = System.DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

        // Añadir datos especificos usando reflexion
        foreach (var prop in data.GetType().GetProperties())
        {
            combinedDict[prop.Name] = prop.GetValue(data, null);
        }

        // Convertir a JSON
        string json = JsonUtility.ToJson(combinedData);

        // Enviar a la API
        byte[] postData = Encoding.UTF8.GetBytes(json);

        var headers = new System.Collections.Generic.Dictionary<string, string>();
        headers.Add("Content-Type", "application/json");

        WWW request = new WWW(apiURL, postData, headers); //actualitzar aixo!!
        yield return request;

        if (!string.IsNullOrEmpty(request.error))
        {
            Debug.LogWarning($"Error sending metric {metricType}: {request.error}");
        }
        else
        {
            Debug.Log($"Metric {metricType} sent successfully: {request.text}");
        }
    }

    private IEnumerator StartGameSession()
    {
        // Podriem afegir aqui un endpoint especific para iniciar sesiones
        Debug.Log($"Game session started: Player={playerID}, Session={sessionID}");
        yield return null;
    }

    void OnApplicationQuit()
    {
        // Registrar endsession (opcional)
        Debug.Log($"Game session ended: {sessionID}");
    }
}

// Clase auxiliar para seriaklizacion
public static class JsonHelper
{
    public static string ToJson(object obj)
    {
        return JsonUtility.ToJson(obj);
    }
}