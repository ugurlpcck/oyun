using UnityEngine;

namespace CompanySim
{
    [RequireComponent(typeof(BoxCollider),typeof(Rigidbody))]
    public sealed class VehicleDoor : InteractableBehaviour
    {
        public string displayName="Araç kapısı";
        public float openAngle=75,speed=95;
        public ServiceVehicle vehicle;
        public GarageLift lift;
        public bool IsOpen { get; private set; }
        public bool IsBlocked { get; private set; }
        public float CurrentAngle { get; private set; }
        public bool IsClosed => !IsOpen && Mathf.Abs(CurrentAngle)<.1f;
        Quaternion closedRotation;
        Rigidbody body;
        BoxCollider leaf;
        readonly Collider[] overlaps=new Collider[48];
        void Awake()
        {
            body=GetComponent<Rigidbody>(); leaf=GetComponent<BoxCollider>(); closedRotation=transform.localRotation;
            body.isKinematic=true; body.useGravity=false; body.interpolation=RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
        }
        Quaternion Rotation(float angle) => transform.parent.rotation*closedRotation*Quaternion.Euler(0,angle,0);
        void FixedUpdate()
        {
            float target=IsOpen?openAngle:0;
            if(Mathf.Abs(CurrentAngle-target)<.01f) { IsBlocked=false; return; }
            float next=Mathf.MoveTowards(CurrentAngle,target,speed*Time.fixedDeltaTime);
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Abs(next-CurrentAngle)));
            float start=CurrentAngle; IsBlocked=false;
            for(int i=1;i<=steps;i++)
            {
                float candidate=Mathf.Lerp(start,next,i/(float)steps);
                var rotation=Rotation(candidate);
                var center=body.position+rotation*Vector3.Scale(leaf.center,transform.lossyScale);
                int count=Physics.OverlapBoxNonAlloc(center,Vector3.Scale(leaf.size,transform.lossyScale)*.5f+Vector3.one*.015f,overlaps,rotation,~0,QueryTriggerInteraction.Ignore);
                bool blocked=count==overlaps.Length;
                for(int j=0;j<count && !blocked;j++)
                {
                    var obstacle=overlaps[j]; if(obstacle.transform.IsChildOf(vehicle.transform)) continue;
                    if(obstacle is CharacterController controller)
                    {
                        float depthNow=CapsuleDepth(controller,rotation),depthBefore=CapsuleDepth(controller,Rotation(CurrentAngle));
                        if(depthNow>0 && !(depthBefore>0 && depthNow<depthBefore-.0001f)) blocked=true;
                        continue;
                    }
                    if(!Physics.ComputePenetration(leaf,body.position,rotation,obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out float depth)) continue;
                    bool retreat=Physics.ComputePenetration(leaf,body.position,Rotation(CurrentAngle),obstacle,obstacle.transform.position,obstacle.transform.rotation,out _,out float oldDepth) && depth<oldDepth-.0001f;
                    if(!retreat) blocked=true;
                }
                if(blocked) { IsBlocked=true; break; }
                CurrentAngle=candidate;
            }
            body.MoveRotation(Rotation(CurrentAngle));
        }
        float CapsuleDepth(CharacterController controller,Quaternion rotation)
        {
            var scale=controller.transform.lossyScale;
            float radius=controller.radius*Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z))+controller.skinWidth*.5f;
            float segment=Mathf.Max(0,controller.height*Mathf.Abs(scale.y)*.5f-radius);
            var local=Quaternion.Inverse(rotation)*(controller.transform.TransformPoint(controller.center)-body.position)-Vector3.Scale(leaf.center,transform.lossyScale);
            var half=Vector3.Scale(leaf.size,transform.lossyScale)*.5f+Vector3.one*.015f;
            float dx=Mathf.Max(0,Mathf.Abs(local.x)-half.x),dz=Mathf.Max(0,Mathf.Abs(local.z)-half.z),dy=Mathf.Max(0,Mathf.Abs(local.y)-segment-half.y);
            return radius-Mathf.Sqrt(dx*dx+dy*dy+dz*dz);
        }
        public override string Prompt(PlayerInteractor player)
        {
            if(lift && !lift.IsLowered) return "Kapı için lifti indirin";
            return "E — "+displayName+(IsOpen?" kapat":" aç")+(IsBlocked?" · Önü dolu":"");
        }
        public override void Interact(PlayerInteractor player)
        {
            if(lift && !lift.IsLowered) { if(player) player.ui.Toast("Kapıları kullanmadan önce lifti indirin."); return; }
            IsOpen=!IsOpen;
        }
    }
}
