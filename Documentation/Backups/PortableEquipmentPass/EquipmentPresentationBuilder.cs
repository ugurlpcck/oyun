using System.IO;
using System.Linq;
using CompanySim;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace CompanySimEditor
{
    public static class EquipmentPresentationBuilder
    {
        const string Folder="Assets/_Project/Art/EquipmentDetail";
        const string Textures="Assets/_Project/Art/Garage/Textures/";
        static string Safe(string value)=>new string(value.Select(c=>char.IsLetterOrDigit(c)||c=='_'?c:'_').ToArray());
        public static void Configure()
        {
            Directory.CreateDirectory(Folder+"/Materials"); Directory.CreateDirectory(Folder+"/Meshes"); AssetDatabase.Refresh();
            foreach(var guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs/Equipment"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid); var root=PrefabUtility.LoadPrefabContents(path);
                try { Finish(root); PrefabUtility.SaveAsPrefabAsset(root,path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach(var item in Object.FindObjectsByType<CarryableItem>()) Finish(item.gameObject);
            AssetDatabase.SaveAssets();
        }
        static void Finish(GameObject root)
        {
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.sharedMaterials=renderer.sharedMaterials.Select(FinishMaterial).ToArray();
                var filter=renderer.GetComponent<MeshFilter>(); if(!filter || !filter.sharedMesh) continue;
                var source=filter.sharedMesh;
                if(AssetDatabase.GetAssetPath(source).StartsWith(Folder)) continue;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source,out string guid,out long local);
                string path=Folder+"/Meshes/"+guid+"_"+local+".asset";
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(!mesh)
                {
                    mesh=Object.Instantiate(source); mesh.name=source.name+"_EquipmentSurface";
                    var vertices=mesh.vertices; var normals=mesh.normals; var uv=new Vector2[vertices.Length];
                    for(int i=0;i<vertices.Length;i++)
                    {
                        // Prefab-local metric UVs keep the grain at the same size on every tool.
                        var p=root.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertices[i]));
                        var n=root.transform.InverseTransformDirection(renderer.transform.TransformDirection(normals[i]));
                        uv[i]=Mathf.Abs(n.y)>.6f?new Vector2(p.x,p.z)*.5f:
                            Mathf.Abs(n.x)>Mathf.Abs(n.z)?new Vector2(p.z,p.y)*.5f:new Vector2(p.x,p.y)*.5f;
                    }
                    mesh.uv=uv; mesh.RecalculateTangents(); AssetDatabase.CreateAsset(mesh,path);
                }
                filter.sharedMesh=mesh;
            }
        }
        static Material FinishMaterial(Material current)
        {
            if(!current) return current;
            const string prefix="EquipmentFinish_";
            string name=current.name.StartsWith(prefix)?current.name.Substring(prefix.Length):current.name;
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/"+Safe(name)+".mat");
            if(!source) source=current;
            string lower=name.ToLowerInvariant(),texture=null;
            if(lower.Contains("rubber") || lower.Contains("tire")) texture="GripRubber";
            else if(lower.Contains("enamel") || lower.Contains("paint")) texture="PowderCoat";
            else if(lower.Contains("plastic")) texture="MouldedPlastic";
            else if(lower.Contains("steel") || lower.Contains("iron") || lower.Contains("chrome") || lower.Contains("metal")) texture="BrushedSteel";
            if(texture==null || source.GetTexture("_BaseMap")) return source;
            string path=Folder+"/Materials/"+Safe(name)+".mat"; var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material) { material=new Material(source); AssetDatabase.CreateAsset(material,path); }
            material.CopyPropertiesFromMaterial(source); material.name=prefix+name;
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+texture+"_Albedo.png"));
            material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+texture+"_Normal.png"));
            material.SetFloat("_BumpScale",texture=="BrushedSteel"?.25f:.18f);
            material.SetTexture("_MetallicGlossMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Textures+texture+"_MetalSmooth.png"));
            material.SetFloat("_Smoothness",1); material.EnableKeyword("_NORMALMAP"); material.EnableKeyword("_METALLICSPECGLOSSMAP");
            BaseShaderGUI.SetMaterialKeywords(material,LitGUI.SetMaterialKeywords);
            EditorUtility.SetDirty(material); return material;
        }
    }
}
