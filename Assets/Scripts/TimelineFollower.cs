using UnityEngine;

public class TimelineFollower : MonoBehaviour
{
    public Transform head;
    public Vector3 offset = new Vector3(0.3f, 0.3f, 1f);

    void LateUpdate()
    {
        if (head == null) return;

        Vector3 forward = head.forward;
        forward.y = 0;
        forward.Normalize();

        transform.position = head.position + head.TransformVector(offset);
        transform.rotation = Quaternion.LookRotation(forward);
    }
}
