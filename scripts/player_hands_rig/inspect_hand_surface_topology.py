"""Read-only localization of intrinsic hand holes and winding seams in observed geometry."""
import json
from collections import defaultdict
from pathlib import Path
import numpy as np

root=Path(__file__).resolve().parents[2]
folder=root/'docs/validation/consumable_item_grips_2026-09-09'
d=json.loads((folder/'weight_pose_readout/Battery_Aux_Plug.json').read_text(encoding='utf-8'))
def matrix(m):return np.array([[m['e'+str(r)+str(c)] for c in range(4)] for r in range(4)])
p=np.array([[v['x'],v['y'],v['z'],1.] for v in d['vertices']])
rest=(p@matrix(d['handBind']).T)[:,:3]
unique={};mapping=[];representatives=[]
for i,point in enumerate(p):
    key=tuple(np.round(point[:3],5))
    if key not in unique:unique[key]=len(unique);representatives.append(i)
    mapping.append(unique[key])
mapping=np.array(mapping);rest=rest[representatives]
tri=mapping[np.array(d['triangles']).reshape(-1,3)]
edges=defaultdict(list)
for f,(a,b,c) in enumerate(tri):
    if len({a,b,c})<3:continue
    for u,v in [(a,b),(b,c),(c,a)]:edges[tuple(sorted((u,v)))].append((f,u,v))
def right(v):return -.1<rest[v,0]<.1 and .1<rest[v,1]<.3 and -.1<rest[v,2]<.1
boundary=[];winding=[];nonmanifold=[]
for (a,b),faces in edges.items():
    if not(right(a) and right(b)):continue
    row={'vertices':[int(representatives[a]),int(representatives[b])], 'rest':[rest[a].tolist(),rest[b].tolist()], 'faces':[int(f[0]) for f in faces]}
    if len(faces)==1:boundary.append(row)
    if len(faces)>2:nonmanifold.append(row)
    if len(faces)==2 and faces[0][1]==faces[1][1]:winding.append(row)
result={'boundaryEdges':boundary,'sameDirectionSharedEdges':winding,'nonManifoldEdges':nonmanifold}
(folder/'hand_surface_topology.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print({k:len(v) for k,v in result.items()})
print('boundary samples',boundary[:3])

# Independently inspect the unmodified FBX topology, before and after the earlier rig work.
import bpy
import bmesh
fbx_results=[]
for relative in ['Backups/PlayerHandsRig_2026-09-09/player.fbx',
                 'Backups/ConsumableItemGrips_2026-09-09/finger_weights_before/player.fbx',
                 'Assets/_Project/Art/Player/HandsRig/player_hands_candidate.fbx']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(root/relative),use_anim=False,automatic_bone_orientation=False)
    arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    mesh=next(o for o in bpy.data.objects if o.type=='MESH')
    to_hand=arm.data.bones['RightHand'].matrix_local.inverted()@arm.matrix_world.inverted()@mesh.matrix_world
    bm=bmesh.new();bm.from_mesh(mesh.data)
    def in_hand(v):
        q=to_hand@v.co
        return abs(q.x)<10 and 10<q.y<30 and abs(q.z)<10
    holes=[e for e in bm.edges if e.is_boundary and all(in_hand(v) for v in e.verts)]
    adjacency=defaultdict(set)
    for edge in holes:
        a,b=[v.index for v in edge.verts];adjacency[a].add(b);adjacency[b].add(a)
    remaining=set(adjacency);loops=[]
    while remaining:
        seed=remaining.pop();component={seed};stack=[seed]
        while stack:
            for neighbor in adjacency[stack.pop()]:
                if neighbor in remaining:remaining.remove(neighbor);component.add(neighbor);stack.append(neighbor)
        seed_vertex=next(v for v in bm.verts if v.index==seed)
        connected_faces=set(seed_vertex.link_faces);queue=list(connected_faces)
        while queue:
            for edge in queue.pop().edges:
                for face in edge.link_faces:
                    if face not in connected_faces:connected_faces.add(face);queue.append(face)
        connected_vertices={v for face in connected_faces for v in face.verts}
        loops.append({'vertices':sorted(component),'closedBoundary':all(len(adjacency[v])==2 for v in component),
                      'connectedFaceCount':len(connected_faces),'connectedVertexCount':len(connected_vertices)})
    points=[{'vertices':[v.index for v in e.verts],'local':[list(to_hand@v.co) for v in e.verts]} for e in holes]
    row={'source':relative,'bones':len(arm.data.bones),'rightHandBoundaryEdges':len(holes),'boundaryLoops':loops,'boundaries':points}
    fbx_results.append(row);print(relative,'bones',row['bones'],'rightHandBoundaryEdges',len(holes))
    print('boundary loops',[(len(loop['vertices']),loop['closedBoundary'],loop['connectedFaceCount'],loop['connectedVertexCount']) for loop in loops])
    if len(arm.data.bones)==24:
        uv=bm.loops.layers.uv.active
        for loop in loops:
            print('ORIGINAL_BOUNDARY_UV',[(v.index,tuple(round(x,3) for x in to_hand@v.co),
                [tuple(round(x,4) for x in corner[uv].uv) for corner in v.link_loops]) for v in bm.verts if v.index in loop['vertices']])
    bm.free()
(folder/'hand_source_topology_comparison.json').write_text(json.dumps(fbx_results,indent=2),encoding='utf-8')
