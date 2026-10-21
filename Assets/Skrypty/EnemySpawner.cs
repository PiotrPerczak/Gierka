using System.Collections;
using UnityEngine;

public class BurrowSpawn : MonoBehaviour
{
    public float riseDuration = 1.5f; // czas wychodzenia spod ziemi
    public float spawnDepth = 4.0f;    // jak głęboko pod ziemią startuje
    public LayerMask groundLayer;      // warstwa góry (Ground)

    private EnemyController enemyController;
    private CharacterController characterController;

    void Start()
    {
        enemyController = GetComponent<EnemyController>();
        characterController = GetComponent<CharacterController>();

        if (enemyController != null) enemyController.enabled = false;
        if (characterController != null) characterController.enabled = false;

        StartCoroutine(EmergeRoutine());
    }

    private IEnumerator EmergeRoutine()
    {
        Vector3 finalPos = transform.position;

        int layerMask = groundLayer.value != 0 ? groundLayer.value : (1 << LayerMask.NameToLayer("Ground"));

        // Szukamy powierzchni góry:
        if (Physics.Raycast(transform.position + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 30f, layerMask, QueryTriggerInteraction.Ignore))
        {
            // +1.3f unosi środek klatki piersiowej, stawiając stopy idealnie na skale:
            finalPos.y = hit.point.y + 1.3f;
        }

        Vector3 startPos = finalPos - Vector3.up * spawnDepth;
        transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, finalPos, elapsed / riseDuration);
            yield return null;
        }

        transform.position = finalPos;

        if (characterController != null) characterController.enabled = true;
        if (enemyController != null) enemyController.enabled = true;
    }
}