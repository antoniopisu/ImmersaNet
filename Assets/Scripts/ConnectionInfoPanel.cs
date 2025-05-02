using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ConnectionInfoPanel : MonoBehaviour
{
    public GameObject panelPrefab;
    private GameObject currentPanel;

    public void ShowInfo(Dictionary<string, string> info)
    {
        if (panelPrefab == null)
        {
            Debug.LogError("Panel prefab not assigned.");
            return;
        }

        if (currentPanel != null)
            Destroy(currentPanel);

        Transform cam = Camera.main.transform;
        Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 position = cam.position + forward * 1.2f + Vector3.up * 0.2f;

        currentPanel = Instantiate(panelPrefab, position, Quaternion.LookRotation(forward));
        currentPanel.transform.localScale = Vector3.one * 0.002f;

        TextMeshProUGUI textField = currentPanel.GetComponentInChildren<TextMeshProUGUI>();
        if (textField != null)
        {
            textField.text = FormatInfo(info);
        }

        StartCoroutine(AutoHidePanel(5f));
    }

    private string FormatInfo(Dictionary<string, string> data)
    {
        string result = "<b>Connection Info</b>\n\n";
        foreach (var entry in data)
        {
            result += $"<b>{entry.Key}:</b> {entry.Value}\n";
        }
        return result;
    }

    private IEnumerator AutoHidePanel(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (currentPanel != null)
            Destroy(currentPanel);
    }
}
