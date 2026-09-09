"""Read-only atlas chart measurements for the approved right-hand reconstruction."""
import bpy
import json
from pathlib import Path
from collections import defaultdict
from mathutils import Vector

root=Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Backups/ConsumableItemGrips_2026-09-09/finger_shape_before/player_hands_candidate.fbx'),use_anim=False)
obj=next(o for o in bpy.data.objects if o.type=='MESH');arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
mesh=obj.data;mesh.calc_loop_triangles();uv=mesh.uv_layers.active.data
matrix=arm.data.bones['RightHand'].matrix_local.inverted()@arm.matrix_world.inverted()@obj.matrix_world
points=[matrix@v.co for v in mesh.vertices]
faces=[t for t in mesh.loop_triangles if all(abs(points[v].x)<11 and 6<points[v].y<28 and abs(points[v].z)<11 for v in t.vertices)]
edges=defaultdict(list)
for i,t in enumerate(faces):
    corners=[(v,tuple(round(x,5) for x in uv[l].uv)) for v,l in zip(t.vertices,t.loops)]
    for a,b in [(0,1),(1,2),(2,0)]:edges[tuple(sorted([corners[a],corners[b]]))].append(i)
adj=defaultdict(set)
for entries in edges.values():
    for i in entries:adj[i].update(entries)
remaining=set(range(len(faces)));charts=[]
while remaining:
    seed=min(remaining);remaining.remove(seed);todo=[seed];component={seed}
    while todo:
        for i in adj[todo.pop()]:
            if i in remaining:remaining.remove(i);component.add(i);todo.append(i)
    verts={v for i in component for v in faces[i].vertices};tex=[uv[l].uv for i in component for l in faces[i].loops]
    charts.append({'triangles':len(component),'vertices':len(verts),'center':list(sum((points[v] for v in verts),Vector())/len(verts)),
        'bounds':[[min(points[v][k] for v in verts),max(points[v][k] for v in verts)] for k in range(3)],
        'uvBounds':[[min(p[k] for p in tex),max(p[k] for p in tex)] for k in range(2)],
        'samples':[{'p':list(points[v]),'uv':list(uv[l].uv)} for i in sorted(component)[::max(1,len(component)//10)] for v,l in zip(faces[i].vertices,faces[i].loops)]})
charts.sort(key=lambda c:-c['triangles'])
(root/'docs/validation/consumable_item_grips_2026-09-09/right_hand_uv_charts.json').write_text(json.dumps(charts,indent=2),encoding='utf-8')
for i,c in enumerate(charts):print(i,{k:v for k,v in c.items() if k!='samples'})
