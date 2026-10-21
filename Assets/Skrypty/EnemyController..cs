using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class EnemyController : MonoBehaviour
{
    [Header("Odpychanie")]
    public float knockbackSpeed = 3f;
    public float knockbackDuration = 0.12f;

    private Vector3 knockbackDirection;
    private float knockbackRemaining;
    private float currentKnockbackScale = 1f;

    [Header("Statystyki")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 2.5f;
    public float stopDistance = 1.8f; // Odległość, na jaką zbliża się do gracza
    private bool isDead;
    private Vector3 fatalHitPoint, fatalHitDirection;
    private float fatalHitStrength;

    [Header("UI")]
    public Slider healthSlider;
    public Transform healthBarCanvas;

    private CharacterController controller;
    private Transform player;
    private Camera mainCamera;
    private EnemyHitFeedback hitFeedback;

    void Awake()
    {
        hitFeedback = GetComponent<EnemyHitFeedback>();
        if (hitFeedback == null) hitFeedback = gameObject.AddComponent<EnemyHitFeedback>();
    }

    void Start()
    {
        controller = GetComponent<CharacterController>();
        mainCamera = Camera.main;
        currentHealth = maxHealth;

        // Szukamy gracza w scenie
        TopDownPlayerController playerCtrl = Object.FindAnyObjectByType<TopDownPlayerController>();
        if (playerCtrl != null)
        {
            player = playerCtrl.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindWithTag("Player") ?? GameObject.Find("Player") ?? GameObject.Find("URP - Giant01 - Golem");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
        if (healthBarCanvas != null)
            healthBarCanvas.gameObject.SetActive(false);
        else if (healthSlider != null)
            healthSlider.gameObject.SetActive(false);
    }

    void Update()
    {
        if (knockbackRemaining > 0f)
        {
            float strength = Mathf.Clamp01(
                knockbackRemaining / Mathf.Max(0.01f, knockbackDuration));

            controller.SimpleMove(
                knockbackDirection * knockbackSpeed * currentKnockbackScale * strength);

            knockbackRemaining -= Time.deltaTime;
        }
        else
        {
            FollowPlayer();
        }

        RotateHealthBarToCamera();
    }

    void FollowPlayer()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0; // Poruszamy się tylko po płaszczyźnie

        float distance = toPlayer.magnitude;

        if (distance > stopDistance)
        {
            // Obrót ku graczowi
            Quaternion targetRot = Quaternion.LookRotation(toPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);

            // Ruch do przodu
            Vector3 move = toPlayer.normalized * moveSpeed;
            controller.SimpleMove(move);
        }
    }

    void RotateHealthBarToCamera()
    {
        // Sprawia, że pasek zdrowia zawsze jest zwrócony idealnie w stronę kamery
        if (healthBarCanvas != null && mainCamera != null)
        {
            healthBarCanvas.rotation = mainCamera.transform.rotation;
        }
    }

    public void TakeDamage(float damage)
    {
        // Directionless damage has no invented impulse from the player position.
        TakeDamage(damage, transform.position + Vector3.up, Vector3.zero, 0f);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 incomingDirection, float impactStrength)
    {
        if (isDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage))
            return;
        fatalHitPoint = hitPoint;
        fatalHitDirection = incomingDirection;
        fatalHitStrength = Mathf.Max(0f, impactStrength);
        float healthBefore = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        float healthLost = Mathf.Max(0f, healthBefore - currentHealth);
        if (healthLost > 0f && hitFeedback != null) hitFeedback.Play(healthLost);
        if (healthLost > 0f && currentHealth > 0f)
        {
            Vector3 direction = incomingDirection;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                currentKnockbackScale = damage > 0f ? Mathf.Clamp(impactStrength / damage, 0f, 10f) : 0f;
                knockbackDirection = direction.normalized;
                knockbackRemaining = knockbackDuration;
            }
        }
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        PlayerStats stats = player != null
            ? player.GetComponent<PlayerStats>()
            : null;

        if (stats == null)
            stats = Object.FindAnyObjectByType<PlayerStats>();

        if (stats != null)
        {
            stats.kills++;
            stats.AddExp(100f);
        }

        var breakup = GetComponent<DirectionalSkeletonDeath>();
        if (breakup != null) breakup.Break(fatalHitPoint, fatalHitDirection, fatalHitStrength);
        // Remove gameplay immediately; the pooled corpse lives independently.
        enabled = false;
        foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
        if (healthBarCanvas != null) healthBarCanvas.gameObject.SetActive(false);
        Destroy(gameObject, hitFeedback != null ? Mathf.Max(0.02f, hitFeedback.flashDuration) : 0.12f);
    }
}


