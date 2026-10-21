using UnityEngine;
using UnityEngine.InputSystem;

public class AirBookPickup : MonoBehaviour
{
    public PlayerInventory playerInventory;
    public float pickupDistance = 2.5f;

    void Start()
    {
        if (playerInventory == null)
            playerInventory =
                Object.FindAnyObjectByType<PlayerInventory>();
    }

    void Update()
    {
        if (playerInventory == null || Keyboard.current == null)
            return;

        if (!Keyboard.current.eKey.wasPressedThisFrame)
            return;

        float distance = Vector3.Distance(
            transform.position,
            playerInventory.transform.position);

        if (distance > pickupDistance)
            return;

        if (playerInventory.AddAirBook())
            Destroy(gameObject);
    }
}