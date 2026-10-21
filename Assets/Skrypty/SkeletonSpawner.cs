using System.Collections;
using UnityEngine;

public class SkeletonSpawner : MonoBehaviour
{
    public GameObject skeletonPrefab;
    public Collider rockCollider;

    public int enemiesPerWave = 10;
    public float interval = 5f;
    public float spawnOffsetY = 0f;

    [Range(0f, 1f)]
    public float minimumHeight = 0.75f;

    IEnumerator Start()
    {
        if (skeletonPrefab == null || rockCollider == null)
        {
            Debug.LogError("Przypisz prefab szkieleta i collider skały.", this);
            yield break;
        }

        while (true)
        {
            yield return new WaitForSeconds(interval);

            int spawned = 0;

            // Próby ograniczone, bo nie każdy punkt trafi w skałę.
            for (int attempt = 0;
                 attempt < enemiesPerWave * 30 && spawned < enemiesPerWave;
                 attempt++)
            {
                Bounds bounds = rockCollider.bounds;

                Vector3 origin = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    bounds.max.y + 2f,
                    Random.Range(bounds.min.z, bounds.max.z)
                );

                Ray ray = new Ray(origin, Vector3.down);

                if (!rockCollider.Raycast(
                    ray, out RaycastHit hit, bounds.size.y + 4f))
                    continue;

                // Dopuszczamy tylko górną część skały.
                float minimumY = Mathf.Lerp(
                    bounds.min.y, bounds.max.y, minimumHeight);

                if (hit.point.y < minimumY)
                    continue;

                // Pomijamy zbyt strome powierzchnie.
                if (Vector3.Angle(hit.normal, Vector3.up) > 45f)
                    continue;

                Vector3 position =
                    hit.point + Vector3.up * spawnOffsetY;

                Quaternion rotation = Quaternion.Euler(
                    0f, Random.Range(0f, 360f), 0f);

                Instantiate(skeletonPrefab, position, rotation);
                spawned++;
            }
        }
    }
}