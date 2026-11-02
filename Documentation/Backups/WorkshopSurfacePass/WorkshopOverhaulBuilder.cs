using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CompanySim;

namespace CompanySimEditor
{
    public static class WorkshopOverhaulBuilder
    {
        const string Folder="Assets/_Project/Art/Garage/Meshes/";
        static Material Material(string name,Color color,float smooth=.35f,float metal=.12f,string texture=null)
            => GarageRefinementBuilder.Surface(name,color,smooth,metal,texture);
        public static void Configure()
        {
            var old=GameObject.Find("11_WorkshopInterior"); if(old) Object.DestroyImmediate(old);
            var root=new GameObject("11_WorkshopInterior").transform;
            var graphite=Material("Workshop_Graphite",new Color(.075f,.10f,.12f),.36f,.35f);
            var red=Material("Workshop_CabinetRed",new Color(.48f,.065f,.045f),.42f,.20f);
            var steel=Material("Workshop_BrushedMetal",new Color(.52f,.57f,.59f),.46f,.75f);
            var white=Material("Workshop_EnamelWhite",new Color(.73f,.77f,.78f),.31f,.12f);
            var blueFloor=Material("Workshop_BayFloor",new Color(.15f,.28f,.35f),.36f,.02f,"Epoxy");
            var passage=Material("Workshop_Walkway",new Color(.43f,.48f,.48f),.32f,.02f,"Epoxy");
            var yellow=Material("Workshop_Marking",new Color(.95f,.68f,.15f),.27f,0);
            var rubber=Material("Workshop_Rubber",new Color(.055f,.068f,.075f),.12f,0);
            var felt=Material("Workshop_DrawerLiner",new Color(.10f,.16f,.145f),.06f,0);
            var env=GameObject.Find("01_Environment_OfficeGarage");
            foreach(var renderer in env.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name.StartsWith("A07_"))
                    renderer.sharedMaterial=Material("Garage_Epoxy",new Color(.29f,.34f,.37f),.36f,.02f,"Epoxy");
                if(renderer.name.StartsWith("A43_") || renderer.name.StartsWith("A41_") || renderer.name.StartsWith("A44_") ||
                   ((renderer.name.StartsWith("A02_") || renderer.name.StartsWith("A42_")) &&
                    (renderer.name.Contains("paint chip") || renderer.name.Contains("hairline") || renderer.name.Contains("forked"))))
                    renderer.enabled=false;
                if(renderer.name.StartsWith("T30",System.StringComparison.Ordinal)) renderer.enabled=false;
                if(renderer.bounds.center.x>8.0f && renderer.bounds.center.x<8.95f && renderer.bounds.center.z> -1.86f &&
                    renderer.bounds.center.z<2.20f && renderer.bounds.max.y<.91f) renderer.enabled=false;
                // The supplied tools remain in their original working positions.
                // Refinish only their garage bench and tool-board backing.
                if(renderer.bounds.center.x<7.9f || renderer.bounds.center.z< -2.0f || renderer.bounds.center.z>2.5f) continue;
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    string name=materials[i]?materials[i].name:"";
                    if(name.Contains("workshop oak") || name.Contains("oak edge") || name.Contains("Oak end")) materials[i]=graphite;
                    if(name.Contains("fibreboard") || renderer.name.Contains("Pegboard backing")) materials[i]=graphite;
                }
                renderer.sharedMaterials=materials;
            }
            // Architectural changes are confined to the garage footprint x > 0.
            Part(root,"ServiceFloor",new Vector3(4.65f,.007f,-.42f),new Vector3(4.48f,.008f,7.66f),blueFloor,0);
            Part(root,"WalkwayFloor",new Vector3(1.24f,.007f,-.25f),new Vector3(.98f,.008f,8.55f),passage,0);
            foreach(float x in new[]{2.37f,6.93f})
                Part(root,"BayBoundary",new Vector3(x,.016f,-.42f),new Vector3(.065f,.004f,7.66f),yellow,0);
            foreach(float x in new[]{.72f,1.76f})
                for(int i=0;i<13;i++) Part(root,"WalkwayDash",new Vector3(x,.016f,-4.35f+i*.65f),new Vector3(.035f,.004f,.30f),white,0);
            Part(root,"BayHeadLine",new Vector3(4.65f,.016f,-4.26f),new Vector3(4.48f,.004f,.065f),yellow,0);
            Part(root,"BayRearLine",new Vector3(4.65f,.016f,3.42f),new Vector3(4.48f,.004f,.065f),yellow,0);
            // Clean cladding between the existing structural ceiling beams.
            foreach(float x in new[]{1.55f,4.65f,7.75f})
            {
                Part(root,"CeilingCassette",new Vector3(x,4.012f,0),new Vector3(2.50f,.055f,10.12f),white,.012f);
                for(int z=0;z<9;z++) Part(root,"CeilingPanelJoint",new Vector3(x,3.981f,-4.6f+z*1.15f),new Vector3(2.48f,.006f,.012f),steel,0);
            }
            // Four real cabinet carcasses with recessed drawer fronts and metal pulls.
            for(int i=0;i<4;i++)
            {
                float z=-1.34f+i*1.02f;
                Part(root,"CabinetCarcass_"+i,new Vector3(8.88f,.47f,z),new Vector3(.022f,.82f,.98f),graphite,.006f);
                foreach(float side in new[]{-1f,1f})
                    Part(root,"CabinetSide",new Vector3(8.47f,.47f,z+side*.479f),new Vector3(.84f,.82f,.022f),graphite,.006f);
                Part(root,"CabinetBottom",new Vector3(8.47f,.092f,z),new Vector3(.84f,.028f,.94f),graphite,.005f);
                Part(root,"CabinetPlinth_"+i,new Vector3(8.47f,.055f,z),new Vector3(.78f,.10f,.89f),rubber,.006f);
                for(int drawer=0;drawer<4;drawer++)
                {
                    float y=.18f+drawer*.185f;
                    var tray=new GameObject("WorkshopDrawer_"+i+"_"+drawer).transform;
                    tray.SetParent(root,false); tray.position=new Vector3(8.028f,y,z);
                    Part(tray,"Drawer_"+i+"_"+drawer,tray.position,new Vector3(.035f,.169f,.91f),red,.009f);
                    Part(tray,"DrawerPull",tray.position+new Vector3(-.031f,.050f,0),new Vector3(.035f,.022f,.65f),steel,.004f);
                    Part(tray,"TrayBottom",tray.position+new Vector3(.33f,-.066f,0),new Vector3(.65f,.012f,.88f),steel,.004f);
                    Part(tray,"TrayLiner",tray.position+new Vector3(.33f,-.057f,0),new Vector3(.62f,.004f,.85f),felt,.001f);
                    foreach(float side in new[]{-1f,1f})
                        Part(tray,"TraySide",tray.position+new Vector3(.33f,-.010f,side*.441f),new Vector3(.65f,.112f,.012f),steel,.003f);
                    Part(tray,"TrayBack",tray.position+new Vector3(.655f,-.010f,0),new Vector3(.012f,.112f,.89f),steel,.003f);
                    var collider=tray.gameObject.AddComponent<BoxCollider>(); collider.size=new Vector3(.70f,.169f,.91f); collider.center=new Vector3(.33f,0,0);
                    var body=tray.gameObject.AddComponent<Rigidbody>(); body.isKinematic=true; body.useGravity=false;
                    body.interpolation=RigidbodyInterpolation.Interpolate; body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
                    tray.gameObject.AddComponent<WorkshopDrawer>();
                    if(drawer==3) PopulateTray(tray,env);
                }
            }
            Part(root,"WorkbenchCounter",new Vector3(8.46f,.957f,.25f),new Vector3(.96f,.055f,4.36f),steel,.012f);
            Part(root,"CounterFrontRubber",new Vector3(7.973f,.95f,.25f),new Vector3(.022f,.068f,4.34f),graphite,.004f);
            var glow=Material("Workshop_TaskLens",Color.white,.42f,0);
            glow.SetColor("_EmissionColor",new Color(2.7f,2.85f,3)); glow.globalIlluminationFlags=MaterialGlobalIlluminationFlags.BakedEmissive;
            BaseShaderGUI.SetMaterialKeywords(glow,UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords); EditorUtility.SetDirty(glow);
            Part(root,"TaskHood",new Vector3(8.82f,2.40f,.25f),new Vector3(.37f,.09f,4.34f),graphite,.012f);
            Part(root,"TaskStrip",new Vector3(8.76f,2.348f,.25f),new Vector3(.12f,.012f,4.20f),glow,.003f);
            Part(root,"RearTaskHousing",new Vector3(4.3f,3.19f,4.96f),new Vector3(1.72f,.075f,.12f),graphite,.010f);
            Part(root,"RearTaskLens",new Vector3(4.3f,3.145f,4.92f),new Vector3(1.62f,.015f,.075f),glow,.003f);
            var rearFill=new GameObject("RearVehicleWorkLight"); rearFill.transform.SetParent(root,false);
            rearFill.transform.position=new Vector3(4.3f,3.1f,4.80f); rearFill.transform.LookAt(new Vector3(4.65f,1.35f,2.40f));
            var rearLight=rearFill.AddComponent<Light>(); rearLight.type=LightType.Spot; rearLight.intensity=4.2f; rearLight.range=5.5f;
            rearLight.spotAngle=105; rearLight.innerSpotAngle=80; rearLight.color=new Color(.97f,.985f,1); rearLight.shadows=LightShadows.None;
            LiftController(root,graphite,steel,white,red);
            // A shallow corrugated metal liner gives the lower walls a consistent finish.
            foreach(float z in new[]{-4.30f,-.58f})
            {
                float length=z< -2?1.8f:3.56f;
                Part(root,"PartitionKickPanel",new Vector3(.092f,.55f,z),new Vector3(.014f,1.08f,length),graphite,.003f);
                Part(root,"PartitionCapRail",new Vector3(.105f,1.10f,z),new Vector3(.025f,.026f,length),steel,.004f);
            }
            Part(root,"RearWallCapRail",new Vector3(4.5f,1.10f,5.055f),new Vector3(8.9f,.030f,.03f),steel,.005f);
            Part(root,"RightWallCapRail",new Vector3(9.095f,1.10f,0),new Vector3(.030f,.030f,10.12f),steel,.005f);
            foreach(var light in GameObject.Find("10_GarageFinish").GetComponentsInChildren<Light>())
                if(light.name.StartsWith("Garage_LED_Light")) { light.intensity=11; light.color=new Color(.97f,.985f,1); }
            // Soft light entering the open garage reaches the vehicle's front.
            var portal=new GameObject("Garage_PortalDaylight"); portal.transform.SetParent(root,false);
            portal.transform.position=new Vector3(4.65f,2.7f,-4.95f); portal.transform.LookAt(new Vector3(4.65f,1.2f,1.2f));
            var daylight=portal.AddComponent<Light>(); daylight.type=LightType.Spot; daylight.intensity=3.2f; daylight.range=10;
            daylight.color=new Color(.87f,.94f,1); daylight.spotAngle=105; daylight.innerSpotAngle=75;
            daylight.shadows=LightShadows.None;
            // Remove the old extra floor text when the bay surface supersedes it.
            var oldLabel=GameObject.Find("ServiceBayLabel"); if(oldLabel) oldLabel.SetActive(false);
            AssetDatabase.SaveAssets();
        }
        static void PopulateTray(Transform tray,GameObject environment)
        {
            var source=environment.GetComponentsInChildren<MeshFilter>().First(x=>x.name.StartsWith("Forged open-ended wrench"));
            var mesh=source.sharedMesh; var bounds=mesh.bounds;
            int axis=bounds.size.x>bounds.size.y?0:1; if(bounds.size.z>bounds.size[axis]) axis=2;
            var along=Vector3.zero; along[axis]=1;
            for(int i=0;i<3;i++)
            {
                var go=new GameObject("DrawerWrench",typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(tray,false);
                float scale=(.21f+i*.035f)/bounds.size[axis];
                var rotation=Quaternion.AngleAxis(-6+i*6,Vector3.up)*Quaternion.FromToRotation(along,Vector3.forward);
                go.transform.localRotation=rotation; go.transform.localScale=Vector3.one*scale;
                float halfHeight=(Mathf.Abs((rotation*Vector3.right).y)*bounds.extents.x+
                    Mathf.Abs((rotation*Vector3.up).y)*bounds.extents.y+Mathf.Abs((rotation*Vector3.forward).y)*bounds.extents.z)*scale;
                go.transform.localPosition=new Vector3(.17f,-.054f+halfHeight,-.27f+i*.27f)-rotation*(bounds.center*scale);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                go.GetComponent<MeshRenderer>().sharedMaterials=source.GetComponent<MeshRenderer>().sharedMaterials;
            }
        }
        static void LiftController(Transform parent,Material dark,Material steel,Material white,Material red)
        {
            var root=new GameObject("LiftController").transform; root.SetParent(parent,false); root.position=new Vector3(6.26f,1.48f,.13f);
            var housing=Part(root,"LiftControlHousing",root.position,new Vector3(.28f,.48f,.14f),dark,.014f);
            housing.AddComponent<BoxCollider>().size=new Vector3(.28f,.48f,.14f);
            Part(root,"LiftControlFascia",root.position+new Vector3(0,0,-.077f),new Vector3(.235f,.43f,.013f),steel,.006f);
            var green=Material("Workshop_ButtonGreen",new Color(.07f,.36f,.20f),.35f,0);
            for(int i=0;i<2;i++)
            {
                float y=i==0?.075f:-.060f;
                var button=GameObject.CreatePrimitive(PrimitiveType.Cylinder); button.name=i==0?"LiftUpButton":"LiftDownButton";
                button.transform.SetParent(root,false); button.transform.localPosition=new Vector3(0,y,-.095f);
                button.transform.localRotation=Quaternion.Euler(90,0,0); button.transform.localScale=new Vector3(.085f,.012f,.085f);
                button.GetComponent<MeshRenderer>().sharedMaterial=i==0?green:dark; Object.DestroyImmediate(button.GetComponent<Collider>());
                var symbol=new GameObject("DirectionArrow",typeof(MeshFilter),typeof(MeshRenderer)); symbol.transform.SetParent(root,false);
                symbol.transform.localPosition=new Vector3(0,y,-.109f); if(i==1) symbol.transform.localRotation=Quaternion.Euler(0,0,180);
                const string path=Folder+"Kit_ControlArrow.asset"; var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh) {mesh=new Mesh {name="ControlArrow",vertices=new[]{new Vector3(-.022f,-.016f,0),new Vector3(0,.021f,0),new Vector3(.022f,-.016f,0)},triangles=new[]{0,1,2}}; mesh.RecalculateNormals(); AssetDatabase.CreateAsset(mesh,path);}
                symbol.GetComponent<MeshFilter>().sharedMesh=mesh; symbol.GetComponent<MeshRenderer>().sharedMaterial=white;
            }
            var target=GameObject.Find("ModuleStation_ControlPanel"); target.transform.position=root.position;
            target.GetComponent<BoxCollider>().size=new Vector3(.40f,.60f,.32f);
        }
        static GameObject Part(Transform parent,string name,Vector3 position,Vector3 size,Material material,float bevel)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.position=position;
            go.GetComponent<MeshFilter>().sharedMesh=BoxMesh(size,bevel);
            go.GetComponent<MeshRenderer>().sharedMaterial=material; return go;
        }
        static Mesh BoxMesh(Vector3 size,float radius)
        {
            string key="Kit_"+size.x.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+
                size.y.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+size.z.ToString("F3",System.Globalization.CultureInfo.InvariantCulture)+"_"+
                radius.ToString("F3",System.Globalization.CultureInfo.InvariantCulture);
            string path=Folder+key+".asset"; var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(mesh) return mesh;
            var vertices=new List<Vector3>(); var normals=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            var half=size*.5f; radius=Mathf.Min(radius,Mathf.Min(half.x,Mathf.Min(half.y,half.z))*.8f);
            var inner=half-Vector3.one*radius;
            foreach(var face in new[]{Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back})
            {
                int axis=Mathf.Abs(face.x)>.5f?0:Mathf.Abs(face.y)>.5f?1:2;
                int u=(axis+1)%3,v=(axis+2)%3,start=vertices.Count;
                float[] us={-half[u],-inner[u],inner[u],half[u]},vs={-half[v],-inner[v],inner[v],half[v]};
                for(int row=0;row<4;row++) for(int col=0;col<4;col++)
                {
                    Vector3 p=Vector3.zero; p[axis]=face[axis]*half[axis]; p[u]=us[col]; p[v]=vs[row];
                    var core=new Vector3(Mathf.Clamp(p.x,-inner.x,inner.x),Mathf.Clamp(p.y,-inner.y,inner.y),Mathf.Clamp(p.z,-inner.z,inner.z));
                    var n=radius>0?(p-core).normalized:face;
                    if(radius>0) p=core+n*radius;
                    vertices.Add(p); normals.Add(n); uv.Add(new Vector2(p[u],p[v])*.5f);
                }
                for(int row=0;row<3;row++) for(int col=0;col<3;col++)
                {
                    int a=start+row*4+col,b=a+1,c=a+5,d=a+4;
                    var tangentU=Vector3.zero; tangentU[u]=1;
                    var tangentV=Vector3.zero; tangentV[v]=1;
                    bool reverse=Vector3.Dot(Vector3.Cross(tangentU,tangentV),face)<0;
                    triangles.AddRange(reverse?new[]{a,c,b,a,d,c}:new[]{a,b,c,a,c,d});
                }
            }
            mesh=new Mesh {name=key}; mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0);
            mesh.RecalculateBounds(); mesh.RecalculateTangents(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
    }
}
