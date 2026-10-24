using UnityEngine;
namespace DuskYard
{
    // Lightweight visual gait. Physics/controller and hit timing stay in CombatDemo.
    public sealed class GuardGait : MonoBehaviour
    {
        public Transform left, right;
        public Transform leftCalf,rightCalf;
        Vector3 previous; float phase, amount;
        void Start() { previous=transform.position; }
        void LateUpdate()
        {
            Vector3 delta=transform.position-previous; delta.y=0; previous=transform.position;
            float speed=delta.magnitude/Mathf.Max(Time.deltaTime,.0001f);
            amount=Mathf.MoveTowards(amount,Mathf.Clamp01(speed/3.5f),Time.deltaTime*6);
            phase+=delta.magnitude*7;
            float angle=Mathf.Sin(phase)*25*amount;
            if(left!=null)left.localRotation=Quaternion.Euler(angle,0,0);
            if(right!=null)right.localRotation=Quaternion.Euler(-angle,0,0);
            if(leftCalf!=null)leftCalf.localRotation=Quaternion.Euler(Mathf.Max(0,-Mathf.Sin(phase))*36*amount,0,0);
            if(rightCalf!=null)rightCalf.localRotation=Quaternion.Euler(Mathf.Max(0,Mathf.Sin(phase))*36*amount,0,0);
        }
    }
}
