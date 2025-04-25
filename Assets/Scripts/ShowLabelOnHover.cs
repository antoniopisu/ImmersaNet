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

    void Awake()
    {
        var interactable = GetComponent<XRBaseInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener((args) => OnHoverEnter());
            interactable.hoverExited.AddListener((args) => OnHoverExit());
        }
    }

    public void OnHoverEnter()
    {
        if (visualizer != null)
        {
            float tb = byteValue / 1_000_000_000_000f;
            string content = $"{ipText}\n{tb:F2} TB";
            visualizer.UpdateSharedLabel(content, transform.position);
        }
    }

    public void OnHoverExit()
    {
        visualizer?.HideSharedLabel();
    }
}
