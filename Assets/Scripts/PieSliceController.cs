using UnityEngine;
using TMPro;

public class PieSliceController : MonoBehaviour
{
    public string label;
    public int count;
    public float percentage;

    public TextMeshPro labelText;

    public void SetSlice(string label, int count, float percentage)
    {
        this.label = label;
        this.count = count;
        this.percentage = percentage;

        // Aggiorna il testo se esiste
        if (labelText != null)
        {
            labelText.text = $"{label}\n{count} flows\n{(percentage * 100f):F1}%";
        }
    }

    void OnMouseEnter()
    {
        if (labelText != null)
            labelText.gameObject.SetActive(true);
    }

    void OnMouseExit()
    {
        if (labelText != null)
            labelText.gameObject.SetActive(false);
    }
}
