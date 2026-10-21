using System.Collections.Generic;
using UnityEngine;

// Hold the rendered skeleton pose while the Animator continues to run normally.
// CharacterController movement and Animator state timing are unchanged.
[DefaultExecutionOrder(10000)]
public class SteppedCharacterPose : MonoBehaviour
{
    [Range(1f, 30f)] public float framesPerSecond = 5f;
    public Animator targetAnimator;
    private Transform[] bones;
    private Vector3[] positions, scales;
    private Quaternion[] rotations;
    private float elapsed;
    private bool captured;

    void Awake()
    {
        if (targetAnimator == null) targetAnimator = GetComponentInChildren<Animator>();
        if (targetAnimator == null)
        { Debug.LogWarning("SteppedCharacterPose: przypisz Animator gracza.", this); enabled = false; return; }
        var unique = new HashSet<Transform>();
        foreach (var skin in targetAnimator.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            foreach (var bone in skin.bones)
                if (bone != null && bone != transform && bone != targetAnimator.transform)
                    unique.Add(bone);
        bones = new Transform[unique.Count];
        unique.CopyTo(bones);
        positions = new Vector3[bones.Length];
        scales = new Vector3[bones.Length];
        rotations = new Quaternion[bones.Length];
        if (bones.Length == 0)
        { Debug.LogWarning("SteppedCharacterPose: brak kosci SkinnedMeshRenderer.", this); enabled = false; }
    }

    void OnEnable() { elapsed = 0f; captured = false; }

    void LateUpdate()
    {
        if (bones == null || targetAnimator == null || !targetAnimator.isActiveAndEnabled) return;
        elapsed += Time.deltaTime;
        float interval = 1f / Mathf.Max(1f, framesPerSecond);
        if (!captured || elapsed >= interval)
        {
            elapsed %= interval;
            captured = true;
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                positions[i] = bones[i].localPosition;
                rotations[i] = bones[i].localRotation;
                scales[i] = bones[i].localScale;
            }
        }
        else
        {
            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null) continue;
                bones[i].localPosition = positions[i];
                bones[i].localRotation = rotations[i];
                bones[i].localScale = scales[i];
            }
        }
    }
}
