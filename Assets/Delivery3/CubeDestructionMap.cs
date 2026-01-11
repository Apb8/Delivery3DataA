using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class CubeDestructionMap : MonoBehaviour
{
    private string apiURL = "http://citmalumnes.upc.es/~hugocc2/game_analytics.php";
    
    [Header("Configuración")]
    public KeyCode toggleKey = KeyCode.Alpha2;
    public int fontSize = 15;
    
    private List<GameObject> visibleTexts = new List<GameObject>();
    private bool isShowing = false;
    
    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (!isShowing)
            {
                ShowCubeCounts();
            }
            else
            {
                HideCubeCounts();
            }
        }
    }
    
    void ShowCubeCounts()
    {
        isShowing = true;
        StartCoroutine(LoadAndShow());
    }
    
    void HideCubeCounts()
    {
        isShowing = false;
        
        foreach (GameObject text in visibleTexts)
        {
            if (text != null) 
                text.SetActive(false);
        }
    }
    
    IEnumerator LoadAndShow()
    {
        ClearTexts();
        
        string url = apiURL + "?get_cube_positions=1&limit=50";
        
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Datos de cubos recibidos");
                CreateTextsFromResponse(www.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning("Error cargando cubos. Usando datos de prueba.");
                CreateTestTexts();
            }
        }
    }
    
    void CreateTextsFromResponse(string json)
    {
        try
        {
            int startIndex = json.IndexOf("\"cube_positions\":[");
            
            if (startIndex > 0)
            {
                startIndex += "\"cube_positions\":[".Length;
                int endIndex = json.LastIndexOf("]");
                
                if (endIndex > startIndex)
                {
                    string arrayStr = json.Substring(startIndex, endIndex - startIndex);
                    string[] items = arrayStr.Split(new string[] {"},"}, System.StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (string item in items)
                    {
                        ParseAndCreateText(item);
                    }
                    
                    Debug.Log($"Creados {visibleTexts.Count} contadores de cubos");
                    return;
                }
            }
            
            CreateTestTexts();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parseando cubos: " + e.Message);
            CreateTestTexts();
        }
    }
    
    void ParseAndCreateText(string item)
    {
        string cleanItem = item.Replace("{", "").Replace("}", "").Replace("\"", "").Trim();
        
        if (string.IsNullOrEmpty(cleanItem)) return;
        
        float x = 0, z = 0;
        int count = 0;
        string type = "";
        
        string[] parts = cleanItem.Split(',');
        foreach (string part in parts)
        {
            string[] keyValue = part.Split(':');
            if (keyValue.Length >= 2)
            {
                string key = keyValue[0].Trim();
                string value = keyValue[1].Trim();
                
                switch (key)
                {
                    case "grid_x":
                        float.TryParse(value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out x);
                        x = x / 10f;
                        break;
                        
                    case "grid_z":
                        float.TryParse(value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out z);
                        z = z / 10f;
                        break;
                        
                    case "destruction_count":
                        int.TryParse(value, out count);
                        break;
                        
                    case "cube_type":
                        type = value;
                        break;
                }
            }
        }
        
        if (count > 0)
        {
            CreateTextAtPosition(x, z, count, type);
        }
    }
    
    void CreateTextAtPosition(float x, float z, int count, string type)
    {
        Vector3 position = new Vector3(x, 2f, z);
        
        GameObject textObj = new GameObject("CubeCounterText");
        textObj.transform.position = position;
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = $"×{count}";
        textMesh.fontSize = fontSize;
        textMesh.color = GetColorForType(type);
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        textObj.AddComponent<LookAtCamera>();
        
        visibleTexts.Add(textObj);
    }
    
    Color GetColorForType(string type)
    {
        type = type.ToLower();
        
        if (type.Contains("wood")) return new Color(0.6f, 0.3f, 0.1f);
        if (type.Contains("stone")) return Color.gray;
        if (type.Contains("metal")) return Color.cyan;
        if (type.Contains("glass")) return new Color(0.8f, 0.9f, 1f);
        
        return Color.blue;
    }
    
    void CreateTestTexts()
    {
        CreateTextAtPosition(10f, 5f, 3, "wood");
        CreateTextAtPosition(-5f, 8f, 1, "stone");
        CreateTextAtPosition(3f, -7f, 5, "metal");
        CreateTextAtPosition(-8f, -4f, 2, "wood");
        
        Debug.Log("Creados 4 contadores de prueba");
    }
    
    void ClearTexts()
    {
        foreach (GameObject text in visibleTexts)
        {
            if (text != null)
                Destroy(text);
        }
        visibleTexts.Clear();
    }
    
    void OnDestroy()
    {
        ClearTexts();
    }
}

public class LookAtCamera : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                           Camera.main.transform.rotation * Vector3.up);
        }
    }
}