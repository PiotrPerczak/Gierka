using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public bool HasAirBook { get; private set; }

    public bool AddAirBook()
    {
        if (HasAirBook)
            return false;

        HasAirBook = true;
        return true;
    }
}