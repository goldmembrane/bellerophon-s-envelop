"""Close only the two recorded right-hand boundary loops; preserve every existing face."""
import bpy
import bmesh
import json
import runpy
import sys
import numpy as np
from pathlib import Path
from mathutils import Vector

root=Path(__file__).resolve().parents[2]
folder=root/'Assets/_Project/Art/Player/HandsRig'
source=root/'Backups/ConsumableItemGrips_2026-09-09/topology_before/player_hands_candidate.fbx'
evidence=root/'docs/validation/consumable_item_grips_2026-09-09'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(source),use_anim=False,automatic_bone_orientation=False)
arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
obj=next(o for o in bpy.data.objects if o.type=='MESH')
mesh=obj.data
to_hand=arm.data.bones['RightHand'].matrix_local.inverted()@arm.matrix_world.inverted()@obj.matrix_world
positions=[tuple(v.co) for v in mesh.vertices]
original_weights=[{g.group:g.weight for g in v.groups} for v in mesh.vertices]
original_faces=[tuple(p.vertices) for p in mesh.polygons]
original_uv=[tuple(v.uv) for v in mesh.uv_layers.active.data]
original_normals=[n.vector.copy() for n in mesh.corner_normals]
bm=bmesh.new();bm.from_mesh(mesh)
bm.verts.ensure_lookup_table();bm.faces.ensure_lookup_table()
saved_normal=bm.loops.layers.float_vector.new('RepairPreservedNormal')
old_face=bm.faces.layers.int.new('RepairOriginalFace')
uv=bm.loops.layers.uv.active
for face,polygon in zip(bm.faces,mesh.polygons):
    face[old_face]=polygon.index+1
    for loop,index in zip(face.loops,polygon.loop_indices):loop[saved_normal]=original_normals[index]
def in_hand(v):
    p=to_hand@v.co
    return abs(p.x)<10 and 10<p.y<30 and abs(p.z)<10
boundary=[e for e in bm.edges if e.is_boundary and all(in_hand(v) for v in e.verts)]
if len(boundary)!=99:raise RuntimeError('Expected exactly the recorded 99 boundary segments, not a new target.')
remaining=set(boundary);groups=[]
while remaining:
    start=remaining.pop();group={start};todo=[start]
    while todo:
        edge=todo.pop()
        for vertex in edge.verts:
            for adjacent in vertex.link_edges:
                if adjacent in remaining:remaining.remove(adjacent);group.add(adjacent);todo.append(adjacent)
    groups.append(group)
if sorted(len(g) for g in groups)!=[49,50]:raise RuntimeError('Boundary components differ from approved holes.')
deform=bm.verts.layers.deform.active
boundary_points=[]
for group in sorted(groups,key=lambda g:min(v.index for e in g for v in e.verts)):
    verts={v for edge in group for v in edge.verts}
    # Each boundary is one continuous hole. Select existing incident UVs, not a new texture.
    uv_candidates={v:[loop[uv].uv.copy() for loop in v.link_loops if loop.face[old_face]>0] for v in verts}
    centers=[q for values in uv_candidates.values() for q in values]
    # The hole straddles atlas seams. Interpolating unrelated islands produces striped patches.
    # Continue the dominant existing glove chart; retain all original corner UVs unchanged.
    center=max(centers,key=lambda q:sum(any((p-q).length<.08 for p in values) for values in uv_candidates.values()))
    chart={v:[p for p in values if (p-center).length<.08] for v,values in uv_candidates.items()}
    samples=[(v,p) for v,values in chart.items() for p in values]
    design=np.array([list(v.co)+[1.] for v,p in samples]);values=np.array([list(p) for v,p in samples])
    transform=np.linalg.lstsq(design,values,rcond=None)[0]
    low=values.min(axis=0);high=values.max(axis=0);margin=(high-low)*.05
    uv_for={}
    for v in verts:
        prediction=np.clip(np.array(list(v.co)+[1.])@transform,low-margin,high+margin)
        candidates=chart[v]
        uv_for[v]=min(candidates,key=lambda q:(q-Vector(prediction)).length).copy() if candidates else Vector(prediction)
    # Follow the explicitly inspected boundary, rather than the general holes-fill operator.
    start=min(group,key=lambda e:tuple(sorted(v.index for v in e.verts)));ordered=[start.verts[0],start.verts[1]];walked={start}
    while len(walked)<len(group):
        following=[edge for edge in ordered[-1].link_edges if edge in group and edge not in walked]
        if len(following)!=1:raise RuntimeError('Ambiguous boundary traversal.')
        edge=following[0];walked.add(edge);vertex=edge.other_vert(ordered[-1])
        if vertex==ordered[0]:break
        ordered.append(vertex)
    if len(ordered)!=len(group):raise RuntimeError('Boundary did not close once.')
    neighbor=start.link_loops[0]
    if ordered[0]==neighbor.vert:ordered.reverse()
    n=len(ordered);coordinates=np.array([list(v.co) for v in ordered])
    normal=np.sum(np.cross(coordinates,np.roll(coordinates,-1,axis=0)),axis=0)
    normal/=np.linalg.norm(normal)
    def diagonal(i,j):
        if abs(i-j)==1 or {i,j}=={0,n-1}:return True
        return bm.edges.get((ordered[i],ordered[j])) is None
    costs=np.full((n,n),float('inf'));splits={}
    for i in range(n-1):costs[i,i+1]=0
    for span in range(2,n):
        for i in range(n-span):
            j=i+span
            if not diagonal(i,j):continue
            for k in range(i+1,j):
                if not diagonal(i,k) or not diagonal(k,j):continue
                cross=np.cross(coordinates[k]-coordinates[i],coordinates[j]-coordinates[i])
                area=np.linalg.norm(cross)
                if area<1e-8 or np.dot(cross,normal)<1e-9:continue
                longest=max(np.linalg.norm(coordinates[x]-coordinates[y]) for x,y in [(i,k),(k,j),(j,i)])
                value=costs[i,k]+costs[k,j]+area+.00001*longest**4/area
                if value<costs[i,j]:costs[i,j]=value;splits[i,j]=k
    if (0,n-1) not in splits:raise RuntimeError('No nonoverlapping patch triangulation preserving existing edges.')
    def triangles(i,j):
        if j<=i+1:return []
        k=splits[i,j]
        return [(i,k,j)]+triangles(i,k)+triangles(k,j)
    created=[bm.faces.new([ordered[i],ordered[j],ordered[k]]) for i,j,k in triangles(0,n-1)]
    print('PATCH_INITIAL_FACES',len(created),[len(f.verts) for f in created],flush=True)
    for face in created:
        face[old_face]=0;face.material_index=next(iter(group)).link_faces[0].material_index
        face.normal_update()
        # Orient against an original neighbor where present; internal faces inherit fill winding.
        adjacent=[loop for loop in face.loops if any(other.face[old_face]>0 for other in loop.edge.link_loops)]
        if adjacent:
            loop=adjacent[0];neighbor=next(other for other in loop.edge.link_loops if other.face[old_face]>0)
            if loop.vert==neighbor.vert:face.normal_flip()
        face.normal_update()
        for loop in face.loops:
            loop[uv].uv=uv_for[loop.vert]
            loop[saved_normal]=face.normal
    boundary_points.append([list(obj.matrix_world@v.co) for v in verts])
patch=[f for f in bm.faces if f[old_face]==0]
# Subdivide patch interior only; original boundary and original triangles stay intact.
internal=[e for e in bm.edges if len(e.link_faces)==2 and all(f[old_face]==0 for f in e.link_faces) and e.calc_length()>.6]
if internal:bmesh.ops.subdivide_edges(bm,edges=internal,cuts=3,use_grid_fill=True,smooth=0.)
bmesh.ops.triangulate(bm,faces=[f for f in bm.faces if f[old_face]==0 and len(f.verts)>3])
right_names=['RightHand']+['Right'+d+j for d in ['Thumb','Index','Middle','Ring','Little'] for j in ['Proximal','Intermediate','Distal']]
hand_id=obj.vertex_groups['RightHand'].index
right_ids={obj.vertex_groups[name].index for name in right_names}
bm.verts.ensure_lookup_table();bm.verts.index_update()
for v in bm.verts:
    if v.index<len(positions):continue
    values=dict(v[deform]);other={g:w for g,w in values.items() if g not in right_ids and w>1e-8}
    if other:raise RuntimeError('A local finger patch acquired a non-hand weight.')
    digits=sorted([(g,w) for g,w in values.items() if g!=hand_id and w>0],key=lambda p:-p[1])
    cutoff=digits[3][1] if len(digits)>3 else 0.
    keep={g:max(0.,w-cutoff) for g,w in digits[:3]}
    keep[hand_id]=max(0.,1.-sum(keep.values()))
    v[deform].clear()
    for g,w in keep.items():
        if w>1e-8:v[deform][g]=w
bm.normal_update()
for face in bm.faces:
    if face[old_face]==0:
        for loop in face.loops:
            if loop[saved_normal].length<.01:loop[saved_normal]=face.normal
remaining_holes=[e for e in bm.edges if e.is_boundary and all(in_hand(v) for v in e.verts)]
if remaining_holes:raise RuntimeError('Hand patch is not closed.')
if any(len(e.link_faces)!=2 for e in bm.edges if all(in_hand(v) for v in e.verts)):
    raise RuntimeError('A hand edge has overlapping or missing incident faces.')
bm.to_mesh(mesh);bm.free();mesh.update()
normals=mesh.attributes.get('RepairPreservedNormal')
mesh.normals_split_custom_set([v.vector.normalized() for v in normals.data]);mesh.attributes.remove(normals)
original_face_ids=mesh.attributes.get('RepairOriginalFace')
old_polygons=[(p,original_face_ids.data[p.index].value) for p in mesh.polygons if original_face_ids.data[p.index].value>0]
if len(old_polygons)!=len(original_faces):raise RuntimeError('Existing face count changed.')
for polygon,index in old_polygons:
    if tuple(polygon.vertices)!=original_faces[index-1]:raise RuntimeError('Existing face topology changed.')
for i,p in enumerate(positions):
    if (mesh.vertices[i].co-Vector(p)).length>1e-6:raise RuntimeError('Existing vertex moved.')
    values={g.group:g.weight for g in mesh.vertices[i].groups}
    if values!=original_weights[i]:raise RuntimeError('Existing vertex weights changed.')
mesh.attributes.remove(original_face_ids)
if len(arm.data.bones)!=54:raise RuntimeError('Skeleton changed.')
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);arm.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(folder/'player_hands_candidate.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},
    add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False,axis_forward='-Z',axis_up='Y',mesh_smooth_type='OFF',use_tspace=True,path_mode='AUTO')
report={'source':str(source),'originalVertices':len(positions),'vertices':len(mesh.vertices),'originalFaces':len(original_faces),
        'faces':len(mesh.polygons),'repairedBoundaryLoops':2,'remainingRightHandBoundaryEdges':0,
        'originalPositionsAndFacesAndWeightsPreserved':True,'skeletonBones':54,'directReviewRequired':True}
(evidence/'right_topology_authoring.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('RIGHT_HAND_TOPOLOGY_AUTHORING',json.dumps(report),flush=True)
sys.argv+=['--candidate-only']
runpy.run_path(str(root/'scripts/player_hands_rig/export_authored_skin_weights.py'),run_name='__main__')
