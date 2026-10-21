using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StormSpell : MonoBehaviour
{
    [Header("Gracz i menu")]
    public PlayerInventory inventory;
    public PlayerStats stats;
    public CharacterPanelUI characterPanel;
    public CameraViewTransition cameraRig;
    public bool requireAirBook = false;

    [Header("Efekt i dzwiek glownego uderzenia")]
    public GameObject lightningPrefab;
    public AudioSource audioSource;
    public AudioClip lightningSound;

    [Header("Automatyczny piorun")]
    public float range = 20f;
    public float cooldown = 1f;
    public float baseDamage = 25f;
    [Min(0f)] public float manaCost = 3f;
    [Min(0)] public int baseChains = 0;
    [Min(.1f)] public float chainRange = 12f;
    public float CurrentSize => stats ? Mathf.Max(.01f, stats.sizeMultiplier) : 1f;
    public int CurrentChainCount => Mathf.Max(0, baseChains) + (stats ? Mathf.Max(0, stats.lightningChains) : 0);
    public float CurrentRange => Mathf.Max(0f, range) * CurrentSize;
    public float strikeDelay = 0.1f;
    public float effectLifetime = 4f;
    public float effectOffsetY = 0f;

    [Header("Przeskoki")]
    public Material chainMaterial;
    public float chainDelay = 0.12f;
    public float chainLifetime = 0.22f;
    public float chainWidth = 0.055f;
    public Color chainColor = new Color(0.5f, 0.85f, 1f, 1f);
    public float chainHeight = 1.1f;

    float charge = 1f;
    float nextSearchTime;
    bool Paused => (characterPanel != null && characterPanel.panel != null && characterPanel.panel.activeInHierarchy)
        || (cameraRig != null && cameraRig.IsTransitioning);

    void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (characterPanel == null) characterPanel = Object.FindAnyObjectByType<CharacterPanelUI>();
        if (cameraRig == null && Camera.main != null) cameraRig = Camera.main.GetComponent<CameraViewTransition>();
    }

    public float CurrentInterval => Mathf.Max(0.08f, cooldown / (stats != null ? stats.AttackSpeedMultiplier : 1f));

    void Update()
    {
        if ((stats && !stats.IsAlive) || Time.timeScale <= 0f || Paused || (requireAirBook && (inventory == null || !inventory.HasAirBook))) return;
        charge = Mathf.Min(1f, charge + Time.deltaTime / CurrentInterval);
        if (charge < 1f || lightningPrefab == null || Time.time < nextSearchTime) return;
        var enemies = Object.FindObjectsByType<EnemyController>();
        EnemyController nearest = null;
        float nearestDistance = CurrentRange * CurrentRange;
        foreach (var enemy in enemies)
        {
            if (!Alive(enemy)) continue;
            float distance = (enemy.transform.position - transform.position).sqrMagnitude;
            if (distance <= nearestDistance) { nearestDistance = distance; nearest = enemy; }
        }
        if (nearest == null) { nextSearchTime = Time.time + 0.1f; return; }
        if (stats && !stats.TrySpendMana(Mathf.Max(0f, manaCost))) { nextSearchTime = Time.time + .1f; return; }
        charge = 0f;
        float damage = Mathf.Max(0f, baseDamage);
        int jumps = CurrentChainCount;
        StartCoroutine(Cast(nearest, enemies, damage, jumps));
    }

    static bool Alive(EnemyController enemy) => enemy != null && enemy.isActiveAndEnabled && enemy.currentHealth > 0f;

    IEnumerator Delay(float duration)
    {
        float elapsed = 0f;
        while (elapsed < Mathf.Max(0f, duration))
        {
            yield return null;
            if (!Paused) elapsed += Time.deltaTime;
        }
        while (Paused) yield return null;
    }

    IEnumerator Cast(EnemyController target, EnemyController[] snapshot, float damage, int jumps)
    {
        Vector3 castOrigin = transform.position;
        yield return Delay(strikeDelay);
        if (!Alive(target)) yield break;
        var hit = new HashSet<EnemyController> { target };
        Vector3 previous = target.transform.position + Vector3.up * chainHeight;
        var effect = Instantiate(lightningPrefab, target.transform.position + Vector3.up * effectOffsetY, Quaternion.identity);
        effect.transform.localScale *= CurrentSize;
        Destroy(effect, Mathf.Max(0.05f, effectLifetime));
        if (audioSource != null && lightningSound != null) audioSource.PlayOneShot(lightningSound);
        DealHit(target, damage, previous, target.transform.position - castOrigin);

        var candidates = new List<EnemyController>(snapshot.Length);
        for (int jump = 0; jump < jumps; jump++)
        {
            yield return Delay(chainDelay);
            candidates.Clear();
            foreach (var enemy in snapshot)
                if (Alive(enemy) && !hit.Contains(enemy) && (enemy.transform.position - previous).sqrMagnitude <= chainRange * chainRange * CurrentSize * CurrentSize) candidates.Add(enemy);
            if (candidates.Count == 0) break;
            var next = candidates[Random.Range(0, candidates.Count)];
            Vector3 nextPosition = next.transform.position + Vector3.up * chainHeight;
            if (chainMaterial != null)
                LightningChainVisual.Spawn(previous, nextPosition, chainMaterial, chainColor, chainWidth * CurrentSize, chainLifetime);
            hit.Add(next);
            DealHit(next, damage * .5f, nextPosition, nextPosition - previous);
            previous = nextPosition;
        }
    }

    void DealHit(EnemyController target, float baseHitDamage, Vector3 point, Vector3 direction)
    {
        if (stats) stats.DealSpellDamage(target, baseHitDamage, point, direction);
        else if (target) target.TakeDamage(baseHitDamage, point, direction, baseHitDamage);
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }
}



