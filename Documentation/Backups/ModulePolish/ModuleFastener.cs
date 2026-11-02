using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CompanySim
{
    public sealed class ModuleFastener : InteractableBehaviour
    {
        public ManualModuleSystem system;
        public int index;
        public Transform boltVisual;
        public Renderer indicator;
        bool working;
        float progress;
        Vector3 restPosition;
        Quaternion restRotation;
        void Awake() {if(boltVisual) {restPosition=boltVisual.localPosition; restRotation=boltVisual.localRotation;}}
        void Start() {system.Changed+=Refresh; Refresh();}
        void OnDestroy() {if(system) system.Changed-=Refresh;}
        void Refresh()
        {
            bool tight=system.Current && (index<0?system.Current.record.connected:(system.Current.record.bolts&(1<<index))!=0);
            if(boltVisual) {boltVisual.localPosition=restPosition+(tight?Vector3.zero:Vector3.forward*.025f); boltVisual.localRotation=restRotation*Quaternion.Euler(0,tight?0:90,0);}
            if(indicator) indicator.material.color=tight?new Color(.20f,.42f,.24f):new Color(.64f,.36f,.13f);
        }
        public override string Prompt(PlayerInteractor player)
        {
            if(!system.Current) return "Önce modülü yuvaya yerleştirin";
            if(!system.CanWork(player,out var reason)) return reason;
            bool tight=index<0?system.Current.record.connected:(system.Current.record.bolts&(1<<index))!=0;
            if(index<0) return "E — Modül bağlantısını "+(tight?"ayır":"bağla");
            if(!player.HeldItem || player.HeldItem.definitionId!="toolbox") return "Cıvatalar için alet çantasını alın";
            return "E basılı tut — Cıvata "+(index+1)+(tight?" sök":" sık")+(working?"  %"+Mathf.RoundToInt(progress*100):"");
        }
        public override void Interact(PlayerInteractor player)
        {
            if(working || !system.Current || !system.CanWork(player,out _)) return;
            if(index<0) {system.ToggleConnection(player); return;}
            if(!player.HeldItem || player.HeldItem.definitionId!="toolbox") {player.ui.Toast("Cıvatalar için alet çantası gerekiyor."); return;}
            StartCoroutine(Turn(player));
        }
        IEnumerator Turn(PlayerInteractor player)
        {
            working=true; progress=0;
            while(progress<1)
            {
                if(Keyboard.current==null || !Keyboard.current[player.interactKey].isPressed || player.InputLocked || player.FindTarget()!=this || !system.CanWork(player,out _)) break;
                progress+=Time.deltaTime/.65f; yield return null;
            }
            if(progress>=1) system.ToggleBolt(index,player);
            working=false; progress=0;
        }
    }
}
