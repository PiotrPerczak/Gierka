using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Fixed-size corpse pool. Directional launch with varied lateral trajectories and restrained tumble.</summary>
public sealed class SkeletonDebris : MonoBehaviour
{
    public Rigidbody[] bodies;
    public Collider[] pieceColliders;
    public Material[] boneMaterials;
    public const int MaximumActiveCorpses = 6;
    public float visibleSeconds = 6f;
    public float simulationSeconds = 3f;
    [Min(0f)] public float ragdollDuration = 0f;
    [Min(.02f)] public float breakupBlendDuration = .08f;
        [Header("Death variation - direction of impact is preserved")]
    [Range(0f, 1f)] public float trajectoryVariation = .65f;
    [Range(0f, 2.5f)] public float lateralScatter = 1.3f;
    [Range(0f, 1f)] public float liftVariation = .5f;
    [Range(0f, 1f)] public float spinVariation = .65f;
    [Range(0f, 2f)] public float ragdollTwist = .65f;
    public bool HasSeparated { get; private set; }
    public int ActiveJointCount { get { int n=0; if(joints!=null)foreach(var j in joints)if(j && j.xMotion!=ConfigurableJointMotion.Free)n++;return n; } }
    public Vector3 LastImpulse { get; private set; }
    static readonly List<SkeletonDebris> Pool = new();
    SkeletonDebris template;
    float born;
    bool live, settled;
    float[] weights;
    static int launchSequence;
    ConfigurableJoint[] joints;
    Vector3[] separationVelocity, separationSpin;
    float blendApplied;
    EnemyHitFeedback deathFlash;

    public void PlayDeathFlash(EnemyHitFeedback source)
    {
        if (!source) return;
        if (!deathFlash) deathFlash = gameObject.AddComponent<EnemyHitFeedback>();
        deathFlash.hitColor = source.hitColor;
        deathFlash.flashDuration = source.flashDuration;
        deathFlash.flashStrength = source.flashStrength;
        deathFlash.emissionStrength = source.emissionStrength;
        deathFlash.PlayFlashOnly();
    }

    public static SkeletonDebris Acquire(SkeletonDebris prefab)
    {
        Pool.RemoveAll(x => !x);
        SkeletonDebris chosen = null;
        foreach (var corpse in Pool)
            if (corpse.template == prefab && !corpse.live) { chosen = corpse; break; }
        if (!chosen && Pool.Count < MaximumActiveCorpses)
        {
            chosen = Instantiate(prefab);
            chosen.template = prefab;
            Pool.Add(chosen);
        }
        if (!chosen)
        {
            // Reuse the oldest matching corpse rather than growing physics without a bound.
            foreach (var corpse in Pool)
                if (corpse.template == prefab && (!chosen || corpse.born < chosen.born)) chosen = corpse;
        }
        if (!chosen) return null;
        chosen.Prepare();
        return chosen;
    }

    void Prepare()
    {
        live = false;
        if (deathFlash) deathFlash.ClearFlash();
        ReleaseJoints(); HasSeparated = false; blendApplied = 0f;
        gameObject.SetActive(true);
        weights ??= new float[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            var rb = bodies[i];
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            rb.isKinematic = true;
            pieceColliders[i].enabled = false;
        }
        // Prevent initial overlap impulses and pointless bone-to-bone contacts.
        for (int i = 0; i < pieceColliders.Length; i++)
            for (int j = i + 1; j < pieceColliders.Length; j++)
                Physics.IgnoreCollision(pieceColliders[i], pieceColliders[j], true);
    }

    public void Launch(DirectionalSkeletonDeath.Binding[] pose, Vector3 hitPoint,
        Vector3 impulse, Collider[] owner, float strength = 1f)
    {
        float total = 0f;
        float totalMass = 0f;
        for (int i = 0; i < bodies.Length; i++)
        {
            var bone = pose[i].bone;
            if (!bone) { gameObject.SetActive(false); return; }
                        // Write Transform and PhysX together. A later scale sync must not restore
            // the prefab's old world position after Rigidbody.position was assigned.
            var rb = bodies[i];
            Vector3 position = bone.TransformPoint(pose[i].boneLocalPosition);
            Quaternion rotation = bone.rotation * pose[i].boneLocalRotation;
            rb.transform.localScale = bone.lossyScale;
            rb.transform.SetPositionAndRotation(position, rotation);
            rb.position = position;
            rb.rotation = rotation;
            float distance = Vector3.Distance(bodies[i].worldCenterOfMass, hitPoint);
            weights[i] = bodies[i].mass * (.65f + .7f / (1f + distance * distance));
            total += weights[i];
            totalMass += bodies[i].mass;
        }
        foreach (var col in pieceColliders) col.enabled = true;
        for (int a = 0; a < pieceColliders.Length; a++)
            for (int b = a + 1; b < pieceColliders.Length; b++)
                Physics.IgnoreCollision(pieceColliders[a], pieceColliders[b], true);
        var player = Object.FindAnyObjectByType<TopDownPlayerController>();
        if (player) foreach (var col in pieceColliders) foreach (var pc in player.GetComponentsInChildren<Collider>()) if(col.gameObject.scene.GetPhysicsScene() == pc.gameObject.scene.GetPhysicsScene()) Physics.IgnoreCollision(col, pc, true);
        Physics.SyncTransforms();
        if (ragdollDuration > 0f) BuildRagdoll(pose);
        separationVelocity ??= new Vector3[bodies.Length];
        separationSpin ??= new Vector3[bodies.Length];
        LastImpulse = impulse;
        // A local generator varies deaths without consuming gameplay's Unity Random state.
        var random = new System.Random(unchecked(++launchSequence * 486187739));
        float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
        Vector3 forward = impulse.sqrMagnitude > .001f ? impulse.normalized : Vector3.forward;
        Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
        if (side.sqrMagnitude < .01f) side = Vector3.right;
        Vector3 center = Vector3.zero;
        foreach (var rb in bodies) center += rb.worldCenterOfMass * rb.mass;
        center /= Mathf.Max(.01f, totalMass);
        float sharedSpeed = impulse.magnitude / Mathf.Max(1f, totalMass) * 1.45f;
        float variation = Mathf.Clamp01(trajectoryVariation);
        float deathSpread = Mathf.Lerp(1f, Range(.7f, 1.4f), variation);
        float deathLift = Mathf.Lerp(1f, Range(.65f, 1.35f), liftVariation);
        float deathSpin = Mathf.Lerp(1f, Range(.6f, 1.4f), spinVariation);
        Vector3 initialTwist = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f))
            * ragdollTwist * variation;
        float spread = Mathf.Lerp(.55f, 3.4f, Mathf.Clamp01(strength)) * lateralScatter * deathSpread;
        for (int i = 0; i < bodies.Length; i++)
        {
            var rb = bodies[i];
            var collider = pieceColliders[i];
            collider.enabled = true;
            foreach (var own in owner) if (own && own.gameObject.scene.GetPhysicsScene() == collider.gameObject.scene.GetPhysicsScene()) Physics.IgnoreCollision(collider, own, true);
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();
            // Translation dominates: a shared push backwards plus independent lateral flight.
            Vector3 radial = Vector3.ProjectOnPlane(rb.worldCenterOfMass - center, forward);
            float radialSide = Mathf.Clamp(Vector3.Dot(radial, side) * 1.8f, -1f, 1f);
            float sideSpeed = spread * (.55f * radialSide + Range(-1.15f, 1.15f) * variation);
            float forwardVariation = 1f + Range(-.35f, .35f) * variation;
            float lift = Mathf.Lerp(.25f, 1.2f, strength) * deathLift * (1f + Range(-.7f, .7f) * liftVariation);
            rb.linearDamping = .035f;
            rb.angularDamping = .45f;
            rb.maxAngularVelocity = 6f;
            rb.linearVelocity = forward * sharedSpeed + Vector3.up * .3f;
            rb.angularVelocity = initialTwist;
            separationVelocity[i] = forward * sharedSpeed * (forwardVariation - 1f)
                + side * sideSpeed + Vector3.up * lift;
            // Small, varied spin rather than identical torque around the impact point.
            float tumble = Mathf.Lerp(.6f, 2.8f, strength) * deathSpin;
            Vector3 lever = Vector3.ClampMagnitude(hitPoint - rb.worldCenterOfMass, .07f);
            Vector3 impactAxis = Vector3.Cross(lever, forward);
            separationSpin[i] = impactAxis * tumble * 5f
                + new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f)) * tumble * spinVariation;
        }
        int nearest=0;float nearestDistance=float.PositiveInfinity;
        for(int i=0;i<bodies.Length;i++){float d=(bodies[i].worldCenterOfMass-hitPoint).sqrMagnitude;if(d<nearestDistance){nearestDistance=d;nearest=i;}}
        var struck=bodies[nearest];
        struck.AddForceAtPosition(impulse*.08f,struck.worldCenterOfMass+Vector3.ClampMagnitude(hitPoint-struck.worldCenterOfMass,.1f),ForceMode.Impulse);
        born = Time.time;
        live = true;
        settled = false;
    }

    void BuildRagdoll(DirectionalSkeletonDeath.Binding[] pose)
    {
        joints ??= new ConfigurableJoint[bodies.Length];
        var lookup = new Dictionary<Transform,int>(pose.Length);
        for(int i=0;i<pose.Length;i++)lookup[pose[i].bone]=i;
        for(int i=0;i<pose.Length;i++)
        {
            int parentIndex=-1;
            for(var parent=pose[i].bone.parent;parent!=null;parent=parent.parent)
            { if(lookup.TryGetValue(parent,out parentIndex))break; parentIndex=-1; }
            if(parentIndex<0)continue;
            var rb=bodies[i];var parentBody=bodies[parentIndex];
            var joint=joints[i];
            if(!joint){joint=rb.gameObject.AddComponent<ConfigurableJoint>();joints[i]=joint;}
            joint.autoConfigureConnectedAnchor=false;
            joint.connectedBody=parentBody;
            Vector3 pivot=pose[i].bone.position;
            joint.anchor=rb.transform.InverseTransformPoint(pivot);
            joint.connectedAnchor=parentBody.transform.InverseTransformPoint(pivot);
            joint.axis=Vector3.right;joint.secondaryAxis=Vector3.up;
            joint.xMotion=joint.yMotion=joint.zMotion=ConfigurableJointMotion.Locked;
            joint.angularXMotion=joint.angularYMotion=joint.angularZMotion=ConfigurableJointMotion.Limited;
            joint.lowAngularXLimit=new SoftJointLimit{limit=-40};
            joint.highAngularXLimit=new SoftJointLimit{limit=40};
            joint.angularYLimit=new SoftJointLimit{limit=35};
            joint.angularZLimit=new SoftJointLimit{limit=35};
            joint.enableCollision=false;joint.enablePreprocessing=false;
            joint.projectionMode=JointProjectionMode.PositionAndRotation;
            joint.projectionDistance=.03f;joint.projectionAngle=15f;
            rb.solverIterations=8;
        }
    }

    void ReleaseJoints()
    {
        if(joints==null)return;
        foreach(var joint in joints)
        {
            if(!joint)continue;
            joint.xMotion=joint.yMotion=joint.zMotion=ConfigurableJointMotion.Free;
            joint.angularXMotion=joint.angularYMotion=joint.angularZMotion=ConfigurableJointMotion.Free;
            joint.projectionMode=JointProjectionMode.None;
            joint.connectedBody=null;
        }
    }

    void FixedUpdate()
    {
        if(!live||settled)return;
        if(!HasSeparated)
        {
            if(Time.time-born<ragdollDuration)return;
            ReleaseJoints();HasSeparated=true;
            // No replacement or teleport: retain the complete physical state.
        }
        float portion=Mathf.Min(1f-blendApplied,Time.fixedDeltaTime/Mathf.Max(.02f,breakupBlendDuration));
        if(portion<=0)return;
        for(int i=0;i<bodies.Length;i++)
        {
            bodies[i].AddForce(separationVelocity[i]*portion,ForceMode.VelocityChange);
            bodies[i].angularVelocity+=separationSpin[i]*portion;
        }
        blendApplied+=portion;
    }
    void Update()
    {
        if (!live) return;
        float age = Time.time - born;
        bool allResting = true;
        if (!settled && age >= simulationSeconds)
            foreach (var rb in bodies) if (!rb.IsSleeping()) { allResting = false; break; }
        if (!settled && age >= simulationSeconds && allResting)
        {
            foreach (var rb in bodies) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }
            foreach (var col in pieceColliders) col.enabled = false;
            settled = true;
        }
        if (age >= visibleSeconds) { live = false; gameObject.SetActive(false); }
    }
    void OnDestroy() { Pool.Remove(this); }
}









