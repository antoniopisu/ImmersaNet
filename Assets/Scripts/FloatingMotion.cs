using UnityEngine;

public class FloatingMotion : MonoBehaviour
{
    public float amplitude = 0.02f;  // ampiezza oscillazione verticale
    public float frequency = 1.5f;   // velocità oscillazione

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        float offsetY = Mathf.Sin(Time.time * frequency + GetHashCode()) * amplitude;
        transform.localPosition = startPos + new Vector3(0f, offsetY, 0f);
    }
}
