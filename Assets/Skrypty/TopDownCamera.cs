using UnityEngine;

public class TopDownCamera : MonoBehaviour
{
    public Transform target; // Przeciągnij tutaj swoją postać
    public Vector3 offset = new Vector3(0, 10, -5); // Wysokość i oddalenie
    public float smoothSpeed = 5f;

    void LateUpdate()
    {
        if (target == null) return;

        // Płynne podążanie za graczem
        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);

        // Kamera zawsze patrzy na gracza
        transform.LookAt(target.position + Vector3.up * 1.5f); 
    }
}