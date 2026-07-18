import bmesh
import bpy
import os
import sys
from mathutils.bvhtree import BVHTree


# A coarser closing pass removes the many narrow tunnels left between the source model's
# overlapping wax drips.  The transform proxy needs a solid sculpting mass, not merely one
# connected component with dozens of visually open passages.
VOXEL_FRACTION = 0.030
CLOSING_VOXEL_FRACTIONS = (0.040, 0.045, 0.050, 0.055, 0.060, 0.070, 0.080)

# One topology-preserving split gives the closed proxy enough runtime deformation resolution
# without pulling its vertices back onto the disconnected Bake components.
SUBDIVISION_CUTS = 1
COARSE_PROJECTION_EDGE_FACTOR = 0.80
REFINED_PROJECTION_EDGE_FACTOR = 0.65
DETAIL_PROJECTION_EDGE_FACTOR = 0.45
USE_FILTERED_BAKE_COMPONENTS = True


def arguments_after_double_dash():
    if "--" not in sys.argv:
        raise RuntimeError("Expected source and output mesh-data paths after --")
    arguments = sys.argv[sys.argv.index("--") + 1 :]
    if len(arguments) != 2:
        raise RuntimeError("Expected exactly two arguments: source and output mesh data")
    return arguments


def keep_largest_connected_component(mesh):
    editable = bmesh.new()
    editable.from_mesh(mesh)
    remaining = set(editable.verts)
    components = []
    while remaining:
        start = remaining.pop()
        stack = [start]
        component = {start}
        while stack:
            current = stack.pop()
            for edge in current.link_edges:
                neighbour = edge.other_vert(current)
                if neighbour in remaining:
                    remaining.remove(neighbour)
                    stack.append(neighbour)
                    component.add(neighbour)
        components.append(component)

    components.sort(key=len, reverse=True)
    removed_sizes = [len(component) for component in components[1:]]
    if len(components) > 1:
        bmesh.ops.delete(
            editable,
            geom=[vertex for component in components[1:] for vertex in component],
            context="VERTS",
        )
        editable.to_mesh(mesh)
        mesh.update()
    editable.free()
    return len(components[0]), removed_sizes


def connected_component_details(mesh):
    editable = bmesh.new()
    editable.from_mesh(mesh)
    remaining = set(editable.verts)
    details = []
    while remaining:
        start = remaining.pop()
        stack = [start]
        component = {start}
        while stack:
            current = stack.pop()
            for edge in current.link_edges:
                neighbour = edge.other_vert(current)
                if neighbour in remaining:
                    remaining.remove(neighbour)
                    stack.append(neighbour)
                    component.add(neighbour)
        minimum = tuple(min(vertex.co[axis] for vertex in component) for axis in range(3))
        maximum = tuple(max(vertex.co[axis] for vertex in component) for axis in range(3))
        component_faces = {
            face
            for vertex in component
            for face in vertex.link_faces
            if all(face_vertex in component for face_vertex in face.verts)
        }
        signed_volume = 0.0
        for face in component_faces:
            if len(face.verts) == 3:
                first, second, third = (vertex.co for vertex in face.verts)
                signed_volume += first.dot(second.cross(third)) / 6.0
        details.append((len(component), minimum, maximum, signed_volume))
    editable.free()
    return sorted(details, key=lambda detail: detail[0], reverse=True)


def reverse_large_opposite_winding_components(mesh):
    editable = bmesh.new()
    editable.from_mesh(mesh)
    remaining = set(editable.verts)
    components = []
    while remaining:
        start = remaining.pop()
        stack = [start]
        component = {start}
        while stack:
            current = stack.pop()
            for edge in current.link_edges:
                neighbour = edge.other_vert(current)
                if neighbour in remaining:
                    remaining.remove(neighbour)
                    stack.append(neighbour)
                    component.add(neighbour)
        component_faces = {
            face
            for vertex in component
            for face in vertex.link_faces
            if all(face_vertex in component for face_vertex in face.verts)
        }
        signed_volume = 0.0
        for face in component_faces:
            if len(face.verts) == 3:
                first, second, third = (vertex.co for vertex in face.verts)
                signed_volume += first.dot(second.cross(third)) / 6.0
        components.append((component, signed_volume))

    components.sort(key=lambda item: len(item[0]), reverse=True)
    main_component, main_volume = components[0]
    minimum_large_size = len(main_component) * 0.1
    reversed_sizes = []
    reversed_faces = []
    for component, signed_volume in components[1:]:
        if len(component) >= minimum_large_size and signed_volume * main_volume < 0.0:
            reversed_sizes.append(len(component))
            reversed_faces.extend(
                face
                for vertex in component
                for face in vertex.link_faces
                if all(face_vertex in component for face_vertex in face.verts)
            )
    if reversed_faces:
        bmesh.ops.reverse_faces(editable, faces=list(set(reversed_faces)))
        editable.to_mesh(mesh)
        mesh.update()
    editable.free()
    return reversed_sizes


def read_unity_mesh_data(path):
    vertices = []
    faces = []
    face_submeshes = []
    with open(path, "r", encoding="utf-8") as source_file:
        for raw_line in source_file:
            parts = raw_line.strip().split()
            if len(parts) == 4 and parts[0] == "v":
                unity_x, unity_y, unity_z = (float(value) for value in parts[1:])
                vertices.append((unity_x, -unity_z, unity_y))
            elif len(parts) == 5 and parts[0] == "t":
                submesh, first, second, third = (int(value) for value in parts[1:])
                faces.append((first, second, third))
                face_submeshes.append(submesh)
    if not vertices or not faces:
        raise RuntimeError("Source Unity Bake mesh data contains no geometry")
    return vertices, faces, face_submeshes


def write_unity_mesh_data(path, mesh):
    with open(path, "w", encoding="utf-8", newline="\n") as output_file:
        output_file.write("SMORZANDO_MESH_DATA_V1\n")
        for vertex in mesh.vertices:
            blender_x, blender_y, blender_z = vertex.co
            output_file.write(
                f"v {blender_x:.9g} {blender_z:.9g} {-blender_y:.9g}\n"
            )
        for polygon in mesh.polygons:
            if len(polygon.vertices) != 3:
                raise RuntimeError("Voxel remesh output contains a non-triangle polygon")
            first, second, third = polygon.vertices
            output_file.write(f"t {first} {second} {third}\n")


def align_to_source_bounds(mesh, source_minimum, source_maximum):
    source_size = [source_maximum[axis] - source_minimum[axis] for axis in range(3)]
    current_minimum = [min(vertex.co[axis] for vertex in mesh.vertices) for axis in range(3)]
    current_maximum = [max(vertex.co[axis] for vertex in mesh.vertices) for axis in range(3)]
    current_size = [current_maximum[axis] - current_minimum[axis] for axis in range(3)]
    source_center = [(source_minimum[axis] + source_maximum[axis]) * 0.5 for axis in range(3)]
    current_center = [(current_minimum[axis] + current_maximum[axis]) * 0.5 for axis in range(3)]
    for vertex in mesh.vertices:
        for axis in range(3):
            scale = source_size[axis] / max(current_size[axis], 1.0e-9)
            vertex.co[axis] = source_center[axis] + (vertex.co[axis] - current_center[axis]) * scale
    mesh.update()


def recalculate_outward_normals(mesh):
    editable = bmesh.new()
    editable.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(editable, faces=list(editable.faces))
    signed_volume = sum(
        face.verts[0].co.dot(face.verts[1].co.cross(face.verts[2].co)) / 6.0
        for face in editable.faces
        if len(face.verts) == 3
    )
    if signed_volume < 0.0:
        bmesh.ops.reverse_faces(editable, faces=list(editable.faces))
        signed_volume = -signed_volume
    editable.to_mesh(mesh)
    mesh.update()
    editable.free()
    return signed_volume


def calculate_signed_volume(mesh):
    signed_volume = 0.0
    for polygon in mesh.polygons:
        if len(polygon.vertices) != 3:
            continue
        first, second, third = (mesh.vertices[index].co for index in polygon.vertices)
        signed_volume += first.dot(second.cross(third)) / 6.0
    return signed_volume


def subdivide_surface(mesh):
    editable = bmesh.new()
    editable.from_mesh(mesh)
    bmesh.ops.triangulate(editable, faces=list(editable.faces))
    bmesh.ops.subdivide_edges(
        editable,
        edges=list(editable.edges),
        cuts=SUBDIVISION_CUTS,
        use_grid_fill=True,
    )
    bmesh.ops.triangulate(editable, faces=list(editable.faces))
    bmesh.ops.recalc_face_normals(editable, faces=list(editable.faces))
    editable.to_mesh(mesh)
    mesh.update()
    editable.free()


def project_toward_reference(mesh, reference_mesh, edge_factor):
    reference_tree = BVHTree.FromPolygons(
        [vertex.co.copy() for vertex in reference_mesh.vertices],
        [tuple(polygon.vertices) for polygon in reference_mesh.polygons],
        all_triangles=False,
    )
    editable = bmesh.new()
    editable.from_mesh(mesh)
    bmesh.ops.recalc_face_normals(editable, faces=list(editable.faces))
    editable.verts.ensure_lookup_table()
    projected_count = 0
    total_applied_distance = 0.0
    maximum_applied_distance = 0.0
    for vertex in editable.verts:
        if not vertex.link_edges or vertex.normal.length_squared <= 1.0e-12:
            continue
        local_edge_length = sum(edge.calc_length() for edge in vertex.link_edges) / len(vertex.link_edges)
        maximum_distance = local_edge_length * edge_factor
        vertex_normal = vertex.normal.normalized()
        candidates = []
        for direction in (vertex_normal, -vertex_normal):
            target, target_normal, _face_index, distance = reference_tree.ray_cast(
                vertex.co,
                direction,
                maximum_distance,
            )
            if target is None or target_normal is None or distance <= 1.0e-9:
                continue
            if target_normal.normalized().dot(vertex_normal) <= 0.15:
                continue
            candidates.append((distance, target))
        if not candidates:
            continue
        applied_distance, target = min(candidates, key=lambda candidate: candidate[0])
        vertex.co = target
        projected_count += 1
        total_applied_distance += applied_distance
        maximum_applied_distance = max(maximum_applied_distance, applied_distance)
    bmesh.ops.recalc_face_normals(editable, faces=list(editable.faces))
    editable.to_mesh(mesh)
    mesh.update()
    editable.free()
    average_applied_distance = total_applied_distance / projected_count if projected_count else 0.0
    return projected_count, average_applied_distance, maximum_applied_distance


def close_until_genus_zero(surface, source_size):
    reports = []
    selected_fraction = None
    for closing_fraction in CLOSING_VOXEL_FRACTIONS:
        surface.data.remesh_voxel_size = max(source_size) * closing_fraction
        surface.data.remesh_voxel_adaptivity = 0.0
        bpy.ops.object.voxel_remesh()
        bpy.context.view_layer.update()
        keep_largest_connected_component(surface.data)
        boundary_edges, non_manifold_edges, euler_characteristic = mesh_topology_details(surface.data)
        reports.append(
            (
                closing_fraction,
                len(surface.data.vertices),
                len(surface.data.polygons),
                boundary_edges,
                non_manifold_edges,
                euler_characteristic,
            )
        )
        if boundary_edges == 0 and non_manifold_edges == 0 and euler_characteristic == 2:
            selected_fraction = closing_fraction
            break
    if selected_fraction is None:
        raise RuntimeError(
            "Transform surface did not reach genus zero during adaptive voxel closing: "
            + str(reports)
        )
    return selected_fraction, reports


def mesh_topology_details(mesh):
    editable = bmesh.new()
    editable.from_mesh(mesh)
    boundary_edges = sum(1 for edge in editable.edges if edge.is_boundary)
    non_manifold_edges = sum(1 for edge in editable.edges if not edge.is_manifold)
    euler_characteristic = len(editable.verts) - len(editable.edges) + len(editable.faces)
    editable.free()
    return boundary_edges, non_manifold_edges, euler_characteristic


source_path, output_path = arguments_after_double_dash()
output_path = os.path.abspath(output_path)
blend_path = os.path.splitext(output_path)[0] + ".blend"
os.makedirs(os.path.dirname(output_path), exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
source_vertices, source_faces, source_face_submeshes = read_unity_mesh_data(source_path)
surface_mesh = bpy.data.meshes.new("Smorzando_TransformSurface")
surface_mesh.from_pydata(source_vertices, [], source_faces)
surface_mesh.update()
for polygon, submesh in zip(surface_mesh.polygons, source_face_submeshes):
    polygon.material_index = submesh
surface = bpy.data.objects.new("Smorzando_TransformSurface", surface_mesh)
bpy.context.collection.objects.link(surface)

bpy.context.view_layer.objects.active = surface
surface.select_set(True)
source_minimum = [min(vertex.co[axis] for vertex in surface.data.vertices) for axis in range(3)]
source_maximum = [max(vertex.co[axis] for vertex in surface.data.vertices) for axis in range(3)]
source_size = [source_maximum[axis] - source_minimum[axis] for axis in range(3)]
source_components = connected_component_details(surface.data)
print(
    "SMORZANDO_SOURCE_COMPONENTS "
    + " | ".join(
        f"vertices={size},min={minimum},max={maximum},signed_volume={signed_volume:.9g}"
        for size, minimum, maximum, signed_volume in source_components[:24]
    )
)
# Preserve the large shell that contributes visible torso coverage, but make its winding agree
# with the main surface so Unity's runtime normal recalculation lights the wax continuously.
reversed_opposite_winding_components = reverse_large_opposite_winding_components(surface.data)
print(
    "SMORZANDO_REVERSED_OPPOSITE_WINDING_COMPONENTS "
    + str(reversed_opposite_winding_components)
)
source_minimum = [min(vertex.co[axis] for vertex in surface.data.vertices) for axis in range(3)]
source_maximum = [max(vertex.co[axis] for vertex in surface.data.vertices) for axis in range(3)]
source_size = [source_maximum[axis] - source_minimum[axis] for axis in range(3)]
detail_reference_mesh = surface.data.copy()
voxel_size = max(source_size) * VOXEL_FRACTION
surface.data.remesh_voxel_size = voxel_size
surface.data.remesh_voxel_adaptivity = 0.0
bpy.ops.object.voxel_remesh()
bpy.context.view_layer.update()

# Starting at the previous closing resolution, select the first coarser voxel size that removes
# every topological tunnel.  This preserves as much silhouette detail as possible while ensuring
# the runtime sculpt surface is a genus-zero closed mass.
selected_closing_fraction, closing_reports = close_until_genus_zero(surface, source_size)
print("SMORZANDO_ADAPTIVE_CLOSING " + str(closing_reports))

# Voxel closing slightly contracts the silhouette, so restore the exact exported Bake bounds
# before Unity imports the proxy.  This keeps the 3-second hand-off spatially aligned.
align_to_source_bounds(surface.data, source_minimum, source_maximum)

main_component_size, removed_component_sizes = keep_largest_connected_component(surface.data)

triangulated = bmesh.new()
triangulated.from_mesh(surface.data)
bmesh.ops.triangulate(triangulated, faces=list(triangulated.faces))
bmesh.ops.recalc_face_normals(triangulated, faces=list(triangulated.faces))
triangulated.to_mesh(surface.data)
surface.data.update()
triangulated.free()

coarse_projection = project_toward_reference(
    surface.data,
    detail_reference_mesh,
    COARSE_PROJECTION_EDGE_FACTOR,
)
align_to_source_bounds(surface.data, source_minimum, source_maximum)
subdivide_surface(surface.data)
refined_projection = project_toward_reference(
    surface.data,
    detail_reference_mesh,
    REFINED_PROJECTION_EDGE_FACTOR,
)
align_to_source_bounds(surface.data, source_minimum, source_maximum)
subdivide_surface(surface.data)
detail_projection = project_toward_reference(
    surface.data,
    detail_reference_mesh,
    DETAIL_PROJECTION_EDGE_FACTOR,
)
align_to_source_bounds(surface.data, source_minimum, source_maximum)

if USE_FILTERED_BAKE_COMPONENTS:
    refined_surface_mesh = surface.data
    surface.data = detail_reference_mesh
    bpy.data.meshes.remove(refined_surface_mesh)
    component_count = len(connected_component_details(surface.data))
    signed_volume = calculate_signed_volume(surface.data)
else:
    bpy.data.meshes.remove(detail_reference_mesh)
    component_count = 1
    signed_volume = recalculate_outward_normals(surface.data)

boundary_edges, non_manifold_edges, euler_characteristic = mesh_topology_details(surface.data)
if not USE_FILTERED_BAKE_COMPONENTS and (
    boundary_edges != 0 or non_manifold_edges != 0 or euler_characteristic != 2
):
    raise RuntimeError(
        "Refined transform surface is not a closed genus-zero manifold: "
        f"boundary_edges={boundary_edges}, non_manifold_edges={non_manifold_edges}, "
        f"euler={euler_characteristic}"
    )

for polygon in surface.data.polygons:
    polygon.use_smooth = True

surface.data.materials.clear()
material = bpy.data.materials.new("Smorzando_TransformWax")
material.diffuse_color = (0.22, 0.055, 0.035, 1.0)
material.roughness = 0.48
surface.data.materials.append(material)

minimum = [min(vertex.co[axis] for vertex in surface.data.vertices) for axis in range(3)]
maximum = [max(vertex.co[axis] for vertex in surface.data.vertices) for axis in range(3)]
print(
    "SMORZANDO_TRANSFORM_SURFACE "
    f"vertices={len(surface.data.vertices)} polygons={len(surface.data.polygons)} "
    f"components={component_count} main_component_before_cleanup={main_component_size} "
    f"removed_components={removed_component_sizes} voxel={voxel_size} "
    f"closing_fraction={selected_closing_fraction} subdivision_cuts={SUBDIVISION_CUTS} "
    f"coarse_projection={coarse_projection} refined_projection={refined_projection} "
    f"detail_projection={detail_projection} "
    f"surface_mode={'filtered_bake_components' if USE_FILTERED_BAKE_COMPONENTS else 'genus_zero_proxy'} "
    f"boundary_edges={boundary_edges} non_manifold_edges={non_manifold_edges} "
    f"euler={euler_characteristic} signed_volume={signed_volume:.9g} "
    f"min={tuple(minimum)} max={tuple(maximum)}"
)

bpy.ops.wm.save_as_mainfile(filepath=blend_path)
write_unity_mesh_data(output_path, surface.data)
