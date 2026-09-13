#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Bellerophon.Editor
{
    /// <summary>Retains the authored FBX weights that the native importer prunes below .001.</summary>
    internal sealed class PlayerHandSkinImport : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Art/Player/HandsRig/";
        internal const string NativeExportSessionKey = "Bellerophon.PlayerHands.ExportNativeWeights";
        internal const string NativeExportCompletedSessionKey = "Bellerophon.PlayerHands.ExportNativeWeights.Completed";
        internal const string NativeExportPath =
            "docs/validation/consumable_right_back_fix_2026-09-10/review_103229/native_fbx_weights.json";
        [Serializable] private class Influence { public string bone; public float weight; }
        [Serializable] private class Vertex { public Vector3 position; public Vector2 uv; public Influence[] influences; }
        [Serializable] private class Skin { public Vertex[] vertices; }

        private void OnPostprocessModel(GameObject model)
        {
            if(assetPath!=Folder+"player_hands_candidate.fbx"&&assetPath!="Assets/_Project/Art/Player/player.fbx")return;
            SkinnedMeshRenderer renderer=model.GetComponentInChildren<SkinnedMeshRenderer>();
            if(renderer==null||renderer.bones.Length!=54)return;
            Mesh mesh=renderer.sharedMesh;
            if(assetPath=="Assets/_Project/Art/Player/player.fbx"&&
                SessionState.GetBool(NativeExportSessionKey,false))
                ExportNativeWeights(model,renderer,mesh);
            string path=Folder+"player_hands_skin_weights.json";
            // Candidate authoring must not change the promoted model before visual review.
            if(assetPath==Folder+"player_hands_candidate.fbx"&&File.Exists(Folder+"player_hands_candidate_weights.json"))
                path=Folder+"player_hands_candidate_weights.json";
            if(!File.Exists(path))throw new InvalidOperationException("The shared hand rig requires its authored weight source.");
            Vertex[] source=JsonUtility.FromJson<Skin>(File.ReadAllText(path)).vertices;
            Vector3Int Cell(Vector3 p)=>Vector3Int.FloorToInt(p/.0001f);
            var cells=new Dictionary<Vector3Int,List<Vertex>>();
            foreach(Vertex row in source)
            {var key=Cell(row.position);if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<Vertex>();list.Add(row);}
            var names=renderer.bones.Select((bone,id)=>(bone.name,id)).ToDictionary(p=>p.name,p=>p.id);
            Vector3[] points=mesh.vertices;Vector2[] uvs=mesh.uv;
            var weights=new BoneWeight[points.Length];int restoredSmall=0;
            for(int v=0;v<points.Length;v++)
            {
                Vector3 point=model.transform.InverseTransformPoint(renderer.transform.TransformPoint(points[v]));
                Vector3Int cell=Cell(point);Vertex match=null;float best=4e-10f;
                for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++)for(int z=-1;z<=1;z++)
                    if(cells.TryGetValue(cell+new Vector3Int(x,y,z),out var rows))foreach(Vertex row in rows)
                    {
                        float distance=(row.position-point).sqrMagnitude;
                        if(distance>best||(row.uv-uvs[v]).sqrMagnitude>4e-8f)continue;
                        match=row;best=distance;
                    }
                if(match==null)throw new InvalidOperationException("Authored hand skin correspondence missing: "+v+" at "+point);
                Influence[] values=match.influences.OrderByDescending(i=>i.weight).ToArray();
                int Index(int i)=>i<values.Length?names[values[i].bone]:0;
                float Weight(int i)=>i<values.Length?values[i].weight:0;
                weights[v]=new BoneWeight{boneIndex0=Index(0),boneIndex1=Index(1),boneIndex2=Index(2),boneIndex3=Index(3),
                    weight0=Weight(0),weight1=Weight(1),weight2=Weight(2),weight3=Weight(3)};
                restoredSmall+=values.Count(i=>i.weight<.001f);
            }
            mesh.boneWeights=weights;
            Debug.Log("Authored shared-hand weights retained: "+assetPath+" vertices="+points.Length+" smallInfluences="+restoredSmall);
        }

        private static void ExportNativeWeights(GameObject model,SkinnedMeshRenderer renderer,Mesh mesh)
        {
            Vector3[] points=mesh.vertices;Vector2[] uvs=mesh.uv;BoneWeight[] weights=mesh.boneWeights;
            string[] names=renderer.bones.Select(bone=>bone.name).ToArray();
            if(points.Length!=17230||weights.Length!=points.Length||uvs.Length!=points.Length)
                throw new InvalidOperationException("Native player skin layout changed before export.");
            Influence[] Values(BoneWeight weight)
            {
                int[] indices={weight.boneIndex0,weight.boneIndex1,weight.boneIndex2,weight.boneIndex3};
                float[] values={weight.weight0,weight.weight1,weight.weight2,weight.weight3};
                return Enumerable.Range(0,4).Where(index=>values[index]>0f).Select(index=>new Influence
                {
                    bone=names[indices[index]],weight=values[index]
                }).ToArray();
            }
            var skin=new Skin
            {
                vertices=Enumerable.Range(0,points.Length).Select(vertex=>new Vertex
                {
                    position=model.transform.InverseTransformPoint(renderer.transform.TransformPoint(points[vertex])),
                    uv=uvs[vertex],influences=Values(weights[vertex])
                }).ToArray()
            };
            string absolute=Path.GetFullPath(NativeExportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolute));
            File.WriteAllText(absolute,JsonUtility.ToJson(skin),new UTF8Encoding(false));
            SessionState.SetBool(NativeExportCompletedSessionKey,true);
            Debug.Log("Native player FBX skin weights exported before authored JSON override: "+absolute);
        }
    }
}
#endif
