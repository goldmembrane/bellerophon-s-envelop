"""Read original/candidate rest surfaces; never change a mesh or a pose."""
import bpy
import json
import numpy as np
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree

root=Path(__file__).resolve().parents[2]
reports=[]
def segment_face(start,end,tri):
    direction=end-start;u=tri[1]-tri[0];v=tri[2]-tri[0]
    p=np.cross(direction,v);det=np.dot(u,p)
    if abs(det)<1e-10:return None
    inverse=1./det;offset=start-tri[0];a=np.dot(offset,p)*inverse
    q=np.cross(offset,u);b=np.dot(direction,q)*inverse;t=np.dot(v,q)*inverse
    if 1e-5<t<1-1e-5 and a>1e-5 and b>1e-5 and a+b<1-1e-5:return start+t*direction
    return None
for relative in ['Backups/PlayerHandsRig_2026-09-09/player.fbx',
                 'Backups/ConsumableItemGrips_2026-09-09/finger_weights_before/player.fbx',
                 'Assets/_Project/Art/Player/HandsRig/player_hands_candidate.fbx']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(root/relative),use_anim=False)
    obj=next(o for o in bpy.data.objects if o.type=='MESH');arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
    mesh=obj.data;to_hand=arm.data.bones['RightHand'].matrix_local.inverted()@arm.matrix_world.inverted()@obj.matrix_world
    points=np.array([list(to_hand@v.co) for v in mesh.vertices])
    mesh.calc_loop_triangles()
    selected=[];source_faces=[]
    for face in mesh.loop_triangles:
        p=points[list(face.vertices)]
        if np.all((p[:,1]>9)&(p[:,1]<28)&(np.abs(p[:,0])<10)&(np.abs(p[:,2])<10)):
            selected.append(list(face.vertices));source_faces.append(face.polygon_index)
    faces=np.array(selected,dtype=int)
    tree=BVHTree.FromPolygons([Vector(p) for p in points],selected,all_triangles=True)
    found=[];visited=set()
    for a,b in tree.overlap(tree):
        if a==b or (min(a,b),max(a,b)) in visited:continue
        visited.add((min(a,b),max(a,b)))
        if set(faces[a])&set(faces[b]):continue
        hits=[]
        for first,second in [(a,b),(b,a)]:
            triangle=points[faces[first]]
            for edge in [(0,1),(1,2),(2,0)]:
                hit=segment_face(triangle[edge[0]],triangle[edge[1]],points[faces[second]])
                if hit is not None:hits.append(hit.tolist())
        if hits:found.append({'faces':[int(source_faces[a]),int(source_faces[b])],
                             'vertices':[faces[a].tolist(),faces[b].tolist()],'hitsCm':hits})
    report={'source':relative,'handTriangles':len(faces),'intersectingPairs':len(found),'pairs':found}
    reports.append(report);print('REST_HAND_INTERSECTIONS',relative,len(found),json.dumps(found[:8]),flush=True)
(root/'docs/validation/consumable_item_grips_2026-09-09/rest_hand_self_intersections.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
