using UnityEngine;
namespace DuskYard
{
    // Two-bone geometric arm pose keeps the sword in the glove throughout each cut.
    public sealed class GuardSwordPose : MonoBehaviour
    {
        [System.NonSerialized] public CombatDemo combat;
        public Transform body, upperArm, forearm, glove;
        float upperMeshLength, foreMeshLength;
        void Start()
        {
            if(combat==null)combat=Object.FindAnyObjectByType<CombatDemo>();
            if(combat==null){enabled=false;return;}
            upperMeshLength = upperArm.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
            foreMeshLength = forearm.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
        }
        void LateUpdate()
        {
            float twist = combat.AttackWeight * (combat.ComboStep == 1 ? -12 : 12);
            body.localRotation = Quaternion.Euler(0, twist, combat.AttackWeight * (combat.ComboStep == 2 ? 3 : 0));
            Vector3 shoulder = body.TransformPoint(new Vector3(.30f, 1.42f, 0));
            Vector3 hand = combat.blade.TransformPoint(new Vector3(0, 0, -.09f));
            Vector3 direction = hand - shoulder;
            float distance = Mathf.Clamp(direction.magnitude, .04f, .57f);
            direction.Normalize();
            Vector3 pole = Vector3.ProjectOnPlane(body.right - body.forward * .4f, direction).normalized;
            Vector3 elbow = shoulder + direction * (distance * .5f) + pole * Mathf.Sqrt(Mathf.Max(0, .29f * .29f - distance * distance * .25f));
            PoseSegment(upperArm, shoulder, elbow, upperMeshLength);
            PoseSegment(forearm, elbow, hand, foreMeshLength);
            glove.SetPositionAndRotation(hand, combat.blade.rotation * Quaternion.Euler(90, 0, 0));
        }
        static void PoseSegment(Transform part, Vector3 from, Vector3 to, float meshLength)
        {
            part.SetPositionAndRotation(from, Quaternion.FromToRotation(Vector3.up, to - from));
            part.localScale = new Vector3(1, (to - from).magnitude / Mathf.Max(.01f, meshLength), 1);
        }
    }
}
