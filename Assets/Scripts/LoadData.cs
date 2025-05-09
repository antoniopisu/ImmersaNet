using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LoadData : MonoBehaviour
{
    public List<Dictionary<string, string>> data = new List<Dictionary<string, string>>();
    public bool isLoaded = false;           

    public IEnumerator LoadFile()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("benign_high_bytes");

        if (csvFile == null)
        {
            Debug.LogError("CSV file not found in Resources.");
            yield break;
        }

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);

        if (lines.Length <= 1)
        {
            Debug.LogError("CSV file is empty or incorrectly formatted.");
            yield break;
        }

        string[] headers = lines[0].Split(',');
        for (int j = 0; j < headers.Length; j++)
        {
            headers[j] = headers[j].Trim().Replace("\r", "");
        }

        Debug.Log("Header puliti:");
        foreach (var h in headers)
        {
            //Debug.Log("Header: [" + h + "]");
        }

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;

            string[] values = line.Split(',');

            Dictionary<string, string> rowDict = new Dictionary<string, string>();
            for (int j = 0; j < headers.Length; j++)
            {
                if (j < values.Length)
                {
                    string cleanValue = values[j].Trim().Replace("\r", "");
                    rowDict[headers[j]] = cleanValue;
                }
            }

            data.Add(rowDict);

            // Solo ogni 10 righe lascia tempo al caricamento
            if (i % 10 == 0)
            {
                yield return null;
            }
        }

        Debug.Log("Dati caricati: " + data.Count);
        isLoaded = true;
    }

    void Start()
    {
        StartCoroutine(LoadFile());
    }
}
