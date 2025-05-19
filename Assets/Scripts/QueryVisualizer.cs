using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class QueryVisualizer : MonoBehaviour
{
    public LoadData loadData;
    public GameObject barPrefab;
    public GameObject protocolBubblePrefab;
    public float barWidth = 0.02f;
    public float spacing = 0.03f;
    public Transform fixedPosition;
    public Material barMaterial;
    public InputActionProperty hideGraphAction;

    private Transform wrapper;
    private Transform anchor;
    private Transform protocolWrapper;
    private TextMeshPro sharedLabel;

    public int axisFontSize = 1;
    public float axisLabelOffset = 0f;

    private static readonly Dictionary<string, string> protocolNameMap = new Dictionary<string, string>
    {
        { "1", "ICMP" },
        { "2", "IGMP" },
        { "6", "TCP" },
        { "17", "UDP" },
        { "41", "IPv6" },
        { "47", "GRE" },
        { "50", "ESP" },
        { "51", "AH" },
        { "58", "ICMPv6" },
        { "89", "OSPF" }
    };

    void Update()
    {
        if (hideGraphAction.action != null && hideGraphAction.action.WasPressedThisFrame())
        {
            HideHistogramAnimated();
        }
    }

    public void GenerateHistogram()
    {
        if (!loadData.isLoaded)
        {
            Debug.LogWarning("I dati non sono ancora stati caricati.");
            return;
        }

        if (wrapper == null)
        {
            GameObject wrapperGO = new GameObject("HistogramWrapper");
            wrapper = wrapperGO.transform;

            Transform cam = Camera.main.transform;
            Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
            wrapper.position = cam.position + forward * 1.2f + Vector3.down * 0.3f + -cam.right * 0.3f;
            wrapper.rotation = Quaternion.LookRotation(forward);

            var rb = wrapperGO.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var collider = wrapperGO.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.2f, 1.2f, 1.2f);
            collider.center = new Vector3(0.5f, 0.5f, 0f);

            var grab = wrapperGO.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.interactionLayers = InteractionLayerMask.GetMask("Default");
            grab.interactionManager = FindAnyObjectByType<XRInteractionManager>();
            grab.useDynamicAttach = true;
        }

        if (anchor == null)
        {
            GameObject anchorGO = new GameObject("HistogramAnchor");
            anchorGO.transform.SetParent(wrapper);
            anchor = anchorGO.transform;
            anchor.localPosition = Vector3.zero;
            anchor.localRotation = Quaternion.identity;
            anchor.localScale = Vector3.one * 2f;
        }

        foreach (Transform child in anchor)
            Destroy(child.gameObject);

        if (sharedLabel == null)
        {
            GameObject labelObj = new GameObject("SharedLabel");
            labelObj.transform.SetParent(anchor);
            sharedLabel = labelObj.AddComponent<TextMeshPro>();
            sharedLabel.fontSize = 1;
            sharedLabel.alignment = TextAlignmentOptions.Center;
            sharedLabel.color = Color.white;
            labelObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
            sharedLabel.gameObject.SetActive(false);
        }

        Dictionary<string, float> ipToByteSum = new Dictionary<string, float>();
        foreach (var row in loadData.data)
        {
            if (row.TryGetValue("Src_IP", out string ip) &&
                row.TryGetValue("Flow_Bytes_s", out string byteStr) &&
                float.TryParse(byteStr, out float bytes))
            {
                if (!ipToByteSum.ContainsKey(ip))
                    ipToByteSum[ip] = 0;
                ipToByteSum[ip] += bytes;
            }
        }

        float maxValue = 0f;
        foreach (var value in ipToByteSum.Values)
            if (value > maxValue)
                maxValue = value;

        int index = 0;
        foreach (var entry in ipToByteSum)
        {
            float normalizedHeight = (entry.Value / maxValue) * 0.15f;

            GameObject bar = Instantiate(barPrefab, anchor);
            bar.transform.localPosition = new Vector3(index * spacing, 0f, 0f);
            bar.transform.localScale = new Vector3(barWidth, 0f, barWidth);

            if (barMaterial != null)
                bar.GetComponent<Renderer>().material = barMaterial;

            StartCoroutine(AnimateBarGrowth(bar, normalizedHeight));

            var hover = bar.AddComponent<ShowLabelOnHover>();
            hover.ipText = entry.Key;
            hover.byteValue = entry.Value;
            hover.visualizer = this;

            bar.AddComponent<BoxCollider>();

            index++;
        }

        CreateXAxisLabel();
        CreateYAxisLabel();

        Debug.Log("Istogramma generato con " + index + " barre.");
    }

    public void GenerateProtocolBubbles()
    {
        if (!loadData.isLoaded)
        {
            Debug.LogWarning("I dati non sono ancora stati caricati.");
            return;
        }

        if (protocolWrapper != null)
            Destroy(protocolWrapper.gameObject);

        GameObject wrapperGO = new GameObject("ProtocolBubbleWrapper");
        protocolWrapper = wrapperGO.transform;

        Transform cam = Camera.main.transform;
        Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        protocolWrapper.position = cam.position + forward * 1.5f + Vector3.down * 0.2f;
        protocolWrapper.rotation = Quaternion.LookRotation(forward);

        Dictionary<string, int> protocolCount = new Dictionary<string, int>();
        foreach (var row in loadData.data)
        {
            if (row.TryGetValue("Protocol", out string protoCode))
            {
                protoCode = protoCode.Trim();

                if (string.IsNullOrEmpty(protoCode))
                    protoCode = "Unknown";

                Debug.Log($"Protocol letto: {protoCode}");

                string protoName = protocolNameMap.TryGetValue(protoCode, out var name) ? name : $"Unknown ({protoCode})";

                if (!protocolCount.ContainsKey(protoName))
                    protocolCount[protoName] = 0;

                protocolCount[protoName]++;
            }
        }

        Debug.Log("Conteggi protocolli trovati:");
        foreach (var entry in protocolCount)
        {
            Debug.Log($"Protocollo: {entry.Key}, Flussi: {entry.Value}");
        }

        int totalFlows = 0;
        foreach (var val in protocolCount.Values)
            totalFlows += val;

        int i = 0;
        foreach (var entry in protocolCount)
        {
            float angle = i * Mathf.PI * 2f / protocolCount.Count;
            Vector3 pos = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.05f;

            GameObject bubble = Instantiate(protocolBubblePrefab, protocolWrapper);
            bubble.transform.localPosition = pos + Vector3.up * UnityEngine.Random.Range(-0.1f, 0.1f);
            bubble.transform.localRotation = Quaternion.identity;
            bubble.transform.localScale = Vector3.one * 0.2f;

            var script = bubble.GetComponent<ProtocolBubble>();
            script.SetInfo(entry.Key, entry.Value, (float)entry.Value / totalFlows);

            bubble.AddComponent<FloatingMotion>();

            Rigidbody bubbleRb = bubble.GetComponent<Rigidbody>();
            if (bubbleRb == null)
            {
                bubbleRb = bubble.AddComponent<Rigidbody>();
            }
            bubbleRb.useGravity = false;
            bubbleRb.isKinematic = false;

            var bubbleGrab = bubble.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            if (bubbleGrab == null)
            {
                bubbleGrab = bubble.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                bubbleGrab.interactionLayers = InteractionLayerMask.GetMask("Default");
                bubbleGrab.interactionManager = FindAnyObjectByType<XRInteractionManager>();
                bubbleGrab.useDynamicAttach = true;
            }

            i++;
        }

        Debug.Log("Bolle dei protocolli generate.");
    }

    Color GetHeatmapColor(float valueInTB)
    {
        if (valueInTB == 0f)
            return Color.blue;
        else if (valueInTB < 2e-9f)
            return Color.cyan;
        else if (valueInTB < 1.6e-7f)
            return Color.green;
        else if (valueInTB < 3.4e-7f)
            return Color.yellow;
        else
            return Color.red;
    }

    public void GenerateHeatmap()
    {
        if (!loadData.isLoaded)
        {
            Debug.LogWarning("Dati non caricati.");
            return;
        }

        if (protocolWrapper != null)
            Destroy(protocolWrapper.gameObject);

        GameObject wrapperGO = new GameObject("HeatmapWrapper");
        protocolWrapper = wrapperGO.transform;

        // Posizionamento davanti alla camera (senza offset verticale)
        Transform cam = Camera.main.transform;
        Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        protocolWrapper.position = cam.position + forward * 3f;
        protocolWrapper.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(-90f, 0f, 0f);

        // Step 1: Mappa traffico (Src_IP, Dst_IP) -> byte
        Dictionary<(string, string), float> trafficMap = new Dictionary<(string, string), float>();
        foreach (var row in loadData.data)
        {
            if (row.TryGetValue("Src_IP", out string src) &&
                row.TryGetValue("Dst_IP", out string dst) &&
                row.TryGetValue("Flow_Bytes_s", out string bytesStr) &&
                float.TryParse(bytesStr, out float bytes))
            {
                var key = (src, dst);
                if (!trafficMap.ContainsKey(key))
                    trafficMap[key] = 0f;
                trafficMap[key] += bytes;
            }
        }

        // Step 2: IP unici
        List<string> srcIPs = trafficMap.Keys.Select(k => k.Item1).Distinct().ToList();
        List<string> dstIPs = trafficMap.Keys.Select(k => k.Item2).Distinct().ToList();

        float spacing = 0.1f;
        int numCols = srcIPs.Count;
        int numRows = dstIPs.Count;

        // Offset per centrare la griglia
        Vector3 centerOffset = new Vector3(
            (numCols - 1) * spacing / 2f,
            0f,
            (numRows - 1) * spacing / 2f
        );

        // Etichette colonna (sorgente)
        for (int x = 0; x < numCols; x++)
        {
            GameObject labelX = new GameObject("SrcLabel");
            labelX.transform.SetParent(protocolWrapper);
            labelX.transform.localPosition = new Vector3(x * spacing, spacing * 0.6f, -spacing) - centerOffset;

            var textX = labelX.AddComponent<TextMeshPro>();
            textX.text = srcIPs[x];
            textX.fontSize = 0.2f;
            textX.alignment = TextAlignmentOptions.Center;
            textX.rectTransform.sizeDelta = new Vector2(1, 1);
        }

        // Etichette riga (destinazione)
        for (int z = 0; z < numRows; z++)
        {
            GameObject labelZ = new GameObject("DstLabel");
            labelZ.transform.SetParent(protocolWrapper);
            labelZ.transform.localPosition = new Vector3(-spacing, 0, z * spacing) - centerOffset;

            var textZ = labelZ.AddComponent<TextMeshPro>();
            textZ.text = dstIPs[z];
            textZ.fontSize = 0.2f;
            textZ.alignment = TextAlignmentOptions.Right;
            textZ.rectTransform.sizeDelta = new Vector2(1, 1);
        }

        // Celle
        for (int x = 0; x < numCols; x++)
        {
            for (int z = 0; z < numRows; z++)
            {
                string src = srcIPs[x];
                string dst = dstIPs[z];
                float value = trafficMap.ContainsKey((src, dst)) ? trafficMap[(src, dst)] : 0f;
                float valueInTB = value / 1e12f;

                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.transform.SetParent(protocolWrapper);
                cell.transform.localScale = Vector3.one * spacing * 0.9f;
                cell.transform.localPosition = new Vector3(x * spacing, 0, z * spacing) - centerOffset;

                Color color = GetHeatmapColor(valueInTB);
                var renderer = cell.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = color;
                renderer.material = mat;
                if (mat == null)
                    Debug.LogError("SHADER NON TROVATO!");

            }
        }
    }









    private void CreateXAxisLabel()
    {
        GameObject xAxisLabel = new GameObject("XAxisLabel");
        xAxisLabel.transform.SetParent(anchor);
        TextMeshPro tmp = xAxisLabel.AddComponent<TextMeshPro>();
        tmp.text = "IP nel Tempo";
        tmp.fontSize = axisFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        xAxisLabel.transform.localPosition = new Vector3(0.25f, -axisLabelOffset + 0.15f, 0f);
        xAxisLabel.transform.localRotation = Quaternion.Euler(0, 0, 0);
    }

    private void CreateYAxisLabel()
    {
        GameObject yAxisLabel = new GameObject("YAxisLabel");
        yAxisLabel.transform.SetParent(anchor);
        TextMeshPro tmp = yAxisLabel.AddComponent<TextMeshPro>();
        tmp.text = "Terabyte per IP";
        tmp.fontSize = axisFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        yAxisLabel.transform.localPosition = new Vector3(-axisLabelOffset + 0.1f, 0.15f, 0f);
        yAxisLabel.transform.localRotation = Quaternion.Euler(0, 0, 90);
    }

    private IEnumerator AnimateBarGrowth(GameObject bar, float targetHeight, float duration = 0.5f)
    {
        Vector3 startScale = bar.transform.localScale;
        Vector3 endScale = new Vector3(startScale.x, targetHeight, startScale.z);

        Vector3 startPos = bar.transform.localPosition;
        Vector3 endPos = new Vector3(startPos.x, targetHeight / 2f, startPos.z);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            bar.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            bar.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        bar.transform.localScale = endScale;
        bar.transform.localPosition = endPos;
    }

    public void UpdateSharedLabel(string content, Vector3 barPosition)
    {
        if (sharedLabel != null)
        {
            sharedLabel.text = content;
            Vector3 newPos = new Vector3(barPosition.x, wrapper.position.y + 0.5f, barPosition.z);
            sharedLabel.transform.position = newPos;
            sharedLabel.transform.rotation = Quaternion.LookRotation(sharedLabel.transform.position - Camera.main.transform.position);
            sharedLabel.gameObject.SetActive(true);
        }
    }

    public void HideSharedLabel()
    {
        if (sharedLabel != null)
            sharedLabel.gameObject.SetActive(false);
    }

    public void HideHistogramAnimated()
    {
        if (wrapper == null) return;
        StartCoroutine(HideHistogramRoutine());
    }

    private IEnumerator HideHistogramRoutine()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 originalScale = wrapper.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            wrapper.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        wrapper.localScale = Vector3.zero;

        foreach (Transform child in wrapper)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Istogramma nascosto.");
    }
}
