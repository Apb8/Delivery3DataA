using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

public class AcidLakeDeathCounter : MonoBehaviour
{
    private string apiURL = "http://citmalumnes.upc.es/~hugocc2/game_analytics.php";
    
    [Header("Configuración")]
    public KeyCode toggleKey = KeyCode.Alpha3;
    public int fontSize = 15;
    public float textHeight = 3f;
    
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
            ShowLakeCounts();
        }
        else
        {
            HideLakeCounts();
        }
    }
    
    void ShowLakeCounts()
    {
        StartCoroutine(LoadAcidLakeData());
    }
    
    IEnumerator LoadAcidLakeData()
    {
        ClearAllTexts();
        
        string url = apiURL + "?get_acid_lake_deaths=1";
        
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Datos de lagos recibidos");
                CreateTextsFromResponse(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Error cargando lagos: " + www.error);
            }
        }
    }
    
    void CreateTextsFromResponse(string json)
    {
        try
        {
            int startIndex = json.IndexOf("\"acid_lakes\":[");
            
            if (startIndex > 0)
            {
                startIndex += "\"acid_lakes\":[".Length;
                int endIndex = json.LastIndexOf("]");
                
                if (endIndex > startIndex)
                {
                    string arrayStr = json.Substring(startIndex, endIndex - startIndex);
                    string[] items = arrayStr.Split(new string[] {"},"}, System.StringSplitOptions.RemoveEmptyEntries);
                    
                    foreach (string item in items)
                    {
                        ParseAndCreateText(item);
                    }
                    
                    Debug.Log($"Creados {textObjects.Count} contadores de lagos");
                    return;
                }
            }
            
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parseando lagos: " + e.Message);
        }
    }
    
    void ParseAndCreateText(string item)
    {
        string cleanItem = item.Replace("{", "").Replace("}", "").Replace("\"", "").Trim();
        
        if (string.IsNullOrEmpty(cleanItem)) return;
        
        string lakeName = "";
        int deathCount = 0;
        float posX = 0, posZ = 0;
        
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
                    case "lake_name":
                        lakeName = value;
                        break;
                        
                    case "deaths_count":
                        int.TryParse(value, out deathCount);
                        break;
                        
                    case "position_x":
                        float.TryParse(value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out posX);
                        posX = posX;
                        break;
                        
                    case "position_z":
                        float.TryParse(value, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out posZ);
                        posZ = posZ;
                        break;
                }
            }
        }
        
        if ((posX == 0 && posZ == 0) && !string.IsNullOrEmpty(lakeName))
        {
            Vector3 lakePosition = FindLakeInScene(lakeName);
            if (lakePosition != Vector3.zero)
            {
                posX = lakePosition.x;
                posZ = lakePosition.z;
            }
        }
        
        if (deathCount > 0 && (posX != 0 || posZ != 0))
        {
            CreateTextAtPosition(posX, posZ, deathCount, lakeName);
        }
    }
    
    Vector3 FindLakeInScene(string lakeName)
    {
        GameObject lake = GameObject.Find(lakeName);
        
        if (lake == null)
        {
            GameObject[] acidLakes = GameObject.FindGameObjectsWithTag("AcidLake");
            foreach (GameObject obj in acidLakes)
            {
                if (obj.name.Contains(lakeName) || lakeName.Contains(obj.name))
                {
                    lake = obj;
                    break;
                }
            }
        }
        
        return lake != null ? lake.transform.position : Vector3.zero;
    }
    
    void CreateTextAtPosition(float x, float z, int count, string lakeName)
    {
        Vector3 position = new Vector3(x, textHeight, z);
        
        GameObject textObj = new GameObject($"LakeCounter_{lakeName}");
        textObj.transform.position = position;
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = $"×{count}";
        textMesh.fontSize = fontSize;
        textMesh.color = Color.green;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        
        textObj.AddComponent<LookAtCamera>();
        
        textObjects.Add(textObj);
    }
    
    void HideLakeCounts()
    {
        foreach (GameObject text in textObjects)
        {
            if (text != null)
                Destroy(text);
        }
        textObjects.Clear();
    }
    
    void ClearAllTexts()
    {
        HideLakeCounts();
    }
    
    void OnDestroy()
    {
        ClearAllTexts();
    }
}