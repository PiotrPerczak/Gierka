using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[DefaultExecutionOrder(-200)]
[RequireComponent(typeof(AudioSource))]
public sealed class BellInteraction : MonoBehaviour
{
    public Transform player;
    public CameraViewTransition cameraRig;
    public Collider interactionCollider;
    public AudioClip bellSound;
    public Shader outlineShader;
    public float interactionDistance = 3f;
    public float outlineWidth = 0.012f;
    [Range(0, 1)] public float outlineOpacity = 0.65f;
    static readonly List<BellInteraction> Bells = new();
    readonly List<GameObject> outlines = new();
    Material outlineMaterial;
    AudioSource source;
    float visibility;

    void OnEnable() => Bells.Add(this);
    void OnDisable()
    {
        Bells.Remove(this);
        foreach (var shell in outlines) if (shell != null) shell.SetActive(false);
    }

    void Start()
    {
        source = GetComponent<AudioSource>();
        if (interactionCollider == null) interactionCollider = GetComponentInChildren<Collider>();
        if (outlineShader == null) return;
        outlineMaterial = new Material(outlineShader);
        var meshes = GetComponentsInChildren<MeshFilter>(true);
        foreach (var mesh in meshes)
        {
            if (mesh.sharedMesh == null || mesh.GetComponent<MeshRenderer>() == null) continue;
            var shell = new GameObject("Bell outline (runtime)");
            shell.layer = 0; // Transparent shell must remain outside PixelWorld.
            shell.transform.SetParent(mesh.transform, false);
            shell.AddComponent<MeshFilter>().sharedMesh = mesh.sharedMesh;
            var renderer = shell.AddComponent<MeshRenderer>();
            var materials = new Material[mesh.sharedMesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = outlineMaterial;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            shell.SetActive(false);
            outlines.Add(shell);
        }
    }

    bool IsNear()
    {
        if (player == null || interactionCollider == null) return false;
        return Vector3.Distance(player.position, interactionCollider.ClosestPoint(player.position)) <= interactionDistance;
    }

    static BellInteraction GetNearestBell()
    {
        BellInteraction nearest = null;
        float closest = float.PositiveInfinity;
        foreach (var bell in Bells)
        {
            if (bell == null || bell.cameraRig == null || !bell.IsNear()) continue;
            float distance = (bell.interactionCollider.ClosestPoint(bell.player.position) - bell.player.position).sqrMagnitude;
            if (distance < closest)
            {
                closest = distance;
                nearest = bell;
            }
        }
        return nearest;
    }

    void Update()
    {
        bool near = IsNear();
        visibility = Mathf.MoveTowards(visibility, near ? 1f : 0f, Time.unscaledDeltaTime * 5f);
        if (outlineMaterial != null)
        {
            outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
            outlineMaterial.SetColor("_OutlineColor", new Color(1, 1, 1, visibility * outlineOpacity));
            foreach (var shell in outlines) if (shell != null) shell.SetActive(visibility > 0.001f);
        }
        if (!near || cameraRig == null || cameraRig.IsTransitioning || cameraRig.MenuOpen) return;
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame && GetNearestBell() == this)
        {
            if (bellSound != null) source.PlayOneShot(bellSound);
            cameraRig.ToggleView();
        }
    }

    void OnDestroy()
    {
        if (outlineMaterial != null) Destroy(outlineMaterial);
        foreach (var shell in outlines) if (shell != null) Destroy(shell);
    }
}
    