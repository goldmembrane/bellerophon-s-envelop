"""Preserve the FBX's authored influences through Unity's small-weight import pruning."""
import bpy
import json
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[2]
folder = root/'Assets/_Project/Art/Player/HandsRig'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(folder/'player_hands_candidate.fbx'), use_anim=False, automatic_bone_orientation=False)
mesh = next(o for o in bpy.data.objects if o.type == 'MESH')
rows = []
seen = set()
def point(value):
    return {'x': -value.x, 'y': value.z, 'z': -value.y}
for loop in mesh.data.loops:
    vertex = mesh.data.vertices[loop.vertex_index]
    uv = mesh.data.uv_layers.active.data[loop.index].uv
    key = (vertex.index, round(uv.x, 7), round(uv.y, 7))
    if key in seen:
        continue
    seen.add(key)
    influences = [{'bone': mesh.vertex_groups[g.group].name, 'weight': g.weight}
                  for g in vertex.groups if g.weight > 0]
    if len(influences) > 4 or abs(sum(g['weight'] for g in influences)-1) > .0001:
        raise RuntimeError('Authored skin must have four or fewer normalized influences: '+str(vertex.index))
    rows.append({'position': point(mesh.matrix_world @ vertex.co), 'uv': {'x': uv.x, 'y': uv.y}, 'influences': influences})
filename = 'player_hands_candidate_weights.json' if '--candidate-only' in sys.argv else 'player_hands_skin_weights.json'
(folder/filename).write_text(json.dumps({'vertices': rows}, separators=(',', ':')), encoding='utf-8')
print('EXPORTED_AUTHORED_SKIN_ROWS', len(rows))
