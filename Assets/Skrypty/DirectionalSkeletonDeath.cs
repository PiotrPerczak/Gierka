using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Swaps an animated skeleton for pooled rigid fragments in its current pose.</summary>
[DisallowMultipleComponent]
public sealed class DirectionalSkeletonDeath : MonoBehaviour
{
    [Serializable] public struct Binding
    {
        public Transform bone;
        public Vector3 boneLocalPosition;
        public Quaternion boneLocalRotation;
    }

    public SkeletonDebris debrisPrefab;
    public Binding[] bindings;
    [Min(1f)] public float minimumImpulse = 22f;
    [Min(1f)] public float maximumImpulse = 115f;
    [Min(1f)] public float fullStrengthDamage = 100f;
    [Range(0f, .5f)] public float upwardBias = .18f;
    public bool HasBroken { get; private set; }

    public bool Break(Vector3 point, Vector3 direction, float impactStrength)
    {
        if (HasBroken || !debrisPrefab || bindings == null || bindings.Length == 0) return false;
        var debris = SkeletonDebris.Acquire(debrisPrefab);
        if (!debris || debris.bodies.Length != bindings.Length) return false;
        HasBroken = true;
        if (direction.sqrMagnitude < .0001f) direction = transform.forward;
        direction.Normalize();
        direction.y = Mathf.Clamp(direction.y, -.2f, .6f);
        direction = (direction + Vector3.up * upwardBias).normalized;
        float strength = Mathf.Clamp01(impactStrength / Mathf.Max(1f, fullStrengthDamage));
        float force = impactStrength <= 0f ? 0f : Mathf.Lerp(minimumImpulse, maximumImpulse,
            Mathf.Clamp01(impactStrength / Mathf.Max(1f, fullStrengthDamage)));
        debris.Launch(bindings, point, direction * force, GetComponentsInChildren<Collider>(true), strength);
        debris.PlayDeathFlash(GetComponent<EnemyHitFeedback>());
        foreach (var lod in GetComponentsInChildren<LODGroup>()) lod.enabled = false;
        foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = false;
        foreach (var animator in GetComponentsInChildren<Animator>()) animator.enabled = false;
        return true;
    }
}


