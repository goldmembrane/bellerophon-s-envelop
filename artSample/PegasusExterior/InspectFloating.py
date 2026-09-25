"""Read-only geometry investigation; candidate groups require direct visual review."""
import bpy, json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
root=Path(r'D:/Bellerophon2/Bellerophon')
source=root/'scripts/GeneratePegasusExteriorSample.py'
namespace={'__file__':str(source),'__name__':'__inspection__'}
exec(compile(source.read_text(encoding='utf-8').split("# Apply modeling modifiers")[0],str(source),'exec'),namespace)
bpy.context.view_layer.update()
deps=bpy.context.evaluated_depsgraph_get()
items=[]
for o in namespace['mesh_objects']:
    eo=o.evaluated_get(deps);me=eo.to_mesh()
    v=[o.matrix_world@p.co for p in me.vertices]
    faces=[list(p.vertices) for p in me.polygons]
    tree=BVHTree.FromPolygons(v,faces,epsilon=.002)
    items.append(dict(name=o.name,vertices=v,tree=tree,lo=Vector(tuple(min(p[k] for p in v) for k in range(3))),hi=Vector(tuple(max(p[k] for p in v) for k in range(3)))))
    eo.to_mesh_clear()
parent=list(range(len(items)))
def find(a):
    while parent[a]!=a:parent[a]=parent[parent[a]];a=parent[a]
    return a
def inside(point,tree):
    direction=Vector((.317,.529,.787)).normalized()
    nearest=tree.find_nearest(point)
    if nearest[0] is not None and nearest[3]<.005:return True
    origin=point.copy();hits=0
    for _ in range(80):
        hit=tree.ray_cast(origin,direction)
        if hit[0] is None:break
        hits+=1;origin=hit[0]+direction*.0001
    return hits%2==1
edges=0
for i,a in enumerate(items):
    for j in range(i):
        b=items[j]
        if any(a['lo'][k]>b['hi'][k]+.005 or b['lo'][k]>a['hi'][k]+.005 for k in range(3)):continue
        if a['tree'].overlap(b['tree']) or inside(a['vertices'][0],b['tree']) or inside(b['vertices'][0],a['tree']):
            parent[find(i)]=find(j);edges+=1
groups={}
for i,a in enumerate(items):groups.setdefault(find(i),[]).append(a)
result=[]
for group in groups.values():
    if any(g['name'].startswith(('Central pressure envelope','Forward tapered hull','Aft machinery shroud','Bow pressure bulkhead','Aft pressure bulkhead')) for g in group):continue
    result.append([dict(name=g['name'],center=list((g['lo']+g['hi'])/2),lo=list(g['lo']),hi=list(g['hi'])) for g in group])
(root/'docs/validation/PegasusExterior/FloatingCandidates.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print('CANDIDATE_GROUPS',len(result),'PARTS',sum(map(len,result)),'EDGES',edges,flush=True)
for group in result:print(len(group),', '.join(x['name'] for x in group[:8]),flush=True)
