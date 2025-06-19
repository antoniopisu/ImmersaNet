using UnityEngine;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRBaseInteractable))]
public class ShowLabelOnHover : MonoBehaviour
{
    public string ipText;
    public float byteValue;
    public QueryVisualizer visualizer;

    public bool isQuery2 = false;

    public Material defaultMaterial;  // Materiale normale (assegnabile nel prefab o rilevato dinamicamente)
    public Material hoverMaterial;    // Materiale quando il controller passa sopra

    private Renderer barRenderer;

    void Awake()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener((args) => OnHoverEnter());
            interactable.hoverExited.AddListener((args) => OnHoverExit());
        }

        // Cerca il Renderer sia su sé stesso che nei figli (anche disattivati)
        barRenderer = GetComponent<Renderer>();
        if (barRenderer == null)
        {
            barRenderer = GetComponentInChildren<Renderer>(true); // include figli disattivati
            if (barRenderer != null)
                Debug.Log($"[ShowLabelOnHover] Renderer found on child: {barRenderer.gameObject.name}");
            else
                Debug.LogWarning($"[ShowLabelOnHover] Renderer not found in {gameObject.name} or its children");
        }
        else
        {
            Debug.Log($"[ShowLabelOnHover] Renderer found on self: {barRenderer.gameObject.name}");
        }

        // Imposta materiale di default se non già assegnato
        if (barRenderer != null && defaultMaterial == null)
            defaultMaterial = barRenderer.material;
    }

    public void OnHoverEnter()
    {
        if (visualizer != null)
        {
            string formattedBytes = FormatBytes(byteValue);
            string content = $"<b>Ip:</b> {ipText}\n<b>Data:</b> {formattedBytes}";

            if (isQuery2)
                visualizer.UpdateSharedLabelQ2(content, transform.position);
            else
                visualizer.UpdateSharedLabel(content, transform.position);
        }

        if (barRenderer != null && hoverMaterial != null)
        {
            barRenderer.material = hoverMaterial;
            Debug.Log($"[HoverEnter] Changed material to {hoverMaterial.name}");
        }
    }

    public void OnHoverExit()
    {
        if (visualizer != null)
        {
            if (isQuery2)
                visualizer.HideSharedLabelQ2();
            else
                visualizer.HideSharedLabel();
        }

        if (barRenderer != null && defaultMaterial != null)
        {
            barRenderer.material = defaultMaterial;
            Debug.Log($"[HoverExit] Restored material to {defaultMaterial.name}");
        }
    }

    private string FormatBytes(float bytes)
    {
        float tb = bytes / 1_000_000_000_000f;
        if (tb >= 1f) return $"{tb:F2} TB";

        float gb = bytes / 1_000_000_000f;
        if (gb >= 1f) return $"{gb:F2} GB";

        float mb = bytes / 1_000_000f;
        if (mb >= 1f) return $"{mb:F2} MB";

        float kb = bytes / 1_000f;
        if (kb >= 1f) return $"{kb:F2} KB";

        return $"{bytes:F2} B";
    }
}
