"""Generate only the new exterior; the measured Pegasus interior is read-only."""
import bpy, math, json, random, bmesh
from pathlib import Path
from mathutils import Vector, Matrix
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'artSample/PegasusExterior'
OUT=ROOT/'Assets/_Project/ArtSamples/PegasusExterior'
REPORT=ROOT/'docs/validation/PegasusExterior'
for p in [ART/'blender',ART/'renders',OUT/'Models',OUT/'Textures',REPORT]: p.mkdir(parents=True,exist_ok=True)
layout=json.loads((REPORT/'SourceLayout.json').read_text(encoding='utf-8-sig'))
mins=np.array([[r['min'][k] for k in ['x','y','z']] for r in layout['roots']])
maxs=np.array([[r['max'][k] for k in ['x','y','z']] for r in layout['roots']])
lo=mins.min(axis=0); hi=maxs.max(axis=0); center=(lo+hi)/2
# Model coordinates: X lateral, Y forward, Z up; export uses Unity Y-up.
W=(hi[0]-lo[0])/2+5; L=(hi[2]-lo[2])/2+5; H=max(11,(hi[1]-lo[1])/2+5)
(REPORT/'ExteriorDimensions.json').write_text(json.dumps({'center':dict(zip(['x','y','z'],map(float,center))),'halfWidth':W,'halfLength':L,'halfHeight':H},indent=2),encoding='utf-8')
bpy.ops.object.select_all(action='SELECT'); bpy.ops.object.delete(use_global=False)
bpy.context.scene.unit_settings.system='METRIC'
random.seed(709)

# Reusable painted metal maps: mottling, worn edges, chipped paint, streaks and fine bump.
N=1024
rng=np.random.default_rng(709)
yy,xx=np.mgrid[0:N,0:N]/N
noise=rng.random((N,N))
macro=(np.sin(xx*49+np.sin(yy*9))*np.cos(yy*33)+np.sin(xx*93+yy*17))*.045
wear=np.zeros((N,N),dtype=np.float32)
for _ in range(1100):
    x=int(rng.integers(N)); y=int(rng.integers(N)); ln=int(rng.integers(2,35)); thick=int(rng.integers(1,3))
    wear[y:min(N,y+thick),x:min(N,x+ln)]=rng.uniform(.25,1)
edge=np.maximum(np.maximum(np.exp(-xx*100),np.exp(-(1-xx)*100)),np.maximum(np.exp(-yy*100),np.exp(-(1-yy)*100)))
streak=(np.sin(xx*170)*.5+.5)**20*(.5+.5*np.cos(yy*5))*.15
tone=np.clip(.76+macro+(noise-.5)*.13-streak+wear*.3+edge*.12,0,1)
def image(name,values):
    existing=OUT/'Textures'/f'{name}.png'
    if existing.exists():return bpy.data.images.load(str(existing),check_existing=True)
    rgba=np.ones((N,N,4),dtype=np.float32)
    if values.ndim==2: rgba[:,:,:3]=values[:,:,None]
    else: rgba[:,:,:3]=values
    im=bpy.data.images.new(name,width=N,height=N,alpha=True)
    im.pixels.foreach_set(rgba.ravel()); im.filepath_raw=str(OUT/'Textures'/f'{name}.png'); im.file_format='PNG'; im.save(); return im
albedo=image('HullWear_Albedo',np.stack([tone*.94,tone*.98,tone],axis=2))
metal=image('HullWear_Metallic',np.clip(.7+wear*.25-edge*.1,0,1))
rough=image('HullWear_Roughness',np.clip(.55+streak+noise*.12-wear*.3,0,1))
height=(noise-.5)*.025+wear*.055
dy,dx=np.gradient(height)
normal=image('HullWear_Normal',np.stack([.5-dx*2,.5-dy*2,np.ones_like(dx)],axis=2))
mats={}
def mat(name,col,metallic=.65,roughness=.48,emission=0):
    m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True
    bs=m.node_tree.nodes.get('Principled BSDF');bs.inputs['Base Color'].default_value=(*col,1);bs.inputs['Metallic'].default_value=metallic;bs.inputs['Roughness'].default_value=roughness
    if emission:
        bs.inputs['Emission Color'].default_value=(*col,1);bs.inputs['Emission Strength'].default_value=emission
    elif name not in ['Glass','Solar','Black']:
        tex=m.node_tree.nodes.new('ShaderNodeTexImage');tex.image=albedo
        mix=m.node_tree.nodes.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(*col,1)
        m.node_tree.links.new(tex.outputs['Color'],mix.inputs[1]);m.node_tree.links.new(mix.outputs[0],bs.inputs['Base Color'])
        rt=m.node_tree.nodes.new('ShaderNodeTexImage');rt.image=rough;m.node_tree.links.new(rt.outputs['Color'],bs.inputs['Roughness'])
        nt=m.node_tree.nodes.new('ShaderNodeTexImage');nt.image=normal;normal.colorspace_settings.name='Non-Color'; nm=m.node_tree.nodes.new('ShaderNodeNormalMap');nm.inputs['Strength'].default_value=.28;m.node_tree.links.new(nt.outputs['Color'],nm.inputs['Color']);m.node_tree.links.new(nm.outputs[0],bs.inputs['Normal'])
    mats[name]=m;return m
mat('Hull',(.19,.225,.245));mat('Panel',(.34,.37,.38));mat('Cargo',(.245,.29,.315));mat('Tank',(.3,.335,.35));mat('Bronze',(.48,.29,.12));mat('Black',(.025,.036,.043),.5,.55);mat('Glass',(.026,.1,.145),.8,.17);mat('Solar',(.035,.07,.12),.7,.28);mat('Red',(.8,.025,.012),.1,.35,5);mat('Cyan',(.03,.5,.9),.1,.3,4);mat('White',(.9,.77,.5),.15,.4,2);mat('Marking',(.65,.7,.69),.05,.75)
mesh_objects=[]
templates={}
def instance(key,make,p,scale=(1,1,1)):
    if key not in templates:
        me=bpy.data.meshes.new(key);bm=bmesh.new();make(bm);bm.to_mesh(me);bm.free();templates[key]=me
    me=templates[key].copy();me.transform(Matrix.Diagonal(Vector((*scale,1))));o=bpy.data.objects.new(key,me);bpy.context.collection.objects.link(o);o.location=p;return o
def finish(o,name,material,bevel=0):
    o.name=name;o.data.materials.append(mats[material]);mesh_objects.append(o)
    if bevel:
        mod=o.modifiers.new('Machined edge bevel','BEVEL');mod.width=bevel;mod.segments=2
        mod=o.modifiers.new('Weighted surface normals','WEIGHTED_NORMAL')
    return o
def box(name,p,s,m='Hull',bevel=.12,rot=None):
    o=instance('BoxTemplate',lambda bm:bmesh.ops.create_cube(bm,size=1),p,s)
    if rot:o.rotation_euler=rot
    return finish(o,name,m,bevel)
def cyl(name,p,r,length,m='Tank',axis=(0,1,0),vertices=32):
    o=instance('CylinderTemplate'+str(vertices),lambda bm:bmesh.ops.create_cone(bm,cap_ends=True,cap_tris=False,segments=vertices,radius1=1,radius2=1,depth=1),p,(r,r,length));o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    for f in o.data.polygons:f.use_smooth=True
    return finish(o,name,m,.07)
def ball(name,p,s,m='Tank'):
    o=instance('SphereTemplate',lambda bm:bmesh.ops.create_uvsphere(bm,u_segments=24,v_segments=12,radius=1),p,s)
    for f in o.data.polygons:f.use_smooth=True
    return finish(o,name,m)
def beam(name,a,b,r=.18,m='Panel'):
    a=Vector(a);b=Vector(b);return cyl(name,(a+b)/2,r,(b-a).length,m,b-a,12)
def ring(name,p,r,minor=.17,m='Panel',axis=(0,1,0)):
    verts=[];faces=[]
    for i in range(40):
        a=math.tau*i/40
        for j in range(8):
            b=math.tau*j/8;verts.append(((r+minor*math.cos(b))*math.cos(a),(r+minor*math.cos(b))*math.sin(a),minor*math.sin(b)))
    for i in range(40):
        for j in range(8):faces.append((i*8+j,((i+1)%40)*8+j,((i+1)%40)*8+(j+1)%8,i*8+(j+1)%8))
    me=bpy.data.meshes.new(name);me.from_pydata(verts,[],faces);me.update();o=bpy.data.objects.new(name,me);bpy.context.collection.objects.link(o);o.location=p;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler()
    return finish(o,name,m)
def shell_section(name,y0,y1,w0,w1,h0,h1):
    # Eight longitudinal plates; no closed solid block through the preserved interior.
    c=2.2
    def cross(w,h,y):return [(-w+c,y,-h),(w-c,y,-h),(w,y,-h+c),(w,y,h-c),(w-c,y,h),(-w+c,y,h),(-w,y,h-c),(-w,y,-h+c)]
    a=cross(w0,h0,y0);b=cross(w1,h1,y1)
    for i in range(8):
        j=(i+1)%8;me=bpy.data.meshes.new(name+str(i));me.from_pydata([a[i],a[j],b[j],b[i]],[],[(0,1,2,3)]);me.update();o=bpy.data.objects.new(name+str(i),me);bpy.context.collection.objects.link(o);finish(o,name+str(i),'Hull')
        so=o.modifiers.new('Outward skin thickness','SOLIDIFY');so.thickness=.42;so.offset=0
        bpy.context.view_layer.objects.active=o;o.select_set(True)
        bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.mesh.normals_make_consistent(inside=False);bpy.ops.object.mode_set(mode='OBJECT');o.select_set(False)

shell_section('Central pressure envelope ', -L,L,W,W,H,H)
shell_section('Forward tapered hull ',L,L+29,W,W*.43,H,H*.85)
shell_section('Aft machinery shroud ',-L-23,-L,W*.72,W,H*.92,H)
box('Bow pressure bulkhead',(0,L+29,0),(W*.86,.6,H*1.7),'Hull',.35)
box('Aft pressure bulkhead',(0,-L-23,0),(W*1.44,.6,H*1.84),'Hull',.35)

# Container bays and layered external framing, deliberately outside the pressure shell.
bays=5; pitch=(2*L-4)/bays
for side in [-1,1]:
    x=side*(W+2.6)
    for level in [-1,1]:
        z=level*6.3
        for i in range(bays):
            y=-L+2+pitch*(i+.5)
            box(f'Cargo module {side} {level} {i}',(x,y,z),(5,pitch-.65,8.2),'Cargo',.35)
            for offset in [-.41,.41]:box('Container recessed end panel',(x+side*2.52,y+pitch*offset,z),(.14,.65,7.1),'Panel',.04)
            for offset in [-.27,0,.27]:box('Container pressed rib',(x+side*2.6,y+pitch*offset,z),(.24,.16,7.4),'Panel',.04)
            for dz in [-3.65,3.65]:box('Container rail',(x+side*2.64,y,z+dz),(.24,pitch-.9,.19),'Panel',.03)
            for dy in [-pitch*.4,pitch*.4]:
                for dz in [-3.4,3.4]:box('Corner locking casting',(x+side*2.76,y+dy,z+dz),(.26,.56,.56),'Bronze',.05)
    # Continuous central truss separates the stacked cargo rows.
    tx=side*(W+5.7)
    for z in [-1.3,1.3]:beam('Longitudinal side girder',(tx,-L,z),(tx,L,z),.32)
    for i in range(12):
        y=-L+2*L*i/12; yn=-L+2*L*(i+1)/12
        beam('Triangulated side frame',(tx,y,-1.3),(tx,yn,1.3),.23)
        beam('Truss post',(tx,y,-1.3),(tx,y,1.3),.25)
        if i%3==0:
            box('Truss knuckle',(tx,y,0),(.9,1.1,3.4),'Panel',.14)
            ball('Navigation red',(tx+side*.51,y,.7),(.16,.2,.2),'Red')
    for z in [-H+.6,H-.6]:
        for i in range(3):beam('Exposed utility pipeline',(side*(W+.6+i*.27),-L,z),(side*(W+.6+i*.27),L,z),.12,'Bronze' if i==1 else 'Panel')

# Dorsal and ventral plating and cargo racks. Central spine leaves recognisable stepped silhouette.
for sign in [-1,1]:
    z=sign*(H+.45)
    for ix in range(-3,4):
        for iy in range(8):
            y=-L+(iy+.5)*(2*L/8)
            box('Hull maintenance tile',(ix*(W*2/7),y,z),(W*2/7-.5,2*L/8-.55,.28),'Panel' if (ix+iy)%4==0 else 'Hull',.07)
    for x in [-W*.6,W*.6]:
        for i in range(3):
            y=-L*.45+i*L*.47
            box('Dorsal freight cassette' if sign==1 else 'Ventral equipment cassette',(x,y,sign*(H+3.3)),(W*.49,L*.4,5.7),'Cargo',.28)
            for j in [-1,0,1]:box('Freight reinforcing hoop',(x+j*W*.16,y,sign*(H+6.18)),(.22,L*.4+.1,.25),'Panel',.04)
            for end in [-1,1]:box('Freight rail',(x,y+end*L*.195,sign*(H+3.3)),(W*.49+.2,.28,5.9),'Panel',.07)
    box('Axial service spine',(0,0,sign*(H+1.2)),(4.4,2*L,1.7),'Black',.2)
    for i in range(16):box('Spine cover',(0,-L+(i+.5)*2*L/16,sign*(H+2.05)),(4.7,2*L/16-.3,.24),'Panel',.06)

# Bow bridge is an external superstructure, not a replacement or relocation of the interior cockpit.
bow=L+15
box('Forward avionics block',(0,bow,5),(W*.87,23,13),'Hull',1.4)
box('Bridge upper armored crown',(0,bow+3,13),(W*.77,16,5),'Panel',.8)
for i in range(-3,4):
    x=i*W*.098
    box('Recessed armored bridge window',(x,bow+11.2,13.1),(W*.088,.26,2.3),'Black',.16)
    box('Smoked bridge glass',(x,bow+11.38,13.1),(W*.076,.08,1.8),'Glass',.1)
    box('Window internal glint',(x-.35,bow+11.44,12.7),(.55,.04,.17),'Cyan',.015)
for side in [-1,1]:
    box('Forward shoulder armor',(side*W*.56,L+7,0),(W*.25,12,14),'Panel',.7)
    for z in [-4,0,4]:beam('Forward shoulder conduit',(side*W*.7,L+10,z),(side*W*.7,L+17,z),.22,'Bronze')
    for y in [L+2,L+11]:ball('Shoulder running lamp',(side*W*.7,y,6),(.23,.25,.23),'Red')
    for i in range(4):box('Bow service vent',(side*(W*.37),bow-2+i*1.2,15.65),(2.8,.6,.1),'Black',.025)
cyl('Forward docking housing',(0,L+30,-2),6.3,3,'Panel')
cyl('Docking recess',(0,L+31.7,-2),4.8,.25,'Black')
ring('Docking armored collar',(0,L+31.9,-2),5.25,.55)
ring('Docking seal',(0,L+32.4,-2),3.9,.22,'Bronze')
cyl('Docking pressure hatch',(0,L+32,-2),3.55,.16,'Hull')
for i in range(12):
    a=i*math.tau/12;cyl('Docking collar bolt',(5.25*math.cos(a),L+32.4,-2+5.25*math.sin(a)),.16,.12,'Black',vertices=12)
for side in [-1,1]:
    for z in [-7,-9]:beam('Forward sensor boom',(side*8,L+25,z),(side*8,L+39,z),.13)
    box('Bow sensor base',(side*8,L+28,-8),(3,4,3),'Hull',.35)

# Rear tank/engine pack, with rounded fuel tanks, retaining bands and nozzle interiors.
aft=-L-28
box('Reactor trunk',(0,aft+6,0),(W*1.35,21,H*1.8),'Hull',1.0)
for x in [-W*.66,-W*.24,W*.24,W*.66]:
    for z in [-7.6,7.6]:
        cyl('Main propellant drum',(x,aft-8,z),5.3,20)
        ball('Tank forward dome',(x,aft+2,z),(5.3,3.2,5.3))
        ball('Tank rear dome',(x,aft-18,z),(5.3,3.2,5.3))
        for y in [aft-15,aft-2]:
            cyl('Tank restraint band',(x,y,z),5.43,.85,'Bronze')
            ring('Band outer rim',(x,y-.43,z),5.4,.12,'Panel')
        cyl('Engine heat shield',(x,aft-23,z),5.7,6,'Hull')
        for y in [aft-21,aft-24.5]:ring('Engine armored rim',(x,y,z),5.8,.38)
        cyl('Nozzle dark throat',(x,aft-26.1,z),4.75,.18,'Black')
        ring('Nozzle lip',(x,aft-26.25,z),4.9,.42,'Panel')
        ring('Nozzle cyan ion ring',(x,aft-26.5,z),3.45,.2,'Cyan')
        cyl('Nozzle inner light',(x,aft-26.4,z),2.6,.05,'Cyan')
        for i in range(8):
            a=math.tau*i/8;beam('Engine cooling fin',(x+5.5*math.cos(a),aft-21,z+5.5*math.sin(a)),(x+5.5*math.cos(a),aft-25,z+5.5*math.sin(a)),.19)
        cyl('Tank inspection port',(x,aft+5.1,z),1.4,.6,'Black')
        ring('Tank inspection rim',(x,aft+5.45,z),1.3,.18)
for side in [-1,1]:
    x=side*W*.93
    box('Engine service equipment bank',(x,aft-5,0),(4.5,26,4.5),'Hull',.4)
    for i in range(7):
        box('Equipment module face',(x+side*2.3,aft-16+i*3.4,0),(.3,2.8,3.4),'Panel',.09)
        for z in [-.7,0,.7]:box('Equipment grille slot',(x+side*2.5,aft-16+i*3.4,z),(.12,1.9,.22),'Black',.03)
    for k in range(3):beam('Aft exposed feed pipe',(x+side*2.9,aft-17,-1+k),(x+side*2.9,-L+2,-1+k),.15,'Bronze' if k==1 else 'Panel')

# Radio masts and dishes are retained; exterior ladders, solar assemblies and lettering are omitted.
for y,z,side in [(aft+8,16,1),(L+10,18,-1)]:
    x=side*W*.18
    box('Antenna plinth',(x,y,z),(5,5,1.5),'Panel',.25)
    for dx in [-1.8,1.8]:
        for dy in [-1.8,1.8]:beam('Antenna foundation leg',(x+dx,y+dy,H),(x+dx,y+dy,z-.5),.22)
    beam('Antenna tower',(x,y,z),(x,y,z+13),.16)
    beam('Antenna second leg',(x+1.4,y,z),(x+1.4,y,z+10),.12)
    for h in range(2,11,2):beam('Antenna cross bracing',(x,y,z+h),(x+1.4,y,z+h+1.5),.07)
    beam('Antenna whip',(x,y,z+12),(x,y,z+19),.07)
    for h in [5,9,12]:beam('Radio horizontal element',(x-2,y,z+h),(x+2,y,z+h),.055)
    cyl('Radar dish',(x+4,y,z+4),2.5,.22,'Panel',axis=(0,-.65,.75))
    beam('Radar feed',(x+4,y-1.7,z+5.8),(x+4,y-3.0,z+7),.07)

# Break up the large machinery and bow armor surfaces with readable, layered service detail.
for side in [-1,1]:
    angle=side*math.atan((W-W*.43)/29)
    for f in [.25,.65]:
        x=side*(W*(1-.57*f)+.45);y=L+29*f
        for z in [-4.5,4.5]:
            box('Bow bevel armor plate',(x,y,z),(.65,9,6.8),'Panel',.22,rot=(0,0,angle))
            box('Bow plate inset',(x+side*.4,y,z),(.1,6.5,4.6),'Hull',.07,rot=(0,0,angle))
            for dz in [-2.7,2.7]:
                for dy in [-3.4,3.4]:ball('Armor fastener',(x+side*.45,y+dy,z+dz),(.13,.13,.13),'Bronze')
    for zsign in [-1,1]:
        for ix in range(3):
            x=side*(7+ix*9)
            box('Rear machinery raised plinth',(x,-L-9,zsign*(H+1)),(7.5,13,1.6),'Black',.2)
            box('Rear machinery service lid',(x,-L-9,zsign*(H+1.95)),(7,12,.5),'Panel',.14)
            for i in range(8):box('Heat exchanger louvre',(x,-L-14+i*1.3,zsign*(H+2.26)),(5.5,.55,.18),'Black',.03)
        for k in range(4):
            x=side*(4+k*.8);beam('Reactor manifold line',(x,-L-20,zsign*(H+2.2)),(x,-L+8,zsign*(H+2.2)),.19,'Bronze' if k==1 else 'Panel')
    for f in [-.45,.02,.49]:
        y=L*f;x=side*(W*.6)
        for n in range(5):box('Top cassette side ribs',(x+(n-2)*2.9,y,H+6.2),(.16,L*.35,.21),'Panel',.035)
        box('Top cassette access panel',(x,y,H+6.23),(6,4,.18),'Hull',.08)
        box('Top cassette caution stripe',(x-3.5,y,H+6.22),(.65,4,.16),'Bronze',.025)
box('Reactor raised superstructure',(0,-L-5,H+4.2),(20,14,6.4),'Hull',.7)
for x in [-8,-4,0,4,8]:
    box('Reactor cabinet inset',(x,-L+2.15,H+4.2),(3.2,.24,4.6),'Panel',.13)
    for z in [H+3,H+4,H+5]:box('Reactor cabinet louvre',(x,-L+2.31,z),(2.4,.1,.25),'Black',.02)
for zsign in [-1,1]:
    for x in [-W*.48,W*.48]:
        cyl('Dorsal auxiliary pressure tank',(x,-L+2,zsign*(H+9)),2.2,10,'Tank')
        for y in [-L-1,-L+5]:box('Auxiliary tank saddle',(x,y,zsign*(H+4.3)),(3.2,1,8.6),'Hull',.2)
        for y in [-L-2,-L+6]:ring('Auxiliary tank clamp',(x,y,zsign*(H+9)),2.25,.16,'Bronze')
        ball('Auxiliary tank rounded cap',(x,-L+7,zsign*(H+9)),(2.2,1.4,2.2))

# Remove only explicit parts identified by the exterior contact review.
# Construction/name assignment is unchanged so retained geometry stays identical.
removal_path=ART/'FloatingRemoval.json'
if removal_path.exists():
    removal_names=set(json.loads(removal_path.read_text(encoding='utf-8'))['names'])
    missing=removal_names-{o.name for o in mesh_objects}
    if missing:raise RuntimeError('Reviewed removal name missing: '+str(sorted(missing)))
    for o in list(mesh_objects):
        if o.name in removal_names:
            mesh_objects.remove(o);bpy.data.objects.remove(o,do_unlink=True)
    print('REVIEWED_FLOATING_PARTS_REMOVED',len(removal_names),flush=True)

# Apply modeling modifiers, provide non-stretched world-space box UVs, join by material for Unity.
print('GEOMETRY_CREATED',len(mesh_objects),flush=True)
bpy.context.view_layer.update()
for o in mesh_objects:
    bpy.context.view_layer.objects.active=o;o.select_set(True)
    for modifier in list(o.modifiers):
        try:bpy.ops.object.modifier_apply(modifier=modifier.name)
        except RuntimeError:pass
    if o.type=='MESH' and o.data.polygons:
        if not o.data.uv_layers:o.data.uv_layers.new()
        uv=o.data.uv_layers.active.data
        for poly in o.data.polygons:
            normal_world=o.matrix_world.to_3x3()@poly.normal;axis=max(range(3),key=lambda k:abs(normal_world[k])); axes=[k for k in range(3) if k!=axis]
            for li in poly.loop_indices:
                v=o.matrix_world@o.data.vertices[o.data.loops[li].vertex_index].co;uv[li].uv=(v[axes[0]]/4,v[axes[1]]/4)
    o.select_set(False)
print('SURFACES_READY',flush=True)
for name,m in mats.items():
    objects=[o for o in list(bpy.context.scene.objects) if o.type=='MESH' and o.data.materials and o.data.materials[0]==m]
    if not objects:continue
    bpy.ops.object.select_all(action='DESELECT')
    for o in objects:o.select_set(True)
    bpy.context.view_layer.objects.active=objects[0];bpy.ops.object.join();o=bpy.context.object;o.name='Exterior_'+name
    bpy.context.scene.cursor.location=(0,0,0);bpy.ops.object.origin_set(type='ORIGIN_CURSOR')
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.context.scene.objects:
    if o.type=='MESH':o.select_set(True)
bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
bpy.ops.wm.save_as_mainfile(filepath=str(ART/'blender/PegasusExterior.blend'))
bpy.ops.export_scene.fbx(filepath=str(OUT/'Models/PegasusExterior.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,bake_space_transform=True,add_leaf_bones=False,path_mode='AUTO')

# A Blender reference view is supplementary; completion uses the actual Unity sample.
scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=24
scene.render.resolution_x=1600;scene.render.resolution_y=1000;scene.render.resolution_percentage=100
scene.world.color=(.018,.023,.03)
def area(p,power,size,col):
    bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=power;o.data.shape='DISK';o.data.size=size;o.data.color=col;o.rotation_euler=(-o.location).to_track_quat('-Z','Y').to_euler()
area((70,40,140),260000,100,(.8,.9,1));area((-100,-30,70),180000,90,(.36,.57,1));area((10,-120,45),220000,65,(1,.65,.35))
bpy.ops.object.camera_add(location=(170,140,95));camera=bpy.context.object;camera.rotation_euler=(Vector((0,-8,0))-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.type='ORTHO';camera.data.ortho_scale=205;scene.camera=camera
scene.render.filepath=str(ART/'renders/BlenderOverview.png');bpy.ops.render.render(write_still=True)
print('PEGASUS_EXTERIOR_GENERATED')
