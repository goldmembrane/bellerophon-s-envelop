"""Repair cross-digit leakage continuously, preserving the existing authored skin."""
import bpy
import json
import runpy
import sys
import numpy as np
from pathlib import Path
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT/'Backups/ConsumableItemGrips_2026-09-09/finger_weights_before/player.fbx'
OUTPUT = ROOT/'Assets/_Project/Art/Player/HandsRig'
EVIDENCE = ROOT/'docs/validation/consumable_item_grips_2026-09-09'
DIGITS = ['Thumb','Index','Middle','Ring','Little']
JOINTS = ['Proximal','Intermediate','Distal']
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(SOURCE),use_anim=False,automatic_bone_orientation=False)
arm = next(o for o in bpy.data.objects if o.type=='ARMATURE')
mesh = next(o for o in bpy.data.objects if o.type=='MESH')
if len(arm.data.bones)!=54:
    raise RuntimeError('Repair expects the already promoted 54-bone rig.')
hand=arm.data.bones['RightHand']
mesh_to_hand=hand.matrix_local.inverted()@arm.matrix_world.inverted()@mesh.matrix_world
def metres(p): return Vector((-p.x*.01,p.y*.01,p.z*.01))
points=[metres(mesh_to_hand@v.co) for v in mesh.data.vertices]
chains={}
for digit in DIGITS:
    bones=[arm.data.bones['Right'+digit+j] for j in JOINTS]
    chains[digit]=[metres(hand.matrix_local.inverted()@b.head_local) for b in bones]
    chains[digit].append(metres(hand.matrix_local.inverted()@bones[-1].tail_local))
def smooth(a,b,x):
    t=max(0.,min(1.,(x-a)/(b-a)))
    return t*t*(3.-2.*t)
def distance(p,chain):
    result=float('inf')
    for a,b in zip(chain,chain[1:]):
        delta=b-a;t=max(0.,min(1.,(p-a).dot(delta)/delta.length_squared))
        result=min(result,(p-a-delta*t).length)
    return result
names=['RightHand']+['Right'+d+j for d in DIGITS for j in JOINTS]
group_ids=[mesh.vertex_groups[name].index for name in names]
original=[{g.group:g.weight for g in v.groups} for v in mesh.data.vertices]
positions=[tuple(v.co) for v in mesh.data.vertices]
matrices={b.name:b.matrix_local.copy() for b in arm.data.bones}
budgets=[sum(w for g,w in row.items() if g in group_ids) for row in original]
ids=[i for i,b in enumerate(budgets) if b>=.001];lookup={v:i for i,v in enumerate(ids)}
adj={i:set() for i in ids}
for edge in mesh.data.edges:
    a,b=edge.vertices
    if a in lookup and b in lookup: adj[a].add(b);adj[b].add(a)
coincident={}
for i in ids:
    key=tuple(round(c,7) for c in points[i])
    if key in coincident:
        n=coincident[key];adj[i].add(n);adj[n].add(i)
    else:coincident[key]=i
field=np.array([[original[v].get(g,0.)/budgets[v] for g in group_ids] for v in ids])
support=np.ones_like(field)
for i,v in enumerate(ids):
    for d,digit in enumerate(DIGITS):
        gap=distance(points[v],chains[digit])
        for j in range(3):
            # Far-away hinge leakage is not legitimate glove web deformation.
            if digit=='Thumb': support[i,1+d*3+j]=1.-smooth(.020,.040,gap)
            elif j>0: support[i,1+d*3+j]=1.-smooth(.020,.042,gap)
    removed=np.sum(field[i]*(1.-support[i]))
    field[i]*=support[i];field[i,0]+=removed
target=field.copy()
rows=[];neighbors=[];conductance=[]
for v in ids:
    for n in adj[v]:
        rows.append(lookup[v]);neighbors.append(lookup[n])
        conductance.append(1./max(.001,(points[v]-points[n]).length))
rows=np.asarray(rows);neighbors=np.asarray(neighbors);conductance=np.asarray(conductance)
totals=np.bincount(rows,weights=conductance,minlength=len(ids))
movable=np.array([points[v].y>.07 for v in ids])&(totals>0)
for iteration in range(24):
    average=np.stack([np.bincount(rows,weights=field[neighbors,c]*conductance,minlength=len(ids)) for c in range(16)],axis=1)
    average/=np.maximum(totals[:,None],1e-12)
    field[movable]=.35*target[movable]+.65*average[movable]
    removed=np.sum(field*(1.-support),axis=1)
    field*=support;field[:,0]+=removed
for i,v in enumerate(ids):
    other={g:w for g,w in original[v].items() if g not in group_ids and w>0.}
    slots=4-len(other)
    ordered=np.sort(field[i,1:])[::-1]
    # Reserve Hand as the continuous residual. Renormalizing a nearly flat
    # top-K field amplifies tiny rank differences into large deformation jumps.
    digit_slots=max(0,slots-1)
    cutoff=ordered[digit_slots] if digit_slots<len(ordered) else 0.
    weights=np.zeros(16)
    weights[1:]=np.maximum(0.,field[i,1:]-cutoff)
    weights[0]=1.-np.sum(weights[1:])
    weights*=budgets[v]
    for name in names:mesh.vertex_groups[name].remove([v])
    for name,w in zip(names,weights):
        if w>1e-8:mesh.vertex_groups[name].add([v],float(w),'REPLACE')
max_other=0.;max_budget=0.
for vertex in mesh.data.vertices:
    before=original[vertex.index];after={g.group:g.weight for g in vertex.groups}
    for g in set(before)|set(after):
        if g not in group_ids:max_other=max(max_other,abs(before.get(g,0.)-after.get(g,0.)))
    max_budget=max(max_budget,abs(sum(w for g,w in before.items() if g in group_ids)-sum(w for g,w in after.items() if g in group_ids)))
if max_other>1e-8 or max_budget>1e-6 or positions!=[tuple(v.co) for v in mesh.data.vertices]:
    raise RuntimeError('Protected geometry or non-right-hand weights changed.')
for bone in arm.data.bones:
    if matrices[bone.name]!=bone.matrix_local:raise RuntimeError('Existing bone changed.')
bpy.ops.object.select_all(action='DESELECT');arm.select_set(True);mesh.select_set(True)
bpy.ops.export_scene.fbx(filepath=str(OUTPUT/'player_hands_candidate.fbx'),use_selection=True,
    object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,use_mesh_modifiers=False,
    axis_forward='-Z',axis_up='Y',mesh_smooth_type='OFF',use_tspace=True,path_mode='AUTO')
report={'source':str(SOURCE),'method':'existing skin, anatomical leakage removal, regularized smoothing, continuous sparse palette',
        'rightHandVertices':len(ids),'vertices':len(mesh.data.vertices),'bones':len(arm.data.bones),
        'maxNonRightWeightDifference':max_other,'maxRightBudgetDifference':max_budget,
        'geometryAndBoneMatricesUnchanged':True,'directNaturalReviewRequired':True}
(EVIDENCE/'right_weight_authoring.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('RIGHT_WEIGHT_CANDIDATE',json.dumps(report))
sys.argv+=['--candidate-only']
runpy.run_path(str(ROOT/'scripts/player_hands_rig/export_authored_skin_weights.py'),run_name='__main__')
