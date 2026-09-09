"""Read the authored FBX skin data at the unmatched Unity surface point."""
import bpy
from pathlib import Path
from mathutils import Vector

root = Path(__file__).resolve().parents[2]
for path in [root/'Backups/PlayerHandsRig_2026-09-09/player.fbx',
             root/'Assets/_Project/Art/Player/HandsRig/player_hands_candidate.fbx']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(path), use_anim=False, automatic_bone_orientation=False)
    mesh = next(o for o in bpy.data.objects if o.type == 'MESH')
    # Blender world is right-handed Z-up; the Unity point is metres, left-handed Y-up.
    target = Vector((-.37963260, -.16141260, .77676780))
    print('FILE', path, 'matrix', mesh.matrix_world)
    for vertex in sorted(mesh.data.vertices, key=lambda v: (mesh.matrix_world @ v.co-target).length_squared)[:8]:
        print('VERTEX', vertex.index, 'world', list(mesh.matrix_world @ vertex.co),
              'weights', [(mesh.vertex_groups[g.group].name, g.weight) for g in vertex.groups])
