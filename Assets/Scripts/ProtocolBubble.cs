using UnityEngine;
using TMPro;

public class ProtocolBubble : MonoBehaviour
{
    public TextMeshPro textMesh;

    void Awake()
    {
        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>();

        if (textMesh == null)
            Debug.LogWarning("TextMeshPro component not found in children of ProtocolBubble.");
    }

    public void SetInfo(string protocol, int count, float percentage)
    {
        if (textMesh != null)
        {
            textMesh.text = $"{protocol}\n{count} flows\n{(percentage * 100f):F1}%";
        }
        else
        {
            Debug.LogWarning("TextMeshPro component not assigned in ProtocolBubble.");
        }
    }
}
