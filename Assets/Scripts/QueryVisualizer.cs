using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
//using static UnityEditor.PlayerSettings;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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
    private GameObject sharedLabelQ2Background;
    private bool protocolBubblesGenerated = false;

    public int axisFontSize = 1;
    public float axisLabelOffset = 0f;
    public TextMeshPro sharedLabelQ2;

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

        if (protocolBubblesGenerated)
        {
            Debug.Log("Le bolle sono già presenti, non vengono rigenerate.");
            return;
        }

        Dictionary<string, int> protocolCount = new Dictionary<string, int>();
        foreach (var row in loadData.data)
        {
            if (row.TryGetValue("Protocol", out string protoCode))
            {
                protoCode = protoCode.Trim();
                if (string.IsNullOrEmpty(protoCode)) protoCode = "Unknown";

                string protoName = protocolNameMap.TryGetValue(protoCode, out var name) ? name : $"Unknown ({protoCode})";

                if (!protocolCount.ContainsKey(protoName))
                    protocolCount[protoName] = 0;

                protocolCount[protoName]++;
            }
        }

        int totalFlows = protocolCount.Values.Sum();
        int maxCount = protocolCount.Values.Max();
        float minScale = 0.3f;
        float maxScale = 0.8f;

        Transform cam = Camera.main.transform;
        Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        Vector3 basePos = cam.position + forward * 1.5f + Vector3.down * 0.2f;

        int i = 0;
        foreach (var entry in protocolCount)
        {
            float percentage = (float)entry.Value / totalFlows;
            float normalized = (float)entry.Value / maxCount;
            float scaledSize = Mathf.Lerp(minScale, maxScale, normalized);

            float angle = i * Mathf.PI * 2f / protocolCount.Count;
            float distance = 0.1f + scaledSize * 0.2f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance + Vector3.up * UnityEngine.Random.Range(-0.1f, 0.1f);
            Vector3 worldPos = basePos + offset;

            // === CREAZIONE DEL WRAPPER ===
            GameObject wrapper = new GameObject($"ProtocolBubbleWrapper_{entry.Key}");
            wrapper.transform.position = worldPos;
            wrapper.transform.rotation = Quaternion.identity;
            wrapper.transform.localScale = Vector3.one;

            // Collider (approssimativo, per il grab)
            SphereCollider col = wrapper.AddComponent<SphereCollider>();
            col.radius = scaledSize / 2f;
            col.center = Vector3.zero;

            // Rigidbody
            Rigidbody rb = wrapper.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.mass = 1f;
            rb.linearDamping = 2f;
            rb.angularDamping = 2f;

            // XRGrabInteractable
            var grab = wrapper.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            grab.interactionLayers = InteractionLayerMask.GetMask("Default");
            grab.interactionManager = FindAnyObjectByType<XRInteractionManager>();
            grab.useDynamicAttach = false;
            grab.movementType = XRBaseInteractable.MovementType.Instantaneous;

            // === SFERA COME FIGLIA ===
            GameObject bubble = Instantiate(protocolBubblePrefab);
            bubble.transform.SetParent(wrapper.transform, worldPositionStays: false);
            bubble.transform.localPosition = Vector3.zero;
            bubble.transform.localRotation = Quaternion.identity;
            bubble.transform.localScale = Vector3.one * scaledSize;

            var script = bubble.GetComponent<ProtocolBubble>();
            script.SetInfo(entry.Key, entry.Value, percentage);

            // Assicurati che il prefab NON abbia altri XRGrabInteractable o Rigidbody attivi!
            if (bubble.TryGetComponent<Rigidbody>(out var rbChild)) Destroy(rbChild);
            if (bubble.TryGetComponent<Collider>(out var colChild)) Destroy(colChild);
            if (bubble.TryGetComponent<XRGrabInteractable>(out var grabChild)) Destroy(grabChild);

            i++;
        }

        protocolBubblesGenerated = true;
        Debug.Log("Bolle dei protocolli generate con contenitore individuale.");
    }








    Color GetHeatmapColor(float value, float min, float max)
    {
        if (value <= 0f)
            return Color.blue;

        float logValue = Mathf.Log10(Mathf.Max(value, min));
        float normalized = Mathf.InverseLerp(Mathf.Log10(min), Mathf.Log10(max), logValue);

        if (normalized < 0.25f)
            return Color.Lerp(Color.blue, Color.cyan, normalized / 0.25f);
        else if (normalized < 0.5f)
            return Color.Lerp(Color.cyan, Color.green, (normalized - 0.25f) / 0.25f);
        else if (normalized < 0.75f)
            return Color.Lerp(Color.green, Color.yellow, (normalized - 0.5f) / 0.25f);
        else
            return Color.Lerp(Color.yellow, Color.red, (normalized - 0.75f) / 0.25f);
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

        Transform cam = Camera.main.transform;
        Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
        protocolWrapper.position = cam.position + forward * 3f;
        protocolWrapper.rotation = Quaternion.LookRotation(forward);

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

        float min = float.MaxValue;
        float max = float.MinValue;
        foreach (var val in trafficMap.Values)
        {
            if (val > 0)
            {
                min = Mathf.Min(min, val);
                max = Mathf.Max(max, val);
            }
        }
        min = Mathf.Max(min, 1f);
        Debug.Log($"[Heatmap] Min traffic: {min}, Max traffic: {max}");

        List<string> srcIPs = trafficMap.Keys.Select(k => k.Item1).Distinct().ToList();
        List<string> dstIPs = trafficMap.Keys.Select(k => k.Item2).Distinct().ToList();

        int numCols = srcIPs.Count;
        int numRows = dstIPs.Count;

        Vector3 centerOffset = new Vector3(
            (numCols - 1) * spacing / 2f,
            (numRows - 1) * spacing / 2f,
            0f
        );

        for (int x = 0; x < numCols; x++)
        {
            for (int y = 0; y < numRows; y++)
            {
                string src = srcIPs[x];
                string dst = dstIPs[y];
                float value = trafficMap.ContainsKey((src, dst)) ? trafficMap[(src, dst)] : 0f;

                GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                cell.transform.SetParent(protocolWrapper);
                cell.transform.localScale = Vector3.one * spacing * 0.9f;
                cell.transform.localPosition = new Vector3(x * spacing, y * spacing, 0) - centerOffset;
                cell.transform.localRotation = Quaternion.identity;

                Color color = GetHeatmapColor(value, min, max);
                var renderer = cell.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = color;
                renderer.material = mat;

                cell.AddComponent<BoxCollider>();
                cell.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                var hover = cell.AddComponent<ShowLabelOnHover>();
                hover.ipText = $"{src} -> {dst}";
                hover.byteValue = value;
                hover.visualizer = this;
                hover.isQuery2 = true;

                float logValue = Mathf.Log10(Mathf.Max(value, min));
                float normalized = Mathf.InverseLerp(Mathf.Log10(min), Mathf.Log10(max), logValue);
                Debug.Log($"[Heatmap] {src}->{dst}: {value} Byte/s -> norm: {normalized:F2} -> color: {color}");
            }
        }

        // Titolo sopra la heatmap
        GameObject titleLabel = new GameObject("HeatmapTitle");
        titleLabel.transform.SetParent(protocolWrapper);

        var titleText = titleLabel.AddComponent<TextMeshPro>();
        titleText.text = "Heatmap del Traffico tra IP";
        titleText.fontSize = 0.6f;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.outlineWidth = 0.2f;
        titleText.outlineColor = Color.black;

        // Posizionato al centro, sopra la griglia
        titleLabel.transform.localPosition = new Vector3(0f, (numRows * spacing / 2f) + 0.1f, 0f);
        titleLabel.transform.localRotation = Quaternion.LookRotation(titleLabel.transform.position - cam.position);

        // Etichetta asse X (Src_IP) – orizzontale, vicina alla base
        GameObject xAxisLabel = new GameObject("X_Label_SrcIP");
        xAxisLabel.transform.SetParent(protocolWrapper);
        var xText = xAxisLabel.AddComponent<TextMeshPro>();
        xText.text = "Indirizzi Sorgente (Src_IP)";
        xText.fontSize = 0.5f;
        xText.color = Color.white;
        xText.alignment = TextAlignmentOptions.Center;

        // Posizionata al centro lungo l'asse X, sotto la heatmap
        xAxisLabel.transform.localPosition = new Vector3(0f, -(numRows * spacing / 2f) - 0.1f, 0f);
        xAxisLabel.transform.localRotation = Quaternion.LookRotation(xAxisLabel.transform.position - cam.position);

        // Etichetta asse Y (Dst_IP) – verticale
        GameObject yAxisLabel = new GameObject("Y_Label_DstIP");
        yAxisLabel.transform.SetParent(protocolWrapper);
        var yText = yAxisLabel.AddComponent<TextMeshPro>();
        yText.text = "Indirizzi Destinazione (Dst_IP)";
        yText.fontSize = 0.5f;
        yText.color = Color.white;
        yText.alignment = TextAlignmentOptions.Center;

        // Posizionata al centro lungo l'asse Y, a sinistra, con rotazione verticale
        yAxisLabel.transform.localPosition = new Vector3(-(numCols * spacing / 2f) - 0.1f, 0f, 0f);
        yAxisLabel.transform.localRotation = Quaternion.LookRotation(yAxisLabel.transform.position - cam.position) * Quaternion.Euler(0, 0, 90);

        // Etichetta interattiva (già presente)
        if (sharedLabelQ2 == null)
        {
            GameObject labelObj = new GameObject("SharedLabelQ2");
            labelObj.transform.SetParent(Camera.main.transform);
            sharedLabelQ2 = labelObj.AddComponent<TextMeshPro>();
            sharedLabelQ2.fontSize = 0.15f;
            sharedLabelQ2.alignment = TextAlignmentOptions.Center;
            sharedLabelQ2.color = Color.black;
            sharedLabelQ2.outlineWidth = 0.2f;
            sharedLabelQ2.outlineColor = Color.white;

            labelObj.transform.localRotation = Quaternion.identity;
            sharedLabelQ2.gameObject.SetActive(false);

            sharedLabelQ2Background = GameObject.CreatePrimitive(PrimitiveType.Quad);
            sharedLabelQ2Background.name = "LabelBackground";
            sharedLabelQ2Background.transform.SetParent(sharedLabelQ2.transform);
            sharedLabelQ2Background.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            sharedLabelQ2Background.transform.localScale = new Vector3(0.25f, 0.1f, 1f);

            var bgRenderer = sharedLabelQ2Background.GetComponent<Renderer>();
            Material mat = Resources.Load<Material>("Materials/BiancoPanelHeat");
            bgRenderer.material = mat;

            //bgRenderer.material.color = new Color(0.9f, 0.9f, 0.9f, 0.5f);
        }

        // === WRAPPER DELLA LEGENDA ===
        GameObject legendWrapperGO = new GameObject("HeatmapLegendWrapper");
        Transform legendWrapper = legendWrapperGO.transform;
        legendWrapper.SetParent(protocolWrapper);

        // Posiziona il wrapper a destra della heatmap
        legendWrapper.localPosition = new Vector3((numCols * spacing / 2f) + 0.2f, -0.2f, 0f);
        legendWrapper.localRotation = Quaternion.identity;
        legendWrapper.localScale = Vector3.one;

        // Colori e descrizioni
        string[] levels = new string[]
        {
    "Traffico nullo o molto basso",
    "Traffico basso",
    "Traffico medio",
    "Traffico alto",
    "Traffico molto alto"
        };

        Color[] colors = new Color[]
        {
    Color.blue,
    Color.cyan,
    Color.green,
    Color.yellow,
    Color.red
        };

        // Posizione iniziale nel wrapper
        Vector3 legendStartLocal = new Vector3(0f, (numRows * spacing / 2f), 0f);

        for (int i = 0; i < levels.Length; i++)
        {
            // === CELLA COLORATA ===
            GameObject legendCell = GameObject.CreatePrimitive(PrimitiveType.Quad);
            legendCell.name = $"LegendCell_{i}";
            legendCell.transform.SetParent(legendWrapper);
            legendCell.transform.localScale = Vector3.one * spacing * 2f;
            legendCell.transform.localPosition = legendStartLocal + new Vector3(0f, -i * 0.15f, 0f);
            legendCell.transform.localRotation = Quaternion.identity;

            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = colors[i];
            legendCell.GetComponent<Renderer>().material = mat;

            // === ETICHETTA DESCRITTIVA ===
            GameObject legendLabel = new GameObject($"LegendLabel_{i}");
            legendLabel.transform.SetParent(legendWrapper);
            legendLabel.transform.localScale = Vector3.one * 0.01f; // molto importante per visibilità in 3D

            var textMesh = legendLabel.AddComponent<TextMeshPro>();
            textMesh.text = levels[i];
            textMesh.fontSize = 20f;
            textMesh.color = Color.white;
            textMesh.outlineWidth = 0.2f;
            textMesh.outlineColor = Color.black;
            textMesh.alignment = TextAlignmentOptions.Left;
            textMesh.textWrappingMode = TextWrappingModes.NoWrap;
            textMesh.enableAutoSizing = false;

            legendLabel.transform.localPosition = legendCell.transform.localPosition + new Vector3(0.15f, 0f, 0f);
            legendLabel.transform.localRotation = Quaternion.identity;
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

    public void UpdateSharedLabelQ2(string content, Vector3 labelPosition)
    {
        if (sharedLabelQ2 != null)
        {
            sharedLabelQ2.text = content;

            // Calcola direzione dalla cella verso la camera e normalizzala
            Vector3 toCameraDir = (Camera.main.transform.position - labelPosition).normalized;
            Vector3 offset = toCameraDir * 0.2f + Vector3.up * 0.05f;
            Vector3 newPos = labelPosition + offset;

            sharedLabelQ2.transform.position = newPos;
            sharedLabelQ2.transform.rotation = Quaternion.LookRotation(sharedLabelQ2.transform.position - Camera.main.transform.position);

            sharedLabelQ2.gameObject.SetActive(true);
        }
    }

    public void HideSharedLabelQ2()
    {
        if (sharedLabelQ2 != null)
            sharedLabelQ2.gameObject.SetActive(false);
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
