using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class ToggleTimelineVisibility : MonoBehaviour
{
    public GameObject timelineCanvas;
    private bool isVisible = false;
    private bool hasInitialized = false;

    void Awake()
    {
        // Nasconde il canvas appena viene caricato, prima ancora che Start venga eseguito, utile mettere false se non si ha il visore
        if (timelineCanvas != null)
        {
            timelineCanvas.SetActive(false);
        }
    }

    void Update()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device is InputDevice && device.name.ToLower().Contains("left"))
            {
                if (device.TryGetChildControl<ButtonControl>("secondaryButton") is ButtonControl xButton &&
                    xButton.wasPressedThisFrame)
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
        {
            timelineCanvas.SetActive(isVisible);

            // Inizializza TimelineManager solo la prima volta che viene mostrato
            if (isVisible && !hasInitialized)
            {
                var manager = timelineCanvas.GetComponent<TimelineManager>();
                if (manager != null)
                    manager.enabled = true;

                hasInitialized = true;
            }
        }
    }
}
