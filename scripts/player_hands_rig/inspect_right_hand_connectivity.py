"""Read-only connected digit section audit on the pre-repair model."""
import bpy
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Backups/ConsumableItemGrips_2026-09-09/finger_weights_before/player.fbx'), use_anim=False)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
mesh = next(o for o in bpy.data.objects if o.type == 'MESH')
to_hand = arm.data.bones['RightHand'].matrix_local.inverted() @ arm.matrix_world.inverted() @ mesh.matrix_world
points = [to_hand @ v.co for v in mesh.data.vertices]
points = [(-p.x*.01,p.y*.01,p.z*.01) for p in points]
groups = {g.index for g in mesh.vertex_groups if g.name=='RightHand' or any(g.name.startswith('Right'+d) for d in ['Thumb','Index','Middle','Ring','Little'])}
eligible = {v.index for v in mesh.data.vertices if sum(g.weight for g in v.groups if g.group in groups)>.001}
adj = {i:set() for i in eligible}
for edge in mesh.data.edges:
    a,b=edge.vertices
    if a in eligible and b in eligible:
        adj[a].add(b);adj[b].add(a)
same = {}
for i in eligible:
    key=tuple(round(c,7) for c in points[i])
    if key in same:
        adj[i].add(same[key]);adj[same[key]].add(i)
    else: same[key]=i
report=[]
for height in [.13,.14,.15,.16,.165,.17,.175,.18,.185,.19]:
    remaining={i for i in eligible if points[i][1]>height}
    components=[]
    while remaining:
        seed=remaining.pop();queue=[seed];members=[seed]
        while queue:
            i=queue.pop()
            for n in adj[i]:
                if n in remaining:
                    remaining.remove(n);queue.append(n);members.append(n)
        components.append({'count':len(members),'bounds':[[min(points[i][axis] for i in members),max(points[i][axis] for i in members)] for axis in range(3)]})
    report.append({'height':height,'components':sorted(components,key=lambda c:-c['count'])})
(root/'docs/validation/consumable_item_grips_2026-09-09/right_digit_connectivity.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
