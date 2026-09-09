"""Fair newly inserted hole vertices only; original positions/faces/UV/weights stay fixed."""
import bpy
import json
import numpy as np
import runpy
import sys
from pathlib import Path
from mathutils.kdtree import KDTree
from mathutils import Vector

root=Path(__file__).resolve().parents[2]
folder=root/'Assets/_Project/Art/Player/HandsRig'
evidence=root/'docs/validation/consumable_item_grips_2026-09-09'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Backups/ConsumableItemGrips_2026-09-09/topology_before/player_hands_candidate.fbx'),use_anim=False)
original=next(o for o in bpy.data.objects if o.type=='MESH')
old_points=np.array([list(v.co) for v in original.data.vertices])
def key(p):return tuple(np.round(p,5))
old_faces={tuple(sorted(key(old_points[v]) for v in f.vertices)) for f in original.data.polygons}
tree=KDTree(len(old_points))
for i,p in enumerate(old_points):tree.insert(p,i)
tree.balance()
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(folder/'player_hands_candidate.fbx'),use_anim=False)
obj=next(o for o in bpy.data.objects if o.type=='MESH');arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
mesh=obj.data;points=np.array([list(v.co) for v in mesh.vertices]);saved_normals=[n.vector.copy() for n in mesh.corner_normals]
free=np.array([i for i,p in enumerate(points) if tree.find(p)[2]>.00001],dtype=int)
if len(free)!=255:raise RuntimeError('Unexpected newly inserted patch vertex count: '+str(len(free)))
free_set=set(free);column={v:i for i,v in enumerate(free)}
neighbors=[set() for p in points]
for edge in mesh.edges:
    a,b=edge.vertices;neighbors[a].add(b);neighbors[b].add(a)
rows=sorted(free_set|{v for a in free for v in neighbors[a]})
design=np.zeros((len(rows),len(free)));rhs=np.zeros((len(rows),3))
for r,v in enumerate(rows):
    # Inverse-edge weighting limits tessellation-density bias; fixed one-ring vertices
    # enforce continuation of the original surface at the hole perimeter.
    adjacent=sorted(neighbors[v]);w=1/np.maximum(.01,np.linalg.norm(points[adjacent]-points[v],axis=1));w/=w.sum()
    for j,value in [(v,1.)]+[(j,-value) for j,value in zip(adjacent,w)]:
        if j in column:design[r,column[j]]+=value
        else:rhs[r]-=points[j]*value
solution=np.linalg.lstsq(design,rhs,rcond=None)[0]
delta=solution-points[free]
triangles=np.array([list(f.vertices) for f in mesh.polygons])
patch=np.array([i for i,f in enumerate(mesh.polygons) if tuple(sorted(key(points[v]) for v in f.vertices)) not in old_faces])
if len(patch)!=605:raise RuntimeError('The existing surface correspondence changed.')
faces=triangles[patch];a,b,c=faces.T
before=np.cross(points[b]-points[a],points[c]-points[a]);areas=np.linalg.norm(before,axis=1)
amount=min(1.,.3/max(1e-12,float(np.max(np.linalg.norm(delta,axis=1)))))
for unused in range(12):
    trial=points.copy();trial[free]+=delta*amount
    after=np.cross(trial[b]-trial[a],trial[c]-trial[a])
    alignment=np.sum(before*after,axis=1)/np.maximum(1e-15,areas*np.linalg.norm(after,axis=1))
    if np.min(alignment)>.25 and np.min(np.linalg.norm(after,axis=1)/areas)>.25:break
    amount*=.5
else:raise RuntimeError('Patch curvature would invert a face; no export.')
for v in free:mesh.vertices[int(v)].co=trial[v]
mesh.update()
# Continue shading only on new patch corners. Existing custom normals are untouched.
patch_set=set(patch)
incident=[[] for p in points]
for polygon in mesh.polygons:
    for loop_index in polygon.loop_indices:incident[mesh.loops[loop_index].vertex_index].append((polygon.index,loop_index))
normals=list(saved_normals)
for face_index in patch:
    polygon=mesh.polygons[int(face_index)]
    for loop_index in polygon.loop_indices:
        v=mesh.loops[loop_index].vertex_index
        original=[saved_normals[l] for f,l in incident[v] if f not in patch_set and saved_normals[l].dot(polygon.normal)>.25]
        if original:
            normal=sum(original,Vector())
        else:
            normal=sum((mesh.polygons[f].normal*mesh.polygons[f].area for f,l in incident[v] if mesh.polygons[f].normal.dot(polygon.normal)>.25),Vector())
        normals[loop_index]=normal.normalized() if normal.length>1e-8 else polygon.normal
mesh.normals_split_custom_set(normals)
for i,p in enumerate(points):
    if i not in free_set and (mesh.vertices[i].co-Vector(p)).length>1e-7:raise RuntimeError('Original vertex moved.')
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(folder/'player_hands_candidate.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False,
    axis_forward='-Z',axis_up='Y',mesh_smooth_type='OFF',use_tspace=True,path_mode='AUTO')
report={'newVertices':len(free),'newFaces':len(patch),'fraction':amount,
        'maxMoveCm':float(np.max(np.linalg.norm(delta*amount,axis=1))),
        'minimumFaceAlignment':float(np.min(alignment)),'originalPositionsPreserved':True,
        'originalNormalsPreserved':True,'directReviewRequired':True}
(evidence/'patch_curvature_authoring.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('PATCH_CURVATURE',json.dumps(report),flush=True)
sys.argv+=['--candidate-only']
runpy.run_path(str(root/'scripts/player_hands_rig/export_authored_skin_weights.py'),run_name='__main__')
