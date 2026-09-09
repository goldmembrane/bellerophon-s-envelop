"""Reconstruct only approved right digits; retain original wrist/body and existing atlas."""
import bpy
import bmesh
import json
import math
import numpy as np
import runpy
import sys
import ast
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils.geometry import barycentric_transform
from mathutils.kdtree import KDTree
from collections import defaultdict

root=Path(__file__).resolve().parents[2]
folder=root/'Assets/_Project/Art/Player/HandsRig'
evidence=root/'docs/validation/consumable_item_grips_2026-09-09'
source=root/'Backups/ConsumableItemGrips_2026-09-09/finger_shape_before/player_hands_candidate.fbx'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source),use_anim=False,automatic_bone_orientation=False)
obj=next(o for o in bpy.data.objects if o.type=='MESH');arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
original=obj.data.copy();original.calc_loop_triangles()
to_hand=arm.data.bones['RightHand'].matrix_local.inverted()@arm.matrix_world.inverted()@obj.matrix_world
to_mesh=to_hand.inverted();normal_to_mesh=to_mesh.to_3x3().inverted().transposed()
group_names=[g.name for g in obj.vertex_groups];hand_id=group_names.index('RightHand')
digits=['Thumb','Index','Middle','Ring','Little'];joints=['Proximal','Intermediate','Distal']
right_ids={i for i,n in enumerate(group_names) if n=='RightHand' or any(n.startswith('Right'+d) for d in digits)}
budgets=[sum(g.weight for g in v.groups if g.group in right_ids) for v in original.vertices]
positions=[to_hand@v.co for v in original.vertices]
eligible={i for i,v in enumerate(original.vertices) if budgets[i]>.01}
old_weights=[{g.group:g.weight for g in v.groups} for v in original.vertices]
old_tree=KDTree(len(positions))
for i,p in enumerate(positions):old_tree.insert(p,i)
old_tree.balance()
authored=ast.parse((root/'scripts/player_hands_rig/build_shared_hand_rig.py').read_text(encoding='utf-8-sig'))
joint_data=next(ast.literal_eval(node.value) for node in ast.walk(authored) if isinstance(node,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='joints' for t in node.targets))
chains={}
for digit in digits:
    bones=[arm.data.bones['Right'+digit+joint] for joint in joints]
    hand_inv=arm.data.bones['RightHand'].matrix_local.inverted()
    endpoint=joint_data['Right'][digit][-1]
    chains[digit]=[hand_inv@b.head_local for b in bones]+[Vector((-endpoint[0]*100,endpoint[1]*100,endpoint[2]*100))]

# Cut at 65 mm distal to the wrist. This seam is below the repaired fingers, not
# an alteration of the wrist joint, forearm or any left-hand surface.
cut=6.5;knuckle=14.5
def add_normal_attribute(bm):
    layer=bm.loops.layers.float_vector.new('FingerRepairProtectedNormal')
    for face,polygon in zip(bm.faces,original.polygons):
        for loop,index in zip(face.loops,polygon.loop_indices):loop[layer]=original.corner_normals[index].vector
    return layer
body=bmesh.new();body.from_mesh(original);body.verts.ensure_lookup_table();body.faces.ensure_lookup_table()
protected_normal=add_normal_attribute(body)
hand_faces=[f for f in body.faces if all(v.index in eligible for v in f.verts)]
hand_edges={e for f in hand_faces for e in f.edges};hand_verts={v for f in hand_faces for v in f.verts}
plane=to_mesh@Vector((0,cut,0));normal=to_hand.to_3x3().transposed()@Vector((0,1,0))
bmesh.ops.bisect_plane(body,geom=list(hand_verts)+list(hand_edges)+hand_faces,dist=1e-6,
    plane_co=plane,plane_no=normal.normalized(),clear_outer=True,clear_inner=False)
body.verts.ensure_lookup_table()
seam=[v for v in body.verts if abs((to_hand@v.co).y-cut)<1e-4 and any(e.is_boundary for e in v.link_edges)]
if len(seam)<8:raise RuntimeError('No stable original wrist-to-palm seam.')

palm=bmesh.new();palm.from_mesh(original);palm.verts.ensure_lookup_table()
for v in palm.verts:v.co=to_hand@v.co
bmesh.ops.delete(palm,geom=[f for f in palm.faces if not all(v.index in eligible for v in f.verts)],context='FACES')
for height,inner,outer in [(cut,True,False),(knuckle,False,True)]:
    bmesh.ops.bisect_plane(palm,geom=list(palm.verts)+list(palm.edges)+list(palm.faces),dist=1e-6,
        plane_co=Vector((0,height,0)),plane_no=Vector((0,1,0)),clear_inner=inner,clear_outer=outer)
loose=[v for v in palm.verts if not v.link_faces]
if loose:bmesh.ops.delete(palm,geom=loose,context='VERTS')
def close_boundaries(bm):
    bm.verts.index_update();bm.edges.index_update()
    remaining={e for e in bm.edges if e.is_boundary};count=0
    while remaining:
        start=min(remaining,key=lambda e:e.index);remaining.remove(start);ordered=[start.verts[0],start.verts[1]];current=start
        while ordered[-1]!=ordered[0]:
            choices=[e for e in ordered[-1].link_edges if e in remaining]
            if len(choices)!=1:raise RuntimeError('Ambiguous palm boundary.')
            current=choices[0];remaining.remove(current);ordered.append(current.other_vert(ordered[-1]))
        ordered.pop()
        if start.link_loops[0].vert==ordered[0]:ordered.reverse()
        face=bm.faces.new(ordered);face.normal_update();count+=1
    bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if len(f.verts)>3])
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    return count
print('PALM_CAPS',close_boundaries(palm),'SEAM',len(seam),flush=True)
print('PALM_SOLID',palm.calc_volume(signed=True),'boundary',sum(e.is_boundary for e in palm.edges),
      'baseFaces',sum(all(abs(v.co.y-cut)<1e-4 for v in f.verts) for f in palm.faces),flush=True)
palm_data=bpy.data.meshes.new('RightFingerRepairPalm');palm.to_mesh(palm_data);palm.free()
palm_obj=bpy.data.objects.new('RightFingerRepairPalm',palm_data);bpy.context.collection.objects.link(palm_obj)
# Boolean authoring is local and disposable. Only its final joined surface is exported.
palm_obj.matrix_world=obj.matrix_world@to_mesh
bpy.context.view_layer.update()
for material in original.materials:palm_data.materials.append(material)

def at_distance(chain,distance):
    lengths=[(chain[i+1]-chain[i]).length for i in range(3)]
    if distance<0:return chain[0]+(chain[1]-chain[0]).normalized()*distance,(chain[1]-chain[0]).normalized()
    for i,length in enumerate(lengths):
        if distance<=length or i==2:
            direction=(chain[i+1]-chain[i]).normalized();return chain[i]+direction*distance,direction
        distance-=length
def make_finger(digit):
    chain=chains[digit];lengths=[(chain[i+1]-chain[i]).length for i in range(3)];total=sum(lengths)
    radius={'Thumb':1.25,'Index':1.25,'Middle':1.32,'Ring':1.23,'Little':1.12}[digit]
    start=2.1 if digit=='Thumb' else -1.7
    tip_radius=radius*.76;cap_start=total-tip_radius
    samples=list(np.arange(start,cap_start,.35))+[cap_start]
    for distance in [0,lengths[0],lengths[0]+lengths[1]]:
        samples.extend(distance+offset for offset in [-.55,-.3,0,.3,.55] if start<distance+offset<cap_start)
    samples=sorted(set(round(float(s),6) for s in samples))
    rings=[];verts=[];faces=[];sides=16
    def ring(distance,r):
        center,tangent=at_distance(chain,distance)
        u=(Vector((1,0,0))-tangent*tangent.x).normalized();v=tangent.cross(u).normalized()
        result=[]
        for j in range(sides):
            angle=2*math.pi*j/sides
            result.append(len(verts));verts.append(center+u*(math.cos(angle)*r)+v*(math.sin(angle)*r*.88))
        rings.append(result)
    for distance in samples:
        fraction=max(0.,distance)/total;r=radius*(1.-.24*fraction)
        # Small joint fullness, not a material/design addition.
        r*=1.+.025*sum(math.exp(-((distance-j)/.45)**2) for j in [lengths[0],sum(lengths[:2])])
        ring(distance,r)
    for angle in [math.pi/8,math.pi/4,3*math.pi/8]:ring(cap_start+tip_radius*math.sin(angle),tip_radius*math.cos(angle))
    for lower,upper in zip(rings,rings[1:]):
        for j in range(sides):faces.append((lower[j],lower[(j+1)%sides],upper[(j+1)%sides],upper[j]))
    faces.append(tuple(reversed(rings[0])))
    verts.append(chain[-1]);tip=len(verts)-1
    for j in range(sides):faces.append((rings[-1][j],rings[-1][(j+1)%sides],tip))
    data=bpy.data.meshes.new('Right'+digit+'Repair');data.from_pydata(verts,[],faces);data.update()
    audit=bmesh.new();audit.from_mesh(data)
    print('FINGER_SOLID',digit,audit.calc_volume(signed=True),'boundary',sum(e.is_boundary for e in audit.edges),flush=True);audit.free()
    finger=bpy.data.objects.new(data.name,data);bpy.context.collection.objects.link(finger);finger.matrix_world=palm_obj.matrix_world
    bpy.context.view_layer.update()
    for polygon in data.polygons:polygon.use_smooth=True
    return finger
for digit in digits:
    finger=make_finger(digit)
    bpy.context.view_layer.objects.active=palm_obj;palm_obj.select_set(True)
    modifier=palm_obj.modifiers.new('Join'+digit,'BOOLEAN');modifier.operation='UNION';modifier.solver='EXACT';modifier.object=finger
    print('AUTHOR_TRANSFORMS',palm_obj.matrix_world.determinant(),finger.matrix_world.determinant(),flush=True)
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    bpy.data.objects.remove(finger,do_unlink=True)
    print('JOINED_DIGIT',digit,len(palm_obj.data.vertices),len(palm_obj.data.polygons),
          'yBounds',min(v.co.y for v in palm_obj.data.vertices),max(v.co.y for v in palm_obj.data.vertices),flush=True)

joined=bmesh.new();joined.from_mesh(palm_obj.data)
caps=[f for f in joined.faces if all(abs(v.co.y-cut)<1e-4 for v in f.verts)]
if not caps:
    print('BASE_CAP_NEAREST',sorted([(max(abs(v.co.y-cut) for v in f.verts),[list(v.co) for v in f.verts]) for f in joined.faces],key=lambda p:p[0])[:3],flush=True)
    raise RuntimeError('The protected base cap was lost.')
bmesh.ops.delete(joined,geom=caps,context='FACES_ONLY')
orphan_edges=[e for e in joined.edges if not e.link_faces]
if orphan_edges:bmesh.ops.delete(joined,geom=orphan_edges,context='EDGES')
loose=[v for v in joined.verts if not v.link_faces]
if loose:bmesh.ops.delete(joined,geom=loose,context='VERTS')
bmesh.ops.triangulate(joined,faces=[f for f in joined.faces if len(f.verts)>3])
bmesh.ops.recalc_face_normals(joined,faces=list(joined.faces));joined.normal_update()
bad=[e for e in joined.edges if not e.is_manifold and not (e.is_boundary and all(abs(v.co.y-cut)<1e-4 for v in e.verts))]
if bad:
    print('HAND_JOIN_BAD_EDGES',[(len(e.link_faces),[list(v.co) for v in e.verts]) for e in bad[:10]],flush=True)
    raise RuntimeError('New finger surface has non-manifold edges: '+str(len(bad)))

# Surface atlas transfer is restricted to the existing right glove triangles.
source_triangles=[];source_uv=[]
for tri in original.loop_triangles:
    if not all(v in eligible for v in tri.vertices):continue
    source_triangles.append(list(tri.vertices));source_uv.append([Vector((*original.uv_layers.active.data[l].uv,0)) for l in tri.loops])
tree=BVHTree.FromPolygons(positions,source_triangles,all_triangles=True)
# Restrict every face to one existing UV island, but project each corner onto
# that island independently. Adjacent faces then share continuous coordinates
# instead of clamping every new triangle into a different tiny old triangle.
chart_edges=defaultdict(list)
for i,(tri,tex) in enumerate(zip(source_triangles,source_uv)):
    corners=[(v,tuple(round(x,5) for x in uv[:2])) for v,uv in zip(tri,tex)]
    for a,b in [(0,1),(1,2),(2,0)]:chart_edges[tuple(sorted((corners[a],corners[b])))].append(i)
adjacency=defaultdict(set)
for entries in chart_edges.values():
    for i in entries:adjacency[i].update(entries)
remaining=set(range(len(source_triangles)));chart_of={};chart_trees=[];chart_faces=[]
while remaining:
    seed=min(remaining);remaining.remove(seed);component={seed};todo=[seed]
    while todo:
        for i in adjacency[todo.pop()]:
            if i in remaining:remaining.remove(i);component.add(i);todo.append(i)
    faces=sorted(component);chart=len(chart_faces)
    for i in faces:chart_of[i]=chart
    chart_faces.append(faces);chart_trees.append(BVHTree.FromPolygons(positions,[source_triangles[i] for i in faces],all_triangles=True))
uv_layer=body.loops.layers.uv.active;deform=body.verts.layers.deform.active
def smooth(value):value=max(0.,min(1.,value));return value*value*(3-2*value)
def weights(point):
    _,old_index,old_gap=old_tree.find(point)
    other={g:w for g,w in old_weights[old_index].items() if g not in right_ids} if old_gap<.0001 else {}
    right_budget=1-sum(other.values())
    contributions=[]
    for digit,chain in chains.items():
        before=0;choices=[]
        for i in range(3):
            delta=chain[i+1]-chain[i];length=delta.length;t=max(0.,min(1.,(point-chain[i]).dot(delta)/(length*length)))
            closest=chain[i]+t*delta;choices.append(((point-closest).length,before+t*length))
            before+=length
        gap,distance=min(choices);lengths=[(chain[i+1]-chain[i]).length for i in range(3)]
        axial=(point-chain[0]).dot((chain[1]-chain[0]).normalized())
        root=smooth((axial+1.8)/3.6)
        # A continuous radial field leaves distant palm surfaces attached to the
        # hand and blends neighboring MCP influences without a Voronoi seam.
        radius={'Thumb':1.25,'Index':1.25,'Middle':1.32,'Ring':1.23,'Little':1.12}[digit]
        radial=1-smooth((gap-radius-.35)/1.8)
        strength=root*radial
        first=smooth((distance-lengths[0]+.85)/1.7)
        second=smooth((distance-sum(lengths[:2])+.65)/1.3)
        contributions.append((strength,digit,[(1-first),first*(1-second),second]))
    total=sum(v[0] for v in contributions);scale=1/max(1.,total)
    result={hand_id:max(0.,1-total)}
    for strength,digit,parts in contributions:
        for joint,value in zip(joints,parts):
            if strength*value>1e-8:result[group_names.index('Right'+digit+joint)]=strength*value*scale
    # Smooth fields overlap only around roots. Keep the strongest digit entries
    # and return the small omitted residual to Hand; preserve every other bone.
    available=3-len(other)
    retained=sorted(((g,w) for g,w in result.items() if g!=hand_id),key=lambda p:-p[1])[:available]
    result={g:w*right_budget for g,w in retained};result[hand_id]=(1-sum(w for _,w in retained))*right_budget
    if old_gap<.0001 and point.y<10.:
        blend=smooth((point.y-cut)/(10.-cut))
        prior={g:w for g,w in old_weights[old_index].items() if g in right_ids}
        result={g:result.get(g,0)*blend+prior.get(g,0)*(1-blend) for g in set(prior)|set(result)}
        retained=sorted(((g,w) for g,w in result.items() if g!=hand_id),key=lambda p:-p[1])[:available]
        result={g:w for g,w in retained};result[hand_id]=right_budget-sum(result.values())
    result={g:w for g,w in result.items() if w>1e-8}
    result.update(other)
    if len(result)>4:raise RuntimeError('New palm weights would discard an existing influence.')
    return result
for vertex in seam:
    previous=dict(vertex[deform]);other={g:w for g,w in previous.items() if g not in right_ids and w>1e-8}
    budget=1-sum(other.values())
    # Keep the pre-existing seam's finger influence too. Replacing it with pure
    # Hand creates a sharp discontinuity next to the protected wrist vertices.
    retained=sorted(((g,w) for g,w in previous.items() if g in right_ids and g!=hand_id),key=lambda p:-p[1])[:3-len(other)]
    values=dict(other);values.update(retained);values[hand_id]=budget-sum(w for _,w in retained)
    vertex[deform].clear()
    for group,value in values.items():
        if value>1e-8:vertex[deform][group]=value
new_map={};seam_used=set()
for vertex in joined.verts:
    point=to_mesh@vertex.co
    nearby=[old for old in seam if (old.co-point).length<.0001] if abs(vertex.co.y-cut)<1e-4 else []
    if nearby:
        new_map[vertex]=nearby[0];seam_used.add(nearby[0]);continue
    new=body.verts.new(point);new[deform].clear()
    for group,value in weights(vertex.co).items():
        if value>1e-8:new[deform][group]=value
    new_map[vertex]=new
if len(seam_used)!=len(seam):raise RuntimeError('Original wrist seam changed during boolean authoring.')
for face in joined.faces:
    new=body.faces.new([new_map[v] for v in face.verts]);new.material_index=face.material_index;new.smooth=True
    center=face.calc_center_median();nearest=tree.find_nearest(center)
    possibilities=tree.find_nearest_range(center,nearest[3]+1.5)
    aligned=[entry for entry in possibilities if entry[1].dot(face.normal)>.1]
    choice=min(aligned or [nearest],key=lambda entry:entry[3]+.25*(1-entry[1].dot(face.normal)))
    chart=chart_of[choice[2]]
    for old_loop,new_loop in zip(face.loops,new.loops):
        projected,_,local_index,_=chart_trees[chart].find_nearest(old_loop.vert.co)
        source_index=chart_faces[chart][local_index]
        triangle=source_triangles[source_index];tex=source_uv[source_index]
        xyz=[positions[v] for v in triangle]
        bary=barycentric_transform(projected,*xyz,Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1)))
        bary=Vector([max(0.,min(1.,v)) for v in bary]);bary/=max(1e-12,sum(bary))
        texel=sum((tex[i]*bary[i] for i in range(3)),Vector())
        new_loop[uv_layer].uv=(texel.x,texel.y)
        new_loop[protected_normal]=(normal_to_mesh@old_loop.vert.normal).normalized()
joined.free();body.normal_update()
if any(e.is_boundary and all((to_hand@v.co).y>=cut-.001 for v in e.verts) and all(v[deform].get(hand_id,0)+sum(v[deform].get(g,0) for g in right_ids if g!=hand_id)>.99 for v in e.verts) for e in body.edges):
    raise RuntimeError('Repaired hand is not closed after attachment.')
body.to_mesh(obj.data);body.free();obj.data.update()
normals=obj.data.attributes.get('FingerRepairProtectedNormal')
obj.data.normals_split_custom_set([entry.vector.normalized() for entry in normals.data]);obj.data.attributes.remove(normals)
if len(arm.data.bones)!=54:raise RuntimeError('The preserved skeleton changed.')
for vertex in obj.data.vertices:
    values=[g.weight for g in vertex.groups if g.weight>1e-8]
    if len(values)>4 or abs(sum(values)-1)>.0001:
        raise RuntimeError('Pre-export skin budget is invalid: '+str(vertex.index)+' '+str(values))
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(folder/'player_hands_candidate.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},
    add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False,axis_forward='-Z',axis_up='Y',mesh_smooth_type='OFF',use_tspace=True,path_mode='AUTO')
manifest={'kind':'RightFingerSurfaceReconstruction','source':str(source),'protectedHandLocalBelowMeters':cut*.01,
    'skeletonBones':54,'vertices':len(obj.data.vertices),'faces':len(obj.data.polygons),'seamVertices':len(seam),
    'existingAtlasOnly':True,'directReviewRequired':True}
(folder/'right_finger_repair_manifest.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
(evidence/'right_finger_surface_authoring.json').write_text(json.dumps(manifest,indent=2),encoding='utf-8')
print('RIGHT_FINGER_SURFACE_AUTHORING',json.dumps(manifest),flush=True)
sys.argv+=['--candidate-only']
runpy.run_path(str(root/'scripts/player_hands_rig/export_authored_skin_weights.py'),run_name='__main__')
