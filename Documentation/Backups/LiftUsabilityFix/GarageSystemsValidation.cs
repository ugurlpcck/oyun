using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Stopwatch=System.Diagnostics.Stopwatch;
using CompanySim;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;

namespace CompanySimEditor
{
    public static class GarageSystemsValidation
    {
        public static bool Finished { get; private set; }
        public static Exception Error { get; private set; }
        static void Check(bool value,string message) {if(!value) throw new Exception("Garage systems: "+message);}
        public static void Begin(PlayerInteractor player) {Finished=false; Error=null; player.StartCoroutine(Drive(Run(player)));}
        static IEnumerator Drive(IEnumerator test)
        {
            yield return Guard(test); Finished=true;
        }
        static IEnumerator Guard(IEnumerator test)
        {
            while(Error==null)
            {
                object next=null; bool more=false;
                try {more=test.MoveNext(); if(more) next=test.Current;} catch(Exception error) {Error=error;}
                if(Error!=null) break;
                if(!more) break;
                if(next is IEnumerator nested) yield return Guard(nested); else yield return next;
            }
        }
        static readonly FieldInfo Pitch=typeof(PlayerInteractor).GetField("pitch",BindingFlags.Instance|BindingFlags.NonPublic);
        internal static void Aim(PlayerInteractor player,Vector3 position,Vector3 target)
        {
            var controller=player.GetComponent<CharacterController>(); controller.enabled=false; player.transform.position=position;
            var dir=target-player.viewCamera.transform.position; float yaw=Mathf.Atan2(dir.x,dir.z)*Mathf.Rad2Deg;
            float pitch=-Mathf.Atan2(dir.y,new Vector2(dir.x,dir.z).magnitude)*Mathf.Rad2Deg;
            player.transform.rotation=Quaternion.Euler(0,yaw,0); Pitch.SetValue(player,pitch); player.viewCamera.transform.localRotation=Quaternion.Euler(pitch,0,0);
            controller.enabled=true; Physics.SyncTransforms();
        }
        static IEnumerator Press(Keyboard keyboard)
        {InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.E)); yield return new WaitForSeconds(.08f); InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;}
        static IEnumerator Run(PlayerInteractor player)
        {
            var initialPosition=player.transform.position; var initialRotation=player.transform.rotation; var initialView=player.viewCamera.transform.localRotation; float initialPitch=(float)Pitch.GetValue(player);
            var lift=UnityEngine.Object.FindAnyObjectByType<GarageLift>(); var vehicle=player.ui.Runtime.vehicle;
            var optimizer=UnityEngine.Object.FindAnyObjectByType<GarageRuntimeOptimization>();
            var keyboard=InputSystem.AddDevice<Keyboard>("GarageSystemsKeyboard"); var mouse=InputSystem.AddDevice<Mouse>("GarageSystemsMouse");
            var background=InputSystem.settings.backgroundBehavior; var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                Check(lift && lift.doors.Length==4 && lift.movingParts.Length>100,"four original vehicle doors and original lift arms/carriages are connected");
                File.WriteAllText("Logs/vehicle_exterior_fix_checks.txt","Vehicle interior / rear hardware regression\n");
                VehicleInteriorValidation.CheckRearOwnership(vehicle);
                VehicleInteriorValidation.CheckInterior(vehicle);
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard.MakeCurrent(); mouse.MakeCurrent(); InputSystem.EnableDevice(keyboard); InputSystem.EnableDevice(mouse);
                player.SetCursor(true); Aim(player,new Vector3(7.45f,.01f,-4.23f),new Vector3(4.65f,1.35f,.1f));
                yield return null;
                var craft=GameObject.Find("15_WorkshopCraft");
                if(craft)
                {
                    Check(craft.GetComponentsInChildren<Collider>().Length==0,"new bench/shelf details introduce no walking collider");
                    Check(craft.GetComponentsInChildren<MeshRenderer>().Length<=5,"crafted workshop details remain combined into five renderers");
                    // Let loose stock settle and the real FPS sampling window advance;
                    // the first loading frame is not a representative play view.
                    for(int i=0;i<60;i++) yield return null;
                    ComputerDesktopValidation.Capture(player.ui,"Documentation/WorkshopCraftPlayView.png",1600,900);
                }
                var beforeFrames=new List<float>(); var afterFrames=new List<float>(); var beforeCalls=new List<long>(); var afterCalls=new List<long>();
                var target=RenderTexture.GetTemporary(1600,900,24);
                using(var recorder=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1))
                {
                    try
                    {
                        for(int i=0;i<6;i++) {Render(player.viewCamera,target); yield return null;}
                        for(int i=0;i<24;i++) {beforeFrames.Add(Render(player.viewCamera,target)); yield return null; if(recorder.Valid && recorder.LastValue>0) beforeCalls.Add(recorder.LastValue);}
                        optimizer.Combine(); Check(optimizer.CombinedRendererCount>1000,"static geometry actually enters batches: "+optimizer.CombinedRendererCount);
                        for(int i=0;i<6;i++) {Render(player.viewCamera,target); yield return null;}
                        for(int i=0;i<24;i++) {afterFrames.Add(Render(player.viewCamera,target)); yield return null; if(recorder.Valid && recorder.LastValue>0) afterCalls.Add(recorder.LastValue);}
                    }
                    finally {RenderTexture.ReleaseTemporary(target);}
                }
                var report=new PerformanceReport {combinedRenderers=optimizer.CombinedRendererCount,beforeMedianMs=Median(beforeFrames),afterMedianMs=Median(afterFrames),
                    beforeMedianDrawCalls=beforeCalls.Count>0?Median(beforeCalls.Select(x=>(float)x).ToList()):-1,afterMedianDrawCalls=afterCalls.Count>0?Median(afterCalls.Select(x=>(float)x).ToList()):-1};
                File.WriteAllText("Logs/garage_performance_comparison.json",JsonUtility.ToJson(report,true));
                Check(lift.doors.All(d=>d.GetComponentsInChildren<MeshRenderer>().All(r=>!r.isPartOfStaticBatch)) && lift.movingParts.All(t=>!t.GetComponent<MeshRenderer>().isPartOfStaticBatch),"doors, hardware and moving lift geometry are excluded from static batches");
                foreach(var door in lift.doors)
                {
                    bool rear=door.name.StartsWith("Rear"); int side=door.name.EndsWith("Right")?-1:1;
                    var start=rear?new Vector3(4.65f,.01f,3.70f):new Vector3(4.65f+side*2.5f,.01f,-1.245f);
                    var leaf=door.GetComponent<BoxCollider>();
                    Aim(player,start,door.transform.TransformPoint(leaf.center)); player.SetCursor(false); yield return null;
                    Check(player.FindTarget()==door,"E reaches "+door.name+", got "+(player.FindTarget()?player.FindTarget().name:"none"));
                    var hardware=door.GetComponentsInChildren<MeshRenderer>().Select(r=>r.transform).ToArray(); var relative=hardware.Select(t=>door.transform.InverseTransformPoint(t.position)).ToArray();
                    yield return Press(keyboard); yield return new WaitForSeconds(rear?2.25f:1.6f);
                    Check(door.IsOpen && Mathf.Abs(door.CurrentAngle-door.openAngle)<.2f,"door opens fully: "+door.name+" angle "+door.CurrentAngle+" blocked "+door.IsBlocked+" by "+door.BlockingObstacle);
                    for(int i=0;i<hardware.Length;i++) Check(Vector3.Distance(relative[i],door.transform.InverseTransformPoint(hardware[i].position))<.002f,"handles/glass/paint remain attached to "+door.name);
                    Check(!lift.RequestMotion(1,player),"open door prevents lift motion");
                    if(door.name=="FrontDoor_Right") ComputerDesktopValidation.Capture(player.ui,"Documentation/VanFrontDoorOpen.png",1600,900);
                    if(door.name=="RearDoor_Left") ComputerDesktopValidation.Capture(player.ui,"Documentation/VanRearDoorOpen.png",1600,900);
                    if(door.name=="FrontDoor_Right") VehicleInteriorValidation.Capture(vehicle,"CabOpenInteriorFixed",new Vector3(-2.2f,1.7f,-.95f),new Vector3(.60f,1.50f,-1.25f));
                    if(door.name.StartsWith("Rear")) VehicleInteriorValidation.Capture(vehicle,door.name+"_HardwareFixed",new Vector3(0,1.65f,4.25f),new Vector3(0,1.5f,2.65f));
                    Aim(player,start,door.transform.TransformPoint(leaf.center)); player.SetCursor(false); yield return null;
                    Check(player.FindTarget()==door,"opened door stays targetable: "+door.name);
                    yield return Press(keyboard); yield return new WaitForSeconds(rear?2.25f:1.6f); Check(door.IsClosed,"door closes again: "+door.name);
                }
                foreach(var rear in lift.doors.Where(d=>d.name.StartsWith("Rear"))) rear.Interact(player);
                yield return new WaitForSeconds(2.25f);
                VehicleInteriorValidation.Capture(vehicle,"CargoOpenInteriorFixed",new Vector3(0,1.65f,3.85f),new Vector3(0,1.48f,.60f));
                Check(!vehicle.transform.Find("ManualModuleBay"),"no module fitting in the rear cargo bay");
                Check(!vehicle.manualModules.CanWork(player,out _),"moving/open carrier door blocks module fitting");
                Aim(player,new Vector3(7.10f,.01f,-2.80f),new Vector3(7.10f,1.58f,1)); player.SetCursor(false);
                foreach(var point in new[]{new Vector3(7.10f,.01f,1.80f),new Vector3(7.10f,.01f,3.69f),new Vector3(4.65f,.01f,3.69f)})
                    yield return WalkTo(player,keyboard,point);
                yield return WalkTo(player,keyboard,new Vector3(4.65f,.66f,1.55f));
                Check(player.transform.position.y>.60f && player.transform.position.y<.74f,"real W steps up into the cargo floor without jumping or a teleport");
                yield return WalkTo(player,keyboard,new Vector3(4.65f,.01f,3.69f));
                foreach(var point in new[]{new Vector3(2.10f,.01f,3.69f),new Vector3(2.10f,.01f,.20f)}) yield return WalkTo(player,keyboard,point);
                VehicleInteriorValidation.Capture(vehicle,"RearDoorWalkwayClear",new Vector3(1.90f,2.10f,3.85f),new Vector3(.25f,1.45f,2.5f));
                foreach(var rear in lift.doors.Where(d=>d.name.StartsWith("Rear"))) rear.Interact(player);
                yield return new WaitForSeconds(2.25f);
                var raise=GameObject.Find("LiftRaiseTarget").GetComponent<LiftButton>(); var lower=GameObject.Find("LiftLowerTarget").GetComponent<LiftButton>();
                var vehicleBase=vehicle.transform.position; var part=lift.movingParts[0]; var partBase=part.position;
                Aim(player,new Vector3(6.26f,.01f,-1.25f),raise.transform.position); player.SetCursor(false); yield return null;
                Check(player.FindTarget()==raise,"E targets the upper lift button, not the module desktop"); yield return Press(keyboard);
                yield return new WaitForSeconds(5);
                Check(!lift.IsMoving && Mathf.Abs(lift.Height-.85f)<.01f,"lift raises the original van to its safe ceiling clearance");
                Check(Vector3.Distance(vehicle.transform.position,vehicleBase+Vector3.up*.85f)<.01f && Mathf.Abs(part.position.y-partBase.y-1.19f)<.01f,"vehicle and original lift arms/carriages move together after pad contact");
                Check(!vehicle.TryChangeModule("towing",player.ui.station),"module swap blocked while raised");
                var firstDoor=lift.doors[0]; firstDoor.Interact(player); Check(!firstDoor.IsOpen,"doors cannot open on the raised vehicle");
                ComputerDesktopValidation.Capture(player.ui,"Documentation/LiftRaised.png",1600,900);
                Aim(player,new Vector3(6.26f,.01f,-1.25f),lower.transform.position); player.SetCursor(false); yield return null;
                Check(player.FindTarget()==lower,"E reaches lower lift button"); yield return Press(keyboard);
                player.SetCursor(true); Aim(player,new Vector3(4.65f,.01f,.5f),Vector3.forward*4+Vector3.up);
                float stoppedHeight=lift.Height; yield return new WaitForSeconds(.25f);
                Check(lift.IsBlocked && Mathf.Abs(lift.Height-stoppedHeight)<.015f,"lowering stops when the player's capsule is under the vehicle");
                Aim(player,vehicleBase+new Vector3(.52f,.01f,3.35f),vehicleBase+new Vector3(.52f,2,2.9f));
                yield return new WaitForSeconds(3.0f);
                Check(lift.IsBlocked && lift.Height>.05f,"lowering also stops for a player under the rear exterior module, outside the original body sweep");
                Aim(player,new Vector3(7.45f,.01f,-4.23f),new Vector3(4.65f,1.35f,.1f)); yield return new WaitForSeconds(5);
                Check(lift.IsLowered && Vector3.Distance(vehicleBase,vehicle.transform.position)<.002f && Vector3.Distance(partBase,part.position)<.002f,"lift resumes safely and returns every part to its original parking pose");
                var feedback=player.ui.audioFeedback; feedback.Click(); feedback.Notice(); feedback.Door(true,player.transform.position); feedback.Drawer(player.transform.position); feedback.Impact(player.transform.position,1);
                Check(UnityEngine.Object.FindObjectsByType<AudioSource>().All(s=>!s.enabled && !s.isPlaying),"removed sounds create no active audio sources");
                var counter=player.ui.GetComponentInChildren<PerformanceCounter>(); int samples=counter.SampleCount;
                player.ui.SetPaused(true); yield return new WaitForSecondsRealtime(.65f);
                Check(counter.SampleCount>samples && counter.FramesPerSecond>0 && counter.FrameMilliseconds>0 && counter.label.text.Contains("FPS"),"FPS/ms counter updates with real unscaled frames, including pause"); player.ui.SetPaused(false);
                File.AppendAllText("Logs/office_garage_validation.txt","PASS: real E vehicle doors including 180-degree rear leaves and attached rear module; real W route around open rear doors and up/down cargo steps; lift, rear carrier/step clearance and player-safe lowering/resume; audio removed, FPS/ms and static batching verified\n");
            }
            finally
            {
                if(player.ui.IsPaused) player.ui.SetPaused(false);
                if(lift) lift.Stop();
                var controller=player.GetComponent<CharacterController>(); controller.enabled=false; player.transform.SetPositionAndRotation(initialPosition,initialRotation); controller.enabled=true;
                Pitch.SetValue(player,initialPitch); player.viewCamera.transform.localRotation=initialView; player.SetCursor(false);
                InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); InputSystem.settings.backgroundBehavior=background; InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;
            }
        }
        static float Median(List<float> values) {values.Sort(); return values[values.Count/2];}
        static IEnumerator WalkTo(PlayerInteractor player,Keyboard keyboard,Vector3 destination)
        {
            Aim(player,player.transform.position,destination+Vector3.up*1.57f); player.SetCursor(false);
            float started=Time.time,limit=Vector3.Distance(player.transform.position,destination)/player.walkSpeed+2.5f;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.W));
            while(new Vector2(player.transform.position.x-destination.x,player.transform.position.z-destination.z).magnitude>.17f)
            {
                if(Time.time-started>=limit)
                {
                    var center=player.GetComponent<CharacterController>().bounds.center;
                    var nearby=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
                        .Where(c=>c.enabled && !c.isTrigger && !c.transform.IsChildOf(player.transform))
                        .Select(c=>new {collider=c,distance=Vector3.Distance(c.ClosestPoint(center),center)})
                        .OrderBy(x=>x.distance).Take(8);
                    Check(false,"real W rear circulation blocked at "+player.transform.position+", target "+destination+"; nearby "+
                        string.Join("; ",nearby.Select(x=>x.collider.name+" d="+x.distance.ToString("F2")+" bounds="+x.collider.bounds)));
                }
                yield return null;
            }
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return new WaitForSeconds(.20f);
        }
        static float Render(Camera camera,RenderTexture target)
        {
            var watch=Stopwatch.StartNew(); RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});
            watch.Stop(); return (float)watch.Elapsed.TotalMilliseconds;
        }
        [Serializable] sealed class PerformanceReport
        {
            public string scope="Explicit 1600x900 camera renders, same-view static-batching A/B. Times are CPU render submission milliseconds; draw calls use ProfilerRecorder (-1 if unavailable). Not standalone GPU/FPS certification.";
            public int combinedRenderers;
            public float beforeMedianMs,afterMedianMs,beforeMedianDrawCalls,afterMedianDrawCalls;
        }
    }
}
