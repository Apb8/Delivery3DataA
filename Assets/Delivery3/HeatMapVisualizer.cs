using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

public class HeatmapVisualizer : MonoBehaviour
{
    private string apiURL = "http://citmalumnes.upc.es/~hugocc2/game_analytics.php";

    [Header("Heatmap Visuals")]
    public float baseHeight = 1f; // <--- NUEVO: Altura base (el "suelo" del heatmap)
    public float maxHeight = 10f; // Altura extra de los picos
    public bool use3DHeight = true; 
    
    [Range(0f, 1f)]
    public float globalOpacity = 0.6f; 
    
    public Gradient heatmapGradient; 

    [Header("Grid Settings")]
    public int gridPrecision = 5; 

    [Header("Auto Settings")]
    public bool autoLoadOnStart = true;
    public float reloadInterval = 30f;

    private GameObject heatmapContainer;

    [System.Serializable]
    private class DeathResponse
    {
        public bool success;
        public List<DeathPoint> death_points;
    }

    [System.Serializable]
    private class DeathPoint
    {
        public float grid_x;
        public float grid_z;
        public int death_count;
    }

    void Start()
    {
        if (heatmapGradient == null || heatmapGradient.colorKeys.Length == 0) ConfigureDefaultGradient();
        
        if (autoLoadOnStart) LoadHeatmapData();
        if (reloadInterval > 0) InvokeRepeating("LoadHeatmapData", reloadInterval, reloadInterval);
    }

    void ConfigureDefaultGradient()
    {
        heatmapGradient = new Gradient();
        var colors = new GradientColorKey[] {
            new GradientColorKey(Color.blue, 0.0f),
            new GradientColorKey(Color.green, 0.5f),
            new GradientColorKey(Color.red, 1.0f)
        };
        var alphas = new GradientAlphaKey[] {
            new GradientAlphaKey(0.0f, 0.0f), 
            new GradientAlphaKey(0.8f, 0.2f),
            new GradientAlphaKey(0.9f, 1.0f)
        };
        heatmapGradient.SetKeys(colors, alphas);
    }

    public void LoadHeatmapData()
    {
        StartCoroutine(LoadDeathPointsFromSQL());
    }

    IEnumerator LoadDeathPointsFromSQL()
    {
        string url = apiURL + "?get_death_points=1&limit=1000"; 
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                ParseAndCreateHeatmap(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error descarga Heatmap: " + request.error);
                CreateTestHeatmap();
            }
        }
    }

    void ParseAndCreateHeatmap(string jsonData)
    {
        jsonData = jsonData.Replace("\uFEFF", "").Trim();
        try
        {
            DeathResponse response = JsonUtility.FromJson<DeathResponse>(jsonData);
            if (response != null && response.death_points != null)
            {
                ProcessData(response.death_points);
            }
            else
            {
                ParseManualJSON(jsonData);
            }
        }
        catch { ParseManualJSON(jsonData); }
    }

    void ParseManualJSON(string jsonData)
    {
        List<DeathPoint> points = new List<DeathPoint>();
        try {
            int start = jsonData.IndexOf("[");
            int end = jsonData.LastIndexOf("]");
            if(start >= 0 && end > start) {
                string arrayContent = jsonData.Substring(start + 1, end - start - 1);
                string[] items = arrayContent.Split(new string[] { "}," }, StringSplitOptions.RemoveEmptyEntries);
                foreach(var item in items) {
                    DeathPoint dp = new DeathPoint();
                    string[] parts = item.Split(',');
                    foreach(var part in parts) {
                        if(part.Contains("grid_x")) dp.grid_x = float.Parse(part.Split(':')[1].Replace("\"", ""));
                        if(part.Contains("grid_z")) dp.grid_z = float.Parse(part.Split(':')[1].Replace("\"", ""));
                        if(part.Contains("death_count")) dp.death_count = int.Parse(part.Split(':')[1].Replace("\"", "").Replace("}", ""));
                    }
                    if(dp.death_count > 0) points.Add(dp);
                }
            }
        } catch {}
        
        if (points.Count > 0) ProcessData(points);
        else CreateTestHeatmap();
    }

    void ProcessData(List<DeathPoint> deathPoints)
    {
        if (heatmapContainer != null) Destroy(heatmapContainer);
        heatmapContainer = new GameObject("HeatmapContainer");
        heatmapContainer.transform.SetParent(transform, false);

        Dictionary<Vector2Int, int> gridCounts = new Dictionary<Vector2Int, int>();
        int maxDeaths = 0;

        foreach (DeathPoint p in deathPoints)
        {
            int x = Mathf.RoundToInt(p.grid_x / gridPrecision) * gridPrecision;
            int z = Mathf.RoundToInt(p.grid_z / gridPrecision) * gridPrecision;
            Vector2Int pos = new Vector2Int(x, z);

            if (!gridCounts.ContainsKey(pos)) gridCounts[pos] = 0;
            gridCounts[pos] += p.death_count;

            if (gridCounts[pos] > maxDeaths) maxDeaths = gridCounts[pos];
        }

        GenerateContinuousMesh(gridCounts, maxDeaths);
        CreateLegend(maxDeaths);
    }

    void GenerateContinuousMesh(Dictionary<Vector2Int, int> data, int maxVal)
    {
        if (data.Count == 0) return;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (var k in data.Keys)
        {
            if (k.x < minX) minX = k.x;
            if (k.x > maxX) maxX = k.x;
            if (k.y < minZ) minZ = k.y;
            if (k.y > maxZ) maxZ = k.y;
        }

        minX -= gridPrecision; maxX += gridPrecision;
        minZ -= gridPrecision; maxZ += gridPrecision;

        int widthSegments = (maxX - minX) / gridPrecision + 1;
        int depthSegments = (maxZ - minZ) / gridPrecision + 1;

        Vector3[] vertices = new Vector3[widthSegments * depthSegments];
        Color[] colors = new Color[vertices.Length];
        int[] triangles = new int[(widthSegments - 1) * (depthSegments - 1) * 6];

        for (int z = 0; z < depthSegments; z++)
        {
            for (int x = 0; x < widthSegments; x++)
            {
                int index = z * widthSegments + x;
                
                float realX = minX + (x * gridPrecision);
                float realZ = minZ + (z * gridPrecision);
                Vector2Int key = new Vector2Int((int)realX, (int)realZ);

                int count = data.ContainsKey(key) ? data[key] : 0;
                float normalized = maxVal > 0 ? (float)count / maxVal : 0;

                // Color y Opacidad
                Color c = heatmapGradient.Evaluate(normalized);
                c.a *= globalOpacity;
                colors[index] = c;

                // --- CÁLCULO DE ALTURA CORREGIDO A Y=1 ---
                // Si es 3D: baseHeight + altura calculada
                // Si es Plano: baseHeight fijo
                float yOffset = use3DHeight ? (normalized * maxHeight) : 0f;
                float finalY = baseHeight + yOffset;
                
                vertices[index] = new Vector3(realX, finalY, realZ);
            }
        }

        int t = 0;
        for (int z = 0; z < depthSegments - 1; z++)
        {
            for (int x = 0; x < widthSegments - 1; x++)
            {
                int bottomLeft = z * widthSegments + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = (z + 1) * widthSegments + x;
                int topRight = topLeft + 1;

                triangles[t++] = bottomLeft;
                triangles[t++] = topLeft;
                triangles[t++] = bottomRight;

                triangles[t++] = bottomRight;
                triangles[t++] = topLeft;
                triangles[t++] = topRight;
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; 
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GameObject surface = new GameObject("HeatmapSurface");
        surface.transform.SetParent(heatmapContainer.transform, false);
        
        surface.AddComponent<MeshFilter>().mesh = mesh;
        MeshRenderer mr = surface.AddComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Sprites/Default"));
    }

    void CreateLegend(int maxDeaths)
    {
        GameObject legend = new GameObject("LegendInfo");
        legend.transform.SetParent(heatmapContainer.transform);
        // Ponemos la leyenda un poco por encima del pico más alto
        legend.transform.localPosition = new Vector3(0, baseHeight + maxHeight + 2, 0);
        
        TextMesh tm = legend.AddComponent<TextMesh>();
        tm.text = $"Pico máx: {maxDeaths} muertes";
        tm.fontSize = 24;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;
        
        legend.AddComponent<LookAtCameraHM>(); 
    }

    void CreateTestHeatmap()
    {
        Debug.Log("Generando datos de prueba...");
        List<DeathPoint> points = new List<DeathPoint>();
        for (int i = 0; i < 300; i++) {
            points.Add(new DeathPoint { 
                grid_x = UnityEngine.Random.Range(-30, 30), 
                grid_z = UnityEngine.Random.Range(-30, 30), 
                death_count = UnityEngine.Random.Range(1, 15) 
            });
        }
        ProcessData(points);
    }

    void OnDestroy()
    {
        if (heatmapContainer != null) Destroy(heatmapContainer);
    }
}

public class LookAtCameraHM : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null) 
            transform.LookAt(transform.position + Camera.main.transform.forward);
    }
}