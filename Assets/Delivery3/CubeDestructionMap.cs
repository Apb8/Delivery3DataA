using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class SimpleCubeGrouped : MonoBehaviour
{
    private string apiURL = "http://citmalumnes.upc.es/~hugocc2/game_analytics.php";
    
    [Header("Configuracion")]
    public KeyCode toggleKey = KeyCode.Alpha2;
    public int fontSize = 15;
    public float textHeight = 2f;
    
    private List<GameObject> textObjects = new List<GameObject>();
    private bool isVisible = false;
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleDisplay();
        }
    }
    
    void ToggleDisplay()
    {
        isVisible = !isVisible;
        
        if (isVisible)
        {
            ShowCounters();
        }
        else
        {
            HideCounters();
        }
    }
    
    void ShowCounters()
    {
        StartCoroutine(LoadGroupedData());
    }
    
    IEnumerator LoadGroupedData()
    {
        // Limpiar anteriores
        ClearTexts();
        
        string url = apiURL + "?get_cube_positions_grouped=1&limit=50";
        
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                ProcessData(www.downloadHandler.text);
            }
            else
            {
                //Debug.Log("Error, mostrando datos d prueba");
                CreateGroupedTestData();
            }
        }
    }
    
    void ProcessData(string json)
    {
        try
        {            
            int start = json.IndexOf("\"cube_positions\":[") + "\"cube_positions\":[".Length;
            int end = json.LastIndexOf("]");
            
            if (start > "\"cube_positions\":[".Length && end > start)
            {
                string array = json.Substring(start, end - start);
                string[] items = array.Split(new string[] {"},"}, System.StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string item in items)
                {
                    ParseItem(item);
                }
                
                Debug.Log($"Mostrando {textObjects.Count} contadores agrupados");
                return;
            }
        }
        catch
        {
            // Si falla, crear datos de prueba
        }
        
        CreateGroupedTestData();
    }
    
    void ParseItem(string item)
    {
        string clean = item.Replace("{", "").Replace("}", "").Replace("\"", "").Trim();
        
        if (string.IsNullOrEmpty(clean)) return;
        
        float x = 0, z = 0, y = 2f;
        int count = 0;
        
        string[] parts = clean.Split(',');
        foreach (string part in parts)
        {
            string[] kv = part.Split(':');
            if (kv.Length >= 2)
            {
                string key = kv[0].Trim();
                string value = kv[1].Trim();
                
                if (key == "grid_x" || key == "position_x")
                {
                    if (float.TryParse(value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out float val))
                    {
                        x = val / 10f; // Dividir por 10
                    }
                }
                else if (key == "grid_z" || key == "position_z")
                {
                    if (float.TryParse(value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out float val))
                    {
                        z = val / 10f; // Dividir por 10
                    }
                }
                else if (key == "avg_y" || key == "position_y")
                {
                    if (float.TryParse(value, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out float val))
                    {
                        y = (val / 10f) + 1f; // Dividir por 10 y add altura
                    }
                }
                else if (key == "total_destructions" || key == "destruction_count" || key == "count")
                {
                    int.TryParse(value, out count);
                }
            }
        }
        
        if (count > 0)
        {
            CreateText(x, y, z, count);
        }
    }
    
    void CreateText(float x, float y, float z, int count)
    {
        Vector3 position = new Vector3(x, y + 1f, z);
        
        GameObject textObj = new GameObject("CubeGrouped");
        textObj.transform.position = position;
        
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = $"×{count}";
        tm.fontSize = fontSize;
        tm.color = new Color(0, 0.5f, 1f); // Azul
        tm.anchor = TextAnchor.MiddleCenter;
                
        textObj.AddComponent<SimpleLookAt>();
        
        textObjects.Add(textObj);
    }
    
    void CreateGroupedTestData()
    {
        // Datos de prueba AGRUPADOS
        // Misma pos = suma auto
        
        // Pos (10, 2, 5) con 3 destrucciones + 2 destrucciones = 5 total
        CreateText(10f, 2f, 5f, 5);  // ×5
        
        // Pos (-5, 1, 8) con 1 destruccion
        CreateText(-5f, 1f, 8f, 1);  // ×1
        
        // Pos (3, 0, -7) con 5 destrucciones + 3 destrucciones = 8 total
        CreateText(3f, 0f, -7f, 8);  // ×8
        
        Debug.Log("Mostrando datos agrupados de prueba");
    }
    
    void HideCounters()
    {
        foreach (GameObject text in textObjects)
        {
            if (text != null)
                Destroy(text);
        }
        textObjects.Clear();
    }
    
    void ClearTexts()
    {
        HideCounters();
    }
    
    void OnDestroy()
    {
        ClearTexts();
    }
}

public class SimpleLookAt : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}