using System.IO;
using CompanySim;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CompanySimEditor
{
    public static class ManualModuleBuilder
    {
        const string PrefabPath="Assets/_Project/Prefabs/Modules/Module_ServiceCassette.prefab";
        public static void Configure(GameObject root,ServiceVehicle vehicle,GarageLift lift)
        {
            var runtime=Object.FindAnyObjectByType<CompanyRuntime>();
            var system=root.GetComponent<ManualModuleSystem>(); if(!system) system=root.AddComponent<ManualModuleSystem>();
            system.runtime=runtime; system.vehicle=vehicle; system.lift=lift; runtime.manualModules=system; vehicle.manualModules=system;
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            {
                Directory.CreateDirectory("Assets/_Project/Prefabs/Modules");
                var source=AssetDatabase.LoadAssetAtPath<EquipmentDefinition>("Assets/_Project/Data/Equipment/case.asset");
                var package=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(source.visualPrefab));
                try
                {
                    package.name="Module_ServiceCassette"; var item=package.GetComponent<CarryableItem>(); item.uid=""; item.definitionId="module:";
                    package.AddComponent<ManualModulePackage>(); package.GetComponent<Rigidbody>().mass=24;
                    var label=new GameObject("ModuleLabel",typeof(RectTransform),typeof(Canvas)); label.transform.SetParent(package.transform,false);
                    label.transform.localPosition=new Vector3(0,.304f,0); label.transform.localRotation=Quaternion.Euler(90,0,0); label.transform.localScale=Vector3.one*.001f;
                    label.GetComponent<Canvas>().renderMode=RenderMode.WorldSpace; label.GetComponent<RectTransform>().sizeDelta=new Vector2(430,100);
                    var paper=new GameObject("PrintedLabel",typeof(RectTransform),typeof(Image)); paper.transform.SetParent(label.transform,false);
                    var paperRect=paper.GetComponent<RectTransform>(); paperRect.anchorMin=Vector2.zero; paperRect.anchorMax=Vector2.one; paperRect.offsetMin=paperRect.offsetMax=Vector2.zero;
                    paper.GetComponent<Image>().color=new Color(.84f,.84f,.77f); paper.GetComponent<Image>().raycastTarget=false;
                    var text=new GameObject("ModuleName",typeof(RectTransform),typeof(Text)); text.transform.SetParent(paper.transform,false);
                    var textRect=text.GetComponent<RectTransform>(); textRect.anchorMin=Vector2.zero; textRect.anchorMax=Vector2.one; textRect.offsetMin=new Vector2(12,8); textRect.offsetMax=new Vector2(-12,-8);
                    var caption=text.GetComponent<Text>(); caption.font=Resources.Load<Font>("Fonts/SourceSans3-Semibold"); caption.fontSize=38; caption.alignment=TextAnchor.MiddleCenter; caption.color=new Color(.12f,.15f,.16f); caption.raycastTarget=false;
                    PrefabUtility.SaveAsPrefabAsset(package,PrefabPath);
                }
                finally {PrefabUtility.UnloadPrefabContents(package);}
            }
            system.packagePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var legacyBay=vehicle.transform.Find("ManualModuleBay"); if(legacyBay) Object.DestroyImmediate(legacyBay.gameObject);
            var previous=vehicle.transform.Find("ExteriorModuleCarrier"); if(previous) Object.DestroyImmediate(previous.gameObject);
            var bay=new GameObject("ExteriorModuleCarrier").transform; bay.SetParent(vehicle.transform,false);
            // Side-mounted, ahead of the rear door sweep, with an unobstructed load aperture.
            bay.localPosition=new Vector3(1.06f,0,1.35f); bay.localRotation=Quaternion.Euler(0,90,0);
            var mount=bay.Find("CassetteSocket"); if(!mount) {mount=new GameObject("CassetteSocket").transform; mount.SetParent(bay,false);}
            mount.localPosition=new Vector3(0,1.12f,.24f); system.mount=mount;
            var metal=Material("Module_MountSteel",new Color(.19f,.23f,.24f),.65f);
            var bolt=Material("Module_BoltSteel",new Color(.52f,.55f,.54f),.75f);
            var rubber=Material("Module_ConnectorRubber",new Color(.055f,.065f,.07f),0);
            var liner=Material("Van_InnerLiner",new Color(.43f,.45f,.43f),.12f);
            system.packageVisuals=ModulePackagePresentationBuilder.Build(runtime.catalog);
            Interior(vehicle.transform,"CargoInnerLeft",new Vector3(-.90f,1.56f,.95f),new Vector3(.025f,1.88f,3.25f),liner);
            Interior(vehicle.transform,"CargoInnerRight",new Vector3(.90f,1.56f,.95f),new Vector3(.025f,1.88f,3.25f),liner);
            Interior(vehicle.transform,"CargoInnerRoof",new Vector3(0,2.49f,.95f),new Vector3(1.80f,.025f,3.25f),liner);
            Interior(vehicle.transform,"CargoInnerBulkhead",new Vector3(0,1.56f,-.68f),new Vector3(1.80f,1.88f,.025f),liner);
            Part(bay,"MountingPlate",new Vector3(0,1.40f,.045f),new Vector3(.78f,.44f,.025f),metal);
            Part(bay,"LowerSupportRail",new Vector3(0,1.09f,.30f),new Vector3(1.08f,.055f,.56f),metal);
            Part(bay,"UpperSupportRail",new Vector3(0,1.74f,.08f),new Vector3(1.08f,.045f,.12f),metal);
            foreach(int side in new[]{-1,1})
            {
                Part(bay,"SideRail_"+side,new Vector3(side*.50f,1.42f,.20f),new Vector3(.055f,.65f,.36f),metal);
                Part(bay,"BodyBracket_"+side,new Vector3(side*.43f,1.40f,-.025f),new Vector3(.14f,.78f,.09f),metal);
            }
            var target=new GameObject("ModuleDockTarget"); target.transform.SetParent(bay,false); target.transform.localPosition=new Vector3(0,1.40f,.65f);
            var collider=target.AddComponent<BoxCollider>(); collider.size=new Vector3(.62f,.43f,.055f); collider.isTrigger=true;
            target.AddComponent<ModuleMountPoint>().system=system;
            for(int index=0;index<4;index++) Fastener(bay,system,index,new Vector3(index%2==0?-.43f:.43f,index<2?1.14f:1.69f,.65f),bolt,metal);
            Fastener(bay,system,-1,new Vector3(-.64f,1.40f,.50f),rubber,metal);
            var cable=bay.gameObject.AddComponent<ModuleConnectionVisual>(); cable.system=system;
            cable.cableMaterial=rubber; cable.plugMaterial=bolt;
            // Collision follows the visible external carrier, including while the lift moves.
            var shell=bay.gameObject.AddComponent<BoxCollider>(); shell.center=new Vector3(0,1.40f,.29f); shell.size=new Vector3(1.08f,.70f,.58f);
        }
        static void Interior(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {if(!parent.Find(name)) Part(parent,name,position,size,material);}
        static void Fastener(Transform bay,ManualModuleSystem system,int index,Vector3 position,Material material,Material backing)
        {
            var root=new GameObject(index<0?"ModuleConnector":"ModuleBolt_"+(index+1)); root.transform.SetParent(bay,false); root.transform.localPosition=position;
            var hit=root.AddComponent<BoxCollider>(); hit.size=new Vector3(.12f,.12f,.10f);
            var component=root.AddComponent<ModuleFastener>(); component.system=system; component.index=index;
            var seat=Part(root.transform,"Washer",Vector3.zero,new Vector3(.085f,.085f,.025f),backing);
            var head=GameObject.CreatePrimitive(PrimitiveType.Cube); head.name=index<0?"ConnectorLock":"BoltHead";
            head.transform.SetParent(root.transform,false); head.transform.localPosition=new Vector3(0,0,.025f);
            head.transform.localRotation=Quaternion.identity; head.transform.localScale=index<0?new Vector3(.07f,.05f,.085f):Vector3.one;
            if(index>=0) head.GetComponent<MeshFilter>().sharedMesh=HexBoltMesh();
            Object.DestroyImmediate(head.GetComponent<Collider>()); head.GetComponent<Renderer>().sharedMaterial=material;
            component.boltVisual=head.transform;
            var indicator=Part(root.transform,"StatusMark",new Vector3(.048f,.047f,.03f),new Vector3(.014f,.014f,.015f),Material("Module_Status",new Color(.64f,.36f,.13f),0));
            component.indicator=indicator.GetComponent<Renderer>();
        }
        static Mesh HexBoltMesh()
        {
            const string path="Assets/_Project/Art/VehicleMechanics/Module_HexBolt.asset";
            var stored=AssetDatabase.LoadAssetAtPath<Mesh>(path); if(stored) return stored;
            var vertices=new System.Collections.Generic.List<Vector3>(); var indices=new System.Collections.Generic.List<int>();
            for(int face=0;face<6;face++)
            {
                float a=face*Mathf.PI/3,b=(face+1)*Mathf.PI/3;
                var p=new Vector3(Mathf.Cos(a)*.028f,Mathf.Sin(a)*.028f,0); var q=new Vector3(Mathf.Cos(b)*.028f,Mathf.Sin(b)*.028f,0);
                int n=vertices.Count; vertices.Add(p); vertices.Add(q); vertices.Add(q+Vector3.forward*.024f); vertices.Add(p+Vector3.forward*.024f);
                indices.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
                n=vertices.Count; vertices.Add(Vector3.forward*.024f); vertices.Add(p+Vector3.forward*.024f); vertices.Add(q+Vector3.forward*.024f); indices.AddRange(new[]{n,n+1,n+2});
                n=vertices.Count; vertices.Add(Vector3.zero); vertices.Add(q); vertices.Add(p); indices.AddRange(new[]{n,n+1,n+2});
            }
            var mesh=new Mesh {name="Module_HexBolt"}; mesh.SetVertices(vertices); mesh.SetTriangles(indices,0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh,path); return mesh;
        }
        static GameObject Part(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(parent,false); go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=WorkshopOverhaulBuilder.BoxMesh(size,Mathf.Min(.006f,Mathf.Min(size.x,Mathf.Min(size.y,size.z))*.18f));
            go.GetComponent<Renderer>().sharedMaterial=material; return go;
        }
        static Material Material(string name,Color color,float metallic)
        {
            const string folder="Assets/_Project/Art/VehicleMechanics"; Directory.CreateDirectory(folder); string path=folder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",color); material.SetFloat("_Metallic",metallic); material.SetFloat("_Smoothness",.35f); EditorUtility.SetDirty(material); return material;
        }
    }
}
