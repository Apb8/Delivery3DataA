using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

public class HeatmapVisualizer : MonoBehaviour
{
    private string apiURL = "http://citmalumnes.upc.es/~hugocc2/game_analytics.php";

    [Header("Heatmap Configuration")]
    public float tileSize = 2f; // Tamaño de cada tile del heatmap
    public float maxHeight = 10f; // Altura máxima de las columnas
    public bool use3DColumns = true; // True: columnas 3D, False: quads planos

    [Header("Color Gradient")]
    public Gradient heatmapGradient; // Configurar en Inspector

    [Header("Grid Settings")]
    public int gridPrecision = 5; // Agrupa cada 5 unidades
    public int maxTiles = 200; // Límite de tiles a crear

    [Header("Auto Settings")]
    public bool autoLoadOnStart = true;
    public float reloadInterval = 30f; // Segundos entre recargas (0 = no recargar)

    private GameObject heatmapContainer;
    private Dictionary<Vector2Int, HeatmapTile> heatmapTiles = new Dictionary<Vector2Int, HeatmapTile>();

    // Usamos las mismas clases que DeathMap
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
        // Crear contenedor
        heatmapContainer = new GameObject("HeatmapContainer");
        heatmapContainer.transform.SetParent(transform);

        // Configurar gradiente por defecto si no hay uno
        if (heatmapGradient == null || heatmapGradient.colorKeys.Length == 0)
        {
            ConfigureDefaultGradient();
        }

        // Cargar automáticamente si está configurado
        if (autoLoadOnStart)
        {
            LoadHeatmapData();
        }

        // Configurar recarga automática si está configurada
        if (reloadInterval > 0)
        {
            InvokeRepeating("LoadHeatmapData", reloadInterval, reloadInterval);
        }

        Debug.Log("Heatmap Visualizer listo");
    }

    void ConfigureDefaultGradient()
    {
        heatmapGradient = new Gradient();

        // Configurar colores del gradiente
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        colorKeys[0] = new GradientColorKey(new Color(0, 1, 0, 0.7f), 0.0f); // Verde (bajo)
        colorKeys[1] = new GradientColorKey(new Color(1, 1, 0, 0.8f), 0.25f); // Amarillo
        colorKeys[2] = new GradientColorKey(new Color(1, 0.5f, 0, 0.9f), 0.5f); // Naranja
        colorKeys[3] = new GradientColorKey(new Color(1, 0, 0, 0.9f), 0.75f); // Rojo
        colorKeys[4] = new GradientColorKey(new Color(0.5f, 0, 0, 1f), 1.0f); // Rojo oscuro (alto)

        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[3];
        alphaKeys[0] = new GradientAlphaKey(0.7f, 0f);
        alphaKeys[1] = new GradientAlphaKey(0.8f, 0.5f);
        alphaKeys[2] = new GradientAlphaKey(0.9f, 1f);

        heatmapGradient.SetKeys(colorKeys, alphaKeys);
    }

    public void LoadHeatmapData()
    {
        StartCoroutine(LoadDeathPointsFromSQL());
    }

    IEnumerator LoadDeathPointsFromSQL()
    {
        ClearHeatmap();

        string url = apiURL + "?get_death_points=1&limit=300";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("Heatmap: Datos recibidos");

                ParseAndCreateHeatmap(jsonResponse);
            }
            else
            {
                Debug.LogError("Heatmap Error: " + request.error);
                CreateTestHeatmap();
            }
        }
    }

    void ParseAndCreateHeatmap(string jsonData)
    {
        try
        {
            DeathResponse response = JsonUtility.FromJson<DeathResponse>(jsonData);

            if (response != null && response.success && response.death_points != null)
            {
                CreateHeatmapFromData(response.death_points);
            }
            else
            {
                Debug.LogWarning("Heatmap: Respuesta JSON inválida, usando parseo manual");
                ParseManualJSONAndCreateHeatmap(jsonData);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("Heatmap JsonUtility falló: " + e.Message);
            ParseManualJSONAndCreateHeatmap(jsonData);
        }
    }

    void ParseManualJSONAndCreateHeatmap(string jsonData)
    {
        try
        {
            List<DeathPoint> deathPoints = new List<DeathPoint>();

            int startIndex = jsonData.IndexOf("\"death_points\":[") + "\"death_points\":[".Length;
            int endIndex = jsonData.IndexOf("]", startIndex);

            if (startIndex > 15 && endIndex > startIndex)
            {
                string pointsArray = jsonData.Substring(startIndex, endIndex - startIndex);
                string[] points = pointsArray.Split(new string[] { "}," }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string pointStr in points)
                {
                    string cleanPoint = pointStr.Replace("{", "").Replace("}", "").Replace("\"", "").Trim();

                    if (string.IsNullOrEmpty(cleanPoint)) continue;

                    DeathPoint point = new DeathPoint();

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
                                    point.grid_x = parsedX;
                                }
                            }
                            else if (key == "grid_z")
                            {
                                if (float.TryParse(value, System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture, out float parsedZ))
                                {
                                    point.grid_z = parsedZ;
                                }
                            }
                            else if (key == "death_count")
                            {
                                int.TryParse(value, out point.death_count);
                            }
                            else if (key == "causes")
                            {
                                point.causes = value;
                            }
                            else if (key == "zone_name")
                            {
                                point.zone_name = value;
                            }
                        }
                    }

                    if (point.death_count > 0)
                    {
                        deathPoints.Add(point);
                    }
                }

                if (deathPoints.Count > 0)
                {
                    CreateHeatmapFromData(deathPoints);
                }
                else
                {
                    CreateTestHeatmap();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Heatmap Error en parseo manual: " + e.Message);
            CreateTestHeatmap();
        }
    }

    void CreateHeatmapFromData(List<DeathPoint> deathPoints)
    {
        Debug.Log($"Heatmap: Procesando {deathPoints.Count} puntos de muerte");

        // 1. Agrupar muertes por posición de grid
        Dictionary<Vector2Int, int> gridCounts = new Dictionary<Vector2Int, int>();
        int maxDeathsInCell = 0;

        foreach (DeathPoint point in deathPoints)
        {
            // Convertir coordenadas continuas a grid discreto
            int gridX = Mathf.RoundToInt(point.grid_x / gridPrecision) * gridPrecision;
            int gridZ = Mathf.RoundToInt(point.grid_z / gridPrecision) * gridPrecision;
            Vector2Int gridPos = new Vector2Int(gridX, gridZ);

            if (!gridCounts.ContainsKey(gridPos))
            {
                gridCounts[gridPos] = 0;
            }

            gridCounts[gridPos] += point.death_count;

            if (gridCounts[gridPos] > maxDeathsInCell)
            {
                maxDeathsInCell = gridCounts[gridPos];
            }
        }

        Debug.Log($"Heatmap: Máximo de muertes en una celda: {maxDeathsInCell}");
        Debug.Log($"Heatmap: Celdas únicas con muertes: {gridCounts.Count}");

        // 2. Crear tiles del heatmap
        int tilesCreated = 0;
        foreach (var kvp in gridCounts)
        {
            if (tilesCreated >= maxTiles) break;

            CreateHeatmapTile(kvp.Key, kvp.Value, maxDeathsInCell);
            tilesCreated++;
        }

        Debug.Log($"Heatmap: Creados {tilesCreated} tiles");

        // 3. Opcional: Crear leyenda
        if (tilesCreated > 0)
        {
            CreateLegend(maxDeathsInCell);
        }
    }

    void CreateHeatmapTile(Vector2Int gridPos, int deathCount, int maxDeaths)
    {
        // Posición en el mundo (NOTA: No dividimos por 10 como en DeathMap)
        Vector3 position = new Vector3(gridPos.x, 0, gridPos.y);

        GameObject tile;

        if (use3DColumns)
        {
            // Crear columna 3D
            tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.transform.position = position;

            // Calcular altura basada en densidad
            float normalizedValue = maxDeaths > 0 ? Mathf.Clamp01((float)deathCount / maxDeaths) : 0;
            float height = 0.1f + (normalizedValue * maxHeight);

            tile.transform.localScale = new Vector3(tileSize, height, tileSize);
            // Posicionar para que la base esté en el suelo
            tile.transform.position = new Vector3(position.x, height / 2, position.z);
        }
        else
        {
            // Crear quad plano
            tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.transform.position = position;
            tile.transform.localScale = new Vector3(tileSize, tileSize, 1);
            // Rotar para que mire hacia arriba
            tile.transform.Rotate(90, 0, 0);
        }

        tile.transform.SetParent(heatmapContainer.transform);

        // Aplicar color del gradiente
        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null)
        {
            float normalizedValue = maxDeaths > 0 ? Mathf.Clamp01((float)deathCount / maxDeaths) : 0;
            Color tileColor = heatmapGradient.Evaluate(normalizedValue);

            // Crear material transparente
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = tileColor;
            mat.SetFloat("_Mode", 2); // Fade mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            renderer.material = mat;
        }

        // Añadir etiqueta con contador
        AddCountLabel(tile, deathCount);

        // Guardar referencia
        HeatmapTile heatmapTile = new HeatmapTile
        {
            tileObject = tile,
            gridPosition = gridPos,
            deathCount = deathCount
        };

        heatmapTiles[gridPos] = heatmapTile;

        tile.name = $"HeatTile_{gridPos.x}_{gridPos.y}_Deaths{deathCount}";
    }

    void AddCountLabel(GameObject tile, int count)
    {
        GameObject label = new GameObject("HeatmapCountLabel");
        label.transform.SetParent(tile.transform);

        // Posicionar arriba del tile
        float yOffset = use3DColumns ? (tile.transform.localScale.y / 2 + 0.3f) : 0.1f;
        label.transform.localPosition = new Vector3(0, yOffset, 0);

        TextMesh textMesh = label.AddComponent<TextMesh>();
        textMesh.text = count.ToString();
        textMesh.fontSize = 15;
        textMesh.characterSize = 0.1f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.white;

        // Hacer que el texto siempre mire a la cámara
        label.AddComponent<LookAtCamera>();
    }

    void CreateLegend(int maxDeaths)
    {
        GameObject legend = new GameObject("HeatmapLegend");
        legend.transform.position = new Vector3(-50, 5, -50);

        // Crear 5 muestras del gradiente
        int samples = 5;
        float sampleWidth = 3f;

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            int sampleCount = Mathf.RoundToInt(t * maxDeaths);

            // Crear muestra de color
            GameObject sample = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sample.transform.parent = legend.transform;
            sample.transform.localPosition = new Vector3(i * sampleWidth, 0, 0);
            sample.transform.localScale = new Vector3(sampleWidth - 0.5f, 1, 1);
            sample.transform.Rotate(90, 0, 0);

            Renderer sampleRenderer = sample.GetComponent<Renderer>();
            sampleRenderer.material.color = heatmapGradient.Evaluate(t);

            // Texto de la leyenda
            GameObject label = new GameObject("LegendLabel");
            label.transform.parent = legend.transform;
            label.transform.localPosition = new Vector3(i * sampleWidth, 0.3f, 0);

            TextMesh labelText = label.AddComponent<TextMesh>();
            labelText.text = $"{sampleCount}";
            labelText.fontSize = 12;
            labelText.anchor = TextAnchor.UpperCenter;
            labelText.alignment = TextAlignment.Center;
            labelText.color = Color.white;

            label.AddComponent<LookAtCamera>();
        }

        // Título de la leyenda
        GameObject title = new GameObject("LegendTitle");
        title.transform.parent = legend.transform;
        title.transform.localPosition = new Vector3((samples * sampleWidth) / 2 - sampleWidth / 2, 0.6f, 0);

        TextMesh titleText = title.AddComponent<TextMesh>();
        titleText.text = "MUERTES POR ZONA";
        titleText.fontSize = 15;
        titleText.anchor = TextAnchor.UpperCenter;
        titleText.alignment = TextAlignment.Center;
        titleText.color = Color.yellow;

        title.AddComponent<LookAtCamera>();
    }

    void CreateTestHeatmap()
    {
        Debug.Log("Heatmap: Creando datos de prueba");

        List<DeathPoint> testPoints = new List<DeathPoint>();

        // Crear datos de prueba concentrados en algunas áreas
        for (int i = 0; i < 50; i++)
        {
            // Área 1: Muchas muertes
            DeathPoint point1 = new DeathPoint
            {
                grid_x = UnityEngine.Random.Range(-20, 20),
                grid_z = UnityEngine.Random.Range(-20, 20),
                death_count = UnityEngine.Random.Range(3, 10),
                causes = "test",
                zone_name = "hot_zone"
            };
            testPoints.Add(point1);

            // Área 2: Pocas muertes
            DeathPoint point2 = new DeathPoint
            {
                grid_x = UnityEngine.Random.Range(40, 60),
                grid_z = UnityEngine.Random.Range(40, 60),
                death_count = UnityEngine.Random.Range(1, 3),
                causes = "test",
                zone_name = "cool_zone"
            };
            testPoints.Add(point2);
        }

        CreateHeatmapFromData(testPoints);
    }

    void ClearHeatmap()
    {
        foreach (var kvp in heatmapTiles)
        {
            if (kvp.Value.tileObject != null)
            {
                Destroy(kvp.Value.tileObject);
            }
        }
        heatmapTiles.Clear();

        // Destruir también objetos hijos del contenedor
        if (heatmapContainer != null)
        {
            foreach (Transform child in heatmapContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }

    // Método público para recargar
    public void ReloadHeatmap()
    {
        LoadHeatmapData();
    }

    void OnDestroy()
    {
        ClearHeatmap();
        if (heatmapContainer != null)
        {
            Destroy(heatmapContainer);
        }
    }

    // Clase auxiliar para almacenar info de tiles
    private class HeatmapTile
    {
        public GameObject tileObject;
        public Vector2Int gridPosition;
        public int deathCount;
    }
}

// Script auxiliar para hacer que los textos miren a la cámara
public class LookAtCamera : MonoBehaviour
{
    void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.forward);
        }
    }
}