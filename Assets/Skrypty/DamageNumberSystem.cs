using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(500)]
[DisallowMultipleComponent]
public sealed class DamageNumberSystem : MonoBehaviour
{
    public TMP_FontAsset font;
    public Camera targetCamera;
    public Color textColor = new Color(1f, 0.96f, 0.82f, 1f);
    public Color outlineColor = new Color(0.08f, 0.05f, 0.03f, 1f);
    [Range(0f, 0.4f)] public float outlineWidth = 0.18f;
    [Min(8f)] public float heightAt1080p = 34f;
    [Min(0.1f)] public float lifetime = 0.75f;
    public float riseDistance = 0.65f;
    [Min(1)] public int maximumNumbers = 96;
    [Min(1f)] public float maximumDistance = 80f;

    static DamageNumberSystem instance;
    static bool warnedMissing;
    Material numberMaterial;
    bool warnedFont;
    sealed class Popup
    {
        public TextMeshPro text;
        public Transform transform;
        public Renderer renderer;
        public Vector3 start, drift;
        public float age, glyphHeight;
    }
    readonly List<Popup> active = new();
    readonly Stack<Popup> available = new();
    int created;

    void OnEnable()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning("Keep only one active DamageNumberSystem in the scene.", this);
            enabled = false;
            return;
        }
        instance = this;
        warnedMissing = false;
    }

    public static void Show(float damage, Vector3 position)
    {
        if (instance != null) instance.Spawn(damage, position);
        else if (!warnedMissing)
        {
            warnedMissing = true;
            Debug.LogWarning("Add DamageNumberSystem to the scene and assign Russo One SDF to show damage numbers.");
        }
    }

    bool Prepare()
    {
        if (font == null)
        {
            if (!warnedFont) Debug.LogWarning("Assign Russo One SDF to DamageNumberSystem > Font.", this);
            warnedFont = true;
            return false;
        }
        if (targetCamera == null) targetCamera = Camera.main;
        if (targetCamera == null) return false;
        if (numberMaterial == null)
        {
            numberMaterial = new Material(font.material) { name = "Damage numbers (runtime)" };
            numberMaterial.EnableKeyword("OUTLINE_ON");
            numberMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outlineColor);
            numberMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, outlineWidth);
        }
        return true;
    }

    void Spawn(float damage, Vector3 position)
    {
        if (!Prepare() || damage <= 0f) return;
        if (Vector3.Distance(position, targetCamera.transform.position) > maximumDistance) return;
        Popup popup;
        if (available.Count > 0) popup = available.Pop();
        else if (created < Mathf.Max(1, maximumNumbers))
        {
            var go = new GameObject("Damage number", typeof(RectTransform));
            go.layer = 0;
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<TextMeshPro>();
            text.font = font;
            text.fontSharedMaterial = numberMaterial;
            text.fontSize = 10;
            text.enableAutoSizing = false;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.richText = false;
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(20, 5);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            popup = new Popup { text = text, transform = go.transform, renderer = renderer };
            created++;
        }
        else
        {
            popup = active[0];
            active.RemoveAt(0);
        }
        popup.text.gameObject.SetActive(true);
        popup.transform.localScale = Vector3.one;
        popup.text.text = damage.ToString("0.##", CultureInfo.InvariantCulture);
        popup.text.color = textColor;
        popup.text.ForceMeshUpdate();
        popup.glyphHeight = Mathf.Max(0.01f, popup.text.textBounds.size.y);
        popup.start = position + targetCamera.transform.right * Random.Range(-0.15f, 0.15f);
        popup.drift = targetCamera.transform.right * Random.Range(-0.3f, 0.3f);
        popup.age = 0f;
        active.Add(popup);
        Position(popup);
    }

    void Position(Popup popup)
    {
        if (targetCamera == null) { popup.renderer.enabled = false; return; }
        float t = Mathf.Clamp01(popup.age / Mathf.Max(0.1f, lifetime));
        popup.transform.position = popup.start + Vector3.up * (riseDistance * t) + popup.drift * t;
        popup.transform.rotation = targetCamera.transform.rotation;
        float depth = Vector3.Dot(popup.transform.position - targetCamera.transform.position, targetCamera.transform.forward);
        popup.renderer.enabled = depth > targetCamera.nearClipPlane;
        float worldHeight = targetCamera.orthographic ? 2f * targetCamera.orthographicSize
            : 2f * Mathf.Max(depth, targetCamera.nearClipPlane) * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float punch = t < 0.12f ? Mathf.Lerp(0.7f, 1.2f, t / 0.12f)
            : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((t - 0.12f) / 0.2f));
        popup.transform.localScale = Vector3.one * (worldHeight * heightAt1080p / 1080f / popup.glyphHeight * punch);
        Color color = textColor;
        color.a *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 1f, t));
        popup.text.color = color;
    }

    void LateUpdate()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var popup = active[i];
            popup.age += Time.deltaTime;
            if (popup.age >= Mathf.Max(0.1f, lifetime))
            {
                popup.text.gameObject.SetActive(false);
                active.RemoveAt(i);
                available.Push(popup);
            }
            else Position(popup);
        }
    }

    void OnDisable()
    {
        if (instance == this) instance = null;
        foreach (var popup in active)
        {
            popup.text.gameObject.SetActive(false);
            available.Push(popup);
        }
        active.Clear();
    }

    void OnDestroy()
    {
        if (numberMaterial != null) Destroy(numberMaterial);
    }
}
