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

    private Dictionary<GameObject, GameObject> lineToPanel = new();
    private Dictionary<GameObject, (Color start, Color end)> originalColors = new();

    public void ShowInfo(Dictionary<string, string> info, GameObject lineObject)
    {
        if (panelPrefab == null)
        {
            Debug.LogError("Panel prefab not assigned.");
            return;
        }

        if (lineToPanel.ContainsKey(lineObject))
        {
            Debug.Log($"[ShowInfo] Panel already open for line {lineObject.name}");
            return;
        }

        Transform cam = Camera.main.transform;
        Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 position = cam.position + forward * 1.2f + Vector3.up * 0.2f;

        GameObject panelInstance = Instantiate(panelPrefab, position, Quaternion.LookRotation(forward));
        panelInstance.transform.localScale = Vector3.one * 0.002f;

        TextMeshProUGUI textField = panelInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (textField != null)
            textField.text = FormatInfo(info);

        // Evidenzia la linea
        var lr = lineObject.GetComponent<LineRenderer>();
        if (lr != null)
        {
            originalColors[lineObject] = (lr.startColor, lr.endColor);
            lr.startColor = Color.yellow;
            lr.endColor = Color.yellow;
        }

        lineToPanel[lineObject] = panelInstance;

        Button closeBtn = panelInstance.GetComponentsInChildren<Button>().FirstOrDefault(b => b.name == "CloseButton");
        if (closeBtn != null)
        {
            closeBtn.onClick.AddListener(() => ClosePanel(lineObject));
        }

        StartCoroutine(AutoHidePanel(lineObject, 20f));
    }

    private void ClosePanel(GameObject lineObject)
    {
        if (lineToPanel.TryGetValue(lineObject, out GameObject panel))
        {
            Destroy(panel);
            lineToPanel.Remove(lineObject);
        }

        if (originalColors.TryGetValue(lineObject, out var colors))
        {
            var lr = lineObject.GetComponent<LineRenderer>();
            if (lr != null)
            {
                lr.startColor = colors.start;
                lr.endColor = colors.end;
            }

            originalColors.Remove(lineObject);
        }
    }

    private IEnumerator AutoHidePanel(GameObject lineObject, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ClosePanel(lineObject);
    }

    private string FormatInfo(Dictionary<string, string> data)
    {
        Dictionary<string, string> protocolNames = new()
        {
            { "6", "TCP" }, { "17", "UDP" }, { "1", "ICMP" }, { "2", "IGMP" },
            { "47", "GRE" }, { "50", "ESP" }, { "51", "AH" }, { "89", "OSPF" }
        };

        string result = "<b>Connection Info</b>\n\n";

        foreach (var entry in data)
        {
            string key = entry.Key;
            string value = entry.Value;

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double numericValue))
                value = numericValue.ToString("F2", CultureInfo.InvariantCulture);

            if (key.Equals("Protocol", StringComparison.OrdinalIgnoreCase) &&
                protocolNames.TryGetValue(((int)Math.Round(numericValue)).ToString(), out string name))
                value = $"{name} ({(int)Math.Round(numericValue)})";

            if (key == "Label" && !value.ToLower().Contains("benign"))
                result += $"<b>{key}:</b> <color=red>{value}</color>\n";
            else
                result += $"<b>{key}:</b> {value}\n";
        }

        return result;
    }
}
