using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ToggleTimelineVisibility : MonoBehaviour
{
    public GameObject timelineCanvas;
    private bool isVisible = false;

    void Update()
    {
        foreach (var device in InputSystem.devices)
        {
            // Verifica che sia un controller sinistro
            if (device is InputDevice && device.name.ToLower().Contains("left"))
            {
                // Cerca il pulsante Y (secondaryButton)
                if (device.TryGetChildControl<ButtonControl>("secondaryButton") is ButtonControl yButton &&
                    yButton.wasPressedThisFrame)
                {
                    ToggleTimeline();
                    break;
                }
            }
        }
    }

    private void ToggleTimeline()
    {
        isVisible = !isVisible;
        if (timelineCanvas != null)
            timelineCanvas.SetActive(isVisible);
    }
}
