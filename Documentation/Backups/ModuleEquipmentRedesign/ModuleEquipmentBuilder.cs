using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompanySim;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;
using Geometry=CompanySimEditor.WorkshopCraftBuilder.Geometry;

namespace CompanySimEditor
{
    public static class ModuleEquipmentBuilder
    {
        const string Folder="Assets/_Project/Art/ModuleEquipment";
        const string Prefabs="Assets/_Project/Prefabs/Modules";
        static Material metal,dark,paint,rubber,cable,red,brass,ivory,green,webbing;
        [MenuItem("Company/Rebuild Portable Module Models")]
        public static void Apply()
        {
            if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before editing equipment assets.");
            EditorSceneManager.OpenScene(GarageSystemsBuilder.ScenePath);
            var vehicle=Object.FindFirstObjectByType<ServiceVehicle>();string before=VehicleState(vehicle);
            var runtime=Object.FindFirstObjectByType<CompanyRuntime>();
            runtime.manualModules.packageVisuals=Build(runtime.catalog);
            foreach(var variant in runtime.manualModules.packageVisuals)
            {
                Render(variant.prefab,"Documentation/ModuleEquipmentAfter_"+variant.moduleId+".png",false);
                Render(variant.prefab,"Assets/_Project/Resources/OfficeThumbnails/"+variant.moduleId+".png",true);
            }
            if(VehicleState(vehicle)!=before)throw new Exception("Equipment art must not change the van's geometry, materials or transforms.");
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            File.WriteAllText("Logs/module_equipment_vehicle_parity.txt","PASS: van child transforms, mesh references, material references and parking pose unchanged.\n");
        }
        public static void ApplyAndValidate(){Apply();ProjectValidation.Run();}
        static string VehicleState(ServiceVehicle vehicle)
        {
            string PathOf(Transform t)=>t==vehicle.transform?t.name:PathOf(t.parent)+"/"+t.name;
            return string.Join("\n",vehicle.GetComponentsInChildren<Transform>(true).OrderBy(PathOf).Select(t=>PathOf(t)+"|"+t.localPosition.ToString("R")+"|"+t.localRotation.ToString("R")+"|"+t.localScale.ToString("R")))+
                string.Join("\n",vehicle.GetComponentsInChildren<MeshFilter>(true).OrderBy(f=>PathOf(f.transform)).Select(f=>PathOf(f.transform)+"|"+AssetDatabase.GetAssetPath(f.sharedMesh)))+
                string.Join("\n",vehicle.GetComponentsInChildren<MeshRenderer>(true).OrderBy(r=>PathOf(r.transform)).Select(r=>PathOf(r.transform)+"|"+string.Join(",",r.sharedMaterials.Select(AssetDatabase.GetAssetPath))));
        }
        internal static ModulePackageVisual[] Build(CompanyCatalog catalog)
        {
            Directory.CreateDirectory(Folder+"/Meshes");Directory.CreateDirectory(Folder+"/Materials");PrepareMaterials();
            var result=new List<ModulePackageVisual>();var summary=new List<string>();
            foreach(var definition in catalog.modules)
            {
                var root=new GameObject("ServiceModule_"+definition.id);
                try
                {
                    var collision=root.AddComponent<BoxCollider>();collision.center=new Vector3(0,.22f,0);collision.size=new Vector3(.66f,.44f,.46f);
                    var body=root.AddComponent<Rigidbody>();body.mass=24;body.interpolation=RigidbodyInterpolation.Interpolate;body.useGravity=true;
                    root.AddComponent<CarryableItem>().definitionId="module:"+definition.id;root.AddComponent<ManualModulePackage>();
                    Geometry mechanism;
                    switch(definition.id)
                    {
                        case "towing":mechanism=Winch();break;
                        case "fire":mechanism=FireReel();break;
                        case "water":mechanism=WaterPump();break;
                        case "tech":mechanism=ServiceUnit();break;
                        case "cargo":mechanism=TransitCase();break;
                        default:throw new Exception("Unknown equipment type: "+definition.id);
                    }
                    var device=Attach(root.transform,"PortableModuleDevice",mechanism,definition.id+"_Device");
                    var bounds=device.GetComponent<MeshFilter>().sharedMesh.bounds;
                    if(bounds.min.y<.072f||bounds.max.y>.441f||Mathf.Abs(bounds.min.x)>.322f||Mathf.Abs(bounds.max.x)>.322f||bounds.min.z<-.222f||bounds.max.z>.231f)
                        throw new Exception("Equipment must fit the existing portable collision, "+definition.id+": "+bounds);
                    Attach(root.transform,"ModuleSkidAndBrackets",Skid(),definition.id+"_Skid");
                    var port=Attach(root.transform,"ServicePort",ServicePort(),definition.id+"_Port");port.transform.localPosition=new Vector3(-.29f,.27f,.20f);
                    Label(root.transform,definition.displayName);
                    var prefab=PrefabUtility.SaveAsPrefabAsset(root,Prefabs+"/ServiceModule_"+definition.id+".prefab");
                    result.Add(new ModulePackageVisual{moduleId=definition.id,prefab=prefab});
                    summary.Add(definition.id+": "+device.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3+" mechanism triangles; "+bounds);
                }
                finally{Object.DestroyImmediate(root);}
            }
            File.WriteAllLines("Logs/module_equipment_geometry.txt",new[]{"Five new complete mechanisms; three MeshRenderers per module. Original physical box, mass, item IDs, port position and manual fitting interface retained."}.Concat(summary));
            return result.ToArray();
        }
        static void PrepareMaterials()
        {
            metal=Surface("SatinSteel",new Color(.52f,.56f,.55f),.68f,.42f,"Steel");
            dark=Surface("MachinedRecess",new Color(.075f,.092f,.095f),.45f,.29f,"Steel");
            paint=Surface("GraphiteEnamel",new Color(.12f,.155f,.16f),.22f,.32f,"Enamel");
            rubber=Surface("Rubber",new Color(.043f,.050f,.049f),0,.18f,"Rubber");
            cable=Surface("WireRope",new Color(.35f,.39f,.38f),.70f,.40f,"Steel");
            red=Surface("ReelRed",new Color(.43f,.070f,.043f),.20f,.32f,"Enamel");
            brass=Surface("ValveBrass",new Color(.48f,.35f,.16f),.73f,.40f,"Steel");
            ivory=Surface("InstrumentIvory",new Color(.77f,.77f,.69f),0,.24f,null);
            green=Surface("EquipmentEnamel",new Color(.23f,.29f,.255f),.15f,.29f,"Enamel");
            webbing=Surface("Webbing",new Color(.25f,.27f,.20f),0,.12f,"Cloth");
        }
        static Material Surface(string name,Color color,float metallic,float smoothness,string map)
        {
            string path=Folder+"/Materials/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!m){m=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_BaseColor",color);m.SetFloat("_Metallic",metallic);m.SetFloat("_Smoothness",smoothness);
            m.SetFloat("_Surface",0);m.SetFloat("_Cull",2);m.SetFloat("_ZWrite",1);
            if(map!=null)
            {
                string textures=map=="Cloth"?"Assets/_Project/Art/WorkshopCraft/Textures/":"Assets/_Project/Art/WorkshopFinish/Textures/";
                m.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+map+"_Albedo.png"));
                m.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(textures+map+"_Normal.png"));m.SetFloat("_BumpScale",.14f);
            }
            BaseShaderGUI.SetMaterialKeywords(m,LitGUI.SetMaterialKeywords);EditorUtility.SetDirty(m);return m;
        }
        static GameObject Attach(Transform root,string name,Geometry geometry,string key)
        {
            var mesh=geometry.Build(out var materials);mesh.name=key;
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);
            go.GetComponent<MeshFilter>().sharedMesh=GeneratedMeshAsset.Store(mesh,Folder+"/Meshes/"+key+".asset");go.GetComponent<MeshRenderer>().sharedMaterials=materials;return go;
        }
        static void Axial(Geometry g,Vector3 center,Vector2[] profile,Material material,Vector3 axis,int segments=48)
        {var shape=new Geometry();shape.Lathe(Vector3.zero,profile,material,segments);g.Append(shape,Matrix4x4.TRS(center,Quaternion.FromToRotation(Vector3.right,axis),Vector3.one));}
        static void Rod(Geometry g,Vector3 a,Vector3 b,float radius,Material material,int sides=24)
            =>g.Cylinder((a+b)*.5f,radius,Vector3.Distance(a,b),material,(b-a).normalized,sides);
        static void Ring(Geometry g,Vector3 center,float radius,float thickness,Material material,Vector3 axis,int segments=48)
        {
            var profile=Enumerable.Range(0,10).Select(i=>{float a=i*Mathf.PI/5;return new Vector2(Mathf.Cos(a)*thickness,radius+Mathf.Sin(a)*thickness);}).ToArray();
            Axial(g,center,profile,material,axis,segments);
        }
        static void Bolt(Geometry g,Vector3 center,Vector3 axis,float radius=.004f)
        {g.Cylinder(center,radius*1.35f,.0015f,metal,axis,24);g.Cylinder(center+axis*.0023f,radius,.0038f,dark,axis,6);}
        static Geometry Skid()
        {
            var g=new Geometry();g.Box(new Vector3(0,.055f,0),new Vector3(.64f,.035f,.40f),paint,.008f);
            foreach(int side in new[]{-1,1})
            {
                g.Box(new Vector3(side*.25f,.025f,0),new Vector3(.055f,.05f,.38f),rubber,.008f);
                g.Box(new Vector3(side*.295f,.089f,-.02f),new Vector3(.021f,.040f,.33f),paint,.005f);
                foreach(float z in new[]{-.14f,.14f})Bolt(g,new Vector3(side*.285f,.078f,z),Vector3.up,.005f);
            }
            g.Box(new Vector3(0,.105f,-.17f),new Vector3(.59f,.048f,.018f),paint,.005f);
            g.Box(new Vector3(-.29f,.244f,.165f),new Vector3(.050f,.103f,.021f),paint,.005f);
            g.Box(new Vector3(-.29f,.134f,.155f),new Vector3(.025f,.135f,.025f),paint,.005f);
            foreach(float y in new[]{.207f,.281f})Bolt(g,new Vector3(-.29f,y,.178f),Vector3.forward,.003f);
            return g;
        }
        static Geometry ServicePort()
        {
            var g=new Geometry();
            Axial(g,Vector3.zero,new[]{new Vector2(-.024f,.010f),new Vector2(-.024f,.020f),new Vector2(-.017f,.023f),
                new Vector2(.017f,.023f),new Vector2(.023f,.019f),new Vector2(.023f,.011f)},metal,Vector3.forward,40);
            g.Cylinder(new Vector3(0,0,.005f),.011f,.012f,rubber,Vector3.forward,32);
            Ring(g,new Vector3(0,0,.019f),.018f,.002f,rubber,Vector3.forward,40);
            return g;
        }
        static Geometry Winch()
        {
            var g=new Geometry();var c=new Vector3(.022f,.218f,-.010f);
            // Drum and chamfered cheeks are separate from the motor and reduction housing.
            g.Cylinder(c,.052f,.228f,dark,Vector3.right,64);
            foreach(float x in new[]{-.098f,.143f})
            {
                Axial(g,new Vector3(x,c.y,c.z),new[]{new Vector2(-.006f,.018f),new Vector2(-.006f,.080f),new Vector2(-.003f,.086f),
                    new Vector2(.003f,.086f),new Vector2(.006f,.080f),new Vector2(.006f,.018f)},metal,Vector3.right,64);
                g.Box(new Vector3(x,.133f,c.z),new Vector3(.023f,.121f,.079f),paint,.007f);
                foreach(float z in new[]{-.028f,.028f})Bolt(g,new Vector3(x,.092f,c.z+z),Vector3.up,.004f);
            }
            var rope=new List<Vector3>();
            for(int i=0;i<=720;i++){float t=i/720f,a=t*30*Mathf.PI*2+Mathf.PI*.5f;rope.Add(new Vector3(Mathf.Lerp(-.085f,.130f,t),c.y+Mathf.Sin(a)*.061f,c.z+Mathf.Cos(a)*.061f));}
            Tube(g,rope,.0033f,cable,8);
            Axial(g,new Vector3(-.187f,c.y,c.z),new[]{new Vector2(-.080f,0),new Vector2(-.080f,.038f),new Vector2(-.068f,.049f),
                new Vector2(.060f,.049f),new Vector2(.073f,.034f),new Vector2(.073f,0)},paint,Vector3.right,48);
            for(int i=0;i<7;i++)Axial(g,new Vector3(-.239f+i*.014f,c.y,c.z),new[]{new Vector2(-.002f,.049f),new Vector2(.002f,.049f),new Vector2(.002f,.053f),new Vector2(-.002f,.053f)},dark,Vector3.right,40);
            g.Box(new Vector3(-.185f,.282f,-.011f),new Vector3(.065f,.029f,.048f),paint,.007f);
            g.Box(new Vector3(-.184f,.299f,-.008f),new Vector3(.021f,.009f,.022f),rubber,.003f);
            g.Box(new Vector3(-.190f,.121f,c.z),new Vector3(.095f,.097f,.070f),paint,.007f);
            Axial(g,new Vector3(.211f,c.y,c.z),new[]{new Vector2(-.054f,0),new Vector2(-.054f,.065f),new Vector2(-.041f,.080f),
                new Vector2(.035f,.080f),new Vector2(.048f,.065f),new Vector2(.048f,0)},paint,Vector3.right,64);
            g.Cylinder(new Vector3(.261f,c.y,c.z),.056f,.007f,metal,Vector3.right,48);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Bolt(g,new Vector3(.266f,c.y+Mathf.Cos(a)*.064f,c.z+Mathf.Sin(a)*.064f),Vector3.right);}
            g.Box(new Vector3(.211f,.133f,c.z),new Vector3(.080f,.121f,.085f),paint,.010f);
            g.Cylinder(new Vector3(.214f,.313f,c.z),.011f,.044f,metal,Vector3.up,32);
            Rod(g,new Vector3(.214f,.333f,c.z),new Vector3(.250f,.345f,c.z),.007f,dark);
            g.Cylinder(new Vector3(.253f,.348f,c.z),.010f,.027f,rubber,Vector3.up,32);
            foreach(float x in new[]{-.135f,.135f})
            {
                g.Box(new Vector3(x,.183f,.154f),new Vector3(.025f,.105f,.023f),paint,.005f);
                g.Box(new Vector3(x,.1045f,.154f),new Vector3(.030f,.064f,.034f),paint,.005f);
                g.Cylinder(new Vector3(x,.194f,.166f),.013f,.071f,metal,Vector3.up,40);
                Bolt(g,new Vector3(x,.244f,.154f),Vector3.forward);
            }
            foreach(float y in new[]{.154f,.234f})g.Cylinder(new Vector3(0,y,.164f),.013f,.247f,metal,Vector3.right,48);
            Curve(g,new[]{rope.Last(),new Vector3(.125f,.278f,.058f),new Vector3(.086f,.249f,.118f),new Vector3(.041f,.204f,.168f),new Vector3(.041f,.162f,.217f)},.0033f,cable);
            Ring(g,new Vector3(.041f,.151f,.217f),.009f,.003f,metal,Vector3.forward,32);
            var hook=new List<Vector3>();var radii=new List<float>();
            for(int i=0;i<=40;i++){float t=i/40f,a=Mathf.Lerp(80,336,t)*Mathf.Deg2Rad;hook.Add(new Vector3(.041f+Mathf.Cos(a)*.018f,.117f+Mathf.Sin(a)*.018f,.217f));radii.Add(Mathf.Lerp(.0065f,.003f,t));}
            Rod(g,new Vector3(.041f,.144f,.217f),hook[0],.006f,metal);Tube(g,hook,.006f,metal,12,radii.ToArray());
            Rod(g,new Vector3(.043f,.133f,.217f),new Vector3(.057f,.111f,.217f),.0015f,dark,12);
            return g;
        }
        static Geometry FireReel()
        {
            var g=new Geometry();var c=new Vector3(.041f,.250f,-.020f);
            g.Cylinder(c,.048f,.114f,dark,Vector3.forward,48);
            foreach(float z in new[]{-.081f,.046f})
            {
                Axial(g,new Vector3(c.x,c.y,z),new[]{new Vector2(-.005f,.106f),new Vector2(.005f,.106f),new Vector2(.007f,.121f),
                    new Vector2(.003f,.128f),new Vector2(-.003f,.128f),new Vector2(-.007f,.121f)},red,Vector3.forward,64);
                g.Cylinder(new Vector3(c.x,c.y,z),.039f,.013f,red,Vector3.forward,48);
                for(int i=0;i<3;i++)RotatedBox(g,new Vector3(c.x,c.y,z),Quaternion.Euler(0,0,i*120),new Vector3(0,.070f,0),new Vector3(.017f,.083f,.009f),red,.004f);
            }
            var hose=new List<Vector3>();
            for(int i=0;i<=420;i++){float t=i/420f,a=t*12*Mathf.PI*2+Mathf.PI*.5f;hose.Add(new Vector3(c.x+Mathf.Sin(a)*.091f,c.y+Mathf.Cos(a)*.091f,Mathf.Lerp(-.071f,.036f,t)));}
            Tube(g,hose,.0043f,rubber,10);
            foreach(float x in new[]{-.040f,.120f})g.Box(new Vector3(x,.155f,-.077f),new Vector3(.027f,.161f,.044f),paint,.006f);
            Rod(g,new Vector3(-.192f,.112f,.09f),new Vector3(.122f,.112f,.09f),.012f,brass,32);
            Axial(g,new Vector3(-.215f,.112f,.09f),new[]{new Vector2(-.023f,.008f),new Vector2(-.023f,.018f),new Vector2(.016f,.018f),new Vector2(.022f,.012f)},brass,Vector3.right,40);
            Curve(g,new[]{new Vector3(.122f,.112f,.09f),new Vector3(.164f,.112f,.055f),new Vector3(.164f,.134f,-.091f),
                new Vector3(.164f,.240f,-.096f),new Vector3(.041f,.250f,-.096f)},.007f,brass);
            foreach(float x in new[]{-.116f,.085f})
            {g.Box(new Vector3(x,.092f,.09f),new Vector3(.034f,.039f,.032f),paint,.004f);Bolt(g,new Vector3(x,.114f,.075f),Vector3.up,.003f);}
            Rod(g,new Vector3(-.177f,.112f,.09f),new Vector3(-.177f,.228f,.09f),.007f,brass);
            Gauge(g,new Vector3(-.177f,.251f,.103f),.033f);
            g.Cylinder(new Vector3(-.177f,.145f,.09f),.006f,.039f,metal,Vector3.up,24);
            Ring(g,new Vector3(-.177f,.166f,.09f),.022f,.003f,red,Vector3.up,40);
            for(int i=0;i<4;i++){float a=i*Mathf.PI*.5f;Rod(g,new Vector3(-.177f,.166f,.09f),new Vector3(-.177f+Mathf.Cos(a)*.021f,.166f,.09f+Mathf.Sin(a)*.021f),.0025f,red,16);}
            Curve(g,new[]{hose.Last(),new Vector3(.207f,.240f,.09f),new Vector3(.244f,.16f,.165f),new Vector3(.230f,.091f,.173f),new Vector3(.207f,.124f,.172f)},.0043f,rubber);
            Axial(g,new Vector3(.207f,.170f,.172f),new[]{new Vector2(-.047f,.005f),new Vector2(-.047f,.014f),new Vector2(-.032f,.012f),
                new Vector2(.014f,.011f),new Vector2(.037f,.008f),new Vector2(.042f,.005f)},brass,Vector3.up,40);
            g.Box(new Vector3(.207f,.149f,.153f),new Vector3(.033f,.019f,.022f),paint,.004f);
            g.Box(new Vector3(.207f,.108f,.147f),new Vector3(.021f,.071f,.022f),paint,.004f);
            return g;
        }
        static Geometry WaterPump()
        {
            var g=new Geometry();var pump=new Vector3(.132f,.203f,-.015f);
            Axial(g,pump,new[]{new Vector2(-.053f,0),new Vector2(-.053f,.060f),new Vector2(-.035f,.082f),new Vector2(.022f,.087f),
                new Vector2(.044f,.074f),new Vector2(.048f,.028f)},metal,Vector3.forward,64);
            Axial(g,pump+new Vector3(0,0,.095f),new[]{new Vector2(-.049f,.024f),new Vector2(-.049f,.035f),new Vector2(.025f,.035f),
                new Vector2(.031f,.039f),new Vector2(.044f,.039f),new Vector2(.044f,.025f)},metal,Vector3.forward,48);
            for(int i=0;i<6;i++){float a=i*Mathf.PI/3;Bolt(g,pump+new Vector3(Mathf.Cos(a)*.068f,Mathf.Sin(a)*.068f,.047f),Vector3.forward);}
            Axial(g,new Vector3(.176f,.300f,-.012f),new[]{new Vector2(-.060f,.015f),new Vector2(-.060f,.022f),new Vector2(.033f,.022f),
                new Vector2(.035f,.029f),new Vector2(.048f,.029f),new Vector2(.048f,.017f)},metal,Vector3.up,40);
            var engine=new Vector3(-.122f,.212f,-.021f);
            g.Box(engine,new Vector3(.135f,.108f,.143f),dark,.012f);
            for(int i=0;i<5;i++)g.Box(engine+new Vector3(0,.014f+i*.013f,0),new Vector3(.150f,.006f,.126f),metal,.003f);
            g.Box(new Vector3(-.122f,.293f,-.021f),new Vector3(.136f,.026f,.116f),paint,.008f);
            g.Box(new Vector3(-.119f,.343f,-.030f),new Vector3(.154f,.058f,.151f),green,.014f);
            foreach(float x in new[]{-.167f,-.078f})g.Box(new Vector3(x,.311f,-.030f),new Vector3(.020f,.016f,.090f),paint,.003f);
            g.Cylinder(new Vector3(-.125f,.378f,-.040f),.018f,.015f,dark,Vector3.up,32);
            var face=new Vector3(-.122f,.214f,.076f);
            g.Cylinder(face,.065f,.020f,paint,Vector3.forward,64);
            g.Cylinder(face+Vector3.forward*.012f,.056f,.004f,rubber,Vector3.forward,48);
            Ring(g,face+Vector3.forward*.017f,.058f,.004f,metal,Vector3.forward,48);
            for(int i=0;i<9;i++)RotatedBox(g,face+Vector3.forward*.018f,Quaternion.Euler(0,0,i*40+12),new Vector3(0,.039f,0),new Vector3(.010f,.033f,.004f),paint,.002f);
            g.Cylinder(face+Vector3.forward*.023f,.022f,.009f,paint,Vector3.forward,40);
            Curve(g,new[]{face+new Vector3(-.025f,.029f,.014f),new Vector3(-.185f,.274f,.111f),new Vector3(-.208f,.267f,.116f)},.0025f,rubber);
            g.Box(new Vector3(-.211f,.266f,.116f),new Vector3(.035f,.019f,.019f),rubber,.006f);
            g.Cylinder(new Vector3(-.181f,.276f,-.101f),.024f,.069f,dark,Vector3.forward,32);
            Rod(g,new Vector3(-.19f,.276f,-.13f),new Vector3(-.229f,.280f,-.13f),.006f,dark);
            foreach(float x in new[]{-.248f,.248f})Curve(g,new[]{new Vector3(x,.092f,-.122f),new Vector3(x,.381f,-.122f),
                new Vector3(x,.408f,-.095f),new Vector3(x,.408f,.096f),new Vector3(x,.380f,.123f),new Vector3(x,.092f,.123f)},.010f,paint);
            foreach(float z in new[]{-.114f,.114f})Rod(g,new Vector3(-.248f,.087f,z),new Vector3(.248f,.087f,z),.010f,paint);
            foreach(float x in new[]{-.122f,.132f})foreach(float z in new[]{-.065f,.065f})
            {g.Box(new Vector3(x,.101f,z),new Vector3(.055f,.056f,.045f),rubber,.006f);Bolt(g,new Vector3(x,.133f,z),Vector3.up,.005f);}
            foreach(float z in new[]{-.065f,.065f})g.Box(new Vector3(-.122f,.146f,z),new Vector3(.055f,.034f,.045f),paint,.005f);
            g.Box(new Vector3(.132f,.135f,.002f),new Vector3(.131f,.028f,.178f),paint,.006f);
            return g;
        }
        static Geometry ServiceUnit()
        {
            var g=new Geometry();float left=-.225f,right=.256f,back=-.145f,front=.146f;
            foreach(float x in new[]{left,right})g.Box(new Vector3(x,.231f,-.005f),new Vector3(.015f,.284f,.298f),paint,.006f);
            g.Box(new Vector3(.015f,.231f,back),new Vector3(.477f,.284f,.012f),paint,.005f);
            foreach(float y in new[]{.094f,.367f})g.Box(new Vector3(.015f,y,-.005f),new Vector3(.485f,.020f,.301f),paint,.008f);
            foreach(float y in new[]{.147f,.235f})
            {
                g.Box(new Vector3(.015f,y,front-.007f),new Vector3(.453f,.077f,.012f),rubber,.005f);
                g.Box(new Vector3(.015f,y,front+.002f),new Vector3(.440f,.069f,.013f),green,.005f);
                foreach(float x in new[]{-.101f,.131f})g.Box(new Vector3(x,y+.002f,front+.018f),new Vector3(.020f,.013f,.022f),metal,.003f);
                g.Cylinder(new Vector3(.015f,y+.002f,front+.029f),.005f,.250f,metal,Vector3.right,32);
            }
            g.Box(new Vector3(.015f,.326f,front),new Vector3(.449f,.082f,.014f),dark,.005f);
            g.Box(new Vector3(-.110f,.332f,front+.009f),new Vector3(.126f,.048f,.008f),paint,.005f);
            g.Box(new Vector3(-.110f,.332f,front+.014f),new Vector3(.100f,.030f,.003f),rubber,.002f);
            foreach(float x in new[]{.011f,.069f})
            {
                Axial(g,new Vector3(x,.326f,front+.020f),new[]{new Vector2(-.008f,.008f),new Vector2(-.008f,.017f),new Vector2(.008f,.017f),new Vector2(.008f,.009f)},metal,Vector3.forward,32);
                g.Cylinder(new Vector3(x,.326f,front+.019f),.009f,.010f,rubber,Vector3.forward,24);
            }
            g.Cylinder(new Vector3(.168f,.326f,front+.020f),.017f,.021f,rubber,Vector3.forward,40);
            g.Box(new Vector3(.168f,.326f,front+.032f),new Vector3(.003f,.017f,.002f),ivory,.0005f);
            foreach(float x in new[]{-.146f,.176f})g.Box(new Vector3(x,.385f,-.012f),new Vector3(.025f,.029f,.029f),metal,.005f);
            g.Cylinder(new Vector3(.015f,.401f,-.012f),.010f,.334f,rubber,Vector3.right,40);
            var lead=new List<Vector3>();
            for(int i=0;i<=200;i++){float t=i/200f,a=t*4*Mathf.PI*2;lead.Add(new Vector3(-.242f-t*.020f,.233f+Mathf.Sin(a)*.063f,-.025f+Mathf.Cos(a)*.063f));}
            Tube(g,lead,.0035f,rubber,8);
            Curve(g,new[]{lead.Last(),new Vector3(-.266f,.149f,.04f),new Vector3(-.266f,.123f,.074f)},.0035f,rubber);
            g.Cylinder(new Vector3(-.266f,.155f,.074f),.006f,.063f,red,Vector3.up,24);
            g.Cylinder(new Vector3(-.266f,.194f,.074f),.002f,.019f,metal,Vector3.up,16);
            return g;
        }
        static Geometry TransitCase()
        {
            var g=new Geometry();
            g.Box(new Vector3(0,.225f,-.003f),new Vector3(.490f,.254f,.302f),green,.021f);
            g.Box(new Vector3(0,.354f,-.003f),new Vector3(.496f,.009f,.307f),rubber,.005f);
            g.Box(new Vector3(0,.379f,-.003f),new Vector3(.502f,.042f,.314f),paint,.016f);
            g.Box(new Vector3(0,.402f,-.003f),new Vector3(.425f,.004f,.239f),green,.009f);
            foreach(int side in new[]{-1,1})
            {
                foreach(float z in new[]{-.105f,.099f})g.Box(new Vector3(side*.239f,.225f,z),new Vector3(.022f,.215f,.021f),paint,.006f);
                g.Box(new Vector3(side*.155f,.407f,-.003f),new Vector3(.025f,.004f,.322f),webbing,.001f);
                foreach(int end in new[]{-1,1})
                {
                    float z=-.003f+end*.154f;
                    g.Box(new Vector3(side*.155f,.227f,z),new Vector3(.025f,.258f,.004f),webbing,.001f);
                    g.Box(new Vector3(side*.155f,.3815f,z+end*.006f),new Vector3(.025f,.051f,.004f),webbing,.001f);
                    g.Box(new Vector3(side*.155f,.357f,z+end*.003f),new Vector3(.025f,.005f,.012f),webbing,.001f);
                }
                g.Box(new Vector3(side*.155f,.228f,.155f),new Vector3(.040f,.052f,.010f),metal,.005f);
                g.Box(new Vector3(side*.155f,.228f,.162f),new Vector3(.025f,.028f,.004f),webbing,.002f);
                g.Box(new Vector3(side*.155f,.092f,.174f),new Vector3(.039f,.019f,.035f),metal,.004f);
                g.Box(new Vector3(side*.155f,.098f,.162f),new Vector3(.025f,.005f,.026f),webbing,.001f);
                g.Box(new Vector3(side*.102f,.358f,.159f),new Vector3(.031f,.051f,.011f),metal,.004f);
                Bolt(g,new Vector3(side*.102f,.343f,.167f),Vector3.forward,.003f);
                g.Box(new Vector3(side*.109f,.410f,-.003f),new Vector3(.025f,.015f,.029f),rubber,.004f);
            }
            g.Box(new Vector3(0,.418f,-.003f),new Vector3(.232f,.015f,.023f),rubber,.006f);
            g.Cylinder(new Vector3(.196f,.257f,.153f),.013f,.009f,dark,Vector3.forward,32);
            for(int i=0;i<4;i++)g.Box(new Vector3(-.238f,.225f,-.071f+i*.046f),new Vector3(.008f,.122f,.008f),paint,.002f);
            return g;
        }
        static void Gauge(Geometry g,Vector3 face,float radius)
        {
            g.Cylinder(face,radius,.014f,metal,Vector3.forward,48);g.Cylinder(face+Vector3.forward*.008f,radius*.86f,.003f,ivory,Vector3.forward,48);
            for(int i=0;i<11;i++)RotatedBox(g,face+Vector3.forward*.0105f,Quaternion.Euler(0,0,-120+i*24),new Vector3(0,radius*.68f,0),new Vector3(.0013f,.0045f,.0008f),dark,.0002f);
            RotatedBox(g,face+Vector3.forward*.012f,Quaternion.Euler(0,0,42),new Vector3(0,radius*.25f,0),new Vector3(.0015f,radius*.90f,.001f),red,.0002f);
            g.Cylinder(face+Vector3.forward*.014f,.003f,.002f,dark,Vector3.forward,20);
        }
        static void RotatedBox(Geometry g,Vector3 origin,Quaternion rotation,Vector3 offset,Vector3 size,Material material,float bevel)
        {var box=new Geometry();box.Box(offset,size,material,bevel);g.Append(box,Matrix4x4.TRS(origin,rotation,Vector3.one));}
        static void Curve(Geometry g,Vector3[] anchors,float radius,Material material)
        {
            var points=new List<Vector3>();
            for(int i=0;i<anchors.Length-1;i++)for(int j=0;j<8;j++)
            {
                float t=j/8f;var a=anchors[Math.Max(0,i-1)];var b=anchors[i];var c=anchors[i+1];var d=anchors[Math.Min(anchors.Length-1,i+2)];
                points.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
            }
            points.Add(anchors.Last());Tube(g,points,radius,material,12);
        }
        static void Tube(Geometry g,List<Vector3> points,float radius,Material material,int sides=10,float[] radii=null)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            var previous=(points[1]-points[0]).normalized;var normal=Vector3.Cross(previous,Mathf.Abs(previous.y)>.85f?Vector3.right:Vector3.up).normalized;float length=0;
            for(int i=0;i<points.Count;i++)
            {
                var tangent=(points[Math.Min(i+1,points.Count-1)]-points[Math.Max(0,i-1)]).normalized;
                normal=Vector3.ProjectOnPlane(Quaternion.FromToRotation(previous,tangent)*normal,tangent).normalized;var binormal=Vector3.Cross(tangent,normal).normalized;
                if(i>0)length+=Vector3.Distance(points[i],points[i-1]);
                for(int j=0;j<sides;j++){float a=j*Mathf.PI*2/sides;var n=normal*Mathf.Cos(a)+binormal*Mathf.Sin(a);vertices.Add(points[i]+n*(radii==null?radius:radii[i]));normals.Add(n);uv.Add(new Vector2(length*8,j/(float)sides));}
                previous=tangent;
            }
            for(int i=0;i<points.Count-1;i++)for(int j=0;j<sides;j++){int a=i*sides+j,b=i*sides+(j+1)%sides,c=b+sides,d=a+sides;triangles.AddRange(new[]{a,b,c,a,c,d});}
            foreach(int end in new[]{0,points.Count-1})
            {
                var n=(end==0?points[0]-points[1]:points[end]-points[end-1]).normalized;int start=vertices.Count;vertices.Add(points[end]);normals.Add(n);uv.Add(Vector2.zero);
                for(int j=0;j<sides;j++){vertices.Add(vertices[end*sides+j]);normals.Add(n);uv.Add(Vector2.zero);}
                for(int j=0;j<sides;j++)triangles.AddRange(new[]{start,start+1+j,start+1+(j+1)%sides});
            }
            for(int i=0;i<triangles.Count;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];if(Vector3.Dot(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]),normals[a]+normals[b]+normals[c])<0){triangles[i+1]=c;triangles[i+2]=b;}}
            var mesh=new Mesh{indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();mesh.RecalculateTangents();g.AddMesh(mesh,material);
        }
        static void Label(Transform root,string caption)
        {
            var label=new GameObject("ModuleLabel",typeof(RectTransform),typeof(Canvas));label.transform.SetParent(root,false);
            label.transform.localPosition=new Vector3(0,.055f,.204f);label.transform.localRotation=Quaternion.Euler(0,180,0);label.transform.localScale=Vector3.one*.001f;
            label.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace;label.GetComponent<RectTransform>().sizeDelta=new Vector2(260,27);
            var paper=new GameObject("PrintedLabel",typeof(RectTransform),typeof(Image));paper.transform.SetParent(label.transform,false);Fill(paper.GetComponent<RectTransform>());
            paper.GetComponent<Image>().color=new Color(.72f,.74f,.69f);paper.GetComponent<Image>().raycastTarget=false;
            var go=new GameObject("ModuleName",typeof(RectTransform),typeof(Text));go.transform.SetParent(paper.transform,false);Fill(go.GetComponent<RectTransform>());
            var text=go.GetComponent<Text>();text.font=Resources.Load<Font>("Fonts/SourceSans3-Semibold");text.text=caption;text.fontSize=21;text.resizeTextForBestFit=true;text.resizeTextMinSize=17;text.resizeTextMaxSize=21;
            text.color=new Color(.12f,.15f,.15f);text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        }
        static void Fill(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        static void Render(GameObject prefab,string path,bool thumbnail)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));var scene=EditorSceneManager.NewPreviewScene();var old=RenderTexture.active;RenderTexture target=null;Texture2D pixels=null;
            try
            {
                var model=Object.Instantiate(prefab);SceneManager.MoveGameObjectToScene(model,scene);model.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                foreach(var script in model.GetComponentsInChildren<MonoBehaviour>())script.enabled=false;
                var renderers=model.GetComponentsInChildren<MeshRenderer>();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                var go=new GameObject("ModulePreviewCamera");SceneManager.MoveGameObjectToScene(go,scene);var camera=go.AddComponent<Camera>();camera.scene=scene;
                camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=thumbnail?new Color(0,0,0,0):new Color(.19f,.21f,.22f,1);
                camera.allowHDR=false;camera.nearClipPlane=.01f;camera.farClipPlane=20;
                camera.transform.position=bounds.center+new Vector3(.95f,.62f,1.45f).normalized*3;camera.transform.LookAt(bounds.center);
                int width=thumbnail?512:1400,height=thumbnail?512:1000;camera.aspect=width/(float)height;float extent=0;
                foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})foreach(float z in new[]{-1f,1f})
                {var p=camera.transform.InverseTransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)));extent=Mathf.Max(extent,Mathf.Abs(p.y),Mathf.Abs(p.x)/camera.aspect);}
                camera.orthographicSize=extent*1.12f;var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;
                Light(scene,new Vector3(45,-145,0),2.1f,new Color(1,.96f,.90f));Light(scene,new Vector3(25,40,0),.9f,new Color(.86f,.92f,1));Light(scene,new Vector3(30,120,0),.9f,Color.white);
                target=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32);target.antiAliasing=4;camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();camera.Render();
                RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
            }
            finally{RenderTexture.active=old;if(target)RenderTexture.ReleaseTemporary(target);if(pixels)Object.DestroyImmediate(pixels);EditorSceneManager.ClosePreviewScene(scene);}
        }
        static void Light(Scene scene,Vector3 angles,float intensity,Color color)
        {var go=new GameObject("ModulePreviewLight");SceneManager.MoveGameObjectToScene(go,scene);go.transform.rotation=Quaternion.Euler(angles);var light=go.AddComponent<Light>();light.type=LightType.Directional;light.intensity=intensity;light.color=color;light.shadows=LightShadows.None;}
    }
}
