using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SpellProjectile : MonoBehaviour
{
    public float speed = 15f;
    public float lifetime = 4f;
    public float damage = 25f;
    [Min(0f)] public float impactMultiplier = 1f;
    [Min(.1f)] public float bounceRange = 8f;
    public PlayerStats ownerStats;
    public GameObject explosionPrefab;
    bool consumed, damageAlreadyScaled;
    int remainingBounces;
    EnemyController nextTarget;
    CharacterPanelUI panel;
    readonly HashSet<EnemyController> hit = new();
    public float CurrentSpeed => Mathf.Max(.1f, speed) *
        (ownerStats ? Mathf.Max(.01f, ownerStats.projectileSpeedMultiplier) : 1f);

    void Awake()
    {
        if (!ownerStats) ownerStats = Object.FindAnyObjectByType<PlayerStats>();
        panel = Object.FindAnyObjectByType<CharacterPanelUI>();
    }

    void Start()
    {
        remainingBounces = ownerStats ? Mathf.Clamp(ownerStats.projectileBounces, 0, 32) : 0;
        if (ownerStats) transform.localScale *= Mathf.Max(.01f, ownerStats.sizeMultiplier);
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (consumed || (panel && panel.panel && panel.panel.activeInHierarchy)) return;
        if (nextTarget && nextTarget.currentHealth > 0f)
        {
            Vector3 delta = nextTarget.transform.position + Vector3.up - transform.position;
            if (delta.sqrMagnitude > .0001f) transform.rotation = Quaternion.LookRotation(delta);
        }
        transform.position += transform.forward * CurrentSpeed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (consumed) return;
        var enemy = other.GetComponentInParent<EnemyController>();
        if (!enemy || enemy.currentHealth <= 0f || !hit.Add(enemy)) return;
        Vector3 point = other.ClosestPoint(transform.position);
        if (explosionPrefab)
        {
            var effect = Instantiate(explosionPrefab, point, Quaternion.identity);
            if (ownerStats) effect.transform.localScale *= Mathf.Max(.01f, ownerStats.sizeMultiplier);
            Destroy(effect, 3f);
        }
        if (ownerStats) ownerStats.DealSpellDamage(enemy, damage, point, transform.forward,
            damageAlreadyScaled, impactMultiplier);
        else enemy.TakeDamage(damage, point, transform.forward, damage * impactMultiplier);
        if (remainingBounces > 0)
        {
            float best = bounceRange * bounceRange;
            nextTarget = null;
            foreach (var target in Object.FindObjectsByType<EnemyController>())
            {
                if (hit.Contains(target) || !target.isActiveAndEnabled || target.currentHealth <= 0f) continue;
                float distance = (target.transform.position - transform.position).sqrMagnitude;
                if (distance < best) { best = distance; nextTarget = target; }
            }
            if (nextTarget)
            {
                remainingBounces--;
                transform.rotation = Quaternion.LookRotation(nextTarget.transform.position + Vector3.up - transform.position);
                return;
            }
        }
        consumed = true;
        Destroy(gameObject);
    }

    public void SetDamage(float calculatedDamage)
    {
        damage = calculatedDamage;
        damageAlreadyScaled = true; // Legacy callers already calculated the damage multiplier.
    }
}
