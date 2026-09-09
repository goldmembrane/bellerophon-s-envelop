"""Shared transporter hand authoring. Never overwrite the original without a checked candidate."""
import bpy
import json
import sys
import bmesh
import heapq
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
# The immutable pre-task source remains the authoring input after the checked rig is promoted.
SOURCE = ROOT / 'Backups/PlayerHandsRig_2026-09-09/player.fbx'
EVIDENCE = ROOT / 'docs/validation/player_hands_rig_2026-09-09'

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE), use_anim=False, automatic_bone_orientation=False)
armature = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
mesh = next(o for o in bpy.data.objects if o.type == 'MESH' and o.vertex_groups.get('LeftHand'))
report = {'source': str(SOURCE), 'armature': armature.name, 'mesh': mesh.name,
          'vertices': len(mesh.data.vertices), 'polygons': len(mesh.data.polygons),
          'bones': [], 'hands': []}
for bone in armature.data.bones:
    report['bones'].append({'name': bone.name, 'parent': bone.parent.name if bone.parent else None,
                            'head': list(bone.head_local), 'tail': list(bone.tail_local)})
for side in ['Left', 'Right']:
    bone = armature.data.bones[side + 'Hand']
    matrix = bone.matrix_local.inverted() @ armature.matrix_world.inverted() @ mesh.matrix_world
    group = mesh.vertex_groups[bone.name].index
    vertices = []
    for vertex in mesh.data.vertices:
        weight = sum(g.weight for g in vertex.groups if g.group == group)
        if weight < .05:
            continue
        vertices.append({'index': vertex.index, 'local': list(matrix @ vertex.co), 'weight': weight})
    report['hands'].append({'side': side, 'vertices': vertices})
EVIDENCE.mkdir(parents=True, exist_ok=True)
(EVIDENCE / 'blender_source.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
for hand in report['hands']:
    print(hand['side'], 'vertices', len(hand['vertices']), 'bounds',
          [(min(v['local'][a] for v in hand['vertices']), max(v['local'][a] for v in hand['vertices'])) for a in range(3)])
print('INSPECT_ONLY_SOURCE_UNCHANGED')

if '--build' in sys.argv:
    # Measured joint centers in the Unity hand-local frame, in metres. Existing
    # joints are never reoriented; Blender's imported FBX uses centimetres and -X.
    joints = {
        'Left': {
            'Thumb': [( .031,.069,-.017),(.045,.119,-.035),(.048,.158,-.036),(.047,.184,-.036)],
            'Index': [(-.014,.132,-.032),(-.002,.174,-.027),(.009,.205,-.024),(.010,.226,-.019)],
            'Middle':[(-.017,.132,.010),(-.020,.181,.012),(-.009,.209,.018),(-.004,.228,.019)],
            'Ring':  [(-.012,.122,.044),(-.014,.168,.049),(-.004,.199,.050),(.000,.218,.051)],
            'Little':[( .011,.114,.072),(.010,.153,.074),(.014,.179,.074),(.016,.195,.073)]},
        'Right': {
            'Thumb': [(-.027,.065,-.047),(-.043,.114,-.065),(-.046,.156,-.071),(-.046,.180,-.071)],
            'Index': [( .000,.140,-.066),(-.010,.181,-.075),(-.015,.209,-.076),(-.016,.227,-.074)],
            'Middle':[( .012,.139,-.035),(.010,.181,-.039),(.002,.211,-.039),(.002,.232,-.036)],
            'Ring':  [( .019,.132,-.005),(.023,.177,-.012),(.012,.207,-.010),(.008,.230,-.008)],
            'Little':[( .027,.128,.028),(.023,.171,.029),(.005,.201,.029),(-.005,.221,.026)]}}

    def unity_point(point):
        return Vector((-point[0]*100, point[1]*100, point[2]*100))

    hand_matrices = {side: armature.data.bones[side+'Hand'].matrix_local.copy() for side in joints}
    mesh_to_arm = armature.matrix_world.inverted() @ mesh.matrix_world
    mesh_to_hand = {side: hand_matrices[side].inverted() @ mesh_to_arm for side in joints}
    original_bones = {bone.name: bone.matrix_local.copy() for bone in armature.data.bones}
    original_positions = [vertex.co.copy() for vertex in mesh.data.vertices]
    hand_groups = {side: mesh.vertex_groups[side+'Hand'].index for side in joints}
    original_group_weights = {vertex.index: [(g.group, g.weight) for g in vertex.groups] for vertex in mesh.data.vertices}

    def to_unity(point):
        return Vector((-point.x*.01, point.y*.01, point.z*.01))

    def segment_distance(point, a, b):
        delta = b-a
        t = max(0., min(1., (point-a).dot(delta) / delta.length_squared))
        return (point-(a+delta*t)).length

    def finger_at(side, point):
        candidates = []
        for name, values in joints[side].items():
            ps = [Vector(v) for v in values]
            score = min(segment_distance(point, ps[i], ps[i+1]) for i in range(3))
            candidates.append((score, name, ps))
        return min(candidates, key=lambda item: item[0])

    # Add subdivisions on the existing triangle surfaces. Smooth=0 preserves the
    # source silhouette. Custom corner normals and UVs interpolate with the cuts.
    corner_normals = [n.vector.copy() for n in mesh.data.corner_normals]
    bm = bmesh.new()
    bm.from_mesh(mesh.data)
    bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
    saved_normal = bm.loops.layers.float_vector.new('HandRigPreservedNormal')
    for face, polygon in zip(bm.faces, mesh.data.polygons):
        for loop, loop_index in zip(face.loops, polygon.loop_indices):
            loop[saved_normal] = corner_normals[loop_index]
    deform = bm.verts.layers.deform.active
    selected_edges = []
    for edge in bm.edges:
        if edge.calc_length() < .8:
            continue
        for side in joints:
            if min(v[deform].get(hand_groups[side], 0.) for v in edge.verts) < .99:
                continue
            points = [to_unity(mesh_to_hand[side] @ v.co) for v in edge.verts]
            midpoint = (points[0]+points[1])*.5
            _, _, chain = finger_at(side, midpoint)
            if midpoint.y > chain[0].y-.016 and midpoint.y < chain[-1].y-.009:
                selected_edges.append(edge)
                break
    bmesh.ops.subdivide_edges(bm, edges=selected_edges, cuts=7, use_grid_fill=True, smooth=0.)
    bmesh.ops.triangulate(bm, faces=[face for face in bm.faces if len(face.verts)>3])
    bm.to_mesh(mesh.data); bm.free(); mesh.data.update()
    normal_attr = mesh.data.attributes.get('HandRigPreservedNormal')
    if normal_attr:
        mesh.data.normals_split_custom_set([entry.vector.normalized() for entry in normal_attr.data])
        mesh.data.attributes.remove(normal_attr)

    # A Euclidean nearest-bone label can cross the air gap between adjacent digits.
    # Propagate digit ownership along the existing mesh surface from distal pads instead.
    hand_points = {side: [to_unity(mesh_to_hand[side] @ v.co) for v in mesh.data.vertices] for side in joints}
    adjacency = [[] for _ in mesh.data.vertices]
    for edge in mesh.data.edges:
        a, b = edge.vertices
        adjacency[a].append(b); adjacency[b].append(a)
    coincident = {}
    for vertex in mesh.data.vertices:
        key = tuple(round(c, 5) for c in vertex.co)
        if key in coincident:
            other = coincident[key]
            adjacency[vertex.index].append(other); adjacency[other].append(vertex.index)
        else:
            coincident[key] = vertex.index
    geodesic = {}
    hand_budgets = {side: [sum(g.weight for g in v.groups if g.group == hand_groups[side])
                           for v in mesh.data.vertices] for side in joints}
    pad_anchors = {side: set() for side in joints}
    for side, fingers in joints.items():
        points = hand_points[side]
        eligible = {v.index for v in mesh.data.vertices
                    if any(g.group == hand_groups[side] and g.weight > .001 for g in v.groups)}
        geodesic[side] = {}
        for finger, values in fingers.items():
            chain = [Vector(v) for v in values]
            seeds = [i for i in eligible if points[i].y > chain[2].y+.004
                     and segment_distance(points[i], chain[2], chain[3]) < .016]
            if not seeds:
                raise RuntimeError('No distal surface anchors: '+side+finger)
            pad_anchors[side].update(seeds)
            distances = [float('inf')]*len(points)
            queue = []
            for i in seeds:
                distances[i] = (points[i]-chain[3]).length
                heapq.heappush(queue, (distances[i], i))
            while queue:
                distance, i = heapq.heappop(queue)
                if distance != distances[i]:
                    continue
                for n in adjacency[i]:
                    if n not in eligible:
                        continue
                    candidate = distance+(points[i]-points[n]).length
                    if candidate < distances[n]:
                        distances[n] = candidate
                        heapq.heappush(queue, (candidate, n))
            geodesic[side][finger] = distances

    bpy.context.view_layer.objects.active = armature
    armature.select_set(True); mesh.select_set(False)
    bpy.ops.object.mode_set(mode='EDIT')
    created = []
    for side, fingers in joints.items():
        for finger, points in fingers.items():
            parent = armature.data.edit_bones[side+'Hand']
            for index in range(3):
                name = side + finger + ['Proximal','Intermediate','Distal'][index]
                bone = armature.data.edit_bones.new(name)
                bone.head = hand_matrices[side] @ unity_point(points[index])
                bone.tail = hand_matrices[side] @ unity_point(points[index+1])
                bone.parent = parent
                bone.use_connect = False
                palm = Vector((0,0,1)) if finger == 'Thumb' else Vector((1 if side=='Left' else -1,0,0))
                bend_normal = hand_matrices[side].to_3x3() @ Vector((-palm.x,palm.y,palm.z))
                # Local Z is the flexion axis. The sign is exported in the rig manifest.
                flex_axis = bend_normal.cross((bone.tail-bone.head).normalized()).normalized()
                bone.align_roll(flex_axis)
                parent = bone
                created.append(name)
    bpy.ops.object.mode_set(mode='OBJECT')
    for name in created:
        mesh.vertex_groups.new(name=name)

    changed = {'Left': 0, 'Right': 0}
    for vertex in mesh.data.vertices:
        for side in joints:
            hand_weight = sum(g.weight for g in vertex.groups if g.group == hand_groups[side])
            if hand_weight < .001:
                continue
            p = to_unity(mesh_to_hand[side] @ vertex.co)
            _, nearest_finger, _ = finger_at(side, p)
            finger = min(joints[side], key=lambda name: geodesic[side][name][vertex.index])
            if not geodesic[side][finger][vertex.index] < float('inf'):
                finger = nearest_finger
            chain = [Vector(v) for v in joints[side][finger]]
            distance = min(segment_distance(p,chain[i],chain[i+1]) for i in range(3))
            direction = (chain[1]-chain[0]).normalized()
            along = (p-chain[0]).dot(direction)
            influence = max(0., min(1., (along+.008)/.048 if finger == 'Thumb' else (along+.012)/.024))
            influence = influence*influence*(3-2*influence)
            thumb_web = 0.
            if finger == 'Thumb':
                # The thumb-index web belongs partly to the palm, not wholly to the
                # rotating metacarpal. Preserve distal thumb weights beyond the web.
                delta = chain[1]-chain[0]
                t = max(0., min(1., (p-chain[0]).dot(delta)/delta.length_squared))
                center = chain[0]+delta*t
                sign = 1 if side == 'Left' else -1
                # Index-facing web is on the dorsal side of the thumb centerline,
                # mainly across hand-local X, not merely on its positive Z side.
                center = min((chain[i]+(chain[i+1]-chain[i])*max(0.,min(1.,
                    (p-chain[i]).dot(chain[i+1]-chain[i])/(chain[i+1]-chain[i]).length_squared))
                    for i in range(3)), key=lambda q: (p-q).length_squared)
                web = max(0., min(1., (-sign*(p.x-center.x)-.010)/.018))
                web = web*web*(3-2*web)
                # This is proximal web tissue, not the distal thumb pad. A lateral-only
                # mask left distal vertices partly attached to Hand even past the IP joint.
                distal = max(0., min(1., (p.y-chain[1].y-.005)/(chain[2].y-chain[1].y-.010)))
                web *= 1-distal*distal*(3-2*distal)
                thumb_web = web
                influence *= 1-.95*web
            if influence < .0001 or distance > .060:
                continue
            # Smooth joint bands avoid a single collapsing crease while leaving
            # the palm and existing forearm/wrist blending untouched.
            arc = [0.]
            for i in range(3): arc.append(arc[-1]+(chain[i+1]-chain[i]).length)
            best = min(range(3), key=lambda i: segment_distance(p,chain[i],chain[i+1]))
            delta = chain[best+1]-chain[best]
            t = max(0., min(1., (p-chain[best]).dot(delta)/delta.length_squared))
            s = arc[best]+t*delta.length
            blend_width = .012
            blends = [1.,0.,0.]
            for joint in [1,2]:
                f = max(0.,min(1.,(s-arc[joint]+blend_width)/(2*blend_width)))
                f = f*f*(3-2*f)
                if joint == 1: blends = [1-f,f,0.]
                else: blends = [blends[0],blends[1]*(1-f),f]
            if finger == 'Thumb':
                # Web tissue must not follow a distal thumb hinge beside a fixed index base.
                blends = [blends[0]+thumb_web*(blends[1]+blends[2]),
                          (1-thumb_web)*blends[1],(1-thumb_web)*blends[2]]
            remain = hand_weight*(1-influence)
            if remain > .00001: mesh.vertex_groups[side+'Hand'].add([vertex.index],remain,'REPLACE')
            else: mesh.vertex_groups[side+'Hand'].remove([vertex.index])
            for i, blend in enumerate(blends):
                if blend > .00001:
                    mesh.vertex_groups[side+finger+['Proximal','Intermediate','Distal'][i]].add([vertex.index],hand_weight*influence*blend,'REPLACE')
            changed[side] += 1

    # Diffuse only the hand's existing weight budget over connected glove vertices.
    # Pinned distal pads preserve independent digits; wrist/non-hand influences are untouched.
    # This removes discontinuous thumb/index ownership that stretched 1.4 mm edges to 78 mm.
    for side, fingers in joints.items():
        group_names = [side+'Hand']+[side+finger+joint for finger in fingers
                        for joint in ['Proximal','Intermediate','Distal']]
        group_ids = [mesh.vertex_groups[name].index for name in group_names]
        ids = [i for i, budget in enumerate(hand_budgets[side]) if budget > .001]
        local_index = {v: i for i, v in enumerate(ids)}
        field = np.zeros((len(ids), len(group_ids)), dtype=np.float64)
        for i, v in enumerate(ids):
            for group in mesh.data.vertices[v].groups:
                if group.group in group_ids:
                    field[i, group_ids.index(group.group)] = group.weight/hand_budgets[side][v]
        source_rows, neighbor_rows, conductance = [], [], []
        for i, v in enumerate(ids):
            for n in adjacency[v]:
                if n in local_index:
                    source_rows.append(i); neighbor_rows.append(local_index[n])
                    conductance.append(1/max(.001, (hand_points[side][v]-hand_points[side][n]).length))
        source_rows = np.asarray(source_rows); neighbor_rows = np.asarray(neighbor_rows)
        conductance = np.asarray(conductance)
        totals = np.zeros(len(ids)); np.add.at(totals, source_rows, conductance)
        movable = np.asarray([v not in pad_anchors[side] and hand_points[side][v].y > .055 for v in ids])
        movable &= totals > 0
        for iteration in range(100):
            average = np.zeros_like(field)
            np.add.at(average, source_rows, field[neighbor_rows]*conductance[:, None])
            average /= np.maximum(totals[:, None], 1e-12)
            field[movable] = .55*field[movable]+.45*average[movable]
        for i, v in enumerate(ids):
            if not movable[i]:
                continue
            budget = hand_budgets[side][v]
            old_other = [g for g in mesh.data.vertices[v].groups if g.group not in group_ids and g.weight > .00001]
            slots = max(1, 4-len(old_other))
            selected = sorted(range(len(group_ids)), key=lambda k: field[i,k], reverse=True)[:slots]
            total = sum(field[i,k] for k in selected)
            for name in group_names:
                mesh.vertex_groups[name].remove([v])
            for k in selected:
                if field[i,k] > .000001:
                    mesh.vertex_groups[group_names[k]].add([v], budget*field[i,k]/total, 'REPLACE')

    max_bone_difference = max(max(abs(original_bones[name][r][c]-armature.data.bones[name].matrix_local[r][c])
                                  for r in range(4) for c in range(4)) for name in original_bones)
    if max_bone_difference > .00001:
        raise RuntimeError('An existing bone changed: '+str(max_bone_difference))
    # Every source vertex is retained at exactly the same position after subdivision.
    positions = {tuple(round(c,5) for c in v.co) for v in mesh.data.vertices}
    missing = sum(tuple(round(c,5) for c in p) not in positions for p in original_positions)
    if missing:
        raise RuntimeError('Source vertices moved or disappeared: '+str(missing))
    # Existing vertices retain every non-hand influence. New digit weights must
    # collapse to exactly the original Hand budget when those digits are at rest.
    max_other_weight_difference = 0.
    max_hand_budget_difference = 0.
    finger_ids = {side: [mesh.vertex_groups[name].index for name in created if name.startswith(side)]
                  for side in joints}
    for index, position in enumerate(original_positions):
        vertex = mesh.data.vertices[index]
        if (vertex.co-position).length > .00001:
            raise RuntimeError('Source index changed before weight comparison: '+str(index))
        before = dict(original_group_weights[index])
        after = {g.group: g.weight for g in vertex.groups}
        for group in set(before) | set(after):
            if group in hand_groups.values() or any(group in ids for ids in finger_ids.values()):
                continue
            max_other_weight_difference = max(max_other_weight_difference, abs(before.get(group, 0)-after.get(group, 0)))
        for side in joints:
            budget = after.get(hand_groups[side], 0)+sum(after.get(g, 0) for g in finger_ids[side])
            max_hand_budget_difference = max(max_hand_budget_difference, abs(before.get(hand_groups[side], 0)-budget))
    if max_other_weight_difference > .00001 or max_hand_budget_difference > .00001:
        raise RuntimeError('Original skin influences changed outside the hand budget')
    output = ROOT / 'Assets/_Project/Art/Player/HandsRig'
    output.mkdir(parents=True,exist_ok=True)
    bpy.ops.object.select_all(action='DESELECT')
    armature.select_set(True); mesh.select_set(True)
    bpy.ops.export_scene.fbx(filepath=str(output/'player_hands_candidate.fbx'),use_selection=True,
        object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False,
        axis_forward='-Z',axis_up='Y',mesh_smooth_type='OFF',use_tspace=True,path_mode='AUTO')
    manifest = {'sourceUnchanged':True,'existingBoneMatrixMaxDifference':max_bone_difference,
                'existingNonHandWeightMaxDifference':max_other_weight_difference,
                'existingHandBudgetMaxDifference':max_hand_budget_difference,
                'sourceVerticesRetained':len(original_positions),'newVertexCount':len(mesh.data.vertices),
                'subdividedEdges':len(selected_edges),'newBones':created,'weightedVertices':changed,'jointsUnity':joints}
    (EVIDENCE/'candidate_authoring.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
    print('CANDIDATE_CREATED',json.dumps({k:v for k,v in manifest.items() if k not in ['newBones','jointsUnity']}))
    # Keep the Unity import companion synchronized with this exact exported FBX.
    import runpy
    runpy.run_path(str(ROOT/'scripts/player_hands_rig/export_authored_skin_weights.py'), run_name='__main__')
