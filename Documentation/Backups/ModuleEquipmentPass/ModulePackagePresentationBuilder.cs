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
                case "towing": return name.Contains("winch spool");
                case "fire": return name.Contains("hose reel") || name.Contains("reel hub") || name.Contains("gauge");
                case "water": return name.Contains("diesel pump engine") || name.Contains("engine cover") || name.Contains("intake pipe");
                case "tech": return name.Contains("service body");
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
            string path=Art+"/PortableDevice_"+id+".asset"; mesh=GeneratedMeshAsset.Store(mesh,path);
            var go=new GameObject("PortableModuleDevice",typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterials=materials.ToArray();
            Decorate(parent,id,mesh.bounds,renderers,sourceRoot,orientation,bounds.center,scale);
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
        static Bounds MappedBounds(MeshRenderer renderer,Transform sourceRoot,Matrix4x4 orientation,Vector3 center,float scale)
        {
            var matrix=orientation*sourceRoot.worldToLocalMatrix*renderer.transform.localToWorldMatrix;
            var points=renderer.GetComponent<MeshFilter>().sharedMesh.vertices.Select(p=>(matrix.MultiplyPoint3x4(p)-center)*scale+new Vector3(0,.245f,0)).ToArray();
            var bounds=new Bounds(points[0],Vector3.zero); foreach(var point in points) bounds.Encapsulate(point); return bounds;
        }
        static void Decorate(Transform root,string id,Bounds bounds,MeshRenderer[] sources,Transform sourceRoot,Matrix4x4 orientation,Vector3 center,float scale)
        {
            var metal=DetailMaterial("Hardware",new Color(.46f,.51f,.51f),.65f,.40f);
            var rubber=DetailMaterial("Hose",new Color(.065f,.075f,.075f),0,.17f);
            if(id=="towing")
            {
                var cable=DetailMaterial("WoundCable",new Color(.32f,.35f,.34f),.75f,.36f);
                var points=new List<Vector3>(); float radius=bounds.extents.y*1.015f;
                for(int i=0;i<=288;i++) {float t=i/288f,a=t*18*Mathf.PI*2; points.Add(new Vector3(Mathf.Lerp(bounds.min.x+.02f,bounds.max.x-.02f,t),bounds.center.y+Mathf.Cos(a)*radius,bounds.center.z+Mathf.Sin(a)*radius));}
                Tube(root,"WoundRecoveryCable",points,.0045f,cable);
                foreach(int side in new[]{-1,1})
                {
                    Part(root,"WinchBearingSupport",new Vector3(side*(bounds.extents.x-.012f),.16f,0),new Vector3(.032f,.18f,.10f),metal);
                    Cylinder(root,"WinchAxle",new Vector3(side*(bounds.extents.x+.015f),bounds.center.y,bounds.center.z),.028f,.035f,Vector3.right,metal);
                }
            }
            if(id=="fire")
            {
                var source=sources.First(r=>r.name.Replace('_',' ').ToLowerInvariant().Contains("hose reel"));
                var reel=MappedBounds(source,sourceRoot,orientation,center,scale);
                var red=DetailMaterial("ReelEnamel",new Color(.52f,.12f,.075f),.24f,.42f);
                var rim=new List<Vector3>(); for(int i=0;i<=64;i++) {float a=i/64f*Mathf.PI*2; rim.Add(reel.center+new Vector3(Mathf.Cos(a)*reel.extents.x*.94f,Mathf.Sin(a)*reel.extents.y*.94f,reel.extents.z+.008f));}
                Tube(root,"HoseReelRim",rim,.007f,red);
                var spiral=new List<Vector3>();
                for(int i=0;i<=240;i++) {float t=i/240f,a=t*5*Mathf.PI*2,r=Mathf.Lerp(.048f,reel.extents.x*.82f,t); spiral.Add(new Vector3(reel.center.x+Mathf.Cos(a)*r,reel.center.y+Mathf.Sin(a)*r,reel.max.z+.010f));}
                Tube(root,"WoundFireHose",spiral,.007f,rubber);
                foreach(var gauge in sources.Where(r=>r.name.ToLowerInvariant().Contains("gauge")))
                    Gauge(root,MappedBounds(gauge,sourceRoot,orientation,center,scale),metal);
            }
            if(id=="water")
            {
                var source=sources.First(r=>r.name.Replace('_',' ').ToLowerInvariant().Contains("diesel pump"));
                var engine=MappedBounds(source,sourceRoot,orientation,center,scale);
                for(int i=0;i<8;i++)
                {
                    float x=Mathf.Lerp(engine.min.x+.018f,engine.max.x-.018f,i/7f); if(Mathf.Abs(x-engine.center.x)<.038f) continue;
                    Part(root,"PumpCoolingFin",new Vector3(x,engine.center.y,engine.max.z+.006f),new Vector3(.008f,engine.size.y*.68f,.018f),metal);
                }
                var handle=new[]{new Vector3(-.17f,.32f,-.10f),new Vector3(-.17f,.425f,-.10f),new Vector3(.17f,.425f,-.10f),new Vector3(.17f,.32f,-.10f)};
                Tube(root,"PumpCarryHandle",handle,.010f,metal);
                foreach(float x in new[]{-.30f,.30f}) foreach(float z in new[]{-.25f,.25f})
                {
                    float height=Mathf.Max(.014f,engine.min.y-.0725f);
                    Part(root,"PumpIsolationFoot",new Vector3(engine.center.x+x*engine.size.x,.0725f+height*.5f,engine.center.z+z*engine.size.z),new Vector3(.026f,height,.035f),rubber);
                }
                var intake=sources.First(r=>r.name.Replace('_',' ').ToLowerInvariant().Contains("intake pipe"));
                var pipe=MappedBounds(intake,sourceRoot,orientation,center,scale); float radius=Mathf.Min(pipe.size.x,pipe.size.y)*.5f;
                var coupling=new WorkshopCraftBuilder.Geometry();
                coupling.Cylinder(new Vector3(pipe.center.x,pipe.center.y,pipe.max.z+.002f),radius*.97f,.003f,rubber,Vector3.forward,40);
                var ring=new WorkshopCraftBuilder.Geometry();
                ring.Lathe(Vector3.zero,new[]{new Vector2(0,radius*.75f),new Vector2(.018f,radius*.75f),new Vector2(.018f,radius*1.09f),
                    new Vector2(.014f,radius*1.18f),new Vector2(.004f,radius*1.18f),new Vector2(0,radius*1.05f)},metal,48);
                coupling.Append(ring,Matrix4x4.TRS(new Vector3(pipe.center.x,pipe.center.y,pipe.max.z),Quaternion.FromToRotation(Vector3.right,Vector3.forward),Vector3.one));
                AttachGeometry(root,"PumpHoseCoupling",coupling);
            }
            if(id=="tech")
            {
                var grey=DetailMaterial("ToolDrawerEnamel",new Color(.40f,.46f,.47f),.30f,.36f);
                for(int i=0;i<3;i++)
                {
                    float y=Mathf.Lerp(bounds.min.y+.04f,bounds.max.y-.04f,i/2f),z=bounds.max.z+.012f;
                    Part(root,"ToolDrawerFace",new Vector3(0,y,z),new Vector3(bounds.size.x-.045f,.065f,.018f),grey);
                    Tube(root,"DrawerGrip",new[]{new Vector3(-.085f,y,z+.012f),new Vector3(-.085f,y,z+.034f),new Vector3(.085f,y,z+.034f),new Vector3(.085f,y,z+.012f)},.005f,metal);
                }
            }
            if(id=="cargo")
            {
                var webbing=DetailMaterial("RetentionWebbing",new Color(.31f,.255f,.16f),0,.15f);
                // The carrying handle is the highest point of the complete case;
                // straps must touch the broad lid beneath it, not bridge over air.
                var surfaces=sources.Select(s=>MappedBounds(s,sourceRoot,orientation,center,scale)).ToArray();
                float area=surfaces.Max(b=>b.size.x*b.size.z);
                var lid=surfaces.Where(b=>b.size.x*b.size.z>=area*.92f).OrderByDescending(b=>b.max.y).First();
                foreach(float x in new[]{-.19f,.19f})
                {
                    float top=lid.max.y+.003f,z=lid.max.z+.006f,bottom=.105f;
                    Part(root,"WebbingTop",new Vector3(x,top,lid.center.z),new Vector3(.025f,.006f,lid.size.z+.018f),webbing);
                    Part(root,"WebbingFront",new Vector3(x,(top+bottom)*.5f,z),new Vector3(.025f,top-bottom,.009f),webbing);
                    Part(root,"WebbingBuckle",new Vector3(x,.155f,z+.009f),new Vector3(.045f,.038f,.014f),metal);
                }
            }
        }
        static void AttachGeometry(Transform root,string name,WorkshopCraftBuilder.Geometry geometry)
        {
            var mesh=geometry.Build(out var materials); mesh.name=name;
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterials=materials;
        }
        static void Gauge(Transform root,Bounds instrument,Material metal)
        {
            var paper=DetailMaterial("GaugeDial",new Color(.79f,.77f,.67f),0,.22f);
            var ink=DetailMaterial("GaugeMark",new Color(.06f,.075f,.073f),0,.18f);
            var red=DetailMaterial("GaugeNeedle",new Color(.48f,.09f,.055f),.10f,.30f);
            float r=Mathf.Min(instrument.size.x,instrument.size.y)*.43f;
            var face=new Vector3(instrument.center.x,instrument.center.y,instrument.max.z+.0016f);
            var g=new WorkshopCraftBuilder.Geometry(); g.Cylinder(face,r,.0024f,paper,Vector3.forward,48);
            for(int i=0;i<11;i++)
            {
                var tick=new WorkshopCraftBuilder.Geometry(); tick.Box(new Vector3(0,r*.78f,0),new Vector3(r*.045f,r*(i%5==0?.18f:.11f),.0008f),ink,.0001f);
                g.Append(tick,Matrix4x4.TRS(face+Vector3.forward*.0018f,Quaternion.Euler(0,0,-120+i*24),Vector3.one));
            }
            var needle=new WorkshopCraftBuilder.Geometry(); needle.Box(new Vector3(0,r*.24f,0),new Vector3(r*.055f,r*.90f,.0012f),red,.0002f);
            g.Append(needle,Matrix4x4.TRS(face+Vector3.forward*.0024f,Quaternion.Euler(0,0,-38),Vector3.one));
            g.Cylinder(face+Vector3.forward*.003f,r*.10f,.002f,metal,Vector3.forward,20);
            AttachGeometry(root,"AnalogGaugeFace",g);
        }
        static Material DetailMaterial(string key,Color color,float metallic,float smoothness)
        {
            string path=Art+"/PortableDetail_"+key+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color); material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",smoothness); EditorUtility.SetDirty(material); return material;
        }
        static void Cylinder(Transform parent,string name,Vector3 position,float radius,float length,Vector3 axis,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=position;
            go.transform.localRotation=Quaternion.FromToRotation(Vector3.up,axis); go.transform.localScale=new Vector3(radius*2,length*.5f,radius*2);
            go.GetComponent<Renderer>().sharedMaterial=material; Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        static void Tube(Transform parent,string name,IEnumerable<Vector3> path,float radius,Material material)
        {
            var points=path.ToArray(); var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var uv=new List<Vector2>(); var indices=new List<int>(); const int sides=8;
            for(int i=0;i<points.Length;i++)
            {
                var tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;
                var side=Vector3.Cross(tangent,Mathf.Abs(tangent.y)>.95f?Vector3.right:Vector3.up).normalized; var up=Vector3.Cross(side,tangent).normalized;
                for(int j=0;j<sides;j++) {float a=j/(float)sides*Mathf.PI*2; var n=up*Mathf.Cos(a)+side*Mathf.Sin(a); vertices.Add(points[i]+n*radius); normals.Add(n); uv.Add(new Vector2(i/(float)(points.Length-1),j/(float)sides));}
                if(i==0) continue;
                for(int j=0;j<sides;j++) {int a=(i-1)*sides+j,b=i*sides+j,c=i*sides+(j+1)%sides,d=(i-1)*sides+(j+1)%sides; indices.AddRange(new[]{a,c,b,a,d,c});}
            }
            var mesh=new Mesh {name=name,indexFormat=IndexFormat.UInt32}; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.SetTriangles(indices,0); mesh.RecalculateBounds(); mesh.RecalculateTangents();
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        static void MergeFixtures(Transform root,string id)
        {
            var renderers=root.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name!="PortableModuleDevice" && r.name!="ServicePort").ToArray();
            var merged=new List<Mesh>(); var materials=new List<Material>();
            var pieces=renderers.SelectMany(r=>Enumerable.Range(0,r.GetComponent<MeshFilter>().sharedMesh.subMeshCount)
                .Select(sub=>new {renderer=r,sub,material=r.sharedMaterials[sub]}));
            foreach(var group in pieces.GroupBy(p=>p.material))
            {
                var combines=group.Select(p=>new CombineInstance {mesh=p.renderer.GetComponent<MeshFilter>().sharedMesh,subMeshIndex=p.sub,transform=root.worldToLocalMatrix*p.renderer.transform.localToWorldMatrix}).ToArray();
                var part=new Mesh {indexFormat=IndexFormat.UInt32}; part.CombineMeshes(combines,true,true); merged.Add(part); materials.Add(group.Key);
            }
            var mesh=new Mesh {name="PortableFixtures_"+id,indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(merged.Select(m=>new CombineInstance {mesh=m,transform=Matrix4x4.identity}).ToArray(),false,true);
            string path=Art+"/PortableFixtures_"+id+".asset"; mesh=GeneratedMeshAsset.Store(mesh,path);
            foreach(var part in merged) Object.DestroyImmediate(part);
            foreach(var renderer in renderers)
            {
                var temporary=renderer.GetComponent<MeshFilter>().sharedMesh;
                Object.DestroyImmediate(renderer.gameObject);
                if(temporary && !EditorUtility.IsPersistent(temporary)) Object.DestroyImmediate(temporary);
            }
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
