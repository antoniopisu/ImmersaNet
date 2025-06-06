using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using System.Globalization;

public class VisualizeNetwork : MonoBehaviour
{
    public LoadData loadData;
    public GameObject nodePrefab;
    public GameObject linePrefab;
    private AudioClip alarmClip;
    public int nodesPerLevel = 7;
    public float levelHeight = 1f;
    public float ringRadius = 3f;
    public Vector3 treeCenterOffset = Vector3.zero;
    public Transform playerCamera;
    public float forwardOffset = 10.0f;


    [Header("Node Scaling Settings")]
    public float minNodeScale = 0.2f;
    public float maxNodeScale = 1.0f;

    [Header("DoS Detection")]
    [Tooltip("Soglia per segnalazione traffico sospetto (99 percentile Flow_Bytes_s)")]
    public double dosThreshold = 0;

    [HideInInspector] public bool visualizationEnabled = false;

    private Dictionary<string, GameObject> ipToNode = new Dictionary<string, GameObject>();
    private Dictionary<string, double> ipToCumulativeBytes = new Dictionary<string, double>();

    private class ActiveLine
    {
        public GameObject lineObj;
        public DateTime startTime;
        public DateTime endTime;
    }

    private List<ActiveLine> activeLines = new List<ActiveLine>();

    void Start()
    {
        alarmClip = Resources.Load<AudioClip>("Sounds/cyber-alarms-synthesized");

        if (playerCamera != null)
        {
            Vector3 forwardXZ = new Vector3(playerCamera.forward.x, 0f, playerCamera.forward.z).normalized;
            treeCenterOffset = playerCamera.position + forwardXZ * forwardOffset;
        }

        StartCoroutine(InitVisualNetwork());
    }


    IEnumerator InitVisualNetwork()
    {
        while (!loadData.isLoaded)
            yield return null;

        HashSet<string> ipSet = new HashSet<string>();
        foreach (var row in loadData.data)
        {
            if (row.ContainsKey("Src_IP") && row.ContainsKey("Dst_IP"))
            {
                ipSet.Add(row["Src_IP"].Trim());
                ipSet.Add(row["Dst_IP"].Trim());
            }
        }

        List<string> uniqueIPs = new List<string>(ipSet);
        uniqueIPs.Sort();

        int index = 0;
        foreach (string ip in uniqueIPs)
        {
            int level = index / nodesPerLevel;
            int indexInLevel = index % nodesPerLevel;
            float angle = indexInLevel * Mathf.PI * 2f / nodesPerLevel;

            Vector3 pos = new Vector3(
                Mathf.Cos(angle) * ringRadius,
                level * levelHeight + 0.2f,
                Mathf.Sin(angle) * ringRadius
            ) + treeCenterOffset;

            GameObject node = Instantiate(nodePrefab, pos, Quaternion.identity);
            node.name = ip;
            ipToNode[ip] = node;
            ipToCumulativeBytes[ip] = 0.0;

            var label = node.GetComponentInChildren<TextMeshPro>();
            if (label != null) label.text = ip;

            node.SetActive(false);
            node.AddComponent<AnimatedNodeFlag>();
            index++;
        }

        Debug.Log("Rete inizializzata con " + uniqueIPs.Count + " nodi su livelli verticali.");
    }

    public void VisualizzaReteInTempo(DateTime tempo)
    {
        if (!visualizationEnabled) return;

        foreach (var node in ipToNode.Values)
            node.SetActive(false);

        activeLines.RemoveAll(l =>
        {
            if (tempo < l.startTime || tempo > l.endTime)
            {
                if (l.lineObj != null)
                    Destroy(l.lineObj);
                return true;
            }
            return false;
        });

        double maxBytesObserved = 0;
        int orangeNodeAlerts = 0;

        foreach (var row in loadData.data)
        {
            if (!row.TryGetValue("Timestamp", out string rawTime)) continue;
            if (!DateTime.TryParse(rawTime, out DateTime startTime)) continue;
            if (!row.TryGetValue("Flow_Duration", out string durationStr)) continue;
            if (!double.TryParse(durationStr, out double durationMs)) continue;

            DateTime endTime = startTime.AddMilliseconds(durationMs);

            if (tempo >= startTime && tempo <= endTime)
            {
                string src = row["Src_IP"].Trim();
                string dst = row["Dst_IP"].Trim();
                string label = row.ContainsKey("Label") ? row["Label"] : "";

                double bytes = 0;
                if (row.TryGetValue("Flow_Bytes_s", out string byteStr))
                {
                    double.TryParse(byteStr, NumberStyles.Float, CultureInfo.InvariantCulture, out bytes);
                }

                if (ipToCumulativeBytes.ContainsKey(src)) ipToCumulativeBytes[src] += bytes;
                if (ipToCumulativeBytes.ContainsKey(dst)) ipToCumulativeBytes[dst] += bytes;

                maxBytesObserved = Math.Max(maxBytesObserved, Math.Max(ipToCumulativeBytes[src], ipToCumulativeBytes[dst]));

                if (ipToNode.TryGetValue(src, out GameObject srcNode))
                {
                    if (!srcNode.activeSelf)
                    {
                        srcNode.SetActive(true);
                        if (!srcNode.GetComponent<AnimatedNodeFlag>().hasAnimated)
                        {
                            StartCoroutine(AnimateNodeAppearance(srcNode));
                            srcNode.GetComponent<AnimatedNodeFlag>().hasAnimated = true;
                        }
                    }
                }

                if (ipToNode.TryGetValue(dst, out GameObject dstNode))
                {
                    if (!dstNode.activeSelf)
                    {
                        dstNode.SetActive(true);
                        if (!dstNode.GetComponent<AnimatedNodeFlag>().hasAnimated)
                        {
                            StartCoroutine(AnimateNodeAppearance(dstNode));
                            dstNode.GetComponent<AnimatedNodeFlag>().hasAnimated = true;
                        }
                    }
                }

                bool alreadyExists = activeLines.Exists(l => l.lineObj != null && l.lineObj.name == $"{src}->{dst}");

                if (!alreadyExists && ipToNode.ContainsKey(src) && ipToNode.ContainsKey(dst))
                {
                    GameObject lineObj = Instantiate(linePrefab);
                    lineObj.name = $"{src}->{dst}";
                    LineRenderer lr = lineObj.GetComponent<LineRenderer>();

                    Vector3 start = ipToNode[src].transform.position;
                    Vector3 end = ipToNode[dst].transform.position;

                    lr.positionCount = 2;
                    lr.SetPosition(0, start);
                    lr.SetPosition(1, start);

                    if (!label.Trim().Equals("Benign", StringComparison.OrdinalIgnoreCase))
                    {
                        lr.startColor = Color.red;
                        lr.endColor = Color.red;

                        Renderer srcRend = ipToNode[src].GetComponentInChildren<Renderer>();
                        Renderer dstRend = ipToNode[dst].GetComponentInChildren<Renderer>();

                        if (srcRend != null)
                        {
                            srcRend.material.color = Color.red;
                            AddAudioAndPlay(ipToNode[src]);
                        }
                        if (dstRend != null)
                        {
                            dstRend.material.color = Color.red;
                            AddAudioAndPlay(ipToNode[dst]);
                        }
                    }
                    else
                    {
                        Color orange = new Color(1f, 0.6f, 0f);
                        Color red = Color.red;

                        Renderer srcRend = ipToNode[src].GetComponentInChildren<Renderer>();
                        Renderer dstRend = ipToNode[dst].GetComponentInChildren<Renderer>();

                        if (bytes > dosThreshold)
                        {
                            if (srcRend != null && srcRend.material.color != red)
                                srcRend.material.color = orange;

                            if (dstRend != null && dstRend.material.color != red)
                                dstRend.material.color = orange;

                            orangeNodeAlerts++;
                        }
                    }

                    StartCoroutine(AnimateLineDraw(lr, start, end));

                    BoxCollider collider = lineObj.AddComponent<BoxCollider>();
                    Vector3 midPoint = (start + end) / 2f;
                    lineObj.transform.position = midPoint;
                    Vector3 direction = (end - start).normalized;
                    lineObj.transform.rotation = Quaternion.LookRotation(direction);
                    collider.size = new Vector3(0.02f, 0.02f, Vector3.Distance(start, end));
                    collider.center = Vector3.zero;

                    var interactable = lineObj.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
                    interactable.interactionLayers = InteractionLayerMask.GetMask("Default");
                    interactable.selectEntered.AddListener((args) =>
                    {
                        var info = new Dictionary<string, string>
                        {
                            { "Source IP", src },
                            { "Destination IP", dst },
                            { "Protocol", row.ContainsKey("Protocol") ? row["Protocol"] : "-" },
                            { "Flow Bytes/s", row.ContainsKey("Flow_Bytes_s") ? row["Flow_Bytes_s"] : "-" },
                            { "Duration (ms)", row.ContainsKey("Flow_Duration") ? row["Flow_Duration"] : "-" },
                            { "Packets/s", row.ContainsKey("Flow_Packets/s") ? row["Flow_Packets/s"] : "-" },
                            { "Mean IAT", row.ContainsKey("Flow_IAT_Mean") ? row["Flow_IAT_Mean"] : "-" },
                            { "Down/Up Ratio", row.ContainsKey("Down/Up_Ratio") ? row["Down/Up_Ratio"] : "-" },
                            { "Label", row.ContainsKey("Label") ? row["Label"] : "-" }
                        };

                        var infoPanel = FindAnyObjectByType<ConnectionInfoPanel>();
                        if (infoPanel != null)
                        {
                            infoPanel.ShowInfo(info);
                        }
                    });

                    activeLines.Add(new ActiveLine
                    {
                        lineObj = lineObj,
                        startTime = startTime,
                        endTime = endTime
                    });
                }
            }
        }

        foreach (var kvp in ipToCumulativeBytes)
        {
            if (ipToNode.TryGetValue(kvp.Key, out GameObject node))
            {
                double normalized = maxBytesObserved > 0 ? kvp.Value / maxBytesObserved : 0;
                float scale = Mathf.Lerp(minNodeScale, maxNodeScale, (float)normalized);
                node.transform.localScale = Vector3.one * scale;
            }
        }
    }

    private void AddAudioAndPlay(GameObject node)
    {
        if (alarmClip == null)
        {
            Debug.LogError("Alarm clip is null!");
            return;
        }

        AudioSource source = node.GetComponent<AudioSource>();
        if (source == null)
        {
            source = node.AddComponent<AudioSource>();
            source.clip = alarmClip;
            source.playOnAwake = false;
            source.spatialBlend = 1f;
            source.minDistance = 1f;
            source.maxDistance = 15f;
        }

        Debug.Log($"Playing sound from node {node.name}");
        source.PlayOneShot(alarmClip);
    }

    private IEnumerator AnimateNodeAppearance(GameObject node, float duration = 0.3f)
    {
        Vector3 targetScale = node.transform.localScale;
        node.transform.localScale = Vector3.zero;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            node.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        node.transform.localScale = targetScale;
    }

    private IEnumerator AnimateLineDraw(LineRenderer lr, Vector3 start, Vector3 end, float duration = 0.3f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 current = Vector3.Lerp(start, end, t);
            lr.SetPosition(1, current);
            elapsed += Time.deltaTime;
            yield return null;
        }

        lr.SetPosition(1, end);
    }

    private class AnimatedNodeFlag : MonoBehaviour
    {
        public bool hasAnimated = false;
    }
}
