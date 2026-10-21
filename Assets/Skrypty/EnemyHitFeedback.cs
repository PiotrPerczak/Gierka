using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class EnemyHitFeedback : MonoBehaviour
{
    public Color hitColor = Color.white;

    [Min(0.02f)]
    public float flashDuration = 0.08f;
    
    [Min(0f)]
    public float emissionStrength = 0.05f;

    [Range(0f, 1f)]
    public float flashStrength = 0.4f;
    [Min(0f)] public float numberHeight = 0.25f;
    public Renderer[] modelRenderers;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int LegacyColor = Shader.PropertyToID("_Color");
    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    sealed class Slot
    {
        public Material material;
        public int colorProperty;
        public Color color, emission;
        public bool hasEmission, emissionEnabled;
    }
    sealed class Binding
    {
        public Renderer renderer;
        public Material[] originals;
    }
    readonly List<Slot> slots = new();
    readonly List<Binding> bindings = new();
    bool initialized, flashing;
    float remaining;

    void Initialize()
    {
        if (initialized) return;
        initialized = true;
        if (modelRenderers == null || modelRenderers.Length == 0)
        {
            var found = new List<Renderer>();
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                if ((renderer is SkinnedMeshRenderer || renderer is MeshRenderer) &&
                    renderer.GetComponent<TMP_Text>() == null)
                    found.Add(renderer);
            modelRenderers = found.ToArray();
        }
        var seen = new HashSet<Renderer>();
        foreach (var renderer in modelRenderers)
        {
            if (renderer == null || !seen.Add(renderer)) continue;
            var originals = renderer.sharedMaterials;
            var copies = (Material[])originals.Clone();
            for (int i = 0; i < originals.Length; i++)
            {
                Material original = originals[i];
                if (original == null) continue;
                // Nie zmieniaj koloru materialu obrysu podczas trafienia.
                if (original.shader != null &&
                    original.shader.name.IndexOf(
                        "Outline",
                        System.StringComparison.OrdinalIgnoreCase
                    ) >= 0)
                {
                    continue;
                }
                int property = original.HasProperty(BaseColor) ? BaseColor : LegacyColor;
                if (!original.HasProperty(property)) continue;
                var copy = new Material(original) { name = original.name + " (Hit feedback)" };
                copies[i] = copy;
                bool hasEmission = copy.HasProperty(EmissionColor);
                slots.Add(new Slot
                {
                    material = copy, colorProperty = property, color = copy.GetColor(property),
                    hasEmission = hasEmission,
                    emission = hasEmission ? copy.GetColor(EmissionColor) : Color.black,
                    emissionEnabled = copy.IsKeywordEnabled("_EMISSION")
                });
            }
            bindings.Add(new Binding { renderer = renderer, originals = originals });
            renderer.sharedMaterials = copies;
        }
    }

    public void Play(float healthLost) => PlayFeedback(healthLost, true);

    public void PlayFlashOnly() => PlayFeedback(1f, false);

    public void ClearFlash() => ResetFlash();

    void PlayFeedback(float healthLost, bool showDamageNumber)
    {
        if (healthLost <= 0f) return;
        Initialize();
        remaining = Mathf.Max(0.02f, flashDuration);
        flashing = true;
        foreach (var slot in slots)
        {
            Color color = Color.Lerp(
        slot.color,
         hitColor,
        flashStrength
          );

            color.a = slot.color.a;
            slot.material.SetColor(slot.colorProperty, color);
            if (slot.hasEmission)
            {
                slot.material.EnableKeyword("_EMISSION");
                slot.material.SetColor(EmissionColor, hitColor * emissionStrength);
            }
        }
        if (!showDamageNumber) return;
        Vector3 position = transform.position + Vector3.up * 2f;
        bool hasBounds = false;
        Bounds bounds = default;
        foreach (var renderer in modelRenderers)
        {
            if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (hasBounds) position = new Vector3(bounds.center.x, bounds.max.y + numberHeight, bounds.center.z);
        DamageNumberSystem.Show(healthLost, position);
    }

    void Update()
    {
        if (!flashing) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f) ResetFlash();
    }

    void ResetFlash()
    {
        flashing = false;
        foreach (var slot in slots)
        {
            if (slot.material == null) continue;
            slot.material.SetColor(slot.colorProperty, slot.color);
            if (slot.hasEmission)
            {
                slot.material.SetColor(EmissionColor, slot.emission);
                if (slot.emissionEnabled) slot.material.EnableKeyword("_EMISSION");
                else slot.material.DisableKeyword("_EMISSION");
            }
        }
    }

    void OnDisable() => ResetFlash();
    void OnDestroy()
    {
        foreach (var binding in bindings)
            if (binding.renderer != null) binding.renderer.sharedMaterials = binding.originals;
        foreach (var slot in slots)
            if (slot.material != null) Destroy(slot.material);
    }
}
