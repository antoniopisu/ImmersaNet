using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Globalization;
using System.Linq;
using UnityEngine.UI;

public class ConnectionInfoPanel : MonoBehaviour
{
    public GameObject panelPrefab;

    public void ShowInfo(Dictionary<string, string> info)
    {
        if (panelPrefab == null)
        {
            Debug.LogError("Panel prefab not assigned.");
            return;
        }

        Transform cam = Camera.main.transform;
        Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 position = cam.position + forward * 1.2f + Vector3.up * 0.2f;

        GameObject panelInstance = Instantiate(panelPrefab, position, Quaternion.LookRotation(forward));
        panelInstance.transform.localScale = Vector3.one * 0.002f;
        Debug.Log("[ShowInfo] Called with keys: " + string.Join(", ", info.Keys));

        TextMeshProUGUI textField = panelInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (textField != null)
            textField.text = FormatInfo(info);

        Button closeBtn = panelInstance.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "CloseButton");
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(() => Destroy(panelInstance));
        }
        else
        {
            Debug.LogWarning("CloseButton non trovato nel prefab.");
        }

        StartCoroutine(AutoHidePanel(panelInstance, 60f));
    }

    private string FormatInfo(Dictionary<string, string> data)
    {
        Dictionary<string, string> protocolNames = new Dictionary<string, string>
        {
            { "6", "TCP" },
            { "17", "UDP" },
            { "1", "ICMP" },
            { "2", "IGMP" },
            { "47", "GRE" },
            { "50", "ESP" },
            { "51", "AH" },
            { "89", "OSPF" }
        };

        string result = "<b>Connection Info</b>\n\n";

        foreach (var entry in data)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double numericValue))
            {
                value = numericValue.ToString("F2", CultureInfo.InvariantCulture);
            }

            if (key.Equals("Protocol", StringComparison.OrdinalIgnoreCase))
            {
                string original = value;

                if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double protoNumber))
                {
                    int protoInt = (int)Math.Round(protoNumber);
                    string protoKey = protoInt.ToString();

                    if (protocolNames.TryGetValue(protoKey, out string protoName))
                        value = $"{protoName} ({protoKey})";
                    else
                        value = $"Unknown ({original})";

                    Debug.Log($"[ConnectionInfoPanel] Protocol resolved: {value}");
                }
            }

            if (key == "Label" && !value.ToLower().Contains("benign"))
                result += $"<b>{key}:</b> <color=red>{value}</color>\n";
            else
                result += $"<b>{key}:</b> {value}\n";
        }

        return result;
    }

    private IEnumerator AutoHidePanel(GameObject panel, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (panel != null)
            Destroy(panel);
    }
}