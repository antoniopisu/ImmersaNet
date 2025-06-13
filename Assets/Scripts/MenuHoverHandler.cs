using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string description;
    private TextMeshProUGUI descriptionText;

    public void Initialize(string desc, TextMeshProUGUI descBox)
    {
        description = desc;
        descriptionText = descBox;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (descriptionText != null)
            descriptionText.text = description;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (descriptionText != null)
            descriptionText.text = "";
    }
}
