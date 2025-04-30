using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class QueryVisualizer : MonoBehaviour
{
    public LoadData loadData;
    public GameObject barPrefab;
    public float barWidth = 0.02f;
    public float spacing = 0.03f;
    public Transform anchor;
    public Material barMaterial;
    public InputActionProperty hideGraphAction;

    public Transform fixedPosition;

    private TextMeshPro sharedLabel;

    public int axisFontSize = 1;
    public float axisLabelOffset = 0f;

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

        if (anchor == null)
        {
            GameObject anchorGO = new GameObject("HistogramAnchor");
            anchor = anchorGO.transform;
        }

        if (fixedPosition != null)
        {
            anchor.position = fixedPosition.position;
            anchor.rotation = fixedPosition.rotation;
        }
        else
        {
            Transform cam = Camera.main.transform;
            Vector3 forward = new Vector3(cam.forward.x, 0, cam.forward.z).normalized;
            anchor.position = cam.position + forward * 1.2f + Vector3.down * 0.3f + -cam.right * 0.3f;
            anchor.rotation = Quaternion.LookRotation(forward);
        }

        anchor.localScale = Vector3.one * 2f;

        foreach (Transform child in anchor)
        {
            Destroy(child.gameObject);
        }

        // Rende il grafico trascinabile anche con grip
        var rb = anchor.gameObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = anchor.gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        var grab = anchor.gameObject.GetComponent<XRGrabInteractable>();
        if (grab == null)
        {
            grab = anchor.gameObject.AddComponent<XRGrabInteractable>();
            grab.movementType = XRBaseInteractable.MovementType.VelocityTracking;
            grab.trackRotation = false;
        }

        var collider = anchor.gameObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = anchor.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.5f, 0f);
            collider.size = new Vector3(1f, 1f, 1f);
        }

        // Supporto a Select e Activate
        grab.interactionManager = FindAnyObjectByType<XRInteractionManager>();
        grab.interactionLayers = InteractionLayerMask.GetMask("Everything");
        grab.useDynamicAttach = true;

        // Debug: afferrato/rilasciato
        grab.selectEntered.AddListener((args) =>
        {
            Debug.Log(" Grafico AFFERRATO da: " + args.interactorObject.transform.name);
        });

        grab.selectExited.AddListener((args) =>
        {
            Debug.Log(" Grafico RILASCIATO da: " + args.interactorObject.transform.name);
        });

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
        {
            if (value > maxValue)
                maxValue = value;
        }

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

    private void CreateXAxisLabel()
    {
        GameObject xAxisLabel = new GameObject("XAxisLabel");
        xAxisLabel.transform.SetParent(anchor);
        TextMeshPro tmp = xAxisLabel.AddComponent<TextMeshPro>();
        tmp.text = "IP nel Tempo";
        tmp.fontSize = axisFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        xAxisLabel.transform.localPosition = new Vector3(0f, -axisLabelOffset, 0f);
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
        yAxisLabel.transform.localPosition = new Vector3(-axisLabelOffset, 0f, 0f);
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
            Vector3 newPos = new Vector3(barPosition.x, anchor.position.y + 0.5f, barPosition.z);
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
        if (anchor == null) return;
        StartCoroutine(HideHistogramRoutine());
    }

    private IEnumerator HideHistogramRoutine()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 originalScale = anchor.localScale;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            anchor.localScale = Vector3.Lerp(originalScale, Vector3.zero, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        anchor.localScale = Vector3.zero;

        foreach (Transform child in anchor)
        {
            Destroy(child.gameObject);
        }

        Debug.Log("Istogramma nascosto.");
    }
}
