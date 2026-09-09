"""Read-only geometric localization of a naturally observed hand surface."""
import json
import numpy as np
from pathlib import Path

root=Path(__file__).resolve().parents[2]
folder=root/'docs/validation/consumable_item_grips_2026-09-09/weight_pose_readout'
def matrix(m):return np.array([[m['e'+str(r)+str(c)] for c in range(4)] for r in range(4)])
for path in sorted(folder.glob('*.json')):
    data=json.loads(path.read_text(encoding='utf-8'))
    points=np.array([[p['x'],p['y'],p['z'],1.] for p in data['vertices']])
    names=data['names'];weights=np.array([[w['m_Weight'+str(k)] for k in range(4)] for w in data['weights']])
    indices=np.array([[w['m_BoneIndex'+str(k)] for k in range(4)] for w in data['weights']])
    transforms=np.array([matrix(m) for m in data['matrices']])
    pose=np.sum(np.einsum('nkij,nj->nki',transforms[indices],points)*weights[:,:,None],axis=1)[:,:3]
    rest=(points@matrix(data['handBind']).T)[:,:3]
    hand=np.array([n=='RightHand' or any(n.startswith('Right'+d) for d in ['Thumb','Index','Middle','Ring','Little']) for n in names])
    selected=np.sum(hand[indices]*weights,axis=1)>.5
    triangles=np.array(data['triangles']).reshape(-1,3)
    hand_triangles=triangles[np.all(selected[triangles],axis=1)]
    rn=np.cross(points[hand_triangles[:,1],:3]-points[hand_triangles[:,0],:3],points[hand_triangles[:,2],:3]-points[hand_triangles[:,0],:3])
    pn=np.cross(pose[hand_triangles[:,1]]-pose[hand_triangles[:,0]],pose[hand_triangles[:,2]]-pose[hand_triangles[:,0]])
    blended=np.sum(transforms[indices]*weights[:,:,None,None],axis=1)
    face_matrix=np.mean(blended[hand_triangles,:3,:3],axis=1)
    expected=np.einsum('nij,nj->ni',face_matrix,rn)
    align=np.sum(pn*expected,axis=1)/np.maximum(1e-20,np.linalg.norm(pn,axis=1)*np.linalg.norm(expected,axis=1))
    print(path.stem,'foldedFaces',int(np.sum(align<-.1)),'nearCollapsedFaces',int(np.sum(np.linalg.norm(pn,axis=1)<np.linalg.norm(rn,axis=1)*.1)),
          'worstFace',hand_triangles[np.argmin(align)].tolist(),'worstAlign',float(np.min(align)))
    edges=np.unique(np.sort(np.concatenate([triangles[:,[0,1]],triangles[:,[1,2]],triangles[:,[2,0]]]),axis=1),axis=0)
    edges=edges[np.all(selected[edges],axis=1)]
    lengths=np.linalg.norm(rest[edges[:,0]]-rest[edges[:,1]],axis=1)
    valid=lengths>.0005;edges=edges[valid];lengths=lengths[valid]
    ratios=np.linalg.norm(pose[edges[:,0]]-pose[edges[:,1]],axis=1)/lengths
    result=[]
    for i in np.argsort(ratios)[-8:][::-1]:
        row={'edge':edges[i].tolist(),'ratio':float(ratios[i]),'restLength':float(lengths[i]),'vertices':[]}
        for v in edges[i]:
            row['vertices'].append({'index':int(v),'rest':rest[v].tolist(),'posed':pose[v].tolist(),
                'weights':{names[b]:float(w) for b,w in zip(indices[v],weights[v]) if w>0}})
        result.append(row)
    (folder/(path.stem+'.edges.txt')).write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(path.stem,json.dumps(result[:2]))
