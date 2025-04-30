using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class DraggableObject : MonoBehaviour
{
    public InputActionProperty grabAction;
    public Transform rightHandTransform;

    private bool isGrabbed = false;
    private Vector3 grabOffset;

    void Update()
    {
        if (grabAction.action.WasPressedThisFrame())
        {
            TryGrab();
        }
        else if (grabAction.action.WasReleasedThisFrame())
        {
            Release();
        }

        if (isGrabbed && rightHandTransform != null)
        {
            transform.position = rightHandTransform.position + grabOffset;
            transform.rotation = rightHandTransform.rotation;
        }
    }

    private void TryGrab()
    {
        if (Vector3.Distance(transform.position, rightHandTransform.position) < 0.3f) // distanza massima per poterlo afferrare
        {
            isGrabbed = true;
            grabOffset = transform.position - rightHandTransform.position;
        }
    }

    private void Release()
    {
        isGrabbed = false;
    }
}
