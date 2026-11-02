using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CompanySim
{
    public sealed class OfficeDoor : InteractableBehaviour
    {
        public float openAngle = 95, speed = 160;
        bool open;
        bool latchPending;
        Quaternion closedRotation;
        float progress,velocity;
        AudioFeedback feedback;
        BoxCollider leaf;
        readonly Collider[] overlaps=new Collider[12];
        void Awake() { closedRotation = transform.localRotation; leaf=GetComponent<BoxCollider>(); }
        void Start() { feedback=FindAnyObjectByType<AudioFeedback>(); }
        void Update()
        {
            float next=Mathf.SmoothDamp(progress,open?1:0,ref velocity,
                Mathf.Max(.12f,Mathf.Abs(openAngle)/speed*.45f));
            var rotation=closedRotation*Quaternion.Euler(0,openAngle*next,0);
            if(!open && leaf)
            {
                var worldRotation=transform.parent?transform.parent.rotation*rotation:rotation;
                var center=transform.position+worldRotation*Vector3.Scale(leaf.center,transform.lossyScale);
                int count=Physics.OverlapBoxNonAlloc(center,Vector3.Scale(leaf.size,transform.lossyScale)*.5f,
                    overlaps,worldRotation,~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)
                    if(overlaps[i].GetComponent<PlayerInteractor>()) { velocity=0; return; }
            }
            progress=next; transform.localRotation=rotation;
            if(latchPending && next<.005f)
            {
                latchPending=false;
                if(feedback) feedback.Door(false,transform.position+Vector3.up);
            }
        }
        public override string Prompt(PlayerInteractor player) => open ? "E — Kapıyı kapat" : "E — Kapıyı aç";
        public override void Interact(PlayerInteractor player)
        {
            open=!open;
            latchPending=!open;
            if(open && feedback) feedback.Door(true,transform.position+Vector3.up);
        }
    }
}
