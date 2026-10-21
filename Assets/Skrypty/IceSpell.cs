using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceSpell : MonoBehaviour
{
    [Header("References")]
    public PlayerStats stats;
    public Transform firePoint;
    [Header("Pozycja wylotu")]
    public float spawnHeight = 0.7f;

    [Min(0f)]
    public float spawnMinOffset = 0.08f;

    [Min(0f)]
    public float spawnMaxOffset = 0.15f;
    public CharacterPanelUI characterPanel;
    public CameraViewTransition cameraRig;
    public GameObject projectilePrefab;
    public GameObject impactPrefab;

    [Header("Attack")]
    public float range = 20f;
    public float cooldown = 1f;
    public float baseDamage = 25f;
    [Min(0f)] public float manaCost = 3f;
    [Min(0f)] public float impactRadius = .65f;
    public float CurrentSize => stats ? Mathf.Max(.01f, stats.sizeMultiplier) : 1f;
    public float CurrentProjectileSpeed => Mathf.Max(.1f, projectileSpeed) * (stats ? Mathf.Max(.01f, stats.projectileSpeedMultiplier) : 1f);
    public float CurrentImpactRadius => Mathf.Max(0f, impactRadius) * CurrentSize;
    public int CurrentBounces => stats ? Mathf.Clamp(stats.projectileBounces, 0, 32) : 0;
    public int CurrentProjectileCount => stats ? Mathf.Clamp(1 + stats.extraProjectileCount, 1, 32) : 1;
    readonly Collider[] splashBuffer = new Collider[128];
    readonly HashSet<EnemyController> splashTargets = new HashSet<EnemyController>();
    public float projectileSpeed = 25f;
    public float targetHeight = 1.1f;
    public float maxFlightTime = 1.5f;
    public float mainScale = 1f;

    [Header("Multishot trajectories")]
    [Min(0f)] public float volleySpread = 1.4f;
    [Min(0f)] public float volleyArcHeight = .45f;

    [Header("Chains")]
    public float chainRange = 8f;
    [Range(0.1f, 1f)] public float chainScale = 0.8f;
    [Range(0f, 1f)] public float chainDamageMultiplier = 0.5f;

    [Header("Sound")]
    public AudioClip[] castSounds;
    public AudioClip[] impactSounds;
    [Range(0f, 1f)] public float castVolume = 0.35f;
    [Range(0f, 1f)] public float impactVolume = 0.45f;
    [Range(0f, 1f)] public float spatialBlend = 0.5f;

    readonly HashSet<GameObject> flying = new HashSet<GameObject>();
    float charge = 1f;
    float nextSearch;

    bool Paused => Time.timeScale <= 0f
        || (characterPanel != null && characterPanel.panel != null
            && characterPanel.panel.activeInHierarchy)
        || (cameraRig != null && cameraRig.IsTransitioning);

    public float CurrentInterval => Mathf.Max(0.08f,
        cooldown / (stats != null ? stats.AttackSpeedMultiplier : 1f));

    void Awake()
    {
        if (stats == null) stats = GetComponent<PlayerStats>();
        if (characterPanel == null)
            characterPanel = Object.FindAnyObjectByType<CharacterPanelUI>();
        if (cameraRig == null && Camera.main != null)
            cameraRig = Camera.main.GetComponent<CameraViewTransition>();
    }

    void Update()
    {
        if ((stats && !stats.IsAlive) || Paused || projectilePrefab == null || impactPrefab == null) return;
        charge = Mathf.Min(1f, charge + Time.deltaTime / CurrentInterval);
        if (charge < 1f || Time.time < nextSearch) return;
        var candidates = Object.FindObjectsByType<EnemyController>();
        EnemyController target = Nearest(transform.position, range, null, candidates);
        if (target == null) { nextSearch = Time.time + 0.1f; return; }
        if (stats && !stats.TrySpendMana(Mathf.Max(0f, manaCost))) { nextSearch = Time.time + .1f; return; }
        charge = 0f;
        float damage = Mathf.Max(0f, baseDamage);
        int chains = CurrentBounces;
        float angle = Random.Range(0f, Mathf.PI * 2f);

        float minOffset = Mathf.Max(0f, spawnMinOffset);
        float maxOffset = Mathf.Max(minOffset, spawnMaxOffset);

        float distance = Random.Range(minOffset, maxOffset);

        Vector3 offset = new Vector3(
            Mathf.Cos(angle),
            0f,
            Mathf.Sin(angle)
        ) * distance;

        Vector3 origin = transform.position
            + Vector3.up * spawnHeight
            + offset;
        var initialTargets = new HashSet<EnemyController>();
        for (int i = 0; i < CurrentProjectileCount; i++)
        {
            var chosen = Nearest(transform.position, range, initialTargets, candidates) ?? target;
            initialTargets.Add(chosen);
            Vector3 shotOrigin = origin + transform.right * ((i - (CurrentProjectileCount - 1) * .5f) * .06f);
            StartCoroutine(Cast(shotOrigin, chosen, damage, chains, i, CurrentProjectileCount));
        }
    }

    static bool Alive(EnemyController e) => e != null
        && e.isActiveAndEnabled && e.currentHealth > 0f;

    static EnemyController Nearest(Vector3 origin, float radius,
        HashSet<EnemyController> visited, EnemyController[] candidates = null)
    {
        EnemyController nearest = null;
        float best = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
        foreach (var e in candidates ?? Object.FindObjectsByType<EnemyController>())
        {
            if (!Alive(e) || (visited != null && visited.Contains(e))) continue;
            // Horizontal distance: vertical aim offset must not affect chain range.
            Vector3 delta = e.transform.position - origin;
            delta.y = 0f;
            float distance = delta.sqrMagnitude;
            if (distance < best) { best = distance; nearest = e; }
        }
        return nearest;
    }

    IEnumerator Cast(Vector3 origin, EnemyController target, float damage, int chains, int lane = 0, int laneCount = 1)
    {
        var visited = new HashSet<EnemyController>();
        for (int segment = 0; segment <= chains; segment++)
        {
            if (!Alive(target)) yield break;
            float scale = mainScale * CurrentSize * (segment == 0 ? 1f : chainScale);
            var visual = Instantiate(projectilePrefab, origin, Quaternion.identity);
            visual.transform.localScale = projectilePrefab.transform.localScale * scale;
            flying.Add(visual);
            AudioSource flightAudio = Sound(visual, castSounds, castVolume);
            Vector3 destination = target.transform.position + Vector3.up * targetHeight;
            Vector3 initialDirection = (destination - origin).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, initialDirection).normalized;
            float laneOffset = laneCount > 1 ? (2f * lane / (laneCount - 1) - 1f) : 0f;
            Vector3 bend = side * laneOffset * volleySpread
                + Vector3.up * (laneCount > 1 ? volleyArcHeight * (1f - .35f * Mathf.Abs(laneOffset)) : 0f);
            if (segment > 0) bend *= .5f;
            float progress = 0f;
            Vector3 incomingDirection = initialDirection;
            float elapsed = 0f;
            bool arrived = false;
            bool frozen = false;
            var particles = visual.GetComponentsInChildren<ParticleSystem>(true);

            while (visual != null && elapsed < maxFlightTime)
            {
                if (Paused)
                {
                    if (!frozen)
                    {
                        foreach (var p in particles) if (p != null) p.Pause(false);
                        if (flightAudio != null) flightAudio.Pause();

                        frozen = true;
                    }
                    yield return null;
                    continue;
                }
                if (frozen)
                {
                    foreach (var p in particles)
                        if (p != null && p.isPaused) p.Play(false);

                    if (flightAudio != null) flightAudio.UnPause();
                    frozen = false;
                }
                // Keep the last destination after a kill so the rest of a volley finishes its flight.
                if (Alive(target)) destination = target.transform.position + Vector3.up * targetHeight;
                Vector3 oldPosition = visual.transform.position;
                float pathLength = Mathf.Max(.1f, Vector3.Distance(origin, destination) + bend.magnitude);
                progress = Mathf.Min(1f, progress + CurrentProjectileSpeed * Time.deltaTime / pathLength);
                Vector3 position = EvaluateFlight(origin, destination, bend, progress);
                Vector3 movement = position - oldPosition;
                if (movement.sqrMagnitude > .000001f)
                {
                    incomingDirection = movement.normalized;
                    visual.transform.rotation = Quaternion.LookRotation(incomingDirection);
                }
                visual.transform.position = position;
                if (progress >= 1f) { arrived = true; break; }
                elapsed += Time.deltaTime;
                yield return null;
            }

            Vector3 hitPosition = visual != null ? visual.transform.position : origin;
            Quaternion hitRotation = visual != null ? visual.transform.rotation : Quaternion.identity;
            FinishFlight(visual);
            if (!arrived) yield break;

            visited.Add(target);
            var impact = Instantiate(impactPrefab, hitPosition, hitRotation);
            impact.transform.localScale = impactPrefab.transform.localScale * scale;
            AudioSource impactAudio = Sound(impact, impactSounds, impactVolume);
            float duration = 2f;
            if (impactAudio != null && impactAudio.clip != null)
                duration = Mathf.Max(duration, impactAudio.clip.length + 0.1f);
            Destroy(impact, duration);
            float hitDamage = segment == 0 ? damage : damage * chainDamageMultiplier;
            DealImpact(target, hitDamage, hitPosition, incomingDirection, segment == 0 ? 1f : chainScale);

            if (segment == chains) yield break;
            origin = hitPosition;
            target = Nearest(origin, chainRange, visited);
        }
    }

    void DealImpact(EnemyController target, float hitDamage, Vector3 point, Vector3 direction, float scale)
    {
        DealHit(target, hitDamage, point, direction);
        float radius = CurrentImpactRadius * scale;
        if (radius <= 0f) return;
        splashTargets.Clear();
        int count = Physics.OverlapSphereNonAlloc(point, radius, splashBuffer, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var enemy = splashBuffer[i].GetComponentInParent<EnemyController>();
            if (!Alive(enemy) || enemy == target || !splashTargets.Add(enemy)) continue;
            DealHit(enemy, hitDamage, point, direction);
        }
    }

    void DealHit(EnemyController target, float damage, Vector3 point, Vector3 direction)
    {
        if (!Alive(target)) return;
        if (stats) stats.DealSpellDamage(target, damage, point, direction);
        else if (target) target.TakeDamage(damage, point, direction, damage);
    }

    public static Vector3 EvaluateFlight(Vector3 origin, Vector3 destination, Vector3 bend, float progress)
    {
        float t = Mathf.Clamp01(progress);
        return Vector3.Lerp(origin, destination, t) + bend * (4f * t * (1f - t));
    }

    AudioSource Sound(GameObject holder, AudioClip[] clips, float volume)
    {
        if (clips == null || clips.Length == 0) return null;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return null;
        var source = holder.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.dopplerLevel = 0f;
        source.minDistance = 4f;
        source.maxDistance = 35f;
        source.Play();
        return source;
    }

    void FinishFlight(GameObject visual)
    {
        if (visual == null) return;
        flying.Remove(visual);
        foreach (var source in visual.GetComponents<AudioSource>()) source.Stop();
        var core = visual.GetComponent<ParticleSystem>();
        foreach (var ps in visual.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (ps.isPaused) ps.Play(false);
            ps.Stop(false, ps == core
                ? ParticleSystemStopBehavior.StopEmittingAndClear
                : ParticleSystemStopBehavior.StopEmitting);
        }
        Destroy(visual, 2f);
    }

    void OnDisable()
    {
        StopAllCoroutines();
        foreach (var visual in flying) if (visual != null) Destroy(visual);
        flying.Clear();
    }
}




