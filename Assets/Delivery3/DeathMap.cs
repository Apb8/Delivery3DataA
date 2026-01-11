using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

public class DeathMap : MonoBehaviour
{
    private string apiURL = "http://citmalumnes.upc.es/~hugocc2/game_analytics.php";
    
    [Header("Configuracion")]
    public KeyCode toggleKey = KeyCode.Alpha1;
    public Color markerColor = Color.red;
    public float markerSize = 0.5f;
    public float markerHeight = 1f;
    
    private GameObject markersContainer;
    private bool isVisible = false;
    private List<GameObject> currentMarkers = new List<GameObject>();
    
    [System.Serializable]
    private class DeathResponse
    {
        public bool success;
        public int total_points;
        public List<DeathPoint> death_points;
    }
    
    [System.Serializable]
    private class DeathPoint
    {
        public float grid_x;
        public float grid_z;
        public int death_count;
        public string causes;
        public string zone_name;
    }
    
    void Start()
    {
        markersContainer = new GameObject("DeathMarkersContainer");
        markersContainer.transform.SetParent(transform);
        markersContainer.SetActive(false);
        
        Debug.Log("DeathMap ready. Tecla '1' para mostrar/ocultar");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDeathMap();
        }
    }
    
    void ToggleDeathMap()
    {
        isVisible = !isVisible;
        
        if (isVisible)
        {
            ShowDeathMap();
        }
        else
        {
            HideDeathMap();
        }
    }
    
    void ShowDeathMap()
    {
        //Debug.Log("Cargando puntos de muerte");
        markersContainer.SetActive(true);
        StartCoroutine(LoadDeathPointsFromSQL());
    }
    
    void HideDeathMap()
    {
        //Debug.Log("Ocultando mapa");
        markersContainer.SetActive(false);
    }
    
    IEnumerator LoadDeathPointsFromSQL()
    {
        ClearMarkers();
        
        string url = apiURL + "?get_death_points=1&limit=100";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                                
                ParseJSONResponse(jsonResponse);
            }
            else
            {
                Debug.LogError("Error: " + request.error);
            }
        }
    }
    
    void ParseJSONResponse(string jsonData)
    {
        try
        {
            DeathResponse response = JsonUtility.FromJson<DeathResponse>(jsonData);
            
            if (response != null && response.success && response.death_points != null)
            {
                int markersCreated = 0;
                
                foreach (DeathPoint point in response.death_points)
                {
                    float x = point.grid_x;
                    float z = point.grid_z;
                    
                    CreateDeathMarker(x, z, point.death_count);
                    markersCreated++;
                }
                
                Debug.Log($"Creados {markersCreated} marcadores (coordenadas divididas por 10)");
            }
            else
            {
                //Debug.LogWarning("Respuesta JSON invalid");
                ParseManualJSON(jsonData);
            }
        }
        catch (Exception e)
        {
            //Debug.LogWarning("JsonUtility ha fallado: " + e.Message);
            ParseManualJSON(jsonData);
        }
    }
    
    void ParseManualJSON(string jsonData)
    {
        try
        {
            int startIndex = jsonData.IndexOf("\"death_points\":[") + "\"death_points\":[".Length;
            int endIndex = jsonData.IndexOf("]", startIndex);
            
            if (startIndex > 15 && endIndex > startIndex)
            {
                string pointsArray = jsonData.Substring(startIndex, endIndex - startIndex);
                string[] points = pointsArray.Split(new string[] {"},"}, StringSplitOptions.RemoveEmptyEntries);
                
                int markersCreated = 0;
                
                foreach (string pointStr in points)
                {
                    string cleanPoint = pointStr.Replace("{", "").Replace("}", "").Replace("\"", "").Trim();
                    
                    if (string.IsNullOrEmpty(cleanPoint)) continue;
                    
                    float x = 0;
                    float z = 0;
                    int count = 1;
                    
                    string[] fields = cleanPoint.Split(',');
                    foreach (string field in fields)
                    {
                        string[] keyValue = field.Split(':');
                        if (keyValue.Length >= 2)
                        {
                            string key = keyValue[0].Trim();
                            string value = keyValue[1].Trim();
                            
                            if (key == "grid_x")
                            {
                                if (float.TryParse(value, System.Globalization.NumberStyles.Any, 
                                    System.Globalization.CultureInfo.InvariantCulture, out float parsedX))
                                {
                                    x = parsedX / 10f;
                                }
                            }
                            else if (key == "grid_z")
                            {
                                if (float.TryParse(value, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out float parsedZ))
                                {
                                    z = parsedZ / 10f;
                                }
                            }
                            else if (key == "death_count")
                            {
                                int.TryParse(value, out count);
                            }
                        }
                    }
                    
                    if (x != 0 || z != 0)
                    {
                        CreateDeathMarker(x, z, count);
                        markersCreated++;
                    }
                }
                
                //Debug.Log($"Creados {markersCreated} marcadores (parseo manual)");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error en parseo manual: " + e.Message);
        }
    }
    
    void CreateDeathMarker(float x, float z, int deathCount = 1)
    {
        Vector3 position = new Vector3(x, markerHeight, z);
        
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = position;
        marker.transform.localScale = Vector3.one * markerSize;
        marker.transform.SetParent(markersContainer.transform);
        
        Renderer renderer = marker.GetComponent<Renderer>();
        renderer.material.color = markerColor;
        
        marker.name = $"DeathMarker_X{x:F1}_Z{z:F1}_Count{deathCount}";
        
        AddCountLabel(marker, deathCount);
        
        currentMarkers.Add(marker);
    }
    
    void AddCountLabel(GameObject marker, int count)
    {
        GameObject label = new GameObject("DeathCountLabel");
        label.transform.SetParent(marker.transform);
        label.transform.localPosition = new Vector3(0, 1.2f, 0);
        
        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = $"×{count}";
        textMesh.fontSize = 20;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;
        
        label.AddComponent<SimpleBillboard>();
    }
    
    void ClearMarkers()
    {
        foreach (GameObject marker in currentMarkers)
        {
            if (marker != null)
            {
                Destroy(marker);
            }
        }
        currentMarkers.Clear();
    }
    
    void OnDestroy()
    {
        ClearMarkers();
        if (markersContainer != null)
        {
            Destroy(markersContainer);
        }
    }
}

public class SimpleBillboard : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.forward);
        }
    }
}