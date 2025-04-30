using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class VisualizeNetwork : MonoBehaviour
{
    public LoadData loadData;
    public GameObject nodePrefab;
    public GameObject linePrefab;

    public int nodesPerLevel = 7;
    public float levelHeight = 1f;
    public float ringRadius = 3f;

    private Dictionary<string, GameObject> ipToNode = new Dictionary<string, GameObject>();

    private class ActiveLine
    {
        public GameObject lineObj;
        public DateTime startTime;
        public DateTime endTime;
    }

    private List<ActiveLine> activeLines = new List<ActiveLine>();

    void Start()
    {
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
            );

            GameObject node = Instantiate(nodePrefab, pos, Quaternion.identity);
            node.name = ip;
            ipToNode[ip] = node;

            var label = node.GetComponentInChildren<TextMeshPro>();
            if (label != null) label.text = ip;

            node.SetActive(false);
            node.AddComponent<AnimatedNodeFlag>(); // serve a evitare rianimazioni
            index++;
        }

        Debug.Log("Rete inizializzata con " + uniqueIPs.Count + " nodi su livelli verticali.");
    }

    public void VisualizzaReteInTempo(DateTime tempo)
    {
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

                bool alreadyExists = activeLines.Exists(l =>
                    l.lineObj != null && l.lineObj.name == $"{src}->{dst}");

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

                    StartCoroutine(AnimateLineDraw(lr, start, end));

                    activeLines.Add(new ActiveLine
                    {
                        lineObj = lineObj,
                        startTime = startTime,
                        endTime = endTime
                    });
                }
            }
        }
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

    // Helper class per nodo animato
    private class AnimatedNodeFlag : MonoBehaviour
    {
        public bool hasAnimated = false;
    }
}