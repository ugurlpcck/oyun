using System.Collections.Generic;
using UnityEngine;

namespace CompanySim
{
    public sealed class GarageLift : MonoBehaviour
    {
        public ServiceVehicle vehicle;
        public Transform[] movingParts;
        public VehicleDoor[] doors;
        public ModuleStation moduleStation;
        public float maximumHeight=.85f,speed=.28f;
        public float Height { get; private set; }
        public float CarriageHeight { get; private set; }
        public bool IsLowered => !IsMoving && CarriageHeight<.005f;
        public bool IsMoving => direction!=0;
        public bool IsBlocked { get; private set; }
        int direction;
        Vector3 vehicleBase;
        BoxCollider exteriorCarrier;
        Vector3[] partBases;
        readonly HashSet<Transform> movable=new HashSet<Transform>();
        readonly Collider[] overlaps=new Collider[80];
        public bool Owns(Transform part) => movable.Contains(part);
        void Awake()
        {
            vehicleBase=vehicle.transform.position; partBases=new Vector3[movingParts.Length];
            var carrier=vehicle.transform.Find("ExteriorModuleCarrier"); if(carrier) exteriorCarrier=carrier.GetComponent<BoxCollider>();
            for(int i=0;i<movingParts.Length;i++) { partBases[i]=movingParts[i].position; movable.Add(movingParts[i]); }
        }
        public bool RequestMotion(int requested,PlayerInteractor player)
        {
            requested=requested>0?1:-1;
            if(direction==requested) { direction=0; IsBlocked=false; return true; }
            if(moduleStation && moduleStation.Busy) { Notify(player,"Modül değişimi bitmeden lift kullanılamaz."); return false; }
            if(vehicle.manualModules && !vehicle.manualModules.IsSecured) {Notify(player,"Lift için modül bağlantılarını tamamlayın."); return false;}
            foreach(var door in doors) if(!door.IsClosed) { Notify(player,"Önce aracın tüm kapılarını kapatın."); return false; }
            if((requested>0 && CarriageHeight>=maximumHeight+.34f-.001f) || (requested<0 && CarriageHeight<=.001f)) return false;
            direction=requested; IsBlocked=false; return true;
        }
        void Notify(PlayerInteractor player,string value) { if(player && player.ui) player.ui.Toast(value); }
        void FixedUpdate()
        {
            if(direction==0) return;
            float carriage=Mathf.Clamp(CarriageHeight+direction*speed*Time.fixedDeltaTime,0,maximumHeight+.34f);
            float next=Mathf.Max(0,carriage-.34f);
            var center=vehicleBase+Vector3.up*(next+1.35f)+vehicle.transform.forward*-.12f;
            IsBlocked=Obstructed(center,new Vector3(1.23f,1.36f,2.93f),vehicle.transform.rotation);
            if(!IsBlocked && exteriorCarrier)
            {
                // Check the side carrier separately. Enlarging the whole van sweep
                // also covers the operator standing at the front lift controls.
                var bounds=exteriorCarrier.bounds;
                IsBlocked=Obstructed(bounds.center+Vector3.up*(next-Height),bounds.extents+Vector3.one*.10f,Quaternion.identity);
            }
            if(IsBlocked) return;
            CarriageHeight=carriage; Height=next; vehicle.transform.position=vehicleBase+Vector3.up*Height;
            for(int i=0;i<movingParts.Length;i++) movingParts[i].position=partBases[i]+Vector3.up*CarriageHeight;
            if(CarriageHeight<=.001f || CarriageHeight>=maximumHeight+.34f-.001f) { direction=0; IsBlocked=false; }
        }
        bool Obstructed(Vector3 center,Vector3 half,Quaternion rotation)
        {
            int count=Physics.OverlapBoxNonAlloc(center,half,overlaps,rotation,~0,QueryTriggerInteraction.Ignore);
            if(count==overlaps.Length) return true;
            for(int i=0;i<count;i++)
            {
                var obstacle=overlaps[i]; if(obstacle.transform.IsChildOf(vehicle.transform) || Owns(obstacle.transform)) continue;
                if(obstacle is CharacterController || obstacle.GetComponentInParent<CarryableItem>()) return true;
            }
            return false;
        }
        public void Stop() { direction=0; IsBlocked=false; }
    }
}
