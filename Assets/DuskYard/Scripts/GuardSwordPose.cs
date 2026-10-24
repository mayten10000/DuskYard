using UnityEngine;
namespace DuskYard
{
    // Two-bone geometric arm pose keeps the sword in the glove throughout each cut.
    public sealed class GuardSwordPose : MonoBehaviour
    {
        [System.NonSerialized] public CombatDemo combat;
        public Transform body, upperArm, forearm, glove;
        public Transform leftUpperArm, leftForearm, leftGlove, head;
        float upperMeshLength, foreMeshLength;
        float leftUpperLength,leftForeLength;
        void Start()
        {
            if(combat==null)combat=Object.FindAnyObjectByType<CombatDemo>();
            if(combat==null){enabled=false;return;}
            upperMeshLength = upperArm.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
            foreMeshLength = forearm.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
            if(leftUpperArm!=null)leftUpperLength=leftUpperArm.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
            if(leftForearm!=null)leftForeLength=leftForearm.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
        }
        void LateUpdate()
        {
            float p=combat.AttackProgress, twist=0;
            if(p>=0)
            {
                float sign=combat.ComboStep==1?-1:1;
                twist=p<.24f?Mathf.Lerp(0,-13*sign,Mathf.SmoothStep(0,1,p/.24f)):p<.58f?Mathf.Lerp(-13*sign,20*sign,Mathf.SmoothStep(0,1,(p-.24f)/.34f)):Mathf.Lerp(20*sign,0,Mathf.SmoothStep(0,1,(p-.58f)/.42f));
            }
            body.localRotation = Quaternion.Euler(combat.ComboStep==2?combat.AttackWeight*5:0,twist,combat.AttackWeight*(combat.ComboStep==1?-3:3));
            body.localPosition = Vector3.up * (Mathf.Sin(Time.time*1.8f)*.003f);
            if(head!=null)head.localRotation=Quaternion.Euler(0,-twist*.65f,0);
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
            if(leftUpperArm!=null&&leftForearm!=null&&leftGlove!=null)
            {
                Vector3 a=body.TransformPoint(new Vector3(-.30f,1.42f,0));
                Vector3 b=body.TransformPoint(new Vector3(-.23f,1.13f,.24f+combat.AttackWeight*.07f));
                Vector3 d=(b-a).normalized;
                float length=Vector3.Distance(a,b);
                Vector3 bend=Vector3.ProjectOnPlane(-body.right-body.forward*.25f,d).normalized;
                Vector3 e=a+d*(length*.5f)+bend*Mathf.Sqrt(Mathf.Max(0,.29f*.29f-length*length*.25f));
                PoseSegment(leftUpperArm,a,e,leftUpperLength);PoseSegment(leftForearm,e,b,leftForeLength);
                leftGlove.SetPositionAndRotation(b,body.rotation*Quaternion.Euler(0,0,-20));
            }
        }
        static void PoseSegment(Transform part, Vector3 from, Vector3 to, float meshLength)
        {
            part.SetPositionAndRotation(from, Quaternion.FromToRotation(Vector3.up, to - from));
            part.localScale = new Vector3(1, (to - from).magnitude / Mathf.Max(.01f, meshLength), 1);
        }
    }
}
