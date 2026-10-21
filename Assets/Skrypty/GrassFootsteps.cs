using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GrassFootsteps : MonoBehaviour
{
    public AudioClip[] grassClips;
    public AudioSource footstepSource;
    [Min(0.1f)] public float distancePerStep = 1.1f;
    [Min(0f)] public float groundedGraceTime = 0.15f;
    [Range(0f, 1f)] public float volume = 0.4f;
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
    private CharacterController controller;
    private Vector3 previousPosition;
    private float distance;
    private int lastClip = -1;
    private float lastGroundedTime;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (footstepSource == null)
        {
            var child = new GameObject("FootstepAudio");
            child.transform.SetParent(transform, false);
            footstepSource = child.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.loop = false;
            footstepSource.spatialBlend = 0f;
        }
    }

    void OnEnable()
    {
        previousPosition = transform.position;
        distance = 0f;
        lastGroundedTime = float.NegativeInfinity;
    }

    void LateUpdate()
    {
        Vector3 delta = transform.position - previousPosition;
        previousPosition = transform.position;
        delta.y = 0f;
        float moved = delta.magnitude;
        if (Time.deltaTime <= 0f) return;
        if (controller.isGrounded) lastGroundedTime = Time.time;
        // Brief losses of ground contact must not reset the step cadence.
        if (Time.time - lastGroundedTime > groundedGraceTime)
        { distance = 0f; return; }
        if (moved < 0.0001f) return;
        // Ignore teleports rather than playing a burst of footsteps.
        if (moved > Mathf.Max(1f, distancePerStep * 2f))
        { distance = 0f; return; }
        distance += moved;
        if (distance < Mathf.Max(0.1f, distancePerStep)) return;
        distance %= Mathf.Max(0.1f, distancePerStep);
        if (grassClips == null || grassClips.Length == 0 || footstepSource == null) return;
        int index = Random.Range(0, grassClips.Length);
        if (grassClips.Length > 1 && index == lastClip)
            index = (index + Random.Range(1, grassClips.Length)) % grassClips.Length;
        lastClip = index;
        if (grassClips[index] == null) return;
        footstepSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
        footstepSource.PlayOneShot(grassClips[index], volume);
    }
}
