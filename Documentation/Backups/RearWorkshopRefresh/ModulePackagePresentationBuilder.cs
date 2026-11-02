using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompanySim;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace CompanySimEditor
{
    static class ModulePackagePresentationBuilder
    {
        const string Art="Assets/_Project/Art/VehicleMechanics";
        const string Prefabs="Assets/_Project/Prefabs/Modules";
        public static ModulePackageVisual[] Build(CompanyCatalog catalog)
        {
            var result=new List<ModulePackageVisual>(); Directory.CreateDirectory(Art);
            foreach(var definition in catalog.modules)
            {
                var source=definition.id=="cargo"?catalog.Item("case").visualPrefab:definition.visualPrefab;
                var content=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source));
                try
                {
                    var candidates=content.GetComponentsInChildren<MeshRenderer>().Where(r=>!r.name.StartsWith("COL_")).ToArray();
                    var selected=candidates.Where(r=>Select(definition.id,r,content.transform)).ToArray();
                    if(selected.Length==0) throw new Exception("No source parts for portable module "+definition.id+": "+string.Join(", ",candidates.Select(r=>r.name)));
                    var root=new GameObject("ServiceModule_"+definition.id);
                    try
                    {
                        var collider=root.AddComponent<BoxCollider>(); collider.center=new Vector3(0,.22f,0); collider.size=new Vector3(.66f,.44f,.46f);
                        var body=root.AddComponent<Rigidbody>(); body.mass=24; body.interpolation=RigidbodyInterpolation.Interpolate; body.useGravity=true;
                        var item=root.AddComponent<CarryableItem>(); item.definitionId="module:"+definition.id;
                        root.AddComponent<ManualModulePackage>();
                        BuildMechanism(root.transform,content.transform,selected,definition.id);
                        var steel=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Module_MountSteel.mat");
                        var rubber=AssetDatabase.LoadAssetAtPath<Material>(Art+"/Module_ConnectorRubber.mat");
                        foreach(int side in new[]{-1,1})
                        {
                            Part(root.transform,"SkidFoot_"+side,new Vector3(side*.25f,.025f,0),new Vector3(.055f,.05f,.38f),rubber);
                            Part(root.transform,"CarrierEdge_"+side,new Vector3(side*.31f,.22f,-.15f),new Vector3(.025f,.37f,.035f),steel);
                        }
                        Part(root.transform,"DeviceBase",new Vector3(0,.055f,0),new Vector3(.64f,.035f,.40f),steel);
                        Part(root.transform,"DeviceBackPanel",new Vector3(0,.24f,-.167f),new Vector3(.62f,.335f,.025f),steel);
                        Part(root.transform,"ServicePortBlock",new Vector3(-.29f,.18f,.075f),new Vector3(.055f,.27f,.27f),steel);
                        var port=GameObject.CreatePrimitive(PrimitiveType.Cylinder); port.name="ServicePort"; port.transform.SetParent(root.transform,false);
                        port.transform.localPosition=new Vector3(-.29f,.27f,.20f); port.transform.localRotation=Quaternion.Euler(90,0,0); port.transform.localScale=new Vector3(.045f,.025f,.045f);
                        port.GetComponent<Renderer>().sharedMaterial=rubber; Object.DestroyImmediate(port.GetComponent<Collider>());
                        MergeFixtures(root.transform,definition.id);
                        Label(root.transform,definition.displayName);
                        var path=Prefabs+"/ServiceModule_"+definition.id+".prefab";
                        var prefab=PrefabUtility.SaveAsPrefabAsset(root,path);
                        result.Add(new ModulePackageVisual {moduleId=definition.id,prefab=prefab});
                    }
                    finally {Object.DestroyImmediate(root);}
                }
                finally {PrefabUtility.UnloadPrefabContents(content);}
            }
            return result.ToArray();
        }
        static bool Select(string id,MeshRenderer renderer,Transform root)
        {
            string name=renderer.name.Replace('_',' ').ToLowerInvariant();
            switch(id)
            {
                case "cargo": return true;
                case "towing": return name.Contains("winch pedestal") || name.Contains("winch spool");
                case "fire": return name.Contains("hose reel") || name.Contains("reel hub") || name.Contains("gauge");
                case "water": return name.Contains("diesel pump engine") || name.Contains("engine cover") || name.Contains("intake pipe");
                case "tech": return name.Contains("service body") || name.Contains("roof skin") ||
                    ((name.Contains("tool locker shutter") || name.Contains("locker pull")) && root.InverseTransformPoint(renderer.bounds.center).x<0);
                default: return false;
            }
        }
        static void BuildMechanism(Transform parent,Transform sourceRoot,MeshRenderer[] renderers,string id)
        {
            var combines=new List<CombineInstance>(); var materials=new List<Material>();
            var orientation=Matrix4x4.Rotate(id=="tech"?Quaternion.Euler(0,90,0):Quaternion.identity);
            foreach(var renderer in renderers)
            {
                var filter=renderer.GetComponent<MeshFilter>(); if(!filter || !filter.sharedMesh) continue;
                for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)
                {
                    if(filter.sharedMesh.GetIndexCount(sub)==0) continue;
                    combines.Add(new CombineInstance {mesh=filter.sharedMesh,subMeshIndex=sub,transform=orientation*sourceRoot.worldToLocalMatrix*filter.transform.localToWorldMatrix});
                    materials.Add(Tone(renderer.sharedMaterials[sub]));
                }
            }
            var mesh=new Mesh {name="PortableDevice_"+id,indexFormat=IndexFormat.UInt32}; mesh.CombineMeshes(combines.ToArray(),false,true); mesh.RecalculateBounds();
            var bounds=mesh.bounds;
            float scale=Mathf.Min(.58f/bounds.size.x,.33f/bounds.size.y,.32f/bounds.size.z);
            var vertices=mesh.vertices;
            for(int i=0;i<vertices.Length;i++) vertices[i]=(vertices[i]-bounds.center)*scale+new Vector3(0,.245f,0);
            mesh.vertices=vertices; mesh.RecalculateBounds();
            string path=Art+"/PortableDevice_"+id+".asset"; var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old) {EditorUtility.CopySerialized(mesh,old); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(old); mesh=old;} else AssetDatabase.CreateAsset(mesh,path);
            var go=new GameObject("PortableModuleDevice",typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
            // Gauges in the original fire body were attached to a full-sized panel.
            // Give the extracted instruments real brackets down to the portable skid.
            int support=0;
            foreach(var renderer in renderers.Where(r=>r.name.ToLowerInvariant().Contains("gauge")))
            {
                var matrix=orientation*sourceRoot.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
                var points=renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>(matrix.MultiplyPoint3x4(p)-bounds.center)*scale+new Vector3(0,.245f,0)).ToArray();
                var instrument=new Bounds(points[0],Vector3.zero); foreach(var point in points) instrument.Encapsulate(point);
                float height=Mathf.Max(.02f,instrument.min.y-.0725f+.015f);
                Part(parent,"InstrumentBracket_"+support++,new Vector3(instrument.center.x,.0725f+height*.5f,instrument.center.z),new Vector3(.028f,height,.045f),AssetDatabase.LoadAssetAtPath<Material>(Art+"/Module_MountSteel.mat"));
            }
        }
        static void MergeFixtures(Transform root,string id)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name!="PortableModuleDevice" && r.name!="ServicePort").ToArray();
            var merged=new List<Mesh>(); var materials=new List<Material>();
            foreach(var group in renderers.GroupBy(r=>r.sharedMaterial))
            {
                var combines=group.Select(r=>new CombineInstance {mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=root.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray();
                var part=new Mesh {indexFormat=IndexFormat.UInt32}; part.CombineMeshes(combines,true,true); merged.Add(part); materials.Add(group.Key);
            }
            var mesh=new Mesh {name="PortableFixtures_"+id,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(merged.Select(m=>new CombineInstance {mesh=m,transform=Matrix4x4.identity}).ToArray(),false,true);
            string path=Art+"/PortableFixtures_"+id+".asset"; var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(old) {EditorUtility.CopySerialized(mesh,old); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(old); mesh=old;} else AssetDatabase.CreateAsset(mesh,path);
            foreach(var part in merged) Object.DestroyImmediate(part);
            foreach(var renderer in renderers) Object.DestroyImmediate(renderer.gameObject);
            var go=new GameObject("ModuleSkidAndBrackets",typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
        }
        static Material Tone(Material source)
        {
            string key=new string(source.name.Select(c=>char.IsLetterOrDigit(c)?c:'_').ToArray()); string path=Art+"/Portable_"+key+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(source); AssetDatabase.CreateAsset(material,path);}
            var color=source.GetColor("_BaseColor"); material.SetColor("_BaseColor",Color.Lerp(color,new Color(.35f,.37f,.36f),.18f));
            material.SetFloat("_Smoothness",.32f); EditorUtility.SetDirty(material); return material;
        }
        static void Part(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=WorkshopOverhaulBuilder.BoxMesh(size,Mathf.Min(.004f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.18f));
            go.GetComponent<Renderer>().sharedMaterial=material;
        }
        static void Label(Transform parent,string name)
        {
            var go=new GameObject("ModuleLabel",typeof(RectTransform),typeof(Canvas)); go.transform.SetParent(parent,false);
            go.transform.localPosition=new Vector3(0,.095f,.232f); go.transform.localRotation=Quaternion.Euler(0,180,0); go.transform.localScale=Vector3.one*.001f;
            go.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace; go.GetComponent<RectTransform>().sizeDelta=new Vector2(540,60);
            var paper=new GameObject("PrintedLabel",typeof(RectTransform),typeof(Image)); paper.transform.SetParent(go.transform,false);
            Stretch(paper.GetComponent<RectTransform>()); paper.GetComponent<Image>().color=new Color(.84f,.84f,.77f); paper.GetComponent<Image>().raycastTarget=false;
            var text=new GameObject("ModuleName",typeof(RectTransform),typeof(Text)); text.transform.SetParent(paper.transform,false); Stretch(text.GetComponent<RectTransform>());
            var caption=text.GetComponent<Text>(); caption.text=name; caption.font=Resources.Load<Font>("Fonts/SourceSans3-Semibold"); caption.fontSize=28;
            caption.alignment=TextAnchor.MiddleCenter; caption.color=new Color(.12f,.15f,.16f); caption.raycastTarget=false;
        }
        static void Stretch(RectTransform rect) {rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;}
    }
}
