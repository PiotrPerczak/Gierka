using UnityEngine;
using UnityEngine.Rendering;

public sealed class LightningChainVisual : MonoBehaviour
{
    LineRenderer line;
    Vector3 from, to;
    Color color;
    float duration, age, refresh;
    int segments;

    public static void Spawn(Vector3 from, Vector3 to, Material material, Color color, float width, float lifetime)
    {
        if (material == null || (to - from).sqrMagnitude < 0.001f) return;
        var go = new GameObject("Lightning chain");
        go.layer = 0;
        var visual = go.AddComponent<LightningChainVisual>();
        visual.from = from;
        visual.to = to;
        visual.color = color;
        visual.duration = Mathf.Max(0.05f, lifetime);
        visual.segments = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from, to) / 1.5f), 6, 32);
        visual.line = go.AddComponent<LineRenderer>();
        visual.line.useWorldSpace = true;
        visual.line.sharedMaterial = material;
        visual.line.positionCount = visual.segments + 1;
        visual.line.widthMultiplier = Mathf.Max(0.005f, width);
        visual.line.numCapVertices = 2;
        visual.line.shadowCastingMode = ShadowCastingMode.Off;
        visual.line.receiveShadows = false;
        visual.line.startColor = color;
        visual.line.endColor = color;
        visual.Redraw();
    }

    void Redraw()
    {
        Vector3 direction = (to - from).normalized;
        Vector3 side = Vector3.Cross(direction, Vector3.up);
        if (side.sqrMagnitude < 0.01f) side = Vector3.Cross(direction, Vector3.right);
        side.Normalize();
        Vector3 up = Vector3.Cross(direction, side).normalized;
        float amplitude = Mathf.Clamp(Vector3.Distance(from, to) * 0.04f, 0.08f, 0.8f);
        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            Vector3 position = Vector3.Lerp(from, to, t);
            if (i > 0 && i < segments)
                position += (side * Random.Range(-1f, 1f) + up * Random.Range(-1f, 1f)) * amplitude;
            line.SetPosition(i, position);
        }
    }

    void Update()
    {
        age += Time.deltaTime;
        if (age >= duration) { Destroy(gameObject); return; }
        refresh += Time.deltaTime;
        if (refresh >= 0.035f) { refresh = 0f; Redraw(); }
        Color fading = color;
        fading.a *= 1f - age / duration;
        line.startColor = fading;
        line.endColor = fading;
    }
}
