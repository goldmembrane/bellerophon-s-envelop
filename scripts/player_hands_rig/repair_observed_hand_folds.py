"""Constrained weight authoring from read-only natural-pose measurements, not pose edits."""
import bpy
import json
import numpy as np
import runpy
import sys
from pathlib import Path
from mathutils.kdtree import KDTree
from mathutils import Vector
from mathutils.bvhtree import BVHTree

root=Path(__file__).resolve().parents[2]
folder=root/'Assets/_Project/Art/Player/HandsRig'
evidence=root/'docs/validation/consumable_item_grips_2026-09-09'
paths=[evidence/'weight_pose_readout'/name for name in ['Battery_Aux_Plug.json','Converter_Shield_Plug.json','Buffer_Shield_Plug.json','Nanomachine_Inject.json']]
data=[json.loads(p.read_text(encoding='utf-8')) for p in paths]
def matrix(m):return np.array([[m['e'+str(r)+str(c)] for c in range(4)] for r in range(4)])
def vec(p):return [p['x'],p['y'],p['z']]
names=data[0]['names'];hand_id=names.index('RightHand')
digit_ids={i for i,n in enumerate(names) if any(n.startswith('Right'+d) for d in ['Thumb','Index','Middle','Ring','Little'])}
all_points=np.array([vec(p)+[1.] for p in data[0]['vertices']])
all_weights=np.array([[w['m_Weight'+str(k)] for k in range(4)] for w in data[0]['weights']])
all_indices=np.array([[w['m_BoneIndex'+str(k)] for k in range(4)] for w in data[0]['weights']])
all_budget=np.array([sum(w for b,w in zip(bs,ws) if b==hand_id or b in digit_ids) for bs,ws in zip(all_indices,all_weights)])
# Share variables across exact duplicated positions, so UV seams cannot separate.
keys={};representatives=[];mapping=np.full(len(all_points),-1,dtype=int)
for v,p in enumerate(all_points):
    if all_budget[v]<.001:continue
    key=tuple(np.round(p[:3],6))
    if key not in keys:keys[key]=len(representatives);representatives.append(v)
    mapping[v]=keys[key]
ids=np.array(representatives);points=all_points[ids];budget=all_budget[ids]
n=len(ids);palette=np.full((n,3),hand_id,dtype=int);z=np.zeros((n,3));other=np.zeros((4,n,3))
poses=np.array([[matrix(m) for m in d['matrices']] for d in data])
for i,v in enumerate(ids):
    digits=[(b,w) for b,w in zip(all_indices[v],all_weights[v]) if b in digit_ids and w>0.]
    if len(digits)>3:raise RuntimeError('Candidate must reserve a continuous Hand residual.')
    for j,(b,w) in enumerate(digits):palette[i,j]=b;z[i,j]=w
    for b,w in zip(all_indices[v],all_weights[v]):
        if b!=hand_id and b not in digit_ids:
            other[:,i]+=np.einsum('pij,j->pi',poses[:,b,:3,:],points[i])*w
base=np.einsum('pij,nj->pni',poses[:,hand_id,:3,:],points)*budget[None,:,None]+other
possible=np.einsum('pnkij,nj->pnki',poses[:,palette,:3,:],points)
hand_points=np.einsum('pij,nj->pni',poses[:,hand_id,:3,:],points)
derivative=possible-hand_points[:,:,None,:]
rest=(points@matrix(data[0]['handBind']).T)[:,:3]
triangles=np.array(data[0]['triangles']).reshape(-1,3)
triangles=triangles[np.all(mapping[triangles]>=0,axis=1)]
triangles=mapping[triangles]
triangles=triangles[(triangles[:,0]!=triangles[:,1])&(triangles[:,1]!=triangles[:,2])&(triangles[:,2]!=triangles[:,0])]
a,b,c=triangles.T
rn=np.cross(points[b,:3]-points[a,:3],points[c,:3]-points[a,:3])
areas=np.linalg.norm(rn,axis=1)
valid=areas>1e-10;triangles=triangles[valid];a,b,c=triangles.T;rn=rn[valid];areas=areas[valid]
initial=z.copy()
initial_rot=poses[:,hand_id,None,:3,:3]*budget[None,:,None,None]
initial_rot=np.broadcast_to(initial_rot,(4,n,3,3)).copy()
for k in range(3):initial_rot+=(poses[:,palette[:,k],:3,:3]-poses[:,hand_id,None,:3,:3])*z[None,:,k,None,None]
expected=np.einsum('pfij,fj->pfi',(initial_rot[:,a]+initial_rot[:,b]+initial_rot[:,c])/3.,rn)
expected/=np.maximum(1e-12,np.linalg.norm(expected,axis=2))[:,:,None]
edges=np.unique(np.sort(np.concatenate([triangles[:,[0,1]],triangles[:,[1,2]],triangles[:,[2,0]]]),axis=1),axis=0)
ea,eb=edges.T;lengths=np.linalg.norm(rest[eb]-rest[ea],axis=1)
valid=lengths>.0005;ea=ea[valid];eb=eb[valid];lengths=lengths[valid]
# Differential-coordinate fairness removes local skin spikes while preserving rest-shape detail.
lap_rows=np.concatenate([edges[:,0],edges[:,1]]);lap_neighbors=np.concatenate([edges[:,1],edges[:,0]])
degree=np.maximum(1,np.bincount(lap_rows,minlength=n))
def laplace(values):
    result=values.copy()
    for axis in range(3):result[:,axis]-=np.bincount(lap_rows,weights=values[lap_neighbors,axis],minlength=n)/degree
    return result
rest_laplace=laplace(points[:,:3])
target_curvature=np.einsum('pnij,nj->pni',initial_rot,rest_laplace)
surface_aware='--surface-aware' in sys.argv
surface_trees=[];surface_bounds=[]
if surface_aware:
    for record in data:
        vertices=np.array([vec(v) for v in record['itemVertices']]);faces=np.array(record['itemTriangles']).reshape(-1,3)
        surface_trees.append(BVHTree.FromPolygons([Vector(v) for v in vertices],faces.tolist(),all_triangles=True))
        surface_bounds.append((vertices.min(axis=0),vertices.max(axis=0)))
plane_points=np.zeros((4,n+len(triangles),3));plane_normals=np.zeros_like(plane_points)
ray=Vector((1.,.313,.177)).normalized()
def refresh_planes(p,samples):
    lower,upper=surface_bounds[p];tree=surface_trees[p]
    for i in np.flatnonzero(np.all((samples>lower-.008)&(samples<upper+.008),axis=1)):
        value=Vector(samples[i]);closest,normal,_,distance=tree.find_nearest(value)
        inside=False
        if np.all(samples[i]>lower) and np.all(samples[i]<upper):
            origin=value.copy();count=0
            for unused in range(64):
                hit,_,_,gap=tree.ray_cast(origin,ray)
                if hit is None:break
                count+=1;origin=hit+ray*.000002
            inside=count%2==1
        direction=(value-closest)/distance if distance>1e-9 else normal.normalized()
        if inside:direction=-direction
        plane_points[p,i]=closest;plane_normals[p,i]=direction
# Distal contact pads stay fixed; the repair acts on the surrounding skin transition.
pinned=(np.max(z,axis=1)>.97)&(rest[:,1]>.20)
pinned|=(np.max(z,axis=1)>.97)&(rest[:,1]>.165)&(rest[:,0]<-.032)
finger_surface='--finger-surface' in sys.argv
if finger_surface:
    # Broader finger reconstruction still excludes the wrist and all other body
    # surfaces. Freeze them both during optimization and during FBX transfer.
    # Rebuilt fingertips no longer share the old fixed-pad geometry. Their skin
    # weights must be authored too; the subsequent grip fit restores contact.
    pinned=(rest[:,1]<.065)|(rest[:,1]>=.28)|(np.abs(rest[:,0])>=.11)|(np.abs(rest[:,2])>=.11)
moment=np.zeros_like(z);variance=np.zeros_like(z)
history=[]
best_key=(float('inf'),float('inf'),float('inf'));best_weights=None;best_iteration=-1
iteration_limit=int(sys.argv[sys.argv.index('--iterations')+1]) if '--iterations' in sys.argv else (1800 if surface_aware else 2400)
def describe_faces(signed_values, weights):
    result=[]
    for pose_index,record in enumerate(data):
        faces=[]
        for face_index in np.argsort(signed_values[pose_index])[:8]:
            vertices=triangles[face_index]
            faces.append({'faceIndex':int(face_index),'area':float(areas[face_index]),
                'ratio':float(signed_values[pose_index,face_index]),'vertices':[
                    {'sourceIndex':int(ids[v]),'variableIndex':int(v),'rest':rest[v].tolist(),
                     'pinned':bool(pinned[v]),'palette':[names[b] for b in palette[v]],
                     'weights':weights[v].tolist(),'initial':initial[v].tolist()} for v in vertices]})
        result.append({'pose':record.get('name',paths[pose_index].stem),'faces':faces})
    return result
for iteration in range(iteration_limit):
    current=base+np.einsum('pnki,nk->pni',derivative,z)
    ab=current[:,b]-current[:,a];ac=current[:,c]-current[:,a]
    cross=np.cross(ab,ac)
    current_rot=poses[:,hand_id,None,:3,:3]*budget[None,:,None,None]
    current_rot=np.broadcast_to(current_rot,(4,n,3,3)).copy()
    for k in range(3):current_rot+=(poses[:,palette[:,k],:3,:3]-poses[:,hand_id,None,:3,:3])*z[None,:,k,None,None]
    expected=np.einsum('pfij,fj->pfi',(current_rot[:,a]+current_rot[:,b]+current_rot[:,c])/3.,rn)
    expected_length=np.maximum(1e-12,np.linalg.norm(expected,axis=2))
    expected/=expected_length[:,:,None]
    signed=np.sum(cross*expected,axis=2)/areas[None,:]
    if '--inspect-only' in sys.argv:
        for p in range(4):
            for face in np.flatnonzero((signed[p]<0)&(np.all(pinned[triangles],axis=1)|np.all(palette[triangles]==hand_id,axis=(1,2))|(np.abs(signed[p]+.824041874)<.00001))):
                vs=triangles[face]
                print(json.dumps({'pose':p,'face':int(face),'ratio':float(signed[p,face]),'vertices':ids[vs].tolist(),'rest':rest[vs].tolist(),
                    'pinned':pinned[vs].tolist(),'palette':[[names[i] for i in row] for row in palette[vs]],'weights':z[vs].tolist()}),flush=True)
        raise SystemExit(0)
    deficit=np.maximum(0.,.18-signed)
    # Positive projected area is a smooth barrier against face inversion.
    # Reconstructed joint/web triangles require a stronger authoring barrier than
    # the old hole patch; edge fairness must not win over their positive area.
    multiplier=-2.*deficit*(20. if finger_surface else 1.)
    gb=np.cross(ac,expected)*multiplier[:,:,None]
    gc=np.cross(expected,ab)*multiplier[:,:,None]
    gradient_position=np.zeros_like(current)
    for p in range(4):
        for axis in range(3):
            gradient_position[p,:,axis]+=np.bincount(b,weights=gb[p,:,axis],minlength=n)
            gradient_position[p,:,axis]+=np.bincount(c,weights=gc[p,:,axis],minlength=n)
            gradient_position[p,:,axis]-=np.bincount(a,weights=gb[p,:,axis]+gc[p,:,axis],minlength=n)
    delta=current[:,eb]-current[:,ea];actual=np.linalg.norm(delta,axis=2)
    error=actual-np.clip(actual,lengths[None,:]*.65,lengths[None,:]*1.55)
    surface_error=0.
    if surface_aware:
        samples=np.concatenate([current,(current[:,a]+current[:,b]+current[:,c])/3.],axis=1)
        if iteration%20==0:
            plane_normals.fill(0.)
            for p in range(4):refresh_planes(p,samples[p])
        distance=np.sum((samples-plane_points)*plane_normals,axis=2)
        active=np.linalg.norm(plane_normals,axis=2)>.5
        penetration=np.maximum(0.,.0003-distance)*active
        surface_error=float(np.sum(penetration*penetration))
        surface_gradient=-.8*penetration[:,:,None]*plane_normals
        gradient_position+=surface_gradient[:,:n]
        for p in range(4):
            for axis in range(3):
                for vertices in (a,b,c):
                    gradient_position[p,:,axis]+=np.bincount(vertices,weights=surface_gradient[p,n:,axis]/3.,minlength=n)
    key=(int(np.sum(signed<.1)),surface_error,float(np.max(actual/lengths[None,:])),float(np.sum(error*error)))
    if key<best_key:
        best_key=key;best_weights=z.copy();best_iteration=iteration
    edge_gradient=.10*error[:,:,None]*delta/np.maximum(actual[:,:,None],1e-12)
    for p in range(4):
        for axis in range(3):
            gradient_position[p,:,axis]+=np.bincount(eb,weights=edge_gradient[p,:,axis],minlength=n)
            gradient_position[p,:,axis]-=np.bincount(ea,weights=edge_gradient[p,:,axis],minlength=n)
        if surface_aware:
            curvature=laplace(current[p])-target_curvature[p]
            gradient_position[p]+=.08*curvature
            for axis in range(3):
                gradient_position[p,:,axis]-=.08*np.bincount(lap_neighbors,weights=curvature[lap_rows,axis]/degree[lap_rows],minlength=n)
    gradient=np.einsum('pnki,pni->nk',derivative,gradient_position)+.0000002*(z-initial)
    # The reference normal also depends on the authored weights. Its exact derivative
    # prevents the solver descending an incomplete objective around a bent thumb.
    normal_gradient=multiplier[:,:,None]*(cross-expected*np.sum(cross*expected,axis=2)[:,:,None])/expected_length[:,:,None]
    for vertices in (a,b,c):
        for k in range(3):
            normal_derivative=np.einsum('pfij,fj->pfi',poses[:,palette[vertices,k],:3,:3]-poses[:,hand_id,None,:3,:3],rn)/3.
            contribution=np.sum(normal_derivative*normal_gradient,axis=(0,2))
            gradient[:,k]+=np.bincount(vertices,weights=contribution,minlength=n)
    gradient[pinned]=0.
    moment=.9*moment+.1*gradient;variance=.999*variance+.001*gradient*gradient
    rate=(.002 if iteration<1200 else .0005) if surface_aware or finger_surface else (.012 if iteration<1200 else .004 if iteration<2000 else .001)
    metric=np.sqrt(variance/(1.-.999**(iteration+1)))+1e-10
    proposal=z-rate*(moment/(1.-.9**(iteration+1)))/metric
    proposal[palette==hand_id]=0.
    # Project in Adam's diagonal metric. Radial normalization is not a projection:
    # at a saturated distal pad it steals dominant weight even when its gradient
    # requires increasing it, producing a new fold beside the fixed contact pad.
    lower=np.zeros(n);upper=np.maximum(0.,np.max(proposal*metric,axis=1))
    active=np.sum(np.maximum(proposal,0.),axis=1)>budget
    for unused in range(35):
        threshold=(lower+upper)*.5
        total=np.sum(np.maximum(0.,proposal-threshold[:,None]/metric),axis=1)
        lower=np.where(total>budget,threshold,lower)
        upper=np.where(total>budget,upper,threshold)
    z=np.maximum(0.,proposal-np.where(active,upper,0.)[:,None]/metric)
    z[pinned]=initial[pinned]
    if iteration%100==0 or iteration==2399:
        row={'iteration':iteration,'flippedFaces':np.sum(signed<0,axis=1).tolist(),
             'worstProjectedAreaRatio':np.min(signed,axis=1).tolist(),
             'maxEdgeRatio':np.max(actual/lengths[None,:],axis=1).tolist(),'surfaceSquaredError':surface_error}
        print('WEIGHT_FOLD_AUTHORING',json.dumps(row),flush=True);history.append(row)

if best_key[0]!=0:
    (evidence/'fold_weight_failed_authoring.json').write_text(json.dumps({
        'history':history,'bestIteration':best_iteration,'bestMetrics':best_key,
        'lastFaces':describe_faces(signed,z),'candidateExported':False},indent=2),encoding='utf-8')
    raise RuntimeError('No fold-free candidate was found; do not export an invalid skin.')
z=best_weights
print('SELECTED_FOLD_FREE_CANDIDATE',best_iteration,best_key,flush=True)

if finger_surface:
    # The Unity importer already uses this lossless weight source. Keep the FBX
    # geometry/skeleton byte-identical: a second FBX round trip adds bind-pose
    # rounding even though the intended operation changes weights only.
    weight_path=folder/'player_hands_candidate_weights.json'
    document=json.loads(weight_path.read_text(encoding='utf-8'))
    tree=KDTree(n)
    for i,p in enumerate(points):tree.insert(p[:3],i)
    tree.balance();changed=0
    right_names={'RightHand'}|{names[i] for i in digit_ids}
    for row in document['vertices']:
        total=sum(value['weight'] for value in row['influences'] if value['bone'] in right_names)
        if total<.001:continue
        point=Vector(vec(row['position']));_,index,gap=tree.find(point)
        if gap>.000005:raise RuntimeError('Observed/authored JSON correspondence missing.')
        if rest[index,1]<.065 or rest[index,1]>=.28 or abs(rest[index,0])>=.11 or abs(rest[index,2])>=.11:continue
        values={'RightHand':max(0.,budget[index]-np.sum(z[index]))}
        for bone,w in zip(palette[index],z[index]):
            if w>1e-8:values[names[bone]]=float(w)
        scale=total/sum(values.values())
        row['influences']=[v for v in row['influences'] if v['bone'] not in right_names]+[
            {'bone':name,'weight':w*scale} for name,w in values.items() if w>1e-8]
        if len(row['influences'])>4:raise RuntimeError('Weight authoring exceeds the existing four-influence layout.')
        changed+=1
    weight_path.write_text(json.dumps(document,separators=(',',':')),encoding='utf-8')
    (evidence/'fold_weight_authoring.json').write_text(json.dumps({'history':history,'selectedIteration':best_iteration,
        'selectedMetrics':best_key,'vertices':changed,'fbxGeometrySkeletonPreserved':True,'directReviewRequired':True},indent=2),encoding='utf-8')
    print('AUTHORED_WEIGHT_JSON_ONLY',changed,flush=True)
    raise SystemExit(0)

# Transfer only the optimized right-hand budget back to the same source geometry.
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(folder/'player_hands_candidate.fbx'),use_anim=False,automatic_bone_orientation=False)
mesh=next(o for o in bpy.data.objects if o.type=='MESH');arm=next(o for o in bpy.data.objects if o.type=='ARMATURE')
tree=KDTree(n)
for i,p in enumerate(points):tree.insert(p[:3],i)
tree.balance()
right_names=['RightHand']+[names[i] for i in sorted(digit_ids)]
right_groups={mesh.vertex_groups[name].index for name in right_names}
changed=0
for v in mesh.data.vertices:
    before={g.group:g.weight for g in v.groups}
    total=sum(w for g,w in before.items() if g in right_groups)
    if total<.001:continue
    world=mesh.matrix_world@v.co;point=(-world.x,world.z,-world.y)
    _,index,gap=tree.find(point)
    if gap>.000005:raise RuntimeError('Observed/FBX vertex correspondence missing: '+str(v.index))
    if finger_surface and (rest[index,1]<.065 or rest[index,1]>=.28 or abs(rest[index,0])>=.11 or abs(rest[index,2])>=.11):continue
    values={'RightHand':max(0.,budget[index]-np.sum(z[index]))}
    for bone,w in zip(palette[index],z[index]):
        if w>1e-8:values[names[bone]]=float(w)
    scale=total/sum(values.values())
    for name in right_names:mesh.vertex_groups[name].remove([v.index])
    for name,w in values.items():
        if w>1e-8:mesh.vertex_groups[name].add([v.index],w*scale,'REPLACE')
    changed+=1
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(folder/'player_hands_candidate.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False,
    axis_forward='-Z',axis_up='Y',mesh_smooth_type='OFF',use_tspace=True,path_mode='AUTO')
(evidence/'fold_weight_authoring.json').write_text(json.dumps({'history':history,'selectedIteration':best_iteration,'selectedMetrics':best_key,'vertices':changed,'directReviewRequired':True},indent=2),encoding='utf-8')
sys.argv+=['--candidate-only']
runpy.run_path(str(root/'scripts/player_hands_rig/export_authored_skin_weights.py'),run_name='__main__')
